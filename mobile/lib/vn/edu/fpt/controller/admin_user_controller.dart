import 'package:dio/dio.dart';
import '../model/user_model.dart';
import '../service/api_client.dart';

/// Kết quả phân trang từ API (PagedResultDto).
class PagedUsers {
  final List<UserModel> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  PagedUsers({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  factory PagedUsers.fromJson(Map<String, dynamic> json) {
    final raw = json['items'] as List? ?? [];
    return PagedUsers(
      items: raw
          .map((e) => UserModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 20,
      totalCount: json['totalCount'] as int? ?? 0,
      totalPages: json['totalPages'] as int? ?? 0,
    );
  }
}

/// Controller Admin quản lý người dùng (FR5.1) — /api/users.
class AdminUserController {
  final ApiClient _api = ApiClient();

  /// GET /users?page=&pageSize=&role=&search=&isLocked=
  Future<(PagedUsers?, String?)> list({
    int page = 1,
    int pageSize = 20,
    String? role,
    String? search,
    bool? isLocked,
  }) async {
    try {
      final q = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
      };
      if (role != null && role.isNotEmpty) q['role'] = role;
      if (search != null && search.trim().isNotEmpty) q['search'] = search.trim();
      if (isLocked != null) q['isLocked'] = isLocked;

      final res = await _api.dio.get('/users', queryParameters: q);
      if (res.statusCode == 200 && res.data is Map) {
        return (PagedUsers.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Không tải được danh sách user.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  /// POST /users — tạo tài khoản.
  Future<(UserModel?, String?)> create({
    required String phone,
    required String password,
    required String fullName,
    required String role,
    String? email,
  }) async {
    try {
      final res = await _api.dio.post('/users', data: {
        'phone': phone,
        'password': password,
        'fullName': fullName,
        'role': role,
        'email': email,
        'isPhoneVerified': true,
      });
      if (res.statusCode == 200 || res.statusCode == 201) {
        return (UserModel.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Tạo tài khoản thất bại.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  /// PUT /users/{id} — sửa họ tên / email / role (không đổi SĐT).
  Future<(UserModel?, String?)> update({
    required int id,
    required String fullName,
    required String role,
    String? email,
  }) async {
    try {
      final res = await _api.dio.put('/users/$id', data: {
        'fullName': fullName,
        'role': role,
        'email': email,
      });
      if (res.statusCode == 200) {
        return (UserModel.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Cập nhật thất bại.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(bool, String)> lock(int id) async {
    try {
      final res = await _api.dio.put('/users/$id/lock');
      if (res.statusCode == 200) return (true, 'Đã khóa tài khoản.');
      return (false, _msg(res.data, 'Khóa thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  Future<(bool, String)> unlock(int id) async {
    try {
      final res = await _api.dio.put('/users/$id/unlock');
      if (res.statusCode == 200) return (true, 'Đã mở khóa tài khoản.');
      return (false, _msg(res.data, 'Mở khóa thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  /// POST /users/{id}/reset-password
  Future<(bool, String)> resetPassword(int id, String newPassword) async {
    try {
      final res = await _api.dio.post('/users/$id/reset-password', data: {
        'newPassword': newPassword,
      });
      if (res.statusCode == 200) return (true, 'Đã đặt lại mật khẩu.');
      return (false, _msg(res.data, 'Reset mật khẩu thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  String _msg(dynamic data, String fallback) {
    if (data is Map && data['message'] is String) return data['message'] as String;
    return fallback;
  }

  String _dioErr(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'] as String;
    return 'Lỗi kết nối: ${e.message}';
  }
}
