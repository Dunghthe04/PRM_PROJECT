namespace Api.Models;

/// <summary>
/// Bài tập do GV giao cho 1 lớp + 1 môn (FR3.5, FR2.4 — dữ liệu DB; UI app đã bỏ).
/// HS nộp qua <see cref="Submission"/>; trạng thái To-Do/Done/Overdue tính runtime theo DueDate.
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Assignment — 1 <see cref="Class"/>.</item>
/// <item>N Assignment — 1 <see cref="Subject"/>.</item>
/// <item>N Assignment — 1 CreatedByTeacher (<see cref="User"/>).</item>
/// <item>1 Assignment — N <see cref="Submission"/>.</item>
/// </list>
/// </para>
/// </summary>
public class Assignment
{
    /// <summary>Khóa chính.</summary>
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

    /// <summary>FK → <see cref="Class"/> — lớp nhận bài tập.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="Subject"/> — môn của bài tập.</summary>
    public int SubjectId { get; set; }

    /// <summary>Navigation: môn học.</summary>
    public Subject Subject { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Teacher) — GV tạo bài.</summary>
    public int CreatedByTeacherId { get; set; }

    /// <summary>Navigation: giáo viên tạo bài tập.</summary>
    public User CreatedByTeacher { get; set; } = null!;

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm sửa gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Navigation: các bài nộp của học sinh (1–N).</summary>
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
