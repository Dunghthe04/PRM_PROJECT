using Api.Common;
using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ quản lý user cấp Admin (FR5.1 — Ngày 5 Bước 4).
/// Service = tầng nghiệp vụ: validate → gọi Repository → map DTO.
/// Không truy cập HttpContext trực tiếp (Controller truyền actorId khi cần).
/// </summary>
public interface IAdminUserService
{
    /// <summary>GET /api/users — danh sách phân trang.</summary>
    Task<PagedResultDto<UserDto>> GetPagedAsync(UserListQueryDto query);

    /// <summary>GET /api/users/{id} — chi tiết 1 user.</summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>POST /api/users — Admin tạo tài khoản.</summary>
    Task<(UserDto? Result, string? Error)> CreateAsync(AdminCreateUserDto dto);

    /// <summary>PUT /api/users/{id} — Admin sửa họ tên, email, role.</summary>
    Task<(UserDto? Result, string? Error)> UpdateAsync(int id, AdminUpdateUserDto dto);

    /// <summary>DELETE /api/users/{id} — xóa user (không cho xóa chính mình).</summary>
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorAdminId);

    /// <summary>PUT /api/users/{id}/lock — khóa tài khoản.</summary>
    Task<(bool Success, string Message)> LockAsync(int id, int actorAdminId);

    /// <summary>PUT /api/users/{id}/unlock — mở khóa tài khoản.</summary>
    Task<(bool Success, string Message)> UnlockAsync(int id);

    /// <summary>POST /api/users/{id}/reset-password — Admin đặt mật khẩu mới.</summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(int id, AdminResetPasswordDto dto);

    /// <summary>GET /api/users/import/template — tải file Excel mẫu.</summary>
    byte[] GenerateImportTemplate();

    /// <summary>POST /api/users/import — import hàng loạt từ Excel.</summary>
    Task<(ImportUsersResultDto? Result, string? Error)> ImportFromExcelAsync(Stream excelStream);
}

/// <summary>Implement IAdminUserService.</summary>
public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _userRepository;

    public AdminUserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<UserDto>> GetPagedAsync(UserListQueryDto query)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(query);

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<UserDto>
        {
            Items = items.Select(UserService.MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return user == null ? null : UserService.MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<(UserDto? Result, string? Error)> CreateAsync(AdminCreateUserDto dto)
    {
        var validationError = ValidateCreate(dto);
        if (validationError != null)
            return (null, validationError);

        var phone = UserRepository.NormalizePhone(dto.Phone);
        if (await _userRepository.PhoneExistsAsync(phone))
            return (null, "Số điện thoại đã được đăng ký.");

        var user = new User
        {
            Username = phone,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Role = dto.Role,
            IsPhoneVerified = dto.IsPhoneVerified,
            IsLocked = false
        };

        var created = await _userRepository.CreateUserAsync(user);
        return (UserService.MapToDto(created), null);
    }

    /// <inheritdoc />
    public async Task<(UserDto? Result, string? Error)> UpdateAsync(int id, AdminUpdateUserDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return (null, "Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (null, "Họ tên không được để trống.");

        user.FullName = dto.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        user.Role = dto.Role;

        await _userRepository.UpdateUserAsync(user);
        return (UserService.MapToDto(user), null);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorAdminId)
    {
        if (id == actorAdminId)
            return (false, "Không thể xóa tài khoản Admin đang đăng nhập.");

        var deleted = await _userRepository.DeleteUserAsync(id);
        return deleted
            ? (true, "Đã xóa người dùng.")
            : (false, "Không tìm thấy người dùng.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> LockAsync(int id, int actorAdminId)
    {
        if (id == actorAdminId)
            return (false, "Không thể khóa tài khoản Admin đang đăng nhập.");

        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        if (user.IsLocked)
            return (false, "Tài khoản đã bị khóa trước đó.");

        user.IsLocked = true;
        await _userRepository.UpdateUserAsync(user);
        return (true, "Đã khóa tài khoản.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> UnlockAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        if (!user.IsLocked)
            return (false, "Tài khoản không ở trạng thái khóa.");

        user.IsLocked = false;
        await _userRepository.UpdateUserAsync(user);
        return (true, "Đã mở khóa tài khoản.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> ResetPasswordAsync(int id, AdminResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateUserAsync(user);
        return (true, "Đã đặt lại mật khẩu.");
    }

    /// <inheritdoc />
    public byte[] GenerateImportTemplate() => UserExcelHelper.GenerateTemplate();

    /// <inheritdoc />
    public async Task<(ImportUsersResultDto? Result, string? Error)> ImportFromExcelAsync(Stream excelStream)
    {
        List<(int RowNumber, ImportUserRowDto? Row, string? Error)> parsed;
        try
        {
            parsed = UserExcelHelper.ParseImportStream(excelStream);
        }
        catch (Exception)
        {
            return (null, "Không đọc được file Excel. Kiểm tra định dạng .xlsx và sheet 'Users'.");
        }

        // Lỗi cấp file (trống, không parse được)
        if (parsed.Count == 1 && parsed[0].Row == null && parsed[0].RowNumber == 0)
            return (null, parsed[0].Error);

        var result = new ImportUsersResultDto();
        var phonesInFile = new HashSet<string>();

        foreach (var (rowNum, row, parseError) in parsed)
        {
            result.TotalRows++;

            if (parseError != null || row == null)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportUserErrorDto
                {
                    RowNumber = rowNum,
                    Phone = row?.Phone ?? "",
                    Reason = parseError ?? "Dòng không hợp lệ."
                });
                continue;
            }

            var phone = UserRepository.NormalizePhone(row.Phone);

            // Trùng SĐT trong cùng file Excel
            if (!phonesInFile.Add(phone))
            {
                result.FailedCount++;
                result.Errors.Add(new ImportUserErrorDto
                {
                    RowNumber = rowNum,
                    Phone = row.Phone,
                    Reason = "Số điện thoại trùng trong file."
                });
                continue;
            }

            // Tái dùng CreateAsync — validate + hash BCrypt + lưu DB
            var createDto = new AdminCreateUserDto
            {
                Phone = row.Phone,
                Password = row.Password,
                FullName = row.FullName,
                Email = row.Email,
                Role = row.Role,
                IsPhoneVerified = true // Admin import → coi như đã xác thực
            };

            var (_, createError) = await CreateAsync(createDto);
            if (createError != null)
            {
                result.FailedCount++;
                result.Errors.Add(new ImportUserErrorDto
                {
                    RowNumber = rowNum,
                    Phone = row.Phone,
                    Reason = createError
                });
                continue;
            }

            result.SuccessCount++;
        }

        if (result.TotalRows == 0)
            return (null, "File Excel không có dòng dữ liệu hợp lệ.");

        return (result, null);
    }

    /// <summary>Validate dữ liệu tạo user — tái dùng quy tắc giống đăng ký công khai.</summary>
    private static string? ValidateCreate(AdminCreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return "Số điện thoại là bắt buộc.";

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return "Mật khẩu phải có ít nhất 6 ký tự.";

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return "Họ tên không được để trống.";

        var phone = UserRepository.NormalizePhone(dto.Phone);
        if (phone.Length < 9 || phone.Length > 15)
            return "Số điện thoại không hợp lệ.";

        return null;
    }
}
