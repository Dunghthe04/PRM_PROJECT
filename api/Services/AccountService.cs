using Api.DTOs;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ hồ sơ (FR1.3): xem/sửa thông tin, avatar, đổi mật khẩu.
/// Phone là định danh login → không cho tự đổi qua API hồ sơ.
/// </summary>
public interface IAccountService
{
    /// <summary>Lấy hồ sơ theo userId.</summary>
    Task<UserDto?> GetProfileAsync(int userId);

    /// <summary>Cập nhật FullName / Email.</summary>
    Task<(UserDto? Profile, string? Error)> UpdateProfileAsync(int userId, UpdateProfileDto dto);

    /// <summary>Cập nhật đường dẫn / URL avatar.</summary>
    Task<(UserDto? Profile, string? Error)> UpdateAvatarAsync(int userId, string avatarUrl);

    /// <summary>Đổi mật khẩu khi còn nhớ MK cũ.</summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}

/// <summary>Implement IAccountService.</summary>
public class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;

    public AccountService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        return user == null ? null : UserService.MapToDto(user);
    }

    /// <summary>Sửa họ tên + email liên hệ (không đụng tới Phone).</summary>
    public async Task<(UserDto? Profile, string? Error)> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return (null, "Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (null, "Họ tên không được để trống.");

        user.FullName = dto.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        await _userRepository.UpdateUserAsync(user);
        return (UserService.MapToDto(user), null);
    }

    /// <summary>Ghi AvatarUrl mới vào hồ sơ.</summary>
    public async Task<(UserDto? Profile, string? Error)> UpdateAvatarAsync(int userId, string avatarUrl)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return (null, "Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(avatarUrl))
            return (null, "AvatarUrl không được để trống.");

        user.AvatarUrl = avatarUrl.Trim();
        await _userRepository.UpdateUserAsync(user);
        return (UserService.MapToDto(user), null);
    }

    /// <summary>Verify MK cũ bằng BCrypt rồi hash + lưu MK mới.</summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return (false, "Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        // Bắt buộc đúng MK cũ — chống chiếm session đổi MK
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return (false, "Mật khẩu hiện tại không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateUserAsync(user);
        return (true, "Đổi mật khẩu thành công.");
    }
}
