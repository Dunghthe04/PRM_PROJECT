namespace Api.Models;

/// <summary>
/// Bảng tin toàn trường hoặc theo lớp (FR3.4, FR5.4).
/// </summary>
public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>Global = toàn trường; Class = 1 lớp cụ thể.</summary>
    public AnnouncementType Type { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    /// <summary>Bắt buộc khi Type = Class.</summary>
    public int? TargetClassId { get; set; }
    public Class? TargetClass { get; set; }

    /// <summary>
    /// Môn liên quan khi GV gửi TB lớp (FR3.4) — null với Global hoặc Admin không chọn môn.
    /// </summary>
    public int? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
