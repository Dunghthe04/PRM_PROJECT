namespace Api.Models;

/// <summary>
/// Bảng trung gian N–N giữa Lớp và Học sinh (FR5.2 — gán HS vào lớp).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>1 <see cref="Class"/> — nhiều Student.</item>
/// <item>1 Student (<see cref="User"/>) — có thể thuộc nhiều lớp (các kỳ khác nhau).</item>
/// </list>
/// Khóa chính kép: (<see cref="ClassId"/>, <see cref="StudentId"/>).
/// Index phụ trên <see cref="StudentId"/> để tra TKB/điểm theo HS nhanh.
/// </para>
/// </summary>
public class ClassStudent
{
    /// <summary>FK → <see cref="Class"/>.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp chứa học sinh.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Student).</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh được gán vào lớp.</summary>
    public User Student { get; set; } = null!;
}
