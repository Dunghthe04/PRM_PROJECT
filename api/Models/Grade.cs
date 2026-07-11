namespace Api.Models;

/// <summary>
/// Bản ghi điểm của 1 học sinh cho 1 đầu điểm (FR3.2, FR2.3).
/// Luồng: GV nhập Nháp (Draft) → Publish → HS/PH xem bảng điểm.
/// </summary>
public class Grade
{
    public int Id { get; set; }

    /// <summary>Học sinh được chấm điểm.</summary>
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>Lớp học — ngữ cảnh nhập điểm (GV chọn lớp + môn).</summary>
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    /// <summary>Loại đầu điểm: Midterm, Final, Oral, Quiz15, ...</summary>
    public string AssessmentType { get; set; } = string.Empty;

    public double Score { get; set; }

    /// <summary>Draft = nháp (chỉ GV thấy); Published = đã công bố.</summary>
    public GradeStatus Status { get; set; } = GradeStatus.Draft;

    /// <summary>GV tạo / cập nhật bản ghi điểm.</summary>
    public int CreatedByTeacherId { get; set; }
    public User CreatedByTeacher { get; set; } = null!;

    /// <summary>Thời điểm công bố — null khi còn Nháp.</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Tuỳ chọn: Trưởng bộ môn duyệt sau khi Publish.</summary>
    public bool IsApproved { get; set; }

    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
