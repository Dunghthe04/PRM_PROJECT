namespace Api.Models;

/// <summary>
/// Hóa đơn khoản thu gán cho 1 học sinh (FR4.1, FR2.6).
/// PH thanh toán qua VNPay/PayOS; không lưu thông tin thẻ (NFR4.3).
/// </summary>
public class FeeInvoice
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int FeeCategoryId { get; set; }
    public FeeCategory FeeCategory { get; set; } = null!;

    /// <summary>Số tiền phải thu — decimal(18,2).</summary>
    public decimal Amount { get; set; }

    public DateTime DueDate { get; set; }

    /// <summary>Pending → Paid / Failed / Cancelled.</summary>
    public FeePaymentStatus Status { get; set; } = FeePaymentStatus.Pending;

    /// <summary>Tương thích cũ: true khi Status = Paid.</summary>
    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>VNPay | PayOS | Manual — null khi chưa thanh toán.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Mã giao dịch từ cổng thanh toán (đối soát webhook).</summary>
    public string? TransactionId { get; set; }

    /// <summary>Số biên lai điện tử (sinh khi Paid).</summary>
    public string? ReceiptNumber { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
