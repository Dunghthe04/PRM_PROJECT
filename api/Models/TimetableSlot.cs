namespace Api.Models;

/// <summary>
/// Một tiết trong thời khóa biểu tuần (FR2.3).
/// Lặp theo tuần: Thứ + Tiết + Lớp (+ Môn, GV, Phòng).
/// Unique: (<see cref="ClassId"/>, <see cref="DayOfWeek"/>, <see cref="Period"/>).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N TimetableSlot — 1 <see cref="Class"/>.</item>
/// <item>N TimetableSlot — 1 <see cref="Subject"/>.</item>
/// <item>N TimetableSlot — 1 Teacher (<see cref="User"/>).</item>
/// </list>
/// </para>
/// </summary>
public class TimetableSlot
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Class"/> — lớp có tiết học này.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="Subject"/> — môn của tiết.</summary>
    public int SubjectId { get; set; }

    /// <summary>Navigation: môn học.</summary>
    public Subject Subject { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Teacher) — giáo viên đứng lớp.</summary>
    public int TeacherId { get; set; }

    /// <summary>Navigation: giáo viên dạy tiết.</summary>
    public User Teacher { get; set; } = null!;

    /// <summary>Thứ trong tuần: 1 = Thứ Hai … 7 = Chủ Nhật (ISO).</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Số tiết trong ngày (1, 2, 3, …).</summary>
    public int Period { get; set; }

    /// <summary>Phòng học (vd. A101, Lab-2).</summary>
    public string Room { get; set; } = string.Empty;
}
