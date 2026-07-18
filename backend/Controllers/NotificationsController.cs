using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>API trung tâm thông báo in-app (FR1.4 — Ngày 10 Bước 5).</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] NotificationListQueryDto query)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetMyAsync(userId.Value, query);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.GetUnreadCountAsync(userId.Value);
        return Ok(result);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (success, message) = await _service.MarkReadAsync(id, userId.Value);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MarkReadResultDto { Message = message });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _service.MarkAllReadAsync(userId.Value);
        return Ok(result);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}

/// <summary>Đăng ký / hủy FCM token (FR1.4).</summary>
[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly INotificationService _service;

    public DevicesController(INotificationService service) => _service = service;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (success, error) = await _service.RegisterDeviceAsync(userId.Value, dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new MessageResponseDto { Message = "Đã đăng ký thiết bị nhận push." });
    }

    /// <summary>Hủy đăng ký — token truyền qua query (tránh ký tự đặc biệt trong URL path).</summary>
    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var decoded = Uri.UnescapeDataString(token);
        var (success, message) = await _service.UnregisterDeviceAsync(userId.Value, decoded);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
