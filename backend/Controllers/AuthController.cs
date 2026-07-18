using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller OTP (FR1.1 / FR1.2): xác thực đăng ký (SĐT) + quên mật khẩu (Email).
/// Tất cả endpoint public — không cần JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Xác thực OTP đăng ký → đánh dấu IsPhoneVerified = true.
    /// Sau bước này user mới được phép login.
    /// </summary>
    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone([FromBody] PhoneOtpDto dto)
    {
        var (result, error) = await _authService.VerifyPhoneAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// Gửi lại OTP khi mã cũ hết hạn / chưa nhận được.
    /// Register → body.phone; ResetPassword → body.email.
    /// </summary>
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
    {
        var (result, error) = await _authService.ResendOtpAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// Bước 1 quên MK: gửi OTP 6 số về Gmail đã gắn hồ sơ (SMTP thật).
    /// Cần cấu hình Smtp:User + Smtp:AppPassword trong appsettings.Development.json.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var (result, error) = await _authService.ForgotPasswordAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// Bước 2 quên MK: xác thực OTP (email) → nhận resetToken (vé tạm để đổi MK).
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] EmailOtpDto dto)
    {
        var (result, error) = await _authService.VerifyResetOtpAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// Bước 3 quên MK: đặt mật khẩu mới bằng resetToken (one-time, hết hạn nhanh).
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var (success, message) = await _authService.ResetPasswordAsync(dto);
        if (!success) return BadRequest(new { message });
        return Ok(new MessageResponseDto { Message = message });
    }
}
