namespace Api.Models;

/// <summary>Trạng thái hóa đơn / giao dịch học phí (FR4.2).</summary>
public enum FeePaymentStatus
{
    /// <summary>Chờ thanh toán.</summary>
    Pending = 0,

    /// <summary>Đã thanh toán thành công.</summary>
    Paid = 1,

    /// <summary>Thanh toán thất bại / bị hủy phía cổng.</summary>
    Failed = 2,

    /// <summary>Admin hủy hóa đơn.</summary>
    Cancelled = 3
}
