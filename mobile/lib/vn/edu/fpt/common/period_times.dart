/// Ánh xạ số tiết (Period) → giờ học chuẩn cấp 3 VN (mỗi tiết 45 phút).
/// Backend chỉ lưu Period, chưa có StartTime/EndTime → map phía client.
class PeriodTimes {
  PeriodTimes._();

  static const Map<int, (String start, String end)> _map = {
    1: ('07:00', '07:45'),
    2: ('07:50', '08:35'),
    3: ('08:40', '09:25'),
    4: ('09:40', '10:25'),
    5: ('10:30', '11:15'),
    6: ('11:20', '12:05'),
    7: ('12:50', '13:35'),
    8: ('13:40', '14:25'),
    9: ('14:30', '15:15'),
    10: ('15:20', '16:05'),
    11: ('16:10', '16:55'),
    12: ('17:00', '17:45'),
  };

  static (String start, String end) of(int period) =>
      _map[period] ?? ('—', '—');

  static String label(int period) {
    final (s, e) = of(period);
    return '$s – $e';
  }
}
