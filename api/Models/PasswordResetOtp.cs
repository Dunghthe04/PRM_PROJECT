namespace Api.Models;

/// <summary>
/// Lưu mã OTP gửi về SĐT (xác thực đăng ký hoặc quên mật khẩu).
/// Mỗi mã one-time, TTL ngắn; hết hạn / đã dùng thì không tái sử dụng.
/// </summary>
public class PasswordResetOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Mã OTP 6 số (TTL ngắn; production có thể hash thêm).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Luôn là Phone theo quyết định sản phẩm.</summary>
    public string Channel { get; set; } = OtpChannel.Phone;

    /// <summary>SĐT nhận OTP.</summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>Register | ResetPassword — xem <see cref="OtpPurpose"/>.</summary>
    public string Purpose { get; set; } = OtpPurpose.ResetPassword;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
