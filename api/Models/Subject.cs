namespace Api.Models;

/// <summary>
/// Môn học trong danh mục nhà trường (FR5.2).
/// <para>
/// Quan hệ: được tham chiếu bởi
/// <see cref="TeacherAssignment"/>, <see cref="TimetableSlot"/>,
/// <see cref="Grade"/>, <see cref="Assignment"/>, <see cref="Announcement"/> (tuỳ chọn).
/// Không giữ collection navigation — tránh vòng Include nặng.
/// </para>
/// </summary>
public class Subject
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tên môn (vd. Toán, Ngữ văn).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã môn ngắn (vd. TOAN, NV) — unique nghiệp vụ ở tầng Service.</summary>
    public string Code { get; set; } = string.Empty;
}
