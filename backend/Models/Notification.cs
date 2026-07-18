namespace Api.Models;

/// <summary>
/// Thông báo in-app cá nhân (FR1.4) — trung tâm thông báo trên mobile.
/// <para>
/// Quan hệ: N Notification — 1 <see cref="User"/> (cascade khi xóa User).
/// Index (UserId, IsRead, CreatedAt) phục vụ danh sách + badge chưa đọc.
/// </para>
/// </summary>
public class Notification
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> — người nhận thông báo.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation: người nhận.</summary>
    public User User { get; set; } = null!;

    /// <summary>Tiêu đề ngắn.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung chi tiết.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>False = chưa đọc (tính vào badge); True = đã đọc.</summary>
    public bool IsRead { get; set; }

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
