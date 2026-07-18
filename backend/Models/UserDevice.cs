namespace Api.Models;

/// <summary>
/// Thiết bị đăng ký FCM token để nhận push (FR1.4).
/// <para>
/// Quan hệ: N UserDevice — 1 <see cref="User"/> (cascade khi xóa User).
/// <see cref="FcmToken"/> unique toàn hệ thống.
/// </para>
/// </summary>
public class UserDevice
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> — chủ sở hữu thiết bị.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation: người dùng sở hữu token.</summary>
    public User User { get; set; } = null!;

    /// <summary>FCM registration token — unique toàn hệ thống.</summary>
    public string FcmToken { get; set; } = string.Empty;

    /// <summary>Nền tảng: android / ios / web (tuỳ chọn).</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Thời điểm đăng ký (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật token gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
