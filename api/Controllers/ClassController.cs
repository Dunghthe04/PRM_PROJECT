using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// CRUD Lớp học + gán/bỏ học sinh (FR5.2) — Ngày 4 Bước 3.
/// Tạo/sửa/xóa/gán HS: Admin, HeadOfDept. Xem: mọi user đã login.
/// </summary>
[ApiController]
[Route("api/classes")]
[Authorize]
public class ClassController : ControllerBase
{
    private readonly IClassService _service;

    public ClassController(IClassService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/classes?semesterId= — danh sách lớp (lọc theo kỳ tùy chọn).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? semesterId = null)
    {
        var items = await _service.GetAllAsync(semesterId);
        return Ok(items);
    }

    /// <summary>GET /api/classes/{id} — chi tiết lớp.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null ? NotFound(new { message = "Không tìm thấy lớp học." }) : Ok(item);
    }

    /// <summary>POST /api/classes — tạo lớp thuộc 1 kỳ học.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateClassDto dto)
    {
        var (result, error) = await _service.CreateAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    /// <summary>PUT /api/classes/{id} — cập nhật lớp.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateClassDto dto)
    {
        var (result, error) = await _service.UpdateAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/classes/{id} — xóa lớp (chặn nếu còn HS / phân công).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
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

    /// <summary>GET /api/classes/{id}/students — danh sách HS trong lớp.</summary>
    [HttpGet("{id:int}/students")]
    public async Task<IActionResult> GetStudents(int id)
    {
        var (result, error) = await _service.GetStudentsAsync(id);
        if (error != null) return NotFound(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/classes/{id}/students — thêm HS vào lớp.</summary>
    [HttpPost("{id:int}/students")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> AddStudent(int id, [FromBody] AddStudentToClassDto dto)
    {
        var (success, message) = await _service.AddStudentAsync(id, dto.StudentId);
        if (!success)
        {
            if (message.Contains("Không tìm thấy lớp")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }

    /// <summary>DELETE /api/classes/{id}/students/{studentId} — bỏ HS khỏi lớp.</summary>
    [HttpDelete("{id:int}/students/{studentId:int}")]
    [Authorize(Roles = "Admin,HeadOfDept")]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        var (success, message) = await _service.RemoveStudentAsync(id, studentId);
        if (!success)
        {
            if (message.Contains("Không tìm thấy lớp")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new MessageResponseDto { Message = message });
    }
}
