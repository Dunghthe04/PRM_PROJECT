namespace Api.Models;

/// <summary>Kênh gửi OTP — dự án chỉ dùng Phone (SMS / log console ở Dev).</summary>
public static class OtpChannel
{
    /// <summary>Gửi OTP về số điện thoại.</summary>
    public const string Phone = "Phone";
}
