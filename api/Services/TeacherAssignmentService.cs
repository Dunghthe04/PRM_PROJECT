using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ phân công giảng dạy (FR5.3): gán GV vào Lớp + Môn, luân chuyển.
/// </summary>
public interface ITeacherAssignmentService
{
    /// <summary>DS phân công (lọc classId / teacherId / subjectId).</summary>
    Task<List<TeacherAssignmentDto>> GetAllAsync(int? classId, int? teacherId, int? subjectId);

    /// <summary>Chi tiết 1 phân công.</summary>
    Task<TeacherAssignmentDto?> GetByIdAsync(int id);

    /// <summary>Tạo phân công mới.</summary>
    Task<(TeacherAssignmentDto? Result, string? Error)> CreateAsync(CreateUpdateTeacherAssignmentDto dto);

    /// <summary>Sửa phân công (luân chuyển GV / đổi môn-lớp).</summary>
    Task<(TeacherAssignmentDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateTeacherAssignmentDto dto);

    /// <summary>Gỡ phân công.</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);

    /// <summary>Các lớp + môn một GV đang phụ trách.</summary>
    Task<(List<TeacherClassDto>? Result, string? Error)> GetTeacherClassesAsync(int teacherId);
}

/// <summary>Implement ITeacherAssignmentService.</summary>
public class TeacherAssignmentService : ITeacherAssignmentService
{
    private readonly ITeacherAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISubjectRepository _subjectRepository;

    public TeacherAssignmentService(
        ITeacherAssignmentRepository assignmentRepository,
        IUserRepository userRepository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
        _classRepository = classRepository;
        _subjectRepository = subjectRepository;
    }

    /// <inheritdoc />
    public async Task<List<TeacherAssignmentDto>> GetAllAsync(int? classId, int? teacherId, int? subjectId)
    {
        var items = await _assignmentRepository.GetAllAsync(classId, teacherId, subjectId);
        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<TeacherAssignmentDto?> GetByIdAsync(int id)
    {
        var item = await _assignmentRepository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    /// <summary>
    /// Tạo phân công — Teacher phải role Teacher,
    /// Class + Subject tồn tại, không trùng (Class + Subject).
    /// </summary>
    public async Task<(TeacherAssignmentDto? Result, string? Error)> CreateAsync(CreateUpdateTeacherAssignmentDto dto)
    {
        var error = await ValidateRefsAsync(dto);
        if (error != null) return (null, error);

        var duplicate = await _assignmentRepository.FindByClassAndSubjectAsync(dto.ClassId, dto.SubjectId);
        if (duplicate != null)
            return (null, "Lớp này đã có giáo viên phụ trách môn này.");

        var entity = new TeacherAssignment
        {
            TeacherId = dto.TeacherId,
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId
        };

        var created = await _assignmentRepository.CreateAsync(entity);
        var full = await _assignmentRepository.GetByIdAsync(created.Id);
        return (MapToDto(full!), null);
    }

    /// <summary>Luân chuyển: đổi GV / lớp / môn của bản ghi phân công.</summary>
    public async Task<(TeacherAssignmentDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateTeacherAssignmentDto dto)
    {
        var entity = await _assignmentRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy phân công.");

        var error = await ValidateRefsAsync(dto);
        if (error != null) return (null, error);

        var duplicate = await _assignmentRepository.FindByClassAndSubjectAsync(dto.ClassId, dto.SubjectId);
        if (duplicate != null && duplicate.Id != id)
            return (null, "Lớp này đã có giáo viên phụ trách môn này.");

        entity.TeacherId = dto.TeacherId;
        entity.ClassId = dto.ClassId;
        entity.SubjectId = dto.SubjectId;

        await _assignmentRepository.UpdateAsync(entity);
        var full = await _assignmentRepository.GetByIdAsync(id);
        return (MapToDto(full!), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _assignmentRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy phân công.");

        await _assignmentRepository.DeleteAsync(entity);
        return (true, "Đã gỡ phân công giảng dạy.");
    }

    /// <summary>DS lớp–môn của 1 GV (phục vụ màn hình giáo viên).</summary>
    public async Task<(List<TeacherClassDto>? Result, string? Error)> GetTeacherClassesAsync(int teacherId)
    {
        var teacher = await _userRepository.GetUserByIdAsync(teacherId);
        if (teacher == null) return (null, "Không tìm thấy giáo viên.");

        if (teacher.Role is not (UserRole.Teacher))
            return (null, "User này không phải giáo viên.");

        var items = await _assignmentRepository.GetByTeacherAsync(teacherId);
        var result = items.Select(ta => new TeacherClassDto
        {
            AssignmentId = ta.Id,
            ClassId = ta.ClassId,
            ClassName = ta.Class.Name,
            SubjectId = ta.SubjectId,
            SubjectName = ta.Subject.Name,
            SubjectCode = ta.Subject.Code,
            SemesterId = ta.Class.SemesterId,
            SemesterName = ta.Class.Semester?.Name
        }).ToList();

        return (result, null);
    }

    /// <summary>Kiểm tra Teacher / Class / Subject tồn tại và role hợp lệ.</summary>
    private async Task<string?> ValidateRefsAsync(CreateUpdateTeacherAssignmentDto dto)
    {
        var teacher = await _userRepository.GetUserByIdAsync(dto.TeacherId);
        if (teacher == null)
            return "Không tìm thấy giáo viên.";

        // Admin không gán làm GV lớp
        if (teacher.Role is not (UserRole.Teacher))
            return "User này không phải giáo viên.";

        var cls = await _classRepository.GetByIdAsync(dto.ClassId);
        if (cls == null)
            return "Không tìm thấy lớp học.";

        var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId);
        if (subject == null)
            return "Không tìm thấy môn học.";

        return null;
    }

    private static TeacherAssignmentDto MapToDto(TeacherAssignment ta) => new()
    {
        Id = ta.Id,
        TeacherId = ta.TeacherId,
        TeacherName = ta.Teacher.FullName,
        TeacherPhone = ta.Teacher.Phone,
        ClassId = ta.ClassId,
        ClassName = ta.Class.Name,
        SubjectId = ta.SubjectId,
        SubjectName = ta.Subject.Name,
        SubjectCode = ta.Subject.Code
    };
}
