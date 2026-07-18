import 'package:dio/dio.dart';
import '../common/format_utils.dart';
import '../service/api_client.dart';

/// Dashboard tổng quan — ReportDashboardDto.
class ReportDashboardModel {
  final int totalStudents;
  final int totalTeachers;
  final int totalClasses;
  final int totalParents;
  final int publishedGradeCount;
  final double? averageAttendanceRate;
  final int pendingInvoiceCount;
  final int paidInvoiceCount;
  final double totalPaidAmount;
  final double totalPendingAmount;
  final DateTime generatedAt;

  ReportDashboardModel({
    required this.totalStudents,
    required this.totalTeachers,
    required this.totalClasses,
    required this.totalParents,
    required this.publishedGradeCount,
    this.averageAttendanceRate,
    required this.pendingInvoiceCount,
    required this.paidInvoiceCount,
    required this.totalPaidAmount,
    required this.totalPendingAmount,
    required this.generatedAt,
  });

  factory ReportDashboardModel.fromJson(Map<String, dynamic> json) =>
      ReportDashboardModel(
        totalStudents: json['totalStudents'] as int? ?? 0,
        totalTeachers: json['totalTeachers'] as int? ?? 0,
        totalClasses: json['totalClasses'] as int? ?? 0,
        totalParents: json['totalParents'] as int? ?? 0,
        publishedGradeCount: json['publishedGradeCount'] as int? ?? 0,
        averageAttendanceRate:
            (json['averageAttendanceRate'] as num?)?.toDouble(),
        pendingInvoiceCount: json['pendingInvoiceCount'] as int? ?? 0,
        paidInvoiceCount: json['paidInvoiceCount'] as int? ?? 0,
        totalPaidAmount: (json['totalPaidAmount'] as num?)?.toDouble() ?? 0,
        totalPendingAmount:
            (json['totalPendingAmount'] as num?)?.toDouble() ?? 0,
        generatedAt: DateTime.tryParse(json['generatedAt'] as String? ?? '') ??
            DateTime.now(),
      );

  String get paidLabel => FormatUtils.currency(totalPaidAmount);
  String get pendingLabel => FormatUtils.currency(totalPendingAmount);
  String get attendanceLabel => averageAttendanceRate == null
      ? '—'
      : '${averageAttendanceRate!.toStringAsFixed(1)}%';
}

class GradeReportRow {
  final String studentName;
  final String subjectName;
  final String assessmentType;
  final double score;
  final String status;

  GradeReportRow({
    required this.studentName,
    required this.subjectName,
    required this.assessmentType,
    required this.score,
    required this.status,
  });

  factory GradeReportRow.fromJson(Map<String, dynamic> json) => GradeReportRow(
        studentName: json['studentName'] as String? ?? '',
        subjectName: json['subjectName'] as String? ?? '',
        assessmentType: json['assessmentType'] as String? ?? '',
        score: (json['score'] as num?)?.toDouble() ?? 0,
        status: json['status'] as String? ?? '',
      );
}

class AttendanceReportRow {
  final String studentName;
  final int present;
  final int absent;
  final int late;
  final int totalSessions;
  final double attendanceRate;

  AttendanceReportRow({
    required this.studentName,
    required this.present,
    required this.absent,
    required this.late,
    required this.totalSessions,
    required this.attendanceRate,
  });

  factory AttendanceReportRow.fromJson(Map<String, dynamic> json) =>
      AttendanceReportRow(
        studentName: json['studentName'] as String? ?? '',
        present: json['presentCount'] as int? ?? 0,
        absent: json['absentCount'] as int? ?? 0,
        late: json['lateCount'] as int? ?? 0,
        totalSessions: json['totalSessions'] as int? ?? 0,
        attendanceRate: (json['attendanceRate'] as num?)?.toDouble() ?? 0,
      );
}

class FeeReportRow {
  final String studentName;
  final String categoryName;
  final double amount;
  final String status;
  final bool isPaid;

  FeeReportRow({
    required this.studentName,
    required this.categoryName,
    required this.amount,
    required this.status,
    required this.isPaid,
  });

  factory FeeReportRow.fromJson(Map<String, dynamic> json) => FeeReportRow(
        studentName: json['studentName'] as String? ?? '',
        categoryName: json['feeCategoryName'] as String? ??
            json['categoryName'] as String? ??
            '',
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        status: json['status'] as String? ?? '',
        isPaid: json['isPaid'] as bool? ?? false,
      );
}

/// Controller báo cáo Admin (FR5.5) — chỉ JSON, xem trên màn.
class AdminReportController {
  final ApiClient _api = ApiClient();

  Future<(ReportDashboardModel?, String?)> getDashboard({int? classId}) async {
    try {
      final res = await _api.dio.get(
        '/reports/dashboard',
        queryParameters: classId == null ? null : {'classId': classId},
      );
      if (res.statusCode == 200 && res.data is Map) {
        return (
          ReportDashboardModel.fromJson(res.data as Map<String, dynamic>),
          null
        );
      }
      return (null, _msg(res.data, 'Không tải được dashboard.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(List<GradeReportRow>?, String?)> getGrades({int? classId}) async {
    try {
      final q = <String, dynamic>{};
      if (classId != null) q['classId'] = classId;
      final res = await _api.dio.get(
        '/reports/grades',
        queryParameters: q.isEmpty ? null : q,
      );
      if (res.statusCode == 200 && res.data is Map) {
        final rows = (res.data['rows'] as List? ?? [])
            .map((e) => GradeReportRow.fromJson(e as Map<String, dynamic>))
            .toList();
        return (rows, null);
      }
      return (null, _msg(res.data, 'Không tải được báo cáo điểm.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(List<AttendanceReportRow>?, String?, double?)> getAttendance({
    required int classId,
  }) async {
    try {
      final res = await _api.dio.get('/reports/attendance', queryParameters: {
        'classId': classId,
      });
      if (res.statusCode == 200 && res.data is Map) {
        final data = res.data as Map<String, dynamic>;
        final rows = (data['rows'] as List? ?? [])
            .map((e) =>
                AttendanceReportRow.fromJson(e as Map<String, dynamic>))
            .toList();
        final rate = (data['classAttendanceRate'] as num?)?.toDouble();
        return (rows, null, rate);
      }
      return (null, _msg(res.data, 'Không tải được chuyên cần.'), null);
    } on DioException catch (e) {
      return (null, _dio(e), null);
    }
  }

  Future<(List<FeeReportRow>?, String?)> getFees({int? classId}) async {
    try {
      final q = <String, dynamic>{};
      if (classId != null) q['classId'] = classId;
      final res = await _api.dio.get(
        '/reports/fees',
        queryParameters: q.isEmpty ? null : q,
      );
      if (res.statusCode == 200 && res.data is Map) {
        final rows = (res.data['rows'] as List? ?? [])
            .map((e) => FeeReportRow.fromJson(e as Map<String, dynamic>))
            .toList();
        return (rows, null);
      }
      return (null, _msg(res.data, 'Không tải được báo cáo học phí.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  String _msg(dynamic data, String fallback) {
    if (data is Map && data['message'] is String) {
      return data['message'] as String;
    }
    return fallback;
  }

  String _dio(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) {
      return data['message'] as String;
    }
    return 'Lỗi kết nối: ${e.message}';
  }
}
