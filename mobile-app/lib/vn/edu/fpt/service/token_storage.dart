import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Lưu / đọc / xóa JWT token trong bộ nhớ mã hóa của thiết bị.
/// Cache RAM để mỗi request HTTP không đọc lại Secure Storage (chậm trên 1 số máy).
class TokenStorage {
  static const FlutterSecureStorage _storage = FlutterSecureStorage();
  static const String _keyToken = 'jwt_token';

  /// Cache trong RAM — tránh đọc Secure Storage mỗi request.
  static String? _memoryToken;

  /// Lưu token sau khi đăng nhập thành công.
  static Future<void> saveToken(String token) async {
    _memoryToken = token;
    await _storage.write(key: _keyToken, value: token);
  }

  /// Đọc token đã lưu. Trả về null nếu chưa đăng nhập.
  static Future<String?> getToken() async {
    if (_memoryToken != null && _memoryToken!.isNotEmpty) return _memoryToken;
    _memoryToken = await _storage.read(key: _keyToken);
    return _memoryToken;
  }

  /// Xóa token khi đăng xuất.
  static Future<void> clearToken() async {
    _memoryToken = null;
    await _storage.delete(key: _keyToken);
  }
}
