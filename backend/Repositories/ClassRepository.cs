using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu Classes + ClassStudents.</summary>
public interface IClassRepository
{
    /// <summary>Danh sách lớp (lọc theo kỳ nếu có).</summary>
    Task<List<Class>> GetAllAsync(int? semesterId = null);

    /// <summary>Chi tiết lớp kèm Semester + ClassStudents.</summary>
    Task<Class?> GetByIdAsync(int id);

    /// <summary>Thêm lớp mới.</summary>
    Task<Class> CreateAsync(Class entity);

    /// <summary>Cập nhật lớp.</summary>
    Task UpdateAsync(Class entity);

    /// <summary>Xóa lớp.</summary>
    Task DeleteAsync(Class entity);

    /// <summary>DS học sinh trong lớp.</summary>
    Task<List<ClassStudent>> GetStudentsAsync(int classId);

    /// <summary>Kiểm tra HS đã trong lớp chưa.</summary>
    Task<bool> StudentInClassAsync(int classId, int studentId);

    /// <summary>Gán HS vào lớp.</summary>
    Task AddStudentAsync(ClassStudent link);

    /// <summary>Bỏ HS khỏi lớp.</summary>
    Task RemoveStudentAsync(ClassStudent link);

    /// <summary>Tìm liên kết Class–Student.</summary>
    Task<ClassStudent?> GetStudentLinkAsync(int classId, int studentId);
}

/// <summary>Implement IClassRepository bằng EF Core.</summary>
public class ClassRepository : IClassRepository
{
    private readonly AppDbContext _context;

    public ClassRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Class>> GetAllAsync(int? semesterId = null)
    {
        var query = _context.Classes
            .Include(c => c.Semester)
            .Include(c => c.HomeroomTeacher)
            .Include(c => c.ClassStudents)
            .AsQueryable();

        if (semesterId.HasValue)
            query = query.Where(c => c.SemesterId == semesterId.Value);

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Class?> GetByIdAsync(int id)
    {
        return await _context.Classes
            .Include(c => c.Semester)
            .Include(c => c.HomeroomTeacher)
            .Include(c => c.ClassStudents)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<Class> CreateAsync(Class entity)
    {
        _context.Classes.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Class entity)
    {
        _context.Classes.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Class entity)
    {
        _context.Classes.Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<ClassStudent>> GetStudentsAsync(int classId)
    {
        return await _context.ClassStudents
            .Include(cs => cs.Student)
            .Where(cs => cs.ClassId == classId)
            .OrderBy(cs => cs.Student.FullName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> StudentInClassAsync(int classId, int studentId)
    {
        return await _context.ClassStudents
            .AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
    }

    /// <inheritdoc />
    public async Task AddStudentAsync(ClassStudent link)
    {
        _context.ClassStudents.Add(link);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveStudentAsync(ClassStudent link)
    {
        _context.ClassStudents.Remove(link);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ClassStudent?> GetStudentLinkAsync(int classId, int studentId)
    {
        return await _context.ClassStudents
            .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
    }
}
