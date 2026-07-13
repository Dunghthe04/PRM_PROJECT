using Api.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Api.Common;

/// <summary>
/// Xuất báo cáo ra PDF bằng QuestPDF (FR5.5 — Ngày 12 Bước 4).
/// License Community set 1 lần lúc khởi động (Program.cs).
/// </summary>
public static class ReportPdfHelper
{
    public const string ContentType = "application/pdf";

    // Màu thương hiệu cam #FF6B00
    private const string BrandOrange = "#FF6B00";

    /// <summary>PDF báo cáo bảng điểm.</summary>
    public static byte[] BuildGradeReport(GradeReportDto report)
    {
        var meta = new[]
        {
            ("Lớp", report.ClassName ?? "Tất cả"),
            ("Học kỳ", report.SemesterName ?? "Tất cả"),
            ("Môn", report.SubjectName ?? "Tất cả"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        };
        string[] headers = { "Học sinh", "SĐT", "Môn", "Đầu điểm", "Điểm", "Trạng thái", "Duyệt" };

        return BuildTable("BÁO CÁO BẢNG ĐIỂM", meta, headers, report.Rows.Count, (table, i) =>
        {
            var row = report.Rows[i];
            Cell(table, row.StudentName);
            Cell(table, row.StudentPhone);
            Cell(table, row.SubjectName);
            Cell(table, row.AssessmentType);
            Cell(table, row.Score.ToString("0.##"));
            Cell(table, row.Status);
            Cell(table, row.IsApproved ? "Có" : "Không");
        }, new float[] { 3, 2, 2, 2, 1, 2, 1.2f });
    }

    /// <summary>PDF báo cáo chuyên cần.</summary>
    public static byte[] BuildAttendanceReport(AttendanceReportDto report)
    {
        var meta = new[]
        {
            ("Lớp", report.ClassName),
            ("Từ ngày", report.From.ToString("yyyy-MM-dd")),
            ("Đến ngày", report.To.ToString("yyyy-MM-dd")),
            ("Tỷ lệ cả lớp", $"{report.ClassAttendanceRate}%"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        };
        string[] headers = { "Học sinh", "SĐT", "Có mặt", "Vắng", "Muộn", "Tổng", "Tỷ lệ (%)" };

        return BuildTable("BÁO CÁO CHUYÊN CẦN", meta, headers, report.Rows.Count, (table, i) =>
        {
            var row = report.Rows[i];
            Cell(table, row.StudentName);
            Cell(table, row.StudentPhone);
            Cell(table, row.PresentCount.ToString());
            Cell(table, row.AbsentCount.ToString());
            Cell(table, row.LateCount.ToString());
            Cell(table, row.TotalSessions.ToString());
            Cell(table, row.AttendanceRate.ToString("0.##"));
        }, new float[] { 3, 2, 1.2f, 1, 1, 1.2f, 1.5f });
    }

    /// <summary>PDF báo cáo học phí.</summary>
    public static byte[] BuildFeeReport(FeeReportDto report)
    {
        var meta = new[]
        {
            ("Lớp", report.ClassName ?? "Tất cả"),
            ("Tổng hóa đơn", report.TotalInvoices.ToString()),
            ("Đã thu", $"{report.PaidCount} ({report.PaidAmount:N0} đ)"),
            ("Chưa thu", $"{report.PendingCount} ({report.PendingAmount:N0} đ)"),
            ("Tỷ lệ đã thu", $"{report.PaidRate}%"),
            ("Xuất lúc", report.GeneratedAt.ToString("yyyy-MM-dd HH:mm"))
        };
        string[] headers = { "Học sinh", "Khoản thu", "Số tiền", "Trạng thái", "Hạn nộp", "Ngày trả", "Biên lai" };

        return BuildTable("BÁO CÁO HỌC PHÍ", meta, headers, report.Rows.Count, (table, i) =>
        {
            var row = report.Rows[i];
            Cell(table, row.StudentName);
            Cell(table, row.FeeCategoryName);
            Cell(table, row.Amount.ToString("#,##0"));
            Cell(table, row.Status);
            Cell(table, row.DueDate.ToString("yyyy-MM-dd"));
            Cell(table, row.PaidAt?.ToString("yyyy-MM-dd") ?? "-");
            Cell(table, row.ReceiptNumber ?? "-");
        }, new float[] { 2.5f, 2.5f, 2, 1.8f, 1.8f, 1.8f, 2 });
    }

    // ─── Khung dựng PDF chung ──────────────────────────────────────────────

    private static byte[] BuildTable(
        string title,
        (string Label, string Value)[] meta,
        string[] headers,
        int rowCount,
        Action<TableDescriptor, int> fillRow,
        float[] columnWeights)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(16).Bold().FontColor(BrandOrange);
                    col.Item().PaddingTop(4).Text(
                        string.Join("   |   ", meta.Select(m => $"{m.Label}: {m.Value}")))
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var w in columnWeights)
                            cols.RelativeColumn(w);
                    });

                    // Header
                    table.Header(h =>
                    {
                        foreach (var head in headers)
                        {
                            h.Cell().Background(BrandOrange).Padding(4)
                                .Text(head).FontColor(Colors.White).Bold();
                        }
                    });

                    for (var i = 0; i < rowCount; i++)
                        fillRow(table, i);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Cell(TableDescriptor table, string text)
        => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .Padding(3).Text(text ?? "");
}
