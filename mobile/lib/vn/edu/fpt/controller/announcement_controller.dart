import 'package:dio/dio.dart';
import '../model/announcement_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Bảng tin (FR2.2).
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class AnnouncementController {
  // Client HTTP dùng chung (interceptor tự gắn JWT).
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách bảng tin hiển thị với user hiện tại (GET /announcements).
  ///
  /// Nhận:
  ///   - [type]: lọc theo loại — null = tất cả, 'Global' = toàn trường,
  ///     'Class' = theo lớp.
  /// Trả về `(List<AnnouncementModel>?, String?)`:
  ///   - (danh sách, null) nếu thành công.
  ///   - (null, lỗi)       nếu thất bại.
  Future<(List<AnnouncementModel>?, String?)> getList({String? type}) async {
    try {
      final response = await _apiClient.dio.get(
        '/announcements',
        // Chỉ gắn query khi có lọc; null thì không gửi param.
        queryParameters: type == null ? null : {'type': type},
      );
      if (response.statusCode == 200) {
        // response.data là 1 mảng JSON → map từng phần tử sang model.
        final list = (response.data as List)
            .map((e) => AnnouncementModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được bảng tin (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }
}
