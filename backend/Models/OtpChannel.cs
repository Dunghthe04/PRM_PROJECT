namespace Api.Models;

/// <summary>
/// Kênh gửi OTP:
/// - Phone: xác thực đăng ký (FR1.1)
/// - Email: quên mật khẩu (FR1.2)
/// </summary>
public static class OtpChannel
{
    /// <summary>Gửi OTP về số điện thoại (đăng ký).</summary>
    public const string Phone = "Phone";

    /// <summary>Gửi OTP về email (quên mật khẩu).</summary>
    public const string Email = "Email";
}
