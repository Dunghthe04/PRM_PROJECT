using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập dữ liệu bảng Users.</summary>
public interface IUserRepository
{
    /// <summary>Tìm user theo khóa chính.</summary>
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>Tìm user theo số điện thoại (đã chuẩn hoá).</summary>
    Task<User?> GetUserByPhoneAsync(string phone);

    /// <summary>Kiểm tra SĐT đã tồn tại chưa (phục vụ đăng ký).</summary>
    Task<bool> PhoneExistsAsync(string phone);

    /// <summary>Thêm user mới vào DB.</summary>
    Task<User> CreateUserAsync(User user);

    /// <summary>Cập nhật user đã track / attach.</summary>
    Task UpdateUserAsync(User user);
}

/// <summary>Implement IUserRepository bằng EF Core.</summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByPhoneAsync(string phone)
    {
        var key = NormalizePhone(phone);
        return await _context.Users.FirstOrDefaultAsync(u => u.Phone == key);
    }

    /// <inheritdoc />
    public async Task<bool> PhoneExistsAsync(string phone)
    {
        var key = NormalizePhone(phone);
        return await _context.Users.AnyAsync(u => u.Phone == key);
    }

    /// <inheritdoc />
    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>Chuẩn hoá SĐT: chỉ giữ chữ số (bỏ khoảng trắng, dấu +, ...).</summary>
    public static string NormalizePhone(string phone)
        => new string(phone.Where(char.IsDigit).ToArray());
}
