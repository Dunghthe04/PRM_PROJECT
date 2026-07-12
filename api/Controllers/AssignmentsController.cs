using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API bài tập + nộp bài (FR3.5, FR2.4 — Ngày 8 Bước 5).
/// GV: CRUD assignment, xem nộp, chấm điểm.
/// HS: xem ToDo/Done/Overdue, nộp / nộp lại.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _service;

    public AssignmentsController(IAssignmentService service)
    {
        _service = service;
    }

    // ─── Assignments ────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/assignments?classId=&amp;subjectId= — DS bài tập lớp (GV).
    /// </summary>
    [HttpGet("assignments")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> GetList([FromQuery] AssignmentListQueryDto query)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetListAsync(query, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// GET /api/assignments/me?status=ToDo — bài tập của HS (ToDo/Done/Overdue).
    /// </summary>
    [HttpGet("assignments/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMy([FromQuery] MyAssignmentsQueryDto query)
    {
        var studentId = GetCurrentUserId();
        if (studentId == null) return Unauthorized();

        var (result, error) = await _service.GetMyAsync(studentId.Value, query);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/assignments/{id} — chi tiết bài tập.</summary>
    [HttpGet("assignments/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        // HS xem kèm Status; GV/khác không cần
        int? studentId = role == UserRole.Student ? actorId : null;

        var (result, error) = await _service.GetByIdAsync(id, studentId);
        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/assignments — GV tạo bài tập.</summary>
    [HttpPost("assignments")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateAssignmentDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.CreateAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/assignments/{id} — sửa bài tập.</summary>
    [HttpPut("assignments/{id:int}")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateAssignmentDto dto)
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

    /// <summary>DELETE /api/assignments/{id} — xóa bài tập.</summary>
    [HttpDelete("assignments/{id:int}")]
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

    /// <summary>GET /api/assignments/{id}/submissions — GV xem DS bài nộp.</summary>
    [HttpGet("assignments/{id:int}/submissions")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetSubmissionsAsync(id, actorId.Value, role.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>POST /api/assignments/{id}/submissions — HS nộp bài.</summary>
    [HttpPost("assignments/{id:int}/submissions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitAssignmentDto dto)
    {
        var studentId = GetCurrentUserId();
        if (studentId == null) return Unauthorized();

        var (result, error) = await _service.SubmitAsync(id, dto, studentId.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    // ─── Submissions ────────────────────────────────────────────────────────

    /// <summary>GET /api/submissions/{id} — chi tiết bài nộp.</summary>
    [HttpGet("submissions/{id:int}")]
    public async Task<IActionResult> GetSubmission(int id)
    {
        var (result, error) = await _service.GetSubmissionByIdAsync(id);
        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>PUT /api/submissions/{id} — HS nộp lại.</summary>
    [HttpPut("submissions/{id:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Resubmit(int id, [FromBody] SubmitAssignmentDto dto)
    {
        var studentId = GetCurrentUserId();
        if (studentId == null) return Unauthorized();

        var (result, error) = await _service.ResubmitAsync(id, dto, studentId.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>PUT /api/submissions/{id}/grade — GV chấm điểm + feedback.</summary>
    [HttpPut("submissions/{id:int}/grade")]
    [Authorize(Roles = "Teacher,Admin,HeadOfDept")]
    public async Task<IActionResult> Grade(int id, [FromBody] GradeSubmissionDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GradeSubmissionAsync(id, dto, actorId.Value, role.Value);
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
