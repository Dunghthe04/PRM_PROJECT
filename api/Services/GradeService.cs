using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ điểm số (FR3.2, FR2.3 — Ngày 6 Bước 4).
/// Validate → gọi Repository → map DTO. Luồng Draft → Publish.
/// </summary>
public interface IGradeService
{
    /// <summary>GET /api/grades — GV xem bảng điểm lớp.</summary>
    Task<(List<GradeDto>? Result, string? Error)> GetListAsync(
        GradeListQueryDto query, int actorId, UserRole actorRole);

    /// <summary>GET /api/grades/me — HS/PH xem điểm đã công bố.</summary>
    Task<List<GradeDto>> GetMyGradesAsync(int actorId, UserRole actorRole, MyGradesQueryDto query);

    /// <summary>GET /api/grades/{id}</summary>
    Task<GradeDto?> GetByIdAsync(int id);

    /// <summary>POST /api/grades/batch — nhập điểm hàng loạt (Nháp).</summary>
    Task<(BatchGradeResultDto? Result, string? Error)> BatchUpsertAsync(
        BatchGradeDto dto, int teacherId, UserRole actorRole);

    /// <summary>PUT /api/grades/{id} — sửa 1 điểm (chỉ Draft).</summary>
    Task<(GradeDto? Result, string? Error)> UpdateAsync(
        int id, UpdateGradeDto dto, int actorId, UserRole actorRole);

    /// <summary>DELETE /api/grades/{id} — xóa (chỉ Draft).</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole);

    /// <summary>POST /api/grades/publish — công bố điểm Nháp.</summary>
    Task<(PublishGradesResultDto? Result, string? Error)> PublishAsync(
        PublishGradesDto dto, int actorId, UserRole actorRole);

    /// <summary>PUT /api/grades/{id}/approve — Trưởng BM duyệt (tuỳ chọn).</summary>
    Task<(GradeDto? Result, string? Error)> ApproveAsync(int id, int headOfDeptId);
}

/// <summary>Implement IGradeService.</summary>
public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITeacherAssignmentRepository _teacherAssignmentRepository;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public GradeService(
        IGradeRepository gradeRepository,
        IClassRepository classRepository,
        ISemesterRepository semesterRepository,
        ISubjectRepository subjectRepository,
        ITeacherAssignmentRepository teacherAssignmentRepository,
        INotificationService notificationService,
        AppDbContext context)
    {
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
        _semesterRepository = semesterRepository;
        _subjectRepository = subjectRepository;
        _teacherAssignmentRepository = teacherAssignmentRepository;
        _notificationService = notificationService;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(List<GradeDto>? Result, string? Error)> GetListAsync(
        GradeListQueryDto query, int actorId, UserRole actorRole)
    {
        var contextError = await ValidateGradeContextAsync(query.ClassId, query.SubjectId, query.SemesterId);
        if (contextError != null) return (null, contextError);

        var permError = await VerifyTeacherCanGradeAsync(actorId, actorRole, query.ClassId, query.SubjectId);
        if (permError != null) return (null, permError);

        var items = await _gradeRepository.GetListAsync(query);
        return (items.Select(MapToDto).ToList(), null);
    }

    /// <inheritdoc />
    public async Task<List<GradeDto>> GetMyGradesAsync(int actorId, UserRole actorRole, MyGradesQueryDto query)
    {
        var studentIds = new List<int>();

        if (actorRole == UserRole.Student)
        {
            studentIds.Add(actorId);
        }
        else if (actorRole == UserRole.Parent)
        {
            // PH xem điểm đã Publish của các con liên kết (FR2.1).
            var childIds = await _context.StudentParents
                .Where(sp => sp.ParentId == actorId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            // Nếu PH chọn 1 con cụ thể (Switch Profile) → chỉ lấy con đó,
            // và phải là con đã liên kết; ngược lại lấy tất cả các con.
            if (query.StudentId.HasValue)
            {
                if (!childIds.Contains(query.StudentId.Value))
                    return new List<GradeDto>(); // con không thuộc PH → trả rỗng
                studentIds.Add(query.StudentId.Value);
            }
            else
            {
                studentIds = childIds;
            }
        }
        else
        {
            return new List<GradeDto>();
        }

        if (studentIds.Count == 0)
            return new List<GradeDto>();

        var allGrades = new List<Grade>();
        foreach (var studentId in studentIds)
        {
            var grades = await _gradeRepository.GetPublishedByStudentAsync(studentId, query.SemesterId);
            allGrades.AddRange(grades);
        }

        return allGrades
            .OrderByDescending(g => g.SemesterId)
            .ThenBy(g => g.Student.FullName)
            .ThenBy(g => g.Subject.Name)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<GradeDto?> GetByIdAsync(int id)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        return grade == null ? null : MapToDto(grade);
    }

    /// <inheritdoc />
    public async Task<(BatchGradeResultDto? Result, string? Error)> BatchUpsertAsync(
        BatchGradeDto dto, int teacherId, UserRole actorRole)
    {
        if (dto.Entries.Count == 0)
            return (null, "Danh sách điểm không được trống.");

        if (string.IsNullOrWhiteSpace(dto.AssessmentType))
            return (null, "Loại điểm (AssessmentType) là bắt buộc.");

        var assessmentType = dto.AssessmentType.Trim();

        var contextError = await ValidateGradeContextAsync(dto.ClassId, dto.SubjectId, dto.SemesterId);
        if (contextError != null) return (null, contextError);

        var permError = await VerifyTeacherCanGradeAsync(teacherId, actorRole, dto.ClassId, dto.SubjectId);
        if (permError != null) return (null, permError);

        var result = new BatchGradeResultDto();
        var savedIds = new List<int>();

        foreach (var entry in dto.Entries)
        {
            var scoreError = ValidateScore(entry.Score);
            if (scoreError != null)
                return (null, $"HS Id {entry.StudentId}: {scoreError}");

            if (!await _classRepository.StudentInClassAsync(dto.ClassId, entry.StudentId))
                return (null, $"Học sinh Id {entry.StudentId} không thuộc lớp này.");

            var existing = await _gradeRepository.FindByKeyAsync(
                entry.StudentId, dto.ClassId, dto.SubjectId, dto.SemesterId, assessmentType);

            if (existing != null)
            {
                if (existing.Status == GradeStatus.Published)
                    return (null, $"Điểm HS Id {entry.StudentId} đã công bố — không thể sửa qua batch.");

                existing.Score = entry.Score;
                existing.UpdatedAt = DateTime.UtcNow;
                await _gradeRepository.UpdateAsync(existing);
                result.UpdatedCount++;
                savedIds.Add(existing.Id);
            }
            else
            {
                var grade = new Grade
                {
                    StudentId = entry.StudentId,
                    ClassId = dto.ClassId,
                    SubjectId = dto.SubjectId,
                    SemesterId = dto.SemesterId,
                    AssessmentType = assessmentType,
                    Score = entry.Score,
                    Status = GradeStatus.Draft,
                    CreatedByTeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow
                };
                var created = await _gradeRepository.CreateAsync(grade);
                result.CreatedCount++;
                savedIds.Add(created.Id);
            }
        }

        result.Message = $"Đã lưu nháp: {result.CreatedCount} mới, {result.UpdatedCount} cập nhật.";
        foreach (var id in savedIds)
        {
            var g = await _gradeRepository.GetByIdAsync(id);
            if (g != null) result.Grades.Add(MapToDto(g));
        }

        return (result, null);
    }

    /// <inheritdoc />
    public async Task<(GradeDto? Result, string? Error)> UpdateAsync(
        int id, UpdateGradeDto dto, int actorId, UserRole actorRole)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        if (grade == null) return (null, "Không tìm thấy bản ghi điểm.");

        if (grade.Status == GradeStatus.Published)
            return (null, "Điểm đã công bố — không thể sửa.");

        var permError = await VerifyTeacherCanGradeAsync(actorId, actorRole, grade.ClassId, grade.SubjectId);
        if (permError != null) return (null, permError);

        var scoreError = ValidateScore(dto.Score);
        if (scoreError != null) return (null, scoreError);

        grade.Score = dto.Score;
        grade.UpdatedAt = DateTime.UtcNow;
        await _gradeRepository.UpdateAsync(grade);

        var updated = await _gradeRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        if (grade == null) return (false, "Không tìm thấy bản ghi điểm.");

        if (grade.Status == GradeStatus.Published)
            return (false, "Điểm đã công bố — không thể xóa.");

        var permError = await VerifyTeacherCanGradeAsync(actorId, actorRole, grade.ClassId, grade.SubjectId);
        if (permError != null) return (false, permError);

        await _gradeRepository.DeleteAsync(grade);
        return (true, "Đã xóa bản ghi điểm nháp.");
    }

    /// <inheritdoc />
    public async Task<(PublishGradesResultDto? Result, string? Error)> PublishAsync(
        PublishGradesDto dto, int actorId, UserRole actorRole)
    {
        if (string.IsNullOrWhiteSpace(dto.AssessmentType))
            return (null, "Loại điểm (AssessmentType) là bắt buộc.");

        var assessmentType = dto.AssessmentType.Trim();

        var contextError = await ValidateGradeContextAsync(dto.ClassId, dto.SubjectId, dto.SemesterId);
        if (contextError != null) return (null, contextError);

        var permError = await VerifyTeacherCanGradeAsync(actorId, actorRole, dto.ClassId, dto.SubjectId);
        if (permError != null) return (null, permError);

        var drafts = await _gradeRepository.GetDraftsForPublishAsync(
            dto.ClassId, dto.SubjectId, dto.SemesterId, assessmentType);

        if (drafts.Count == 0)
            return (null, "Không có điểm nháp nào để công bố.");

        var now = DateTime.UtcNow;
        foreach (var grade in drafts)
        {
            grade.Status = GradeStatus.Published;
            grade.PublishedAt = now;
            grade.UpdatedAt = now;
            await _gradeRepository.UpdateAsync(grade);
        }

        var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
        var subjectName = subject?.Name ?? "môn học";

        var studentIds = drafts.Select(g => g.StudentId).Distinct().ToList();
        var recipientIds = new HashSet<int>(studentIds);

        var parentIds = await _context.StudentParents
            .Where(sp => studentIds.Contains(sp.StudentId))
            .Select(sp => sp.ParentId)
            .ToListAsync();
        foreach (var pid in parentIds) recipientIds.Add(pid);

        await _notificationService.NotifyUsersAsync(
            recipientIds.ToList(),
            "Điểm mới đã được công bố",
            $"Giáo viên vừa công bố điểm {assessmentType} môn {subjectName} ({drafts.Count} bản ghi).",
            sendPush: true);

        return (new PublishGradesResultDto
        {
            Message = $"Đã công bố {drafts.Count} bản ghi điểm.",
            PublishedCount = drafts.Count
        }, null);
    }

    /// <inheritdoc />
    public async Task<(GradeDto? Result, string? Error)> ApproveAsync(int id, int headOfDeptId)
    {
        var grade = await _gradeRepository.GetByIdAsync(id);
        if (grade == null) return (null, "Không tìm thấy bản ghi điểm.");

        if (grade.Status != GradeStatus.Published)
            return (null, "Chỉ duyệt điểm đã công bố.");

        if (grade.IsApproved)
            return (null, "Điểm đã được duyệt trước đó.");

        grade.IsApproved = true;
        grade.ApprovedById = headOfDeptId;
        grade.ApprovedAt = DateTime.UtcNow;
        grade.UpdatedAt = DateTime.UtcNow;
        await _gradeRepository.UpdateAsync(grade);

        var updated = await _gradeRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    /// <summary>Kiểm tra lớp, môn, kỳ tồn tại và lớp thuộc đúng kỳ.</summary>
    private async Task<string?> ValidateGradeContextAsync(int classId, int subjectId, int semesterId)
    {
        var cls = await _classRepository.GetByIdAsync(classId);
        if (cls == null) return "Không tìm thấy lớp học.";

        if (cls.SemesterId != semesterId)
            return "Lớp không thuộc kỳ học đã chọn.";

        if (await _subjectRepository.GetByIdAsync(subjectId) == null)
            return "Không tìm thấy môn học.";

        if (await _semesterRepository.GetByIdAsync(semesterId) == null)
            return "Không tìm thấy kỳ học.";

        return null;
    }

    /// <summary>GV phải được phân công dạy lớp+môn; Admin/Trưởng BM bypass.</summary>
    private async Task<string?> VerifyTeacherCanGradeAsync(
        int actorId, UserRole role, int classId, int subjectId)
    {
        if (role is UserRole.Admin or UserRole.HeadOfDept)
            return null;

        if (role != UserRole.Teacher)
            return "Không có quyền thao tác điểm số.";

        var assignment = await _teacherAssignmentRepository.FindByClassAndSubjectAsync(classId, subjectId);
        if (assignment == null || assignment.TeacherId != actorId)
            return "Bạn chưa được phân công dạy môn này ở lớp này.";

        return null;
    }

    /// <summary>Thang điểm 10 (chuẩn VN).</summary>
    private static string? ValidateScore(double score)
    {
        if (score < 0 || score > 10)
            return "Điểm phải nằm trong khoảng 0–10.";
        return null;
    }

    /// <summary>Map entity → GradeDto (kèm tên HS, lớp, môn từ navigation).</summary>
    internal static GradeDto MapToDto(Grade g) => new()
    {
        Id = g.Id,
        StudentId = g.StudentId,
        StudentName = g.Student?.FullName ?? string.Empty,
        StudentPhone = g.Student?.Phone ?? string.Empty,
        ClassId = g.ClassId,
        ClassName = g.Class?.Name ?? string.Empty,
        SubjectId = g.SubjectId,
        SubjectName = g.Subject?.Name ?? string.Empty,
        SemesterId = g.SemesterId,
        SemesterName = g.Semester?.Name ?? string.Empty,
        AssessmentType = g.AssessmentType,
        Score = g.Score,
        Status = g.Status.ToString(),
        CreatedByTeacherId = g.CreatedByTeacherId,
        CreatedByTeacherName = g.CreatedByTeacher?.FullName ?? string.Empty,
        PublishedAt = g.PublishedAt,
        IsApproved = g.IsApproved,
        ApprovedAt = g.ApprovedAt,
        CreatedAt = g.CreatedAt,
        UpdatedAt = g.UpdatedAt
    };
}
