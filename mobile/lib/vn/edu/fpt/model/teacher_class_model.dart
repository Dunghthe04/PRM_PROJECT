/// Model phân công giảng dạy & roster lớp (FR3.1).
///
/// Quan hệ server:
/// - [TeacherClassModel] ← `TeacherAssignment` (Teacher + Class + Subject + Semester).
/// - [ClassStudentModel] ← `ClassStudent` (Class ↔ Student).

/// Một phân công: GV dạy môn X cho lớp Y trong kỳ Z.
class TeacherClassModel {
  /// Id `TeacherAssignment`.
  final int assignmentId;

  /// FK → lớp.
  final int classId;

  /// Tên lớp.
  final String className;

  /// FK → môn.
  final int subjectId;

  /// Tên môn.
  final String subjectName;

  /// Mã môn.
  final String subjectCode;

  /// FK → học kỳ (lấy từ Class.Semester).
  final int semesterId;

  /// Tên học kỳ (có thể null nếu API không trả).
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

  /// Parse từ JSON `TeacherClassDto` (`GET /teachers/{id}/classes`).
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

/// Một học sinh trong roster lớp — khớp `ClassStudentDto`.
class ClassStudentModel {
  /// FK → User (Role = Student).
  final int studentId;

  /// SĐT học sinh.
  final String phone;

  /// Họ tên học sinh.
  final String fullName;

  ClassStudentModel({
    required this.studentId,
    required this.phone,
    required this.fullName,
  });

  /// Parse từ JSON (`GET /classes/{id}/students`).
  factory ClassStudentModel.fromJson(Map<String, dynamic> json) {
    return ClassStudentModel(
      studentId: json['studentId'] as int? ?? 0,
      phone: json['phone'] as String? ?? '',
      fullName: json['fullName'] as String? ?? '',
    );
  }
}
