using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API điểm số (FR3.2, FR2.3 — Ngày 6 Bước 5).
/// GV: nhập Nháp → Publish. HS/PH: xem điểm đã công bố. Trưởng BM: duyệt.
/// </summary>
[ApiController]
[Route("api/grades")]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IGradeService _service;

    public GradesController(IGradeService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/grades?classId=1&amp;subjectId=2&amp;semesterId=1&amp;assessmentType=Midterm
    /// GV xem bảng điểm lớp (Draft + Published).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> GetList([FromQuery] GradeListQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetListAsync(query, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/grades/me?semesterId=1 — HS/PH xem điểm đã công bố.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> GetMyGrades([FromQuery] MyGradesQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var grades = await _service.GetMyGradesAsync(actorId.Value, role.Value, query);
        return Ok(grades);
    }

    /// <summary>POST /api/grades/batch — nhập điểm hàng loạt (lưu Nháp).</summary>
    [HttpPost("batch")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> BatchUpsert([FromBody] BatchGradeDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.BatchUpsertAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/grades/publish — công bố toàn bộ điểm Nháp của 1 đầu điểm.</summary>
    [HttpPost("publish")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Publish([FromBody] PublishGradesDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.PublishAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/grades/{id} — chi tiết 1 bản ghi điểm.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var grade = await _service.GetByIdAsync(id);
        return grade == null
            ? NotFound(new { message = "Không tìm thấy bản ghi điểm." })
            : Ok(grade);
    }

    /// <summary>PUT /api/grades/{id} — sửa điểm (chỉ khi còn Nháp).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGradeDto dto)
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

    /// <summary>PUT /api/grades/{id}/approve — Trưởng bộ môn duyệt điểm đã công bố.</summary>
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "HeadOfDept,Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Unauthorized();

        var (result, error) = await _service.ApproveAsync(id, actorId.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/grades/{id} — xóa điểm Nháp.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Delete(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (success, message) = await _service.DeleteAsync(id, actorId.Value, role.Value);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>Đọc userId từ JWT claim NameIdentifier.</summary>
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>Đọc role từ JWT claim Role — map sang enum UserRole.</summary>
    private UserRole? GetCurrentUserRole()
    {
        var claim = User.FindFirstValue(ClaimTypes.Role);
        return claim != null && Enum.TryParse<UserRole>(claim, out var role) ? role : null;
    }
}
