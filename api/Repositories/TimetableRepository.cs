using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng TimetableSlots.</summary>
public interface ITimetableRepository
{
    /// <summary>Lấy tiết theo lớp, sắp theo thứ + tiết.</summary>
    Task<List<TimetableSlot>> GetByClassAsync(int classId);

    /// <summary>Lấy tiết theo danh sách lớp (HS có thể nhiều lớp).</summary>
    Task<List<TimetableSlot>> GetByClassIdsAsync(IEnumerable<int> classIds);

    /// <summary>Lấy tiết theo giáo viên.</summary>
    Task<List<TimetableSlot>> GetByTeacherAsync(int teacherId);

    /// <summary>Chi tiết 1 tiết.</summary>
    Task<TimetableSlot?> GetByIdAsync(int id);

    /// <summary>Tìm tiết trùng (cùng lớp + thứ + tiết).</summary>
    Task<TimetableSlot?> FindConflictAsync(int classId, int dayOfWeek, int period, int? excludeId = null);

    /// <summary>Thêm tiết.</summary>
    Task<TimetableSlot> CreateAsync(TimetableSlot entity);

    /// <summary>Cập nhật tiết.</summary>
    Task UpdateAsync(TimetableSlot entity);

    /// <summary>Xóa tiết.</summary>
    Task DeleteAsync(TimetableSlot entity);
}

/// <summary>Implement ITimetableRepository bằng EF Core.</summary>
public class TimetableRepository : ITimetableRepository
{
    private readonly AppDbContext _context;

    public TimetableRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<TimetableSlot> QueryWithIncludes() =>
        _context.TimetableSlots
            .Include(t => t.Class)
            .Include(t => t.Subject)
            .Include(t => t.Teacher);

    /// <inheritdoc />
    public async Task<List<TimetableSlot>> GetByClassAsync(int classId)
    {
        return await QueryWithIncludes()
            .Where(t => t.ClassId == classId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.Period)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<TimetableSlot>> GetByClassIdsAsync(IEnumerable<int> classIds)
    {
        var ids = classIds.ToList();
        return await QueryWithIncludes()
            .Where(t => ids.Contains(t.ClassId))
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.Period)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<TimetableSlot>> GetByTeacherAsync(int teacherId)
    {
        return await QueryWithIncludes()
            .Where(t => t.TeacherId == teacherId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.Period)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TimetableSlot?> GetByIdAsync(int id)
    {
        return await QueryWithIncludes().FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <inheritdoc />
    public async Task<TimetableSlot?> FindConflictAsync(int classId, int dayOfWeek, int period, int? excludeId = null)
    {
        var query = _context.TimetableSlots
            .Where(t => t.ClassId == classId && t.DayOfWeek == dayOfWeek && t.Period == period);

        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);

        return await query.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<TimetableSlot> CreateAsync(TimetableSlot entity)
    {
        _context.TimetableSlots.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TimetableSlot entity)
    {
        _context.TimetableSlots.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TimetableSlot entity)
    {
        _context.TimetableSlots.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
