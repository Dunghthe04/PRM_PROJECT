namespace Api.Services;

/// <summary>
/// Gửi OTP ra kênh ngoài (SMS).
/// Dev: log console. Production: thay bằng SMS gateway thật.
/// </summary>
public interface IOtpDeliveryService
{
    /// <summary>Gửi mã OTP tới destination (SĐT).</summary>
    Task SendAsync(string channel, string destination, string otpCode);
}

/// <summary>
/// Implementation Development — không gọi SMS thật, chỉ ghi log.
/// Giúp test trên Swagger/Postman mà không cần cấu hình gateway.
/// </summary>
public class ConsoleOtpDeliveryService : IOtpDeliveryService
{
    private readonly ILogger<ConsoleOtpDeliveryService> _logger;

    public ConsoleOtpDeliveryService(ILogger<ConsoleOtpDeliveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>Log OTP ra console với prefix [DEV OTP SMS] để QA copy mã.</summary>
    public Task SendAsync(string channel, string destination, string otpCode)
    {
        _logger.LogWarning(
            "[DEV OTP SMS] Phone={Phone} Code={Code} — nhập mã này trên Swagger",
            destination,
            otpCode);

        return Task.CompletedTask;
    }
}
