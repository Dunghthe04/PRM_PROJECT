using System.Security.Cryptography;
using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ OTP: xác thực đăng ký (SĐT) + quên mật khẩu (Email) — 3 bước.
/// </summary>
public interface IAuthService
{
    /// <summary>Gửi OTP mục đích Register sau khi tạo tài khoản (SĐT).</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> SendRegisterOtpAsync(int userId, string phone);

    /// <summary>Xác thực OTP đăng ký → bật IsPhoneVerified.</summary>
    Task<(MessageResponseDto? Result, string? Error)> VerifyPhoneAsync(PhoneOtpDto dto);

    /// <summary>Gửi OTP quên mật khẩu về Email.</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> ForgotPasswordAsync(ForgotPasswordDto dto);

    /// <summary>Xác thực OTP quên MK (email) → cấp resetToken.</summary>
    Task<(VerifyOtpResponseDto? Result, string? Error)> VerifyResetOtpAsync(EmailOtpDto dto);

    /// <summary>Đặt mật khẩu mới bằng resetToken (one-time).</summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);

    /// <summary>Gửi lại OTP (Register = Phone, ResetPassword = Email).</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> ResendOtpAsync(ResendOtpDto dto);
}

/// <summary>Implement IAuthService — OTP 6 số, TTL 5 phút.</summary>
public class AuthService : IAuthService
{
    /// <summary>OTP sống 5 phút — đủ thời gian nhập, hạn chế brute-force.</summary>
    private const int OtpExpiryMinutes = 5;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetOtpRepository _otpRepository;
    private readonly IOtpDeliveryService _otpDelivery;

    public AuthService(
        IUserRepository userRepository,
        IPasswordResetOtpRepository otpRepository,
        IOtpDeliveryService otpDelivery)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _otpDelivery = otpDelivery;
    }

    /// <inheritdoc />
    public async Task<(ForgotPasswordResponseDto? Result, string? Error)> SendRegisterOtpAsync(int userId, string phone)
    {
        return await IssueOtpAsync(
            userId, OtpChannel.Phone, phone, OtpPurpose.Register,
            "Đã gửi OTP xác thực đăng ký về SĐT (Dev: xem log server).");
    }

    /// <summary>Verify OTP Register → đánh dấu SĐT đã xác thực, cho phép login.</summary>
    public async Task<(MessageResponseDto? Result, string? Error)> VerifyPhoneAsync(PhoneOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.OtpCode))
            return (null, "Số điện thoại và mã OTP là bắt buộc.");

        var user = await _userRepository.GetUserByPhoneAsync(dto.Phone);
        if (user == null)
            return (null, "Không tìm thấy tài khoản.");

        if (user.IsPhoneVerified)
            return (new MessageResponseDto { Message = "Số điện thoại đã được xác thực trước đó." }, null);

        var otp = await _otpRepository.GetActiveByUserAndCodeAsync(
            user.Id, dto.OtpCode.Trim(), OtpPurpose.Register);

        if (otp == null)
            return (null, "Mã OTP không hợp lệ hoặc đã hết hạn.");

        user.IsPhoneVerified = true;
        otp.IsUsed = true;
        otp.IsVerified = true;

        await _userRepository.UpdateUserAsync(user);
        await _otpRepository.UpdateAsync(otp);

        return (new MessageResponseDto { Message = "Xác thực SĐT thành công. Bạn có thể đăng nhập." }, null);
    }

    /// <summary>Bước 1 quên MK — gửi OTP về email đã gắn hồ sơ.</summary>
    public async Task<(ForgotPasswordResponseDto? Result, string? Error)> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (null, "Vui lòng nhập email.");

        var user = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (user == null)
            return (null, "Không tìm thấy tài khoản với email này.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return (null, "Tài khoản chưa cập nhật email. Vui lòng liên hệ nhà trường.");

        if (!user.IsPhoneVerified)
            return (null, "Tài khoản chưa xác thực SĐT. Vui lòng hoàn tất đăng ký trước.");

        if (user.IsLocked)
            return (null, "Tài khoản đang bị khóa. Vui lòng liên hệ nhà trường.");

        return await IssueOtpAsync(
            user.Id, OtpChannel.Email, user.Email!, OtpPurpose.ResetPassword,
            "Đã gửi mã OTP về email của bạn. Vui lòng kiểm tra hộp thư (kể cả Spam).");
    }

    /// <summary>Bước 2 quên MK — OTP đúng thì cấp resetToken ngắn hạn.</summary>
    public async Task<(VerifyOtpResponseDto? Result, string? Error)> VerifyResetOtpAsync(EmailOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.OtpCode))
            return (null, "Email và mã OTP là bắt buộc.");

        var user = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (user == null)
            return (null, "Không tìm thấy tài khoản.");

        var otp = await _otpRepository.GetActiveByUserAndCodeAsync(
            user.Id, dto.OtpCode.Trim(), OtpPurpose.ResetPassword);

        if (otp == null)
            return (null, "Mã OTP không hợp lệ hoặc đã hết hạn.");

        otp.IsVerified = true;
        otp.ResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        otp.ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        await _otpRepository.UpdateAsync(otp);

        return (new VerifyOtpResponseDto
        {
            Message = "Xác thực OTP thành công.",
            ResetToken = otp.ResetToken
        }, null);
    }

    /// <summary>Bước 3 quên MK — hash MK mới, hủy resetToken (one-time).</summary>
    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ResetToken))
            return (false, "ResetToken là bắt buộc.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

        var otp = await _otpRepository.GetByResetTokenAsync(dto.ResetToken.Trim());
        if (otp == null)
            return (false, "ResetToken không hợp lệ hoặc đã hết hạn.");

        otp.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        otp.IsUsed = true;
        otp.ResetToken = null;
        await _otpRepository.UpdateAsync(otp);
        await _userRepository.UpdateUserAsync(otp.User);

        return (true, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
    }

    /// <summary>Gửi lại OTP theo purpose; hủy mã active cũ cùng mục đích.</summary>
    public async Task<(ForgotPasswordResponseDto? Result, string? Error)> ResendOtpAsync(ResendOtpDto dto)
    {
        var purpose = dto.Purpose == OtpPurpose.ResetPassword
            ? OtpPurpose.ResetPassword
            : OtpPurpose.Register;

        if (purpose == OtpPurpose.Register)
        {
            if (string.IsNullOrWhiteSpace(dto.Phone))
                return (null, "Vui lòng nhập số điện thoại.");

            var user = await _userRepository.GetUserByPhoneAsync(dto.Phone);
            if (user == null)
                return (null, "Không tìm thấy tài khoản.");

            if (user.IsPhoneVerified)
                return (null, "Số điện thoại đã được xác thực.");

            return await IssueOtpAsync(
                user.Id, OtpChannel.Phone, user.Phone, purpose,
                "Đã gửi lại OTP xác thực đăng ký về SĐT (Dev: xem log server).");
        }

        // ResetPassword → gửi lại qua Email
        if (string.IsNullOrWhiteSpace(dto.Email))
            return (null, "Vui lòng nhập email.");

        var resetUser = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (resetUser == null)
            return (null, "Không tìm thấy tài khoản.");

        if (string.IsNullOrWhiteSpace(resetUser.Email))
            return (null, "Tài khoản chưa cập nhật email.");

        if (!resetUser.IsPhoneVerified)
            return (null, "Tài khoản chưa xác thực SĐT.");

        return await IssueOtpAsync(
            resetUser.Id, OtpChannel.Email, resetUser.Email!, purpose,
            "Đã gửi lại mã OTP về email của bạn. Vui lòng kiểm tra hộp thư (kể cả Spam).");
    }

    /// <summary>
    /// Tạo + lưu OTP, gửi qua IOtpDeliveryService.
    /// Luôn vô hiệu hóa OTP active cùng purpose trước khi phát hành mã mới.
    /// </summary>
    private async Task<(ForgotPasswordResponseDto? Result, string? Error)> IssueOtpAsync(
        int userId, string channel, string destination, string purpose, string message)
    {
        await _otpRepository.InvalidateActiveOtpsAsync(userId, purpose);

        var code = GenerateOtpCode();
        var otp = new PasswordResetOtp
        {
            UserId = userId,
            Code = code,
            Channel = channel,
            Destination = destination,
            Purpose = purpose,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes)
        };

        await _otpRepository.CreateAsync(otp);
        try
        {
            await _otpDelivery.SendAsync(channel, destination, code);
        }
        catch (InvalidOperationException ex)
        {
            // Vô hiệu OTP vừa tạo nếu gửi thất bại — tránh dùng mã không tới tay user.
            otp.IsUsed = true;
            await _otpRepository.UpdateAsync(otp);
            return (null, ex.Message);
        }

        return (new ForgotPasswordResponseDto
        {
            Message = message,
            Channel = channel,
            MaskedDestination = channel == OtpChannel.Email
                ? MaskEmail(destination)
                : MaskPhone(destination),
            ExpiresInSeconds = OtpExpiryMinutes * 60
        }, null);
    }

    /// <summary>Sinh OTP 6 số cryptographically secure (không dùng Random thường).</summary>
    private static string GenerateOtpCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    /// <summary>Che SĐT khi trả response (chỉ hiện 3 số cuối).</summary>
    internal static string MaskPhone(string phone)
    {
        if (phone.Length <= 3) return "***";
        return new string('*', phone.Length - 3) + phone[^3..];
    }

    /// <summary>Che email — giữ 2 ký tự đầu + domain (vd: ab***@gmail.com).</summary>
    internal static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[at..];
        if (local.Length <= 2) return "**" + domain;
        return local[..2] + new string('*', Math.Min(local.Length - 2, 5)) + domain;
    }
}
