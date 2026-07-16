namespace Api.Models;

/// <summary>
/// Bản ghi điểm của 1 học sinh cho 1 đầu điểm (FR5.6 / FR2.3).
/// Luồng: nhập Nháp (<see cref="GradeStatus.Draft"/>) → Publish → HS/PH xem.
/// Unique: (StudentId, ClassId, SubjectId, SemesterId, AssessmentType).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Grade — 1 Student (<see cref="User"/>).</item>
/// <item>N Grade — 1 <see cref="Class"/>.</item>
/// <item>N Grade — 1 <see cref="Subject"/>.</item>
/// <item>N Grade — 1 <see cref="Semester"/>.</item>
/// <item>N Grade — 1 CreatedByTeacher; 0..1 ApprovedBy (Admin duyệt tuỳ chọn).</item>
/// </list>
/// </para>
/// </summary>
public class Grade
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> (Role = Student) — học sinh được chấm.</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh.</summary>
    public User Student { get; set; } = null!;

    /// <summary>FK → <see cref="Class"/> — ngữ cảnh lớp khi nhập điểm.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="Subject"/> — môn được chấm.</summary>
    public int SubjectId { get; set; }

    /// <summary>Navigation: môn học.</summary>
    public Subject Subject { get; set; } = null!;

    /// <summary>FK → <see cref="Semester"/> — kỳ học của đầu điểm.</summary>
    public int SemesterId { get; set; }

    /// <summary>Navigation: học kỳ.</summary>
    public Semester Semester { get; set; } = null!;

    /// <summary>Loại đầu điểm: Midterm, Final, Oral, Quiz15, …</summary>
    public string AssessmentType { get; set; } = string.Empty;

    /// <summary>Điểm số.</summary>
    public double Score { get; set; }

    /// <summary>Draft = nháp (HS/PH chưa thấy); Published = đã công bố.</summary>
    public GradeStatus Status { get; set; } = GradeStatus.Draft;

    /// <summary>FK → <see cref="User"/> — người tạo / cập nhật bản ghi điểm.</summary>
    public int CreatedByTeacherId { get; set; }

    /// <summary>Navigation: người tạo điểm.</summary>
    public User CreatedByTeacher { get; set; } = null!;

    /// <summary>Thời điểm công bố — null khi còn Nháp.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Tuỳ chọn: Admin đã duyệt sau khi Publish.</summary>
    public bool IsApproved { get; set; }

    /// <summary>FK → <see cref="User"/> (Admin) — người duyệt; null nếu chưa duyệt.</summary>
    public int? ApprovedById { get; set; }

    /// <summary>Navigation: Admin duyệt (nullable).</summary>
    public User? ApprovedBy { get; set; }

    /// <summary>Thời điểm duyệt (UTC).</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
