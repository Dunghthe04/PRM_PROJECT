using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập Notifications + UserDevices (Ngày 10 Bước 3).</summary>
public interface INotificationRepository
{
    Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(int userId, NotificationListQueryDto query);
    Task<int> GetUnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int id);
    Task MarkReadAsync(Notification notification);
    Task MarkAllReadAsync(int userId);
    Task CreateManyAsync(IEnumerable<Notification> notifications);

    Task<UserDevice?> FindDeviceByTokenAsync(string fcmToken);
    Task<List<UserDevice>> GetDevicesByUserIdsAsync(IReadOnlyList<int> userIds);
    Task<UserDevice> UpsertDeviceAsync(UserDevice device);
    Task DeleteDeviceAsync(UserDevice device);
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    private const int MaxPageSize = 100;

    public NotificationRepository(AppDbContext context) => _context = context;

    public async Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(
        int userId, NotificationListQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var q = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _context.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<Notification?> GetByIdAsync(int id)
        => await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public async Task MarkReadAsync(Notification notification)
    {
        notification.IsRead = true;
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task CreateManyAsync(IEnumerable<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
    }

    public async Task<UserDevice?> FindDeviceByTokenAsync(string fcmToken)
        => await _context.UserDevices.FirstOrDefaultAsync(d => d.FcmToken == fcmToken);

    public async Task<List<UserDevice>> GetDevicesByUserIdsAsync(IReadOnlyList<int> userIds)
    {
        if (userIds.Count == 0) return new List<UserDevice>();
        return await _context.UserDevices
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync();
    }

    public async Task<UserDevice> UpsertDeviceAsync(UserDevice device)
    {
        var existing = await FindDeviceByTokenAsync(device.FcmToken);
        if (existing != null)
        {
            existing.UserId = device.UserId;
            existing.Platform = device.Platform;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.UserDevices.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        _context.UserDevices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }

    public async Task DeleteDeviceAsync(UserDevice device)
    {
        _context.UserDevices.Remove(device);
        await _context.SaveChangesAsync();
    }
}
