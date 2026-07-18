using System.Globalization;
using Api.Common;
using Api.DTOs;
using Api.Models;
using Api.Repositories;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Import / xuất template Excel (Admin: user; Teacher/Admin: điểm).
/// Không đụng DbSeeder — chỉ thêm dữ liệu qua file tải lên.
/// </summary>
public interface IExcelImportService
{
    byte[] BuildUserImportTemplate();
    Task<ExcelImportResultDto> ImportUsersAsync(Stream excelStream);

    /// <summary>
    /// File mẫu tiếng Việt, sẵn danh sách HS lớp đã chọn.
    /// Cột điểm: Miệng | 15 phút | 1 tiết | Giữa kỳ | Cuối kỳ.
    /// </summary>
    Task<(byte[]? Bytes, string? FileName, string? Error)> BuildGradeImportTemplateAsync(
        int classId, int subjectId, int actorId, UserRole actorRole);

    /// <summary>
    /// Import điểm theo lớp + môn đã chọn trên UI (không đọc kỳ/lớp/môn từ Excel).
    /// Điểm có giá trị sẽ được công bố ngay (không còn cột Publish).
    /// </summary>
    Task<(ExcelImportResultDto? Result, string? Error)> ImportGradesAsync(
        Stream excelStream, int classId, int subjectId, int actorId, UserRole actorRole);
}

public class ExcelImportService : IExcelImportService
{
    private readonly IAdminUserService _adminUserService;
    private readonly IUserRepository _userRepository;
    private readonly IGradeRepository _gradeRepository;
    private readonly IClassRepository _classRepository;
    private readonly ITeacherAssignmentRepository _assignmentRepository;
    private readonly AppDbContext _context;

    public ExcelImportService(
        IAdminUserService adminUserService,
        IUserRepository userRepository,
        IGradeRepository gradeRepository,
        IClassRepository classRepository,
        ITeacherAssignmentRepository assignmentRepository,
        AppDbContext context)
    {
        _adminUserService = adminUserService;
        _userRepository = userRepository;
        _gradeRepository = gradeRepository;
        _classRepository = classRepository;
        _assignmentRepository = assignmentRepository;
        _context = context;
    }

    /// <inheritdoc />
    public byte[] BuildUserImportTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Users");
        var headers = new[]
        {
            "Phone", "Password", "FullName", "Email", "Role", "ClassName"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "0912000001";
        ws.Cell(2, 2).Value = "123456";
        ws.Cell(2, 3).Value = "Nguyen Van Demo";
        ws.Cell(2, 4).Value = "demo@gmail.com";
        ws.Cell(2, 5).Value = "Student";
        ws.Cell(2, 6).Value = "10A1";

        ws.Cell(3, 1).Value = "0912000002";
        ws.Cell(3, 2).Value = "123456";
        ws.Cell(3, 3).Value = "Tran Thi GV";
        ws.Cell(3, 4).Value = "gv@gmail.com";
        ws.Cell(3, 5).Value = "Teacher";
        ws.Cell(3, 6).Value = "";

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <inheritdoc />
    public async Task<ExcelImportResultDto> ImportUsersAsync(Stream excelStream)
    {
        var result = new ExcelImportResultDto();
        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var row = 2; row <= lastRow; row++)
        {
            var phone = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                result.SkipCount++;
                continue;
            }

            var password = ws.Cell(row, 2).GetString().Trim();
            var fullName = ws.Cell(row, 3).GetString().Trim();
            var email = ws.Cell(row, 4).GetString().Trim();
            var roleText = ws.Cell(row, 5).GetString().Trim();
            var className = ws.Cell(row, 6).GetString().Trim();

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: Password tối thiểu 6 ký tự.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: FullName bắt buộc.");
                continue;
            }

            if (!Enum.TryParse<UserRole>(roleText, ignoreCase: true, out var role))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: Role không hợp lệ (Student/Teacher/Parent/Admin).");
                continue;
            }

            var (created, err) = await _adminUserService.CreateAsync(new AdminCreateUserDto
            {
                Phone = phone,
                Password = password,
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Role = role,
                IsPhoneVerified = true
            });

            if (err != null || created == null)
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: {err ?? "Tạo user thất bại."}");
                continue;
            }

            // Gán HS vào lớp nếu có ClassName (lớp kỳ đang diễn ra ưu tiên, không thì lớp tên khớp mới nhất).
            if (role == UserRole.Student && !string.IsNullOrWhiteSpace(className))
            {
                var cls = await ResolveClassByNameAsync(className);
                if (cls == null)
                {
                    result.Errors.Add($"Dòng {row}: Đã tạo user nhưng không tìm thấy lớp '{className}'.");
                }
                else if (!await _classRepository.StudentInClassAsync(cls.Id, created.Id))
                {
                    await _classRepository.AddStudentAsync(new ClassStudent
                    {
                        ClassId = cls.Id,
                        StudentId = created.Id
                    });
                }
            }

            result.SuccessCount++;
        }

        result.Message =
            $"Import xong: {result.SuccessCount} thành công, {result.SkipCount} bỏ qua, {result.ErrorCount} lỗi.";
        return result;
    }

    // Cột điểm theo sổ THPT (ThptGradeCatalog)
    private static readonly (string Header, string AssessmentType)[] GradeScoreColumns =
        ThptGradeCatalog.Columns.Select(c => (c.HeaderVi, c.Code)).ToArray();

    /// <inheritdoc />
    public async Task<(byte[]? Bytes, string? FileName, string? Error)> BuildGradeImportTemplateAsync(
        int classId, int subjectId, int actorId, UserRole actorRole)
    {
        var (cls, subject, error) = await ResolveClassSubjectAsync(classId, subjectId, actorId, actorRole);
        if (error != null) return (null, null, error);

        var students = await _classRepository.GetStudentsAsync(classId);
        students = students.OrderBy(s => s.Student.FullName).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Điểm");

        ws.Cell(1, 1).Value =
            $"Bảng điểm — Lớp {cls!.Name} — Môn {subject!.Name} ({subject.Code}) — {cls.Semester?.Name ?? ""}";
        ws.Range(1, 1, 1, 2 + GradeScoreColumns.Length).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;

        ws.Cell(2, 1).Value = "Số điện thoại";
        ws.Cell(2, 2).Value = "Họ tên";
        for (var i = 0; i < GradeScoreColumns.Length; i++)
            ws.Cell(2, 3 + i).Value = GradeScoreColumns[i].Header;
        ws.Row(2).Style.Font.Bold = true;

        var row = 3;
        foreach (var link in students)
        {
            var phone = link.Student.Phone ?? "";
            ws.Cell(row, 1).Style.NumberFormat.Format = "@";
            ws.Cell(row, 1).Value = phone;
            ws.Cell(row, 2).Value = link.Student.FullName;
            // Cột điểm để trống — GV điền
            row++;
        }

        if (students.Count == 0)
        {
            ws.Cell(3, 1).Style.NumberFormat.Format = "@";
            ws.Cell(3, 1).Value = "0900000004";
            ws.Cell(3, 2).Value = "(ví dụ — lớp chưa có HS)";
            ws.Cell(3, 5).Value = 8.5; // Miệng 3 mẫu
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var safeClass = SanitizeFilePart(cls.Name);
        var safeSubject = SanitizeFilePart(subject.Code);
        var fileName = $"Diem_{safeClass}_{safeSubject}.xlsx";
        return (ms.ToArray(), fileName, null);
    }

    /// <inheritdoc />
    public async Task<(ExcelImportResultDto? Result, string? Error)> ImportGradesAsync(
        Stream excelStream, int classId, int subjectId, int actorId, UserRole actorRole)
    {
        var (cls, subject, resolveError) = await ResolveClassSubjectAsync(
            classId, subjectId, actorId, actorRole);
        if (resolveError != null) return (null, resolveError);

        var semesterId = cls!.SemesterId;
        var result = new ExcelImportResultDto();

        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // Tìm dòng tiêu đề có "Số điện thoại"
        var headerRow = 0;
        for (var r = 1; r <= Math.Min(5, lastRow); r++)
        {
            for (var c = 1; c <= 10; c++)
            {
                if (NormalizeHeader(ws.Cell(r, c).GetString()) == "số điện thoại")
                {
                    headerRow = r;
                    break;
                }
            }
            if (headerRow > 0) break;
        }

        if (headerRow == 0)
            return (null, "File Excel không đúng mẫu — cần cột «Số điện thoại».");

        // Map cột điểm theo header tiếng Việt (hoặc mã EN)
        var colPhone = 0;
        var colByAssessment = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 10;
        for (var c = 1; c <= lastCol; c++)
        {
            var h = NormalizeHeader(ws.Cell(headerRow, c).GetString());
            if (h is "số điện thoại" or "so dien thoai" or "studentphone")
                colPhone = c;
            else
            {
                var type = MapAssessmentHeader(h);
                if (type != null) colByAssessment[type] = c;
            }
        }

        if (colPhone == 0)
            return (null, "Thiếu cột «Số điện thoại».");
        if (colByAssessment.Count == 0)
            return (null, "Thiếu cột điểm (Miệng 1–3 / 15 phút 1–3 / 1 tiết 1–2 / Giữa kỳ / Cuối kỳ).");

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var phone = ws.Cell(row, colPhone).GetFormattedString().Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                result.SkipCount++;
                continue;
            }

            // Excel có thể bỏ số 0 đầu → chuẩn hóa
            if (phone.All(char.IsDigit) && phone.Length == 9)
                phone = "0" + phone;

            var student = await _userRepository.GetUserByPhoneAsync(phone);
            if (student == null || student.Role != UserRole.Student)
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: Không tìm thấy học sinh SĐT {phone}.");
                continue;
            }

            if (!await _classRepository.StudentInClassAsync(classId, student.Id))
            {
                result.ErrorCount++;
                result.Errors.Add($"Dòng {row}: HS không thuộc lớp {cls.Name}.");
                continue;
            }

            var rowHadScore = false;
            foreach (var (assessmentType, col) in colByAssessment)
            {
                var scoreCell = ws.Cell(row, col);
                if (scoreCell.IsEmpty() || string.IsNullOrWhiteSpace(scoreCell.GetString()))
                    continue;

                if (!TryGetDouble(scoreCell, out var score) || score < 0 || score > 10)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Dòng {row} ({assessmentType}): Điểm phải trong khoảng 0–10.");
                    continue;
                }

                rowHadScore = true;
                await UpsertPublishedGradeAsync(
                    student.Id, classId, subject!.Id, semesterId, assessmentType, score, actorId);
                result.SuccessCount++;
            }

            if (!rowHadScore)
                result.SkipCount++;
        }

        result.Message =
            $"Import điểm lớp {cls.Name} — {subject!.Name}: {result.SuccessCount} ô điểm, " +
            $"{result.SkipCount} bỏ qua, {result.ErrorCount} lỗi. Điểm đã công bố.";
        return (result, null);
    }

    private async Task UpsertPublishedGradeAsync(
        int studentId, int classId, int subjectId, int semesterId,
        string assessmentType, double score, int teacherId)
    {
        var existing = await _gradeRepository.FindByKeyAsync(
            studentId, classId, subjectId, semesterId, assessmentType);

        if (existing != null)
        {
            existing.Score = score;
            existing.UpdatedAt = DateTime.UtcNow;
            if (existing.Status != GradeStatus.Published)
            {
                existing.Status = GradeStatus.Published;
                existing.PublishedAt = DateTime.UtcNow;
            }

            await _gradeRepository.UpdateAsync(existing);
        }
        else
        {
            await _gradeRepository.CreateAsync(new Grade
            {
                StudentId = studentId,
                ClassId = classId,
                SubjectId = subjectId,
                SemesterId = semesterId,
                AssessmentType = assessmentType,
                Score = score,
                Status = GradeStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedByTeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<(Class? Cls, Subject? Subject, string? Error)> ResolveClassSubjectAsync(
        int classId, int subjectId, int actorId, UserRole actorRole)
    {
        if (actorRole is not (UserRole.Teacher or UserRole.Admin))
            return (null, null, "Chỉ Giáo viên hoặc Admin được import điểm.");

        var cls = await _context.Classes.AsNoTracking()
            .Include(c => c.Semester)
            .FirstOrDefaultAsync(c => c.Id == classId);
        if (cls == null) return (null, null, "Không tìm thấy lớp.");

        var subject = await _context.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subjectId);
        if (subject == null) return (null, null, "Không tìm thấy môn học.");

        if (actorRole == UserRole.Teacher)
        {
            var assigned = await _assignmentRepository.GetByTeacherAsync(actorId);
            if (!assigned.Any(ta => ta.ClassId == classId && ta.SubjectId == subjectId))
                return (null, null, $"Bạn chưa được phân công dạy {subject.Name} tại lớp {cls.Name}.");
        }

        return (cls, subject, null);
    }

    private static string? MapAssessmentHeader(string normalized) =>
        ThptGradeCatalog.MapHeaderToCode(normalized);

    private static string NormalizeHeader(string raw) =>
        ThptGradeCatalog.Normalize(raw);

    private static string SanitizeFilePart(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(s.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "x" : cleaned;
    }

    /// <summary>Tìm lớp theo tên — ưu tiên kỳ đang diễn ra (dùng khi import user).</summary>
    private async Task<Class?> ResolveClassByNameAsync(string className)
    {
        var today = DateTime.UtcNow.Date;
        var list = await _context.Classes.AsNoTracking()
            .Include(c => c.Semester)
            .Where(c => c.Name == className)
            .OrderByDescending(c => c.Id)
            .ToListAsync();

        if (list.Count == 0) return null;

        var current = list.FirstOrDefault(c =>
            c.Semester != null
            && c.Semester.StartDate.Date <= today
            && c.Semester.EndDate.Date >= today);
        return current ?? list[0];
    }

    private static bool TryGetDouble(IXLCell cell, out double value)
    {
        value = 0;
        if (cell.TryGetValue(out double d))
        {
            value = d;
            return true;
        }

        var s = cell.GetString().Trim().Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}
