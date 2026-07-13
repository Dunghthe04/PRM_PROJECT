using System.Security.Claims;
using System.Text.Json;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API thanh toán VNPay / PayOS + lịch sử (FR4.2, FR2.6 — Ngày 11 Bước 5).
/// Parent tạo link; webhook/IPN Public nhưng bắt buộc verify chữ ký.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service) => _service = service;

    /// <summary>POST /api/payments/vnpay/create — Parent tạo URL VNPay.</summary>
    [HttpPost("vnpay/create")]
    [Authorize(Roles = "Parent,Admin")]
    public async Task<IActionResult> CreateVnPay([FromBody] CreatePaymentDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.CreateVnPayAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/payments/vnpay/return — Return URL (Public).</summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        var query = Request.Query.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var (result, statusCode) = await _service.HandleVnPayReturnAsync(query);
        return StatusCode(statusCode, result);
    }

    /// <summary>POST /api/payments/vnpay/ipn — Webhook VNPay (Public, verify chữ ký).</summary>
    [HttpPost("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn()
    {
        // VNPay có thể gửi form-urlencoded hoặc query
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Request.Query)
            query[kv.Key] = kv.Value.ToString();

        if (Request.HasFormContentType)
        {
            foreach (var kv in Request.Form)
                query[kv.Key] = kv.Value.ToString();
        }

        var result = await _service.HandleVnPayIpnAsync(query);
        return Ok(result);
    }

    /// <summary>POST /api/payments/payos/create — Parent tạo link PayOS.</summary>
    [HttpPost("payos/create")]
    [Authorize(Roles = "Parent,Admin")]
    public async Task<IActionResult> CreatePayOs([FromBody] CreatePaymentDto dto)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.CreatePayOsAsync(dto, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>POST /api/payments/payos/webhook — Webhook PayOS (Public, verify checksum).</summary>
    [HttpPost("payos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOsWebhook([FromBody] JsonElement body)
    {
        var (success, message) = await _service.HandlePayOsWebhookAsync(body);
        if (!success) return BadRequest(new { success = false, message });
        return Ok(new { success = true, message });
    }

    /// <summary>GET /api/payments/history?studentId= — lịch sử giao dịch.</summary>
    [HttpGet("history")]
    [Authorize(Roles = "Parent,Admin,Student")]
    public async Task<IActionResult> GetHistory([FromQuery] int? studentId = null)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (result, error) = await _service.GetHistoryAsync(studentId, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>
    /// GET|POST /api/payments/dev/simulate-paid?orderCode= — Dev giả lập Paid.
    /// Chỉ Development.
    /// </summary>
    [HttpGet("dev/simulate-paid")]
    [HttpPost("dev/simulate-paid")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulatePaid([FromQuery] string orderCode)
    {
        var (result, error) = await _service.SimulatePaidAsync(orderCode);
        if (error != null) return BadRequest(new { message = error });
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
