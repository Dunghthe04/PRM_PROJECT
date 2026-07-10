namespace Api.Models;

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public double Score { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
}
