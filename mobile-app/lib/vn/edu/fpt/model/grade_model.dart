import '../common/grade_utils.dart';

/// Một bản ghi điểm đã công bố (FR2.3) — khớp `GradeDto` bên API.
///
/// Quan hệ server: `Grade` → Student, Class, Subject, Semester, CreatedByTeacher.
/// HS/PH chỉ nhận điểm `Published` (API `/grades/me` đã lọc).
class GradeModel {
  /// Id bản ghi điểm.
  final int id;

  /// FK → học sinh được chấm.
  final int studentId;

  /// Tên học sinh (PH xem nhiều con).
  final String studentName;

  /// FK → lớp.
  final int classId;

  /// Tên lớp.
  final String className;

  /// FK → môn.
  final int subjectId;

  /// Tên môn.
  final String subjectName;

  /// FK → học kỳ.
  final int semesterId;

  /// Tên học kỳ.
  final String semesterName;

  /// Loại đầu điểm (Midterm, Final, Oral…).
  final String assessmentType;

  /// Điểm số.
  final double score;

  /// `Draft` | `Published` (client thường chỉ thấy Published).
  final String status;

  /// Tên người nhập điểm.
  final String createdByTeacherName;

  /// Thời điểm công bố — null nếu còn nháp.
  final DateTime? publishedAt;

  GradeModel({
    required this.id,
    required this.studentId,
    required this.studentName,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.semesterId,
    required this.semesterName,
    required this.assessmentType,
    required this.score,
    required this.status,
    required this.createdByTeacherName,
    this.publishedAt,
  });

  /// Nhãn đầu điểm tiếng Việt.
  String get assessmentLabel => assessmentLabelVi(assessmentType);

  /// Parse từ JSON `GradeDto`.
  factory GradeModel.fromJson(Map<String, dynamic> json) {
    return GradeModel(
      id: json['id'] as int,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      classId: json['classId'] as int? ?? 0,
      className: json['className'] as String? ?? '',
      subjectId: json['subjectId'] as int? ?? 0,
      subjectName: json['subjectName'] as String? ?? '',
      semesterId: json['semesterId'] as int? ?? 0,
      semesterName: json['semesterName'] as String? ?? '',
      assessmentType: json['assessmentType'] as String? ?? '',
      score: (json['score'] as num?)?.toDouble() ?? 0,
      status: json['status'] as String? ?? '',
      createdByTeacherName: json['createdByTeacherName'] as String? ?? '',
      publishedAt: DateTime.tryParse(json['publishedAt'] as String? ?? ''),
    );
  }
}
