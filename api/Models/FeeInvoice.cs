namespace Api.Models;

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
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
}
