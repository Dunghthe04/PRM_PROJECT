using Api.Models;

namespace Api.DTOs;

// ─── FR3.4, FR5.4 — Bảng tin (Ngày 10) ─────────────────────────────────────

public class AnnouncementDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? TargetClassId { get; set; }
    public string? TargetClassName { get; set; }
    public int? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    /// <summary>GV nhận khi Type = Teacher.</summary>
    public int? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AnnouncementListQueryDto
{
    /// <summary>Lọc theo loại (tuỳ chọn).</summary>
    public AnnouncementType? Type { get; set; }
}

public class CreateUpdateAnnouncementDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; }

    /// <summary>Bắt buộc khi Type = Class.</summary>
    public int? TargetClassId { get; set; }

    /// <summary>Môn học (khuyến nghị khi GV gửi TB lớp).</summary>
    public int? SubjectId { get; set; }

    /// <summary>Bắt buộc khi Type = Teacher — id giáo viên nhận.</summary>
    public int? TargetUserId { get; set; }

    /// <summary>Gửi push notification khi đăng (mặc định true).</summary>
    public bool SendPush { get; set; } = true;
}

public class CreateAnnouncementResultDto
{
    public AnnouncementDto Announcement { get; set; } = null!;
    public int NotifiedUserCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
