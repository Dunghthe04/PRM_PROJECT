namespace Api.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? MedicalCertificateUrl { get; set; }

    public LeaveRequestStatus Status { get; set; }

    public int? ApprovedByTeacherId { get; set; }
    public User? ApprovedByTeacher { get; set; }
}
