namespace Api.Models;

/// <summary>
/// Lớp học thuộc một học kỳ (FR5.2).
/// Tên lớp thường gắn khối (vd. "10A1") — không có entity Khối riêng.
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Class — 1 <see cref="Semester"/> (qua <see cref="SemesterId"/>).</item>
/// <item>1 Class — N <see cref="ClassStudent"/> (danh sách HS trong lớp).</item>
/// <item>1 Class — 0..1 HomeroomTeacher (<see cref="User"/> Role=Teacher) — GV chủ nhiệm.</item>
/// <item>Được tham chiếu bởi: TimetableSlot, TeacherAssignment, Attendance, Grade, LeaveRequest, Announcement…</item>
/// </list>
/// GV bộ môn gắn qua <see cref="TeacherAssignment"/> (lớp + môn), không dùng field này.
/// </para>
/// </summary>
public class Class
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tên lớp hiển thị (vd. 10A1, 11B2).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>FK → <see cref="Semester"/> — học kỳ mà lớp thuộc về.</summary>
    public int SemesterId { get; set; }

    /// <summary>Navigation: học kỳ của lớp (nhiều lớp cùng một kỳ).</summary>
    public Semester Semester { get; set; } = null!;

    /// <summary>
    /// FK → <see cref="User"/> (Role = Teacher) — giáo viên chủ nhiệm lớp.
    /// Null = chưa gán. Phân biệt với GV bộ môn (<see cref="TeacherAssignment"/>).
    /// </summary>
    public int? HomeroomTeacherId { get; set; }

    /// <summary>Navigation: GV chủ nhiệm (nullable).</summary>
    public User? HomeroomTeacher { get; set; }

    /// <summary>
    /// Navigation: danh sách học sinh trong lớp
    /// (bảng trung gian <see cref="ClassStudent"/>).
    /// </summary>
    public ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
}
