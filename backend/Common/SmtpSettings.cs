namespace Api.Common;

/// <summary>
/// Cấu hình SMTP gửi OTP quên MK (FR1.2) — thường dùng Gmail + App Password.
/// Đặt trong appsettings / appsettings.Development.json mục "Smtp".
/// </summary>
public class SmtpSettings
{
    /// <summary>Máy chủ SMTP — Gmail: smtp.gmail.com</summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>Cổng — Gmail TLS: 587</summary>
    public int Port { get; set; } = 587;

    /// <summary>Bật SSL/TLS (Gmail = true).</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Tài khoản Gmail gửi thư (vd your.name@gmail.com).</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu ứng dụng Google (App Password 16 ký tự) — KHÔNG dùng mật khẩu đăng nhập Gmail.
    /// Tạo tại: https://myaccount.google.com/apppasswords
    /// </summary>
    public string AppPassword { get; set; } = string.Empty;

    /// <summary>Địa chỉ From (thường = User). Để trống thì dùng User.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Tên hiển thị người gửi.</summary>
    public string FromName { get; set; } = "FSchool";

    /// <summary>Đã cấu hình đủ để gửi mail thật chưa.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(AppPassword);
}
