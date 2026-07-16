namespace Api.Models;

/// <summary>Mục đích OTP gửi về SĐT — gắn vào <see cref="PasswordResetOtp.Purpose"/>.</summary>
public static class OtpPurpose
{
    /// <summary>Xác thực SĐT khi đăng ký tài khoản.</summary>
    public const string Register = "Register";

    /// <summary>Luồng quên mật khẩu (forgot → verify → reset).</summary>
    public const string ResetPassword = "ResetPassword";
}
