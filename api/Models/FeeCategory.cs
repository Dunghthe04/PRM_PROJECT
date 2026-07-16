namespace Api.Models;

/// <summary>
/// Loại khoản thu (học phí, BHYT, phụ phí…) — FR4.1.
/// Admin cấu hình mức mặc định; hóa đơn có thể ghi đè Amount.
/// <para>
/// Quan hệ: 1 FeeCategory — N <see cref="FeeInvoice"/> (không khai báo collection;
/// truy vấn ngược qua FeeInvoice.FeeCategoryId).
/// </para>
/// </summary>
public class FeeCategory
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tên loại khoản thu.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn (tuỳ chọn).</summary>
    public string? Description { get; set; }

    /// <summary>Số tiền mặc định khi tạo hóa đơn — precision decimal(18,2).</summary>
    public decimal DefaultAmount { get; set; }

    /// <summary>False = ẩn khỏi danh sách tạo hóa đơn mới.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
