using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>Nghiệp vụ kỳ học (FR5.2) — nền tảng cho Class / TKB.</summary>
public interface ISemesterService
{
    /// <summary>Danh sách tất cả kỳ học.</summary>
    Task<List<SemesterDto>> GetAllAsync();

    /// <summary>Chi tiết 1 kỳ; null nếu không có.</summary>
    Task<SemesterDto?> GetByIdAsync(int id);

    /// <summary>Tạo kỳ học mới (validate ngày bắt đầu &lt; kết thúc).</summary>
    Task<(SemesterDto? Result, string? Error)> CreateAsync(CreateUpdateSemesterDto dto);

    /// <summary>Cập nhật kỳ học.</summary>
    Task<(SemesterDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateSemesterDto dto);

    /// <summary>Xóa kỳ học (lỗi nếu đang có lớp gắn vào).</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

/// <summary>Implement ISemesterService.</summary>
public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _repository;
    private readonly AppDbContext _context;

    public SemesterService(ISemesterRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<SemesterDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<SemesterDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    /// <summary>Tạo kỳ — Name bắt buộc, StartDate phải trước EndDate.</summary>
    public async Task<(SemesterDto? Result, string? Error)> CreateAsync(CreateUpdateSemesterDto dto)
    {
        var error = Validate(dto);
        if (error != null) return (null, error);

        var entity = new Semester
        {
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };

        var created = await _repository.CreateAsync(entity);
        return (MapToDto(created), null);
    }

    /// <summary>Sửa kỳ — giữ Id, cập nhật tên + khoảng thời gian.</summary>
    public async Task<(SemesterDto? Result, string? Error)> UpdateAsync(int id, CreateUpdateSemesterDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return (null, "Không tìm thấy kỳ học.");

        var error = Validate(dto);
        if (error != null) return (null, error);

        entity.Name = dto.Name.Trim();
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;

        await _repository.UpdateAsync(entity);
        return (MapToDto(entity), null);
    }

    /// <summary>
    /// Xóa kỳ — chặn nếu còn Class thuộc kỳ này (tránh orphan / lỗi FK).
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return (false, "Không tìm thấy kỳ học.");

        var hasClasses = await _context.Classes.AnyAsync(c => c.SemesterId == id);
        if (hasClasses)
            return (false, "Không thể xóa: kỳ học đang có lớp gắn vào.");

        await _repository.DeleteAsync(entity);
        return (true, "Đã xóa kỳ học.");
    }

    /// <summary>Validate tên + khoảng ngày hợp lệ.</summary>
    private static string? Validate(CreateUpdateSemesterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Tên kỳ học không được để trống.";

        if (dto.StartDate >= dto.EndDate)
            return "Ngày bắt đầu phải trước ngày kết thúc.";

        return null;
    }

    private static SemesterDto MapToDto(Semester s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        StartDate = s.StartDate,
        EndDate = s.EndDate
    };
}
