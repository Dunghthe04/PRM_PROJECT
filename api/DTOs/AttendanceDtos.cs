using Api.Models;

namespace Api.DTOs;

// ─── FR3.1 — Điểm danh (Ngày 7 Bước 2) ─────────────────────────────────────
//
// DTO định nghĩa dữ liệu vào/ra cho API điểm danh P/A/L.
// Luồng: GV batch điểm danh → HS/PH xem lịch sử chuyên cần.

/// <summary>Thông tin 1 bản ghi điểm danh trả về client.</summary>
public class AttendanceDto
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;

    /// <summary>Ngày điểm danh (yyyy-MM-dd).</summary>
    public DateTime Date { get; set; }

    /// <summary>Present / Absent / Late.</summary>
    public string Status { get; set; } = string.Empty;

    public int RecordedByTeacherId { get; set; }
    public string RecordedByTeacherName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Query GET /api/attendance — GV xem điểm danh lớp theo ngày.</summary>
public class AttendanceListQueryDto
{
    public int ClassId { get; set; }

    /// <summary>Ngày cần xem (vd: 2026-07-11). Mặc định hôm nay nếu không truyền.</summary>
    public DateTime? Date { get; set; }
}

/// <summary>Query GET /api/attendance/me — HS/PH xem lịch sử chuyên cần.</summary>
public class MyAttendanceQueryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>Query GET /api/attendance/summary — thống kê chuyên cần lớp.</summary>
public class AttendanceSummaryQueryDto
{
    public int ClassId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>Tỷ lệ chuyên cần của 1 học sinh trong lớp.</summary>
public class StudentAttendanceSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }

    /// <summary>Tỷ lệ có mặt + đi muộn (%).</summary>
    public double AttendanceRate { get; set; }
}

/// <summary>Response GET /api/attendance/summary.</summary>
public class AttendanceSummaryDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<StudentAttendanceSummaryDto> Students { get; set; } = new();
}

/// <summary>
/// Body POST /api/attendance/batch — điểm danh hàng loạt 1 lớp / 1 ngày.
/// </summary>
public class BatchAttendanceDto
{
    public int ClassId { get; set; }
    public DateTime Date { get; set; }
    public List<BatchAttendanceItemDto> Entries { get; set; } = new();
}

/// <summary>1 HS + trạng thái P/A/L trong batch.</summary>
public class BatchAttendanceItemDto
{
    public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
}

/// <summary>Body PUT /api/attendance/{id} — sửa trạng thái 1 bản ghi.</summary>
public class UpdateAttendanceDto
{
    public AttendanceStatus Status { get; set; }
}

/// <summary>Response sau POST /api/attendance/batch.</summary>
public class BatchAttendanceResultDto
{
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<AttendanceDto> Records { get; set; } = new();
}
