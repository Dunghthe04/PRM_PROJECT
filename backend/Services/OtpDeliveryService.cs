using System.Net;
using System.Net.Mail;
using Api.Common;
using Api.Models;
using Microsoft.Extensions.Options;

namespace Api.Services;

/// <summary>
/// Gửi OTP ra kênh ngoài:
/// - Email → SMTP Gmail thật (FR1.2 quên MK)
/// - Phone → log console Dev (chưa gắn SMS gateway)
/// </summary>
public interface IOtpDeliveryService
{
    /// <summary>Gửi mã OTP tới destination (SĐT hoặc email tùy channel).</summary>
    Task SendAsync(string channel, string destination, string otpCode);
}

/// <summary>
/// Gửi OTP email qua SMTP; OTP đăng ký (SĐT) vẫn log console.
/// </summary>
public class SmtpOtpDeliveryService : IOtpDeliveryService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<SmtpOtpDeliveryService> _logger;

    public SmtpOtpDeliveryService(
        IOptions<SmtpSettings> smtp,
        ILogger<SmtpOtpDeliveryService> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task SendAsync(string channel, string destination, string otpCode)
    {
        if (string.Equals(channel, OtpChannel.Email, StringComparison.OrdinalIgnoreCase))
        {
            await SendEmailOtpAsync(destination, otpCode);
            return;
        }

        // Đăng ký: chưa có SMS gateway — Dev đọc mã trên console.
        _logger.LogWarning(
            "[DEV OTP SMS] Phone={Phone} Code={Code} — nhập mã này trên Swagger",
            destination,
            otpCode);
    }

    /// <summary>Gửi thư OTP quên MK tới hộp thư thật của user.</summary>
    private async Task SendEmailOtpAsync(string toEmail, string otpCode)
    {
        if (!_smtp.IsConfigured)
        {
            throw new InvalidOperationException(
                "Chưa cấu hình SMTP Gmail. Điền Smtp:User và Smtp:AppPassword trong appsettings.Development.json "
                + "(tạo App Password tại https://myaccount.google.com/apppasswords).");
        }

        var from = string.IsNullOrWhiteSpace(_smtp.FromEmail) ? _smtp.User : _smtp.FromEmail;

        using var message = new MailMessage
        {
            From = new MailAddress(from, _smtp.FromName),
            Subject = "[FSchool] Mã OTP quên mật khẩu",
            Body =
                $"Xin chào,\n\n"
                + $"Mã OTP đặt lại mật khẩu FSchool của bạn là: {otpCode}\n\n"
                + $"Mã có hiệu lực trong 5 phút. Không chia sẻ mã này với người khác.\n\n"
                + $"— Hệ thống FSchool",
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            Credentials = new NetworkCredential(_smtp.User, _smtp.AppPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("[OTP EMAIL] Đã gửi OTP tới {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OTP EMAIL] Gửi thất bại tới {Email}", toEmail);
            throw new InvalidOperationException(
                "Không gửi được email OTP. Kiểm tra Smtp (Gmail App Password) và thử lại.", ex);
        }
    }
}

/// <summary>
/// Fallback Dev thuần console — chỉ dùng khi cố ý đăng ký thay SmtpOtpDeliveryService.
/// </summary>
public class ConsoleOtpDeliveryService : IOtpDeliveryService
{
    private readonly ILogger<ConsoleOtpDeliveryService> _logger;

    public ConsoleOtpDeliveryService(ILogger<ConsoleOtpDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string channel, string destination, string otpCode)
    {
        if (string.Equals(channel, OtpChannel.Email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[DEV OTP EMAIL] Email={Email} Code={Code}",
                destination,
                otpCode);
        }
        else
        {
            _logger.LogWarning(
                "[DEV OTP SMS] Phone={Phone} Code={Code}",
                destination,
                otpCode);
        }

        return Task.CompletedTask;
    }
}
