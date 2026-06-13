using Microsoft.EntityFrameworkCore;

namespace Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<StudentParent> StudentParents { get; set; }
    
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<ClassStudent> ClassStudents { get; set; }
    public DbSet<TeacherAssignment> TeacherAssignments { get; set; }

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

        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<LeaveRequest>()
            .HasOne(lr => lr.Student)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedBy)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
