using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ báo cáo & thống kê (FR5.5 — Ngày 12 Bước 2).
/// Tổng hợp bảng điểm, chuyên cần, học phí + dashboard cho Admin.
/// Chỉ đọc dữ liệu (không thay đổi state); xuất Excel/PDF làm ở Bước 3–4.
/// </summary>
public interface IReportService
{
    /// <summary>Số liệu tổng quan toàn trường (hoặc theo lớp nếu truyền classId).</summary>
    Task<ReportDashboardDto> GetDashboardAsync(int? classId = null);

    /// <summary>Báo cáo bảng điểm đã Published (lọc theo lớp/kỳ/môn).</summary>
    Task<(GradeReportDto? Result, string? Error)> GetGradeReportAsync(ReportQueryDto query);

    /// <summary>Báo cáo chuyên cần theo lớp + khoảng ngày.</summary>
    Task<(AttendanceReportDto? Result, string? Error)> GetAttendanceReportAsync(ReportQueryDto query);

    /// <summary>Báo cáo tình trạng đóng học phí (tuỳ chọn lọc theo lớp).</summary>
    Task<(FeeReportDto? Result, string? Error)> GetFeeReportAsync(ReportQueryDto query);
}

/// <summary>Implement IReportService bằng EF Core (aggregate trực tiếp).</summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<ReportDashboardDto> GetDashboardAsync(int? classId = null)
    {
        // Phạm vi học sinh: cả trường hoặc trong 1 lớp
        var studentIdsInScope = classId.HasValue
            ? await _context.ClassStudents
                .Where(cs => cs.ClassId == classId.Value)
                .Select(cs => cs.StudentId)
                .ToListAsync()
            : null;

        var totalStudents = studentIdsInScope != null
            ? studentIdsInScope.Count
            : await _context.Users.CountAsync(u => u.Role == UserRole.Student);

        var totalTeachers = await _context.Users
            .CountAsync(u => u.Role == UserRole.Teacher);
        var totalParents = await _context.Users.CountAsync(u => u.Role == UserRole.Parent);
        var totalClasses = classId.HasValue ? 1 : await _context.Classes.CountAsync();

        // Điểm đã công bố
        var gradeQuery = _context.Grades.Where(g => g.Status == GradeStatus.Published);
        if (classId.HasValue) gradeQuery = gradeQuery.Where(g => g.ClassId == classId.Value);
        var publishedGradeCount = await gradeQuery.CountAsync();

        // Chuyên cần trung bình
        var attQuery = _context.Attendances.AsQueryable();
        if (classId.HasValue) attQuery = attQuery.Where(a => a.ClassId == classId.Value);
        var totalAtt = await attQuery.CountAsync();
        double? avgAttendance = null;
        if (totalAtt > 0)
        {
            var presentLate = await attQuery
                .CountAsync(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            avgAttendance = Math.Round(presentLate * 100.0 / totalAtt, 2);
        }

        // Học phí
        var feeQuery = _context.FeeInvoices.AsQueryable();
        if (studentIdsInScope != null)
            feeQuery = feeQuery.Where(i => studentIdsInScope.Contains(i.StudentId));

        var invoices = await feeQuery
            .Select(i => new { i.Status, i.Amount })
            .ToListAsync();

        var paid = invoices.Where(i => i.Status == FeePaymentStatus.Paid).ToList();
        var pending = invoices.Where(i => i.Status == FeePaymentStatus.Pending).ToList();

        return new ReportDashboardDto
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TotalClasses = totalClasses,
            TotalParents = totalParents,
            PublishedGradeCount = publishedGradeCount,
            AverageAttendanceRate = avgAttendance,
            PaidInvoiceCount = paid.Count,
            PendingInvoiceCount = pending.Count,
            TotalPaidAmount = paid.Sum(i => i.Amount),
            TotalPendingAmount = pending.Sum(i => i.Amount),
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<(GradeReportDto? Result, string? Error)> GetGradeReportAsync(ReportQueryDto query)
    {
        var q = _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Where(g => g.Status == GradeStatus.Published);

        if (query.ClassId.HasValue) q = q.Where(g => g.ClassId == query.ClassId.Value);
        if (query.SemesterId.HasValue) q = q.Where(g => g.SemesterId == query.SemesterId.Value);
        if (query.SubjectId.HasValue) q = q.Where(g => g.SubjectId == query.SubjectId.Value);

        var grades = await q
            .OrderBy(g => g.Student.FullName)
            .ThenBy(g => g.Subject.Name)
            .ThenBy(g => g.AssessmentType)
            .ToListAsync();

        var className = query.ClassId.HasValue
            ? (await _context.Classes.FindAsync(query.ClassId.Value))?.Name
            : null;
        var semesterName = query.SemesterId.HasValue
            ? (await _context.Semesters.FindAsync(query.SemesterId.Value))?.Name
            : null;
        var subjectName = query.SubjectId.HasValue
            ? (await _context.Subjects.FindAsync(query.SubjectId.Value))?.Name
            : null;

        return (new GradeReportDto
        {
            ClassId = query.ClassId,
            ClassName = className,
            SemesterId = query.SemesterId,
            SemesterName = semesterName,
            SubjectId = query.SubjectId,
            SubjectName = subjectName,
            Rows = grades.Select(g => new GradeReportRowDto
            {
                StudentId = g.StudentId,
                StudentName = g.Student.FullName,
                StudentPhone = g.Student.Phone,
                SubjectName = g.Subject.Name,
                AssessmentType = g.AssessmentType,
                Score = g.Score,
                Status = g.Status.ToString(),
                IsApproved = g.IsApproved,
                PublishedAt = g.PublishedAt
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        }, null);
    }

    /// <inheritdoc />
    public async Task<(AttendanceReportDto? Result, string? Error)> GetAttendanceReportAsync(ReportQueryDto query)
    {
        if (!query.ClassId.HasValue)
            return (null, "classId là bắt buộc cho báo cáo chuyên cần.");

        var cls = await _context.Classes.FindAsync(query.ClassId.Value);
        if (cls == null) return (null, "Không tìm thấy lớp.");

        // Mặc định 30 ngày gần nhất nếu không truyền
        var to = (query.To ?? DateTime.UtcNow).Date;
        var from = (query.From ?? to.AddDays(-30)).Date;
        if (from > to) (from, to) = (to, from);

        var records = await _context.Attendances
            .Include(a => a.Student)
            .Where(a => a.ClassId == query.ClassId.Value
                     && a.Date >= from && a.Date <= to)
            .ToListAsync();

        // Đảm bảo mọi HS trong lớp đều có dòng (kể cả 0 buổi)
        var studentsInClass = await _context.ClassStudents
            .Include(cs => cs.Student)
            .Where(cs => cs.ClassId == query.ClassId.Value)
            .Select(cs => cs.Student)
            .ToListAsync();

        var rows = new List<AttendanceReportRowDto>();
        foreach (var student in studentsInClass.OrderBy(s => s.FullName))
        {
            var studentRecords = records.Where(r => r.StudentId == student.Id).ToList();
            var present = studentRecords.Count(r => r.Status == AttendanceStatus.Present);
            var absent = studentRecords.Count(r => r.Status == AttendanceStatus.Absent);
            var late = studentRecords.Count(r => r.Status == AttendanceStatus.Late);
            var total = studentRecords.Count;
            var rate = total > 0 ? Math.Round((present + late) * 100.0 / total, 2) : 0;

            rows.Add(new AttendanceReportRowDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                StudentPhone = student.Phone,
                PresentCount = present,
                AbsentCount = absent,
                LateCount = late,
                TotalSessions = total,
                AttendanceRate = rate
            });
        }

        var totalSessions = records.Count;
        var classPresentLate = records
            .Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);
        var classRate = totalSessions > 0
            ? Math.Round(classPresentLate * 100.0 / totalSessions, 2)
            : 0;

        return (new AttendanceReportDto
        {
            ClassId = cls.Id,
            ClassName = cls.Name,
            From = from,
            To = to,
            ClassAttendanceRate = classRate,
            Rows = rows,
            GeneratedAt = DateTime.UtcNow
        }, null);
    }

    /// <inheritdoc />
    public async Task<(FeeReportDto? Result, string? Error)> GetFeeReportAsync(ReportQueryDto query)
    {
        var q = _context.FeeInvoices
            .Include(i => i.Student)
            .Include(i => i.FeeCategory)
            .AsQueryable();

        string? className = null;
        if (query.ClassId.HasValue)
        {
            var cls = await _context.Classes.FindAsync(query.ClassId.Value);
            if (cls == null) return (null, "Không tìm thấy lớp.");
            className = cls.Name;

            var studentIds = await _context.ClassStudents
                .Where(cs => cs.ClassId == query.ClassId.Value)
                .Select(cs => cs.StudentId)
                .ToListAsync();
            q = q.Where(i => studentIds.Contains(i.StudentId));
        }

        var invoices = await q
            .OrderBy(i => i.Student.FullName)
            .ThenByDescending(i => i.DueDate)
            .ToListAsync();

        var paidCount = invoices.Count(i => i.Status == FeePaymentStatus.Paid);
        var pendingCount = invoices.Count(i => i.Status == FeePaymentStatus.Pending);
        var failedCount = invoices.Count(i =>
            i.Status == FeePaymentStatus.Failed || i.Status == FeePaymentStatus.Cancelled);

        return (new FeeReportDto
        {
            ClassId = query.ClassId,
            ClassName = className,
            TotalInvoices = invoices.Count,
            PaidCount = paidCount,
            PendingCount = pendingCount,
            FailedOrCancelledCount = failedCount,
            TotalAmount = invoices.Sum(i => i.Amount),
            PaidAmount = invoices.Where(i => i.Status == FeePaymentStatus.Paid).Sum(i => i.Amount),
            PendingAmount = invoices.Where(i => i.Status == FeePaymentStatus.Pending).Sum(i => i.Amount),
            PaidRate = invoices.Count > 0 ? Math.Round(paidCount * 100.0 / invoices.Count, 2) : 0,
            Rows = invoices.Select(i => new FeeReportRowDto
            {
                InvoiceId = i.Id,
                StudentId = i.StudentId,
                StudentName = i.Student.FullName,
                FeeCategoryName = i.FeeCategory.Name,
                Amount = i.Amount,
                Status = i.Status.ToString(),
                IsPaid = i.IsPaid,
                DueDate = i.DueDate,
                PaidAt = i.PaidAt,
                PaymentMethod = i.PaymentMethod,
                ReceiptNumber = i.ReceiptNumber
            }).ToList(),
            GeneratedAt = DateTime.UtcNow
        }, null);
    }
}
