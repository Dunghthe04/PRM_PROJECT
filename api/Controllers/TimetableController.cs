using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Thời khóa biểu (FR2.3) — Ngày 4 Bước 5.
/// CRUD tiết học + xem TKB theo tuần (lớp / HS / GV).
/// </summary>
[ApiController]
[Route("api/timetable")]
[Authorize]
public class TimetableController : ControllerBase
{
    private readonly ITimetableService _service;

    public TimetableController(ITimetableService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/timetable?classId=&amp;weekStart=
    /// TKB theo tuần của 1 lớp. weekStart = bất kỳ ngày trong tuần (mặc định hôm nay).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByClass(
        [FromQuery] int classId,
        [FromQuery] DateTime? weekStart = null)
    {
        if (classId <= 0)
            return BadRequest(new { message = "classId là bắt buộc." });

        var (result, error) = await _service.GetByClassAsync(classId, weekStart);
        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// GET /api/timetable/me?weekStart= — TKB của học sinh đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMy([FromQuery] DateTime? weekStart = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (result, error) = await _service.GetMyAsync(userId.Value, weekStart);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// GET /api/timetable/teacher?weekStart= — lịch dạy của GV đang đăng nhập.
    /// </summary>
    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher,HeadOfDept")]
    public async Task<IActionResult> GetTeacher([FromQuery] DateTime? weekStart = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (result, error) = await _service.GetTeacherAsync(userId.Value, weekStart);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/timetable — tạo tiết học (Admin / Trưởng bộ môn).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateTimetableSlotDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetByClass), new { classId = result!.ClassId }, result);
    }

    /// <summary>PUT /api/timetable/{id} — sửa tiết học.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateTimetableSlotDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/timetable/{id} — xóa tiết học.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success) return NotFound(new { message });
        return Ok(new MessageResponseDto { Message = message });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
