using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng Semesters (kỳ học).</summary>
public interface ISemesterRepository
{
    /// <summary>Lấy tất cả kỳ học, mới nhất trước.</summary>
    Task<List<Semester>> GetAllAsync();

    /// <summary>Tìm kỳ học theo Id.</summary>
    Task<Semester?> GetByIdAsync(int id);

    /// <summary>Thêm kỳ học mới.</summary>
    Task<Semester> CreateAsync(Semester semester);

    /// <summary>Cập nhật kỳ học.</summary>
    Task UpdateAsync(Semester semester);

    /// <summary>Xóa kỳ học.</summary>
    Task DeleteAsync(Semester semester);
}

/// <summary>Implement ISemesterRepository bằng EF Core.</summary>
public class SemesterRepository : ISemesterRepository
{
    private readonly AppDbContext _context;

    public SemesterRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Semester>> GetAllAsync()
    {
        return await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Semester?> GetByIdAsync(int id)
    {
        return await _context.Semesters.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Semester> CreateAsync(Semester semester)
    {
        _context.Semesters.Add(semester);
        await _context.SaveChangesAsync();
        return semester;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Semester semester)
    {
        _context.Semesters.Update(semester);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Semester semester)
    {
        _context.Semesters.Remove(semester);
        await _context.SaveChangesAsync();
    }
}
