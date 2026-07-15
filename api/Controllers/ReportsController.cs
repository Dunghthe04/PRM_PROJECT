using Api.Common;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// API báo cáo & thống kê (FR5.5 — Ngày 12 Bước 4).
/// Admin xem dashboard + xuất bảng điểm / chuyên cần / học phí.
/// format=json (mặc định) | excel | pdf.
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

    /// <summary>GET /api/reports/grades?classId=&amp;semesterId=&amp;subjectId=&amp;format= — bảng điểm.</summary>
    [HttpGet("grades")]
    public async Task<IActionResult> Grades([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetGradeReportAsync(query);
        if (error != null) return BadRequest(new { message = error });

        return Export(query.Format,
            json: () => Ok(result),
            excel: () => ReportExcelHelper.BuildGradeReport(result!),
            pdf: () => ReportPdfHelper.BuildGradeReport(result!),
            fileName: "BaoCao_BangDiem");
    }

    /// <summary>GET /api/reports/attendance?classId=&amp;from=&amp;to=&amp;format= — chuyên cần.</summary>
    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetAttendanceReportAsync(query);
        if (error != null) return BadRequest(new { message = error });

        return Export(query.Format,
            json: () => Ok(result),
            excel: () => ReportExcelHelper.BuildAttendanceReport(result!),
            pdf: () => ReportPdfHelper.BuildAttendanceReport(result!),
            fileName: "BaoCao_ChuyenCan");
    }

    /// <summary>GET /api/reports/fees?classId=&amp;format= — học phí (Admin).</summary>
    [HttpGet("fees")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Fees([FromQuery] ReportQueryDto query)
    {
        var (result, error) = await _service.GetFeeReportAsync(query);
        if (error != null) return BadRequest(new { message = error });

        return Export(query.Format,
            json: () => Ok(result),
            excel: () => ReportExcelHelper.BuildFeeReport(result!),
            pdf: () => ReportPdfHelper.BuildFeeReport(result!),
            fileName: "BaoCao_HocPhi");
    }

    /// <summary>
    /// Chọn kiểu trả về theo format: json (object), excel (.xlsx), pdf (.pdf).
    /// </summary>
    private IActionResult Export(
        string format,
        Func<IActionResult> json,
        Func<byte[]> excel,
        Func<byte[]> pdf,
        string fileName)
    {
        return (format?.Trim().ToLowerInvariant()) switch
        {
            "excel" => File(excel(), ReportExcelHelper.ContentType, $"{fileName}.xlsx"),
            "pdf" => File(pdf(), ReportPdfHelper.ContentType, $"{fileName}.pdf"),
            _ => json()
        };
    }
}
