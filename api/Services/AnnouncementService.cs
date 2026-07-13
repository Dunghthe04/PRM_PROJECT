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
        return items.Select(MapToDto).ToList();
    }

    public async Task<AnnouncementDto?> GetByIdAsync(int id)
    {
        var entity = await _announcementRepository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
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
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _announcementRepository.CreateAsync(entity);
        var loaded = await _announcementRepository.GetByIdAsync(created.Id);

        var recipientIds = await ResolveRecipientIdsAsync(created);
        await _notificationService.NotifyUsersAsync(
            recipientIds,
            $"Bảng tin: {created.Title}",
            created.Content,
            dto.SendPush);

        return (new CreateAnnouncementResultDto
        {
            Announcement = MapToDto(loaded!),
            NotifiedUserCount = recipientIds.Count,
            Message = $"Đã đăng bảng tin và gửi thông báo cho {recipientIds.Count} người dùng."
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
        else if (dto.Type == AnnouncementType.Class)
        {
            if (!dto.TargetClassId.HasValue)
                return "targetClassId là bắt buộc với bảng tin lớp.";

            if (await _classRepository.GetByIdAsync(dto.TargetClassId.Value) == null)
                return "Không tìm thấy lớp học.";

            if (actorRole == UserRole.Teacher)
            {
                var assignments = await _teacherAssignmentRepository.GetByTeacherAsync(actorId);
                if (!assignments.Any(ta => ta.ClassId == dto.TargetClassId.Value))
                    return "Bạn chưa được phân công dạy lớp này.";
            }
            else if (actorRole is not (UserRole.Admin or UserRole.HeadOfDept))
            {
                return "Không có quyền đăng bảng tin lớp.";
            }
        }
        else
        {
            return "Loại bảng tin không hợp lệ.";
        }

        return null;
    }

    private string? VerifyCanModify(Announcement entity, int actorId, UserRole role)
    {
        if (role is UserRole.Admin or UserRole.HeadOfDept)
            return null;

        if (role == UserRole.Teacher && entity.CreatedById == actorId)
            return null;

        return "Bạn không có quyền sửa/xóa bảng tin này.";
    }

    /// <summary>Lớp user được xem bảng tin Class.</summary>
    private async Task<List<int>> GetVisibleClassIdsAsync(int actorId, UserRole role)
    {
        if (role is UserRole.Admin or UserRole.HeadOfDept)
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

        return ids.ToList();
    }

    internal static AnnouncementDto MapToDto(Announcement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Content = a.Content,
        Type = a.Type.ToString(),
        TargetClassId = a.TargetClassId,
        TargetClassName = a.TargetClass?.Name,
        CreatedById = a.CreatedById,
        CreatedByName = a.CreatedBy?.FullName ?? string.Empty,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
