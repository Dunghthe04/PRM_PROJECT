/// Một bảng tin (FR1.4 / FR3.4 / FR5.4) — khớp `AnnouncementDto` bên API.
///
/// Quan hệ server: `Announcement` → CreatedBy (User), TargetClass?, Subject?.
class AnnouncementModel {
  /// Id bảng tin.
  final int id;

  /// Tiêu đề.
  final String title;

  /// Nội dung.
  final String content;

  /// `Global` (toàn trường) | `Class` (theo lớp).
  final String type;

  /// FK → lớp nhận tin — bắt buộc khi type = Class.
  final int? targetClassId;

  /// Tên lớp đích.
  final String? targetClassName;

  /// FK → môn liên quan (GV gửi TB lớp) — tuỳ chọn.
  final int? subjectId;

  /// Tên môn liên quan.
  final String? subjectName;

  /// Tên người đăng.
  final String createdByName;

  /// Thời điểm đăng.
  final DateTime createdAt;

  AnnouncementModel({
    required this.id,
    required this.title,
    required this.content,
    required this.type,
    this.targetClassId,
    this.targetClassName,
    this.subjectId,
    this.subjectName,
    required this.createdByName,
    required this.createdAt,
  });

  /// True nếu bảng tin toàn trường.
  bool get isGlobal => type == 'Global';

  /// Parse từ JSON `AnnouncementDto`.
  factory AnnouncementModel.fromJson(Map<String, dynamic> json) {
    return AnnouncementModel(
      id: json['id'] as int,
      title: json['title'] as String? ?? '',
      content: json['content'] as String? ?? '',
      type: json['type'] as String? ?? 'Global',
      targetClassId: json['targetClassId'] as int?,
      targetClassName: json['targetClassName'] as String?,
      subjectId: json['subjectId'] as int?,
      subjectName: json['subjectName'] as String?,
      createdByName: json['createdByName'] as String? ?? '',
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}
