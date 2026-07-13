namespace Api.Models;

/// <summary>
/// Thiết bị mobile đăng ký FCM token để nhận push (FR1.4 — Ngày 10).
/// </summary>
public class UserDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>FCM registration token — unique toàn hệ thống.</summary>
    public string FcmToken { get; set; } = string.Empty;

    /// <summary>android / ios / web (tuỳ chọn).</summary>
    public string Platform { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
