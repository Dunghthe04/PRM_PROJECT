using Api.Models;

namespace Api.DTOs;

// ─── FR3.2 / FR2.3 — Điểm số (Ngày 6 Bước 2) ───────────────────────────────
//
// DTO định nghĩa dữ liệu vào/ra cho API điểm số.
// Luồng chính: GV nhập batch (Draft) → Publish → HS/PH xem /grades/me.

/// <summary>Thông tin 1 bản ghi điểm trả về client.</summary>
public class GradeDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;

    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;

    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;

    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;

    public string AssessmentType { get; set; } = string.Empty;
    public double Score { get; set; }

    /// <summary>Draft hoặc Published — string để client dễ đọc.</summary>
    public string Status { get; set; } = string.Empty;

    public int CreatedByTeacherId { get; set; }
    public string CreatedByTeacherName { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Query GET /api/grades — GV xem bảng điểm lớp.
/// Bắt buộc classId + subjectId + semesterId; assessmentType tuỳ chọn.
/// </summary>
public class GradeListQueryDto
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }

    /// <summary>Lọc theo đầu điểm (Midterm, Final...) — null = tất cả.</summary>
    public string? AssessmentType { get; set; }

    /// <summary>Lọc Draft / Published — null = tất cả (GV thấy cả nháp).</summary>
    public GradeStatus? Status { get; set; }
}

/// <summary>Query GET /api/grades/me — HS/PH xem điểm đã công bố.</summary>
public class MyGradesQueryDto
{
    /// <summary>Lọc theo kỳ — null = tất cả kỳ.</summary>
    public int? SemesterId { get; set; }
}

/// <summary>
/// Body POST /api/grades/batch — nhập điểm hàng loạt (lưu Nháp).
/// GV chọn lớp + môn + kỳ + loại điểm, rồi gửi danh sách điểm từng HS.
/// </summary>
public class BatchGradeDto
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }

    /// <summary>Loại đầu điểm: Midterm, Final, Oral, Quiz15...</summary>
    public string AssessmentType { get; set; } = string.Empty;

    /// <summary>Danh sách điểm từng học sinh trong lớp.</summary>
    public List<BatchGradeItemDto> Entries { get; set; } = new();
}

/// <summary>Một dòng điểm trong batch — 1 học sinh + điểm số.</summary>
public class BatchGradeItemDto
{
    public int StudentId { get; set; }
    public double Score { get; set; }
}

/// <summary>Body PUT /api/grades/{id} — sửa điểm 1 HS (chỉ khi còn Draft).</summary>
public class UpdateGradeDto
{
    public double Score { get; set; }
}

/// <summary>
/// Body POST /api/grades/publish — công bố toàn bộ điểm Nháp
/// của 1 lớp + môn + kỳ + đầu điểm.
/// </summary>
public class PublishGradesDto
{
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
}

/// <summary>Response sau khi công bố điểm.</summary>
public class PublishGradesResultDto
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Số bản ghi chuyển từ Draft → Published.</summary>
    public int PublishedCount { get; set; }
}

/// <summary>Response sau POST /api/grades/batch.</summary>
public class BatchGradeResultDto
{
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<GradeDto> Grades { get; set; } = new();
}
