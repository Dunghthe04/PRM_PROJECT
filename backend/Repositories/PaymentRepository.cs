using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>
/// Truy cập PaymentTransactions + PaymentGatewayConfigs (FR4.2).
/// </summary>
public interface IPaymentRepository
{
    /// <summary>Chi tiết 1 giao dịch theo Id.</summary>
    Task<PaymentTransaction?> GetTransactionByIdAsync(int id);

    /// <summary>Tìm giao dịch theo OrderCode nội bộ (dùng khi webhook/return URL).</summary>
    Task<PaymentTransaction?> GetTransactionByOrderCodeAsync(string orderCode);

    /// <summary>Lịch sử giao dịch theo danh sách hóa đơn.</summary>
    Task<List<PaymentTransaction>> GetHistoryByInvoiceIdsAsync(IEnumerable<int> invoiceIds);

    /// <summary>Tạo bản ghi giao dịch mới (khi PH bấm thanh toán).</summary>
    Task<PaymentTransaction> CreateTransactionAsync(PaymentTransaction entity);

    /// <summary>Cập nhật giao dịch (status, ProviderTransactionId, RawCallback...).</summary>
    Task UpdateTransactionAsync(PaymentTransaction entity);

    /// <summary>DS cấu hình mọi cổng (VNPay, PayOS).</summary>
    Task<List<PaymentGatewayConfig>> GetAllConfigsAsync();

    /// <summary>Lấy cấu hình 1 cổng theo tên Provider.</summary>
    Task<PaymentGatewayConfig?> GetConfigByProviderAsync(string provider);

    /// <summary>
    /// Upsert cấu hình cổng: Provider đã có → cập nhật; chưa có → thêm mới.
    /// Tránh trùng bản ghi khi Admin lưu lại API key.
    /// </summary>
    Task<PaymentGatewayConfig> UpsertConfigAsync(PaymentGatewayConfig entity);
}

/// <summary>Implement IPaymentRepository bằng EF Core.</summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<PaymentTransaction?> GetTransactionByIdAsync(int id)
        => await _context.PaymentTransactions
            .Include(t => t.FeeInvoice)
            .FirstOrDefaultAsync(t => t.Id == id);

    /// <inheritdoc />
    public async Task<PaymentTransaction?> GetTransactionByOrderCodeAsync(string orderCode)
        => await _context.PaymentTransactions
            .Include(t => t.FeeInvoice).ThenInclude(i => i.FeeCategory)
            .Include(t => t.FeeInvoice).ThenInclude(i => i.Student)
            .FirstOrDefaultAsync(t => t.OrderCode == orderCode);

    /// <inheritdoc />
    public async Task<List<PaymentTransaction>> GetHistoryByInvoiceIdsAsync(IEnumerable<int> invoiceIds)
    {
        var ids = invoiceIds.ToList();
        return await _context.PaymentTransactions
            .Include(t => t.FeeInvoice)
            .Where(t => ids.Contains(t.FeeInvoiceId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<PaymentTransaction> CreateTransactionAsync(PaymentTransaction entity)
    {
        _context.PaymentTransactions.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateTransactionAsync(PaymentTransaction entity)
    {
        _context.PaymentTransactions.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<PaymentGatewayConfig>> GetAllConfigsAsync()
        => await _context.PaymentGatewayConfigs.OrderBy(c => c.Provider).ToListAsync();

    /// <inheritdoc />
    public async Task<PaymentGatewayConfig?> GetConfigByProviderAsync(string provider)
        => await _context.PaymentGatewayConfigs
            .FirstOrDefaultAsync(c => c.Provider == provider);

    /// <summary>
    /// Upsert = Update + Insert.
    /// - Đã có config cùng Provider (vd: "VNPay") → ghi đè IsEnabled / ConfigJson.
    /// - Chưa có → Add bản ghi mới.
    /// Mục đích: Admin bấm "Lưu cấu hình" nhiều lần không tạo trùng dòng.
    /// </summary>
    public async Task<PaymentGatewayConfig> UpsertConfigAsync(PaymentGatewayConfig entity)
    {
        var existing = await GetConfigByProviderAsync(entity.Provider);
        if (existing != null)
        {
            // Update: giữ Id cũ, chỉ đổi nội dung cấu hình
            existing.IsEnabled = entity.IsEnabled;
            existing.ConfigJson = entity.ConfigJson;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.PaymentGatewayConfigs.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        // Insert: cổng chưa từng cấu hình
        _context.PaymentGatewayConfigs.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}
