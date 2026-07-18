namespace Api.Models;

/// <summary>
/// Tài khoản người dùng hệ thống (RBAC: Admin / Teacher / Parent / Student).
/// Định danh đăng nhập: số điện thoại + mật khẩu (không dùng username riêng).
/// <para>
/// Quan hệ:
/// <list type="bullet">
/// <item><see cref="ParentLinks"/> — khi Role = Student: các liên kết PH gắn với HS này.</item>
/// <item><see cref="ChildLinks"/> — khi Role = Parent: các liên kết tới con (Switch Profile FR2.1).</item>
/// </list>
/// Các quan hệ khác (điểm danh, điểm, đơn nghỉ…) trỏ ngược về User qua FK, không khai báo collection ở đây.
/// </para>
/// </summary>
public class User
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Giữ cột Username cho tương thích nội bộ / JWT Name claim.
    /// Giá trị = Phone (đồng bộ lúc đăng ký / tạo user).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã hash BCrypt — không bao giờ trả về client.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Họ tên hiển thị.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>URL avatar (upload qua /api/account/avatar hoặc /api/files).</summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// Email liên hệ — không dùng để đăng nhập.
    /// Dùng nhận OTP quên mật khẩu (FR1.2). Unique khi có giá trị.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>Số điện thoại — định danh chính để đăng ký / đăng nhập / nhận OTP (unique).</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>True sau khi xác thực OTP đăng ký thành công (hoặc Admin tạo hộ).</summary>
    public bool IsPhoneVerified { get; set; }

    /// <summary>
    /// True = tài khoản bị Admin khóa (FR5.1).
    /// User bị khóa không được đăng nhập dù đúng SĐT + mật khẩu.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>Vai trò RBAC — xem <see cref="UserRole"/>.</summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Navigation (N–N qua <see cref="StudentParent"/>):
    /// phía Học sinh → danh sách phụ huynh gắn với HS này
    /// (<c>StudentParent.StudentId</c> = Id).
    /// </summary>
    public ICollection<StudentParent> ParentLinks { get; set; } = new List<StudentParent>();

    /// <summary>
    /// Navigation (N–N qua <see cref="StudentParent"/>):
    /// phía Phụ huynh → danh sách con
    /// (<c>StudentParent.ParentId</c> = Id). Dùng cho Switch Profile.
    /// </summary>
    public ICollection<StudentParent> ChildLinks { get; set; } = new List<StudentParent>();
}
