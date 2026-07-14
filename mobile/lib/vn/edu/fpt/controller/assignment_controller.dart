import 'package:dio/dio.dart';
import '../model/assignment_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Bài tập + nộp bài (FR2.4) cho HS/PH.
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class AssignmentController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách bài tập của HS (hoặc của 1 con khi PH gọi).
  ///
  /// Nhận:
  ///   - [status]: lọc "ToDo" | "Done" | "Overdue" (null = tất cả).
  ///   - [studentId]: (chỉ PH) id con muốn xem; HS bỏ trống.
  /// Trả về `(List<AssignmentModel>?, String?)`.
  Future<(List<AssignmentModel>?, String?)> getMyAssignments({
    String? status,
    int? studentId,
  }) async {
    try {
      final query = <String, dynamic>{};
      if (status != null) query['status'] = status;
      if (studentId != null) query['studentId'] = studentId;

      final response = await _apiClient.dio.get(
        '/assignments/me',
        queryParameters: query.isEmpty ? null : query,
      );
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => AssignmentModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được danh sách bài tập (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Lấy chi tiết 1 bài tập (GET /assignments/{id}).
  ///
  /// Nhận: [id] — id bài tập.
  /// Trả về `(AssignmentModel?, String?)`.
  Future<(AssignmentModel?, String?)> getById(int id) async {
    try {
      final response = await _apiClient.dio.get('/assignments/$id');
      if (response.statusCode == 200) {
        final item =
            AssignmentModel.fromJson(response.data as Map<String, dynamic>);
        return (item, null);
      }
      return (null, 'Không tải được bài tập (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// HS nộp bài (POST /assignments/{id}/submissions).
  ///
  /// Nhận:
  ///   - [assignmentId]: id bài tập cần nộp.
  ///   - [linkUrl]: link bài làm (tùy chọn).
  ///   - [fileUrl]: URL file đã upload (tùy chọn).
  ///   (cần ít nhất 1 trong 2).
  /// Trả về `(SubmissionModel?, String?)`:
  ///   - (bài nộp, null) nếu thành công.
  ///   - (null, thông báo lỗi) nếu thất bại.
  Future<(SubmissionModel?, String?)> submit({
    required int assignmentId,
    String? linkUrl,
    String? fileUrl,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/assignments/$assignmentId/submissions',
        data: {'linkUrl': linkUrl, 'fileUrl': fileUrl},
      );
      if (response.statusCode == 200) {
        final sub =
            SubmissionModel.fromJson(response.data as Map<String, dynamic>);
        return (sub, null);
      }
      return (null, 'Nộp bài thất bại (mã ${response.statusCode}).');
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
