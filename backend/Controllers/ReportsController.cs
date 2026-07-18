using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API báo cáo & thống kê (FR5.5) — Admin xem trên màn hình (JSON).
/// Xuất Excel/PDF đã bỏ khỏi phạm vi app.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    /// <summary>GET /api/reports/dashboard?classId= — số liệu tổng quan.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int? classId = null)
    {
        var result = await _service.GetDashboardAsync(classId);
        return Ok(result);
    }

    /// <summary>GET /api/reports/grades?classId=&amp;semesterId=&amp;subjectId= — bảng điểm.</summary>
    [HttpGet("grades")]
    public async Task<IActionResult> Grades([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetGradeReportAsync(query);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/reports/attendance?classId=&amp;from=&amp;to= — chuyên cần.</summary>
    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetAttendanceReportAsync(query);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }

    /// <summary>GET /api/reports/fees?classId= — học phí.</summary>
    [HttpGet("fees")]
    public async Task<IActionResult> Fees([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetFeeReportAsync(query);
        if (error != null) return BadRequest(new { message = error });
        return Ok(result);
    }
}
