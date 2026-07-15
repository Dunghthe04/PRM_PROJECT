import 'package:dio/dio.dart';
import '../model/leave_request_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Đơn xin nghỉ (FR2.5) cho HS/PH.
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class LeaveRequestController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách đơn của HS (hoặc tất cả con nếu là PH) — GET /leave-requests/me.
  ///
  /// Server tự trả:
  ///   - HS: đơn của chính mình.
  ///   - PH: đơn của tất cả các con (mỗi đơn có studentName để phân biệt).
  /// Trả về `(List<LeaveRequestModel>?, String?)`.
  Future<(List<LeaveRequestModel>?, String?)> getMy() async {
    try {
      final response = await _apiClient.dio.get('/leave-requests/me');
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => LeaveRequestModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được danh sách đơn (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Tạo đơn xin nghỉ mới — POST /leave-requests.
  ///
  /// Nhận:
  ///   - [date]: ngày xin nghỉ (không được là ngày đã qua).
  ///   - [reason]: lý do (bắt buộc).
  ///   - [studentId]: (chỉ PH) id con cần xin nghỉ; HS bỏ trống.
  ///   - [medicalCertificateUrl]: URL ảnh y tế đã upload (tùy chọn).
  /// Trả về `(LeaveRequestModel?, String?)`:
  ///   - (đơn vừa tạo, null) nếu thành công.
  ///   - (null, lỗi) nếu thất bại.
  Future<(LeaveRequestModel?, String?)> create({
    required DateTime date,
    required String reason,
    int? studentId,
    String? medicalCertificateUrl,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/leave-requests',
        data: {
          'studentId': studentId,
          'date': date.toIso8601String(),
          'reason': reason,
          'medicalCertificateUrl': medicalCertificateUrl,
        },
      );
      if (response.statusCode == 200) {
        final item =
            LeaveRequestModel.fromJson(response.data as Map<String, dynamic>);
        return (item, null);
      }
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Tạo đơn thất bại (mã ${response.statusCode}).';
      return (null, msg);
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Hủy đơn khi còn Chờ duyệt — DELETE /leave-requests/{id}.
  ///
  /// Nhận: [id] — id đơn cần hủy.
  /// Trả về `(bool, String)`: (thành công?, thông báo).
  Future<(bool, String)> cancel(int id) async {
    try {
      final response = await _apiClient.dio.delete('/leave-requests/$id');
      if (response.statusCode == 200) {
        return (true, 'Đã hủy đơn.');
      }
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Hủy đơn thất bại (mã ${response.statusCode}).';
      return (false, msg);
    } on DioException catch (e) {
      return (false, _extractError(e));
    }
  }

  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'];
    return 'Lỗi kết nối: ${e.message}';
  }
}
