/// Model điểm danh (FR3.1) — khớp `AttendanceDto` bên API .NET.
///
/// Quan hệ server: `Attendance` → Class, Student, RecordedByTeacher.
/// Request body gửi trạng thái dạng số (0/1/2); response trả chuỗi enum.

/// Ba trạng thái điểm danh — index khớp enum backend.
enum AttendanceStatus {
  /// 0 — Có mặt (P).
  present,

  /// 1 — Vắng (A).
  absent,

  /// 2 — Muộn (L).
  late,
}

/// Tiện ích chuyển đổi / hiển thị [AttendanceStatus].
extension AttendanceStatusX on AttendanceStatus {
  /// Số gửi lên API (khớp enum backend).
  int get apiValue => index;

  /// Ký hiệu ngắn trên nút P/A/L.
  String get symbol {
    switch (this) {
      case AttendanceStatus.present:
        return 'P';
      case AttendanceStatus.absent:
        return 'A';
      case AttendanceStatus.late:
        return 'L';
    }
  }

  /// Nhãn tiếng Việt.
  String get label {
    switch (this) {
      case AttendanceStatus.present:
        return 'Có mặt';
      case AttendanceStatus.absent:
        return 'Vắng';
      case AttendanceStatus.late:
        return 'Muộn';
    }
  }

  /// Chuyển chuỗi API (`Present` / `Absent` / `Late`) sang enum.
  static AttendanceStatus fromApi(String? s) {
    switch (s) {
      case 'Absent':
        return AttendanceStatus.absent;
      case 'Late':
        return AttendanceStatus.late;
      case 'Present':
      default:
        return AttendanceStatus.present;
    }
  }
}

/// Một bản ghi điểm danh đã lưu trên server.
class AttendanceModel {
  /// Id bản ghi.
  final int id;

  /// FK → lớp được điểm danh.
  final int classId;

  /// FK → học sinh.
  final int studentId;

  /// Tên học sinh (denormalized).
  final String studentName;

  /// Ngày điểm danh.
  final DateTime date;

  /// Trạng thái P/A/L.
  final AttendanceStatus status;

  AttendanceModel({
    required this.id,
    required this.classId,
    required this.studentId,
    required this.studentName,
    required this.date,
    required this.status,
  });

  /// Parse từ JSON `AttendanceDto`.
  factory AttendanceModel.fromJson(Map<String, dynamic> json) {
    return AttendanceModel(
      id: json['id'] as int? ?? 0,
      classId: json['classId'] as int? ?? 0,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      date: DateTime.tryParse(json['date'] as String? ?? '') ?? DateTime.now(),
      status: AttendanceStatusX.fromApi(json['status'] as String?),
    );
  }
}
