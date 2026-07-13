namespace Api.DTOs;

// ─── FR1.4 — Thông báo in-app + FCM (Ngày 10) ──────────────────────────────

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class RegisterDeviceDto
{
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
}

public class MarkReadResultDto
{
    public string Message { get; set; } = string.Empty;
}
