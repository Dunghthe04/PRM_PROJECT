using Microsoft.EntityFrameworkCore;

namespace Api.Models;

/// <summary>
/// DbContext chính của FSchool — đăng ký entity và cấu hình quan hệ / index / precision.
/// <para>
/// Sơ đồ quan hệ chính:
/// User ↔ StudentParent (N–N PH–HS) · Class ↔ ClassStudent (N–N lớp–HS) ·
/// TeacherAssignment (GV–Lớp–Môn) · TimetableSlot · Attendance · Grade ·
/// LeaveRequest · Announcement / Notification · FeeInvoice → PaymentTransaction.
/// </para>
/// Hầu hết FK dùng <c>DeleteBehavior.Restrict</c> để tránh xóa dây chuyền ngoài ý muốn;
/// một số quan hệ phụ (Notification, UserDevice, OTP, Submission→Assignment) dùng Cascade.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Khởi tạo context với options (connection string từ DI).</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Tài khoản người dùng (Admin / Teacher / Parent / Student).</summary>
    public DbSet<User> Users { get; set; }

    /// <summary>Liên kết N–N Phụ huynh ↔ Học sinh (Switch Profile).</summary>
    public DbSet<StudentParent> StudentParents { get; set; }

    /// <summary>OTP xác thực SĐT / quên mật khẩu.</summary>
    public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }

    /// <summary>Danh mục học kỳ.</summary>
    public DbSet<Semester> Semesters { get; set; }

    /// <summary>Danh mục môn học.</summary>
    public DbSet<Subject> Subjects { get; set; }

    /// <summary>Lớp học theo kỳ.</summary>
    public DbSet<Class> Classes { get; set; }

    /// <summary>Gán học sinh vào lớp.</summary>
    public DbSet<ClassStudent> ClassStudents { get; set; }

    /// <summary>Phân công GV dạy lớp + môn.</summary>
    public DbSet<TeacherAssignment> TeacherAssignments { get; set; }

    /// <summary>Thời khóa biểu theo tiết / tuần.</summary>
    public DbSet<TimetableSlot> TimetableSlots { get; set; }

    /// <summary>Điểm danh P/A/L.</summary>
    public DbSet<Attendance> Attendances { get; set; }

    /// <summary>Bài tập (giữ DB; UI app đã bỏ).</summary>
    public DbSet<Assignment> Assignments { get; set; }

    /// <summary>Bài nộp gắn Assignment.</summary>
    public DbSet<Submission> Submissions { get; set; }

    /// <summary>Điểm số (Nháp → Publish).</summary>
    public DbSet<Grade> Grades { get; set; }

    /// <summary>Đơn xin nghỉ học.</summary>
    public DbSet<LeaveRequest> LeaveRequests { get; set; }

    /// <summary>Bảng tin toàn trường / theo lớp.</summary>
    public DbSet<Announcement> Announcements { get; set; }

    /// <summary>Thông báo in-app cá nhân.</summary>
    public DbSet<Notification> Notifications { get; set; }

    /// <summary>FCM device token.</summary>
    public DbSet<UserDevice> UserDevices { get; set; }

    /// <summary>Loại khoản thu.</summary>
    public DbSet<FeeCategory> FeeCategories { get; set; }

    /// <summary>Hóa đơn học phí theo học sinh.</summary>
    public DbSet<FeeInvoice> FeeInvoices { get; set; }

    /// <summary>Giao dịch cổng thanh toán.</summary>
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    /// <summary>Cấu hình VNPay / PayOS.</summary>
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs { get; set; }

    /// <summary>
    /// Cấu hình Fluent API: khóa kép, FK, index unique, precision tiền tệ.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── StudentParent: N–N Phụ huynh ↔ Học sinh ─────────────────────
        modelBuilder.Entity<StudentParent>()
            .HasKey(sp => new { sp.StudentId, sp.ParentId });

        // Phía Học sinh: ParentLinks = danh sách PH gắn với HS
        modelBuilder.Entity<StudentParent>()
            .HasOne(sp => sp.Student)
            .WithMany(u => u.ParentLinks)
            .HasForeignKey(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phía Phụ huynh: ChildLinks = danh sách con
        modelBuilder.Entity<StudentParent>()
            .HasOne(sp => sp.Parent)
            .WithMany(u => u.ChildLinks)
            .HasForeignKey(sp => sp.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── ClassStudent: N–N Lớp ↔ Học sinh ─────────────────────────────
        modelBuilder.Entity<ClassStudent>()
            .HasKey(cs => new { cs.ClassId, cs.StudentId });

        modelBuilder.Entity<ClassStudent>()
            .HasOne(cs => cs.Class)
            .WithMany(c => c.ClassStudents)
            .HasForeignKey(cs => cs.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClassStudent>()
            .HasOne(cs => cs.Student)
            .WithMany()
            .HasForeignKey(cs => cs.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tra cứu lớp theo học sinh (TKB / điểm) — PK đang là (ClassId, StudentId)
        modelBuilder.Entity<ClassStudent>()
            .HasIndex(cs => cs.StudentId);

        // GV chủ nhiệm lớp — Restrict để không xóa User kéo theo lớp.
        modelBuilder.Entity<Class>()
            .HasOne(c => c.HomeroomTeacher)
            .WithMany()
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Class>()
            .HasIndex(c => c.HomeroomTeacherId);

        // ─── TeacherAssignment: GV – Lớp – Môn (GV bộ môn) ─────────────────
        modelBuilder.Entity<TeacherAssignment>()
            .HasOne(ta => ta.Teacher)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Assignment / Submission ───────────────────────────────────────
        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.CreatedByTeacher)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Class)
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1 HS chỉ 1 bài nộp / 1 assignment — nộp lại = Update
        modelBuilder.Entity<Submission>()
            .HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique();

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.GradedByTeacher)
            .WithMany()
            .HasForeignKey(s => s.GradedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Grade ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Class)
            .WithMany()
            .HasForeignKey(g => g.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.CreatedByTeacher)
            .WithMany()
            .HasForeignKey(g => g.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.ApprovedBy)
            .WithMany()
            .HasForeignKey(g => g.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Mỗi HS chỉ có 1 điểm / đầu điểm / môn / kỳ / lớp
        modelBuilder.Entity<Grade>()
            .HasIndex(g => new { g.StudentId, g.ClassId, g.SubjectId, g.SemesterId, g.AssessmentType })
            .IsUnique();

        // ─── Attendance ────────────────────────────────────────────────────
        // Mỗi HS chỉ 1 bản ghi / lớp / ngày
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.ClassId, a.StudentId, a.Date })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Class)
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.RecordedByTeacher)
            .WithMany()
            .HasForeignKey(a => a.RecordedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Di sản offline sync — unique khi ClientRecordId có giá trị
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.ClientRecordId)
            .IsUnique()
            .HasFilter("[ClientRecordId] IS NOT NULL");

        // ─── LeaveRequest ──────────────────────────────────────────────────
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Class)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.SubmittedBy)
            .WithMany()
            .HasForeignKey(lr => lr.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.ApprovedByTeacher)
            .WithMany()
            .HasForeignKey(lr => lr.ApprovedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Announcement / Notification / UserDevice ──────────────────────
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedBy)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.TargetClass)
            .WithMany()
            .HasForeignKey(a => a.TargetClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Admin gửi cho 1 GV — Restrict để không xóa user kéo theo lịch sử TB.
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.TargetUser)
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        modelBuilder.Entity<UserDevice>()
            .HasIndex(d => d.FcmToken)
            .IsUnique();

        modelBuilder.Entity<UserDevice>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─── User: unique Phone / Email ────────────────────────────────────
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Phone)
            .IsRequired();

        // ─── OTP ───────────────────────────────────────────────────────────
        modelBuilder.Entity<PasswordResetOtp>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetOtp>()
            .HasIndex(o => o.ResetToken);

        modelBuilder.Entity<PasswordResetOtp>()
            .HasIndex(o => new { o.UserId, o.Purpose, o.IsUsed });

        // ─── TimetableSlot: 1 lớp không trùng tiết trong cùng thứ ──────────
        modelBuilder.Entity<TimetableSlot>()
            .HasIndex(t => new { t.ClassId, t.DayOfWeek, t.Period })
            .IsUnique();

        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Class)
            .WithMany()
            .HasForeignKey(t => t.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Subject)
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Teacher)
            .WithMany()
            .HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // ─── Học phí: decimal(18,2) ────────────────────────────────────────
        modelBuilder.Entity<FeeCategory>()
            .Property(f => f.DefaultAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<FeeInvoice>()
            .Property(f => f.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<FeeInvoice>()
            .HasOne(f => f.Student)
            .WithMany()
            .HasForeignKey(f => f.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FeeInvoice>()
            .HasOne(f => f.FeeCategory)
            .WithMany()
            .HasForeignKey(f => f.FeeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FeeInvoice>()
            .HasIndex(f => f.TransactionId);

        modelBuilder.Entity<FeeInvoice>()
            .HasIndex(f => f.ReceiptNumber);

        modelBuilder.Entity<PaymentTransaction>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(p => p.OrderCode)
            .IsUnique();

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(p => p.FeeInvoice)
            .WithMany(i => i.PaymentTransactions)
            .HasForeignKey(p => p.FeeInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentGatewayConfig>()
            .HasIndex(c => c.Provider)
            .IsUnique();
    }
}
