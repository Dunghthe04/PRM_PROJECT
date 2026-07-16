namespace Api.Models;

/// <summary>Phạm vi bảng tin <see cref="Announcement"/> (FR3.4, FR5.4).</summary>
public enum AnnouncementType
{
    /// <summary>Toàn trường — TargetClassId = null.</summary>
    Global,

    /// <summary>Theo lớp — bắt buộc có TargetClassId.</summary>
    Class
}
