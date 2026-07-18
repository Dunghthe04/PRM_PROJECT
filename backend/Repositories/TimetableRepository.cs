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

    /// <summary>Lấy tiết theo giáo viên trong 1 kỳ học.</summary>
    Task<List<TimetableSlot>> GetByTeacherAsync(int teacherId, int semesterId);

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

    /// <summary>
    /// Projection cột cần thiết — không Include cả entity (tránh chậm lần đầu).
    /// </summary>
    private async Task<List<TimetableSlot>> QuerySlotsAsync(
        System.Linq.Expressions.Expression<Func<TimetableSlot, bool>> predicate)
    {
        var rows = await _context.TimetableSlots
            .AsNoTracking()
            .Where(predicate)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.Period)
            .Select(t => new
            {
                t.Id,
                t.ClassId,
                ClassName = t.Class.Name,
                t.Class.SemesterId,
                t.SubjectId,
                SubjectName = t.Subject.Name,
                SubjectCode = t.Subject.Code,
                t.TeacherId,
                TeacherName = t.Teacher.FullName,
                t.DayOfWeek,
                t.Period,
                t.Room,
            })
            .ToListAsync();

        return rows.Select(r => new TimetableSlot
        {
            Id = r.Id,
            ClassId = r.ClassId,
            SubjectId = r.SubjectId,
            TeacherId = r.TeacherId,
            DayOfWeek = r.DayOfWeek,
            Period = r.Period,
            Room = r.Room,
            Class = new Class { Id = r.ClassId, Name = r.ClassName, SemesterId = r.SemesterId },
            Subject = new Subject { Id = r.SubjectId, Name = r.SubjectName, Code = r.SubjectCode },
            Teacher = new User { Id = r.TeacherId, FullName = r.TeacherName },
        }).ToList();
    }

    /// <inheritdoc />
    public Task<List<TimetableSlot>> GetByClassAsync(int classId)
        => QuerySlotsAsync(t => t.ClassId == classId);

    /// <inheritdoc />
    public async Task<List<TimetableSlot>> GetByClassIdsAsync(IEnumerable<int> classIds)
    {
        var ids = classIds.Distinct().ToList();
        if (ids.Count == 0) return new List<TimetableSlot>();
        // 1 id → so sánh trực tiếp (tránh OPENJSON/Contains chậm lần đầu).
        if (ids.Count == 1)
        {
            var only = ids[0];
            return await QuerySlotsAsync(t => t.ClassId == only);
        }

        return await QuerySlotsAsync(t => ids.Contains(t.ClassId));
    }

    /// <inheritdoc />
    public Task<List<TimetableSlot>> GetByTeacherAsync(int teacherId)
        => QuerySlotsAsync(t => t.TeacherId == teacherId);

    /// <inheritdoc />
    public Task<List<TimetableSlot>> GetByTeacherAsync(int teacherId, int semesterId)
        => QuerySlotsAsync(t => t.TeacherId == teacherId && t.Class.SemesterId == semesterId);

    /// <inheritdoc />
    public async Task<TimetableSlot?> GetByIdAsync(int id)
    {
        var list = await QuerySlotsAsync(t => t.Id == id);
        return list.FirstOrDefault();
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
