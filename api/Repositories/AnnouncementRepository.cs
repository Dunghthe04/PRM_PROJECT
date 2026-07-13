using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng Announcements (Ngày 10 Bước 3).</summary>
public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(int id);
    Task<List<Announcement>> GetVisibleAsync(IReadOnlyList<int> classIds, AnnouncementType? type);
    Task<Announcement> CreateAsync(Announcement entity);
    Task UpdateAsync(Announcement entity);
    Task DeleteAsync(Announcement entity);
}

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _context;

    public AnnouncementRepository(AppDbContext context) => _context = context;

    private IQueryable<Announcement> WithDetails()
        => _context.Announcements
            .Include(a => a.CreatedBy)
            .Include(a => a.TargetClass);

    public async Task<Announcement?> GetByIdAsync(int id)
        => await WithDetails().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Announcement>> GetVisibleAsync(IReadOnlyList<int> classIds, AnnouncementType? type)
    {
        var q = WithDetails().AsQueryable();

        // Global + bảng tin lớp user thuộc về
        q = q.Where(a =>
            a.Type == AnnouncementType.Global
            || (a.Type == AnnouncementType.Class && a.TargetClassId != null && classIds.Contains(a.TargetClassId.Value)));

        if (type.HasValue)
            q = q.Where(a => a.Type == type.Value);

        return await q
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Announcement> CreateAsync(Announcement entity)
    {
        _context.Announcements.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Announcement entity)
    {
        _context.Announcements.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Announcement entity)
    {
        _context.Announcements.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
