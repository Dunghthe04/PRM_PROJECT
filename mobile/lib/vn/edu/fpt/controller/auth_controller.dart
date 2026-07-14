import 'package:dio/dio.dart';
import '../model/user_model.dart';
import '../service/api_client.dart';
import '../service/token_storage.dart';

/// Controller xử lý nghiệp vụ xác thực (đăng nhập / đăng xuất).
class AuthController {
  // Tạo 1 ApiClient để gọi API (đã cấu hình baseUrl + interceptor).
  final ApiClient _apiClient = ApiClient();

/// Đăng nhập bằng SĐT + mật khẩu.
/// Trả về record (user, error):
///  - Thành công: (UserModel, null)
///  - Thất bại:   (null, "thông báo lỗi")
/// (Giống pattern (Result, Error) bên Service API .NET

Future<(UserModel?, String?)> login(String phone, String password) async {
  try{
    // Gọi POST /User/login (baseUrl đã có sẵn '.../api')
    final response = await _apiClient.dio.post(
      '/User/login',
      data: {
        'phone': phone,
        'password': password,
      },
    );

    //statusCode 200 login thành công
    if(response.statusCode ==200){
      final data = response.data; // Map JSON {token, user}

      //lưu token vào kho an toàn để các request khác dùng
      await TokenStorage.saveToken(data['token'] as String);

      //chuyển dữ liệu user JSON thành user model
      final user = UserModel.fromJson(data['user'] as Map<String, dynamic>);
      return (user,null);
    }
    // Các mã khác (401/400...): body có thể là Map {message} HOẶC chuỗi thô
    String? message;
    final data = response.data;
    if (data is Map) {
      message = data['message'] as String?; // trường hợp JSON chuẩn
    } else if (data is String) {
      message = data; // trường hợp server trả chuỗi
    }
    return (null, message ?? 'Đăng nhập thất bại (mã ${response.statusCode}).');
  } on DioException catch (e) {
  // Bắt lỗi kết nối (không tới được server, timeout...)
  return (null, 'Lỗi kết nối tới máy chủ: ${e.message}');
  }
}
//Đăng xuất --> xóa token
Future<void> logout() async {
  await TokenStorage.clearToken();
}
}