using Api.DTOs;
using Api.Models;
using ClosedXML.Excel;

namespace Api.Common;

/// <summary>
/// Đọc / ghi file Excel import user (FR5.1 — Ngày 5 Bước 6).
/// Tách riêng khỏi Service để AdminUserService chỉ lo nghiệp vụ, không lo format Excel.
/// </summary>
public static class UserExcelHelper
{
    /// <summary>Tên sheet và tên file mẫu tải về.</summary>
    public const string SheetName = "Users";
    public const string TemplateFileName = "FSchool_ImportUsers_Template.xlsx";

    // Cột cố định — Admin điền theo đúng thứ tự trong file mẫu
    private const int ColPhone = 1;
    private const int ColFullName = 2;
    private const int ColPassword = 3;
    private const int ColRole = 4;
    private const int ColEmail = 5;

    /// <summary>
    /// Tạo file Excel mẫu có header + 1 dòng ví dụ.
    /// Admin tải về → điền → upload lại qua POST /api/users/import.
    /// </summary>
    public static byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName);

        // Header
        ws.Cell(1, ColPhone).Value = "Phone";
        ws.Cell(1, ColFullName).Value = "FullName";
        ws.Cell(1, ColPassword).Value = "Password";
        ws.Cell(1, ColRole).Value = "Role";
        ws.Cell(1, ColEmail).Value = "Email";

        // Dòng mẫu — Admin tham khảo format
        ws.Cell(2, ColPhone).Value = "0901234567";
        ws.Cell(2, ColFullName).Value = "Nguyen Van A";
        ws.Cell(2, ColPassword).Value = "Test@123";
        ws.Cell(2, ColRole).Value = "Student";
        ws.Cell(2, ColEmail).Value = "a@example.com";

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Đọc stream Excel → danh sách dòng + lỗi parse (nếu có).
    /// Bỏ qua dòng trống; dòng 1 = header.
    /// </summary>
    public static List<(int RowNumber, ImportUserRowDto? Row, string? Error)> ParseImportStream(Stream stream)
    {
        var results = new List<(int, ImportUserRowDto?, string?)>();

        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.TryGetWorksheet(SheetName, out var namedSheet)
            ? namedSheet
            : workbook.Worksheet(1);

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastRow <= 1)
        {
            results.Add((0, null, "File Excel không có dữ liệu (chỉ có header hoặc trống)."));
            return results;
        }

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var phone = ws.Cell(rowNum, ColPhone).GetString().Trim();
            var fullName = ws.Cell(rowNum, ColFullName).GetString().Trim();
            var password = ws.Cell(rowNum, ColPassword).GetString().Trim();
            var roleText = ws.Cell(rowNum, ColRole).GetString().Trim();
            var email = ws.Cell(rowNum, ColEmail).GetString().Trim();

            // Bỏ qua dòng hoàn toàn trống
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(password))
                continue;

            if (string.IsNullOrEmpty(phone))
            {
                results.Add((rowNum, null, "Thiếu số điện thoại."));
                continue;
            }

            if (string.IsNullOrEmpty(fullName))
            {
                results.Add((rowNum, null, "Thiếu họ tên."));
                continue;
            }

            if (string.IsNullOrEmpty(password))
            {
                results.Add((rowNum, null, "Thiếu mật khẩu."));
                continue;
            }

            if (!TryParseRole(roleText, out var role, out var roleError))
            {
                results.Add((rowNum, null, roleError!));
                continue;
            }

            results.Add((rowNum, new ImportUserRowDto
            {
                Phone = phone,
                FullName = fullName,
                Password = password,
                Role = role,
                Email = string.IsNullOrEmpty(email) ? null : email
            }, null));
        }

        return results;
    }

    /// <summary>
    /// Parse Role từ text ("Student") hoặc số ("4" = Student).
    /// </summary>
    private static bool TryParseRole(string text, out UserRole role, out string? error)
    {
        role = UserRole.Student;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Thiếu Role (Admin, Teacher, Parent, Student).";
            return false;
        }

        if (int.TryParse(text, out var roleInt) && Enum.IsDefined(typeof(UserRole), roleInt))
        {
            role = (UserRole)roleInt;
            return true;
        }

        if (Enum.TryParse<UserRole>(text, ignoreCase: true, out role))
            return true;

        error = $"Role không hợp lệ: '{text}'. Dùng: Admin, Teacher, Parent, Student.";
        return false;
    }
}
