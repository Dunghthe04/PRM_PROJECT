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

  /// Lịch sử bảng tin do chính user tạo (GET /announcements/mine) — tab Đã gửi của GV.
  Future<(List<AnnouncementModel>?, String?)> getMine() async {
    try {
      final response = await _apiClient.dio.get('/announcements/mine');
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => AnnouncementModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được lịch sử đã gửi (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }

  // ─── Phần dành cho GIÁO VIÊN / ADMIN (FR3.4, FR5.4) ────────────────────────

  /// Soạn & gửi thông báo (kèm push) — POST /announcements.
  ///
  /// Nhận:
  ///   - [title], [content]: nội dung thông báo.
  ///   - [type]: Global | Class | Teachers | Teacher.
  ///   - [targetClassId]: bắt buộc khi type = Class.
  ///   - [targetUserId]: bắt buộc khi type = Teacher.
  ///   - [sendPush]: có gửi push notification không (mặc định true).
  /// Trả về `(int?, String?)`: (số người được thông báo, null) hoặc (null, lỗi).
  Future<(int?, String?)> create({
    required String title,
    required String content,
    required String type,
    int? targetClassId,
    int? subjectId,
    int? targetUserId,
    bool sendPush = true,
  }) async {
    try {
      final response = await _apiClient.dio.post('/announcements', data: {
        'title': title,
        'content': content,
        'type': type,
        'targetClassId': targetClassId,
        if (subjectId != null) 'subjectId': subjectId,
        if (targetUserId != null) 'targetUserId': targetUserId,
        'sendPush': sendPush,
      });
      if (response.statusCode == 200 || response.statusCode == 201) {
        final data = response.data;
        final count = (data is Map && data['notifiedUserCount'] is int)
            ? data['notifiedUserCount'] as int
            : 0;
        return (count, null);
      }
      // 400 từ service: { message: "..." }; binding lỗi: format khác.
      final data = response.data;
      if (data is Map && data['message'] is String) {
        return (null, data['message'] as String);
      }
      return (null, 'Gửi thông báo thất bại (mã ${response.statusCode}).');
    } on DioException catch (e) {
      final data = e.response?.data;
      if (data is Map && data['message'] is String) {
        return (null, data['message'] as String);
      }
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }
}
