namespace Api.Models;

/// <summary>
/// Bảng tin toàn trường / theo lớp, hoặc thông báo Admin gửi GV (FR3.4, FR5.4).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Announcement — 1 CreatedBy (<see cref="User"/> — Admin hoặc Teacher).</item>
/// <item>N Announcement — 0..1 <see cref="TargetClass"/> (bắt buộc khi Type = Class).</item>
/// <item>N Announcement — 0..1 <see cref="Subject"/> (tuỳ chọn khi GV gửi TB lớp).</item>
/// <item>N Announcement — 0..1 <see cref="TargetUser"/> (bắt buộc khi Type = Teacher).</item>
/// </list>
/// </para>
/// </summary>
public class Announcement
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tiêu đề bảng tin.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung bảng tin.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Global / Class / Teachers / Teacher — xem <see cref="AnnouncementType"/>.</summary>
    public AnnouncementType Type { get; set; }

    /// <summary>FK → <see cref="User"/> — người đăng (Admin / Teacher).</summary>
    public int CreatedById { get; set; }

    /// <summary>Navigation: người tạo bảng tin.</summary>
    public User CreatedBy { get; set; } = null!;

    /// <summary>FK → <see cref="Class"/> — bắt buộc khi Type = Class; null với Global.</summary>
    public int? TargetClassId { get; set; }

    /// <summary>Navigation: lớp nhận bảng tin (nullable).</summary>
    public Class? TargetClass { get; set; }

    /// <summary>
    /// FK → <see cref="Subject"/> — môn liên quan khi GV gửi TB lớp (FR3.4);
    /// null với Global hoặc Admin không chọn môn.
    /// </summary>
    public int? SubjectId { get; set; }

    /// <summary>Navigation: môn liên quan (nullable).</summary>
    public Subject? Subject { get; set; }

    /// <summary>FK → <see cref="User"/> — GV nhận khi Type = Teacher.</summary>
    public int? TargetUserId { get; set; }

    /// <summary>Navigation: giáo viên nhận (nullable).</summary>
    public User? TargetUser { get; set; }

    /// <summary>Thời điểm đăng (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm sửa gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
