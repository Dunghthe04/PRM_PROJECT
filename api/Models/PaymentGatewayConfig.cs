namespace Api.Models;

/// <summary>
/// Cấu hình cổng thanh toán do Admin quản lý (FR4.2).
/// Lưu API key / secret dạng JSON; không lưu thông tin thẻ phụ huynh.
/// <para>Quan hệ: bảng cấu hình độc lập — 1 dòng / Provider (unique index).</para>
/// </summary>
public class PaymentGatewayConfig
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Mã nhà cung cấp: VNPay | PayOS (unique).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>False = tắt cổng trên app (không cho tạo giao dịch mới).</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON cấu hình (TmnCode, HashSecret, ClientId, ApiKey, ChecksumKey…).</summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
