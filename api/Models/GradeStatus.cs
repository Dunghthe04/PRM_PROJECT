namespace Api.Models;

/// <summary>
/// Trạng thái điểm (FR5.6 — Nháp → Công bố).
/// Draft: chỉ người nhập thấy; Published: HS/PH được xem + nhận thông báo.
/// </summary>
public enum GradeStatus
{
    /// <summary>Nháp — chưa công bố cho HS/PH.</summary>
    Draft,

    /// <summary>Đã công bố — hiển thị trên bảng điểm.</summary>
    Published
}
