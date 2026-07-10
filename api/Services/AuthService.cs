using System.Security.Cryptography;
using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Nghiệp vụ OTP về SĐT: xác thực đăng ký + quên mật khẩu (3 bước).
/// </summary>
public interface IAuthService
{
    /// <summary>Gửi OTP mục đích Register sau khi tạo tài khoản.</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> SendRegisterOtpAsync(int userId, string phone);

    /// <summary>Xác thực OTP đăng ký → bật IsPhoneVerified.</summary>
    Task<(MessageResponseDto? Result, string? Error)> VerifyPhoneAsync(PhoneOtpDto dto);

    /// <summary>Gửi OTP quên mật khẩu về SĐT.</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> ForgotPasswordAsync(ForgotPasswordDto dto);

    /// <summary>Xác thực OTP quên MK → cấp resetToken.</summary>
    Task<(VerifyOtpResponseDto? Result, string? Error)> VerifyResetOtpAsync(PhoneOtpDto dto);

    /// <summary>Đặt mật khẩu mới bằng resetToken (one-time).</summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);

    /// <summary>Gửi lại OTP (Register hoặc ResetPassword).</summary>
    Task<(ForgotPasswordResponseDto? Result, string? Error)> ResendOtpAsync(ResendOtpDto dto);
}

/// <summary>Implement IAuthService — OTP 6 số, TTL 5 phút, chỉ gửi về Phone.</summary>
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
        return await IssuePhoneOtpAsync(userId, phone, OtpPurpose.Register,
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

    /// <summary>Bước 1 quên MK — chỉ gửi OTP nếu tài khoản đã verify phone.</summary>
    public async Task<(ForgotPasswordResponseDto? Result, string? Error)> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return (null, "Vui lòng nhập số điện thoại.");

        var user = await _userRepository.GetUserByPhoneAsync(dto.Phone);
        if (user == null)
            return (null, "Không tìm thấy tài khoản với số điện thoại này.");

        if (!user.IsPhoneVerified)
            return (null, "Tài khoản chưa xác thực SĐT. Vui lòng hoàn tất đăng ký trước.");

        return await IssuePhoneOtpAsync(user.Id, user.Phone, OtpPurpose.ResetPassword,
            "Đã gửi OTP quên mật khẩu về SĐT (Dev: xem log server).");
    }

    /// <summary>Bước 2 quên MK — OTP đúng thì cấp resetToken ngắn hạn.</summary>
    public async Task<(VerifyOtpResponseDto? Result, string? Error)> VerifyResetOtpAsync(PhoneOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.OtpCode))
            return (null, "Số điện thoại và mã OTP là bắt buộc.");

        var user = await _userRepository.GetUserByPhoneAsync(dto.Phone);
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
        if (string.IsNullOrWhiteSpace(dto.Phone))
            return (null, "Vui lòng nhập số điện thoại.");

        var purpose = dto.Purpose == OtpPurpose.ResetPassword
            ? OtpPurpose.ResetPassword
            : OtpPurpose.Register;

        var user = await _userRepository.GetUserByPhoneAsync(dto.Phone);
        if (user == null)
            return (null, "Không tìm thấy tài khoản.");

        if (purpose == OtpPurpose.Register && user.IsPhoneVerified)
            return (null, "Số điện thoại đã được xác thực.");

        if (purpose == OtpPurpose.ResetPassword && !user.IsPhoneVerified)
            return (null, "Tài khoản chưa xác thực SĐT.");

        var message = purpose == OtpPurpose.Register
            ? "Đã gửi lại OTP xác thực đăng ký về SĐT (Dev: xem log server)."
            : "Đã gửi lại OTP quên mật khẩu về SĐT (Dev: xem log server).";

        return await IssuePhoneOtpAsync(user.Id, user.Phone, purpose, message);
    }

    /// <summary>
    /// Tạo + lưu OTP, gửi qua IOtpDeliveryService.
    /// Luôn vô hiệu hóa OTP active cùng purpose trước khi phát hành mã mới.
    /// </summary>
    private async Task<(ForgotPasswordResponseDto? Result, string? Error)> IssuePhoneOtpAsync(
        int userId, string phone, string purpose, string message)
    {
        await _otpRepository.InvalidateActiveOtpsAsync(userId, purpose);

        var code = GenerateOtpCode();
        var otp = new PasswordResetOtp
        {
            UserId = userId,
            Code = code,
            Channel = OtpChannel.Phone,
            Destination = phone,
            Purpose = purpose,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes)
        };

        await _otpRepository.CreateAsync(otp);
        await _otpDelivery.SendAsync(OtpChannel.Phone, phone, code);

        return (new ForgotPasswordResponseDto
        {
            Message = message,
            Channel = OtpChannel.Phone,
            MaskedDestination = MaskPhone(phone),
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
}
