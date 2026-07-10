using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Phân công giảng dạy (FR5.3) — Ngày 4 Bước 4.
/// Gán Giáo viên ↔ Lớp ↔ Môn; hỗ trợ luân chuyển.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class TeacherAssignmentController : ControllerBase
{
    private readonly ITeacherAssignmentService _service;

    public TeacherAssignmentController(ITeacherAssignmentService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/teacher-assignments?classId=&amp;teacherId=&amp;subjectId=
    /// Danh sách phân công (lọc tùy chọn).
    /// </summary>
    [HttpGet("teacher-assignments")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? classId = null,
        [FromQuery] int? teacherId = null,
        [FromQuery] int? subjectId = null)
    {
        var items = await _service.GetAllAsync(classId, teacherId, subjectId);
        return Ok(items);
    }

    /// <summary>GET /api/teacher-assignments/{id} — chi tiết phân công.</summary>
    [HttpGet("teacher-assignments/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound(new { message = "Không tìm thấy phân công." }) : Ok(item);
    }

    /// <summary>POST /api/teacher-assignments — gán GV vào lớp + môn.</summary>
    [HttpPost("teacher-assignments")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateTeacherAssignmentDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/teacher-assignments/{id} — sửa / luân chuyển phân công.</summary>
    [HttpPut("teacher-assignments/{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateTeacherAssignmentDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy phân công")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/teacher-assignments/{id} — gỡ phân công.</summary>
    [HttpDelete("teacher-assignments/{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success) return NotFound(new { message });
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>GET /api/teachers/{id}/classes — các lớp + môn một GV phụ trách.</summary>
    [HttpGet("teachers/{id:int}/classes")]
    public async Task<IActionResult> GetTeacherClasses(int id)
    {
        var (result, error) = await _service.GetTeacherClassesAsync(id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }
}
