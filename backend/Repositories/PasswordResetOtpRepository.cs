using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>Truy cập bảng PasswordResetOtps (OTP đăng ký / quên MK).</summary>
public interface IPasswordResetOtpRepository
{
    /// <summary>Lưu bản ghi OTP mới.</summary>
    Task<PasswordResetOtp> CreateAsync(PasswordResetOtp otp);

    /// <summary>Vô hiệu hóa OTP active cùng purpose của user.</summary>
    Task InvalidateActiveOtpsAsync(int userId, string purpose);

    /// <summary>Tìm OTP còn hiệu lực theo user + mã + purpose.</summary>
    Task<PasswordResetOtp?> GetActiveByUserAndCodeAsync(int userId, string code, string purpose);

    /// <summary>Tìm OTP quên MK đã verify theo resetToken (kèm User).</summary>
    Task<PasswordResetOtp?> GetByResetTokenAsync(string resetToken);

    /// <summary>Cập nhật trạng thái OTP (verified / used / token).</summary>
    Task UpdateAsync(PasswordResetOtp otp);
}

/// <summary>Implement IPasswordResetOtpRepository bằng EF Core.</summary>
public class PasswordResetOtpRepository : IPasswordResetOtpRepository
{
    private readonly AppDbContext _context;

    public PasswordResetOtpRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PasswordResetOtp> CreateAsync(PasswordResetOtp otp)
    {
        _context.PasswordResetOtps.Add(otp);
        await _context.SaveChangesAsync();
        return otp;
    }

    /// <summary>Đánh dấu IsUsed=true cho mọi OTP active cùng purpose — chỉ 1 mã hiệu lực.</summary>
    public async Task InvalidateActiveOtpsAsync(int userId, string purpose)
    {
        var actives = await _context.PasswordResetOtps
            .Where(o =>
                o.UserId == userId &&
                o.Purpose == purpose &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var otp in actives)
            otp.IsUsed = true;

        if (actives.Count > 0)
            await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PasswordResetOtp?> GetActiveByUserAndCodeAsync(int userId, string code, string purpose)
    {
        return await _context.PasswordResetOtps
            .Where(o =>
                o.UserId == userId &&
                o.Code == code &&
                o.Purpose == purpose &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<PasswordResetOtp?> GetByResetTokenAsync(string resetToken)
    {
        return await _context.PasswordResetOtps
            .Include(o => o.User)
            .FirstOrDefaultAsync(o =>
                o.ResetToken == resetToken &&
                o.Purpose == OtpPurpose.ResetPassword &&
                o.IsVerified &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PasswordResetOtp otp)
    {
        _context.PasswordResetOtps.Update(otp);
        await _context.SaveChangesAsync();
    }
}
