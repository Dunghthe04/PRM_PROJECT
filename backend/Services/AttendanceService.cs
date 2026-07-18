using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ điểm danh P/A/L (FR3.1 — Ngày 7 Bước 4).
/// Validate → Repository → map DTO. GV phải được phân công dạy lớp.
/// </summary>
public interface IAttendanceService
{
    /// <summary>GET /api/attendance — GV xem điểm danh lớp theo ngày.</summary>
    Task<(List<AttendanceDto>? Result, string? Error)> GetByClassAndDateAsync(
        AttendanceListQueryDto query, int actorId, UserRole actorRole);

    /// <summary>GET /api/attendance/me — HS/PH xem lịch sử chuyên cần.</summary>
    Task<List<AttendanceDto>> GetMyAttendanceAsync(int actorId, UserRole actorRole, MyAttendanceQueryDto query);

    /// <summary>GET /api/attendance/summary — thống kê chuyên cần lớp.</summary>
    Task<(AttendanceSummaryDto? Result, string? Error)> GetSummaryAsync(
        AttendanceSummaryQueryDto query, int actorId, UserRole actorRole);

    /// <summary>POST /api/attendance/batch — điểm danh hàng loạt.</summary>
    Task<(BatchAttendanceResultDto? Result, string? Error)> BatchUpsertAsync(
        BatchAttendanceDto dto, int teacherId, UserRole actorRole);

    /// <summary>PUT /api/attendance/{id} — sửa 1 bản ghi.</summary>
    Task<(AttendanceDto? Result, string? Error)> UpdateAsync(
        int id, UpdateAttendanceDto dto, int actorId, UserRole actorRole);
}

/// <summary>Implement IAttendanceService.</summary>
public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IClassRepository _classRepository;
    private readonly ITeacherAssignmentRepository _teacherAssignmentRepository;
    private readonly AppDbContext _context;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IClassRepository classRepository,
        ITeacherAssignmentRepository teacherAssignmentRepository,
        AppDbContext context)
    {
        _attendanceRepository = attendanceRepository;
        _classRepository = classRepository;
        _teacherAssignmentRepository = teacherAssignmentRepository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(List<AttendanceDto>? Result, string? Error)> GetByClassAndDateAsync(
        AttendanceListQueryDto query, int actorId, UserRole actorRole)
    {
        if (query.ClassId <= 0)
            return (null, "classId là bắt buộc.");

        var classError = await ValidateClassExistsAsync(query.ClassId);
        if (classError != null) return (null, classError);

        var permError = await VerifyTeacherCanRecordAsync(actorId, actorRole, query.ClassId);
        if (permError != null) return (null, permError);

        var date = (query.Date ?? DateTime.UtcNow).Date;
        var items = await _attendanceRepository.GetByClassAndDateAsync(query.ClassId, date);
        return (items.Select(MapToDto).ToList(), null);
    }

    /// <inheritdoc />
    public async Task<List<AttendanceDto>> GetMyAttendanceAsync(
        int actorId, UserRole actorRole, MyAttendanceQueryDto query)
    {
        var studentIds = await ResolveStudentIdsAsync(actorId, actorRole);
        if (studentIds.Count == 0)
            return new List<AttendanceDto>();

        var all = new List<Attendance>();
        foreach (var studentId in studentIds)
        {
            var records = await _attendanceRepository.GetByStudentAsync(studentId, query.From, query.To);
            all.AddRange(records);
        }

        return all
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Student.FullName)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<(AttendanceSummaryDto? Result, string? Error)> GetSummaryAsync(
        AttendanceSummaryQueryDto query, int actorId, UserRole actorRole)
    {
        if (query.ClassId <= 0)
            return (null, "classId là bắt buộc.");

        var cls = await _classRepository.GetByIdAsync(query.ClassId);
        if (cls == null) return (null, "Không tìm thấy lớp học.");

        var permError = await VerifyTeacherCanRecordAsync(actorId, actorRole, query.ClassId);
        if (permError != null) return (null, permError);

        var from = query.From?.Date;
        var to = query.To?.Date;

        var records = await _attendanceRepository.GetByClassAsync(query.ClassId, from, to);
        var students = await _classRepository.GetStudentsAsync(query.ClassId);

        var summary = new AttendanceSummaryDto
        {
            ClassId = cls.Id,
            ClassName = cls.Name,
            From = from,
            To = to,
            Students = students.Select(cs =>
            {
                var studentRecords = records.Where(r => r.StudentId == cs.StudentId).ToList();
                var present = studentRecords.Count(r => r.Status == AttendanceStatus.Present);
                var absent = studentRecords.Count(r => r.Status == AttendanceStatus.Absent);
                var late = studentRecords.Count(r => r.Status == AttendanceStatus.Late);
                var total = studentRecords.Count;
                var rate = total == 0 ? 0 : Math.Round((present + late) * 100.0 / total, 2);

                return new StudentAttendanceSummaryDto
                {
                    StudentId = cs.StudentId,
                    StudentName = cs.Student?.FullName ?? string.Empty,
                    TotalRecords = total,
                    PresentCount = present,
                    AbsentCount = absent,
                    LateCount = late,
                    AttendanceRate = rate
                };
            })
            .OrderBy(s => s.StudentName)
            .ToList()
        };

        return (summary, null);
    }

    /// <inheritdoc />
    public async Task<(BatchAttendanceResultDto? Result, string? Error)> BatchUpsertAsync(
        BatchAttendanceDto dto, int teacherId, UserRole actorRole)
    {
        if (dto.ClassId <= 0)
            return (null, "classId là bắt buộc.");

        if (dto.Entries.Count == 0)
            return (null, "Danh sách điểm danh không được trống.");

        var classError = await ValidateClassExistsAsync(dto.ClassId);
        if (classError != null) return (null, classError);

        var permError = await VerifyTeacherCanRecordAsync(teacherId, actorRole, dto.ClassId);
        if (permError != null) return (null, permError);

        var date = dto.Date.Date;
        var result = new BatchAttendanceResultDto();
        var savedIds = new List<int>();

        foreach (var entry in dto.Entries)
        {
            if (!Enum.IsDefined(typeof(AttendanceStatus), entry.Status))
                return (null, $"HS Id {entry.StudentId}: trạng thái không hợp lệ.");

            if (!await _classRepository.StudentInClassAsync(dto.ClassId, entry.StudentId))
                return (null, $"Học sinh Id {entry.StudentId} không thuộc lớp này.");

            var existing = await _attendanceRepository.FindByKeyAsync(dto.ClassId, entry.StudentId, date);

            if (existing != null)
            {
                existing.Status = entry.Status;
                existing.RecordedByTeacherId = teacherId;
                existing.UpdatedAt = DateTime.UtcNow;
                await _attendanceRepository.UpdateAsync(existing);
                result.UpdatedCount++;
                savedIds.Add(existing.Id);
            }
            else
            {
                var attendance = new Attendance
                {
                    ClassId = dto.ClassId,
                    StudentId = entry.StudentId,
                    Date = date,
                    Status = entry.Status,
                    RecordedByTeacherId = teacherId,
                    CreatedAt = DateTime.UtcNow
                };
                var created = await _attendanceRepository.CreateAsync(attendance);
                result.CreatedCount++;
                savedIds.Add(created.Id);
            }
        }

        result.Message = $"Đã lưu điểm danh: {result.CreatedCount} mới, {result.UpdatedCount} cập nhật.";
        foreach (var id in savedIds)
        {
            var record = await _attendanceRepository.GetByIdAsync(id);
            if (record != null) result.Records.Add(MapToDto(record));
        }

        return (result, null);
    }

    /// <inheritdoc />
    public async Task<(AttendanceDto? Result, string? Error)> UpdateAsync(
        int id, UpdateAttendanceDto dto, int actorId, UserRole actorRole)
    {
        var attendance = await _attendanceRepository.GetByIdAsync(id);
        if (attendance == null) return (null, "Không tìm thấy bản ghi điểm danh.");

        var permError = await VerifyTeacherCanRecordAsync(actorId, actorRole, attendance.ClassId);
        if (permError != null) return (null, permError);

        if (!Enum.IsDefined(typeof(AttendanceStatus), dto.Status))
            return (null, "Trạng thái không hợp lệ.");

        attendance.Status = dto.Status;
        attendance.RecordedByTeacherId = actorId;
        attendance.UpdatedAt = DateTime.UtcNow;
        await _attendanceRepository.UpdateAsync(attendance);

        var updated = await _attendanceRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    /// <summary>HS xem của mình; PH xem tất cả con liên kết.</summary>
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

    private async Task<string?> ValidateClassExistsAsync(int classId)
    {
        return await _classRepository.GetByIdAsync(classId) == null
            ? "Không tìm thấy lớp học."
            : null;
    }

    /// <summary>Chỉ GV chủ nhiệm lớp (hoặc Admin) được điểm danh cả lớp.</summary>
    private async Task<string?> VerifyTeacherCanRecordAsync(int actorId, UserRole role, int classId)
    {
        if (role is UserRole.Admin)
            return null;

        if (role != UserRole.Teacher)
            return "Không có quyền thao tác điểm danh.";

        var isHomeroom = await _teacherAssignmentRepository
            .IsHomeroomTeacherAsync(actorId, classId);
        if (!isHomeroom)
            return "Chỉ giáo viên chủ nhiệm lớp được điểm danh.";

        return null;
    }

    /// <summary>Map entity → AttendanceDto.</summary>
    internal static AttendanceDto MapToDto(Attendance a) => new()
    {
        Id = a.Id,
        ClassId = a.ClassId,
        ClassName = a.Class?.Name ?? string.Empty,
        StudentId = a.StudentId,
        StudentName = a.Student?.FullName ?? string.Empty,
        StudentPhone = a.Student?.Phone ?? string.Empty,
        Date = a.Date,
        Status = a.Status.ToString(),
        RecordedByTeacherId = a.RecordedByTeacherId,
        RecordedByTeacherName = a.RecordedByTeacher?.FullName ?? string.Empty,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
