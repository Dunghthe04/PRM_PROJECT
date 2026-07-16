import 'package:dio/dio.dart';
import '../model/timetable_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Thời khóa biểu (FR2.3).
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class TimetableController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy TKB theo tuần của HS đang login (hoặc của 1 con khi PH gọi).
  ///
  /// Nhận:
  ///   - [weekStart]: ngày bất kỳ trong tuần muốn xem (null = tuần hiện tại).
  ///   - [studentId]: (chỉ PH) id con muốn xem; HS bỏ trống.
  /// Trả về `(WeeklyTimetableModel?, String?)`:
  ///   - (dữ liệu tuần, null) nếu thành công.
  ///   - (null, thông báo lỗi) nếu thất bại.
  Future<(WeeklyTimetableModel?, String?)> getMyWeek({
    DateTime? weekStart,
    int? studentId,
    int? semesterId,
  }) async {
    try {
      // Gộp các query có giá trị vào 1 map (bỏ qua null).
      final query = <String, dynamic>{};
      if (weekStart != null) {
        // Chỉ gửi phần ngày (yyyy-MM-dd) là đủ cho server tính tuần.
        query['weekStart'] = weekStart.toIso8601String();
      }
      if (studentId != null) query['studentId'] = studentId;
      if (semesterId != null) query['semesterId'] = semesterId;

      final response = await _apiClient.dio.get(
        '/timetable/me',
        queryParameters: query.isEmpty ? null : query,
      );
      if (response.statusCode == 200) {
        final week =
            WeeklyTimetableModel.fromJson(response.data as Map<String, dynamic>);
        return (week, null);
      }
      return (null, 'Không tải được thời khóa biểu (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Trích thông điệp lỗi thân thiện từ DioException.
  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'];
    return 'Lỗi kết nối: ${e.message}';
  }
}
