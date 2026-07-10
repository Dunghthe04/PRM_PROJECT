namespace Api.DTOs;

/// <summary>Một tiết TKB trả về client.</summary>
public class TimetableSlotDto
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    /// <summary>1 = Thứ Hai … 7 = Chủ Nhật.</summary>
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int Period { get; set; }
    public string Room { get; set; } = string.Empty;
}

/// <summary>Body tạo / sửa một tiết TKB.</summary>
public class CreateUpdateTimetableSlotDto
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int TeacherId { get; set; }
    /// <summary>1 = Thứ Hai … 7 = Chủ Nhật.</summary>
    public int DayOfWeek { get; set; }
    public int Period { get; set; }
    public string Room { get; set; } = string.Empty;
}

/// <summary>
/// TKB theo tuần: danh sách tiết + khoảng ngày tuần (tính từ weekStart).
/// Slot lặp hàng tuần theo DayOfWeek — weekStart chỉ để client biết đang xem tuần nào.
/// </summary>
public class WeeklyTimetableDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public List<TimetableSlotDto> Slots { get; set; } = new();
}
