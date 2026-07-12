namespace Api.Models;

/// <summary>
/// Bài tập do GV giao cho 1 lớp + 1 môn (FR3.5, FR2.4).
/// HS nộp qua Submission; trạng thái To-Do / Done / Overdue tính theo DueDate + đã nộp chưa.
/// </summary>
public class Assignment
{
    public int Id { get; set; }

    /// <summary>Tiêu đề bài tập.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Đề bài / mô tả chi tiết.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hạn nộp (UTC). Quá hạn + chưa nộp = Overdue.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Điểm tối đa khi chấm (mặc định 10).</summary>
    public double MaxScore { get; set; } = 10;

    /// <summary>Link/file đề bài đính kèm (tuỳ chọn).</summary>
    public string? AttachmentUrl { get; set; }

    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    /// <summary>GV tạo bài tập.</summary>
    public int CreatedByTeacherId { get; set; }
    public User CreatedByTeacher { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Các bài nộp của học sinh (navigation — WithMany rõ ràng).</summary>
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
