import 'package:flutter_secure_storage/flutter_secure_storage.dart';
/// Lưu / đọc / xóa JWT token trong bộ nhớ mã hóa của thiết bị.
/// Dùng chung toàn app: sau login lưu token, mỗi request đọc ra gắn vào header.
class TokenStorage {
  /// _storage: đối tượng truy cập kho an toàn. 'const' vì cấu hình cố định.
  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  /// Khóa (key) để lưu — như "tên ngăn tủ" chứa token.
  static const String _keyToken = 'jwt_token';

  /// Lưu token sau khi đăng nhập thành công.
  /// `Future<void>` = hàm bất đồng bộ, không trả về giá trị.
  static Future<void> saveToken(String token) async{
     await _storage.write(key: _keyToken, value: token);
  }

  /// Đọc token đã lưu. Trả về null nếu chưa đăng nhập.
  static Future<String?> getToken() async{
    return await _storage.read(key: _keyToken);
  }

  ///Xóa token khi đăng xuất
  static Future<void> clearToken() async{
    await _storage.delete(key: _keyToken);
  }
}