namespace Api.Models;

/// <summary>
/// Cấu hình cổng thanh toán (Admin — FR4.2).
/// Lưu API key / secret; không lưu thông tin thẻ phụ huynh.
/// </summary>
public class PaymentGatewayConfig
{
    public int Id { get; set; }

    /// <summary>VNPay | PayOS.</summary>
    public string Provider { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    /// <summary>JSON cấu hình (TmnCode, HashSecret, ClientId, ApiKey, ChecksumKey...).</summary>
    public string ConfigJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
