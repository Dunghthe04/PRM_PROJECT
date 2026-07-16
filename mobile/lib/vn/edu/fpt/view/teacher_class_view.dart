import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../common/period_times.dart';
import '../controller/semester_controller.dart';
import '../controller/timetable_controller.dart';
import '../model/timetable_model.dart';
import '../model/user_model.dart';

/// Tab "Lịch dạy" cho Giáo viên — xem lịch theo tuần (chỉ lịch).
/// Điểm danh / Duyệt đơn / Gửi TB mở từ Trang chủ → màn riêng.
class TeacherClassTab extends StatefulWidget {
  final UserModel user;
  const TeacherClassTab({super.key, required this.user});

  @override
  State<TeacherClassTab> createState() => _TeacherClassTabState();
}

class _TeacherClassTabState extends State<TeacherClassTab> {
  final _timetable = TimetableController();
  final _semestersApi = SemesterController();

  List<SemesterItem> _semesters = [];
  int? _semesterId;

  /// Cache slot theo kỳ — đổi tuần cùng kỳ không gọi lại API.
  final Map<int, List<TimetableSlotModel>> _slotsBySemester = {};

  late DateTime _weekMonday;
  int _selectedDay = 1;

  List<TimetableSlotModel> _slots = [];
  bool _bootstrapping = true;
  bool _loadingWeek = false;
  String? _error;

  static const _dayShort = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  static const _dayFull = [
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
    final now = DateTime.now();
    _weekMonday = _mondayOf(now);
    _selectedDay = now.weekday;
    _bootstrap();
  }

  DateTime _mondayOf(DateTime d) {
    final local = DateTime(d.year, d.month, d.day);
    return local.subtract(Duration(days: local.weekday - 1));
  }

  DateTime _dateOfDay(int dayOfWeek) =>
      _weekMonday.add(Duration(days: dayOfWeek - 1));

  SemesterItem? _pickDefaultSemester(DateTime day) {
    if (_semesters.isEmpty) return null;
    final inWeek = _semesters.where((s) => s.contains(day));
    if (inWeek.isNotEmpty) return inWeek.first;
    final demo =
        _semesters.where((s) => s.name.contains('Học kỳ 1 (2026-2027)'));
    if (demo.isNotEmpty) return demo.first;
    final upcoming = _semesters.where((s) => !s.startDate.isBefore(day)).toList()
      ..sort((a, b) => a.startDate.compareTo(b.startDate));
    if (upcoming.isNotEmpty) return upcoming.first;
    return _semesters.first;
  }

  Future<void> _bootstrap() async {
    setState(() {
      _bootstrapping = true;
      _error = null;
    });
    final (list, err) = await _semestersApi.list();
    if (!mounted) return;
    if (err != null) {
      setState(() {
        _bootstrapping = false;
        _error = err;
      });
      return;
    }
    _semesters = list ?? [];
    final day = _dateOfDay(_selectedDay);
    final picked = _pickDefaultSemester(day);
    _semesterId = picked?.id;
    if (picked != null && !picked.contains(day)) {
      _weekMonday = _mondayOf(picked.startDate);
      _selectedDay = picked.startDate.weekday;
    }
    setState(() => _bootstrapping = false);
    if (_semesterId != null) await _loadWeek(force: true);
  }

  Future<void> _loadWeek({bool force = false}) async {
    final semId = _semesterId;
    if (semId == null) {
      setState(() {
        _slots = [];
        _loadingWeek = false;
      });
      return;
    }
    if (!force && _slotsBySemester.containsKey(semId)) {
      setState(() {
        _slots = _slotsBySemester[semId]!;
        _error = null;
        _loadingWeek = false;
      });
      return;
    }

    setState(() {
      _loadingWeek = true;
      _error = null;
    });
    final (week, err) = await _timetable.getTeacherWeek(
      weekStart: _weekMonday,
      semesterId: semId,
    );
    if (!mounted) return;
    final slots = week?.slots ?? [];
    if (week != null) _slotsBySemester[semId] = slots;
    setState(() {
      _slots = slots;
      _error = err;
      _loadingWeek = false;
    });
  }

  Future<void> _onSemesterChanged(int id) async {
    if (id == _semesterId) return;
    final sem = _semesters.where((s) => s.id == id).firstOrNull;
    setState(() => _semesterId = id);
    if (sem != null) {
      final now = DateTime.now();
      final anchor = sem.contains(now) ? now : sem.startDate;
      _weekMonday = _mondayOf(anchor);
      _selectedDay = anchor.weekday;
    }
    await _loadWeek();
  }

  Future<void> _shiftWeek(int deltaDays) async {
    setState(() {
      _weekMonday = _weekMonday.add(Duration(days: deltaDays));
    });
    // Cùng kỳ → tái dùng cache slot (TKB lặp theo tuần).
    await _loadWeek();
  }

  List<TimetableSlotModel> get _slotsToday {
    return _slots.where((s) => s.dayOfWeek == _selectedDay).toList()
      ..sort((a, b) => a.period.compareTo(b.period));
  }

  @override
  Widget build(BuildContext context) {
    if (_bootstrapping) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null && _semesters.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textDark)),
            const SizedBox(height: 12),
            ElevatedButton(onPressed: _bootstrap, child: const Text('Thử lại')),
          ],
        ),
      );
    }

    final daySlots = _slotsToday;
    final end = _weekMonday.add(const Duration(days: 6));

    return RefreshIndicator(
      color: AppColors.primary,
      onRefresh: () => _loadWeek(force: true),
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          SliverToBoxAdapter(child: _semesterChips()),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
              child: Text(
                'Tuần: ${FormatUtils.date(_weekMonday)} – ${FormatUtils.date(end)}',
                style: const TextStyle(
                  color: AppColors.textDark,
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ),
          SliverToBoxAdapter(child: _weekNav()),
          SliverToBoxAdapter(child: _dayStrip()),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      _dayFull[_selectedDay - 1],
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                        color: AppColors.primary,
                      ),
                    ),
                  ),
                  Text(
                    daySlots.isEmpty
                        ? 'Không có ca'
                        : '${daySlots.length} ca dạy',
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textDark,
                    ),
                  ),
                ],
              ),
            ),
          ),
          if (_loadingWeek)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(child: CircularProgressIndicator()),
            )
          else if (_error != null)
            SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(_error!,
                        textAlign: TextAlign.center,
                        style: const TextStyle(color: AppColors.textDark)),
                    const SizedBox(height: 12),
                    ElevatedButton(
                      onPressed: () => _loadWeek(force: true),
                      child: const Text('Thử lại'),
                    ),
                  ],
                ),
              ),
            )
          else if (daySlots.isEmpty)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: Text(
                  'Ngày này bạn không có tiết dạy.',
                  style: TextStyle(color: AppColors.textDark, fontSize: 15),
                ),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, i) => _TeachingSlotCard(slot: daySlots[i]),
                  childCount: daySlots.length,
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _semesterChips() {
    if (_semesters.isEmpty) return const SizedBox.shrink();
    return SizedBox(
      height: 48,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 0),
        itemCount: _semesters.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, i) {
          final s = _semesters[i];
          final selected = s.id == _semesterId;
          return ChoiceChip(
            label: Text(
              s.name,
              style: TextStyle(
                fontSize: 12,
                color: selected ? AppColors.white : AppColors.textDark,
                fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            selected: selected,
            selectedColor: AppColors.primary,
            backgroundColor: AppColors.white,
            side: BorderSide(
              color: selected ? AppColors.primary : Colors.grey.shade300,
            ),
            onSelected: (_) {
              if (!selected) _onSemesterChanged(s.id);
            },
          );
        },
      ),
    );
  }

  Widget _weekNav() {
    final label = 'Tháng ${_weekMonday.month}/${_weekMonday.year}';
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Row(
        children: [
          IconButton(
            tooltip: 'Tuần trước',
            onPressed: _loadingWeek ? null : () => _shiftWeek(-7),
            icon: const Icon(Icons.chevron_left, color: AppColors.textDark),
          ),
          Expanded(
            child: Text(
              label,
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontWeight: FontWeight.w700,
                fontSize: 15,
                color: AppColors.textDark,
              ),
            ),
          ),
          IconButton(
            tooltip: 'Tuần sau',
            onPressed: _loadingWeek ? null : () => _shiftWeek(7),
            icon: const Icon(Icons.chevron_right, color: AppColors.textDark),
          ),
        ],
      ),
    );
  }

  Widget _dayStrip() {
    final today = DateTime.now();
    final todayDate = DateTime(today.year, today.month, today.day);

    return SizedBox(
      height: 72,
      child: Row(
        children: List.generate(7, (i) {
          final day = i + 1;
          final date = _dateOfDay(day);
          final selected = day == _selectedDay;
          final isToday = date == todayDate;
          final count = _slots.where((s) => s.dayOfWeek == day).length;

          return Expanded(
            child: InkWell(
              borderRadius: BorderRadius.circular(12),
              onTap: () => setState(() => _selectedDay = day),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    _dayShort[i],
                    style: TextStyle(
                      fontSize: 11,
                      color: selected ? AppColors.primary : AppColors.textDark,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Container(
                    width: 34,
                    height: 34,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: selected ? AppColors.primary : Colors.transparent,
                      shape: BoxShape.circle,
                      border: isToday && !selected
                          ? Border.all(color: AppColors.primary)
                          : null,
                    ),
                    child: Text(
                      '${date.day}',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        color: selected ? Colors.white : AppColors.textDark,
                      ),
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    count > 0 ? '$count' : '',
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w700,
                      color: selected
                          ? AppColors.primary.withValues(alpha: 0.7)
                          : AppColors.primary,
                    ),
                  ),
                ],
              ),
            ),
          );
        }),
      ),
    );
  }
}

/// 1 ca dạy: giờ + lớp/môn/phòng (chỉ xem lịch — hành động ở màn riêng).
class _TeachingSlotCard extends StatelessWidget {
  final TimetableSlotModel slot;
  const _TeachingSlotCard({required this.slot});

  @override
  Widget build(BuildContext context) {
    final (start, end) = PeriodTimes.of(slot.period);
    return Card(
      color: AppColors.white,
      margin: const EdgeInsets.only(bottom: 10),
      elevation: 0.5,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 56,
              padding: const EdgeInsets.symmetric(vertical: 6),
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Column(
                children: [
                  Text(
                    'Tiết ${slot.period}',
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(start,
                      style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: AppColors.textDark)),
                  Text(end,
                      style: const TextStyle(
                          fontSize: 11, color: AppColors.textDark)),
                ],
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${slot.className} • ${slot.subjectName}',
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 15,
                      color: AppColors.textDark,
                    ),
                  ),
                  if (slot.subjectCode.isNotEmpty)
                    Text(
                      slot.subjectCode,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textDark,
                      ),
                    ),
                  if (slot.room.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(
                      'Phòng ${slot.room}',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textDark,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
