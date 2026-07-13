namespace Api.Models;

/// <summary>
/// Đơn xin nghỉ học (FR2.5, FR3.3).
/// HS hoặc PH tạo → GV duyệt/từ chối → thông báo kết quả.
/// </summary>
public class LeaveRequest
{
    public int Id { get; set; }

    /// <summary>Lớp học sinh xin nghỉ (lấy từ ClassStudent khi tạo đơn).</summary>
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>HS tự tạo hoặc PH tạo hộ — lưu để audit.</summary>
    public int SubmittedByUserId { get; set; }
    public User SubmittedBy { get; set; } = null!;

    /// <summary>Ngày xin nghỉ (chỉ lấy phần ngày).</summary>
    public DateTime Date { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>URL ảnh giấy xác nhận y tế (upload qua /api/files/upload).</summary>
    public string? MedicalCertificateUrl { get; set; }

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    public int? ApprovedByTeacherId { get; set; }
    public User? ApprovedByTeacher { get; set; }

    /// <summary>Lý do từ chối (khi Status = Rejected).</summary>
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
