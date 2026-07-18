using System.Security.Claims;
using Api.Common;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

/// <summary>
/// Controller đăng ký / đăng nhập bằng SĐT + mật khẩu (FR1.1).
/// Sau register phải gọi /api/auth/verify-phone mới login được.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public UserController(
        IUserService userService,
        IAuthService authService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userService = userService;
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Đăng ký tài khoản mới bằng số điện thoại.
    /// Tạo user (chưa verify) rồi gửi OTP về SĐT.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(CreateUserDto dto)
    {
        var (user, error) = await _userService.CreateUserAsync(dto);
        if (error != null) return BadRequest(new { message = error });

        var (otpResult, otpError) = await _authService.SendRegisterOtpAsync(user!.Id, user.Phone);
        if (otpError != null) return BadRequest(new { message = otpError });

        return Ok(new RegisterResponseDto
        {
            Message = otpResult!.Message,
            User = user,
            MaskedPhone = otpResult.MaskedDestination,
            OtpExpiresInSeconds = otpResult.ExpiresInSeconds
        });
    }

    /// <summary>
    /// Đăng nhập bằng SĐT + mật khẩu.
    /// Yêu cầu đã xác thực OTP đăng ký. Trả JWT chứa userId + role.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var (user, error) = await _userService.AuthenticateAsync(dto.Phone, dto.Password);

        if (error != null)
            return Unauthorized(new { message = error });

        if (user == null)
            return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu." });

        // JWT Name claim = Phone (định danh chính của hệ thống)
        var token = JwtHelper.GenerateToken(
            user.Id,
            user.Phone,
            user.Role.ToString(),
            _jwtSettings);

        return Ok(new LoginResponseDto
        {
            Token = token,
            User = UserService.MapToDto(user)
        });
    }

    /// <summary>
    /// Lấy hồ sơ user đang đăng nhập (đọc userId từ JWT).
    /// Khuyến nghị dùng GET /api/account/me thay endpoint này.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>Lấy thông tin user theo Id (cần JWT).</summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }
}
