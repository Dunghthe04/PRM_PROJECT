using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ đơn xin nghỉ (FR2.5, FR3.3 — Ngày 9 Bước 4).
/// HS/PH tạo đơn → GV duyệt/từ chối → ghi thông báo in-app.
/// </summary>
public interface ILeaveRequestService
{
    Task<(List<LeaveRequestDto>? Result, string? Error)> GetListAsync(
        LeaveRequestListQueryDto query, int actorId, UserRole actorRole);

    Task<List<LeaveRequestDto>> GetMyAsync(int actorId, UserRole actorRole);

    Task<(LeaveRequestDto? Result, string? Error)> GetByIdAsync(
        int id, int actorId, UserRole actorRole);

    Task<(LeaveRequestDto? Result, string? Error)> CreateAsync(
        CreateLeaveRequestDto dto, int actorId, UserRole actorRole);

    Task<(LeaveRequestDto? Result, string? Error)> ApproveAsync(
        int id, int teacherId, UserRole actorRole);

    Task<(LeaveRequestDto? Result, string? Error)> RejectAsync(
        int id, RejectLeaveRequestDto dto, int teacherId, UserRole actorRole);

    Task<(bool Success, string Message)> CancelAsync(
        int id, int actorId, UserRole actorRole);
}

/// <summary>Implement ILeaveRequestService.</summary>
public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IClassRepository _classRepository;
    private readonly ITeacherAssignmentRepository _teacherAssignmentRepository;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        IClassRepository classRepository,
        ITeacherAssignmentRepository teacherAssignmentRepository,
        INotificationService notificationService,
        AppDbContext context)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _classRepository = classRepository;
        _teacherAssignmentRepository = teacherAssignmentRepository;
        _notificationService = notificationService;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(List<LeaveRequestDto>? Result, string? Error)> GetListAsync(
        LeaveRequestListQueryDto query, int actorId, UserRole actorRole)
    {
        if (actorRole is not (UserRole.Teacher or UserRole.Admin))
            return (null, "Không có quyền xem danh sách đơn nghỉ.");

        if (!query.ClassId.HasValue)
            return (null, "classId là bắt buộc.");

        if (await _classRepository.GetByIdAsync(query.ClassId.Value) == null)
            return (null, "Không tìm thấy lớp học.");

        var permError = await VerifyTeacherCanReviewAsync(actorId, actorRole, query.ClassId.Value);
        if (permError != null) return (null, permError);

        var items = await _leaveRequestRepository.GetListAsync(query.ClassId, query.Status);
        return (items.Select(MapToDto).ToList(), null);
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequestDto>> GetMyAsync(int actorId, UserRole actorRole)
    {
        var studentIds = await ResolveStudentIdsAsync(actorId, actorRole);
        var items = await _leaveRequestRepository.GetByStudentIdsAsync(studentIds);
        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<(LeaveRequestDto? Result, string? Error)> GetByIdAsync(
        int id, int actorId, UserRole actorRole)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy đơn xin nghỉ.");

        var accessError = await VerifyCanViewAsync(entity, actorId, actorRole);
        if (accessError != null) return (null, accessError);

        return (MapToDto(entity), null);
    }

    /// <inheritdoc />
    public async Task<(LeaveRequestDto? Result, string? Error)> CreateAsync(
        CreateLeaveRequestDto dto, int actorId, UserRole actorRole)
    {
        if (actorRole is not (UserRole.Student or UserRole.Parent))
            return (null, "Chỉ học sinh hoặc phụ huynh được tạo đơn xin nghỉ.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (null, "Lý do xin nghỉ là bắt buộc.");

        var studentId = actorRole == UserRole.Student
            ? actorId
            : dto.StudentId;

        if (!studentId.HasValue || studentId.Value <= 0)
            return (null, "studentId là bắt buộc khi phụ huynh tạo đơn.");

        if (actorRole == UserRole.Parent)
        {
            var isChild = await _context.StudentParents
                .AnyAsync(sp => sp.ParentId == actorId && sp.StudentId == studentId.Value);
            if (!isChild)
                return (null, "Bạn không có quyền tạo đơn cho học sinh này.");
        }

        var (classId, classIdError) = await ResolveClassIdAsync(studentId.Value, dto.ClassId);
        if (classIdError != null) return (null, classIdError);

        var leaveDate = dto.Date.Date;
        if (leaveDate < DateTime.UtcNow.Date)
            return (null, "Không thể xin nghỉ cho ngày đã qua.");

        var entity = new LeaveRequest
        {
            ClassId = classId,
            StudentId = studentId.Value,
            SubmittedByUserId = actorId,
            Date = leaveDate,
            Reason = dto.Reason.Trim(),
            MedicalCertificateUrl = string.IsNullOrWhiteSpace(dto.MedicalCertificateUrl)
                ? null
                : dto.MedicalCertificateUrl.Trim(),
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _leaveRequestRepository.CreateAsync(entity);
        var loaded = await _leaveRequestRepository.GetByIdAsync(created.Id);
        return (loaded == null ? null : MapToDto(loaded), null);
    }

    /// <inheritdoc />
    public async Task<(LeaveRequestDto? Result, string? Error)> ApproveAsync(
        int id, int teacherId, UserRole actorRole)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy đơn xin nghỉ.");

        var reviewError = await ValidateReviewAsync(entity, teacherId, actorRole);
        if (reviewError != null) return (null, reviewError);

        entity.Status = LeaveRequestStatus.Approved;
        entity.ApprovedByTeacherId = teacherId;
        entity.RejectionReason = null;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(entity);

        await NotifyLeaveResultAsync(entity, approved: true);

        var updated = await _leaveRequestRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    /// <inheritdoc />
    public async Task<(LeaveRequestDto? Result, string? Error)> RejectAsync(
        int id, RejectLeaveRequestDto dto, int teacherId, UserRole actorRole)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy đơn xin nghỉ.");

        var reviewError = await ValidateReviewAsync(entity, teacherId, actorRole);
        if (reviewError != null) return (null, reviewError);

        entity.Status = LeaveRequestStatus.Rejected;
        entity.ApprovedByTeacherId = teacherId;
        entity.RejectionReason = string.IsNullOrWhiteSpace(dto.RejectionReason)
            ? null
            : dto.RejectionReason.Trim();
        entity.ReviewedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _leaveRequestRepository.UpdateAsync(entity);

        await NotifyLeaveResultAsync(entity, approved: false);

        var updated = await _leaveRequestRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> CancelAsync(
        int id, int actorId, UserRole actorRole)
    {
        var entity = await _leaveRequestRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy đơn xin nghỉ.");

        if (entity.Status != LeaveRequestStatus.Pending)
            return (false, "Chỉ hủy được đơn đang chờ duyệt.");

        if (actorRole == UserRole.Student && entity.StudentId != actorId)
            return (false, "Bạn không có quyền hủy đơn này.");

        if (actorRole == UserRole.Parent)
        {
            var isChild = await _context.StudentParents
                .AnyAsync(sp => sp.ParentId == actorId && sp.StudentId == entity.StudentId);
            if (!isChild)
                return (false, "Bạn không có quyền hủy đơn này.");
        }

        if (actorRole is not (UserRole.Student or UserRole.Parent))
            return (false, "Chỉ học sinh hoặc phụ huynh được hủy đơn.");

        await _leaveRequestRepository.DeleteAsync(entity);
        return (true, "Đã hủy đơn xin nghỉ.");
    }

    /// <summary>HS xem của mình; PH xem đơn của các con.</summary>
    private async Task<List<int>> ResolveStudentIdsAsync(int actorId, UserRole actorRole)
    {
        if (actorRole == UserRole.Student)
            return new List<int> { actorId };

        if (actorRole == UserRole.Parent)
        {
            return await _context.StudentParents
                .Where(sp => sp.ParentId == actorId)
                .Select(sp => sp.StudentId)
                .ToListAsync();
        }

        return new List<int>();
    }

    /// <summary>Xác định lớp của HS — nếu nhiều lớp thì bắt buộc truyền classId.</summary>
    private async Task<(int ClassId, string? Error)> ResolveClassIdAsync(int studentId, int? requestedClassId)
    {
        var classLinks = await _context.ClassStudents
            .Where(cs => cs.StudentId == studentId)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        if (classLinks.Count == 0)
            return (0, "Học sinh chưa được gán vào lớp nào.");

        if (requestedClassId.HasValue)
        {
            if (!classLinks.Contains(requestedClassId.Value))
                return (0, "Học sinh không thuộc lớp đã chọn.");

            return (requestedClassId.Value, null);
        }

        if (classLinks.Count > 1)
            return (0, "Học sinh thuộc nhiều lớp — vui lòng chọn classId.");

        return (classLinks[0], null);
    }

    private async Task<string?> VerifyTeacherCanReviewAsync(int actorId, UserRole role, int classId)
    {
        if (role is UserRole.Admin)
            return null;

        if (role != UserRole.Teacher)
            return "Không có quyền duyệt đơn nghỉ.";

        var assignments = await _teacherAssignmentRepository.GetByTeacherAsync(actorId);
        if (!assignments.Any(ta => ta.ClassId == classId))
            return "Bạn chưa được phân công dạy lớp này.";

        return null;
    }

    private async Task<string?> VerifyCanViewAsync(LeaveRequest entity, int actorId, UserRole role)
    {
        if (role is UserRole.Admin)
            return null;

        if (role == UserRole.Teacher)
            return await VerifyTeacherCanReviewAsync(actorId, role, entity.ClassId);

        if (role == UserRole.Student && entity.StudentId == actorId)
            return null;

        if (role == UserRole.Parent)
        {
            var isChild = await _context.StudentParents
                .AnyAsync(sp => sp.ParentId == actorId && sp.StudentId == entity.StudentId);
            if (isChild) return null;
        }

        return "Bạn không có quyền xem đơn này.";
    }

    private async Task<string?> ValidateReviewAsync(LeaveRequest entity, int teacherId, UserRole role)
    {
        if (entity.Status != LeaveRequestStatus.Pending)
            return "Đơn đã được xử lý — không thể duyệt/từ chối lại.";

        return await VerifyTeacherCanReviewAsync(teacherId, role, entity.ClassId);
    }

    /// <summary>Ghi thông báo in-app + push FCM cho HS + PH.</summary>
    private async Task NotifyLeaveResultAsync(LeaveRequest entity, bool approved)
    {
        var className = entity.Class?.Name ?? "lớp học";
        var dateText = entity.Date.ToString("dd/MM/yyyy");

        var title = approved ? "Đơn xin nghỉ đã được duyệt" : "Đơn xin nghỉ bị từ chối";
        var message = approved
            ? $"Đơn xin nghỉ ngày {dateText} ({className}) đã được giáo viên duyệt."
            : $"Đơn xin nghỉ ngày {dateText} ({className}) đã bị từ chối."
                + (string.IsNullOrEmpty(entity.RejectionReason) ? "" : $" Lý do: {entity.RejectionReason}");

        var recipientIds = new List<int> { entity.StudentId };

        var parentIds = await _context.StudentParents
            .Where(sp => sp.StudentId == entity.StudentId)
            .Select(sp => sp.ParentId)
            .ToListAsync();

        recipientIds.AddRange(parentIds);

        await _notificationService.NotifyUsersAsync(recipientIds, title, message, sendPush: true);
    }

    internal static LeaveRequestDto MapToDto(LeaveRequest lr) => new()
    {
        Id = lr.Id,
        ClassId = lr.ClassId,
        ClassName = lr.Class?.Name ?? string.Empty,
        StudentId = lr.StudentId,
        StudentName = lr.Student?.FullName ?? string.Empty,
        StudentPhone = lr.Student?.Phone ?? string.Empty,
        SubmittedByUserId = lr.SubmittedByUserId,
        SubmittedByName = lr.SubmittedBy?.FullName ?? string.Empty,
        Date = lr.Date,
        Reason = lr.Reason,
        MedicalCertificateUrl = lr.MedicalCertificateUrl,
        Status = lr.Status.ToString(),
        ApprovedByTeacherId = lr.ApprovedByTeacherId,
        ApprovedByTeacherName = lr.ApprovedByTeacher?.FullName,
        RejectionReason = lr.RejectionReason,
        CreatedAt = lr.CreatedAt,
        UpdatedAt = lr.UpdatedAt,
        ReviewedAt = lr.ReviewedAt
    };
}
