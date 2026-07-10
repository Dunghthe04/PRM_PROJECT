using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

/// <summary>
/// Controller hồ sơ cá nhân (FR1.3) — mọi role đã đăng nhập.
/// Phone là định danh login → không cho đổi qua API này.
/// </summary>
[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IWebHostEnvironment _env;

    public AccountController(IAccountService accountService, IWebHostEnvironment env)
    {
        _accountService = accountService;
        _env = env;
    }

    /// <summary>Xem hồ sơ của chính mình (userId lấy từ JWT, không cần truyền id).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var profile = await _accountService.GetProfileAsync(userId.Value);
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>Cập nhật họ tên và email liên hệ (không đổi số điện thoại).</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (profile, error) = await _accountService.UpdateProfileAsync(userId.Value, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(profile);
    }

    /// <summary>Đổi avatar bằng URL (client tự host ảnh hoặc CDN).</summary>
    [HttpPut("avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (profile, error) = await _accountService.UpdateAvatarAsync(userId.Value, dto.AvatarUrl);
        if (error != null) return BadRequest(new { message = error });
        return Ok(profile);
    }

    /// <summary>
    /// Upload file ảnh đại diện (tối đa 5MB).
    /// Lưu vào wwwroot/avatars và cập nhật AvatarUrl trên hồ sơ.
    /// </summary>
    [HttpPost("avatar/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Chưa chọn file ảnh." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Chỉ chấp nhận ảnh: jpg, jpeg, png, webp, gif." });

        var avatarsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "avatars");
        Directory.CreateDirectory(avatarsDir);

        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(avatarsDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream);
        }

        // URL tương đối — client ghép với base URL API khi hiển thị
        var avatarUrl = $"/avatars/{fileName}";
        var (profile, error) = await _accountService.UpdateAvatarAsync(userId.Value, avatarUrl);
        if (error != null) return BadRequest(new { message = error });
        return Ok(profile);
    }

    /// <summary>Đổi mật khẩu khi còn nhớ mật khẩu cũ (verify BCrypt trước khi hash MK mới).</summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (success, message) = await _accountService.ChangePasswordAsync(userId.Value, dto);
        if (!success) return BadRequest(new { message });
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>Đọc userId từ claim NameIdentifier trong JWT hiện tại.</summary>
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
