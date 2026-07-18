using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng LeaveRequests (FR2.5, FR3.3 — Ngày 9 Bước 3).</summary>
public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(int id);

    /// <summary>GV lọc đơn theo lớp + trạng thái.</summary>
    Task<List<LeaveRequest>> GetListAsync(int? classId, LeaveRequestStatus? status);

    /// <summary>Đơn của 1 hoặc nhiều học sinh (HS/PH xem của mình/con).</summary>
    Task<List<LeaveRequest>> GetByStudentIdsAsync(IReadOnlyList<int> studentIds);

    Task<LeaveRequest> CreateAsync(LeaveRequest entity);
    Task UpdateAsync(LeaveRequest entity);
    Task DeleteAsync(LeaveRequest entity);
}

/// <summary>Implement ILeaveRequestRepository bằng EF Core.</summary>
public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<LeaveRequest> WithDetails()
        => _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.Class)
            .Include(lr => lr.Student)
            .Include(lr => lr.SubmittedBy)
            .Include(lr => lr.ApprovedByTeacher);

    /// <inheritdoc />
    public async Task<LeaveRequest?> GetByIdAsync(int id)
    {
        return await WithDetails().FirstOrDefaultAsync(lr => lr.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequest>> GetListAsync(int? classId, LeaveRequestStatus? status)
    {
        // Projection cột cần thiết — tránh Include nặng lần đầu.
        var q = _context.LeaveRequests.AsNoTracking().AsQueryable();

        if (classId.HasValue)
            q = q.Where(lr => lr.ClassId == classId.Value);

        if (status.HasValue)
            q = q.Where(lr => lr.Status == status.Value);

        var rows = await q
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new
            {
                lr.Id,
                lr.StudentId,
                StudentName = lr.Student.FullName,
                StudentPhone = lr.Student.Phone,
                lr.ClassId,
                ClassName = lr.Class.Name,
                lr.SubmittedByUserId,
                SubmittedByName = lr.SubmittedBy.FullName,
                lr.Date,
                lr.Reason,
                lr.MedicalCertificateUrl,
                lr.Status,
                lr.ApprovedByTeacherId,
                ApprovedByTeacherName = lr.ApprovedByTeacher != null
                    ? lr.ApprovedByTeacher.FullName
                    : null,
                lr.RejectionReason,
                lr.CreatedAt,
                lr.UpdatedAt,
                lr.ReviewedAt,
            })
            .ToListAsync();

        return rows.Select(r => new LeaveRequest
        {
            Id = r.Id,
            StudentId = r.StudentId,
            ClassId = r.ClassId,
            SubmittedByUserId = r.SubmittedByUserId,
            Date = r.Date,
            Reason = r.Reason,
            MedicalCertificateUrl = r.MedicalCertificateUrl,
            Status = r.Status,
            ApprovedByTeacherId = r.ApprovedByTeacherId,
            RejectionReason = r.RejectionReason,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            ReviewedAt = r.ReviewedAt,
            Student = new User { Id = r.StudentId, FullName = r.StudentName, Phone = r.StudentPhone },
            Class = new Class { Id = r.ClassId, Name = r.ClassName },
            SubmittedBy = new User { Id = r.SubmittedByUserId, FullName = r.SubmittedByName },
            ApprovedByTeacher = r.ApprovedByTeacherId == null
                ? null
                : new User { Id = r.ApprovedByTeacherId.Value, FullName = r.ApprovedByTeacherName ?? "" },
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<LeaveRequest>> GetByStudentIdsAsync(IReadOnlyList<int> studentIds)
    {
        if (studentIds.Count == 0)
            return new List<LeaveRequest>();

        return await WithDetails()
            .Where(lr => studentIds.Contains(lr.StudentId))
            .OrderByDescending(lr => lr.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<LeaveRequest> CreateAsync(LeaveRequest entity)
    {
        entity.Date = entity.Date.Date;
        _context.LeaveRequests.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(LeaveRequest entity)
    {
        entity.Date = entity.Date.Date;
        _context.LeaveRequests.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(LeaveRequest entity)
    {
        _context.LeaveRequests.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
