using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API điểm danh P/A/L (FR3.1 — Ngày 7 Bước 5).
/// GV: batch + sync offline. HS/PH: xem lịch sử chuyên cần.
/// </summary>
[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _service;

    public AttendanceController(IAttendanceService service)
    {
        _service = service;
    }

    /// <summary>GET /api/attendance?classId=1&amp;date=2026-07-11 — GV xem điểm danh lớp theo ngày.</summary>
    [HttpGet]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetByClassAndDate([FromQuery] AttendanceListQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetByClassAndDateAsync(query, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/attendance/me?from=2026-07-01&amp;to=2026-07-31 — HS/PH xem lịch sử chuyên cần.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> GetMyAttendance([FromQuery] MyAttendanceQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var records = await _service.GetMyAttendanceAsync(actorId.Value, role.Value, query);
        return Ok(records);
    }

    /// <summary>GET /api/attendance/summary?classId=1 — thống kê tỷ lệ chuyên cần lớp.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetSummary([FromQuery] AttendanceSummaryQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetSummaryAsync(query, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/attendance/batch — điểm danh hàng loạt P/A/L.</summary>
    [HttpPost("batch")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> BatchUpsert([FromBody] BatchAttendanceDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.BatchUpsertAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>PUT /api/attendance/{id} — sửa trạng thái 1 bản ghi.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.UpdateAsync(id, dto, actorId.Value, role.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
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
