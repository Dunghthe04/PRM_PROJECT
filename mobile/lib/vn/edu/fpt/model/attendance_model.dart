// Model điểm danh (FR3.1) — khớp AttendanceDto bên API (.NET).
// Trạng thái P/A/L: request body gửi SỐ (0/1/2); response trả CHUỖI.

/// 3 trạng thái điểm danh. Giá trị số khớp enum AttendanceStatus bên backend.
enum AttendanceStatus {
  present, // 0 - P (Có mặt)
  absent, // 1 - A (Vắng)
  late, // 2 - L (Muộn)
}

extension AttendanceStatusX on AttendanceStatus {
  /// Số gửi lên API (khớp enum backend).
  int get apiValue => index;

  /// Ký hiệu ngắn hiển thị nút P/A/L.
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

  /// Chuyển chuỗi từ API ("Present"/"Absent"/"Late") sang enum.
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
  final int id;
  final int classId;
  final int studentId;
  final String studentName;
  final DateTime date;
  final AttendanceStatus status;

  AttendanceModel({
    required this.id,
    required this.classId,
    required this.studentId,
    required this.studentName,
    required this.date,
    required this.status,
  });

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
