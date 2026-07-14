using Api.DTOs;
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

    /// <summary>
    /// Kiểm tra SĐT đã thuộc user khác chưa (trừ user đang sửa).
    /// Dùng ở Bước 4 khi Admin đổi SĐT — hiện chưa cho đổi Phone nhưng giữ sẵn.
    /// </summary>
    Task<bool> PhoneExistsExceptAsync(string phone, int excludeUserId);

    /// <summary>Thêm user mới vào DB.</summary>
    Task<User> CreateUserAsync(User user);

    /// <summary>Cập nhật user đã track / attach.</summary>
    Task UpdateUserAsync(User user);

    /// <summary>
    /// Danh sách user có phân trang + lọc — phục vụ GET /api/users (Admin, FR5.1).
    /// Repository chỉ lo query DB; Service sẽ map sang DTO.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserListQueryDto query);

    /// <summary>Xóa user theo Id. Trả false nếu không tìm thấy.</summary>
    Task<bool> DeleteUserAsync(int id);

    /// <summary>
    /// Danh sách học sinh (con) liên kết với 1 phụ huynh qua StudentParent (FR2.1).
    /// Phục vụ Switch Profile trên app.
    /// </summary>
    Task<IReadOnlyList<User>> GetChildrenAsync(int parentId);
}

/// <summary>Implement IUserRepository bằng EF Core.</summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>Giới hạn pageSize tối đa — tránh query quá nặng (NFR4.2).</summary>
    private const int MaxPageSize = 100;

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
    public async Task<bool> PhoneExistsExceptAsync(string phone, int excludeUserId)
    {
        var key = NormalizePhone(phone);
        return await _context.Users.AnyAsync(u => u.Phone == key && u.Id != excludeUserId);
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

    /// <inheritdoc />
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserListQueryDto query)
    {
        // Chuẩn hoá tham số phân trang — Service/Controller có thể validate trước,
        // nhưng Repository cũng clamp để an toàn khi gọi từ nhiều nơi.
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        // Bắt đầu từ toàn bộ Users, áp dụng filter tuần tự (EF Core dịch sang SQL WHERE).
        IQueryable<User> q = _context.Users.AsNoTracking();

        // Lọc theo role nếu Admin chọn (vd: chỉ xem Giáo viên).
        if (query.Role.HasValue)
            q = q.Where(u => u.Role == query.Role.Value);

        // Lọc trạng thái khóa: true = bị khóa, false = đang hoạt động, null = tất cả.
        if (query.IsLocked.HasValue)
            q = q.Where(u => u.IsLocked == query.IsLocked.Value);

        // Tìm kiếm: khớp họ tên (contains) hoặc SĐT (contains chuỗi số đã chuẩn hoá).
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var phoneTerm = NormalizePhone(term);

            q = q.Where(u =>
                u.FullName.Contains(term) ||
                (!string.IsNullOrEmpty(phoneTerm) && u.Phone.Contains(phoneTerm)));
        }

        // Đếm tổng TRƯỚC khi Skip/Take — client cần TotalCount để tính số trang.
        var totalCount = await q.CountAsync();

        // Sắp xếp Id giảm dần = user mới tạo lên đầu (phù hợp màn Admin).
        var items = await q
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        // Hard delete — Bước 4 Service sẽ kiểm tra ràng buộc (vd: không xóa chính mình).
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetChildrenAsync(int parentId)
    {
        // Join StudentParents → lấy các User Student thuộc phụ huynh này.
        return await _context.StudentParents
            .Where(sp => sp.ParentId == parentId)
            .Select(sp => sp.Student)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Chuẩn hoá SĐT: chỉ giữ chữ số (bỏ khoảng trắng, dấu +, ...).</summary>
    public static string NormalizePhone(string phone)
        => new string(phone.Where(char.IsDigit).ToArray());
}
