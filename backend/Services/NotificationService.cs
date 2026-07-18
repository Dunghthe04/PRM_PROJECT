using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>Nghiệp vụ thông báo in-app + đăng ký FCM (FR1.4 — Ngày 10 Bước 4).</summary>
public interface INotificationService
{
    Task<PagedResultDto<NotificationDto>> GetMyAsync(int userId, NotificationListQueryDto query);
    Task<UnreadCountDto> GetUnreadCountAsync(int userId);
    Task<(bool Success, string Message)> MarkReadAsync(int id, int userId);
    Task<MarkReadResultDto> MarkAllReadAsync(int userId);
    Task<(bool Success, string? Error)> RegisterDeviceAsync(int userId, RegisterDeviceDto dto);
    Task<(bool Success, string Message)> UnregisterDeviceAsync(int userId, string fcmToken);

    /// <summary>Ghi in-app + gửi push — dùng chung cho Announcement, LeaveRequest, v.v.</summary>
    Task NotifyUsersAsync(
        IReadOnlyList<int> userIds, string title, string message, bool sendPush = true);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IPushNotificationService _pushService;

    public NotificationService(
        INotificationRepository repository,
        IPushNotificationService pushService)
    {
        _repository = repository;
        _pushService = pushService;
    }

    public async Task<PagedResultDto<NotificationDto>> GetMyAsync(int userId, NotificationListQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var (items, total) = await _repository.GetPagedAsync(userId, query);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResultDto<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = totalPages
        };
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(int userId)
    {
        var count = await _repository.GetUnreadCountAsync(userId);
        return new UnreadCountDto { Count = count };
    }

    public async Task<(bool Success, string Message)> MarkReadAsync(int id, int userId)
    {
        var notification = await _repository.GetByIdAsync(id);
        if (notification == null) return (false, "Không tìm thấy thông báo.");
        if (notification.UserId != userId) return (false, "Không có quyền thao tác thông báo này.");

        if (!notification.IsRead)
            await _repository.MarkReadAsync(notification);

        return (true, "Đã đánh dấu đã đọc.");
    }

    public async Task<MarkReadResultDto> MarkAllReadAsync(int userId)
    {
        await _repository.MarkAllReadAsync(userId);
        return new MarkReadResultDto { Message = "Đã đánh dấu tất cả là đã đọc." };
    }

    public async Task<(bool Success, string? Error)> RegisterDeviceAsync(int userId, RegisterDeviceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FcmToken))
            return (false, "fcmToken là bắt buộc.");

        var device = new UserDevice
        {
            UserId = userId,
            FcmToken = dto.FcmToken.Trim(),
            Platform = string.IsNullOrWhiteSpace(dto.Platform) ? "android" : dto.Platform.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.UpsertDeviceAsync(device);
        return (true, null);
    }

    public async Task<(bool Success, string Message)> UnregisterDeviceAsync(int userId, string fcmToken)
    {
        if (string.IsNullOrWhiteSpace(fcmToken))
            return (false, "token là bắt buộc.");

        var device = await _repository.FindDeviceByTokenAsync(fcmToken.Trim());
        if (device == null) return (false, "Không tìm thấy thiết bị.");
        if (device.UserId != userId) return (false, "Không có quyền hủy thiết bị này.");

        await _repository.DeleteDeviceAsync(device);
        return (true, "Đã hủy đăng ký thiết bị.");
    }

    public async Task NotifyUsersAsync(
        IReadOnlyList<int> userIds, string title, string message, bool sendPush = true)
    {
        if (userIds.Count == 0) return;

        var distinctIds = userIds.Distinct().ToList();
        var now = DateTime.UtcNow;

        var notifications = distinctIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        await _repository.CreateManyAsync(notifications);

        if (!sendPush) return;

        var devices = await _repository.GetDevicesByUserIdsAsync(distinctIds);
        var tokens = devices.Select(d => d.FcmToken).Distinct().ToList();
        await _pushService.SendAsync(tokens, title, message);
    }

    internal static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
