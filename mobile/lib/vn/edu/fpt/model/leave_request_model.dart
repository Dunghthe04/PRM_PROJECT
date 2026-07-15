/// Model 1 đơn xin nghỉ (FR2.5) — khớp LeaveRequestDto bên API .NET.
/// HS/PH tạo & theo dõi; GV duyệt/từ chối.
class LeaveRequestModel {
  final int id;
  final int classId;
  final String className;
  final int studentId;
  final String studentName; // tên HS (hữu ích khi PH có nhiều con)
  final String submittedByName; // người nộp đơn (HS hoặc PH)
  final DateTime date; // ngày xin nghỉ
  final String reason; // lý do
  final String? medicalCertificateUrl; // URL ảnh y tế đính kèm (nếu có)
  final String status; // Pending | Approved | Rejected
  final String? approvedByTeacherName; // GV đã xử lý
  final String? rejectionReason; // lý do từ chối (nếu bị từ chối)
  final DateTime createdAt; // thời điểm nộp
  final DateTime? reviewedAt; // thời điểm GV xử lý

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

  /// Còn chờ duyệt → mới cho phép hủy đơn.
  bool get isPending => status == 'Pending';

  /// Nhãn trạng thái tiếng Việt để hiển thị.
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

  /// Tạo [LeaveRequestModel] từ JSON (LeaveRequestDto).
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
      createdAt:
          DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
      reviewedAt: DateTime.tryParse(json['reviewedAt'] as String? ?? ''),
    );
  }
}
