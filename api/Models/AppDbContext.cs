using Microsoft.EntityFrameworkCore;

namespace Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<StudentParent> StudentParents { get; set; }
    public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
    
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<ClassStudent> ClassStudents { get; set; }
    public DbSet<TeacherAssignment> TeacherAssignments { get; set; }
    public DbSet<TimetableSlot> TimetableSlots { get; set; }

    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<Grade> Grades { get; set; }

    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    public DbSet<FeeCategory> FeeCategories { get; set; }
    public DbSet<FeeInvoice> FeeInvoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Many-to-Many: Student - Parent
        modelBuilder.Entity<StudentParent>()
            .HasKey(sp => new { sp.StudentId, sp.ParentId });

        modelBuilder.Entity<StudentParent>()
            .HasOne(sp => sp.Student)
            .WithMany(u => u.ParentLinks)
            .HasForeignKey(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentParent>()
            .HasOne(sp => sp.Parent)
            .WithMany(u => u.ChildLinks)
            .HasForeignKey(sp => sp.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-Many: Class - Student
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

        // Prevent cascading deletes on TeacherAssignment
        modelBuilder.Entity<TeacherAssignment>()
            .HasOne(ta => ta.Teacher)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent cascading deletes on Assignments and Submissions to avoid cycles
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

        // Điểm danh: mỗi HS chỉ 1 bản ghi / lớp / ngày
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

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.ClientRecordId)
            .IsUnique()
            .HasFilter("[ClientRecordId] IS NOT NULL");
            
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
            
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedBy)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Email unique khi có giá trị; Phone bắt buộc + unique (định danh login)
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

        modelBuilder.Entity<PasswordResetOtp>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetOtp>()
            .HasIndex(o => o.ResetToken);

        modelBuilder.Entity<PasswordResetOtp>()
            .HasIndex(o => new { o.UserId, o.Purpose, o.IsUsed });

        // TimetableSlot: 1 lớp không trùng tiết trong cùng thứ
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
    }
}
