using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng Attendances (FR3.1 — Ngày 7 Bước 3).</summary>
public interface IAttendanceRepository
{
    /// <summary>Chi tiết 1 bản ghi kèm Class, Student, GV ghi nhận.</summary>
    Task<Attendance?> GetByIdAsync(int id);

    /// <summary>Điểm danh của lớp theo ngày — GV xem/sửa trên lớp.</summary>
    Task<List<Attendance>> GetByClassAndDateAsync(int classId, DateTime date);

    /// <summary>Lịch sử chuyên cần của 1 học sinh trong khoảng ngày.</summary>
    Task<List<Attendance>> GetByStudentAsync(int studentId, DateTime? from, DateTime? to);

    /// <summary>
    /// Tất cả bản ghi điểm danh lớp trong khoảng ngày — dùng tính summary.
    /// </summary>
    Task<List<Attendance>> GetByClassAsync(int classId, DateTime? from, DateTime? to);

    /// <summary>
    /// Tìm theo khóa nghiệp vụ (ClassId + StudentId + Date) — upsert batch.
    /// </summary>
    Task<Attendance?> FindByKeyAsync(int classId, int studentId, DateTime date);

    /// <summary>Thêm bản ghi mới.</summary>
    Task<Attendance> CreateAsync(Attendance attendance);

    /// <summary>Cập nhật bản ghi.</summary>
    Task UpdateAsync(Attendance attendance);

    /// <summary>Lưu nhiều thay đổi trong 1 transaction (batch).</summary>
    Task SaveChangesAsync();
}

/// <summary>Implement IAttendanceRepository bằng EF Core.</summary>
public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;

    public AttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Include navigation — map sang AttendanceDto không query N+1.</summary>
    private IQueryable<Attendance> WithDetails()
        => _context.Attendances
            .Include(a => a.Class)
            .Include(a => a.Student)
            .Include(a => a.RecordedByTeacher);

    /// <summary>Chuẩn hóa DateTime về 00:00 UTC để khớp unique index theo ngày.</summary>
    private static DateTime NormalizeDate(DateTime date)
        => date.Date;

    /// <inheritdoc />
    public async Task<Attendance?> GetByIdAsync(int id)
    {
        return await WithDetails().FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<Attendance>> GetByClassAndDateAsync(int classId, DateTime date)
    {
        var day = NormalizeDate(date);
        return await WithDetails()
            .Where(a => a.ClassId == classId && a.Date == day)
            .OrderBy(a => a.Student.FullName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Attendance>> GetByStudentAsync(int studentId, DateTime? from, DateTime? to)
    {
        var q = WithDetails().Where(a => a.StudentId == studentId);

        if (from.HasValue)
            q = q.Where(a => a.Date >= NormalizeDate(from.Value));

        if (to.HasValue)
            q = q.Where(a => a.Date <= NormalizeDate(to.Value));

        return await q
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Attendance>> GetByClassAsync(int classId, DateTime? from, DateTime? to)
    {
        var q = _context.Attendances.Where(a => a.ClassId == classId);

        if (from.HasValue)
            q = q.Where(a => a.Date >= NormalizeDate(from.Value));

        if (to.HasValue)
            q = q.Where(a => a.Date <= NormalizeDate(to.Value));

        return await q.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Attendance?> FindByKeyAsync(int classId, int studentId, DateTime date)
    {
        var day = NormalizeDate(date);
        return await _context.Attendances.FirstOrDefaultAsync(a =>
            a.ClassId == classId && a.StudentId == studentId && a.Date == day);
    }

    /// <inheritdoc />
    public async Task<Attendance> CreateAsync(Attendance attendance)
    {
        attendance.Date = NormalizeDate(attendance.Date);
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Attendance attendance)
    {
        attendance.Date = NormalizeDate(attendance.Date);
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
