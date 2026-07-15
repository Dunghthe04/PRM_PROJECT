// Model lớp+môn mà giáo viên được phân công dạy (FR3.2/FR3.1).
// Khớp TeacherClassDto bên API (.NET): GET /api/teachers/{id}/classes.

/// Một phân công giảng dạy: lớp + môn + kỳ học.
class TeacherClassModel {
  final int assignmentId;
  final int classId;
  final String className;
  final int subjectId;
  final String subjectName;
  final String subjectCode;
  final int semesterId; // dùng luôn cho nhập điểm (khỏi gọi /semesters)
  final String? semesterName;

  TeacherClassModel({
    required this.assignmentId,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.subjectCode,
    required this.semesterId,
    this.semesterName,
  });

  factory TeacherClassModel.fromJson(Map<String, dynamic> json) {
    return TeacherClassModel(
      assignmentId: json['assignmentId'] as int? ?? 0,
      classId: json['classId'] as int? ?? 0,
      className: json['className'] as String? ?? '',
      subjectId: json['subjectId'] as int? ?? 0,
      subjectName: json['subjectName'] as String? ?? '',
      subjectCode: json['subjectCode'] as String? ?? '',
      semesterId: json['semesterId'] as int? ?? 0,
      semesterName: json['semesterName'] as String?,
    );
  }
}

/// Một học sinh trong lớp (roster) — khớp ClassStudentDto.
/// GET /api/classes/{id}/students.
class ClassStudentModel {
  final int studentId;
  final String phone;
  final String fullName;

  ClassStudentModel({
    required this.studentId,
    required this.phone,
    required this.fullName,
  });

  factory ClassStudentModel.fromJson(Map<String, dynamic> json) {
    return ClassStudentModel(
      studentId: json['studentId'] as int? ?? 0,
      phone: json['phone'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
    );
  }
}
