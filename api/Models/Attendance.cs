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
