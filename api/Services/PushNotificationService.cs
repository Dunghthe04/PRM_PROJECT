namespace Api.Services;

/// <summary>
/// Gửi push notification qua FCM (Ngày 10).
/// Dev: log ra console. Production: thay bằng Firebase Admin SDK.
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(IReadOnlyList<string> fcmTokens, string title, string message);
}

/// <summary>Implement dev — in log thay vì gọi Firebase thật.</summary>
public class ConsolePushNotificationService : IPushNotificationService
{
    private readonly ILogger<ConsolePushNotificationService> _logger;

    public ConsolePushNotificationService(ILogger<ConsolePushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(IReadOnlyList<string> fcmTokens, string title, string message)
    {
        if (fcmTokens.Count == 0)
            return Task.CompletedTask;

        _logger.LogInformation(
            "[DEV FCM PUSH] tokens={TokenCount} title={Title} message={Message}",
            fcmTokens.Count, title, message);

        foreach (var token in fcmTokens.Take(3))
            _logger.LogDebug("[DEV FCM TOKEN] {Token}", token[..Math.Min(20, token.Length)] + "...");

        return Task.CompletedTask;
    }
}
