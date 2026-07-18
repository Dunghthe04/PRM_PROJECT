namespace Api.Models;

/// <summary>
/// Bài nộp của 1 học sinh cho 1 <see cref="Assignment"/> (FR2.4, FR3.5).
/// Mỗi cặp (AssignmentId, StudentId) chỉ 1 bản ghi — nộp lại = cập nhật.
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Submission — 1 <see cref="Assignment"/> (cascade khi xóa Assignment).</item>
/// <item>N Submission — 1 Student (<see cref="User"/>).</item>
/// <item>N Submission — 0..1 GradedByTeacher (<see cref="User"/>) — null nếu chưa chấm.</item>
/// </list>
/// </para>
/// </summary>
public class Submission
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Assignment"/> — bài tập được nộp.</summary>
    public int AssignmentId { get; set; }

    /// <summary>Navigation: bài tập cha.</summary>
    public Assignment Assignment { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Student) — học sinh nộp bài.</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh.</summary>
    public User Student { get; set; } = null!;

    /// <summary>Link nộp bài (Drive, URL…) — ưu tiên dùng field này.</summary>
    public string? LinkUrl { get; set; }

    /// <summary>Đường dẫn file đã upload (nếu nộp bằng file).</summary>
    public string? FileUrl { get; set; }

    /// <summary>Thời điểm nộp lần đầu (UTC).</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm nộp lại gần nhất — null nếu chưa nộp lại.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Điểm GV chấm — null = chưa chấm.</summary>
    public double? Score { get; set; }

    /// <summary>Nhận xét / feedback của GV.</summary>
    public string? Feedback { get; set; }

    /// <summary>FK → <see cref="User"/> (Role = Teacher) — GV chấm; null nếu chưa chấm.</summary>
    public int? GradedByTeacherId { get; set; }

    /// <summary>Navigation: giáo viên chấm bài (nullable).</summary>
    public User? GradedByTeacher { get; set; }

    /// <summary>Thời điểm chấm (UTC).</summary>
    public DateTime? GradedAt { get; set; }
}
