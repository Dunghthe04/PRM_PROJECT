/// Model 1 bảng tin (thông báo chung) — khớp AnnouncementDto bên API .NET.
/// Dùng cho tab Bảng tin (FR2.2).
class AnnouncementModel {
  final int id;
  final String title; // tiêu đề
  final String content; // nội dung đầy đủ
  final String type; // "Global" (toàn trường) | "Class" (theo lớp)
  final int? targetClassId; // id lớp đích (chỉ có khi type = Class)
  final String? targetClassName; // tên lớp đích (để hiển thị)
  final String createdByName; // người đăng
  final DateTime createdAt; // thời điểm đăng

  AnnouncementModel({
    required this.id,
    required this.title,
    required this.content,
    required this.type,
    this.targetClassId,
    this.targetClassName,
    required this.createdByName,
    required this.createdAt,
  });

  /// Tiện ích: có phải thông báo toàn trường không.
  bool get isGlobal => type == 'Global';

  /// factory fromJson: tạo [AnnouncementModel] từ Map JSON API trả về.
  ///
  /// Nhận: [json] — 1 phần tử trong mảng /api/announcements.
  /// Trả về: object [AnnouncementModel] đã điền đủ field.
  factory AnnouncementModel.fromJson(Map<String, dynamic> json) {
    return AnnouncementModel(
      id: json['id'] as int,
      title: json['title'] as String? ?? '',
      content: json['content'] as String? ?? '',
      type: json['type'] as String? ?? 'Global',
      targetClassId: json['targetClassId'] as int?,
      targetClassName: json['targetClassName'] as String?,
      createdByName: json['createdByName'] as String? ?? '',
      // createdAt là chuỗi ISO (vd "2026-07-14T08:00:00") → parse sang DateTime.
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}
