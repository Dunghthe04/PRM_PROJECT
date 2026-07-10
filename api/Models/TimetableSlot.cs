namespace Api.Models;

/// <summary>
/// Một tiết trong thời khóa biểu tuần (FR2.3).
/// Lặp theo tuần: Thứ + Tiết + Lớp (+ Môn, GV, Phòng).
/// </summary>
public class TimetableSlot
{
    public int Id { get; set; }

    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    /// <summary>
    /// Thứ trong tuần: 1 = Thứ Hai … 7 = Chủ Nhật (ISO).
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>Số tiết trong ngày (1, 2, 3, …).</summary>
    public int Period { get; set; }

    /// <summary>Phòng học (vd: A101, Lab-2).</summary>
    public string Room { get; set; } = string.Empty;
}
