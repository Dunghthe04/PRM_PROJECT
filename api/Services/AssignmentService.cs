using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ bài tập + nộp bài (FR3.5, FR2.4 — Ngày 8 Bước 4).
/// GV: CRUD assignment, xem nộp, chấm điểm.
/// HS: xem ToDo/Done/Overdue, nộp / nộp lại.
/// </summary>
public interface IAssignmentService
{
    /// <summary>DS bài tập theo lớp/môn (GV / Admin / HeadOfDept).</summary>
    Task<(List<AssignmentDto>? Result, string? Error)> GetListAsync(
        AssignmentListQueryDto query, int actorId, UserRole actorRole);

    /// <summary>Bài tập của HS đang login + trạng thái ToDo/Done/Overdue.</summary>
    /// <summary>
    /// Bài tập của HS (ToDo/Done/Overdue). PH xem theo con đang chọn.
    /// - HS: bỏ qua studentId.
    /// - PH: studentId = con muốn xem (phải liên kết); null → con đầu tiên.
    /// </summary>
    Task<(List<AssignmentDto>? Result, string? Error)> GetMyAsync(
        int userId, UserRole role, int? studentId, MyAssignmentsQueryDto query);

    /// <summary>Chi tiết 1 bài tập.</summary>
    Task<(AssignmentDto? Result, string? Error)> GetByIdAsync(int id, int? studentIdForStatus = null);

    /// <summary>GV tạo bài tập.</summary>
    Task<(AssignmentDto? Result, string? Error)> CreateAsync(
        CreateUpdateAssignmentDto dto, int teacherId, UserRole actorRole);

    /// <summary>GV sửa bài tập (chỉ người tạo hoặc Admin/HeadOfDept).</summary>
    Task<(AssignmentDto? Result, string? Error)> UpdateAsync(
        int id, CreateUpdateAssignmentDto dto, int actorId, UserRole actorRole);

    /// <summary>GV xóa bài tập.</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole);

    /// <summary>GV xem DS bài nộp của 1 assignment.</summary>
    Task<(List<SubmissionDto>? Result, string? Error)> GetSubmissionsAsync(
        int assignmentId, int actorId, UserRole actorRole);

    /// <summary>HS nộp bài (tạo mới hoặc nộp lại nếu đã có).</summary>
    Task<(SubmissionDto? Result, string? Error)> SubmitAsync(
        int assignmentId, SubmitAssignmentDto dto, int studentId);

    /// <summary>Chi tiết 1 bài nộp.</summary>
    Task<(SubmissionDto? Result, string? Error)> GetSubmissionByIdAsync(int submissionId);

    /// <summary>HS nộp lại (cập nhật link/file).</summary>
    Task<(SubmissionDto? Result, string? Error)> ResubmitAsync(
        int submissionId, SubmitAssignmentDto dto, int studentId);

    /// <summary>GV chấm điểm + feedback.</summary>
    Task<(SubmissionDto? Result, string? Error)> GradeSubmissionAsync(
        int submissionId, GradeSubmissionDto dto, int teacherId, UserRole actorRole);
}

/// <summary>Implement IAssignmentService.</summary>
public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeacherAssignmentRepository _teacherAssignmentRepository;
    private readonly AppDbContext _context;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository,
        ITeacherAssignmentRepository teacherAssignmentRepository,
        AppDbContext context)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _classRepository = classRepository;
        _subjectRepository = subjectRepository;
        _teacherAssignmentRepository = teacherAssignmentRepository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(List<AssignmentDto>? Result, string? Error)> GetListAsync(
        AssignmentListQueryDto query, int actorId, UserRole actorRole)
    {
        if (actorRole is not (UserRole.Teacher or UserRole.Admin or UserRole.HeadOfDept))
            return (null, "Không có quyền xem danh sách bài tập lớp.");

        // Teacher: nếu lọc theo class+subject thì kiểm tra phân công
        if (actorRole == UserRole.Teacher && query.ClassId.HasValue && query.SubjectId.HasValue)
        {
            var perm = await VerifyTeacherAssignedAsync(actorId, query.ClassId.Value, query.SubjectId.Value);
            if (perm != null) return (null, perm);
        }

        var items = await _assignmentRepository.GetListAsync(query.ClassId, query.SubjectId);

        // Teacher chỉ thấy bài mình tạo (trừ khi Admin/HeadOfDept)
        if (actorRole == UserRole.Teacher)
            items = items.Where(a => a.CreatedByTeacherId == actorId).ToList();

        return (items.Select(a => MapAssignment(a)).ToList(), null);
    }

    /// <summary>
    /// HS: lấy bài tập các lớp đang học, gắn Status theo DueDate + đã nộp chưa.
    /// </summary>
    public async Task<(List<AssignmentDto>? Result, string? Error)> GetMyAsync(
        int userId, UserRole role, int? studentId, MyAssignmentsQueryDto query)
    {
        // Quy đổi ra học sinh mục tiêu (HS = bản thân; PH = con đang chọn).
        var (targetStudentId, error) = await ResolveTargetStudentAsync(userId, role, studentId);
        if (error != null) return (null, error);

        var classIds = await _context.ClassStudents
            .Where(cs => cs.StudentId == targetStudentId)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        if (classIds.Count == 0)
            return (new List<AssignmentDto>(), null);

        var items = await _assignmentRepository.GetByClassIdsAsync(classIds);
        var dtos = items
            .Select(a => MapAssignment(a, ResolveStatus(a, targetStudentId)))
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            dtos = dtos.Where(d =>
                string.Equals(d.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return (dtos, null);
    }

    /// <summary>
    /// Quy đổi (userId, role, studentId) → id học sinh cần xem dữ liệu.
    /// HS: chính mình. PH: con được chọn (validate liên kết) hoặc con đầu tiên.
    /// Trả về (targetStudentId, error) — error != null nếu không hợp lệ.
    /// </summary>
    private async Task<(int TargetStudentId, string? Error)> ResolveTargetStudentAsync(
        int userId, UserRole role, int? requestedStudentId)
    {
        if (role == UserRole.Student)
            return (userId, null);

        if (role == UserRole.Parent)
        {
            var childIds = await _context.StudentParents
                .Where(sp => sp.ParentId == userId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            if (childIds.Count == 0)
                return (0, "Tài khoản phụ huynh chưa liên kết học sinh nào.");

            if (requestedStudentId.HasValue)
            {
                if (!childIds.Contains(requestedStudentId.Value))
                    return (0, "Học sinh không thuộc quyền quản lý của phụ huynh.");
                return (requestedStudentId.Value, null);
            }

            return (childIds[0], null);
        }

        return (0, "Vai trò không được phép truy cập dữ liệu học tập cá nhân.");
    }

    /// <inheritdoc />
    public async Task<(AssignmentDto? Result, string? Error)> GetByIdAsync(int id, int? studentIdForStatus = null)
    {
        var entity = await _assignmentRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy bài tập.");

        string? status = studentIdForStatus.HasValue
            ? ResolveStatus(entity, studentIdForStatus.Value)
            : null;

        return (MapAssignment(entity, status), null);
    }

    /// <summary>Tạo bài — Class/Subject tồn tại; Teacher phải được phân công (trừ Admin).</summary>
    public async Task<(AssignmentDto? Result, string? Error)> CreateAsync(
        CreateUpdateAssignmentDto dto, int teacherId, UserRole actorRole)
    {
        var error = await ValidateAssignmentDtoAsync(dto);
        if (error != null) return (null, error);

        if (actorRole == UserRole.Teacher)
        {
            var perm = await VerifyTeacherAssignedAsync(teacherId, dto.ClassId, dto.SubjectId);
            if (perm != null) return (null, perm);
        }

        var entity = new Assignment
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            DueDate = dto.DueDate.ToUniversalTime(),
            MaxScore = dto.MaxScore,
            AttachmentUrl = NormalizeOptional(dto.AttachmentUrl),
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId,
            CreatedByTeacherId = teacherId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _assignmentRepository.CreateAsync(entity);
        var full = await _assignmentRepository.GetByIdAsync(created.Id);
        return (MapAssignment(full!), null);
    }

    /// <inheritdoc />
    public async Task<(AssignmentDto? Result, string? Error)> UpdateAsync(
        int id, CreateUpdateAssignmentDto dto, int actorId, UserRole actorRole)
    {
        var entity = await _assignmentRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy bài tập.");

        var ownerError = VerifyCanManageAssignment(entity, actorId, actorRole);
        if (ownerError != null) return (null, ownerError);

        var error = await ValidateAssignmentDtoAsync(dto);
        if (error != null) return (null, error);

        if (actorRole == UserRole.Teacher)
        {
            var perm = await VerifyTeacherAssignedAsync(actorId, dto.ClassId, dto.SubjectId);
            if (perm != null) return (null, perm);
        }

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description.Trim();
        entity.DueDate = dto.DueDate.ToUniversalTime();
        entity.MaxScore = dto.MaxScore;
        entity.AttachmentUrl = NormalizeOptional(dto.AttachmentUrl);
        entity.ClassId = dto.ClassId;
        entity.SubjectId = dto.SubjectId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _assignmentRepository.UpdateAsync(entity);
        var full = await _assignmentRepository.GetByIdAsync(id);
        return (MapAssignment(full!), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole)
    {
        var entity = await _assignmentRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy bài tập.");

        var ownerError = VerifyCanManageAssignment(entity, actorId, actorRole);
        if (ownerError != null) return (false, ownerError);

        await _assignmentRepository.DeleteAsync(entity);
        return (true, "Đã xóa bài tập.");
    }

    /// <inheritdoc />
    public async Task<(List<SubmissionDto>? Result, string? Error)> GetSubmissionsAsync(
        int assignmentId, int actorId, UserRole actorRole)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null) return (null, "Không tìm thấy bài tập.");

        var ownerError = VerifyCanManageAssignment(assignment, actorId, actorRole);
        if (ownerError != null) return (null, ownerError);

        var items = await _submissionRepository.GetByAssignmentAsync(assignmentId);
        return (items.Select(s => MapSubmission(s, assignment.DueDate)).ToList(), null);
    }

    /// <summary>
    /// Nộp bài: phải là HS trong lớp của assignment; có link hoặc file;
    /// đã có bài nộp → cập nhật (nộp lại).
    /// </summary>
    public async Task<(SubmissionDto? Result, string? Error)> SubmitAsync(
        int assignmentId, SubmitAssignmentDto dto, int studentId)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null) return (null, "Không tìm thấy bài tập.");

        var inClass = await _context.ClassStudents
            .AnyAsync(cs => cs.ClassId == assignment.ClassId && cs.StudentId == studentId);
        if (!inClass)
            return (null, "Bạn không thuộc lớp của bài tập này.");

        var contentError = ValidateSubmitContent(dto);
        if (contentError != null) return (null, contentError);

        var existing = await _submissionRepository.GetByAssignmentAndStudentAsync(assignmentId, studentId);
        if (existing != null)
        {
            existing.LinkUrl = NormalizeOptional(dto.LinkUrl);
            existing.FileUrl = NormalizeOptional(dto.FileUrl);
            existing.UpdatedAt = DateTime.UtcNow;
            // Nộp lại → xóa điểm cũ (cần GV chấm lại)
            existing.Score = null;
            existing.Feedback = null;
            existing.GradedByTeacherId = null;
            existing.GradedAt = null;

            await _submissionRepository.UpdateAsync(existing);
            var refreshed = await _submissionRepository.GetByIdAsync(existing.Id);
            return (MapSubmission(refreshed!, assignment.DueDate), null);
        }

        var entity = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            LinkUrl = NormalizeOptional(dto.LinkUrl),
            FileUrl = NormalizeOptional(dto.FileUrl),
            SubmittedAt = DateTime.UtcNow
        };

        var created = await _submissionRepository.CreateAsync(entity);
        var full = await _submissionRepository.GetByIdAsync(created.Id);
        return (MapSubmission(full!, assignment.DueDate), null);
    }

    /// <inheritdoc />
    public async Task<(SubmissionDto? Result, string? Error)> GetSubmissionByIdAsync(int submissionId)
    {
        var entity = await _submissionRepository.GetByIdAsync(submissionId);
        if (entity == null) return (null, "Không tìm thấy bài nộp.");
        return (MapSubmission(entity, entity.Assignment.DueDate), null);
    }

    /// <inheritdoc />
    public async Task<(SubmissionDto? Result, string? Error)> ResubmitAsync(
        int submissionId, SubmitAssignmentDto dto, int studentId)
    {
        var entity = await _submissionRepository.GetByIdAsync(submissionId);
        if (entity == null) return (null, "Không tìm thấy bài nộp.");

        if (entity.StudentId != studentId)
            return (null, "Bạn chỉ được nộp lại bài của mình.");

        var contentError = ValidateSubmitContent(dto);
        if (contentError != null) return (null, contentError);

        entity.LinkUrl = NormalizeOptional(dto.LinkUrl);
        entity.FileUrl = NormalizeOptional(dto.FileUrl);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Score = null;
        entity.Feedback = null;
        entity.GradedByTeacherId = null;
        entity.GradedAt = null;

        await _submissionRepository.UpdateAsync(entity);
        var full = await _submissionRepository.GetByIdAsync(submissionId);
        return (MapSubmission(full!, full!.Assignment.DueDate), null);
    }

    /// <summary>Chấm điểm — Score trong [0, MaxScore]; chỉ GV quản lý assignment.</summary>
    public async Task<(SubmissionDto? Result, string? Error)> GradeSubmissionAsync(
        int submissionId, GradeSubmissionDto dto, int teacherId, UserRole actorRole)
    {
        var entity = await _submissionRepository.GetByIdAsync(submissionId);
        if (entity == null) return (null, "Không tìm thấy bài nộp.");

        var ownerError = VerifyCanManageAssignment(entity.Assignment, teacherId, actorRole);
        if (ownerError != null) return (null, ownerError);

        if (dto.Score < 0 || dto.Score > entity.Assignment.MaxScore)
            return (null, $"Điểm phải từ 0 đến {entity.Assignment.MaxScore}.");

        entity.Score = dto.Score;
        entity.Feedback = NormalizeOptional(dto.Feedback);
        entity.GradedByTeacherId = teacherId;
        entity.GradedAt = DateTime.UtcNow;

        await _submissionRepository.UpdateAsync(entity);
        var full = await _submissionRepository.GetByIdAsync(submissionId);
        return (MapSubmission(full!, full!.Assignment.DueDate), null);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// ToDo = chưa nộp + còn hạn; Done = đã nộp; Overdue = chưa nộp + quá hạn.
    /// </summary>
    private static string ResolveStatus(Assignment assignment, int studentId)
    {
        var submitted = assignment.Submissions.Any(s => s.StudentId == studentId);
        if (submitted) return AssignmentStatus.Done;

        return DateTime.UtcNow > assignment.DueDate
            ? AssignmentStatus.Overdue
            : AssignmentStatus.ToDo;
    }

    private async Task<string?> ValidateAssignmentDtoAsync(CreateUpdateAssignmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "Tiêu đề không được để trống.";

        if (dto.MaxScore <= 0)
            return "MaxScore phải lớn hơn 0.";

        if (await _classRepository.GetByIdAsync(dto.ClassId) == null)
            return "Không tìm thấy lớp học.";

        if (await _subjectRepository.GetByIdAsync(dto.SubjectId) == null)
            return "Không tìm thấy môn học.";

        return null;
    }

    private static string? ValidateSubmitContent(SubmitAssignmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LinkUrl) && string.IsNullOrWhiteSpace(dto.FileUrl))
            return "Cần ít nhất LinkUrl hoặc FileUrl khi nộp bài.";
        return null;
    }

    /// <summary>Teacher chỉ sửa/xóa bài mình tạo; Admin/HeadOfDept được phép.</summary>
    private static string? VerifyCanManageAssignment(Assignment assignment, int actorId, UserRole actorRole)
    {
        if (actorRole is UserRole.Admin or UserRole.HeadOfDept)
            return null;

        if (actorRole == UserRole.Teacher && assignment.CreatedByTeacherId == actorId)
            return null;

        return "Bạn không có quyền thao tác bài tập này.";
    }

    private async Task<string?> VerifyTeacherAssignedAsync(int teacherId, int classId, int subjectId)
    {
        var assigned = await _teacherAssignmentRepository.FindByClassAndSubjectAsync(classId, subjectId);
        if (assigned == null || assigned.TeacherId != teacherId)
            return "Bạn chưa được phân công dạy lớp/môn này.";
        return null;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AssignmentDto MapAssignment(Assignment a, string? status = null) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Description = a.Description,
        DueDate = a.DueDate,
        MaxScore = a.MaxScore,
        AttachmentUrl = a.AttachmentUrl,
        ClassId = a.ClassId,
        ClassName = a.Class?.Name ?? string.Empty,
        SubjectId = a.SubjectId,
        SubjectName = a.Subject?.Name ?? string.Empty,
        SubjectCode = a.Subject?.Code ?? string.Empty,
        CreatedByTeacherId = a.CreatedByTeacherId,
        TeacherName = a.CreatedByTeacher?.FullName ?? string.Empty,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        SubmissionCount = a.Submissions?.Count ?? 0,
        Status = status
    };

    private static SubmissionDto MapSubmission(Submission s, DateTime dueDate) => new()
    {
        Id = s.Id,
        AssignmentId = s.AssignmentId,
        AssignmentTitle = s.Assignment?.Title ?? string.Empty,
        StudentId = s.StudentId,
        StudentName = s.Student?.FullName ?? string.Empty,
        StudentPhone = s.Student?.Phone ?? string.Empty,
        LinkUrl = s.LinkUrl,
        FileUrl = s.FileUrl,
        SubmittedAt = s.SubmittedAt,
        UpdatedAt = s.UpdatedAt,
        Score = s.Score,
        Feedback = s.Feedback,
        GradedByTeacherId = s.GradedByTeacherId,
        GradedByTeacherName = s.GradedByTeacher?.FullName,
        GradedAt = s.GradedAt,
        IsLate = (s.UpdatedAt ?? s.SubmittedAt) > dueDate
    };
}
