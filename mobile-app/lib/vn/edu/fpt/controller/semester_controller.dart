import 'package:dio/dio.dart';
import '../service/api_client.dart';

/// Kỳ học (GET /semesters) — dùng lọc TKB / điểm / lớp GV.
class SemesterItem {
  final int id;
  final String name;
  final DateTime startDate;
  final DateTime endDate;

  SemesterItem({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
  });

  factory SemesterItem.fromJson(Map<String, dynamic> json) => SemesterItem(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        startDate: DateTime.tryParse(json['startDate'] as String? ?? '') ??
            DateTime.now(),
        endDate: DateTime.tryParse(json['endDate'] as String? ?? '') ??
            DateTime.now(),
      );

  /// Kỳ đang diễn ra theo [now] (so ngày địa phương).
  bool contains(DateTime now) {
    final d = DateTime(now.year, now.month, now.day);
    final s = DateTime(startDate.year, startDate.month, startDate.day);
    final e = DateTime(endDate.year, endDate.month, endDate.day);
    return !d.isBefore(s) && !d.isAfter(e);
  }
}

class SemesterController {
  final ApiClient _api = ApiClient();

  /// Cache RAM — tránh gọi /semesters lặp lại trên mọi màn.
  static List<SemesterItem>? _cache;
  static DateTime? _cachedAt;
  static const _ttl = Duration(minutes: 10);

  Future<(List<SemesterItem>?, String?)> list({bool forceRefresh = false}) async {
    if (!forceRefresh &&
        _cache != null &&
        _cachedAt != null &&
        DateTime.now().difference(_cachedAt!) < _ttl) {
      return (List<SemesterItem>.from(_cache!), null);
    }

    try {
      final res = await _api.dio.get('/semesters');
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) => SemesterItem.fromJson(e as Map<String, dynamic>))
            .toList();
        list.sort((a, b) => b.startDate.compareTo(a.startDate));
        _cache = list;
        _cachedAt = DateTime.now();
        return (list, null);
      }
      return (null, 'Không tải được học kỳ.');
    } on DioException catch (e) {
      final data = e.response?.data;
      if (data is Map && data['message'] is String) {
        return (null, data['message'] as String);
      }
      return (null, 'Lỗi kết nối: ${e.message}');
    }
  }
}
