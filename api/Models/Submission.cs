namespace Api.Models;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public string FileUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public double? Score { get; set; }
    public string? Feedback { get; set; }
}
