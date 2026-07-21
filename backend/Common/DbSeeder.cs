using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Api.Common;

/// <summary>
/// Seed dữ liệu demo sạch cho Development — THPT (cấp 3), mỗi năm 2 học kỳ.
/// Luồng: <see cref="WipeAllAsync"/> → seed đủ bảng để demo mọi vai trò.
/// Mật khẩu chung mọi tài khoản mẫu: <c>123456</c>.
/// </summary>
public static class DbSeeder
{
    private const string DemoPassword = "123456";

    /// <summary>
    /// Development: wipe + seed khi <c>Seed:ForceReset=true</c> (mặc định true lần đầu demo).
    /// Đặt <c>false</c> sau khi đã có data sạch để tránh mất dữ liệu mỗi lần restart API.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, IConfiguration? config = null)
    {
        var forceReset = config?.GetValue("Seed:ForceReset", true) ?? true;
        if (forceReset)
        {
            await WipeAllAsync(db);
            await SeedFreshAsync(db, config);
            Console.WriteLine("[DbSeeder] Đã XÓA SẠCH và seed dữ liệu demo THPT (2 kỳ/năm).");
            Console.WriteLine("[DbSeeder] Tài khoản: 0900000000..0008 / MK: 123456");
            Console.WriteLine("[DbSeeder] GV A (CN+Toán+Anh) 0900000002 · GV B (Lý) 0900000006 · GV C (Hóa+Văn) 0900000008");
            Console.WriteLine("[DbSeeder] Email demo: admin@fschool.demo... — muốn nhận OTP Gmail thật thì sửa email hồ sơ thành Gmail của bạn.");
            Console.WriteLine("[DbSeeder] Tip: đặt Seed:ForceReset=false trong appsettings.Development.json để giữ data sau restart.");
        }
        else
        {
            // Không wipe — chỉ seed PayOS config nếu thiếu (không đụng data nghiệp vụ).
            if (!await db.Users.AnyAsync())
            {
                await SeedFreshAsync(db, config);
                Console.WriteLine("[DbSeeder] DB trống → đã seed demo lần đầu.");
            }
            else
            {
                await SeedPayOsConfigAsync(db, config);
                Console.WriteLine("[DbSeeder] ForceReset=false — giữ nguyên dữ liệu hiện có.");
            }
        }
    }

    /// <summary>
    /// Xóa dữ liệu mọi bảng theo thứ tự an toàn FK (con trước, cha sau).
    /// </summary>
    public static async Task WipeAllAsync(AppDbContext db)
    {
        await db.PaymentTransactions.ExecuteDeleteAsync();
        await db.FeeInvoices.ExecuteDeleteAsync();
        await db.FeeCategories.ExecuteDeleteAsync();
        // PaymentGatewayConfigs: giữ lại khi ForceReset (khôi phục sau seed).
        await db.Notifications.ExecuteDeleteAsync();
        await db.UserDevices.ExecuteDeleteAsync();
        await db.Announcements.ExecuteDeleteAsync();
        await db.LeaveRequests.ExecuteDeleteAsync();
        await db.Submissions.ExecuteDeleteAsync();
        await db.Assignments.ExecuteDeleteAsync();
        await db.Grades.ExecuteDeleteAsync();
        await db.Attendances.ExecuteDeleteAsync();
        await db.TimetableSlots.ExecuteDeleteAsync();
        await db.TeacherAssignments.ExecuteDeleteAsync();
        await db.ClassStudents.ExecuteDeleteAsync();
        await db.Classes.ExecuteDeleteAsync();
        await db.Subjects.ExecuteDeleteAsync();
        await db.Semesters.ExecuteDeleteAsync();
        await db.StudentParents.ExecuteDeleteAsync();
        await db.PasswordResetOtps.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    /// <summary>Seed bộ demo đầy đủ sau khi DB trống.</summary>
    private static async Task SeedFreshAsync(AppDbContext db, IConfiguration? config)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        // ─── 1) Users ──────────────────────────────────────────────────────
        // Email dùng cho quên MK (FR1.2) — Dev OTP log [DEV OTP EMAIL].
        var admin = NewUser("0900000000", "Admin Trường", UserRole.Admin, hash, "admin@fschool.demo");
        var teacher = NewUser("0900000002", "Nguyễn Văn A (GV)", UserRole.Teacher, hash, "gv.a@fschool.demo");
        var teacher2 = NewUser("0900000006", "Trần Thị B (GV)", UserRole.Teacher, hash, "gv.b@fschool.demo");
        var teacher3 = NewUser("0900000008", "Lê Văn C (GV)", UserRole.Teacher, hash, "gv.c@fschool.demo");
        var parent = NewUser("0900000003", "Lê Minh Parent", UserRole.Parent, hash, "parent@fschool.demo");
        var studentAn = NewUser("0900000004", "Nguyễn Văn An", UserRole.Student, hash, "an@fschool.demo");
        var studentBinh = NewUser("0900000005", "Nguyễn Thị Bình", UserRole.Student, hash, "binh@fschool.demo");
        var studentCuong = NewUser("0900000007", "Phạm Văn Cường", UserRole.Student, hash, "cuong@fschool.demo");

        db.Users.AddRange(admin, teacher, teacher2, teacher3, parent, studentAn, studentBinh, studentCuong);
        await db.SaveChangesAsync();

        // Parent Switch: 1 PH → 2 con (An + Bình); Cường chỉ để roster lớp đông hơn.
        db.StudentParents.AddRange(
            new StudentParent { ParentId = parent.Id, StudentId = studentAn.Id },
            new StudentParent { ParentId = parent.Id, StudentId = studentBinh.Id });
        await db.SaveChangesAsync();

        // ─── 2) Semesters (THPT: 2 kỳ/năm) ─────────────────────────────────
        // Hôm nay = 16/07/2026 (nghỉ hè lịch chuẩn) → kéo EndDate HK2 2025-2026
        // đến 31/07/2026 để demo vẫn có "kỳ đang diễn ra".
        var hk1_2526 = new Semester
        {
            Name = "Học kỳ 1 (2025-2026)",
            StartDate = new DateTime(2025, 9, 5),
            EndDate = new DateTime(2026, 1, 15),
        };
        var hk2_2526 = new Semester
        {
            Name = "Học kỳ 2 (2025-2026)",
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 7, 31), // demo: bao phủ ngày hiện tại
        };
        var hk1_2627 = new Semester
        {
            Name = "Học kỳ 1 (2026-2027)",
            StartDate = new DateTime(2026, 9, 5),
            EndDate = new DateTime(2027, 1, 15),
        };
        var hk2_2627 = new Semester
        {
            Name = "Học kỳ 2 (2026-2027)",
            StartDate = new DateTime(2027, 2, 1),
            EndDate = new DateTime(2027, 5, 25),
        };
        db.Semesters.AddRange(hk1_2526, hk2_2526, hk1_2627, hk2_2627);
        await db.SaveChangesAsync();

        // ─── 3) Subjects ───────────────────────────────────────────────────
        var math = new Subject { Name = "Toán", Code = "MATH" };
        var lit = new Subject { Name = "Ngữ Văn", Code = "LIT" };
        var eng = new Subject { Name = "Tiếng Anh", Code = "ENG" };
        var phy = new Subject { Name = "Vật Lý", Code = "PHY" };
        var che = new Subject { Name = "Hóa Học", Code = "CHE" };
        db.Subjects.AddRange(math, lit, eng, phy, che);
        await db.SaveChangesAsync();

        var subjects = new[] { math, lit, eng, phy, che };
        var students = new[] { studentAn, studentBinh, studentCuong };

        // ─── 4) Classes + enroll + phân công + TKB (mỗi kỳ một lớp 10A1) ──
        var classBySem = new Dictionary<int, Class>();
        foreach (var sem in new[] { hk1_2526, hk2_2526, hk1_2627, hk2_2627 })
        {
            // GV A = chủ nhiệm 10A1 + dạy Toán, Anh.
            // GV B = bộ môn Vật Lý. GV C = bộ môn Hóa + Ngữ Văn.
            var cls = new Class
            {
                Name = "10A1",
                SemesterId = sem.Id,
                HomeroomTeacherId = teacher.Id,
            };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();
            classBySem[sem.Id] = cls;

            // Chỉ gán HS vào lớp kỳ đang học — tránh 4 bản ghi ClassStudents/HS (lỗi tạo đơn nghỉ).
            if (sem.Id == hk2_2526.Id)
            {
                foreach (var st in students)
                    db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = st.Id });
            }

            db.TeacherAssignments.AddRange(
                new TeacherAssignment { TeacherId = teacher.Id, ClassId = cls.Id, SubjectId = math.Id },
                new TeacherAssignment { TeacherId = teacher.Id, ClassId = cls.Id, SubjectId = eng.Id },
                new TeacherAssignment { TeacherId = teacher2.Id, ClassId = cls.Id, SubjectId = phy.Id },
                new TeacherAssignment { TeacherId = teacher3.Id, ClassId = cls.Id, SubjectId = che.Id },
                new TeacherAssignment { TeacherId = teacher3.Id, ClassId = cls.Id, SubjectId = lit.Id });
            await db.SaveChangesAsync();

            AddTimetable(db, cls, teacher, teacher2, teacher3, math, lit, eng, phy, che);
            await db.SaveChangesAsync();
        }

        var currentClass = classBySem[hk2_2526.Id]; // kỳ đang diễn ra
        var pastClass = classBySem[hk1_2526.Id];

        // ─── 5) Grades (Published) — HK1 lịch sử + HK2 hiện tại ────────────
        AddThptGrades(db, pastClass, hk1_2526, teacher, students, math, lit, eng, phy);
        AddThptGrades(db, currentClass, hk2_2526, teacher, students, math, lit, eng, phy);
        await db.SaveChangesAsync();

        // ─── 6) Attendance — vài ngày gần đây (P/A/L) ──────────────────────
        var today = DateTime.UtcNow.Date;
        for (var i = 0; i < 5; i++)
        {
            var day = today.AddDays(-i);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

            db.Attendances.Add(new Attendance
            {
                ClassId = currentClass.Id,
                StudentId = studentAn.Id,
                Date = day,
                Status = i == 1 ? AttendanceStatus.Late : AttendanceStatus.Present,
                RecordedByTeacherId = teacher.Id,
                CreatedAt = day,
            });
            db.Attendances.Add(new Attendance
            {
                ClassId = currentClass.Id,
                StudentId = studentBinh.Id,
                Date = day,
                Status = i == 2 ? AttendanceStatus.Absent : AttendanceStatus.Present,
                RecordedByTeacherId = teacher.Id,
                CreatedAt = day,
            });
            db.Attendances.Add(new Attendance
            {
                ClassId = currentClass.Id,
                StudentId = studentCuong.Id,
                Date = day,
                Status = AttendanceStatus.Present,
                RecordedByTeacherId = teacher.Id,
                CreatedAt = day,
            });
        }
        await db.SaveChangesAsync();

        // ─── 7) Assignments + Submissions (giữ DB; UI app đã bỏ) ───────────
        var asgMath = new Assignment
        {
            Title = "Bài tập Toán — Hàm số",
            Description = "Làm bài 1–10 trang 45 SGK.",
            DueDate = today.AddDays(5),
            MaxScore = 10,
            ClassId = currentClass.Id,
            SubjectId = math.Id,
            CreatedByTeacherId = teacher.Id,
            CreatedAt = today.AddDays(-3),
        };
        var asgLit = new Assignment
        {
            Title = "Ngữ Văn — Cảm nhận thơ",
            Description = "Viết đoạn văn ~200 từ về bài thơ đã học.",
            DueDate = today.AddDays(-1), // quá hạn — demo Overdue
            MaxScore = 10,
            ClassId = currentClass.Id,
            SubjectId = lit.Id,
            CreatedByTeacherId = teacher.Id,
            CreatedAt = today.AddDays(-7),
        };
        db.Assignments.AddRange(asgMath, asgLit);
        await db.SaveChangesAsync();

        db.Submissions.Add(new Submission
        {
            AssignmentId = asgMath.Id,
            StudentId = studentAn.Id,
            LinkUrl = "https://drive.google.com/demo-an-toan",
            SubmittedAt = today.AddDays(-1),
            Score = 8.5,
            Feedback = "Làm tốt, trình bày rõ.",
            GradedByTeacherId = teacher.Id,
            GradedAt = today,
        });
        await db.SaveChangesAsync();

        // ─── 8) Leave requests ─────────────────────────────────────────────
        db.LeaveRequests.AddRange(
            new LeaveRequest
            {
                ClassId = currentClass.Id,
                StudentId = studentAn.Id,
                SubmittedByUserId = parent.Id, // PH nộp hộ — demo Switch Profile
                Date = today.AddDays(2),
                Reason = "Con sốt, khám bệnh tại phòng khám.",
                Status = LeaveRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
            },
            new LeaveRequest
            {
                ClassId = currentClass.Id,
                StudentId = studentBinh.Id,
                SubmittedByUserId = studentBinh.Id,
                Date = today.AddDays(-3),
                Reason = "Việc gia đình.",
                Status = LeaveRequestStatus.Approved,
                ApprovedByTeacherId = teacher.Id,
                ReviewedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-4),
            },
            new LeaveRequest
            {
                ClassId = currentClass.Id,
                StudentId = studentCuong.Id,
                SubmittedByUserId = studentCuong.Id,
                Date = today.AddDays(-10),
                Reason = "Đau bụng.",
                Status = LeaveRequestStatus.Rejected,
                ApprovedByTeacherId = teacher.Id,
                RejectionReason = "Thiếu giấy xác nhận y tế.",
                ReviewedAt = DateTime.UtcNow.AddDays(-9),
                CreatedAt = DateTime.UtcNow.AddDays(-11),
            });
        await db.SaveChangesAsync();

        // ─── 9) Announcements ──────────────────────────────────────────────
        // Global → Bảng tin nhà trường (mọi role).
        // Class → chỉ lịch sử Đã gửi của GV; HS/PH nhận qua chuông (không lên Bảng tin).
        // Teachers/Teacher → chỉ chuông Đã nhận của GV.
        db.Announcements.AddRange(
            new Announcement
            {
                Title = "Chào mừng năm học 2025-2026",
                Content = "Nhà trường kính chúc quý phụ huynh và học sinh một năm học thành công.",
                Type = AnnouncementType.Global,
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
            },
            new Announcement
            {
                Title = "Họp phụ huynh cuối kỳ 2",
                Content = "Chủ nhật tuần sau, 8h00 tại hội trường A.",
                Type = AnnouncementType.Global,
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            },
            new Announcement
            {
                Title = "Nhắc kiểm tra Toán",
                Content = "Tuần sau kiểm tra 1 tiết chương Hàm số. Ôn kỹ SGK.",
                Type = AnnouncementType.Class,
                TargetClassId = currentClass.Id,
                SubjectId = math.Id,
                CreatedById = teacher.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new Announcement
            {
                Title = "Họp chuyên môn cuối tuần",
                Content = "Thứ Bảy 9h00 họp tổ chuyên môn tại phòng họp B. Vui lòng có mặt đúng giờ.",
                Type = AnnouncementType.Teachers,
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
            },
            new Announcement
            {
                Title = "Nhắc nộp điểm giữa kỳ",
                Content = "Vui lòng hoàn tất nhập điểm giữa kỳ trước thứ Sáu tuần này.",
                Type = AnnouncementType.Teacher,
                TargetUserId = teacher.Id,
                CreatedById = admin.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
            });
        await db.SaveChangesAsync();

        // ─── 10) Notifications (in-app) ────────────────────────────────────
        db.Notifications.AddRange(
            new Notification
            {
                UserId = parent.Id,
                Title = "Điểm mới",
                Message = "Con Nguyễn Văn An vừa có điểm kiểm tra môn Toán.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-3),
            },
            new Notification
            {
                UserId = parent.Id,
                Title = "Đơn nghỉ đã duyệt",
                Message = "Đơn xin nghỉ của Nguyễn Thị Bình đã được duyệt.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            },
            new Notification
            {
                UserId = parent.Id,
                Title = "Nhắc học phí",
                Message = "Học phí học kỳ 2 sắp đến hạn thanh toán.",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            // Tin GV gửi lớp → chuông PH/HS (không hiện Bảng tin).
            new Notification
            {
                UserId = parent.Id,
                Title = "[Toán] Nhắc kiểm tra Toán",
                Message = "Tuần sau kiểm tra 1 tiết chương Hàm số. Ôn kỹ SGK.\n\n— GV Nguyễn Văn A (GV) · 10A1 · Toán",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new Notification
            {
                UserId = studentAn.Id,
                Title = "[Toán] Nhắc kiểm tra Toán",
                Message = "Tuần sau kiểm tra 1 tiết chương Hàm số. Ôn kỹ SGK.\n\n— GV Nguyễn Văn A (GV) · 10A1 · Toán",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new Notification
            {
                UserId = studentBinh.Id,
                Title = "[Toán] Nhắc kiểm tra Toán",
                Message = "Tuần sau kiểm tra 1 tiết chương Hàm số. Ôn kỹ SGK.\n\n— GV Nguyễn Văn A (GV) · 10A1 · Toán",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new Notification
            {
                UserId = studentCuong.Id,
                Title = "[Toán] Nhắc kiểm tra Toán",
                Message = "Tuần sau kiểm tra 1 tiết chương Hàm số. Ôn kỹ SGK.\n\n— GV Nguyễn Văn A (GV) · 10A1 · Toán",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new Notification
            {
                UserId = teacher.Id,
                Title = "Đơn nghỉ mới",
                Message = "Có đơn xin nghỉ mới cần duyệt (Nguyễn Văn An).",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-4),
            },
            new Notification
            {
                UserId = teacher.Id,
                Title = "[Admin → Giáo viên] Họp chuyên môn cuối tuần",
                Message = "Thứ Bảy 9h00 họp tổ chuyên môn tại phòng họp B. Vui lòng có mặt đúng giờ.\n\n— Admin Trường · Gửi toàn bộ giáo viên",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
            },
            new Notification
            {
                UserId = teacher2.Id,
                Title = "[Admin → Giáo viên] Họp chuyên môn cuối tuần",
                Message = "Thứ Bảy 9h00 họp tổ chuyên môn tại phòng họp B. Vui lòng có mặt đúng giờ.\n\n— Admin Trường · Gửi toàn bộ giáo viên",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
            },
            new Notification
            {
                UserId = teacher3.Id,
                Title = "[Admin → Giáo viên] Họp chuyên môn cuối tuần",
                Message = "Thứ Bảy 9h00 họp tổ chuyên môn tại phòng họp B. Vui lòng có mặt đúng giờ.\n\n— Admin Trường · Gửi toàn bộ giáo viên",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-6),
            },
            new Notification
            {
                UserId = teacher.Id,
                Title = "[Admin] Nhắc nộp điểm giữa kỳ",
                Message = "Vui lòng hoàn tất nhập điểm giữa kỳ trước thứ Sáu tuần này.\n\n— Admin Trường · Tới Nguyễn Văn A (GV)",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
            });
        await db.SaveChangesAsync();

        // ─── 11) Fees + 1 giao dịch đã thanh toán mẫu ──────────────────────
        var tuition = new FeeCategory
        {
            Name = "Học phí Học kỳ 2",
            Description = "Học phí HK2 năm học 2025-2026",
            DefaultAmount = 1_500_000m,
            IsActive = true,
        };
        var insurance = new FeeCategory
        {
            Name = "Bảo hiểm y tế",
            Description = "BHYT học sinh",
            DefaultAmount = 800_000m,
            IsActive = true,
        };
        db.FeeCategories.AddRange(tuition, insurance);
        await db.SaveChangesAsync();

        // An: 1 hóa đơn Pending (học phí) + 1 Paid (BHYT) — demo thanh toán / biên lai
        var invAnTuition = new FeeInvoice
        {
            StudentId = studentAn.Id,
            FeeCategoryId = tuition.Id,
            Amount = tuition.DefaultAmount,
            DueDate = today.AddDays(14),
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = "Học phí HK2 — Nguyễn Văn An",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        var invAnBhyt = new FeeInvoice
        {
            StudentId = studentAn.Id,
            FeeCategoryId = insurance.Id,
            Amount = insurance.DefaultAmount,
            DueDate = today.AddDays(-7),
            Status = FeePaymentStatus.Paid,
            IsPaid = true,
            PaidAt = DateTime.UtcNow.AddDays(-3),
            PaymentMethod = "PayOS",
            TransactionId = "DEMO-PAID-AN-BHYT",
            ReceiptNumber = "BL-2026-0001",
            Note = "BHYT — đã thanh toán",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
        };
        // Bình: 2 hóa đơn Pending — PH switch sang con Bình vẫn thấy
        var invBinhTuition = new FeeInvoice
        {
            StudentId = studentBinh.Id,
            FeeCategoryId = tuition.Id,
            Amount = tuition.DefaultAmount,
            DueDate = today.AddDays(14),
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = "Học phí HK2 — Nguyễn Thị Bình",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        var invBinhBhyt = new FeeInvoice
        {
            StudentId = studentBinh.Id,
            FeeCategoryId = insurance.Id,
            Amount = insurance.DefaultAmount,
            DueDate = today.AddDays(7),
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = "BHYT — Nguyễn Thị Bình",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        // Cường: 1 Pending
        var invCuong = new FeeInvoice
        {
            StudentId = studentCuong.Id,
            FeeCategoryId = tuition.Id,
            Amount = tuition.DefaultAmount,
            DueDate = today.AddDays(14),
            Status = FeePaymentStatus.Pending,
            IsPaid = false,
            Note = "Học phí HK2 — Phạm Văn Cường",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };
        db.FeeInvoices.AddRange(invAnTuition, invAnBhyt, invBinhTuition, invBinhBhyt, invCuong);
        await db.SaveChangesAsync();

        db.PaymentTransactions.Add(new PaymentTransaction
        {
            FeeInvoiceId = invAnBhyt.Id,
            Provider = "PayOS",
            OrderCode = "DEMO-ORDER-AN-BHYT",
            ProviderTransactionId = "DEMO-PAID-AN-BHYT",
            Amount = invAnBhyt.Amount,
            Status = FeePaymentStatus.Paid,
            PaymentUrl = "https://pay.payos.vn/demo",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
        });
        await db.SaveChangesAsync();

        // ─── 12) Cổng PayOS (từ appsettings nếu có) ────────────────────────
        await SeedPayOsConfigAsync(db, config);
    }

    /// <summary>Tạo user demo mới (DB đã wipe — không cần check trùng).</summary>
    private static User NewUser(
        string phone, string fullName, UserRole role, string passwordHash, string? email = null)
        => new()
        {
            Phone = phone,
            Username = phone,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsPhoneVerified = true,
            IsLocked = false,
            AvatarUrl = string.Empty,
        };

    /// <summary>
    /// TKB tuần mẫu (T2–T6, 3 tiết/ngày).
    /// Toán/Anh → GV A; Lý → GV B; Hóa/Văn → GV C.
    /// </summary>
    private static void AddTimetable(
        AppDbContext db, Class cls,
        User teacherA, User teacherB, User teacherC,
        Subject math, Subject lit, Subject eng, Subject phy, Subject che)
    {
        var plan = new (int Day, int Period, Subject Subject, string Room)[]
        {
            (1, 1, math, "A101"), (1, 2, lit, "A101"), (1, 3, eng, "A101"),
            (2, 1, phy, "Lab-1"), (2, 2, math, "A101"), (2, 3, che, "Lab-2"),
            (3, 1, lit, "A101"), (3, 2, eng, "A101"), (3, 3, math, "A101"),
            (4, 1, math, "A101"), (4, 2, phy, "Lab-1"), (4, 3, lit, "A101"),
            (5, 1, eng, "A101"), (5, 2, che, "Lab-2"), (5, 3, math, "A101"),
        };
        foreach (var p in plan)
        {
            int teacherId;
            if (p.Subject.Id == math.Id || p.Subject.Id == eng.Id)
                teacherId = teacherA.Id;
            else if (p.Subject.Id == phy.Id)
                teacherId = teacherB.Id;
            else
                teacherId = teacherC.Id; // lit + che

            db.TimetableSlots.Add(new TimetableSlot
            {
                ClassId = cls.Id,
                SubjectId = p.Subject.Id,
                TeacherId = teacherId,
                DayOfWeek = p.Day,
                Period = p.Period,
                Room = p.Room,
            });
        }
    }

    /// <summary>
    /// Điểm form sổ THPT đã Publish: 3 miệng · 3×15p · 2×1 tiết · GK · CK.
    /// Vật Lý điểm thấp → demo badge chưa đạt.
    /// </summary>
    private static void AddThptGrades(
        AppDbContext db, Class cls, Semester semester, User teacher,
        User[] students, Subject math, Subject lit, Subject eng, Subject phy)
    {
        // Điểm mẫu đủ cột sổ điểm (mã = ThptGradeCatalog).
        var baseBySubject = new Dictionary<int, double[]>
        {
            // Oral1-3, Quiz15_1-3, OnePeriod1-2, Midterm, Final
            [math.Id] = new[] { 8.5, 8.0, 9.0, 7.5, 8.0, 7.0, 8.0, 8.5, 7.5, 8.0 },
            [lit.Id] = new[] { 8.0, 7.5, 8.5, 7.0, 7.5, 8.0, 7.5, 8.0, 8.0, 7.5 },
            [eng.Id] = new[] { 9.0, 8.5, 9.0, 8.5, 8.0, 9.0, 8.0, 8.5, 8.5, 9.0 },
            [phy.Id] = new[] { 5.0, 4.5, 5.5, 4.5, 4.0, 5.0, 4.0, 4.5, 4.0, 3.0 },
        };

        var codes = ThptGradeCatalog.Columns.Select(c => c.Code).ToArray();

        foreach (var st in students)
        {
            var bias = st.FullName.Contains("Bình") ? -0.5
                : st.FullName.Contains("Cường") ? 0.3
                : 0.0;

            foreach (var (subjectId, scores) in baseBySubject)
            {
                for (var i = 0; i < codes.Length; i++)
                {
                    var s = Math.Clamp(scores[i] + bias, 0, 10);
                    db.Grades.Add(new Grade
                    {
                        StudentId = st.Id,
                        ClassId = cls.Id,
                        SubjectId = subjectId,
                        SemesterId = semester.Id,
                        AssessmentType = codes[i],
                        Score = Math.Round(s, 1),
                        Status = GradeStatus.Published,
                        CreatedByTeacherId = teacher.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                    });
                }
            }
        }
    }

    /// <summary>Nạp/cập nhật cấu hình PayOS từ appsettings (nếu đủ khóa).</summary>
    private static async Task SeedPayOsConfigAsync(AppDbContext db, IConfiguration? config)
    {
        if (config == null) return;

        var clientId = config["PayOS:ClientId"];
        var apiKey = config["PayOS:ApiKey"];
        var checksumKey = config["PayOS:ChecksumKey"];
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
            existing.IsEnabled = true;
            existing.ConfigJson = configJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }
}