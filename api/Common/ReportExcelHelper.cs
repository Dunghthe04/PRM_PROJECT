using Api.DTOs;
using ClosedXML.Excel;

namespace Api.Common;

/// <summary>
/// Xuất báo cáo ra file Excel (.xlsx) bằng ClosedXML (FR5.5 — Ngày 12 Bước 3).
/// Tách khỏi ReportService để Service chỉ lo tổng hợp dữ liệu, Helper lo format file.
/// </summary>
public static class ReportExcelHelper
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Xuất báo cáo bảng điểm ra Excel.</summary>
    public static byte[] BuildGradeReport(GradeReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BangDiem");

        WriteTitle(ws, "BÁO CÁO BẢNG ĐIỂM", 6);
        WriteMeta(ws, 2, new[]
        {
            ("Lớp", report.ClassName ?? "Tất cả"),
            ("Học kỳ", report.SemesterName ?? "Tất cả"),
            ("Môn", report.SubjectName ?? "Tất cả"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        });

        var header = 4;
        string[] cols = { "Học sinh", "SĐT", "Môn", "Đầu điểm", "Điểm", "Trạng thái", "Đã duyệt" };
        WriteHeaderRow(ws, header, cols);

        var r = header + 1;
        foreach (var row in report.Rows)
        {
            ws.Cell(r, 1).Value = row.StudentName;
            ws.Cell(r, 2).Value = row.StudentPhone;
            ws.Cell(r, 3).Value = row.SubjectName;
            ws.Cell(r, 4).Value = row.AssessmentType;
            ws.Cell(r, 5).Value = row.Score;
            ws.Cell(r, 6).Value = row.Status;
            ws.Cell(r, 7).Value = row.IsApproved ? "Có" : "Không";
            r++;
        }

        Finalize(ws, header, cols.Length);
        return ToBytes(wb);
    }

    /// <summary>Xuất báo cáo chuyên cần ra Excel.</summary>
    public static byte[] BuildAttendanceReport(AttendanceReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ChuyenCan");

        WriteTitle(ws, "BÁO CÁO CHUYÊN CẦN", 7);
        WriteMeta(ws, 2, new[]
        {
            ("Lớp", report.ClassName),
            ("Từ ngày", report.From.ToString("yyyy-MM-dd")),
            ("Đến ngày", report.To.ToString("yyyy-MM-dd")),
            ("Tỷ lệ cả lớp", $"{report.ClassAttendanceRate}%"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        });

        var header = 5;
        string[] cols = { "Học sinh", "SĐT", "Có mặt", "Vắng", "Muộn", "Tổng buổi", "Tỷ lệ (%)" };
        WriteHeaderRow(ws, header, cols);

        var r = header + 1;
        foreach (var row in report.Rows)
        {
            ws.Cell(r, 1).Value = row.StudentName;
            ws.Cell(r, 2).Value = row.StudentPhone;
            ws.Cell(r, 3).Value = row.PresentCount;
            ws.Cell(r, 4).Value = row.AbsentCount;
            ws.Cell(r, 5).Value = row.LateCount;
            ws.Cell(r, 6).Value = row.TotalSessions;
            ws.Cell(r, 7).Value = row.AttendanceRate;
            r++;
        }

        Finalize(ws, header, cols.Length);
        return ToBytes(wb);
    }

    /// <summary>Xuất báo cáo học phí ra Excel.</summary>
    public static byte[] BuildFeeReport(FeeReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("HocPhi");

        WriteTitle(ws, "BÁO CÁO HỌC PHÍ", 8);
        WriteMeta(ws, 2, new[]
        {
            ("Lớp", report.ClassName ?? "Tất cả"),
            ("Tổng hóa đơn", report.TotalInvoices.ToString()),
            ("Đã thu", $"{report.PaidCount} ({report.PaidAmount:N0} đ)"),
            ("Chưa thu", $"{report.PendingCount} ({report.PendingAmount:N0} đ)"),
            ("Tỷ lệ đã thu", $"{report.PaidRate}%"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        });

        var header = 6;
        string[] cols = { "Học sinh", "Khoản thu", "Số tiền", "Trạng thái", "Hạn nộp", "Ngày trả", "Phương thức", "Biên lai" };
        WriteHeaderRow(ws, header, cols);

        var r = header + 1;
        foreach (var row in report.Rows)
        {
            ws.Cell(r, 1).Value = row.StudentName;
            ws.Cell(r, 2).Value = row.FeeCategoryName;
            ws.Cell(r, 3).Value = row.Amount;
            ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 4).Value = row.Status;
            ws.Cell(r, 5).Value = row.DueDate.ToString("yyyy-MM-dd");
            ws.Cell(r, 6).Value = row.PaidAt?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(r, 7).Value = row.PaymentMethod ?? "";
            ws.Cell(r, 8).Value = row.ReceiptNumber ?? "";
            r++;
        }

        Finalize(ws, header, cols.Length);
        return ToBytes(wb);
    }

    // ─── Helpers định dạng chung ───────────────────────────────────────────

    private static void WriteTitle(IXLWorksheet ws, string title, int spanCols)
    {
        var cell = ws.Cell(1, 1);
        cell.Value = title;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, spanCols).Merge();
    }

    private static void WriteMeta(IXLWorksheet ws, int startRow, (string Label, string Value)[] items)
    {
        var r = startRow;
        // Ghi meta dạng "Label: Value" trên 1 cột đầu, xuống dòng
        var parts = items.Select(i => $"{i.Label}: {i.Value}");
        ws.Cell(r, 1).Value = string.Join("   |   ", parts);
        ws.Cell(r, 1).Style.Font.Italic = true;
    }

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] cols)
    {
        for (var c = 0; c < cols.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = cols[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FF6B00");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void Finalize(IXLWorksheet ws, int headerRow, int colCount)
    {
        var lastRow = Math.Max(headerRow, ws.LastRowUsed()?.RowNumber() ?? headerRow);
        ws.Range(headerRow, 1, lastRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(headerRow, 1, lastRow, colCount).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
