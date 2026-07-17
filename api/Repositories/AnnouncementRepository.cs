using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng Announcements (Ngày 10 Bước 3).</summary>
public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(int id);
    Task<List<Announcement>> GetVisibleAsync(IReadOnlyList<int> classIds, AnnouncementType? type);
    /// <summary>Lịch sử bảng tin do user tạo (GV xem TB đã gửi).</summary>
    Task<List<Announcement>> GetByCreatorAsync(int createdById);
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
            .Include(a => a.TargetClass)
            .Include(a => a.Subject)
            .Include(a => a.TargetUser);

    public async Task<Announcement?> GetByIdAsync(int id)
        => await WithDetails().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Announcement>> GetVisibleAsync(IReadOnlyList<int> classIds, AnnouncementType? type)
    {
        // Bảng tin công khai: chỉ tin toàn trường (nhà trường).
        // Class / Teachers / Teacher không lên feed — xem ở chuông hoặc GET /announcements/mine.
        var q = WithDetails().Where(a => a.Type == AnnouncementType.Global);

        if (type.HasValue)
            q = q.Where(a => a.Type == type.Value);

        return await q
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Announcement>> GetByCreatorAsync(int createdById)
        => await WithDetails()
            .Where(a => a.CreatedById == createdById)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

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
