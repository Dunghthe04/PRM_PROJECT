/// Model thời khóa biểu (FR2.3) — khớp `TimetableDtos` bên API .NET.
///
/// Quan hệ server: `TimetableSlot` → Class, Subject, Teacher (User).
/// App nhận DTO đã flatten (có sẵn tên lớp / môn / GV).

/// Một tiết học trong tuần.
class TimetableSlotModel {
  /// Id bản ghi `TimetableSlot`.
  final int id;

  /// FK → lớp học.
  final int classId;

  /// Tên lớp (denormalized từ server).
  final String className;

  /// FK → môn học.
  final int subjectId;

  /// Tên môn.
  final String subjectName;

  /// Mã môn (vd. TOAN).
  final String subjectCode;

  /// FK → giáo viên đứng lớp.
  final int teacherId;

  /// Tên giáo viên.
  final String teacherName;

  /// Thứ ISO: 1 = Thứ Hai … 7 = Chủ Nhật.
  final int dayOfWeek;

  /// Tên thứ tiếng Việt (server trả sẵn).
  final String dayName;

  /// Số tiết trong ngày (1, 2, 3…).
  final int period;

  /// Phòng học.
  final String room;

  TimetableSlotModel({
    required this.id,
    required this.classId,
    required this.className,
    required this.subjectId,
    required this.subjectName,
    required this.subjectCode,
    required this.teacherId,
    required this.teacherName,
    required this.dayOfWeek,
    required this.dayName,
    required this.period,
    required this.room,
  });

  /// Parse 1 phần tử trong mảng `slots` của response tuần.
  factory TimetableSlotModel.fromJson(Map<String, dynamic> json) {
    return TimetableSlotModel(
      id: json['id'] as int,
      classId: json['classId'] as int? ?? 0,
      className: json['className'] as String? ?? '',
      subjectId: json['subjectId'] as int? ?? 0,
      subjectName: json['subjectName'] as String? ?? '',
      subjectCode: json['subjectCode'] as String? ?? '',
      teacherId: json['teacherId'] as int? ?? 0,
      teacherName: json['teacherName'] as String? ?? '',
      dayOfWeek: json['dayOfWeek'] as int? ?? 1,
      dayName: json['dayName'] as String? ?? '',
      period: json['period'] as int? ?? 0,
      room: json['room'] as String? ?? '',
    );
  }
}

/// Thời khóa biểu theo tuần: khoảng ngày + danh sách tiết.
class WeeklyTimetableModel {
  /// Ngày đầu tuần (Thứ Hai).
  final DateTime weekStart;

  /// Ngày cuối tuần (Chủ Nhật).
  final DateTime weekEnd;

  /// Tất cả tiết trong tuần (nhiều lớp nếu HS/GV có nhiều phân công).
  final List<TimetableSlotModel> slots;

  WeeklyTimetableModel({
    required this.weekStart,
    required this.weekEnd,
    required this.slots,
  });

  /// Parse `WeeklyTimetableDto` từ API.
  factory WeeklyTimetableModel.fromJson(Map<String, dynamic> json) {
    final rawSlots = (json['slots'] as List?) ?? [];
    return WeeklyTimetableModel(
      weekStart: DateTime.tryParse(json['weekStart'] as String? ?? '') ??
          DateTime.now(),
      weekEnd: DateTime.tryParse(json['weekEnd'] as String? ?? '') ??
          DateTime.now(),
      slots: rawSlots
          .map((e) => TimetableSlotModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Nhóm tiết theo thứ → dựng UI từng ngày.
  ///
  /// Key = `dayOfWeek` (1..7); value đã sort theo `period` tăng dần.
  Map<int, List<TimetableSlotModel>> groupByDay() {
    final map = <int, List<TimetableSlotModel>>{};
    for (final slot in slots) {
      map.putIfAbsent(slot.dayOfWeek, () => []).add(slot);
    }
    for (final list in map.values) {
      list.sort((a, b) => a.period.compareTo(b.period));
    }
    return map;
  }
}
