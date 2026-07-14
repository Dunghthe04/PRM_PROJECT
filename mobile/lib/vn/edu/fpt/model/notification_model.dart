/// Model 1 thông báo cá nhân (in-app) — khớp NotificationDto bên API.
/// Dùng cho Trung tâm thông báo (FR1.4).
class NotificationModel {
  final int id;
  final String title; // tiêu đề (vd "Điểm mới")
  final String message; // nội dung
  bool isRead; // đã đọc chưa (KHÔNG final: cập nhật tại chỗ khi đọc)
  final DateTime createdAt; // thời điểm nhận

  NotificationModel({
    required this.id,
    required this.title,
    required this.message,
    required this.isRead,
    required this.createdAt,
  });

  /// factory fromJson: tạo [NotificationModel] từ Map JSON API trả về.
  ///
  /// Nhận: [json] — 1 phần tử trong mảng "items" của /api/notifications.
  /// Trả về: object [NotificationModel].
  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      id: json['id'] as int,
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      isRead: json['isRead'] as bool? ?? false,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}
