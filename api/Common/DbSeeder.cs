using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Api.Common;

/// <summary>
/// Seed dữ liệu mẫu cho môi trường Development (idempotent — chỉ tạo khi thiếu,
/// chạy lại nhiều lần không nhân đôi). Giúp test nhanh Ngày 14:
/// 5 tài khoản vai trò, quan hệ phụ huynh–con (Switch Profile), bảng tin, thông báo.
///
/// Mọi tài khoản mẫu dùng chung mật khẩu: 123456
/// </summary>
public static class DbSeeder
{
    private const string DemoPassword = "123456";

    /// <summary>
    /// Chạy seed. Nhận: [db] DbContext, [config] cấu hình (để nạp khóa PayOS). Trả về: Task.
    /// Thứ tự: user → liên kết phụ huynh-con → bảng tin → thông báo → học tập → học phí → cổng thanh toán.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, IConfiguration? config = null)
    {
        // 1) Tài khoản mẫu — tạo nếu chưa có (khớp theo SĐT).
        var admin = await EnsureUserAsync(db, "0900000000", "Admin Demo", UserRole.Admin);
        var teacher = await EnsureUserAsync(db, "0900000002", "Giao Vien Demo", UserRole.Teacher);
        var parent = await EnsureUserAsync(db, "0900000003", "Phu Huynh Demo", UserRole.Parent);
        var student1 = await EnsureUserAsync(db, "0900000004", "Hoc Sinh A", UserRole.Student);
        var student2 = await EnsureUserAsync(db, "0900000005", "Hoc Sinh B", UserRole.Student);
        await db.SaveChangesAsync(); // lưu để các user có Id

        // 2) Gán con cho phụ huynh (phục vụ Switch Profile - FR2.1).
        await EnsureLinkAsync(db, parent.Id, student1.Id);
        await EnsureLinkAsync(db, parent.Id, student2.Id);
        await db.SaveChangesAsync();

        // 2b) Dữ liệu học tập (Ngày 15): lớp + môn + TKB + điểm + bài tập.
        await SeedAcademicAsync(db, teacher);

        // 2c) Cấu hình cổng PayOS (Ngày 16) — nạp khóa từ appsettings nếu có.
        await SeedPayOsConfigAsync(db, config);

        // 3) Bảng tin mẫu — chỉ seed khi CHƯA có bảng tin nào.
        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.AddRange(
                new Announcement
                {
                    Title = "Chào mừng năm học mới 2026-2027",
                    Content = "Nhà trường kính chúc quý phụ huynh và các em học sinh một năm học nhiều thành công.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                },
                new Announcement
                {
                    Title = "Lịch họp phụ huynh đầu năm",
                    Content = "Buổi họp phụ huynh sẽ diễn ra vào Chủ nhật tuần này lúc 8h00 tại hội trường.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new Announcement
                {
                    Title = "Thông báo nghỉ lễ Quốc khánh 2/9",
                    Content = "Học sinh được nghỉ ngày 2/9. Lịch học trở lại bình thường vào ngày 3/9.",
                    Type = AnnouncementType.Global,
                    CreatedById = admin.Id,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        // 4) Thông báo cá nhân mẫu — chỉ seed khi CHƯA có thông báo nào.
        if (!await db.Notifications.AnyAsync())
        {
            db.Notifications.AddRange(
                new Notification
                {
                    UserId = parent.Id,
                    Title = "Điểm mới",
                    Message = "Con của bạn vừa có điểm kiểm tra môn Toán.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                },
                new Notification
                {
                    UserId = parent.Id,
                    Title = "Nhắc học phí",
                    Message = "Học phí tháng này sắp đến hạn thanh toán.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                },
                new Notification
                {
                    UserId = student1.Id,
                    Title = "Bài tập mới",
                    Message = "Bạn có bài tập môn Ngữ Văn cần hoàn thành.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                }
            );
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Tạo user nếu SĐT chưa tồn tại; trả về user (cũ hoặc mới).</summary>
    private static async Task<User> EnsureUserAsync(
        AppDbContext db, string phone, string fullName, UserRole role)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
        if (user != null) return user;

        user = new User
        {
            Phone = phone,
            Username = phone, // Username = Phone theo quy ước dự án
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
            Role = role,
            IsPhoneVerified = true, // để đăng nhập được ngay
            IsLocked = false,
        };
        db.Users.Add(user);
        return user;
    }

    /// <summary>Thêm liên kết phụ huynh–con nếu chưa có.</summary>
    private static async Task EnsureLinkAsync(AppDbContext db, int parentId, int studentId)
    {
        var exists = await db.StudentParents
            .AnyAsync(sp => sp.ParentId == parentId && sp.StudentId == studentId);
        if (!exists)
            db.StudentParents.Add(new StudentParent { ParentId = parentId, StudentId = studentId });
    }

    /// <summary>
    /// Seed dữ liệu học tập (Ngày 15): 1 học kỳ, các môn, 1 lớp "10A1",
    /// ghi danh MỌI học sinh chưa có lớp vào lớp này (gồm cả tài khoản test tự tạo),
    /// phân công GV, thời khóa biểu tuần, điểm đã công bố và vài bài tập mẫu.
    /// Idempotent: chỉ tạo phần nào còn thiếu.
    ///
    /// Nhận: [db] DbContext, [teacher] giáo viên demo (dạy mọi môn cho gọn).
    /// </summary>
    private static async Task SeedAcademicAsync(AppDbContext db, User teacher)
    {
        // --- Học kỳ theo lịch THPT VN: mỗi năm học = HK1 + HK2, giữa 2 kỳ nghỉ Tết, cuối năm nghỉ hè ---
        // Năm học YYYY-(YYYY+1):
        //   HK1: 05/09/YYYY → 15/01/(YYYY+1)
        //   HK2: 01/02/(YYYY+1) → 25/05/(YYYY+1)
        //   Nghỉ hè: ~26/05 → đầu tháng 9 năm sau (không có Semester)
        var semesters = await EnsureVnSemestersAsync(db, startSchoolYear: 2025, yearCount: 4);

        // --- Các môn học (tạo nếu thiếu, khớp theo Code) ---
        var math = await EnsureSubjectAsync(db, "Toán", "MATH");
        var literature = await EnsureSubjectAsync(db, "Ngữ Văn", "LIT");
        var english = await EnsureSubjectAsync(db, "Tiếng Anh", "ENG");
        var physics = await EnsureSubjectAsync(db, "Vật Lý", "PHY");
        var chemistry = await EnsureSubjectAsync(db, "Hóa Học", "CHE");
        await db.SaveChangesAsync();

        var subjects = new[] { math, literature, english, physics, chemistry };
        Class? primaryClass = null;

        // Gắn lớp 10A1 seed cũ (nếu có) vào HK1 2026-2027 trước khi tạo theo từng kỳ.
        var hk1_2627 = semesters.First(s => s.Name == "Học kỳ 1 (2026-2027)");
        var legacy10A1 = await db.Classes.FirstOrDefaultAsync(c => c.Name == "10A1");
        if (legacy10A1 != null &&
            !await db.Classes.AnyAsync(c => c.Name == "10A1" && c.SemesterId == hk1_2627.Id))
        {
            legacy10A1.SemesterId = hk1_2627.Id;
            await db.SaveChangesAsync();
        }

        foreach (var sem in semesters)
        {
            var cls = await EnsureClass10A1Async(db, sem, teacher, subjects,
                math, literature, english, physics, chemistry);

            var today = DateTime.UtcNow.Date;
            if (sem.StartDate.Date <= today && today <= sem.EndDate.Date)
                primaryClass = cls;
            if (primaryClass == null && sem.Name == "Học kỳ 1 (2026-2027)")
                primaryClass = cls;
        }

        primaryClass ??= await db.Classes.FirstAsync(c => c.Name == "10A1");
        var cls2 = primaryClass;
        var semester = await db.Semesters.FindAsync(cls2.SemesterId) ?? hk1_2627;

        // --- Điểm đã công bố đủ form THPT (Miệng / 15p / 1 tiết / GK / CK) ---
        await EnsureThptGradesAsync(
            db, cls2, semester, teacher,
            math, literature, english, physics);

        // --- Bài tập mẫu ---
        if (!await db.Assignments.AnyAsync(a => a.ClassId == cls2.Id))
        {
            db.Assignments.AddRange(
                new Assignment
                {
                    Title = "Bài tập Toán - Chương 1",
                    Description = "Làm các bài 1 đến 10 trang 25 SGK.",
                    DueDate = DateTime.UtcNow.AddDays(-2),
                    MaxScore = 10,
                    ClassId = cls2.Id,
                    SubjectId = math.Id,
                    CreatedByTeacherId = teacher.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                },
                new Assignment
                {
                    Title = "Luyện tập Ngữ Văn - Bài thơ",
                    Description = "Viết đoạn văn cảm nhận về bài thơ đã học (khoảng 200 từ).",
                    DueDate = DateTime.UtcNow.AddDays(3),
                    MaxScore = 10,
                    ClassId = cls2.Id,
                    SubjectId = literature.Id,
                    CreatedByTeacherId = teacher.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }
            );
            await db.SaveChangesAsync();
        }

        // --- Học phí ---
        var tuition = await EnsureFeeCategoryAsync(db, "Học phí Học kỳ 1", 1500000);
        var insurance = await EnsureFeeCategoryAsync(db, "Bảo hiểm y tế", 800000);
        await db.SaveChangesAsync();

        if (!await db.FeeInvoices.AnyAsync())
        {
            var studentIds = await db.ClassStudents
                .Where(cs => cs.ClassId == cls2.Id)
                .Select(cs => cs.StudentId)
                .ToListAsync();

            foreach (var sid in studentIds)
            {
                // 1 hóa đơn học phí chưa đóng + 1 BHYT chưa đóng cho mỗi HS.
                db.FeeInvoices.Add(new FeeInvoice
                {
                    StudentId = sid,
                    FeeCategoryId = tuition.Id,
                    Amount = tuition.DefaultAmount,
                    DueDate = DateTime.UtcNow.AddDays(14),
                    Status = FeePaymentStatus.Pending,
                    IsPaid = false,
                    Note = "Học phí học kỳ 1 năm học 2026-2027",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                });
                db.FeeInvoices.Add(new FeeInvoice
                {
                    StudentId = sid,
                    FeeCategoryId = insurance.Id,
                    Amount = insurance.DefaultAmount,
                    DueDate = DateTime.UtcNow.AddDays(7),
                    Status = FeePaymentStatus.Pending,
                    IsPaid = false,
                    Note = "Bảo hiểm y tế năm học 2026-2027",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                });
            }
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Nạp/cập nhật cấu hình cổng PayOS từ appsettings (section "PayOS").
    /// Không có khóa (thiếu ClientId/ApiKey/ChecksumKey) → bỏ qua, dùng chế độ dev stub.
    /// </summary>
    private static async Task SeedPayOsConfigAsync(AppDbContext db, IConfiguration? config)
    {
        if (config == null) return;

        var clientId = config["PayOS:ClientId"];
        var apiKey = config["PayOS:ApiKey"];
        var checksumKey = config["PayOS:ChecksumKey"];

        // Thiếu bất kỳ khóa nào → không cấu hình (giữ dev stub / giả lập).
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(checksumKey))
        {
            return;
        }

        var configJson = JsonSerializer.Serialize(new
        {
            ClientId = clientId,
            ApiKey = apiKey,
            ChecksumKey = checksumKey,
            ReturnUrl = config["PayOS:ReturnUrl"] ?? "",
            CancelUrl = config["PayOS:CancelUrl"] ?? "",
        });

        var existing = await db.PaymentGatewayConfigs
            .FirstOrDefaultAsync(c => c.Provider == "PayOS");

        if (existing == null)
        {
            db.PaymentGatewayConfigs.Add(new PaymentGatewayConfig
            {
                Provider = "PayOS",
                IsEnabled = true,
                ConfigJson = configJson,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            // Luôn đồng bộ theo appsettings để đổi khóa dễ dàng.
            existing.IsEnabled = true;
            existing.ConfigJson = configJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo đủ đầu điểm form sổ điểm THPT (hệ số 1/1/2/2/3) cho vài môn demo.
    /// Chỉ thêm bản ghi còn thiếu — không ghi đè điểm đã có.
    /// </summary>
    private static async Task EnsureThptGradesAsync(
        AppDbContext db,
        Class cls,
        Semester semester,
        User teacher,
        Subject math,
        Subject literature,
        Subject english,
        Subject physics)
    {
        var studentIds = await db.ClassStudents
            .Where(cs => cs.ClassId == cls.Id)
            .Select(cs => cs.StudentId)
            .ToListAsync();
        if (studentIds.Count == 0) return;

        // (môn, loại, điểm) — Miệng HS1, 15 phút HS1, 1 tiết HS2, GK HS2, CK HS3.
        var gradePlan = new (Subject Subject, string Type, double Score)[]
        {
            (math, "Oral", 8.5),
            (math, "Quiz15", 7.5),
            (math, "OnePeriod", 8.0),
            (math, "Midterm", 7.5),
            (math, "Final", 8.0),

            (literature, "Oral", 8.0),
            (literature, "Quiz15", 7.0),
            (literature, "OnePeriod", 7.5),
            (literature, "Midterm", 8.0),
            (literature, "Final", 7.5),

            (english, "Oral", 9.0),
            (english, "Quiz15", 8.5),
            (english, "OnePeriod", 8.0),
            (english, "Midterm", 8.5),
            (english, "Final", 9.0),

            // Vật Lý: TB có hệ số < 5 → demo badge "Chưa đạt".
            (physics, "Oral", 5.0),
            (physics, "Quiz15", 4.5),
            (physics, "OnePeriod", 4.0),
            (physics, "Midterm", 4.0),
            (physics, "Final", 3.0),
        };

        var existingKeys = await db.Grades
            .Where(g => g.ClassId == cls.Id && g.SemesterId == semester.Id)
            .Select(g => new { g.StudentId, g.SubjectId, g.AssessmentType })
            .ToListAsync();
        var existing = existingKeys
            .Select(k => $"{k.StudentId}|{k.SubjectId}|{k.AssessmentType}")
            .ToHashSet();

        var added = 0;
        foreach (var sid in studentIds)
        {
            foreach (var gp in gradePlan)
            {
                var key = $"{sid}|{gp.Subject.Id}|{gp.Type}";
                if (existing.Contains(key)) continue;

                db.Grades.Add(new Grade
                {
                    StudentId = sid,
                    ClassId = cls.Id,
                    SubjectId = gp.Subject.Id,
                    SemesterId = semester.Id,
                    AssessmentType = gp.Type,
                    Score = gp.Score,
                    Status = GradeStatus.Published,
                    CreatedByTeacherId = teacher.Id,
                    PublishedAt = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                });
                existing.Add(key);
                added++;
            }
        }

        if (added > 0)
            await db.SaveChangesAsync();
    }

    /// <summary>Tạo loại khoản thu nếu tên chưa tồn tại; trả về (cũ hoặc mới).</summary>
    private static async Task<FeeCategory> EnsureFeeCategoryAsync(
        AppDbContext db, string name, decimal defaultAmount)
    {
        var cat = await db.FeeCategories.FirstOrDefaultAsync(c => c.Name == name);
        if (cat != null) return cat;

        cat = new FeeCategory
        {
            Name = name,
            DefaultAmount = defaultAmount,
            IsActive = true,
        };
        db.FeeCategories.Add(cat);
        return cat;
    }

    /// <summary>Tạo môn học nếu Code chưa tồn tại; trả về môn (cũ hoặc mới).</summary>
    private static async Task<Subject> EnsureSubjectAsync(AppDbContext db, string name, string code)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Code == code);
        if (subject != null) return subject;

        subject = new Subject { Name = name, Code = code };
        db.Subjects.Add(subject);
        return subject;
    }

    /// <summary>
    /// Seed các học kỳ theo lịch THPT VN (2 kỳ/năm, có khoảng nghỉ hè giữa các năm học).
    /// </summary>
    private static async Task<List<Semester>> EnsureVnSemestersAsync(
        AppDbContext db, int startSchoolYear, int yearCount)
    {
        var result = new List<Semester>();
        for (var y = startSchoolYear; y < startSchoolYear + yearCount; y++)
        {
            // HK1: 5/9/y → 15/1/(y+1) | HK2: 1/2/(y+1) → 25/5/(y+1)
            var specs = new (string Name, DateTime Start, DateTime End)[]
            {
                ($"Học kỳ 1 ({y}-{y + 1})", new DateTime(y, 9, 5), new DateTime(y + 1, 1, 15)),
                ($"Học kỳ 2 ({y}-{y + 1})", new DateTime(y + 1, 2, 1), new DateTime(y + 1, 5, 25)),
            };
            foreach (var (name, start, end) in specs)
            {
                var sem = await db.Semesters.FirstOrDefaultAsync(s => s.Name == name);
                if (sem == null)
                {
                    sem = new Semester { Name = name, StartDate = start, EndDate = end };
                    db.Semesters.Add(sem);
                    await db.SaveChangesAsync();
                }
                else
                {
                    // Đồng bộ lại khoảng ngày (idempotent fix lịch cũ).
                    if (sem.StartDate.Date != start.Date || sem.EndDate.Date != end.Date)
                    {
                        sem.StartDate = start;
                        sem.EndDate = end;
                        await db.SaveChangesAsync();
                    }
                }
                result.Add(sem);
            }
        }
        return result;
    }

    /// <summary>Đảm bảo lớp 10A1 của 1 kỳ + ghi danh + phân công + TKB.</summary>
    private static async Task<Class> EnsureClass10A1Async(
        AppDbContext db,
        Semester semester,
        User teacher,
        Subject[] subjects,
        Subject math, Subject literature, Subject english, Subject physics, Subject chemistry)
    {
        var cls = await db.Classes.FirstOrDefaultAsync(c =>
            c.Name == "10A1" && c.SemesterId == semester.Id);
        if (cls == null)
        {
            cls = new Class { Name = "10A1", SemesterId = semester.Id };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
        }

        var studentIds = await db.Users
            .Where(u => u.Role == UserRole.Student)
            .Select(u => u.Id)
            .ToListAsync();
        var enrolled = await db.ClassStudents
            .Where(cs => cs.ClassId == cls.Id)
            .Select(cs => cs.StudentId)
            .ToListAsync();
        var enrolledSet = enrolled.ToHashSet();
        foreach (var sid in studentIds)
        {
            if (!enrolledSet.Contains(sid))
                db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = sid });
        }
        await db.SaveChangesAsync();

        var assignedSubjectIds = await db.TeacherAssignments
            .Where(ta => ta.TeacherId == teacher.Id && ta.ClassId == cls.Id)
            .Select(ta => ta.SubjectId)
            .ToListAsync();
        var assignedSet = assignedSubjectIds.ToHashSet();
        foreach (var subj in subjects)
        {
            if (!assignedSet.Contains(subj.Id))
            {
                db.TeacherAssignments.Add(new TeacherAssignment
                {
                    TeacherId = teacher.Id,
                    ClassId = cls.Id,
                    SubjectId = subj.Id,
                });
            }
        }
        await db.SaveChangesAsync();

        await EnsureTimetableForClassAsync(db, cls, teacher, math, literature, english, physics, chemistry);
        return cls;
    }

    private static async Task EnsureTimetableForClassAsync(
        AppDbContext db, Class cls, User teacher,
        Subject math, Subject literature, Subject english, Subject physics, Subject chemistry)
    {
        if (await db.TimetableSlots.AnyAsync(t => t.ClassId == cls.Id)) return;

        var plan = new (int Day, int Period, Subject Subject, string Room)[]
        {
            (1, 1, math, "A101"),
            (1, 2, literature, "A101"),
            (1, 3, english, "A101"),
            (2, 1, physics, "Lab-1"),
            (2, 2, math, "A101"),
            (2, 3, chemistry, "Lab-2"),
            (3, 1, literature, "A101"),
            (3, 2, english, "A101"),
            (3, 3, math, "A101"),
            (4, 1, math, "A101"),
            (4, 2, physics, "Lab-1"),
            (4, 3, literature, "A101"),
            (5, 1, english, "A101"),
            (5, 2, chemistry, "Lab-2"),
            (5, 3, math, "A101"),
        };
        foreach (var p in plan)
        {
            db.TimetableSlots.Add(new TimetableSlot
            {
                ClassId = cls.Id,
                SubjectId = p.Subject.Id,
                TeacherId = teacher.Id,
                DayOfWeek = p.Day,
                Period = p.Period,
                Room = p.Room,
            });
        }
        await db.SaveChangesAsync();
    }
}
