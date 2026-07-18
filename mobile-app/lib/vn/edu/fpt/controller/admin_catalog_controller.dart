import 'package:dio/dio.dart';
import '../service/api_client.dart';

/// Kỳ học — khớp SemesterDto.
class SemesterModel {
  final int id;
  final String name;
  final DateTime startDate;
  final DateTime endDate;

  SemesterModel({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
  });

  factory SemesterModel.fromJson(Map<String, dynamic> json) => SemesterModel(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        startDate:
            DateTime.tryParse(json['startDate'] as String? ?? '') ?? DateTime.now(),
        endDate:
            DateTime.tryParse(json['endDate'] as String? ?? '') ?? DateTime.now(),
      );
}

/// Môn học — khớp SubjectDto.
class SubjectModel {
  final int id;
  final String name;
  final String code;

  SubjectModel({required this.id, required this.name, required this.code});

  factory SubjectModel.fromJson(Map<String, dynamic> json) => SubjectModel(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        code: json['code'] as String? ?? '',
      );
}

/// Lớp học — khớp ClassDto.
class ClassModel {
  final int id;
  final String name;
  final int semesterId;
  final String? semesterName;
  final int studentCount;
  final int? homeroomTeacherId;
  final String? homeroomTeacherName;

  ClassModel({
    required this.id,
    required this.name,
    required this.semesterId,
    this.semesterName,
    required this.studentCount,
    this.homeroomTeacherId,
    this.homeroomTeacherName,
  });

  factory ClassModel.fromJson(Map<String, dynamic> json) => ClassModel(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        semesterId: json['semesterId'] as int? ?? 0,
        semesterName: json['semesterName'] as String?,
        studentCount: json['studentCount'] as int? ?? 0,
        homeroomTeacherId: json['homeroomTeacherId'] as int?,
        homeroomTeacherName: json['homeroomTeacherName'] as String?,
      );
}

/// Controller danh mục Admin (FR5.2): Kỳ · Môn · Lớp.
/// (Không có entity Khối riêng — gắn khối trong tên lớp nếu cần, vd 10A1.)
class AdminCatalogController {
  final ApiClient _api = ApiClient();

  // ── Semesters ───────────────────────────────────────────────────────────

  Future<(List<SemesterModel>?, String?)> getSemesters() async {
    try {
      final res = await _api.dio.get('/semesters');
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) => SemesterModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được kỳ học.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(SemesterModel?, String?)> saveSemester({
    int? id,
    required String name,
    required DateTime start,
    required DateTime end,
  }) async {
    try {
      final body = {
        'name': name,
        'startDate': start.toIso8601String(),
        'endDate': end.toIso8601String(),
      };
      final res = id == null
          ? await _api.dio.post('/semesters', data: body)
          : await _api.dio.put('/semesters/$id', data: body);
      if (res.statusCode == 200 || res.statusCode == 201) {
        return (SemesterModel.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Lưu kỳ học thất bại.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(bool, String)> deleteSemester(int id) async {
    try {
      final res = await _api.dio.delete('/semesters/$id');
      if (res.statusCode == 200) return (true, 'Đã xóa kỳ học.');
      return (false, _msg(res.data, 'Xóa kỳ học thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  // ── Subjects ────────────────────────────────────────────────────────────

  Future<(List<SubjectModel>?, String?)> getSubjects() async {
    try {
      final res = await _api.dio.get('/subjects');
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) => SubjectModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được môn học.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(SubjectModel?, String?)> saveSubject({
    int? id,
    required String name,
    required String code,
  }) async {
    try {
      final body = {'name': name, 'code': code};
      final res = id == null
          ? await _api.dio.post('/subjects', data: body)
          : await _api.dio.put('/subjects/$id', data: body);
      if (res.statusCode == 200 || res.statusCode == 201) {
        return (SubjectModel.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Lưu môn học thất bại.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(bool, String)> deleteSubject(int id) async {
    try {
      final res = await _api.dio.delete('/subjects/$id');
      if (res.statusCode == 200) return (true, 'Đã xóa môn học.');
      return (false, _msg(res.data, 'Xóa môn học thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  // ── Classes ─────────────────────────────────────────────────────────────

  Future<(List<ClassModel>?, String?)> getClasses({int? semesterId}) async {
    try {
      final res = await _api.dio.get(
        '/classes',
        queryParameters: semesterId == null ? null : {'semesterId': semesterId},
      );
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) => ClassModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được lớp học.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(ClassModel?, String?)> saveClass({
    int? id,
    required String name,
    required int semesterId,
    int? homeroomTeacherId,
  }) async {
    try {
      final body = {
        'name': name,
        'semesterId': semesterId,
        'homeroomTeacherId': homeroomTeacherId,
      };
      final res = id == null
          ? await _api.dio.post('/classes', data: body)
          : await _api.dio.put('/classes/$id', data: body);
      if (res.statusCode == 200 || res.statusCode == 201) {
        return (ClassModel.fromJson(res.data as Map<String, dynamic>), null);
      }
      return (null, _msg(res.data, 'Lưu lớp học thất bại.'));
    } on DioException catch (e) {
      return (null, _dioErr(e));
    }
  }

  Future<(bool, String)> deleteClass(int id) async {
    try {
      final res = await _api.dio.delete('/classes/$id');
      if (res.statusCode == 200) return (true, 'Đã xóa lớp học.');
      return (false, _msg(res.data, 'Xóa lớp học thất bại.'));
    } on DioException catch (e) {
      return (false, _dioErr(e));
    }
  }

  String _msg(dynamic data, String fallback) {
    if (data is Map && data['message'] is String) return data['message'] as String;
    return fallback;
  }

  String _dioErr(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'] as String;
    return 'Lỗi kết nối: ${e.message}';
  }
}
