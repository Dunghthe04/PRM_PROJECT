namespace Api.DTOs;

// ─── FeeCategory (loại khoản thu) ───────────────────────────────────────────

/// <summary>DTO loại phí trả về client.</summary>
public class FeeCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Body tạo / sửa loại phí (Admin).</summary>
public class CreateUpdateFeeCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─── FeeInvoice (hóa đơn) ───────────────────────────────────────────────────

/// <summary>DTO hóa đơn khoản thu.</summary>
public class FeeInvoiceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;

    public int FeeCategoryId { get; set; }
    public string FeeCategoryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Body Admin tạo hóa đơn cho 1 học sinh.</summary>
public class CreateFeeInvoiceDto
{
    public int StudentId { get; set; }
    public int FeeCategoryId { get; set; }
    /// <summary>Null = dùng DefaultAmount của loại phí.</summary>
    public decimal? Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
}

/// <summary>Body Admin gán phí hàng loạt theo lớp.</summary>
public class BatchCreateFeeInvoiceDto
{
    public int ClassId { get; set; }
    public int FeeCategoryId { get; set; }
    public decimal? Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
}

/// <summary>Kết quả tạo hàng loạt.</summary>
public class BatchCreateFeeInvoiceResultDto
{
    public int CreatedCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FeeInvoiceDto> Invoices { get; set; } = new();
}

/// <summary>Query lọc hóa đơn (Admin).</summary>
public class FeeInvoiceListQueryDto
{
    public int? StudentId { get; set; }
    public bool? IsPaid { get; set; }
    public string? Status { get; set; }
}

/// <summary>Biên lai điện tử sau khi Paid (FR2.6).</summary>
public class FeeReceiptDto
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FeeCategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime PaidAt { get; set; }
}

// ─── Payment (giao dịch / cấu hình cổng) ────────────────────────────────────

/// <summary>DTO giao dịch thanh toán.</summary>
public class PaymentTransactionDto
{
    public int Id { get; set; }
    public int FeeInvoiceId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Body PH tạo thanh toán VNPay / PayOS.</summary>
public class CreatePaymentDto
{
    public int FeeInvoiceId { get; set; }
}

/// <summary>Response chứa link checkout.</summary>
public class CreatePaymentResultDto
{
    public string Provider { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>Cấu hình cổng (Admin) — secret có thể mask khi GET.</summary>
public class PaymentGatewayConfigDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Body Admin cập nhật API key VNPay/PayOS.</summary>
public class UpdatePaymentGatewayConfigDto
{
    public string Provider { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string ConfigJson { get; set; } = "{}";
}
