using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ thời khóa biểu (FR2.3): CRUD tiết + xem theo tuần (lớp / HS / GV).
/// </summary>
public interface ITimetableService
{
    /// <summary>TKB theo lớp (weekStart tùy chọn — mặc định thứ Hai tuần hiện tại).</summary>
    Task<(WeeklyTimetableDto? Result, string? Error)> GetByClassAsync(int classId, DateTime? weekStart);

    /// <summary>
    /// TKB của Học sinh đang login, hoặc của 1 người con (khi Phụ huynh gọi).
    /// - HS: xem TKB của chính mình (bỏ qua studentId).
    /// - PH: truyền studentId = id của con muốn xem (phải là con đã liên kết).
    ///   Nếu PH không truyền studentId → tự lấy người con đầu tiên.
    /// </summary>
    Task<(WeeklyTimetableDto? Result, string? Error)> GetMyAsync(
        int userId, UserRole role, int? studentId, DateTime? weekStart);

    /// <summary>Lịch dạy của GV đang login.</summary>
    Task<(WeeklyTimetableDto? Result, string? Error)> GetTeacherAsync(int teacherId, DateTime? weekStart);

    /// <summary>Tạo tiết mới.</summary>
    Task<(TimetableSlotDto? Result, string? Error)> CreateAsync(CreateUpdateTimetableSlotDto dto);

    /// <summary>Sửa tiết.</summary>
    Task<(TimetableSlotDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateTimetableSlotDto dto);

    /// <summary>Xóa tiết.</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

/// <summary>Implement ITimetableService.</summary>
public class TimetableService : ITimetableService
{
    private static readonly string[] DayNames =
        { "", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ Nhật" };

    private readonly ITimetableRepository _timetableRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IUserRepository _userRepository;
    private readonly AppDbContext _context;

    public TimetableService(
        ITimetableRepository timetableRepository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository,
        IUserRepository userRepository,
        AppDbContext context)
    {
        _timetableRepository = timetableRepository;
        _classRepository = classRepository;
        _subjectRepository = subjectRepository;
        _userRepository = userRepository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(WeeklyTimetableDto? Result, string? Error)> GetByClassAsync(int classId, DateTime? weekStart)
    {
        var cls = await _classRepository.GetByIdAsync(classId);
        if (cls == null) return (null, "Không tìm thấy lớp học.");

        var slots = await _timetableRepository.GetByClassAsync(classId);
        return (BuildWeekly(slots, weekStart), null);
    }

    /// <summary>HS: lấy mọi lớp đang học → gộp TKB.</summary>
    public async Task<(WeeklyTimetableDto? Result, string? Error)> GetMyAsync(
        int userId, UserRole role, int? studentId, DateTime? weekStart)
    {
        // Xác định "học sinh mục tiêu" cần lấy TKB.
        // - HS: chính là bản thân.
        // - PH: là người con được chọn (studentId) và phải là con đã liên kết.
        var (targetStudentId, error) = await ResolveTargetStudentAsync(userId, role, studentId);
        if (error != null) return (null, error);

        // Lấy các lớp mà học sinh mục tiêu đang thuộc về.
        var classIds = await _context.ClassStudents
            .Where(cs => cs.StudentId == targetStudentId)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        // Chưa xếp lớp → trả TKB rỗng (không coi là lỗi).
        if (classIds.Count == 0)
            return (BuildWeekly(new List<TimetableSlot>(), weekStart), null);

        var slots = await _timetableRepository.GetByClassIdsAsync(classIds);
        return (BuildWeekly(slots, weekStart), null);
    }

    /// <summary>
    /// Quy đổi (userId, role, studentId) → id học sinh cần xem dữ liệu.
    /// Nhận:
    ///   - userId: id người đang đăng nhập.
    ///   - role: vai trò (Student / Parent).
    ///   - requestedStudentId: id con mà PH muốn xem (null nếu HS hoặc PH chưa chọn).
    /// Trả về: (targetStudentId, error). error != null nếu không hợp lệ.
    /// </summary>
    private async Task<(int TargetStudentId, string? Error)> ResolveTargetStudentAsync(
        int userId, UserRole role, int? requestedStudentId)
    {
        if (role == UserRole.Student)
            return (userId, null);

        if (role == UserRole.Parent)
        {
            // Danh sách id các con đã liên kết với phụ huynh này.
            var childIds = await _context.StudentParents
                .Where(sp => sp.ParentId == userId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            if (childIds.Count == 0)
                return (0, "Tài khoản phụ huynh chưa liên kết học sinh nào.");

            // PH có truyền studentId → phải là con của mình.
            if (requestedStudentId.HasValue)
            {
                if (!childIds.Contains(requestedStudentId.Value))
                    return (0, "Học sinh không thuộc quyền quản lý của phụ huynh.");
                return (requestedStudentId.Value, null);
            }

            // Không truyền → mặc định lấy người con đầu tiên.
            return (childIds[0], null);
        }

        return (0, "Vai trò không được phép truy cập dữ liệu học tập cá nhân.");
    }

    /// <summary>GV: các tiết có TeacherId = user hiện tại.</summary>
    public async Task<(WeeklyTimetableDto? Result, string? Error)> GetTeacherAsync(int teacherId, DateTime? weekStart)
    {
        var user = await _userRepository.GetUserByIdAsync(teacherId);
        if (user == null) return (null, "Không tìm thấy giáo viên.");

        if (user.Role is not (UserRole.Teacher or UserRole.HeadOfDept))
            return (null, "Endpoint này dành cho giáo viên.");

        var slots = await _timetableRepository.GetByTeacherAsync(teacherId);
        return (BuildWeekly(slots, weekStart), null);
    }

    /// <summary>
    /// Tạo tiết — validate DayOfWeek/Period, Class/Subject/Teacher tồn tại,
    /// không trùng (Class + Thứ + Tiết).
    /// </summary>
    public async Task<(TimetableSlotDto? Result, string? Error)> CreateAsync(CreateUpdateTimetableSlotDto dto)
    {
        var error = await ValidateAsync(dto);
        if (error != null) return (null, error);

        var conflict = await _timetableRepository.FindConflictAsync(dto.ClassId, dto.DayOfWeek, dto.Period);
        if (conflict != null)
            return (null, "Lớp đã có tiết học vào thứ/tiết này.");

        var entity = new TimetableSlot
        {
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            DayOfWeek = dto.DayOfWeek,
            Period = dto.Period,
            Room = dto.Room.Trim()
        };

        var created = await _timetableRepository.CreateAsync(entity);
        var full = await _timetableRepository.GetByIdAsync(created.Id);
        return (MapToDto(full!), null);
    }

    /// <inheritdoc />
    public async Task<(TimetableSlotDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateTimetableSlotDto dto)
    {
        var entity = await _timetableRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy tiết học.");

        var error = await ValidateAsync(dto);
        if (error != null) return (null, error);

        var conflict = await _timetableRepository.FindConflictAsync(dto.ClassId, dto.DayOfWeek, dto.Period, id);
        if (conflict != null)
            return (null, "Lớp đã có tiết học vào thứ/tiết này.");

        entity.ClassId = dto.ClassId;
        entity.SubjectId = dto.SubjectId;
        entity.TeacherId = dto.TeacherId;
        entity.DayOfWeek = dto.DayOfWeek;
        entity.Period = dto.Period;
        entity.Room = dto.Room.Trim();

        await _timetableRepository.UpdateAsync(entity);
        var full = await _timetableRepository.GetByIdAsync(id);
        return (MapToDto(full!), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _timetableRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy tiết học.");

        await _timetableRepository.DeleteAsync(entity);
        return (true, "Đã xóa tiết học.");
    }

    private async Task<string?> ValidateAsync(CreateUpdateTimetableSlotDto dto)
    {
        if (dto.DayOfWeek is < 1 or > 7)
            return "DayOfWeek phải từ 1 (Thứ Hai) đến 7 (Chủ Nhật).";

        if (dto.Period < 1 || dto.Period > 15)
            return "Period (tiết) phải từ 1 đến 15.";

        if (string.IsNullOrWhiteSpace(dto.Room))
            return "Phòng học không được để trống.";

        if (await _classRepository.GetByIdAsync(dto.ClassId) == null)
            return "Không tìm thấy lớp học.";

        if (await _subjectRepository.GetByIdAsync(dto.SubjectId) == null)
            return "Không tìm thấy môn học.";

        var teacher = await _userRepository.GetUserByIdAsync(dto.TeacherId);
        if (teacher == null)
            return "Không tìm thấy giáo viên.";

        if (teacher.Role is not (UserRole.Teacher or UserRole.HeadOfDept))
            return "User này không phải giáo viên.";

        return null;
    }

    /// <summary>Gói slots + khoảng tuần (Thứ Hai → Chủ Nhật).</summary>
    private static WeeklyTimetableDto BuildWeekly(List<TimetableSlot> slots, DateTime? weekStart)
    {
        var start = NormalizeToMonday(weekStart ?? DateTime.UtcNow);
        return new WeeklyTimetableDto
        {
            WeekStart = start.Date,
            WeekEnd = start.Date.AddDays(6),
            Slots = slots.Select(MapToDto).ToList()
        };
    }

    /// <summary>Đưa về Thứ Hai của tuần.</summary>
    private static DateTime NormalizeToMonday(DateTime date)
    {
        var d = date.Date;
        // .NET: Sunday=0 … Saturday=6 → chuyển về Monday=đầu tuần
        var diff = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-diff);
    }

    private static TimetableSlotDto MapToDto(TimetableSlot t) => new()
    {
        Id = t.Id,
        ClassId = t.ClassId,
        ClassName = t.Class.Name,
        SubjectId = t.SubjectId,
        SubjectName = t.Subject.Name,
        SubjectCode = t.Subject.Code,
        TeacherId = t.TeacherId,
        TeacherName = t.Teacher.FullName,
        DayOfWeek = t.DayOfWeek,
        DayName = t.DayOfWeek is >= 1 and <= 7 ? DayNames[t.DayOfWeek] : "",
        Period = t.Period,
        Room = t.Room
    };
}
