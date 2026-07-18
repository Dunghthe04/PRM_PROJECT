using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// CRUD Kỳ học (FR5.2) — Ngày 4 Bước 1.
/// Chỉ Admin được tạo-sửa-xóa; mọi user đã login được xem.
/// </summary>
[ApiController]
[Route("api/semesters")]
[Authorize]
public class SemesterController : ControllerBase
{
    private readonly ISemesterService _service;

    public SemesterController(ISemesterService service)
    {
        _service = service;
    }

    /// <summary>GET /api/semesters — danh sách kỳ học.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    /// <summary>GET /api/semesters/{id} — chi tiết 1 kỳ.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound(new { message = "Không tìm thấy kỳ học." }) : Ok(item);
    }

    /// <summary>POST /api/semesters — tạo kỳ học mới.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateSemesterDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/semesters/{id} — cập nhật kỳ học.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateSemesterDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/semesters/{id} — xóa kỳ (chặn nếu còn lớp).</summary>
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
