namespace Api.Models;

/// <summary>
/// Vai trò RBAC của <see cref="User"/>.
/// Giữ số cố định để không vỡ dữ liệu cũ khi bỏ HeadOfDept
/// (Admin=0, Teacher=2, Parent=3, Student=4 — bỏ giá trị 1).
/// </summary>
public enum UserRole
{
    /// <summary>Quản trị hệ thống — CRUD danh mục, user, học phí, báo cáo.</summary>
    Admin = 0,

    /// <summary>Giáo viên — điểm danh, duyệt đơn nghỉ, gửi TB lớp.</summary>
    Teacher = 2,

    /// <summary>Phụ huynh — xem thông tin con (Switch Profile), học phí, đơn nghỉ.</summary>
    Parent = 3,

    /// <summary>Học sinh — TKB, điểm, chuyên cần, đơn nghỉ.</summary>
    Student = 4
}
