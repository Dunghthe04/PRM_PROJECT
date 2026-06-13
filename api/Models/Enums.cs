namespace Api.Models;

public enum UserRole
{
    Admin,
    HeadOfDept,
    Teacher,
    Parent,
    Student
}

public enum AttendanceStatus
{
    Present, // Có mặt
    Absent,  // Vắng mặt
    Late     // Đi muộn
}

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public enum AnnouncementType
{
    Global,
    Class
}
