// Model dữ liệu Thời khóa biểu (FR2.3) — khớp TimetableDtos.cs bên API .NET.
//
// Gồm 2 lớp:
//   - TimetableSlotModel: 1 tiết học (môn, GV, thứ, tiết, phòng).
//   - WeeklyTimetableModel: cả tuần = khoảng ngày + danh sách tiết.

/// Một tiết học trong thời khóa biểu.
class TimetableSlotModel {
  final int id;
  final int classId;
  final String className; // tên lớp
  final int subjectId;
  final String subjectName; // tên môn
  final String subjectCode; // mã môn (vd "MATH")
  final int teacherId;
  final String teacherName; // tên giáo viên dạy
  final int dayOfWeek; // 1 = Thứ Hai … 7 = Chủ Nhật
  final String dayName; // tên thứ tiếng Việt (server trả sẵn)
  final int period; // tiết thứ mấy trong ngày (1, 2, 3…)
  final String room; // phòng học

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

  /// Tạo [TimetableSlotModel] từ 1 phần tử JSON trong `slots`.
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

/// Thời khóa biểu theo tuần: khoảng ngày + tất cả tiết trong tuần.
class WeeklyTimetableModel {
  final DateTime weekStart; // ngày đầu tuần (Thứ Hai)
  final DateTime weekEnd; // ngày cuối tuần (Chủ Nhật)
  final List<TimetableSlotModel> slots; // tất cả tiết học trong tuần

  WeeklyTimetableModel({
    required this.weekStart,
    required this.weekEnd,
    required this.slots,
  });

  /// Tạo [WeeklyTimetableModel] từ JSON WeeklyTimetableDto.
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

  /// Nhóm các tiết theo thứ (dayOfWeek) → tiện dựng UI từng ngày.
  ///
  /// Trả về `Map<int, List<TimetableSlotModel>>`: key = thứ (1..7),
  /// value = danh sách tiết trong thứ đó, đã sắp xếp theo `period` tăng dần.
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
