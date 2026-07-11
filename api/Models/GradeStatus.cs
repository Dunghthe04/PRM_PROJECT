namespace Api.Models;

/// <summary>
/// Trạng thái điểm (FR3.2 — Nháp → Công bố).
/// Draft: GV nhập/sửa, HS/PH chưa thấy.
/// Published: đã công bố, HS/PH được xem + nhận thông báo.
/// </summary>
public enum GradeStatus
{
    Draft,
    Published
}
