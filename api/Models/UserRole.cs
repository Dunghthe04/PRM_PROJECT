namespace Api.Models;

/// <summary>
/// Vai trò RBAC. Giữ số cố định để không vỡ dữ liệu cũ khi bỏ HeadOfDept
/// (Admin=0, Teacher=2, Parent=3, Student=4 — bỏ giá trị 1).
/// </summary>
public enum UserRole
{
    Admin = 0,
    Teacher = 2,
    Parent = 3,
    Student = 4
}
