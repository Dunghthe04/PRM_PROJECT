namespace Api.Models;

/// <summary>
/// Bảng trung gian N–N giữa Phụ huynh và Học sinh (FR2.1 — Switch Profile).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>1 Parent (<see cref="User"/>, Role=Parent) — nhiều Student.</item>
/// <item>1 Student (<see cref="User"/>, Role=Student) — nhiều Parent.</item>
/// </list>
/// Khóa chính kép: (<see cref="StudentId"/>, <see cref="ParentId"/>).
/// Xóa User không cascade (Restrict) để tránh mất liên kết ngoài ý muốn.
/// </para>
/// </summary>
public class StudentParent
{
    /// <summary>FK → <see cref="User"/> (Role = Parent).</summary>
    public int ParentId { get; set; }

    /// <summary>Navigation: phụ huynh trong cặp liên kết.</summary>
    public User Parent { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Student).</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh trong cặp liên kết.</summary>
    public User Student { get; set; } = null!;
}
