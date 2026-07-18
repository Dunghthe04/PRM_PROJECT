namespace Api.Models;

/// <summary>
/// Đơn xin nghỉ học (FR2.5, FR3.3).
/// HS hoặc PH tạo → GV duyệt/từ chối → bắn thông báo kết quả.
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item>N LeaveRequest — 1 <see cref="Class"/>.</item>
/// <item>N LeaveRequest — 1 Student (<see cref="User"/>).</item>
/// <item>N LeaveRequest — 1 SubmittedBy (HS tự nộp hoặc PH nộp hộ).</item>
/// <item>N LeaveRequest — 0..1 ApprovedByTeacher (null khi còn Pending).</item>
/// </list>
/// </para>
/// </summary>
public class LeaveRequest
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>FK → <see cref="Class"/> — lớp của học sinh xin nghỉ.</summary>
    public int ClassId { get; set; }

    /// <summary>Navigation: lớp học.</summary>
    public Class Class { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> (Role = Student) — học sinh xin nghỉ.</summary>
    public int StudentId { get; set; }

    /// <summary>Navigation: học sinh.</summary>
    public User Student { get; set; } = null!;

    /// <summary>FK → <see cref="User"/> — người tạo đơn (HS hoặc PH); dùng audit.</summary>
    public int SubmittedByUserId { get; set; }

    /// <summary>Navigation: người nộp đơn.</summary>
    public User SubmittedBy { get; set; } = null!;

    /// <summary>Ngày xin nghỉ (chỉ lấy phần ngày).</summary>
    public DateTime Date { get; set; }

    /// <summary>Lý do xin nghỉ.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>URL ảnh giấy xác nhận y tế (upload qua /api/files/upload).</summary>
    public string? MedicalCertificateUrl { get; set; }

    /// <summary>Pending / Approved / Rejected — xem <see cref="LeaveRequestStatus"/>.</summary>
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    /// <summary>FK → <see cref="User"/> (Teacher) — GV duyệt/từ chối; null khi Pending.</summary>
    public int? ApprovedByTeacherId { get; set; }

    /// <summary>Navigation: giáo viên xử lý đơn (nullable).</summary>
    public User? ApprovedByTeacher { get; set; }

    /// <summary>Lý do từ chối (khi Status = Rejected).</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Thời điểm tạo đơn (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Thời điểm GV duyệt/từ chối (UTC).</summary>
    public DateTime? ReviewedAt { get; set; }
}
