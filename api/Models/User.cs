namespace Api.Models;

/// <summary>
/// Tài khoản người dùng hệ thống (5 role RBAC).
/// Định danh đăng nhập: Số điện thoại + mật khẩu (không dùng username).
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Giữ cột Username cho tương thích nội bộ / JWT Name claim.
    /// Giá trị = Phone (đồng bộ lúc đăng ký).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Email liên hệ (tuỳ chọn) — không dùng để login/OTP.</summary>
    public string? Email { get; set; }

    /// <summary>Số điện thoại — định danh chính để đăng ký / đăng nhập / nhận OTP.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>True sau khi xác thực OTP đăng ký thành công.</summary>
    public bool IsPhoneVerified { get; set; }

    public UserRole Role { get; set; }

    // Parent ↔ Student (nhiều-nhiều) phục vụ Switch Profile (FR2.1)
    public ICollection<StudentParent> ParentLinks { get; set; } = new List<StudentParent>();
    public ICollection<StudentParent> ChildLinks { get; set; } = new List<StudentParent>();
}
