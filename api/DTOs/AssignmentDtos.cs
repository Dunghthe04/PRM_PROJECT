namespace Api.DTOs;

// ─── Assignment (bài tập) ───────────────────────────────────────────────────

/// <summary>DTO trả về thông tin bài tập.</summary>
public class AssignmentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public double MaxScore { get; set; }
    public string? AttachmentUrl { get; set; }

    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;

    public int CreatedByTeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Số bài đã nộp (GV xem danh sách).</summary>
    public int SubmissionCount { get; set; }

    /// <summary>
    /// Trạng thái phía HS: ToDo / Done / Overdue (null khi GV xem DS lớp).
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>Body tạo / sửa bài tập (GV).</summary>
public class CreateUpdateAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public double MaxScore { get; set; } = 10;
    public string? AttachmentUrl { get; set; }
    public int ClassId { get; set; }
    public int SubjectId { get; set; }
}

/// <summary>Query lọc danh sách bài tập theo lớp / môn.</summary>
public class AssignmentListQueryDto
{
    public int? ClassId { get; set; }
    public int? SubjectId { get; set; }
}

/// <summary>Query bài tập của HS — lọc theo status tùy chọn.</summary>
public class MyAssignmentsQueryDto
{
    /// <summary>ToDo | Done | Overdue — null = tất cả.</summary>
    public string? Status { get; set; }
}

// ─── Submission (bài nộp) ───────────────────────────────────────────────────

/// <summary>DTO trả về bài nộp.</summary>
public class SubmissionDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }
    public string? FileUrl { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public double? Score { get; set; }
    public string? Feedback { get; set; }
    public int? GradedByTeacherId { get; set; }
    public string? GradedByTeacherName { get; set; }
    public DateTime? GradedAt { get; set; }

    /// <summary>True nếu nộp sau DueDate.</summary>
    public bool IsLate { get; set; }
}

/// <summary>Body HS nộp bài / nộp lại (link và/hoặc file).</summary>
public class SubmitAssignmentDto
{
    public string? LinkUrl { get; set; }
    public string? FileUrl { get; set; }
}

/// <summary>Body GV chấm điểm + feedback.</summary>
public class GradeSubmissionDto
{
    public double Score { get; set; }
    public string? Feedback { get; set; }
}
