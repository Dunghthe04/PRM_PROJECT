import 'package:dio/dio.dart';
import '../model/grade_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Điểm số (FR2.3) cho HS/PH.
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class GradeController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy điểm đã công bố của HS đang login (hoặc của 1 con khi PH gọi).
  ///
  /// Nhận:
  ///   - [semesterId]: lọc theo kỳ (null = tất cả kỳ).
  ///   - [studentId]: (chỉ PH) id con muốn xem (null = tất cả các con).
  /// Trả về `(List<GradeModel>?, String?)`:
  ///   - (danh sách điểm, null) nếu thành công.
  ///   - (null, thông báo lỗi) nếu thất bại.
  Future<(List<GradeModel>?, String?)> getMyGrades({
    int? semesterId,
    int? studentId,
  }) async {
    try {
      final query = <String, dynamic>{};
      if (semesterId != null) query['semesterId'] = semesterId;
      if (studentId != null) query['studentId'] = studentId;

      final response = await _apiClient.dio.get(
        '/grades/me',
        queryParameters: query.isEmpty ? null : query,
      );
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => GradeModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được bảng điểm (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'];
    return 'Lỗi kết nối: ${e.message}';
  }
}
