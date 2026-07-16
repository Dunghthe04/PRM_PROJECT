/// Model 1 bảng tin — khớp AnnouncementDto bên API .NET.
class AnnouncementModel {
  final int id;
  final String title;
  final String content;
  final String type; // Global | Class
  final int? targetClassId;
  final String? targetClassName;
  final int? subjectId;
  final String? subjectName;
  final String createdByName;
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

  bool get isGlobal => type == 'Global';

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
