import 'package:dio/dio.dart';
import '../model/teacher_class_model.dart';
import '../service/api_client.dart';

/// Controller cho giáo viên: lấy lớp/môn được phân công + roster học sinh.
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class TeacherController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách lớp+môn GV phụ trách — GET /api/teachers/{id}/classes.
  ///
  /// Nhận: [teacherId] — id của GV (lấy từ user đang login).
  Future<(List<TeacherClassModel>?, String?)> getMyClasses(int teacherId) async {
    try {
      final response = await _apiClient.dio.get('/teachers/$teacherId/classes');
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => TeacherClassModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được lớp dạy (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Lấy danh sách học sinh 1 lớp — GET /api/classes/{id}/students.
  ///
  /// Nhận: [classId] — id lớp.
  Future<(List<ClassStudentModel>?, String?)> getClassStudents(int classId) async {
    try {
      final response = await _apiClient.dio.get('/classes/$classId/students');
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => ClassStudentModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được danh sách HS (mã ${response.statusCode}).');
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
