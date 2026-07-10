using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng Subjects (môn học).</summary>
public interface ISubjectRepository
{
    /// <summary>Lấy tất cả môn, sắp theo tên.</summary>
    Task<List<Subject>> GetAllAsync();

    /// <summary>Tìm môn theo Id.</summary>
    Task<Subject?> GetByIdAsync(int id);

    /// <summary>Tìm môn theo mã (Code) — dùng kiểm tra trùng.</summary>
    Task<Subject?> GetByCodeAsync(string code);

    /// <summary>Thêm môn mới.</summary>
    Task<Subject> CreateAsync(Subject subject);

    /// <summary>Cập nhật môn.</summary>
    Task UpdateAsync(Subject subject);

    /// <summary>Xóa môn.</summary>
    Task DeleteAsync(Subject subject);
}

/// <summary>Implement ISubjectRepository bằng EF Core.</summary>
public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _context;

    public SubjectRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Subject>> GetAllAsync()
    {
        return await _context.Subjects
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Subject?> GetByIdAsync(int id)
    {
        return await _context.Subjects.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Subject?> GetByCodeAsync(string code)
    {
        var key = code.Trim().ToUpperInvariant();
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.Code.ToUpper() == key);
    }

    /// <inheritdoc />
    public async Task<Subject> CreateAsync(Subject subject)
    {
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Subject subject)
    {
        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
    }
}
