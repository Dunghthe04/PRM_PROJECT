namespace Api.Models;

/// <summary>
/// Hóa đơn khoản thu gán cho 1 học sinh (FR4.1, FR2.6).
/// PH thanh toán qua VNPay/PayOS; không lưu thông tin thẻ (NFR4.3).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N FeeInvoice — 1 Student (<see cref="User"/>).</item>
/// <item>N FeeInvoice — 1 <see cref="FeeCategory"/>.</item>
/// <item>1 FeeInvoice — N <see cref="PaymentTransaction"/> (lịch sử giao dịch cổng).</item>
/// </list>
/// </para>
/// </summary>
public class FeeInvoice
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> (Role = Student) — học sinh được thu.</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh.</summary>
    public User Student { get; set; } = null!;

    /// <summary>FK → <see cref="FeeCategory"/> — loại khoản thu.</summary>
    public int FeeCategoryId { get; set; }

    /// <summary>Navigation: loại khoản thu.</summary>
    public FeeCategory FeeCategory { get; set; } = null!;

    /// <summary>Số tiền phải thu — decimal(18,2).</summary>
    public decimal Amount { get; set; }

    /// <summary>Hạn thanh toán.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Pending → Paid / Failed / Cancelled — xem <see cref="FeePaymentStatus"/>.</summary>
    public FeePaymentStatus Status { get; set; } = FeePaymentStatus.Pending;

    /// <summary>Tương thích cũ: true khi Status = Paid.</summary>
    public bool IsPaid { get; set; }

    /// <summary>Thời điểm thanh toán thành công (UTC).</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>VNPay | PayOS | Manual — null khi chưa thanh toán.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Mã giao dịch từ cổng thanh toán (đối soát webhook).</summary>
    public string? TransactionId { get; set; }

    /// <summary>Số biên lai điện tử (sinh khi Paid).</summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>Ghi chú nội bộ (tuỳ chọn).</summary>
    public string? Note { get; set; }

    /// <summary>Thời điểm tạo hóa đơn (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Navigation: các lần tạo giao dịch cổng gắn hóa đơn này (1–N).</summary>
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
