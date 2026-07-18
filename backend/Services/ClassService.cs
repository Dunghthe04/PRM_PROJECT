using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ lớp học (FR5.2): CRUD lớp + gán/bỏ học sinh.
/// </summary>
public interface IClassService
{
    /// <summary>Danh sách lớp (lọc theo semesterId tùy chọn).</summary>
    Task<List<ClassDto>> GetAllAsync(int? semesterId = null);

    /// <summary>Chi tiết lớp.</summary>
    Task<ClassDto?> GetByIdAsync(int id);

    /// <summary>Tạo lớp thuộc 1 kỳ học.</summary>
    Task<(ClassDto? Result, string? Error)> CreateAsync(CreateUpdateClassDto dto);

    /// <summary>Cập nhật tên / kỳ của lớp.</summary>
    Task<(ClassDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateClassDto dto);

    /// <summary>Xóa lớp (chặn nếu còn HS hoặc phân công GV).</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);

    /// <summary>DS học sinh trong lớp.</summary>
    Task<(List<ClassStudentDto>? Result, string? Error)> GetStudentsAsync(int classId);

    /// <summary>Thêm HS (role Student) vào lớp.</summary>
    Task<(bool Success, string Message)> AddStudentAsync(int classId, int studentId);

    /// <summary>Bỏ HS khỏi lớp.</summary>
    Task<(bool Success, string Message)> RemoveStudentAsync(int classId, int studentId);
}

/// <summary>Implement IClassService.</summary>
public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly AppDbContext _context;

    public ClassService(
        IClassRepository classRepository,
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        AppDbContext context)
    {
        _classRepository = classRepository;
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<ClassDto>> GetAllAsync(int? semesterId = null)
    {
        var items = await _classRepository.GetAllAsync(semesterId);
        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ClassDto?> GetByIdAsync(int id)
    {
        var item = await _classRepository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    /// <summary>Tạo lớp — Name bắt buộc, SemesterId phải tồn tại.</summary>
    public async Task<(ClassDto? Result, string? Error)> CreateAsync(CreateUpdateClassDto dto)
    {
        var error = await ValidateAsync(dto);
        if (error != null) return (null, error);

        var entity = new Class
        {
            Name = dto.Name.Trim(),
            SemesterId = dto.SemesterId,
            HomeroomTeacherId = dto.HomeroomTeacherId
        };

        var created = await _classRepository.CreateAsync(entity);
        // Reload để có SemesterName / HomeroomTeacher
        var full = await _classRepository.GetByIdAsync(created.Id);
        return (MapToDto(full!), null);
    }

    /// <summary>Sửa lớp — đổi tên, kỳ, hoặc GV chủ nhiệm.</summary>
    public async Task<(ClassDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateClassDto dto)
    {
        var entity = await _classRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy lớp học.");

        var error = await ValidateAsync(dto);
        if (error != null) return (null, error);

        entity.Name = dto.Name.Trim();
        entity.SemesterId = dto.SemesterId;
        entity.HomeroomTeacherId = dto.HomeroomTeacherId;

        await _classRepository.UpdateAsync(entity);
        var full = await _classRepository.GetByIdAsync(id);
        return (MapToDto(full!), null);
    }

    /// <summary>
    /// Xóa lớp — chặn nếu còn học sinh hoặc phân công GV.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _classRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy lớp học.");

        if (entity.ClassStudents.Count > 0)
            return (false, "Không thể xóa: lớp còn học sinh. Hãy bỏ HS trước.");

        var hasAssignment = await _context.TeacherAssignments.AnyAsync(ta => ta.ClassId == id);
        var hasTimetable = await _context.TimetableSlots.AnyAsync(t => t.ClassId == id);
        if (hasAssignment || hasTimetable)
            return (false, "Không thể xóa: lớp đang có phân công giảng dạy hoặc TKB.");

        await _classRepository.DeleteAsync(entity);
        return (true, "Đã xóa lớp học.");
    }

    /// <inheritdoc />
    public async Task<(List<ClassStudentDto>? Result, string? Error)> GetStudentsAsync(int classId)
    {
        var cls = await _classRepository.GetByIdAsync(classId);
        if (cls == null) return (null, "Không tìm thấy lớp học.");

        var links = await _classRepository.GetStudentsAsync(classId);
        var result = links.Select(cs => new ClassStudentDto
        {
            StudentId = cs.StudentId,
            Phone = cs.Student.Phone,
            FullName = cs.Student.FullName
        }).ToList();

        return (result, null);
    }

    /// <summary>
    /// Gán HS vào lớp — user phải tồn tại, role = Student, chưa có trong lớp.
    /// </summary>
    public async Task<(bool Success, string Message)> AddStudentAsync(int classId, int studentId)
    {
        var cls = await _classRepository.GetByIdAsync(classId);
        if (cls == null) return (false, "Không tìm thấy lớp học.");

        var student = await _userRepository.GetUserByIdAsync(studentId);
        if (student == null) return (false, "Không tìm thấy học sinh.");

        if (student.Role != UserRole.Student)
            return (false, "User này không phải học sinh.");

        if (await _classRepository.StudentInClassAsync(classId, studentId))
            return (false, "Học sinh đã có trong lớp.");

        await _classRepository.AddStudentAsync(new ClassStudent
        {
            ClassId = classId,
            StudentId = studentId
        });

        return (true, "Đã thêm học sinh vào lớp.");
    }

    /// <summary>Bỏ liên kết Class–Student.</summary>
    public async Task<(bool Success, string Message)> RemoveStudentAsync(int classId, int studentId)
    {
        var cls = await _classRepository.GetByIdAsync(classId);
        if (cls == null) return (false, "Không tìm thấy lớp học.");

        var link = await _classRepository.GetStudentLinkAsync(classId, studentId);
        if (link == null) return (false, "Học sinh không có trong lớp.");

        await _classRepository.RemoveStudentAsync(link);
        return (true, "Đã bỏ học sinh khỏi lớp.");
    }

    private async Task<string?> ValidateAsync(CreateUpdateClassDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Tên lớp không được để trống.";

        var semester = await _semesterRepository.GetByIdAsync(dto.SemesterId);
        if (semester == null)
            return "Kỳ học không tồn tại.";

        if (dto.HomeroomTeacherId.HasValue)
        {
            var teacher = await _userRepository.GetUserByIdAsync(dto.HomeroomTeacherId.Value);
            if (teacher == null)
                return "Không tìm thấy giáo viên chủ nhiệm.";
            if (teacher.Role != UserRole.Teacher)
                return "Chủ nhiệm phải là tài khoản giáo viên.";
            if (teacher.IsLocked)
                return "Tài khoản giáo viên chủ nhiệm đang bị khóa.";
        }

        return null;
    }

    private static ClassDto MapToDto(Class c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        SemesterId = c.SemesterId,
        SemesterName = c.Semester?.Name,
        StudentCount = c.ClassStudents?.Count ?? 0,
        HomeroomTeacherId = c.HomeroomTeacherId,
        HomeroomTeacherName = c.HomeroomTeacher?.FullName
    };
}
