using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng Assignments (bài tập).</summary>
public interface IAssignmentRepository
{
    /// <summary>Danh sách bài tập, lọc theo lớp / môn.</summary>
    Task<List<Assignment>> GetListAsync(int? classId, int? subjectId);

    /// <summary>Chi tiết bài tập kèm Class, Subject, Teacher, Submissions.</summary>
    Task<Assignment?> GetByIdAsync(int id);

    /// <summary>Bài tập của các lớp mà HS đang học.</summary>
    Task<List<Assignment>> GetByClassIdsAsync(IEnumerable<int> classIds);

    /// <summary>Thêm bài tập.</summary>
    Task<Assignment> CreateAsync(Assignment entity);

    /// <summary>Cập nhật bài tập.</summary>
    Task UpdateAsync(Assignment entity);

    /// <summary>Xóa bài tập (cascade submissions theo cấu hình FK).</summary>
    Task DeleteAsync(Assignment entity);
}

/// <summary>Implement IAssignmentRepository bằng EF Core.</summary>
public class AssignmentRepository : IAssignmentRepository
{
    private readonly AppDbContext _context;

    public AssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Assignment> QueryWithIncludes() =>
        _context.Assignments
            .Include(a => a.Class)
            .Include(a => a.Subject)
            .Include(a => a.CreatedByTeacher)
            .Include(a => a.Submissions);

    /// <inheritdoc />
    public async Task<List<Assignment>> GetListAsync(int? classId, int? subjectId)
    {
        var query = QueryWithIncludes().AsQueryable();

        if (classId.HasValue)
            query = query.Where(a => a.ClassId == classId.Value);

        if (subjectId.HasValue)
            query = query.Where(a => a.SubjectId == subjectId.Value);

        return await query
            .OrderByDescending(a => a.DueDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Assignment?> GetByIdAsync(int id)
    {
        return await QueryWithIncludes()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<Assignment>> GetByClassIdsAsync(IEnumerable<int> classIds)
    {
        var ids = classIds.ToList();
        return await QueryWithIncludes()
            .Where(a => ids.Contains(a.ClassId))
            .OrderByDescending(a => a.DueDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Assignment> CreateAsync(Assignment entity)
    {
        _context.Assignments.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Assignment entity)
    {
        _context.Assignments.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Assignment entity)
    {
        _context.Assignments.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
