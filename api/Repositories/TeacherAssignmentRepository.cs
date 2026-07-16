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

    /// <summary>GV có được phân công dạy lớp này không.</summary>
    Task<bool> TeacherAssignedToClassAsync(int teacherId, int classId);

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
        // Projection — tránh Include/ThenInclude (lần đầu từng ~8s).
        var rows = await _context.TeacherAssignments
            .AsNoTracking()
            .Where(ta => ta.TeacherId == teacherId)
            .OrderBy(ta => ta.Class.Name)
            .ThenBy(ta => ta.Subject.Name)
            .Select(ta => new
            {
                ta.Id,
                ta.TeacherId,
                ta.ClassId,
                ClassName = ta.Class.Name,
                ta.Class.SemesterId,
                SemesterName = ta.Class.Semester != null ? ta.Class.Semester.Name : null,
                ta.SubjectId,
                SubjectName = ta.Subject.Name,
                SubjectCode = ta.Subject.Code,
            })
            .ToListAsync();

        return rows.Select(r => new TeacherAssignment
        {
            Id = r.Id,
            TeacherId = r.TeacherId,
            ClassId = r.ClassId,
            SubjectId = r.SubjectId,
            Class = new Class
            {
                Id = r.ClassId,
                Name = r.ClassName,
                SemesterId = r.SemesterId,
                Semester = new Semester { Id = r.SemesterId, Name = r.SemesterName ?? "" },
            },
            Subject = new Subject
            {
                Id = r.SubjectId,
                Name = r.SubjectName,
                Code = r.SubjectCode,
            },
        }).ToList();
    }

    /// <summary>Kiểm tra GV có phân công dạy lớp không (nhẹ, không load full list).</summary>
    public Task<bool> TeacherAssignedToClassAsync(int teacherId, int classId)
        => _context.TeacherAssignments.AsNoTracking()
            .AnyAsync(ta => ta.TeacherId == teacherId && ta.ClassId == classId);

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
