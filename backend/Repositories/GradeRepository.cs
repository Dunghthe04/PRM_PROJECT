using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng Grades (FR3.2 — Ngày 6 Bước 3).</summary>
public interface IGradeRepository
{
    /// <summary>Chi tiết 1 bản ghi điểm kèm navigation (Student, Class, Subject...).</summary>
    Task<Grade?> GetByIdAsync(int id);

    /// <summary>
    /// Danh sách điểm theo lớp/môn/kỳ — GV xem bảng điểm (cả Draft + Published).
    /// </summary>
    Task<List<Grade>> GetListAsync(GradeListQueryDto query);

    /// <summary>
    /// Điểm đã Published của 1 học sinh — HS/PH tra cứu bảng điểm.
    /// </summary>
    Task<List<Grade>> GetPublishedByStudentAsync(int studentId, int? semesterId = null);

    /// <summary>
    /// Tìm bản ghi theo khóa nghiệp vụ (upsert batch — đã có thì update, chưa có thì create).
    /// </summary>
    Task<Grade?> FindByKeyAsync(
        int studentId,
        int classId,
        int subjectId,
        int semesterId,
        string assessmentType);

    /// <summary>Thêm bản ghi điểm mới.</summary>
    Task<Grade> CreateAsync(Grade grade);

    /// <summary>Cập nhật bản ghi điểm.</summary>
    Task UpdateAsync(Grade grade);

    /// <summary>Xóa bản ghi điểm (chỉ khi Draft — Service sẽ kiểm tra).</summary>
    Task DeleteAsync(Grade grade);

    /// <summary>Lấy tất cả điểm Draft chờ công bố theo lớp/môn/kỳ/đầu điểm.</summary>
    Task<List<Grade>> GetDraftsForPublishAsync(
        int classId,
        int subjectId,
        int semesterId,
        string assessmentType);
}

/// <summary>Implement IGradeRepository bằng EF Core.</summary>
public class GradeRepository : IGradeRepository
{
    private readonly AppDbContext _context;

    public GradeRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Include các bảng liên quan — tránh query N+1 khi map sang GradeDto.
    /// </summary>
    private IQueryable<Grade> WithDetails()
        => _context.Grades
            .Include(g => g.Student)
            .Include(g => g.Class)
            .Include(g => g.Subject)
            .Include(g => g.Semester)
            .Include(g => g.CreatedByTeacher)
            .Include(g => g.ApprovedBy);

    /// <inheritdoc />
    public async Task<Grade?> GetByIdAsync(int id)
    {
        return await WithDetails().FirstOrDefaultAsync(g => g.Id == id);
    }

    /// <inheritdoc />
    public async Task<List<Grade>> GetListAsync(GradeListQueryDto query)
    {
        var q = WithDetails()
            .Where(g => g.ClassId == query.ClassId
                     && g.SubjectId == query.SubjectId
                     && g.SemesterId == query.SemesterId);

        if (!string.IsNullOrWhiteSpace(query.AssessmentType))
            q = q.Where(g => g.AssessmentType == query.AssessmentType.Trim());

        if (query.Status.HasValue)
            q = q.Where(g => g.Status == query.Status.Value);

        return await q
            .OrderBy(g => g.Student.FullName)
            .ThenBy(g => g.AssessmentType)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Grade>> GetPublishedByStudentAsync(int studentId, int? semesterId = null)
    {
        var q = WithDetails()
            .Where(g => g.StudentId == studentId && g.Status == GradeStatus.Published);

        if (semesterId.HasValue)
            q = q.Where(g => g.SemesterId == semesterId.Value);

        return await q
            .OrderByDescending(g => g.SemesterId)
            .ThenBy(g => g.Subject.Name)
            .ThenBy(g => g.AssessmentType)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Grade?> FindByKeyAsync(
        int studentId,
        int classId,
        int subjectId,
        int semesterId,
        string assessmentType)
    {
        return await _context.Grades.FirstOrDefaultAsync(g =>
            g.StudentId == studentId
            && g.ClassId == classId
            && g.SubjectId == subjectId
            && g.SemesterId == semesterId
            && g.AssessmentType == assessmentType);
    }

    /// <inheritdoc />
    public async Task<Grade> CreateAsync(Grade grade)
    {
        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();
        return grade;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Grade grade)
    {
        _context.Grades.Update(grade);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Grade grade)
    {
        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<Grade>> GetDraftsForPublishAsync(
        int classId,
        int subjectId,
        int semesterId,
        string assessmentType)
    {
        return await _context.Grades
            .Where(g => g.ClassId == classId
                     && g.SubjectId == subjectId
                     && g.SemesterId == semesterId
                     && g.AssessmentType == assessmentType
                     && g.Status == GradeStatus.Draft)
            .ToListAsync();
    }
}
