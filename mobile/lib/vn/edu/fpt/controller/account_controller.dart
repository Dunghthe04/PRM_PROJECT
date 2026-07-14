import 'package:dio/dio.dart';
import '../model/user_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ hồ sơ cá nhân (FR1.3).
/// Quy ước trả về: dùng record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class AccountController {
  // Client HTTP dùng chung (interceptor tự gắn JWT vào mỗi request).
  final ApiClient _apiClient = ApiClient();

  /// Lấy hồ sơ user đang đăng nhập (GET /account/me).
  ///
  /// Nhận: không tham số (userId server tự đọc từ token).
  /// Trả về (UserModel?, String?):
  ///   - (user, null)  nếu thành công.
  ///   - (null, lỗi)   nếu token hết hạn / lỗi mạng.
  Future<(UserModel?, String?)> getProfile() async {
    try {
      final response = await _apiClient.dio.get('/account/me');
      if (response.statusCode == 200) {
        final user = UserModel.fromJson(response.data as Map<String, dynamic>);
        return (user, null);
      }
      // 401 = token sai/hết hạn
      return (null, 'Phiên đăng nhập hết hạn (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }

  /// Cập nhật họ tên + email (PUT /account/me).
  ///
  /// Nhận:
  ///   - [fullName]: họ tên mới (bắt buộc).
  ///   - [email]: email mới (có thể null nếu bỏ trống).
  /// Trả về (UserModel?, String?):
  ///   - (user mới, null) nếu thành công (dùng để cập nhật UI).
  ///   - (null, lỗi)      nếu thất bại (message lấy từ backend).
  Future<(UserModel?, String?)> updateProfile({
    required String fullName,
    String? email,
  }) async {
    try {
      final response = await _apiClient.dio.put('/account/me', data: {
        'fullName': fullName,
        'email': email,
      });
      if (response.statusCode == 200) {
        final user = UserModel.fromJson(response.data as Map<String, dynamic>);
        return (user, null);
      }
      return (null, 'Cập nhật thất bại (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }
  /// Đổi mật khẩu (PUT /account/change-password).
  ///
  /// Nhận:
  ///   - [currentPassword]: mật khẩu hiện tại (server verify BCrypt).
  ///   - [newPassword]: mật khẩu mới.
  /// Trả về (bool, String):
  ///   - (true, thông báo thành công).
  ///   - (false, thông báo lỗi từ backend, vd "Mật khẩu cũ không đúng").
  Future<(bool, String)> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    try {
      final response = await _apiClient.dio.put('/account/change-password', data: {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      });
      if (response.statusCode == 200) {
        return (true, 'Đổi mật khẩu thành công.');
      }
      return (false, 'Đổi mật khẩu thất bại (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (false, _extractError(e));
    }
  }
  /// Bóc thông báo lỗi từ response API để hiển thị cho user.
  ///
  /// Nhận: [e] — ngoại lệ Dio (chứa response lỗi nếu có).
  /// Trả về (String): câu lỗi dễ đọc. Ưu tiên field "message" backend gửi,
  /// nếu không có thì dùng nội dung thô hoặc mô tả lỗi mạng.
  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] != null) {
      return data['message'].toString();
    }
    if (data is String && data.isNotEmpty) return data;
    return 'Lỗi kết nối: ${e.message}';
  }
}