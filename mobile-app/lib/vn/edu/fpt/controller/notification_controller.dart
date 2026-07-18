import 'package:dio/dio.dart';
import '../model/notification_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Trung tâm thông báo (FR1.4).
class NotificationController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách thông báo của user (GET /notifications, có phân trang).
  ///
  /// Nhận:
  ///   - [page]: trang cần lấy (mặc định 1).
  ///   - [pageSize]: số item mỗi trang (mặc định 20).
  /// Trả về `(List<NotificationModel>?, String?)`:
  ///   - (danh sách, null) nếu thành công.
  ///   - (null, lỗi)       nếu thất bại.
  ///
  /// Lưu ý: API trả về object phân trang { items, page, totalCount... },
  /// ta chỉ lấy mảng "items".
  Future<(List<NotificationModel>?, String?)> getList({
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final response = await _apiClient.dio.get(
        '/notifications',
        queryParameters: {'page': page, 'pageSize': pageSize},
      );
      if (response.statusCode == 200) {
        final items = (response.data['items'] as List)
            .map((e) => NotificationModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (items, null);
      }
      return (null, 'Không tải được thông báo (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }

  /// Đếm số thông báo CHƯA đọc (GET /notifications/unread-count).
  ///
  /// Nhận: không tham số.
  /// Trả về (int): số lượng chưa đọc; lỗi thì trả 0 (không làm crash badge).
  Future<int> getUnreadCount() async {
    try {
      final response = await _apiClient.dio.get('/notifications/unread-count');
      if (response.statusCode == 200) {
        return (response.data['count'] as int?) ?? 0;
      }
    } catch (_) {
      // Lỗi mạng → coi như 0.
    }
    return 0;
  }

  /// Đánh dấu 1 thông báo đã đọc (PUT /notifications/{id}/read).
  ///
  /// Nhận: [id] — id thông báo.
  /// Trả về (bool): true nếu thành công.
  Future<bool> markRead(int id) async {
    try {
      final response = await _apiClient.dio.put('/notifications/$id/read');
      return response.statusCode == 200;
    } on DioException {
      return false;
    }
  }

  /// Đánh dấu TẤT CẢ đã đọc (PUT /notifications/read-all).
  ///
  /// Nhận: không tham số.
  /// Trả về (bool): true nếu thành công.
  Future<bool> markAllRead() async {
    try {
      final response = await _apiClient.dio.put('/notifications/read-all');
      return response.statusCode == 200;
    } on DioException {
      return false;
    }
  }
}
