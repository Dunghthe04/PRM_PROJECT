using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>Nghiệp vụ bảng tin toàn trường / theo lớp (FR3.4, FR5.4 — Ngày 10 Bước 4).</summary>
public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetListAsync(int actorId, UserRole actorRole, AnnouncementListQueryDto query);
    Task<AnnouncementDto?> GetByIdAsync(int id);
    /// <summary>Lịch sử bảng tin do chính actor tạo (tab Đã gửi của GV).</summary>
    Task<List<AnnouncementDto>> GetMineAsync(int actorId);
    Task<(CreateAnnouncementResultDto? Result, string? Error)> CreateAsync(
        CreateUpdateAnnouncementDto dto, int actorId, UserRole actorRole);
    Task<(AnnouncementDto? Result, string? Error)> UpdateAsync(
        int id, CreateUpdateAnnouncementDto dto, int actorId, UserRole actorRole);
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IClassRepository _classRepository;
    private readonly ITeacherAssignmentRepository _teacherAssignmentRepository;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public AnnouncementService(
        IAnnouncementRepository announcementRepository,
        IClassRepository classRepository,
        ITeacherAssignmentRepository teacherAssignmentRepository,
        INotificationService notificationService,
        AppDbContext context)
    {
        _announcementRepository = announcementRepository;
        _classRepository = classRepository;
        _teacherAssignmentRepository = teacherAssignmentRepository;
        _notificationService = notificationService;
        _context = context;
    }

    public async Task<List<AnnouncementDto>> GetListAsync(
        int actorId, UserRole actorRole, AnnouncementListQueryDto query)
    {
        var classIds = await GetVisibleClassIdsAsync(actorId, actorRole);
        var items = await _announcementRepository.GetVisibleAsync(classIds, query.Type);
        await FillMissingSubjectsAsync(items);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AnnouncementDto?> GetByIdAsync(int id)
    {
        var entity = await _announcementRepository.GetByIdAsync(id);
        if (entity == null) return null;
        await FillMissingSubjectsAsync(new List<Announcement> { entity });
        return MapToDto(entity);
    }

    public async Task<List<AnnouncementDto>> GetMineAsync(int actorId)
    {
        var items = await _announcementRepository.GetByCreatorAsync(actorId);
        await FillMissingSubjectsAsync(items);
        return items.Select(MapToDto).ToList();
    }

    public async Task<(CreateAnnouncementResultDto? Result, string? Error)> CreateAsync(
        CreateUpdateAnnouncementDto dto, int actorId, UserRole actorRole)
    {
        var validateError = await ValidateDtoAsync(dto, actorId, actorRole, isUpdate: false);
        if (validateError != null) return (null, validateError);

        var entity = new Announcement
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            Type = dto.Type,
            TargetClassId = dto.Type == AnnouncementType.Class ? dto.TargetClassId : null,
            SubjectId = dto.Type == AnnouncementType.Class ? dto.SubjectId : null,
            TargetUserId = dto.Type == AnnouncementType.Teacher ? dto.TargetUserId : null,
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _announcementRepository.CreateAsync(entity);
        var loaded = await _announcementRepository.GetByIdAsync(created.Id);

        var recipientIds = await ResolveRecipientIdsAsync(created);
        var (notifTitle, notifMessage) = BuildNotifyCopy(loaded!);
        await _notificationService.NotifyUsersAsync(
            recipientIds,
            notifTitle,
            notifMessage,
            dto.SendPush);

        var scopeMsg = dto.Type switch
        {
            AnnouncementType.Teachers => "giáo viên",
            AnnouncementType.Teacher => "giáo viên được chọn",
            AnnouncementType.Global => "toàn trường",
            _ => "người dùng"
        };

        return (new CreateAnnouncementResultDto
        {
            Announcement = MapToDto(loaded!),
            NotifiedUserCount = recipientIds.Count,
            Message = $"Đã gửi thông báo tới {recipientIds.Count} {scopeMsg}."
        }, null);
    }

    public async Task<(AnnouncementDto? Result, string? Error)> UpdateAsync(
        int id, CreateUpdateAnnouncementDto dto, int actorId, UserRole actorRole)
    {
        var entity = await _announcementRepository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy bảng tin.");

        var permError = VerifyCanModify(entity, actorId, actorRole);
        if (permError != null) return (null, permError);

        var validateError = await ValidateDtoAsync(dto, actorId, actorRole, isUpdate: true);
        if (validateError != null) return (null, validateError);

        entity.Title = dto.Title.Trim();
        entity.Content = dto.Content.Trim();
        entity.Type = dto.Type;
        entity.TargetClassId = dto.Type == AnnouncementType.Class ? dto.TargetClassId : null;
        entity.SubjectId = dto.Type == AnnouncementType.Class ? dto.SubjectId : null;
        entity.TargetUserId = dto.Type == AnnouncementType.Teacher ? dto.TargetUserId : null;
        entity.UpdatedAt = DateTime.UtcNow;

        await _announcementRepository.UpdateAsync(entity);
        var updated = await _announcementRepository.GetByIdAsync(id);
        return (updated == null ? null : MapToDto(updated), null);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorId, UserRole actorRole)
    {
        var entity = await _announcementRepository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy bảng tin.");

        var permError = VerifyCanModify(entity, actorId, actorRole);
        if (permError != null) return (false, permError);

        await _announcementRepository.DeleteAsync(entity);
        return (true, "Đã xóa bảng tin.");
    }

    private async Task<string?> ValidateDtoAsync(
        CreateUpdateAnnouncementDto dto, int actorId, UserRole actorRole, bool isUpdate)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return "Tiêu đề là bắt buộc.";
        if (string.IsNullOrWhiteSpace(dto.Content))
            return "Nội dung là bắt buộc.";

        if (dto.Type == AnnouncementType.Global)
        {
            if (actorRole != UserRole.Admin)
                return "Chỉ Admin được đăng bảng tin toàn trường.";
        }
        else if (dto.Type == AnnouncementType.Teachers)
        {
            if (actorRole != UserRole.Admin)
                return "Chỉ Admin được gửi thông báo tới toàn bộ giáo viên.";
        }
        else if (dto.Type == AnnouncementType.Teacher)
        {
            if (actorRole != UserRole.Admin)
                return "Chỉ Admin được gửi thông báo tới một giáo viên.";
            if (!dto.TargetUserId.HasValue || dto.TargetUserId.Value <= 0)
                return "targetUserId (giáo viên nhận) là bắt buộc.";

            var teacher = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.TargetUserId.Value);
            if (teacher == null)
                return "Không tìm thấy giáo viên.";
            if (teacher.Role != UserRole.Teacher)
                return "Người nhận phải là giáo viên.";
            if (teacher.IsLocked)
                return "Tài khoản giáo viên đang bị khóa.";
        }
        else if (dto.Type == AnnouncementType.Class)
        {
            if (!dto.TargetClassId.HasValue)
                return "targetClassId là bắt buộc với bảng tin lớp.";

            if (await _classRepository.GetByIdAsync(dto.TargetClassId.Value) == null)
                return "Không tìm thấy lớp học.";

            if (actorRole == UserRole.Teacher)
            {
                var classId = dto.TargetClassId.Value;
                var isHomeroom = await _teacherAssignmentRepository
                    .IsHomeroomTeacherAsync(actorId, classId);
                var assignments = await _teacherAssignmentRepository.GetByTeacherAsync(actorId);
                var teachesClass = assignments.Any(ta => ta.ClassId == classId);

                // Chủ nhiệm: gửi tin lớp (môn tùy chọn). Bộ môn: bắt buộc môn mình dạy.
                if (!isHomeroom && !teachesClass)
                    return "Bạn chưa được phân công dạy lớp này và không phải chủ nhiệm.";

                if (!isHomeroom)
                {
                    if (!dto.SubjectId.HasValue || dto.SubjectId.Value <= 0)
                        return "Giáo viên bộ môn phải chọn môn học khi gửi thông báo.";

                    if (!assignments.Any(ta =>
                            ta.ClassId == classId && ta.SubjectId == dto.SubjectId.Value))
                        return "Bạn chưa được phân công môn này tại lớp.";
                }
                // Chủ nhiệm: gửi tin lớp, môn tùy chọn (không bắt buộc phân công môn).
            }
            else if (actorRole is not (UserRole.Admin))
            {
                return "Không có quyền đăng bảng tin lớp.";
            }

            if (dto.SubjectId.HasValue &&
                !await _context.Subjects.AnyAsync(s => s.Id == dto.SubjectId.Value))
                return "Không tìm thấy môn học.";
        }
        else
        {
            return "Loại bảng tin không hợp lệ.";
        }

        return null;
    }

    private string? VerifyCanModify(Announcement entity, int actorId, UserRole role)
    {
        if (role is UserRole.Admin)
            return null;

        if (role == UserRole.Teacher && entity.CreatedById == actorId)
            return null;

        return "Bạn không có quyền sửa/xóa bảng tin này.";
    }

    /// <summary>Lớp user được xem bảng tin Class.</summary>
    private async Task<List<int>> GetVisibleClassIdsAsync(int actorId, UserRole role)
    {
        if (role is UserRole.Admin)
            return await _context.Classes.Select(c => c.Id).ToListAsync();

        if (role == UserRole.Teacher)
        {
            var assignments = await _teacherAssignmentRepository.GetByTeacherAsync(actorId);
            return assignments.Select(ta => ta.ClassId).Distinct().ToList();
        }

        if (role == UserRole.Student)
        {
            return await _context.ClassStudents
                .Where(cs => cs.StudentId == actorId)
                .Select(cs => cs.ClassId)
                .ToListAsync();
        }

        if (role == UserRole.Parent)
        {
            var childIds = await _context.StudentParents
                .Where(sp => sp.ParentId == actorId)
                .Select(sp => sp.StudentId)
                .ToListAsync();

            return await _context.ClassStudents
                .Where(cs => childIds.Contains(cs.StudentId))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToListAsync();
        }

        return new List<int>();
    }

    /// <summary>Danh sách user nhận thông báo khi đăng bảng tin mới.</summary>
    private async Task<List<int>> ResolveRecipientIdsAsync(Announcement announcement)
    {
        var ids = new HashSet<int>();

        if (announcement.Type == AnnouncementType.Global)
        {
            var allActive = await _context.Users
                .Where(u => !u.IsLocked)
                .Select(u => u.Id)
                .ToListAsync();
            foreach (var id in allActive) ids.Add(id);
            // Người tạo không tự nhận lại thông báo của chính mình.
            ids.Remove(announcement.CreatedById);
            return ids.ToList();
        }

        // Admin → toàn bộ / 1 giáo viên: chỉ chuông Đã nhận, không lên Bảng tin.
        if (announcement.Type == AnnouncementType.Teachers)
        {
            var allTeacherIds = await _context.Users
                .Where(u => u.Role == UserRole.Teacher && !u.IsLocked)
                .Select(u => u.Id)
                .ToListAsync();
            foreach (var id in allTeacherIds) ids.Add(id);
            ids.Remove(announcement.CreatedById);
            return ids.ToList();
        }

        if (announcement.Type == AnnouncementType.Teacher)
        {
            if (announcement.TargetUserId.HasValue)
                ids.Add(announcement.TargetUserId.Value);
            ids.Remove(announcement.CreatedById);
            return ids.ToList();
        }

        if (!announcement.TargetClassId.HasValue)
            return ids.ToList();

        var classId = announcement.TargetClassId.Value;

        var studentIds = await _context.ClassStudents
            .Where(cs => cs.ClassId == classId)
            .Select(cs => cs.StudentId)
            .ToListAsync();

        foreach (var sid in studentIds) ids.Add(sid);

        var parentIds = await _context.StudentParents
            .Where(sp => studentIds.Contains(sp.StudentId))
            .Select(sp => sp.ParentId)
            .ToListAsync();

        foreach (var pid in parentIds) ids.Add(pid);

        var teacherIds = await _context.TeacherAssignments
            .Where(ta => ta.ClassId == classId)
            .Select(ta => ta.TeacherId)
            .ToListAsync();

        foreach (var tid in teacherIds) ids.Add(tid);

        // Người tạo TB không tự nhận lại thông báo của chính mình (tránh rối chuông).
        // GV vẫn xem lại TB đã gửi ở Bảng tin (lọc theo lớp mình dạy).
        ids.Remove(announcement.CreatedById);

        return ids.ToList();
    }

    /// <summary>
    /// Tin lớp cũ thiếu SubjectId → suy ra môn từ phân công GV–lớp (có thì ghi luôn DB).
    /// </summary>
    private async Task FillMissingSubjectsAsync(List<Announcement> items)
    {
        var missing = items
            .Where(a => a.Type == AnnouncementType.Class
                        && a.SubjectId == null
                        && a.TargetClassId != null)
            .ToList();
        if (missing.Count == 0) return;

        var teacherIds = missing.Select(a => a.CreatedById).Distinct().ToList();
        var classIds = missing.Select(a => a.TargetClassId!.Value).Distinct().ToList();

        var assignments = await _context.TeacherAssignments
            .AsNoTracking()
            .Include(ta => ta.Subject)
            .Where(ta => teacherIds.Contains(ta.TeacherId) && classIds.Contains(ta.ClassId))
            .ToListAsync();

        var dirty = false;
        foreach (var a in missing)
        {
            var matches = assignments
                .Where(ta => ta.TeacherId == a.CreatedById && ta.ClassId == a.TargetClassId)
                .ToList();
            if (matches.Count == 0) continue;

            // Một GV dạy nhiều môn cùng lớp → lấy môn đầu (đã đủ để hiện nhãn).
            var pick = matches[0];
            a.SubjectId = pick.SubjectId;
            a.Subject = pick.Subject;
            dirty = true;
        }

        if (dirty)
            await _context.SaveChangesAsync();
    }

    /// <summary>Tiêu đề/nội dung cho chuông cá nhân.</summary>
    private static (string Title, string Message) BuildNotifyCopy(Announcement a)
    {
        var className = a.TargetClass?.Name;
        var subjectName = a.Subject?.Name;
        var teacherName = a.CreatedBy?.FullName ?? "";
        var targetTeacher = a.TargetUser?.FullName ?? "";

        string title;
        if (a.Type == AnnouncementType.Global)
            title = $"Toàn trường: {a.Title}";
        else if (a.Type == AnnouncementType.Teachers)
            title = $"[Admin → Giáo viên] {a.Title}";
        else if (a.Type == AnnouncementType.Teacher)
            title = $"[Admin] {a.Title}";
        else if (!string.IsNullOrWhiteSpace(subjectName))
            title = $"[{subjectName}] {a.Title}";
        else
            title = $"TB lớp {className ?? ""}: {a.Title}".Trim();

        var meta = a.Type switch
        {
            AnnouncementType.Global => $"— Admin {teacherName}",
            AnnouncementType.Teachers => $"— Admin {teacherName} · Gửi toàn bộ giáo viên",
            AnnouncementType.Teacher => $"— Admin {teacherName}"
                + (string.IsNullOrWhiteSpace(targetTeacher) ? "" : $" · Tới {targetTeacher}"),
            _ => $"— GV {teacherName}"
                 + (string.IsNullOrWhiteSpace(className) ? "" : $" · {className}")
                 + (string.IsNullOrWhiteSpace(subjectName) ? "" : $" · {subjectName}")
        };

        return (title, $"{a.Content}\n\n{meta}");
    }

    internal static AnnouncementDto MapToDto(Announcement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Type = a.Type.ToString(),
        TargetClassId = a.TargetClassId,
        TargetClassName = a.TargetClass?.Name,
        SubjectId = a.SubjectId,
        SubjectName = a.Subject?.Name,
        TargetUserId = a.TargetUserId,
        TargetUserName = a.TargetUser?.FullName,
        CreatedById = a.CreatedById,
        CreatedByName = a.CreatedBy?.FullName ?? string.Empty,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
