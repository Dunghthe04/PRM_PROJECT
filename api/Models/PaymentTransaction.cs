namespace Api.Models;

/// <summary>
/// Bản ghi giao dịch thanh toán (VNPay / PayOS) gắn 1 hóa đơn — FR4.2.
/// Dùng đối soát webhook; không lưu số thẻ / CVV.
/// </summary>
public class PaymentTransaction
{
    public int Id { get; set; }

    public int FeeInvoiceId { get; set; }
    public FeeInvoice FeeInvoice { get; set; } = null!;

    /// <summary>VNPay | PayOS.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Mã đơn nội bộ gửi sang cổng (duy nhất).</summary>
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>Mã giao dịch phía cổng (null trước khi callback).</summary>
    public string? ProviderTransactionId { get; set; }

    public decimal Amount { get; set; }

    public FeePaymentStatus Status { get; set; } = FeePaymentStatus.Pending;

    /// <summary>URL thanh toán trả về client (checkout).</summary>
    public string? PaymentUrl { get; set; }

    /// <summary>Payload thô từ webhook (debug / đối soát) — không chứa secret thẻ.</summary>
    public string? RawCallback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
