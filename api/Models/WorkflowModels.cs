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

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // If Type == Class, specify which class
    public int? TargetClassId { get; set; }
    public Class? TargetClass { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
