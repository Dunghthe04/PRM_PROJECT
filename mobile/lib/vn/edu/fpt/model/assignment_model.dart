// Model bài tập + bài nộp (FR2.4) — khớp AssignmentDtos.cs bên API .NET.
//
// Gồm 2 lớp:
//   - AssignmentModel: 1 bài tập (đề, hạn nộp, trạng thái HS).
//   - SubmissionModel: 1 bài nộp của HS (link/file, điểm, feedback).

/// Một bài tập được giao.
class AssignmentModel {
  final int id;
  final String title; // tiêu đề bài tập
  final String description; // nội dung / đề bài
  final DateTime dueDate; // hạn nộp
  final double maxScore; // điểm tối đa
  final String? attachmentUrl; // file đính kèm của đề (nếu có)
  final int classId;
  final String className;
  final int subjectId;
  final String subjectName;
  final String subjectCode;
  final String teacherName; // GV giao bài

  /// Trạng thái phía HS: "ToDo" | "Done" | "Overdue" (null khi GV xem DS lớp).
  final String? status;

  AssignmentModel({
    required this.id,
    required this.title,
    required this.description,
    required this.dueDate,
    required this.maxScore,
    this.attachmentUrl,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.subjectCode,
    required this.teacherName,
    this.status,
  });

  /// Nhãn tiếng Việt cho trạng thái bài tập.
  String get statusLabel {
    switch (status) {
      case 'ToDo':
        return 'Cần làm';
      case 'Done':
        return 'Đã nộp';
      case 'Overdue':
        return 'Quá hạn';
      default:
        return '';
    }
  }

  /// Tạo [AssignmentModel] từ JSON (AssignmentDto).
  factory AssignmentModel.fromJson(Map<String, dynamic> json) {
    return AssignmentModel(
      id: json['id'] as int,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      dueDate: DateTime.tryParse(json['dueDate'] as String? ?? '') ??
          DateTime.now(),
      maxScore: (json['maxScore'] as num?)?.toDouble() ?? 10,
      attachmentUrl: json['attachmentUrl'] as String?,
      classId: json['classId'] as int? ?? 0,
      className: json['className'] as String? ?? '',
      subjectId: json['subjectId'] as int? ?? 0,
      subjectName: json['subjectName'] as String? ?? '',
      subjectCode: json['subjectCode'] as String? ?? '',
      teacherName: json['teacherName'] as String? ?? '',
      status: json['status'] as String?,
    );
  }
}

/// Một bài nộp của học sinh cho 1 bài tập.
class SubmissionModel {
  final int id;
  final int assignmentId;
  final String assignmentTitle;
  final int studentId;
  final String studentName;
  final String? linkUrl; // link bài làm (Google Drive, GitHub…)
  final String? fileUrl; // file đã upload (nếu có)
  final DateTime submittedAt; // thời điểm nộp
  final double? score; // điểm GV chấm (null nếu chưa chấm)
  final String? feedback; // nhận xét của GV
  final String? gradedByTeacherName;
  final bool isLate; // nộp sau hạn?

  SubmissionModel({
    required this.id,
    required this.assignmentId,
    required this.assignmentTitle,
    required this.studentId,
    required this.studentName,
    this.linkUrl,
    this.fileUrl,
    required this.submittedAt,
    this.score,
    this.feedback,
    this.gradedByTeacherName,
    required this.isLate,
  });

  /// True nếu GV đã chấm điểm bài nộp này.
  bool get isGraded => score != null;

  /// Tạo [SubmissionModel] từ JSON (SubmissionDto).
  factory SubmissionModel.fromJson(Map<String, dynamic> json) {
    return SubmissionModel(
      id: json['id'] as int,
      assignmentId: json['assignmentId'] as int? ?? 0,
      assignmentTitle: json['assignmentTitle'] as String? ?? '',
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      linkUrl: json['linkUrl'] as String?,
      fileUrl: json['fileUrl'] as String?,
      submittedAt: DateTime.tryParse(json['submittedAt'] as String? ?? '') ??
          DateTime.now(),
      score: (json['score'] as num?)?.toDouble(),
      feedback: json['feedback'] as String?,
      gradedByTeacherName: json['gradedByTeacherName'] as String?,
      isLate: json['isLate'] as bool? ?? false,
    );
  }
}
