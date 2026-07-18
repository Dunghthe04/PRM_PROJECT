import 'package:dio/dio.dart';
import '../model/user_model.dart';
import '../service/api_client.dart';
import '../service/token_storage.dart';

/// Controller xử lý nghiệp vụ xác thực (đăng nhập / đăng xuất / quên MK).
class AuthController {
  // Tạo 1 ApiClient để gọi API (đã cấu hình baseUrl + interceptor).
  final ApiClient _apiClient = ApiClient();

  /// Đăng nhập bằng SĐT + mật khẩu.
  /// Trả về record (user, error):
  ///  - Thành công: (UserModel, null)
  ///  - Thất bại:   (null, "thông báo lỗi")
  Future<(UserModel?, String?)> login(String phone, String password) async {
    try {
      // Gọi POST /User/login (baseUrl đã có sẵn '.../api')
      final response = await _apiClient.dio.post(
        '/User/login',
        data: {
          'phone': phone,
          'password': password,
        },
      );

      // statusCode 200 login thành công
      if (response.statusCode == 200) {
        final data = response.data; // Map JSON {token, user}

        // lưu token vào kho an toàn để các request khác dùng
        await TokenStorage.saveToken(data['token'] as String);

        // chuyển dữ liệu user JSON thành user model
        final user = UserModel.fromJson(data['user'] as Map<String, dynamic>);
        return (user, null);
      }
      // Các mã khác (401/400...): body có thể là Map {message} HOẶC chuỗi thô
      return (null, _extractMessage(response.data) ??
          'Đăng nhập thất bại (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _dioError(e, 'Lỗi kết nối tới máy chủ'));
    }
  }

  /// Bước 1 quên MK — gửi OTP về email (FR1.2).
  /// Trả (message, error, maskedEmail).
  Future<(String?, String?, String?)> forgotPassword(String email) async {
    try {
      final res = await _apiClient.dio.post(
        '/auth/forgot-password',
        data: {'email': email},
      );
      if (res.statusCode == 200) {
        final data = res.data as Map<String, dynamic>;
        return (
          data['message'] as String?,
          null,
          data['maskedDestination'] as String?,
        );
      }
      return (null, _extractMessage(res.data) ?? 'Gửi OTP thất bại.', null);
    } on DioException catch (e) {
      return (null, _dioError(e, 'Gửi OTP thất bại'), null);
    }
  }

  /// Bước 2 quên MK — xác thực OTP nhận qua email → resetToken.
  Future<(String?, String?)> verifyResetOtp(String email, String otpCode) async {
    try {
      final res = await _apiClient.dio.post(
        '/auth/verify-otp',
        data: {'email': email, 'otpCode': otpCode},
      );
      if (res.statusCode == 200) {
        final data = res.data as Map<String, dynamic>;
        return (data['resetToken'] as String?, null);
      }
      return (null, _extractMessage(res.data) ?? 'Xác thực OTP thất bại.');
    } on DioException catch (e) {
      return (null, _dioError(e, 'Xác thực OTP thất bại'));
    }
  }

  /// Bước 3 quên MK — đặt mật khẩu mới bằng resetToken.
  Future<(String?, String?)> resetPassword(
      String resetToken, String newPassword) async {
    try {
      final res = await _apiClient.dio.post(
        '/auth/reset-password',
        data: {'resetToken': resetToken, 'newPassword': newPassword},
      );
      if (res.statusCode == 200) {
        final data = res.data;
        if (data is Map) return (data['message'] as String?, null);
        return ('Đặt lại mật khẩu thành công.', null);
      }
      return (null, _extractMessage(res.data) ?? 'Đặt lại mật khẩu thất bại.');
    } on DioException catch (e) {
      return (null, _dioError(e, 'Đặt lại mật khẩu thất bại'));
    }
  }

  /// Gửi lại OTP quên MK về email.
  Future<(String?, String?, String?)> resendResetOtp(String email) async {
    try {
      final res = await _apiClient.dio.post(
        '/auth/resend-otp',
        data: {'email': email, 'purpose': 'ResetPassword'},
      );
      if (res.statusCode == 200) {
        final data = res.data as Map<String, dynamic>;
        return (
          data['message'] as String?,
          null,
          data['maskedDestination'] as String?,
        );
      }
      return (null, _extractMessage(res.data) ?? 'Gửi lại OTP thất bại.', null);
    } on DioException catch (e) {
      return (null, _dioError(e, 'Gửi lại OTP thất bại'), null);
    }
  }

  /// Đăng xuất → xóa token
  Future<void> logout() async {
    await TokenStorage.clearToken();
  }

  String? _extractMessage(dynamic data) {
    if (data is Map) return data['message'] as String?;
    if (data is String) return data;
    return null;
  }

  String _dioError(DioException e, String fallback) {
    final msg = _extractMessage(e.response?.data);
    if (msg != null) return msg;
    return '$fallback: ${e.message}';
  }
}
