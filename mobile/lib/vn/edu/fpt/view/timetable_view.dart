import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/timetable_controller.dart';
import '../model/timetable_model.dart';

/// Màn Thời khóa biểu theo tuần (FR2.3).
///
/// Dùng cho cả Học sinh (xem của mình) và Phụ huynh (xem của con đang chọn).
/// - [studentId]: null nếu là HS; là id con nếu là PH (Switch Profile).
///   Khi PH đổi con, widget cha truyền studentId mới → didUpdateWidget nạp lại.
class TimetableView extends StatefulWidget {
  final int? studentId;
  const TimetableView({super.key, this.studentId});

  @override
  State<TimetableView> createState() => _TimetableViewState();
}

class _TimetableViewState extends State<TimetableView> {
  final TimetableController _controller = TimetableController();

  // Ngày bất kỳ trong tuần đang xem (mặc định hôm nay → tuần hiện tại).
  DateTime _anchor = DateTime.now();

  late Future<(WeeklyTimetableModel?, String?)> _future;

  // Tên các thứ để hiển thị theo đúng thứ tự (index 1..7).
  static const List<String> _dayNames = [
    '',
    'Thứ Hai',
    'Thứ Ba',
    'Thứ Tư',
    'Thứ Năm',
    'Thứ Sáu',
    'Thứ Bảy',
    'Chủ Nhật',
  ];

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  // Khi widget cha rebuild với studentId khác (PH đổi con) → nạp lại TKB.
  @override
  void didUpdateWidget(covariant TimetableView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.studentId != widget.studentId) {
      setState(() => _future = _load());
    }
  }

  /// Gọi API lấy TKB tuần theo _anchor + studentId hiện tại.
  Future<(WeeklyTimetableModel?, String?)> _load() {
    return _controller.getMyWeek(
      weekStart: _anchor,
      studentId: widget.studentId,
    );
  }

  /// Đổi tuần xem: [deltaDays] = -7 (tuần trước) hoặc +7 (tuần sau).
  void _shiftWeek(int deltaDays) {
    setState(() {
      _anchor = _anchor.add(Duration(days: deltaDays));
      _future = _load();
    });
  }

  /// Kéo để làm mới tuần hiện tại.
  Future<void> _reload() async {
    setState(() => _future = _load());
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<(WeeklyTimetableModel?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }

        final (week, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');

        if (error != null) {
          return _CenteredMessage(
            icon: Icons.error_outline,
            color: AppColors.danger,
            message: error,
            onRetry: _reload,
          );
        }

        return Column(
          children: [
            _buildWeekHeader(week),
            Expanded(child: _buildWeekBody(week)),
          ],
        );
      },
    );
  }

  /// Thanh điều hướng tuần: ◀  (khoảng ngày)  ▶.
  Widget _buildWeekHeader(WeeklyTimetableModel? week) {
    final label = week == null
        ? ''
        : '${FormatUtils.date(week.weekStart)} - ${FormatUtils.date(week.weekEnd)}';
    return Container(
      color: AppColors.primary.withValues(alpha: 0.08),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: () => _shiftWeek(-7),
            tooltip: 'Tuần trước',
          ),
          Text(
            label,
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: () => _shiftWeek(7),
            tooltip: 'Tuần sau',
          ),
        ],
      ),
    );
  }

  /// Danh sách tiết học nhóm theo từng thứ trong tuần.
  Widget _buildWeekBody(WeeklyTimetableModel? week) {
    final grouped = week?.groupByDay() ?? {};

    if (grouped.isEmpty) {
      // Vẫn cho kéo làm mới dù rỗng.
      return RefreshIndicator(
        onRefresh: _reload,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Center(child: Text('Tuần này chưa có tiết học nào.')),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _reload,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        // Duyệt thứ 1..7; chỉ vẽ thứ nào có tiết.
        itemCount: 7,
        itemBuilder: (context, index) {
          final day = index + 1; // 1..7
          final slots = grouped[day];
          if (slots == null || slots.isEmpty) return const SizedBox.shrink();
          return _buildDaySection(day, slots);
        },
      ),
    );
  }

  /// 1 khối cho 1 thứ: tiêu đề thứ + các tiết trong thứ đó.
  Widget _buildDaySection(int day, List<TimetableSlotModel> slots) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 8, bottom: 4, left: 4),
          child: Text(
            _dayNames[day],
            style: const TextStyle(
              fontWeight: FontWeight.bold,
              color: AppColors.primary,
              fontSize: 15,
            ),
          ),
        ),
        ...slots.map((s) => _buildSlotCard(s)),
      ],
    );
  }

  /// 1 thẻ tiết học: tiết số + môn + GV + phòng.
  Widget _buildSlotCard(TimetableSlotModel s) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 3),
      child: ListTile(
        // "Ô" số tiết bên trái.
        leading: CircleAvatar(
          backgroundColor: AppColors.primary,
          child: Text(
            'T${s.period}',
            style: const TextStyle(
              color: AppColors.white,
              fontWeight: FontWeight.bold,
              fontSize: 13,
            ),
          ),
        ),
        title: Text(
          s.subjectName,
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        subtitle: Text('GV: ${s.teacherName}'),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            const Icon(Icons.room, size: 16, color: AppColors.textGrey),
            Text(
              s.room.isEmpty ? '—' : s.room,
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
          ],
        ),
      ),
    );
  }
}

/// Widget hiển thị 1 thông điệp ở giữa màn + (tùy chọn) nút thử lại.
class _CenteredMessage extends StatelessWidget {
  final IconData icon;
  final Color color;
  final String message;
  final Future<void> Function()? onRetry;
  const _CenteredMessage({
    required this.icon,
    required this.color,
    required this.message,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 48, color: color),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Text(message, textAlign: TextAlign.center),
          ),
          if (onRetry != null) ...[
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: onRetry,
              icon: const Icon(Icons.refresh),
              label: const Text('Thử lại'),
            ),
          ],
        ],
      ),
    );
  }
}
