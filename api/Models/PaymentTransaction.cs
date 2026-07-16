namespace Api.Models;

/// <summary>
/// Bản ghi giao dịch thanh toán (VNPay / PayOS) gắn 1 hóa đơn — FR4.2.
/// Dùng đối soát webhook; không lưu số thẻ / CVV.
/// <para>
/// Quan hệ: N PaymentTransaction — 1 <see cref="FeeInvoice"/>
/// (cascade khi xóa hóa đơn). <see cref="OrderCode"/> unique toàn hệ thống.
/// </para>
/// </summary>
public class PaymentTransaction
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="FeeInvoice"/> — hóa đơn được thanh toán.</summary>
    public int FeeInvoiceId { get; set; }

    /// <summary>Navigation: hóa đơn cha.</summary>
    public FeeInvoice FeeInvoice { get; set; } = null!;

    /// <summary>Nhà cung cấp cổng: VNPay | PayOS.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Mã đơn nội bộ gửi sang cổng (duy nhất).</summary>
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>Mã giao dịch phía cổng — null trước khi callback.</summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>Số tiền giao dịch — decimal(18,2).</summary>
    public decimal Amount { get; set; }

    /// <summary>Trạng thái giao dịch — xem <see cref="FeePaymentStatus"/>.</summary>
    public FeePaymentStatus Status { get; set; } = FeePaymentStatus.Pending;

    /// <summary>URL thanh toán trả về client (checkout).</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Payload thô từ webhook (debug / đối soát) — không chứa secret thẻ.</summary>
    public string? RawCallback { get; set; }

    /// <summary>Thời điểm tạo giao dịch (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
