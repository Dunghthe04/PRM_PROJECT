import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../common/period_times.dart';
import '../controller/semester_controller.dart';
import '../controller/timetable_controller.dart';
import '../model/timetable_model.dart';

/// Thời khóa biểu theo tuần (FR2.3) — chọn kỳ rồi mới tải TKB.
///
/// Luồng nhanh: GET /semesters (nhẹ) → hiện chip → GET /timetable/me?semesterId=
/// (API đã lọc theo kỳ, không gọi thêm /classes).
class TimetableView extends StatefulWidget {
  final int? studentId;
  const TimetableView({super.key, this.studentId});

  @override
  State<TimetableView> createState() => _TimetableViewState();
}

class _TimetableViewState extends State<TimetableView> {
  final _timetable = TimetableController();
  final _semestersApi = SemesterController();

  List<SemesterItem> _semesters = [];
  int? _semesterId;

  /// Cache TKB theo kỳ — đổi tuần trong cùng kỳ không gọi lại API.
  final Map<int, List<TimetableSlotModel>> _slotsBySemester = {};

  late DateTime _weekMonday;
  int _selectedDay = 1;

  WeeklyTimetableModel? _week;
  bool _bootstrapping = true; // đang tải danh sách kỳ
  bool _loadingWeek = false; // đang tải TKB của kỳ đã chọn
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

  @override
  void didUpdateWidget(covariant TimetableView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.studentId != widget.studentId) {
      _slotsBySemester.clear();
      _loadWeek(force: true);
    }
  }

  DateTime _mondayOf(DateTime d) {
    final local = DateTime(d.year, d.month, d.day);
    return local.subtract(Duration(days: local.weekday - 1));
  }

  DateTime _dateOfDay(int dayOfWeek) =>
      _weekMonday.add(Duration(days: dayOfWeek - 1));

  /// Chọn kỳ mặc định: đang diễn ra → demo seed → kỳ sắp tới → kỳ mới nhất.
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

  /// Chỉ tải danh sách kỳ → hiện chip ngay; sau đó tải TKB kỳ mặc định.
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
    final weekDay = _dateOfDay(_selectedDay);
    // Ưu tiên: kỳ chứa tuần hiện tại → kỳ demo có seed → kỳ sắp tới → kỳ mới nhất.
    _semesterId = _pickDefaultSemester(weekDay)?.id;
    if (_semesterId != null) {
      final sem = _semesters.firstWhere((s) => s.id == _semesterId);
      if (!sem.contains(weekDay)) {
        _weekMonday = _mondayOf(sem.startDate);
        _selectedDay = sem.startDate.weekday;
      }
    }
    setState(() => _bootstrapping = false);
    if (_semesterId != null) await _loadWeek(force: true);
  }

  WeeklyTimetableModel _weekFromSlots(List<TimetableSlotModel> slots) {
    return WeeklyTimetableModel(
      weekStart: _weekMonday,
      weekEnd: _weekMonday.add(const Duration(days: 6)),
      slots: slots,
    );
  }

  /// Tải TKB theo kỳ. [force]=true bỏ cache.
  Future<void> _loadWeek({bool force = false}) async {
    final semId = _semesterId;
    if (semId == null) {
      setState(() {
        _week = _weekFromSlots(const []);
        _loadingWeek = false;
      });
      return;
    }
    if (!force && _slotsBySemester.containsKey(semId)) {
      setState(() {
        _week = _weekFromSlots(_slotsBySemester[semId]!);
        _error = null;
        _loadingWeek = false;
      });
      return;
    }

    setState(() {
      _loadingWeek = true;
      _error = null;
    });
    final (week, err) = await _timetable.getMyWeek(
      weekStart: _weekMonday,
      studentId: widget.studentId,
      semesterId: semId,
    );
    if (!mounted) return;
    if (week != null) {
      _slotsBySemester[semId] = week.slots;
    }
    setState(() {
      _week = week == null ? null : _weekFromSlots(week.slots);
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
    await _loadWeek(); // dùng cache nếu đã tải kỳ này
  }

  Future<void> _shiftWeek(int deltaDays) async {
    final prevSem = _semesterId;
    setState(() {
      _weekMonday = _weekMonday.add(Duration(days: deltaDays));
    });
    await _syncSemesterToWeek(reloadWeek: false);
    if (_semesterId == prevSem && _semesterId != null) {
      setState(() {
        _week = _weekFromSlots(
          _slotsBySemester[_semesterId!] ?? _week?.slots ?? const [],
        );
      });
      return;
    }
    await _loadWeek();
  }

  Future<void> _syncSemesterToWeek({bool reloadWeek = true}) async {
    final day = _dateOfDay(_selectedDay);
    final match = _semesters.where((s) => s.contains(day)).toList();
    if (match.isEmpty) {
      setState(() {
        _semesterId = null;
        _week = _weekFromSlots(const []);
      });
      return;
    }
    final sem = match.first;
    if (sem.id == _semesterId) return;
    setState(() => _semesterId = sem.id);
    if (reloadWeek) await _loadWeek();
  }

  List<TimetableSlotModel> get _slotsForSelectedDay {
    final all = _week?.slots ?? [];
    return all.where((s) => s.dayOfWeek == _selectedDay).toList()
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
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            ElevatedButton(onPressed: _bootstrap, child: const Text('Thử lại')),
          ],
        ),
      );
    }

    final slots = _slotsForSelectedDay;
    final monthLabel = 'Tháng ${_weekMonday.month}/${_weekMonday.year}';

    return RefreshIndicator(
      onRefresh: () => _loadWeek(force: true),
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          SliverToBoxAdapter(child: _semesterChips()),
          if (_semesterId == null)
            const SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.fromLTRB(16, 8, 16, 0),
                child: Text(
                  'Đang trong kỳ nghỉ (Tết / hè) — không thuộc Học kỳ 1 hoặc 2. Chọn kỳ bên trên hoặc đổi tuần.',
                  style: TextStyle(color: AppColors.textGrey, fontSize: 13),
                ),
              ),
            ),
          SliverToBoxAdapter(child: _weekRangeLabel()),
          SliverToBoxAdapter(child: _monthNav(monthLabel)),
          SliverToBoxAdapter(child: _dayStrip()),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Text(
                _dayFull[_selectedDay - 1],
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                  color: AppColors.primary,
                ),
              ),
            ),
          ),
          if (_loadingWeek)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(child: CircularProgressIndicator()),
            )
          else if (_error != null && _week == null)
            SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(_error!, textAlign: TextAlign.center),
                    const SizedBox(height: 12),
                    ElevatedButton(
                      onPressed: () => _loadWeek(force: true),
                      child: const Text('Thử lại'),
                    ),
                  ],
                ),
              ),
            )
          else if (slots.isEmpty)
            SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: Text(_semesterId == null
                    ? 'Không có lịch trong kỳ nghỉ.'
                    : 'Ngày này không có tiết học.'),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(12, 0, 12, 24),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, i) => _SlotTile(slot: slots[i]),
                  childCount: slots.length,
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
            label: Text(s.name),
            selected: selected,
            selectedColor: AppColors.primary,
            labelStyle: TextStyle(
              color: selected ? Colors.white : AppColors.textDark,
              fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
              fontSize: 13,
            ),
            onSelected: (_) {
              if (!selected) _onSemesterChanged(s.id);
            },
          );
        },
      ),
    );
  }

  Widget _weekRangeLabel() {
    final end = _weekMonday.add(const Duration(days: 6));
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
      child: Text(
        'Tuần: ${FormatUtils.date(_weekMonday)} – ${FormatUtils.date(end)}',
        style: const TextStyle(color: AppColors.textGrey, fontSize: 13),
      ),
    );
  }

  Widget _monthNav(String monthLabel) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Row(
        children: [
          IconButton(
            tooltip: 'Tuần trước',
            onPressed: _loadingWeek ? null : () => _shiftWeek(-7),
            icon: const Icon(Icons.chevron_left),
          ),
          Expanded(
            child: Text(
              monthLabel,
              textAlign: TextAlign.center,
              style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 15),
            ),
          ),
          IconButton(
            tooltip: 'Tuần sau',
            onPressed: _loadingWeek ? null : () => _shiftWeek(7),
            icon: const Icon(Icons.chevron_right),
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
          final hasSlots =
              (_week?.slots ?? []).any((s) => s.dayOfWeek == day);

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
                      color: selected
                          ? AppColors.primary
                          : AppColors.textGrey,
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
                  Container(
                    width: 5,
                    height: 5,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: hasSlots
                          ? (selected
                              ? AppColors.primary.withValues(alpha: 0.5)
                              : AppColors.primary)
                          : Colors.transparent,
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

/// 1 tiết: cột giờ bên trái + thẻ môn/phòng/GV bên phải.
class _SlotTile extends StatelessWidget {
  final TimetableSlotModel slot;
  const _SlotTile({required this.slot});

  @override
  Widget build(BuildContext context) {
    final (start, end) = PeriodTimes.of(slot.period);
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            SizedBox(
              width: 64,
              child: Column(
                children: [
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      'Tiết ${slot.period}',
                      style: const TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.primary,
                      ),
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(start,
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, fontSize: 13)),
                  Container(
                    width: 2,
                    height: 16,
                    margin: const EdgeInsets.symmetric(vertical: 2),
                    color: AppColors.primary.withValues(alpha: 0.35),
                  ),
                  Text(end,
                      style: const TextStyle(
                          fontSize: 13, color: AppColors.textGrey)),
                ],
              ),
            ),
            Container(
              width: 3,
              margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Expanded(
              child: Card(
                margin: EdgeInsets.zero,
                elevation: 0,
                color: const Color(0xFFF5F5F5),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (slot.room.isNotEmpty)
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 3),
                          margin: const EdgeInsets.only(bottom: 6),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            'Phòng ${slot.room}',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      Text(
                        slot.subjectName,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 15,
                        ),
                      ),
                      if (slot.subjectCode.isNotEmpty)
                        Text(
                          slot.subjectCode,
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textGrey,
                          ),
                        ),
                      const SizedBox(height: 4),
                      Text(
                        'GV: ${slot.teacherName}',
                        style: const TextStyle(fontSize: 13),
                      ),
                      if (slot.className.isNotEmpty)
                        Text(
                          'Lớp ${slot.className}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textGrey,
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
