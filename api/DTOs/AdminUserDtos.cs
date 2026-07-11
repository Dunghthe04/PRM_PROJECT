using Api.Models;

namespace Api.DTOs;

// ─── FR5.1 — Admin quản lý người dùng (Ngày 5 Bước 2) ─────────────────────
//
// DTO = Data Transfer Object: lớp trung gian giữa Client ↔ API.
// Mục đích:
//   • Không lộ entity DB (PasswordHash, navigation properties...)
//   • Định nghĩa rõ body request/response cho từng endpoint Admin
//   • Validate dữ liệu đầu vào trước khi vào Service

/// <summary>
/// Query string cho GET /api/users — phân trang + lọc.
/// ASP.NET Core tự bind từ ?page=1&amp;pageSize=20&amp;role=Student&amp;search=0912
/// </summary>
public class UserListQueryDto
{
    /// <summary>Trang hiện tại (bắt đầu từ 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số bản ghi mỗi trang (mặc định 20, tối đa 100).</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Lọc theo vai trò — null = tất cả role.</summary>
    public UserRole? Role { get; set; }

    /// <summary>Tìm theo SĐT hoặc họ tên (tuỳ chọn).</summary>
    public string? Search { get; set; }

    /// <summary>Lọc trạng thái khóa — null = tất cả, true = chỉ bị khóa, false = chỉ đang hoạt động.</summary>
    public bool? IsLocked { get; set; }
}

/// <summary>
/// Wrapper phân trang — dùng chung cho mọi danh sách dài (NFR4.2).
/// Client biết tổng số trang để render UI phân trang.
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Body POST /api/users — Admin tạo tài khoản thay người dùng.
/// Khác register công khai: Admin có thể set IsPhoneVerified=true (bỏ qua OTP).
/// </summary>
public class AdminCreateUserDto
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;

    /// <summary>
    /// true = coi như đã xác thực SĐT (Admin tạo hộ, không cần OTP).
    /// false = user phải tự verify OTP sau (giống đăng ký thường).
    /// </summary>
    public bool IsPhoneVerified { get; set; } = true;
}

/// <summary>
/// Body PUT /api/users/{id} — Admin sửa thông tin user.
/// Không cho đổi Phone (định danh login) và Password (dùng endpoint reset-password).
/// </summary>
public class AdminUpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; }
}

/// <summary>
/// Body POST /api/users/{id}/reset-password — Admin đặt mật khẩu mới.
/// Khác change-password: không cần mật khẩu cũ (Admin có quyền cao hơn).
/// </summary>
public class AdminResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response POST /api/users/import — thống kê kết quả import Excel.
/// </summary>
public class ImportUsersResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }

    /// <summary>Chi tiết từng dòng lỗi (số dòng + lý do).</summary>
    public List<ImportUserErrorDto> Errors { get; set; } = new();
}

/// <summary>Một dòng import thất bại — giúp Admin sửa file Excel.</summary>
public class ImportUserErrorDto
{
    public int RowNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Một dòng trong file Excel mẫu import (map từ sheet).
/// Dùng nội bộ Service khi đọc file — không expose trực tiếp qua API.
/// </summary>
public class ImportUserRowDto
{
    public string Phone { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public string? Email { get; set; }
}
