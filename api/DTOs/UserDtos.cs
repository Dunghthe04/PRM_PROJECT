using Api.Models;

namespace Api.DTOs;

// ─── Auth / Account responses ───────────────────────────────────────────────

/// <summary>Thông tin user trả về client (không gồm mật khẩu).</summary>
public class UserDto
{
    public int Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>Body đăng ký — chỉ SĐT + mật khẩu + họ tên + role.</summary>
public class CreateUserDto
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
}

/// <summary>Response sau register: thông báo + user + SĐT đã mask + TTL OTP.</summary>
public class RegisterResponseDto
{
    public string Message { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public string MaskedPhone { get; set; } = string.Empty;
    public int OtpExpiresInSeconds { get; set; }
}

/// <summary>Body đăng nhập bằng SĐT + mật khẩu.</summary>
public class LoginDto
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Response login: JWT + hồ sơ user.</summary>
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

// ─── FR1.3 — Quản lý hồ sơ ──────────────────────────────────────────────────

/// <summary>Cập nhật hồ sơ — không gồm Phone (định danh login).</summary>
public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

/// <summary>Đổi avatar bằng URL.</summary>
public class UpdateAvatarDto
{
    public string AvatarUrl { get; set; } = string.Empty;
}

/// <summary>Đổi mật khẩu khi còn nhớ mật khẩu cũ.</summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// ─── OTP về SĐT (đăng ký + quên MK) ─────────────────────────────────────────

/// <summary>Body xác thực OTP (verify-phone / verify-otp).</summary>
public class PhoneOtpDto
{
    public string Phone { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

/// <summary>Body yêu cầu gửi OTP quên mật khẩu.</summary>
public class ForgotPasswordDto
{
    public string Phone { get; set; } = string.Empty;
}

/// <summary>Response sau khi gửi OTP (masked phone + TTL).</summary>
public class ForgotPasswordResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = OtpChannel.Phone;
    public string MaskedDestination { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

/// <summary>Response sau verify OTP quên MK — chứa resetToken.</summary>
public class VerifyOtpResponseDto
{
    public string Message { get; set; } = string.Empty;
    /// <summary>Vé tạm dùng cho reset-password; null với verify-phone.</summary>
    public string? ResetToken { get; set; }
}

/// <summary>Body đặt mật khẩu mới sau khi có resetToken.</summary>
public class ResetPasswordDto
{
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Body gửi lại OTP — purpose: Register | ResetPassword.</summary>
public class ResendOtpDto
{
    public string Phone { get; set; } = string.Empty;
    public string Purpose { get; set; } = OtpPurpose.Register;
}

/// <summary>Response thông báo chung (success message).</summary>
public class MessageResponseDto
{
    public string Message { get; set; } = string.Empty;
}
