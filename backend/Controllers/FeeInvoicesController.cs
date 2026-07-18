using System.Security.Claims;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API hóa đơn khoản thu (FR4.1, FR2.6 — Ngày 11 Bước 5).
/// Admin: DS / tạo 1 HS / batch theo lớp.
/// Parent/Student: xem của mình + biên lai khi Paid.
/// </summary>
[ApiController]
[Route("api/fee-invoices")]
[Authorize]
public class FeeInvoicesController : ControllerBase
{
    private readonly IFeeService _service;

    public FeeInvoicesController(IFeeService service) => _service = service;

    /// <summary>GET /api/fee-invoices?studentId=&amp;isPaid=&amp;status= — Admin.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetList([FromQuery] FeeInvoiceListQueryDto query)
    {
        var (result, error) = await _service.GetInvoicesAsync(query);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/fee-invoices/me — Parent/Student xem hóa đơn của mình / con.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> GetMy()
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetMyInvoicesAsync(actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/fee-invoices/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetInvoiceByIdAsync(id, actorId.Value, role.Value);
        if (error != null)
        {
            if (error.Contains("Không tìm thấy")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(result);
    }

    /// <summary>POST /api/fee-invoices — Admin tạo hóa đơn 1 HS.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateFeeInvoiceDto dto)
    {
        var (result, error) = await _service.CreateInvoiceAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/fee-invoices/batch — Admin gán phí cả lớp.</summary>
    [HttpPost("batch")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBatch([FromBody] BatchCreateFeeInvoiceDto dto)
    {
        var (result, error) = await _service.CreateInvoiceBatchAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/fee-invoices/{id}/receipt — biên lai điện tử (đã Paid).</summary>
    [HttpGet("{id:int}/receipt")]
    [Authorize(Roles = "Student,Parent,Admin")]
    public async Task<IActionResult> GetReceipt(int id)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetReceiptAsync(id, actorId.Value, role.Value);
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
