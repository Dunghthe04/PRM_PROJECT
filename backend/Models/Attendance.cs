namespace Api.Models;

/// <summary>
/// Bản ghi điểm danh 1 học sinh trong 1 lớp / 1 ngày (FR3.1).
/// GV chấm Present / Absent / Late; HS/PH tra cứu lịch sử chuyên cần.
/// Unique nghiệp vụ: (<see cref="ClassId"/>, <see cref="StudentId"/>, <see cref="Date"/>).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N Attendance — 1 <see cref="Class"/>.</item>
/// <item>N Attendance — 1 Student (<see cref="User"/>).</item>
/// <item>N Attendance — 1 RecordedByTeacher (<see cref="User"/>) — GV ghi nhận.</item>
/// </list>
/// </para>
/// </summary>
public class Attendance
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Class"/> — lớp được điểm danh.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Student) — học sinh được điểm danh.</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh.</summary>
    public User Student { get; set; } = null!;

    /// <summary>Ngày điểm danh (chỉ lấy phần ngày, bỏ giờ).</summary>
    public DateTime Date { get; set; }

    /// <summary>Trạng thái P/A/L — xem <see cref="AttendanceStatus"/>.</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>FK → <see cref="User"/> (Role = Teacher) — GV thực hiện điểm danh / cập nhật.</summary>
    public int RecordedByTeacherId { get; set; }

    /// <summary>Navigation: giáo viên ghi nhận.</summary>
    public User RecordedByTeacher { get; set; } = null!;

    /// <summary>
    /// Id phía client (di sản offline sync — hiện app online-only).
    /// Nullable; unique khi có giá trị.
    /// </summary>
    public string? ClientRecordId { get; set; }

    /// <summary>Thời điểm tạo (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC) — null nếu chưa sửa.</summary>
    public DateTime? UpdatedAt { get; set; }
}
