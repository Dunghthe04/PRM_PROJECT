namespace Api.Models;

/// <summary>
/// Trạng thái điểm danh (FR3.1): P = Present, A = Absent, L = Late.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>P — Có mặt.</summary>
    Present,

    /// <summary>A — Vắng mặt.</summary>
    Absent,

    /// <summary>L — Đi muộn.</summary>
    Late
}
