using System.Security.Claims;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Import / tải template Excel — Admin (user) + Teacher/Admin (điểm).
/// </summary>
[ApiController]
[Route("api/import")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IExcelImportService _import;

    public ImportController(IExcelImportService import) => _import = import;

    /// <summary>GET /api/import/users/template — file mẫu import tài khoản (Admin).</summary>
    [HttpGet("users/template")]
    [Authorize(Roles = "Admin")]
    public IActionResult DownloadUserTemplate()
    {
        var bytes = _import.BuildUserImportTemplate();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "FSchool_User_Import_Template.xlsx");
    }

    /// <summary>POST /api/import/users — upload Excel tạo User (Admin).</summary>
    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportUsers(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Chọn file Excel (.xlsx)." });

        await using var stream = file.OpenReadStream();
        var result = await _import.ImportUsersAsync(stream);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/import/grades/template?classId=&amp;subjectId=
    /// File mẫu tiếng Việt + sẵn danh sách HS lớp.
    /// </summary>
    [HttpGet("grades/template")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> DownloadGradeTemplate(
        [FromQuery] int classId,
        [FromQuery] int subjectId)
    {
        if (classId <= 0 || subjectId <= 0)
            return BadRequest(new { message = "Chọn lớp và môn trước khi tải mẫu." });

        var actorId = GetUserId();
        var role = GetUserRole();
        if (actorId == null || role == null) return Unauthorized();

        var (bytes, fileName, error) = await _import.BuildGradeImportTemplateAsync(
            classId, subjectId, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });

        return File(
            bytes!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName ?? "Diem.xlsx");
    }

    /// <summary>
    /// POST /api/import/grades?classId=&amp;subjectId= — upload Excel điểm (đã chọn lớp/môn trên UI).
    /// </summary>
    [HttpPost("grades")]
    [Authorize(Roles = "Teacher,Admin")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportGrades(
        IFormFile file,
        [FromQuery] int classId,
        [FromQuery] int subjectId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Chọn file Excel (.xlsx)." });
        if (classId <= 0 || subjectId <= 0)
            return BadRequest(new { message = "Chọn lớp và môn trước khi import." });

        var actorId = GetUserId();
        var role = GetUserRole();
        if (actorId == null || role == null) return Unauthorized();

        await using var stream = file.OpenReadStream();
        var (result, error) = await _import.ImportGradesAsync(
            stream, classId, subjectId, actorId.Value, role.Value);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private UserRole? GetUserRole()
    {
        var claim = User.FindFirstValue(ClaimTypes.Role);
        return claim != null && Enum.TryParse<UserRole>(claim, out var role) ? role : null;
    }
}
