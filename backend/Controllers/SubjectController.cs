using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// CRUD Môn học (FR5.2) — Ngày 4 Bước 2.
/// Chỉ Admin được tạo-sửa-xóa; mọi user đã login được xem.
/// </summary>
[ApiController]
[Route("api/subjects")]
[Authorize]
public class SubjectController : ControllerBase
{
    private readonly ISubjectService _service;

    public SubjectController(ISubjectService service)
    {
        _service = service;
    }

    /// <summary>GET /api/subjects — danh sách môn học.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    /// <summary>GET /api/subjects/{id} — chi tiết 1 môn.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound(new { message = "Không tìm thấy môn học." }) : Ok(item);
    }

    /// <summary>POST /api/subjects — tạo môn mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateSubjectDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/subjects/{id} — cập nhật môn.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateSubjectDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/subjects/{id} — xóa môn (chặn nếu đang phân công).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }
}
