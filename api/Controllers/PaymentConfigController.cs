using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API cấu hình cổng thanh toán VNPay / PayOS (FR4.2 — Ngày 11 Bước 5).
/// Admin xem / cập nhật API key (GET mask secret).
/// </summary>
[ApiController]
[Route("api/payment-config")]
[Authorize(Roles = "Admin")]
public class PaymentConfigController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentConfigController(IPaymentService service) => _service = service;

    /// <summary>GET /api/payment-config — DS cấu hình cổng (secret đã mask).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetConfigsAsync();
        return Ok(items);
    }

    /// <summary>PUT /api/payment-config — upsert API key VNPay hoặc PayOS.</summary>
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpdatePaymentGatewayConfigDto dto)
    {
        var (result, error) = await _service.UpsertConfigAsync(dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }
}
