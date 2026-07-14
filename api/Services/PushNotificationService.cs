using FirebaseAdmin.Messaging;

namespace Api.Services;

/// <summary>
/// Gửi push notification qua FCM (Ngày 10).
/// Dev (không có credential): log ra console.
/// Thật: FirebaseAdminPushNotificationService dùng Firebase Admin SDK.
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(IReadOnlyList<string> fcmTokens, string title, string message);
}

/// <summary>
/// Gửi push THẬT qua Firebase Admin SDK (FR1.4).
/// Yêu cầu FirebaseApp đã được khởi tạo ở Program.cs bằng service account.
/// </summary>
public class FirebaseAdminPushNotificationService : IPushNotificationService
{
    private readonly ILogger<FirebaseAdminPushNotificationService> _logger;

    public FirebaseAdminPushNotificationService(ILogger<FirebaseAdminPushNotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gửi 1 notification tới nhiều thiết bị cùng lúc (multicast).
    /// Nhận: danh sách fcmTokens, tiêu đề, nội dung. Không ném lỗi ra ngoài.
    /// </summary>
    public async Task SendAsync(IReadOnlyList<string> fcmTokens, string title, string message)
    {
        if (fcmTokens.Count == 0) return;

        var msg = new MulticastMessage
        {
            Tokens = fcmTokens.ToList(),
            Notification = new Notification { Title = title, Body = message },
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(msg);
            _logger.LogInformation(
                "[FCM] Gửi push: thành công={Success} thất bại={Failure}",
                response.SuccessCount, response.FailureCount);
        }
        catch (Exception ex)
        {
            // Không để lỗi push làm hỏng nghiệp vụ chính (nhập điểm, duyệt đơn...).
            _logger.LogError(ex, "[FCM] Gửi push thất bại.");
        }
    }
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
