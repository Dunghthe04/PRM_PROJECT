namespace Api.Models;

/// <summary>
/// Bản ghi điểm danh 1 học sinh trong 1 lớp / 1 ngày (FR3.1).
/// GV chấm P/A/L; HS/PH tra cứu lịch sử chuyên cần.
/// </summary>
public class Attendance
{
    public int Id { get; set; }

    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>Ngày điểm danh (chỉ lấy phần ngày, bỏ giờ).</summary>
    public DateTime Date { get; set; }

    public AttendanceStatus Status { get; set; }

    /// <summary>GV thực hiện điểm danh / cập nhật.</summary>
    public int RecordedByTeacherId { get; set; }
    public User RecordedByTeacher { get; set; } = null!;

    /// <summary>
    /// Id phía client (mobile offline) — tránh trùng khi sync (Ngày 17).
    /// Nullable; unique khi có giá trị.
    /// </summary>
    public string? ClientRecordId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
