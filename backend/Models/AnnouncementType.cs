namespace Api.Models;

/// <summary>Phạm vi bảng tin / thông báo Admin (FR3.4, FR5.4).</summary>
public enum AnnouncementType
{
    /// <summary>Toàn trường — hiện Bảng tin công khai + chuông mọi role.</summary>
    Global,

    /// <summary>Theo lớp — bắt buộc TargetClassId; hiện Bảng tin lớp.</summary>
    Class,

    /// <summary>Admin → toàn bộ giáo viên. Chỉ chuông Đã nhận, không lên Bảng tin.</summary>
    Teachers,

    /// <summary>Admin → 1 giáo viên. Bắt buộc TargetUserId. Chỉ chuông Đã nhận.</summary>
    Teacher
}
