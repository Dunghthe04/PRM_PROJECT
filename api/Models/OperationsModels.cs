namespace Api.Models;

public class Attendance
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
}

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int CreatedByTeacherId { get; set; }
    public User CreatedByTeacher { get; set; } = null!;
}

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
    public string AssessmentType { get; set; } = string.Empty; // e.g. "Midterm", "Final"
}
