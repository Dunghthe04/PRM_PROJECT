namespace Api.Models;

/// <summary>Mục đích OTP gửi về SĐT — đăng ký xác thực hoặc quên mật khẩu.</summary>
public static class OtpPurpose
{
    public const string Register = "Register";
    public const string ResetPassword = "ResetPassword";
}
