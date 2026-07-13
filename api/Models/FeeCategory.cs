namespace Api.Models;

/// <summary>
/// Loại khoản thu (học phí, BHYT, phụ phí...) — FR4.1.
/// Admin cấu hình mức mặc định; hóa đơn có thể ghi đè Amount.
/// </summary>
public class FeeCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn (tuỳ chọn).</summary>
    public string? Description { get; set; }

    /// <summary>Số tiền mặc định khi tạo hóa đơn — precision decimal(18,2).</summary>
    public decimal DefaultAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
