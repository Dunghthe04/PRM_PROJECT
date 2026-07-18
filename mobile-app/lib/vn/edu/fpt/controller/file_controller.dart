import 'package:dio/dio.dart';
import '../service/api_client.dart';

/// Controller upload file dùng chung (ảnh y tế, bài nộp…).
/// Gọi POST /api/files/upload dạng multipart; trả về URL tương đối server lưu.
class FileController {
  final ApiClient _apiClient = ApiClient();

  /// Upload 1 file từ đường dẫn cục bộ lên server.
  ///
  /// Nhận:
  ///   - [filePath]: đường dẫn file trên máy (từ image_picker).
  ///   - [folder]: thư mục con lưu trên server (mặc định "uploads").
  /// Trả về `(String?, String?)`:
  ///   - (url, null) — url tương đối vd "/uploads/abc.jpg" nếu thành công.
  ///   - (null, lỗi) nếu thất bại.
  Future<(String?, String?)> upload(String filePath,
      {String folder = 'uploads'}) async {
    try {
      // MultipartFile: bọc file để gửi kèm request dạng form-data.
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(filePath),
      });

      final response = await _apiClient.dio.post(
        '/files/upload',
        data: formData,
        queryParameters: {'folder': folder},
      );

      if (response.statusCode == 200) {
        // Response: { url, fileName }
        final url = (response.data as Map<String, dynamic>)['url'] as String?;
        if (url != null) return (url, null);
        return (null, 'Phản hồi upload không hợp lệ.');
      }
      // API trả message lỗi khi status < 500 (validateStatus ở ApiClient).
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Upload thất bại (mã ${response.statusCode}).';
      return (null, msg);
    } on DioException catch (e) {
      final data = e.response?.data;
      if (data is Map && data['message'] is String) {
        return (null, data['message'] as String);
      }
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }
}
