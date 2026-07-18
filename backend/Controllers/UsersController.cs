using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Quản lý người dùng cấp Admin (FR5.1 — Ngày 5 Bước 5).
/// Controller = cổng HTTP: nhận request → gọi Service → trả HTTP status + JSON.
/// Toàn bộ endpoint yêu cầu role Admin (RBAC).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IAdminUserService _service;

    public UsersController(IAdminUserService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/users?page=1&amp;pageSize=20&amp;role=Student&amp;search=0912&amp;isLocked=false
    /// Danh sách user có phân trang + lọc.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] UserListQueryDto query)
    {
        var result = await _service.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>GET /api/users/{id} — chi tiết 1 user.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _service.GetByIdAsync(id);
        return user == null
            ? NotFound(new { message = "Không tìm thấy người dùng." })
            : Ok(user);
    }

    /// <summary>POST /api/users — Admin tạo tài khoản mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCreateUserDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/users/{id} — sửa họ tên, email, role.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AdminUpdateUserDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/users/{id} — xóa user (không xóa chính mình).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Unauthorized();

        var (success, message) = await _service.DeleteAsync(id, actorId.Value);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>PUT /api/users/{id}/lock — khóa tài khoản.</summary>
    [HttpPut("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Unauthorized();

        var (success, message) = await _service.LockAsync(id, actorId.Value);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>PUT /api/users/{id}/unlock — mở khóa tài khoản.</summary>
    [HttpPut("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id)
    {
        var (success, message) = await _service.UnlockAsync(id);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>POST /api/users/{id}/reset-password — Admin đặt mật khẩu mới.</summary>
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
    {
        var (success, message) = await _service.ResetPasswordAsync(id, dto);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>
    /// Đọc userId từ JWT claim NameIdentifier — do JwtHelper gán lúc login.
    /// Dùng cho các thao tác cần biết Admin nào đang thực hiện.
    /// </summary>
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
