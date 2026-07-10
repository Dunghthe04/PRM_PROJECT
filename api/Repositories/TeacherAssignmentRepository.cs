using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng TeacherAssignments (phân công GV–Lớp–Môn).</summary>
public interface ITeacherAssignmentRepository
{
    /// <summary>Danh sách phân công, lọc theo class/teacher/subject tùy chọn.</summary>
    Task<List<TeacherAssignment>> GetAllAsync(int? classId, int? teacherId, int? subjectId);

    /// <summary>Chi tiết 1 phân công (kèm Teacher, Class, Subject).</summary>
    Task<TeacherAssignment?> GetByIdAsync(int id);

    /// <summary>Kiểm tra đã có phân công trùng (cùng lớp + môn) chưa.</summary>
    Task<TeacherAssignment?> FindByClassAndSubjectAsync(int classId, int subjectId);

    /// <summary>Các phân công của 1 giáo viên.</summary>
    Task<List<TeacherAssignment>> GetByTeacherAsync(int teacherId);

    /// <summary>Thêm phân công.</summary>
    Task<TeacherAssignment> CreateAsync(TeacherAssignment entity);

    /// <summary>Cập nhật phân công.</summary>
    Task UpdateAsync(TeacherAssignment entity);

    /// <summary>Xóa phân công.</summary>
    Task DeleteAsync(TeacherAssignment entity);
}

/// <summary>Implement ITeacherAssignmentRepository bằng EF Core.</summary>
public class TeacherAssignmentRepository : ITeacherAssignmentRepository
{
    private readonly AppDbContext _context;

    public TeacherAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<TeacherAssignment> QueryWithIncludes() =>
        _context.TeacherAssignments
            .Include(ta => ta.Teacher)
            .Include(ta => ta.Class).ThenInclude(c => c.Semester)
            .Include(ta => ta.Subject);

    /// <inheritdoc />
    public async Task<List<TeacherAssignment>> GetAllAsync(int? classId, int? teacherId, int? subjectId)
    {
        var query = QueryWithIncludes().AsQueryable();

        if (classId.HasValue)
            query = query.Where(ta => ta.ClassId == classId.Value);
        if (teacherId.HasValue)
            query = query.Where(ta => ta.TeacherId == teacherId.Value);
        if (subjectId.HasValue)
            query = query.Where(ta => ta.SubjectId == subjectId.Value);

        return await query
            .OrderBy(ta => ta.Class.Name)
            .ThenBy(ta => ta.Subject.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TeacherAssignment?> GetByIdAsync(int id)
    {
        return await QueryWithIncludes().FirstOrDefaultAsync(ta => ta.Id == id);
    }

    /// <inheritdoc />
    public async Task<TeacherAssignment?> FindByClassAndSubjectAsync(int classId, int subjectId)
    {
        return await _context.TeacherAssignments
            .FirstOrDefaultAsync(ta => ta.ClassId == classId && ta.SubjectId == subjectId);
    }

    /// <inheritdoc />
    public async Task<List<TeacherAssignment>> GetByTeacherAsync(int teacherId)
    {
        return await QueryWithIncludes()
            .Where(ta => ta.TeacherId == teacherId)
            .OrderBy(ta => ta.Class.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TeacherAssignment> CreateAsync(TeacherAssignment entity)
    {
        _context.TeacherAssignments.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TeacherAssignment entity)
    {
        _context.TeacherAssignments.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TeacherAssignment entity)
    {
        _context.TeacherAssignments.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
