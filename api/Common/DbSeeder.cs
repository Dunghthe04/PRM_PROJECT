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
        // --- Học kỳ hiện tại ---
        var semester = await db.Semesters.FirstOrDefaultAsync(s => s.Name == "Học kỳ 1 (2026-2027)");
        if (semester == null)
        {
            semester = new Semester
            {
                Name = "Học kỳ 1 (2026-2027)",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2027, 1, 15),
            };
            db.Semesters.Add(semester);
            await db.SaveChangesAsync();
        }

        // --- Các môn học (tạo nếu thiếu, khớp theo Code) ---
        var math = await EnsureSubjectAsync(db, "Toán", "MATH");
        var literature = await EnsureSubjectAsync(db, "Ngữ Văn", "LIT");
        var english = await EnsureSubjectAsync(db, "Tiếng Anh", "ENG");
        var physics = await EnsureSubjectAsync(db, "Vật Lý", "PHY");
        var chemistry = await EnsureSubjectAsync(db, "Hóa Học", "CHE");
        await db.SaveChangesAsync();

        // --- Lớp 10A1 thuộc học kỳ trên ---
        var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == "10A1");
        if (cls == null)
        {
            cls = new Class { Name = "10A1", SemesterId = semester.Id };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
        }

        // --- Ghi danh: mọi học sinh CHƯA có lớp → vào lớp 10A1 ---
        // (đảm bảo tài khoản HS test bạn tự tạo cũng có TKB để xem giao diện)
        var classlessStudents = await db.Users
            .Where(u => u.Role == UserRole.Student
                && !db.ClassStudents.Any(cs => cs.StudentId == u.Id))
            .ToListAsync();
        foreach (var s in classlessStudents)
        {
            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = s.Id });
        }
        await db.SaveChangesAsync();

        // --- Phân công GV dạy các môn cho lớp (nếu chưa có) ---
        var subjects = new[] { math, literature, english, physics, chemistry };
        foreach (var subj in subjects)
        {
            var assigned = await db.TeacherAssignments.AnyAsync(ta =>
                ta.TeacherId == teacher.Id && ta.ClassId == cls.Id && ta.SubjectId == subj.Id);
            if (!assigned)
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

        // --- Thời khóa biểu tuần (chỉ seed khi lớp chưa có tiết nào) ---
        if (!await db.TimetableSlots.AnyAsync(t => t.ClassId == cls.Id))
        {
            // (thứ, tiết, môn, phòng) — 1 = Thứ Hai … 6 = Thứ Bảy
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

        // --- Điểm đã công bố cho từng học sinh trong lớp (nếu lớp chưa có điểm) ---
        if (!await db.Grades.AnyAsync(g => g.ClassId == cls.Id))
        {
            var studentIds = await db.ClassStudents
                .Where(cs => cs.ClassId == cls.Id)
                .Select(cs => cs.StudentId)
                .ToListAsync();

            // (môn, loại đầu điểm, điểm) — dùng chung cho các HS cho gọn.
            var gradePlan = new (Subject Subject, string Type, double Score)[]
            {
                (math, "Oral", 8.5),
                (math, "Midterm", 7.5),
                (literature, "Midterm", 8.0),
                (english, "Oral", 9.0),
                (physics, "Quiz15", 7.0),
            };
            foreach (var sid in studentIds)
            {
                foreach (var gp in gradePlan)
                {
                    db.Grades.Add(new Grade
                    {
                        StudentId = sid,
                        ClassId = cls.Id,
                        SubjectId = gp.Subject.Id,
                        SemesterId = semester.Id,
                        AssessmentType = gp.Type,
                        Score = gp.Score,
                        Status = GradeStatus.Published, // HS/PH mới xem được
                        CreatedByTeacherId = teacher.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        // --- Bài tập mẫu cho lớp (nếu lớp chưa có bài tập) ---
        if (!await db.Assignments.AnyAsync(a => a.ClassId == cls.Id))
        {
            db.Assignments.AddRange(
                new Assignment
                {
                    Title = "Bài tập Toán - Chương 1",
                    Description = "Làm các bài 1 đến 10 trang 25 SGK.",
                    DueDate = DateTime.UtcNow.AddDays(-2), // đã quá hạn → Overdue
                    MaxScore = 10,
                    ClassId = cls.Id,
                    SubjectId = math.Id,
                    CreatedByTeacherId = teacher.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                },
                new Assignment
                {
                    Title = "Luyện tập Ngữ Văn - Bài thơ",
                    Description = "Viết đoạn văn cảm nhận về bài thơ đã học (khoảng 200 từ).",
                    DueDate = DateTime.UtcNow.AddDays(3), // còn hạn → ToDo
                    MaxScore = 10,
                    ClassId = cls.Id,
                    SubjectId = literature.Id,
                    CreatedByTeacherId = teacher.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }
            );
            await db.SaveChangesAsync();
        }

        // --- Học phí: loại khoản thu + hóa đơn cho từng HS (nếu chưa có) ---
        var tuition = await EnsureFeeCategoryAsync(db, "Học phí Học kỳ 1", 1500000);
        var insurance = await EnsureFeeCategoryAsync(db, "Bảo hiểm y tế", 800000);
        await db.SaveChangesAsync();

        if (!await db.FeeInvoices.AnyAsync())
        {
            var studentIds = await db.ClassStudents
                .Where(cs => cs.ClassId == cls.Id)
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
}
