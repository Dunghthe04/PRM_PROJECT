import 'package:dio/dio.dart';
import '../model/attendance_model.dart';
import '../service/api_client.dart';

/// Controller điểm danh (FR3.1) cho giáo viên (online-only ở Ngày 17).
/// Quy ước trả về: record (dữ liệu, lỗi).
class AttendanceController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy điểm danh đã lưu của lớp theo ngày — GET /api/attendance?classId=&date=.
  /// Dùng để pre-fill lưới điểm danh (nếu GV đã điểm danh hôm đó).
  ///
  /// Nhận: [classId], [date].
  Future<(List<AttendanceModel>?, String?)> getAttendance({
    required int classId,
    required DateTime date,
  }) async {
    try {
      final response = await _apiClient.dio.get('/attendance', queryParameters: {
        'classId': classId,
        // Chỉ cần phần ngày (yyyy-MM-dd) để so khớp theo ngày.
        'date': _dateOnly(date),
      });
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => AttendanceModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được điểm danh (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Gửi điểm danh hàng loạt — POST /api/attendance/batch.
  ///
  /// Nhận:
  ///   - [classId], [date].
  ///   - [entries]: map studentId -> trạng thái P/A/L.
  /// Trả về `(bool, String)`: (thành công?, thông báo).
  Future<(bool, String)> submitBatch({
    required int classId,
    required DateTime date,
    required Map<int, AttendanceStatus> entries,
  }) async {
    try {
      final body = {
        'classId': classId,
        'date': _dateOnly(date),
        'entries': entries.entries
            .map((e) => {'studentId': e.key, 'status': e.value.apiValue})
            .toList(),
      };
      final response = await _apiClient.dio.post('/attendance/batch', data: body);
      if (response.statusCode == 200) {
        final data = response.data;
        final msg = (data is Map && data['message'] is String)
            ? data['message'] as String
            : 'Đã lưu điểm danh.';
        return (true, msg);
      }
      return (false, 'Lưu điểm danh thất bại (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (false, _extractError(e));
    }
  }

  /// Cắt lấy phần ngày dạng yyyy-MM-dd (bỏ giờ).
  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'];
    return 'Lỗi kết nối: ${e.message}';
  }
}
