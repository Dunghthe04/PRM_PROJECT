namespace Api.Models;

/// <summary>
/// Bài nộp của 1 học sinh cho 1 Assignment (FR2.4, FR3.5).
/// Mỗi cặp (AssignmentId, StudentId) chỉ 1 bản ghi — nộp lại = cập nhật.
/// </summary>
public class Submission
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>Link nộp bài (Google Drive, URL, ...) — ưu tiên dùng field này.</summary>
    public string? LinkUrl { get; set; }

    /// <summary>File đã upload (đường dẫn /avatars-style hoặc CDN).</summary>
    public string? FileUrl { get; set; }

    /// <summary>Thời điểm nộp lần đầu.</summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm nộp lại gần nhất (null nếu chưa nộp lại).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Điểm GV chấm — null = chưa chấm.</summary>
    public double? Score { get; set; }

    /// <summary>Nhận xét / feedback của GV.</summary>
    public string? Feedback { get; set; }

    /// <summary>GV chấm bài (null nếu chưa chấm).</summary>
    public int? GradedByTeacherId { get; set; }
    public User? GradedByTeacher { get; set; }

    /// <summary>Thời điểm chấm.</summary>
    public DateTime? GradedAt { get; set; }
}
