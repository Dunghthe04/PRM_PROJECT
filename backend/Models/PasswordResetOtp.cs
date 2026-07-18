namespace Api.Models;

/// <summary>
/// Lưu mã OTP (xác thực đăng ký qua SĐT hoặc quên mật khẩu qua Email — FR1.1, FR1.2).
/// Mỗi mã one-time, TTL ngắn; hết hạn / đã dùng thì không tái sử dụng.
/// <para>
/// Quan hệ: N PasswordResetOtp — 1 <see cref="User"/> (cascade khi xóa User).
/// </para>
/// </summary>
public class PasswordResetOtp
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> — chủ tài khoản nhận OTP.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation: người dùng đích.</summary>
    public User User { get; set; } = null!;

    /// <summary>Mã OTP 6 số (TTL ngắn; production có thể hash thêm).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Kênh gửi — Phone (đăng ký) hoặc Email (quên MK). Xem <see cref="OtpChannel"/>.</summary>
    public string Channel { get; set; } = OtpChannel.Phone;

    /// <summary>Đích nhận OTP — SĐT hoặc email tùy Channel.</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Register | ResetPassword — xem <see cref="OtpPurpose"/>.</summary>
    public string Purpose { get; set; } = OtpPurpose.ResetPassword;

    /// <summary>Thời điểm tạo OTP (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm hết hạn (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>True sau khi verify-otp (quên MK) thành công.</summary>
    public bool IsVerified { get; set; }

    /// <summary>True sau khi hoàn tất mục đích (verify phone / reset MK).</summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Token ngắn hạn sau verify-otp quên MK.
    /// Client dùng ở bước reset-password (không gửi lại OTP).
    /// </summary>
    public string? ResetToken { get; set; }
}
