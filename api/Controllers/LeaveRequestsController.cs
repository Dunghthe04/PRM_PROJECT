using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API đơn xin nghỉ (FR2.5, FR3.3 — Ngày 9 Bước 5).
/// HS/PH: tạo, xem, hủy đơn Pending.
/// GV: xem DS, duyệt/từ chối → thông báo in-app.
/// </summary>
[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;

    public LeaveRequestsController(ILeaveRequestService service)
    {
        _service = service;
    }

    /// <summary>GET /api/leave-requests?classId=1&amp;status=Pending — GV xem đơn cần duyệt.</summary>
    [HttpGet]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> GetList([FromQuery] LeaveRequestListQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetListAsync(query, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/leave-requests/me — HS/PH xem đơn của mình/con.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> GetMy()
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var items = await _service.GetMyAsync(actorId.Value, role.Value);
        return Ok(items);
    }

    /// <summary>GET /api/leave-requests/{id} — chi tiết đơn (theo quyền).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetByIdAsync(id, actorId.Value, role.Value);
        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/leave-requests — tạo đơn (đính kèm URL ảnh y tế).</summary>
    [HttpPost]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.CreateAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>PUT /api/leave-requests/{id}/approve — GV duyệt đơn.</summary>
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Approve(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.ApproveAsync(id, actorId.Value, role.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>PUT /api/leave-requests/{id}/reject — GV từ chối đơn.</summary>
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectLeaveRequestDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.RejectAsync(id, dto, actorId.Value, role.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/leave-requests/{id} — hủy đơn khi còn Pending.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Cancel(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (success, message) = await _service.CancelAsync(id, actorId.Value, role.Value);
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

    private UserRole? GetCurrentUserRole()
    {
        var claim = User.FindFirstValue(ClaimTypes.Role);
        return claim != null && Enum.TryParse<UserRole>(claim, out var role) ? role : null;
    }
}
