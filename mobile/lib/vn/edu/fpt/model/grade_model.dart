import '../common/grade_utils.dart';

/// Model 1 bản ghi điểm (FR2.3/FR3.2) — khớp GradeDto bên API .NET.
/// HS/PH chỉ xem điểm đã công bố (Published).
class GradeModel {
  final int id;
  final int studentId;
  final String studentName;
  final int classId;
  final String className;
  final int subjectId;
  final String subjectName;
  final int semesterId;
  final String semesterName;
  final String assessmentType;
  final double score;
  final String status;
  final String createdByTeacherName;
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

  String get assessmentLabel => assessmentLabelVi(assessmentType);

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
