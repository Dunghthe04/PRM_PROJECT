using System.Security.Claims;
using Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Upload file dùng chung (ảnh y tế, bài nộp…) — Ngày 9.
/// Trả về URL tương đối để client gắn vào DTO tạo đơn/bài nộp.
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public FilesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>POST /api/files/upload — upload ảnh/PDF (tối đa 10MB).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? folder = "uploads")
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Chưa chọn file." });

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Chỉ chấp nhận: jpg, jpeg, png, webp, gif, pdf." });

        // Chỉ cho phép thư mục con an toàn
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().ToLowerInvariant();
        if (safeFolder.Contains("..") || safeFolder.Contains('/') || safeFolder.Contains('\\'))
            return BadRequest(new { message = "Tên thư mục không hợp lệ." });

        var uploadDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            safeFolder);
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(uploadDir, fileName);

        await using (var stream = System.IO.File.Create(physicalPath))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"/{safeFolder}/{fileName}";
        return Ok(new FileUploadResultDto { Url = url, FileName = fileName });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
