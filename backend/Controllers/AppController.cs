using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API hệ thống: kiểm tra phiên bản app cho Force Update (FR4.4) + health check.
/// Public — client gọi lúc khởi động để quyết định có bắt cập nhật không.
/// </summary>
[ApiController]
[Route("api/app")]
[AllowAnonymous]
public class AppController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppController(IConfiguration configuration) => _configuration = configuration;

    /// <summary>
    /// GET /api/app/version — trả phiên bản mới nhất + tối thiểu bắt buộc.
    /// App so sánh version hiện tại: nếu nhỏ hơn minSupportedVersion → buộc cập nhật.
    /// Cấu hình trong appsettings mục "AppVersion" (có default nếu thiếu).
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        var section = _configuration.GetSection("AppVersion");
        return Ok(new
        {
            latestVersion = section["Latest"] ?? "1.0.0",
            minSupportedVersion = section["MinSupported"] ?? "1.0.0",
            updateUrl = section["UpdateUrl"] ?? "https://play.google.com/store",
            message = section["Message"]
                ?? "Vui lòng cập nhật phiên bản mới nhất để tiếp tục sử dụng."
        });
    }

    /// <summary>GET /api/health — kiểm tra API còn sống.</summary>
    [HttpGet("/api/health")]
    public IActionResult Health() => Ok(new { status = "ok", time = DateTime.UtcNow });
}
