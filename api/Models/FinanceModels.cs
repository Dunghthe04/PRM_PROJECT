namespace Api.Models;

public class FeeCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g. "Học phí", "BHYT"
    public decimal DefaultAmount { get; set; }
}

public class FeeInvoice
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int FeeCategoryId { get; set; }
    public FeeCategory FeeCategory { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; } // "VNPay", "PayOS"
    public string? TransactionId { get; set; }
}
