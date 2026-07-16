namespace Api.Models;

/// <summary>
/// Phân công giảng dạy: một Giáo viên dạy một Môn cho một Lớp (FR5.3).
/// Dùng để kiểm tra quyền điểm danh, duyệt đơn nghỉ, gửi thông báo lớp.
/// <para>
/// Quan hệ (1 bản ghi gắn 3 phía):
/// <list type="bullet">
/// <item>N TeacherAssignment — 1 Teacher (<see cref="User"/>, Role=Teacher).</item>
/// <item>N TeacherAssignment — 1 <see cref="Class"/>.</item>
/// <item>N TeacherAssignment — 1 <see cref="Subject"/>.</item>
/// </list>
/// Không cascade delete User/Class/Subject (Restrict).
/// </para>
/// </summary>
public class TeacherAssignment
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="User"/> (Role = Teacher) — giáo viên được phân công.</summary>
    public int TeacherId { get; set; }

    /// <summary>Navigation: giáo viên.</summary>
    public User Teacher { get; set; } = null!;

    /// <summary>FK → <see cref="Class"/> — lớp được phân công dạy.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="Subject"/> — môn được phân công dạy.</summary>
    public int SubjectId { get; set; }

    /// <summary>Navigation: môn học.</summary>
    public Subject Subject { get; set; } = null!;
}
