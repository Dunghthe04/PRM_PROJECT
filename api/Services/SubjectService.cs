using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>Nghiệp vụ môn học (FR5.2) — dùng cho phân công GV / TKB / điểm.</summary>
public interface ISubjectService
{
    /// <summary>Danh sách tất cả môn học.</summary>
    Task<List<SubjectDto>> GetAllAsync();

    /// <summary>Chi tiết 1 môn; null nếu không có.</summary>
    Task<SubjectDto?> GetByIdAsync(int id);

    /// <summary>Tạo môn mới (Code phải unique).</summary>
    Task<(SubjectDto? Result, string? Error)> CreateAsync(CreateUpdateSubjectDto dto);

    /// <summary>Cập nhật môn.</summary>
    Task<(SubjectDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateSubjectDto dto);

    /// <summary>Xóa môn (chặn nếu đang được phân công / có trong TKB).</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

/// <summary>Implement ISubjectService.</summary>
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repository;
    private readonly AppDbContext _context;

    public SubjectService(ISubjectRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<SubjectDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<SubjectDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    /// <summary>Tạo môn — Name + Code bắt buộc, Code không trùng.</summary>
    public async Task<(SubjectDto? Result, string? Error)> CreateAsync(CreateUpdateSubjectDto dto)
    {
        var error = Validate(dto);
        if (error != null) return (null, error);

        var code = NormalizeCode(dto.Code);
        if (await _repository.GetByCodeAsync(code) != null)
            return (null, "Mã môn đã tồn tại.");

        var entity = new Subject
        {
            Name = dto.Name.Trim(),
            Code = code
        };

        var created = await _repository.CreateAsync(entity);
        return (MapToDto(created), null);
    }

    /// <summary>Sửa môn — cho đổi Code nếu không trùng môn khác.</summary>
    public async Task<(SubjectDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateSubjectDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy môn học.");

        var error = Validate(dto);
        if (error != null) return (null, error);

        var code = NormalizeCode(dto.Code);
        var existing = await _repository.GetByCodeAsync(code);
        if (existing != null && existing.Id != id)
            return (null, "Mã môn đã tồn tại.");

        entity.Name = dto.Name.Trim();
        entity.Code = code;

        await _repository.UpdateAsync(entity);
        return (MapToDto(entity), null);
    }

    /// <summary>
    /// Xóa môn — chặn nếu đang có TeacherAssignment (sau này thêm TimetableSlot).
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy môn học.");

        var inUse = await _context.TeacherAssignments.AnyAsync(ta => ta.SubjectId == id)
            || await _context.TimetableSlots.AnyAsync(t => t.SubjectId == id);
        if (inUse)
            return (false, "Không thể xóa: môn đang được phân công hoặc có trong TKB.");

        await _repository.DeleteAsync(entity);
        return (true, "Đã xóa môn học.");
    }

    private static string? Validate(CreateUpdateSubjectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Tên môn không được để trống.";

        if (string.IsNullOrWhiteSpace(dto.Code))
            return "Mã môn không được để trống.";

        return null;
    }

    /// <summary>Chuẩn hoá mã môn: trim + viết hoa (MATH, ENG...).</summary>
    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static SubjectDto MapToDto(Subject s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code
    };
}
