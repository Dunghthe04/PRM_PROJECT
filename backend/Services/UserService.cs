using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>Nghiệp vụ user: tạo tài khoản, đăng nhập, lấy hồ sơ theo id.</summary>
public interface IUserService
{
    /// <summary>Lấy DTO hồ sơ theo Id; null nếu không tồn tại.</summary>
    Task<UserDto?> GetUserByIdAsync(int id);

    /// <summary>Tạo tài khoản chưa verify phone — caller gửi OTP đăng ký sau đó.</summary>
    Task<(UserDto? User, string? Error)> CreateUserAsync(CreateUserDto createUserDto);

    /// <summary>Xác thực SĐT + mật khẩu. User=null nếu sai; Error nếu chưa verify phone.</summary>
    Task<(User? User, string? Error)> AuthenticateAsync(string phone, string password);
}

/// <summary>Implement IUserService — hash BCrypt, định danh bằng Phone.</summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    /// <summary>
    /// Đăng ký: validate SĐT/MK → hash mật khẩu → lưu user với IsPhoneVerified=false.
    /// </summary>
    public async Task<(UserDto? User, string? Error)> CreateUserAsync(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return (null, "Số điện thoại là bắt buộc.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return (null, "Mật khẩu phải có ít nhất 6 ký tự.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return (null, "Họ tên không được để trống.");

        var phone = UserRepository.NormalizePhone(dto.Phone);
        if (phone.Length < 9 || phone.Length > 15)
            return (null, "Số điện thoại không hợp lệ.");

        if (await _userRepository.PhoneExistsAsync(phone))
            return (null, "Số điện thoại đã được đăng ký.");

        var user = new User
        {
            // Username nội bộ = Phone (dùng cho JWT Name claim)
            Username = phone,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Role = dto.Role,
            IsPhoneVerified = false
        };

        var created = await _userRepository.CreateUserAsync(user);
        return (MapToDto(created), null);
    }

    /// <summary>
    /// Đăng nhập: tìm theo Phone → verify BCrypt → chặn nếu chưa xác thực OTP.
    /// </summary>
    public async Task<(User? User, string? Error)> AuthenticateAsync(string phone, string password)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return (null, "Vui lòng nhập số điện thoại.");

        var user = await _userRepository.GetUserByPhoneAsync(phone);
        if (user == null)
            return (null, null); // 401 chung — không lộ SĐT có tồn tại hay không

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, null);

        // FR5.1 — chặn đăng nhập nếu Admin đã khóa tài khoản
        if (user.IsLocked)
            return (null, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

        if (!user.IsPhoneVerified)
            return (null, "Số điện thoại chưa được xác thực. Vui lòng nhập OTP.");

        return (user, null);
    }

    /// <summary>Map entity User → UserDto (không lộ PasswordHash).</summary>
    internal static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Phone = user.Phone,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Email = user.Email,
        IsPhoneVerified = user.IsPhoneVerified,
        IsLocked = user.IsLocked,
        Role = user.Role.ToString()
    };
}
