using Api.Models;

namespace Api.DTOs;

// ─── FR2.5, FR3.3 — Đơn xin nghỉ (Ngày 9 Bước 2) ────────────────────────────

/// <summary>Thông tin 1 đơn xin nghỉ trả về client.</summary>
public class LeaveRequestDto
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;

    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;

    public int SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? MedicalCertificateUrl { get; set; }

    /// <summary>Pending / Approved / Rejected.</summary>
    public string Status { get; set; } = string.Empty;

    public int? ApprovedByTeacherId { get; set; }
    public string? ApprovedByTeacherName { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Query GET /api/leave-requests — GV xem DS đơn theo lớp.</summary>
public class LeaveRequestListQueryDto
{
    public int? ClassId { get; set; }
    public LeaveRequestStatus? Status { get; set; }
}

/// <summary>Body POST /api/leave-requests — tạo đơn mới.</summary>
public class CreateLeaveRequestDto
{
    /// <summary>Bắt buộc khi PH tạo hộ; HS có thể bỏ qua (mặc định = chính mình).</summary>
    public int? StudentId { get; set; }

    /// <summary>Lớp của HS (nếu HS thuộc nhiều lớp thì bắt buộc chọn).</summary>
    public int? ClassId { get; set; }

    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>URL ảnh y tế sau khi upload qua POST /api/files/upload.</summary>
    public string? MedicalCertificateUrl { get; set; }
}

/// <summary>Body PUT /api/leave-requests/{id}/reject.</summary>
public class RejectLeaveRequestDto
{
    public string? RejectionReason { get; set; }
}

/// <summary>Response sau upload file.</summary>
public class FileUploadResultDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
