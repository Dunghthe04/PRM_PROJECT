using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng FeeInvoices (hóa đơn khoản thu — FR4.1, FR2.6).</summary>
public interface IFeeInvoiceRepository
{
    /// <summary>DS hóa đơn — lọc theo học sinh / đã trả / trạng thái.</summary>
    Task<List<FeeInvoice>> GetListAsync(int? studentId, bool? isPaid, FeePaymentStatus? status);

    /// <summary>Hóa đơn của nhiều HS (Parent xem nhiều con).</summary>
    Task<List<FeeInvoice>> GetByStudentIdsAsync(IEnumerable<int> studentIds);

    /// <summary>Chi tiết hóa đơn kèm Student + FeeCategory.</summary>
    Task<FeeInvoice?> GetByIdAsync(int id);

    /// <summary>Tạo 1 hóa đơn.</summary>
    Task<FeeInvoice> CreateAsync(FeeInvoice entity);

    /// <summary>Tạo nhiều hóa đơn (gán phí cả lớp).</summary>
    Task<List<FeeInvoice>> CreateManyAsync(IEnumerable<FeeInvoice> entities);

    /// <summary>Cập nhật hóa đơn (vd: đánh dấu Paid sau webhook).</summary>
    Task UpdateAsync(FeeInvoice entity);
}

/// <summary>Implement IFeeInvoiceRepository bằng EF Core.</summary>
public class FeeInvoiceRepository : IFeeInvoiceRepository
{
    private readonly AppDbContext _context;

    public FeeInvoiceRepository(AppDbContext context) => _context = context;

    private IQueryable<FeeInvoice> QueryWithIncludes() =>
        _context.FeeInvoices
            .Include(i => i.Student)
            .Include(i => i.FeeCategory);

    /// <inheritdoc />
    public async Task<List<FeeInvoice>> GetListAsync(int? studentId, bool? isPaid, FeePaymentStatus? status)
    {
        var query = QueryWithIncludes().AsQueryable();

        if (studentId.HasValue)
            query = query.Where(i => i.StudentId == studentId.Value);

        if (isPaid.HasValue)
            query = query.Where(i => i.IsPaid == isPaid.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        return await query
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<FeeInvoice>> GetByStudentIdsAsync(IEnumerable<int> studentIds)
    {
        var ids = studentIds.ToList();
        return await QueryWithIncludes()
            .Where(i => ids.Contains(i.StudentId))
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeeInvoice?> GetByIdAsync(int id)
        => await QueryWithIncludes().FirstOrDefaultAsync(i => i.Id == id);

    /// <inheritdoc />
    public async Task<FeeInvoice> CreateAsync(FeeInvoice entity)
    {
        _context.FeeInvoices.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task<List<FeeInvoice>> CreateManyAsync(IEnumerable<FeeInvoice> entities)
    {
        var list = entities.ToList();
        _context.FeeInvoices.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FeeInvoice entity)
    {
        _context.FeeInvoices.Update(entity);
        await _context.SaveChangesAsync();
    }
}
