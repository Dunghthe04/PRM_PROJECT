namespace Api.DTOs;

/// <summary>
/// DTOs báo cáo & thống kê (FR5.5) — trả JSON cho màn Admin.
/// </summary>

// ─── Query chung ────────────────────────────────────────────────────────────

/// <summary>Tham số lọc báo cáo theo lớp / kỳ / khoảng ngày.</summary>
public class ReportQueryDto
{
    /// <summary>Lọc theo lớp (bắt buộc với grades/attendance/fees chi tiết).</summary>
    public int? ClassId { get; set; }

    /// <summary>Lọc điểm theo học kỳ (tuỳ chọn).</summary>
    public int? SemesterId { get; set; }

    /// <summary>Lọc điểm theo môn (tuỳ chọn).</summary>
    public int? SubjectId { get; set; }

    /// <summary>Khoảng ngày cho chuyên cần (mặc định: 30 ngày gần nhất).</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// ─── Dashboard tổng quan ────────────────────────────────────────────────────

/// <summary>GET /api/reports/dashboard — số liệu tổng quan Admin / Trưởng BM.</summary>
public class ReportDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalClasses { get; set; }
    public int TotalParents { get; set; }

    /// <summary>Số bản ghi điểm đã Published (toàn trường hoặc theo classId nếu lọc).</summary>
    public int PublishedGradeCount { get; set; }

    /// <summary>Tỷ lệ chuyên cần trung bình % (Present+Late)/tổng — null nếu chưa có dữ liệu.</summary>
    public double? AverageAttendanceRate { get; set; }

    /// <summary>Tổng hóa đơn Pending / Paid / số tiền đã thu.</summary>
    public int PendingInvoiceCount { get; set; }
    public int PaidInvoiceCount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }

    public DateTime GeneratedAt { get; set; }
}

// ─── Báo cáo bảng điểm ──────────────────────────────────────────────────────

/// <summary>Báo cáo bảng điểm 1 lớp (chỉ điểm Published).</summary>
public class GradeReportDto
{
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }
    public int? SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectName { get; set; }

    public List<GradeReportRowDto> Rows { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>1 dòng điểm trong báo cáo.</summary>
public class GradeReportRowDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime? PublishedAt { get; set; }
}

// ─── Báo cáo chuyên cần ─────────────────────────────────────────────────────

/// <summary>Báo cáo chuyên cần theo lớp + khoảng ngày.</summary>
public class AttendanceReportDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    /// <summary>Tỷ lệ chuyên cần cả lớp %.</summary>
    public double ClassAttendanceRate { get; set; }

    public List<AttendanceReportRowDto> Rows { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>1 học sinh trong báo cáo chuyên cần.</summary>
public class AttendanceReportRowDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int TotalSessions { get; set; }
    /// <summary>(Present + Late) / Total × 100.</summary>
    public double AttendanceRate { get; set; }
}

// ─── Báo cáo học phí ────────────────────────────────────────────────────────

/// <summary>Báo cáo tình trạng đóng học phí (Admin).</summary>
public class FeeReportDto
{
    public int? ClassId { get; set; }
    public string? ClassName { get; set; }

    public int TotalInvoices { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedOrCancelledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }

    /// <summary>% số hóa đơn đã Paid.</summary>
    public double PaidRate { get; set; }

    public List<FeeReportRowDto> Rows { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>1 hóa đơn trong báo cáo học phí.</summary>
public class FeeReportRowDto
{
    public int InvoiceId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string FeeCategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReceiptNumber { get; set; }
}
