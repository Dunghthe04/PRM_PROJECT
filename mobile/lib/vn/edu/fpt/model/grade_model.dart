/// Model 1 bản ghi điểm (FR2.3/FR3.2) — khớp GradeDto bên API .NET.
/// HS/PH chỉ xem điểm đã công bố (Published).
class GradeModel {
  final int id;
  final int studentId;
  final String studentName; // tên HS (hữu ích khi PH có nhiều con)
  final int subjectId;
  final String subjectName; // tên môn
  final int semesterId;
  final String semesterName; // tên kỳ (vd "Học kỳ 1")
  final String assessmentType; // loại điểm: Midterm, Final, Oral, Quiz15…
  final double score; // điểm số
  final String status; // Draft | Published (HS/PH chỉ nhận Published)
  final String createdByTeacherName; // GV nhập điểm
  final DateTime? publishedAt; // thời điểm công bố

  GradeModel({
    required this.id,
    required this.studentId,
    required this.studentName,
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

  /// Nhãn tiếng Việt cho loại đầu điểm (assessmentType gốc là tiếng Anh).
  String get assessmentLabel {
    switch (assessmentType) {
      case 'Oral':
        return 'Miệng';
      case 'Quiz15':
        return '15 phút';
      case 'Midterm':
        return 'Giữa kỳ';
      case 'Final':
        return 'Cuối kỳ';
      default:
        return assessmentType;
    }
  }

  /// Tạo [GradeModel] từ 1 phần tử JSON trong /api/grades/me.
  factory GradeModel.fromJson(Map<String, dynamic> json) {
    return GradeModel(
      id: json['id'] as int,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      subjectId: json['subjectId'] as int? ?? 0,
      subjectName: json['subjectName'] as String? ?? '',
      semesterId: json['semesterId'] as int? ?? 0,
      semesterName: json['semesterName'] as String? ?? '',
      assessmentType: json['assessmentType'] as String? ?? '',
      // score có thể về dạng int (8) hoặc double (8.5) → ép về double an toàn.
      score: (json['score'] as num?)?.toDouble() ?? 0,
      status: json['status'] as String? ?? '',
      createdByTeacherName: json['createdByTeacherName'] as String? ?? '',
      publishedAt: DateTime.tryParse(json['publishedAt'] as String? ?? ''),
    );
  }
}
