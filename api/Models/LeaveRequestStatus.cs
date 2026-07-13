namespace Api.Models;

/// <summary>Trạng thái duyệt đơn xin nghỉ.</summary>
public enum LeaveRequestStatus
{
    /// <summary>Chờ GV duyệt — có thể hủy.</summary>
    Pending,

    /// <summary>Đã duyệt.</summary>
    Approved,

    /// <summary>Đã từ chối.</summary>
    Rejected
}
