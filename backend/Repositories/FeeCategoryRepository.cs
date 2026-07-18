using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng FeeCategories (loại khoản thu — FR4.1).</summary>
public interface IFeeCategoryRepository
{
    /// <summary>Danh sách loại phí; lọc IsActive nếu truyền.</summary>
    Task<List<FeeCategory>> GetAllAsync(bool? isActive = null);

    /// <summary>Chi tiết 1 loại phí theo Id.</summary>
    Task<FeeCategory?> GetByIdAsync(int id);

    /// <summary>Thêm loại phí mới.</summary>
    Task<FeeCategory> CreateAsync(FeeCategory entity);

    /// <summary>Cập nhật loại phí.</summary>
    Task UpdateAsync(FeeCategory entity);

    /// <summary>Xóa loại phí.</summary>
    Task DeleteAsync(FeeCategory entity);
}

/// <summary>Implement IFeeCategoryRepository bằng EF Core.</summary>
public class FeeCategoryRepository : IFeeCategoryRepository
{
    private readonly AppDbContext _context;

    public FeeCategoryRepository(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<List<FeeCategory>> GetAllAsync(bool? isActive = null)
    {
        var query = _context.FeeCategories.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeeCategory?> GetByIdAsync(int id)
        => await _context.FeeCategories.FindAsync(id);

    /// <inheritdoc />
    public async Task<FeeCategory> CreateAsync(FeeCategory entity)
    {
        _context.FeeCategories.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FeeCategory entity)
    {
        _context.FeeCategories.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(FeeCategory entity)
    {
        _context.FeeCategories.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
