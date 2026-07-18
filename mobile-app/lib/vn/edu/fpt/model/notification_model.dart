/// Thông báo in-app cá nhân (FR1.4) — khớp `NotificationDto` bên API.
///
/// Quan hệ server: N `Notification` → 1 `User` (người nhận).
class NotificationModel {
  /// Id thông báo.
  final int id;

  /// Tiêu đề ngắn (vd. "Điểm mới").
  final String title;

  /// Nội dung chi tiết.
  final String message;

  /// Đã đọc chưa — không `final` để cập nhật tại chỗ khi user mở tin.
  bool isRead;

  /// Thời điểm nhận.
  final DateTime createdAt;

  NotificationModel({
    required this.id,
    required this.title,
    required this.message,
    required this.isRead,
    required this.createdAt,
  });

  /// Parse 1 phần tử trong danh sách `/api/notifications`.
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
