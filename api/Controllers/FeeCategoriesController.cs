using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API loại khoản thu (FR4.1 — Ngày 11 Bước 5).
/// Admin CRUD FeeCategory.
/// </summary>
[ApiController]
[Route("api/fee-categories")]
[Authorize(Roles = "Admin")]
public class FeeCategoriesController : ControllerBase
{
    private readonly IFeeService _service;

    public FeeCategoriesController(IFeeService service) => _service = service;

    /// <summary>GET /api/fee-categories?isActive=true — DS loại phí.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var items = await _service.GetCategoriesAsync(isActive);
        return Ok(items);
    }

    /// <summary>GET /api/fee-categories/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetCategoryByIdAsync(id);
        return item == null
            ? NotFound(new { message = "Không tìm thấy loại phí." })
            : Ok(item);
    }

    /// <summary>POST /api/fee-categories — tạo loại phí.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUpdateFeeCategoryDto dto)
    {
        var (result, error) = await _service.CreateCategoryAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>PUT /api/fee-categories/{id} — sửa loại phí.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateFeeCategoryDto dto)
    {
        var (result, error) = await _service.UpdateCategoryAsync(id, dto);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>DELETE /api/fee-categories/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteCategoryAsync(id);
        if (!success)
        {
            if (message.Contains("Không tìm thấy")) return NotFound(new { message });
            return BadRequest(new { message });
        }
        return Ok(new { message });
    }
}
