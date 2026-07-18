namespace Api.Models;

/// <summary>
/// Học kỳ / kỳ học (FR5.2).
/// Dùng để lọc lớp, thời khóa biểu, bảng điểm theo khoảng thời gian.
/// <para>
/// Quan hệ: 1 Semester — N <see cref="Class"/> (không khai báo collection ở entity này;
/// truy vấn ngược qua <c>Classes.Where(c =&gt; c.SemesterId == Id)</c>).
/// </para>
/// </summary>
public class Semester
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tên kỳ (vd. "HK1 2025-2026").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Ngày bắt đầu kỳ (phần ngày).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Ngày kết thúc kỳ (phần ngày).</summary>
    public DateTime EndDate { get; set; }
}
