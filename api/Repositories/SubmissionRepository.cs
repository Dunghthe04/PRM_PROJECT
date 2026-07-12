using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng Submissions (bài nộp).</summary>
public interface ISubmissionRepository
{
    /// <summary>Chi tiết bài nộp kèm Assignment, Student, GradedByTeacher.</summary>
    Task<Submission?> GetByIdAsync(int id);

    /// <summary>Tìm bài nộp của 1 HS cho 1 assignment (unique pair).</summary>
    Task<Submission?> GetByAssignmentAndStudentAsync(int assignmentId, int studentId);

    /// <summary>DS bài nộp của 1 assignment.</summary>
    Task<List<Submission>> GetByAssignmentAsync(int assignmentId);

    /// <summary>Thêm bài nộp mới.</summary>
    Task<Submission> CreateAsync(Submission entity);

    /// <summary>Cập nhật (nộp lại / chấm điểm).</summary>
    Task UpdateAsync(Submission entity);
}

/// <summary>Implement ISubmissionRepository bằng EF Core.</summary>
public class SubmissionRepository : ISubmissionRepository
{
    private readonly AppDbContext _context;

    public SubmissionRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Submission> QueryWithIncludes() =>
        _context.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Student)
            .Include(s => s.GradedByTeacher);

    /// <inheritdoc />
    public async Task<Submission?> GetByIdAsync(int id)
    {
        return await QueryWithIncludes()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<Submission?> GetByAssignmentAndStudentAsync(int assignmentId, int studentId)
    {
        return await QueryWithIncludes()
            .FirstOrDefaultAsync(s =>
                s.AssignmentId == assignmentId &&
                s.StudentId == studentId);
    }

    /// <inheritdoc />
    public async Task<List<Submission>> GetByAssignmentAsync(int assignmentId)
    {
        return await QueryWithIncludes()
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.Student.FullName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Submission> CreateAsync(Submission entity)
    {
        _context.Submissions.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Submission entity)
    {
        _context.Submissions.Update(entity);
        await _context.SaveChangesAsync();
    }
}
