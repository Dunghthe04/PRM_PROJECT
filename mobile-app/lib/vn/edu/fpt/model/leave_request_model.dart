/// Một đơn xin nghỉ (FR2.5 / FR3.3) — khớp `LeaveRequestDto` bên API.
///
/// Quan hệ server: `LeaveRequest` → Class, Student, SubmittedBy, ApprovedByTeacher.
class LeaveRequestModel {
  /// Id đơn.
  final int id;

  /// FK → lớp của học sinh xin nghỉ.
  final int classId;

  /// Tên lớp.
  final String className;

  /// FK → học sinh xin nghỉ.
  final int studentId;

  /// Tên HS (PH có nhiều con cần phân biệt).
  final String studentName;

  /// Tên người nộp đơn (HS tự nộp hoặc PH nộp hộ).
  final String submittedByName;

  /// Ngày xin nghỉ.
  final DateTime date;

  /// Lý do xin nghỉ.
  final String reason;

  /// URL ảnh giấy y tế (nếu có).
  final String? medicalCertificateUrl;

  /// `Pending` | `Approved` | `Rejected`.
  final String status;

  /// Tên GV đã xử lý — null khi còn chờ duyệt.
  final String? approvedByTeacherName;

  /// Lý do từ chối — chỉ khi Rejected.
  final String? rejectionReason;

  /// Thời điểm nộp đơn.
  final DateTime createdAt;

  /// Thời điểm GV duyệt/từ chối.
  final DateTime? reviewedAt;

  LeaveRequestModel({
    required this.id,
    required this.classId,
    required this.className,
    required this.studentId,
    required this.studentName,
    required this.submittedByName,
    required this.date,
    required this.reason,
    this.medicalCertificateUrl,
    required this.status,
    this.approvedByTeacherName,
    this.rejectionReason,
    required this.createdAt,
    this.reviewedAt,
  });

  /// Còn chờ duyệt → cho phép hủy đơn.
  bool get isPending => status == 'Pending';

  /// Nhãn trạng thái tiếng Việt.
  String get statusLabel {
    switch (status) {
      case 'Pending':
        return 'Chờ duyệt';
      case 'Approved':
        return 'Đã duyệt';
      case 'Rejected':
        return 'Từ chối';
      default:
        return status;
    }
  }

  /// Parse từ JSON `LeaveRequestDto`.
  factory LeaveRequestModel.fromJson(Map<String, dynamic> json) {
    return LeaveRequestModel(
      id: json['id'] as int,
      classId: json['classId'] as int? ?? 0,
      className: json['className'] as String? ?? '',
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      submittedByName: json['submittedByName'] as String? ?? '',
      date: DateTime.tryParse(json['date'] as String? ?? '') ?? DateTime.now(),
      reason: json['reason'] as String? ?? '',
      medicalCertificateUrl: json['medicalCertificateUrl'] as String?,
      status: json['status'] as String? ?? 'Pending',
      approvedByTeacherName: json['approvedByTeacherName'] as String?,
      rejectionReason: json['rejectionReason'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
      reviewedAt: DateTime.tryParse(json['reviewedAt'] as String? ?? ''),
    );
  }
}
