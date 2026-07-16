import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/grade_utils.dart';
import '../controller/grade_controller.dart';
import '../controller/semester_controller.dart';
import '../model/grade_model.dart';

/// Bảng điểm (FR2.3): chọn kỳ → mới tải điểm kỳ đó (có cache).
class GradesView extends StatefulWidget {
  final int? studentId;
  const GradesView({super.key, this.studentId});

  @override
  State<GradesView> createState() => _GradesViewState();
}

class _GradesViewState extends State<GradesView> {
  final _gradesApi = GradeController();
  final _semestersApi = SemesterController();

  List<SemesterItem> _semesters = [];
  int? _semesterId;
  List<GradeModel> _grades = [];
  /// Cache điểm theo kỳ — đổi kỳ đã xem không gọi lại API.
  final Map<int, List<GradeModel>> _cacheBySemester = {};

  bool _bootstrapping = true; // đang tải danh sách kỳ
  bool _loadingGrades = false; // đang tải điểm kỳ đã chọn
  String? _error;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void didUpdateWidget(covariant GradesView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.studentId != widget.studentId) {
      _cacheBySemester.clear();
      _loadGrades(force: true);
    }
  }

  /// Chỉ tải danh sách kỳ → hiện chip ngay; sau đó tải điểm kỳ mặc định.
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
    // Ưu tiên kỳ đang diễn ra → HK1 2026-2027 (kỳ seed) → kỳ sắp tới → kỳ mới nhất.
    _semesterId = _pickDefaultSemester(DateTime.now())?.id;
    setState(() => _bootstrapping = false);
    if (_semesterId != null) await _loadGrades(force: true);
  }

  /// Chọn kỳ mặc định có dữ liệu demo, tránh nhảy sang kỳ xa (vd 2028) lúc nghỉ hè.
  SemesterItem? _pickDefaultSemester(DateTime day) {
    if (_semesters.isEmpty) return null;
    final current = _semesters.where((s) => s.contains(day));
    if (current.isNotEmpty) return current.first;
    final demo =
        _semesters.where((s) => s.name.contains('Học kỳ 1 (2026-2027)'));
    if (demo.isNotEmpty) return demo.first;
    final upcoming = _semesters.where((s) => !s.startDate.isBefore(day)).toList()
      ..sort((a, b) => a.startDate.compareTo(b.startDate));
    if (upcoming.isNotEmpty) return upcoming.first;
    return _semesters.first;
  }

  Future<void> _loadGrades({bool force = false}) async {
    final semId = _semesterId;
    if (semId == null) {
      setState(() {
        _grades = [];
        _loadingGrades = false;
      });
      return;
    }
    if (!force && _cacheBySemester.containsKey(semId)) {
      setState(() {
        _grades = _cacheBySemester[semId]!;
        _error = null;
        _loadingGrades = false;
      });
      return;
    }

    setState(() {
      _loadingGrades = true;
      _error = null;
    });
    final (list, err) = await _gradesApi.getMyGrades(
      semesterId: semId,
      studentId: widget.studentId,
    );
    if (!mounted) return;
    if (list != null) _cacheBySemester[semId] = list;
    setState(() {
      _grades = list ?? [];
      _error = err;
      _loadingGrades = false;
    });
  }

  Future<void> _onSemesterChanged(int id) async {
    if (id == _semesterId) return;
    setState(() => _semesterId = id);
    await _loadGrades(); // dùng cache nếu đã tải kỳ này
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
            const Icon(Icons.error_outline, size: 48, color: AppColors.danger),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _bootstrap,
              child: const Text('Thử lại'),
            ),
          ],
        ),
      );
    }

    final bySubject = groupBySubject(_grades);
    final subjectIds = bySubject.keys.toList()
      ..sort((a, b) => bySubject[a]!
          .first
          .subjectName
          .compareTo(bySubject[b]!.first.subjectName));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (_semesters.isNotEmpty) _buildSemesterChips(),
        Expanded(
          child: _loadingGrades
              ? const Center(child: CircularProgressIndicator())
              : _error != null
                  ? Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(_error!, textAlign: TextAlign.center),
                          const SizedBox(height: 12),
                          ElevatedButton(
                            onPressed: () => _loadGrades(force: true),
                            child: const Text('Thử lại'),
                          ),
                        ],
                      ),
                    )
                  : RefreshIndicator(
                      color: AppColors.primary,
                      onRefresh: () => _loadGrades(force: true),
                      child: subjectIds.isEmpty
                          ? ListView(
                              physics: const AlwaysScrollableScrollPhysics(),
                              children: const [
                                SizedBox(height: 120),
                                Center(
                                  child: Text(
                                      'Chưa có điểm công bố trong kỳ này.'),
                                ),
                              ],
                            )
                          : ListView.builder(
                              padding:
                                  const EdgeInsets.fromLTRB(12, 4, 12, 16),
                              itemCount: subjectIds.length,
                              itemBuilder: (context, i) {
                                final grades = bySubject[subjectIds[i]]!;
                                return _SubjectAvgCard(
                                  grades: grades,
                                  onTap: () => _openDetail(grades),
                                );
                              },
                            ),
                    ),
        ),
      ],
    );
  }

  Widget _buildSemesterChips() {
    return SizedBox(
      height: 48,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
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

  void _openDetail(List<GradeModel> grades) {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => GradeDetailView(grades: grades),
      ),
    );
  }
}

/// Thẻ ngoài: tên môn + lớp + Average (ấn để xem chi tiết).
class _SubjectAvgCard extends StatelessWidget {
  final List<GradeModel> grades;
  final VoidCallback onTap;

  const _SubjectAvgCard({required this.grades, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final first = grades.first;
    final avg = weightedAverage(grades);
    final passed = isPassed(avg);
    final accent = passed ? AppColors.success : AppColors.danger;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Material(
        color: AppColors.white,
        borderRadius: BorderRadius.circular(12),
        elevation: 0.5,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(12),
          child: IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Container(
                  width: 5,
                  decoration: BoxDecoration(
                    color: accent,
                    borderRadius: const BorderRadius.horizontal(
                      left: Radius.circular(12),
                    ),
                  ),
                ),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(12, 12, 12, 12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Text(
                                first.subjectName,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  fontSize: 15,
                                  color: AppColors.textDark,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            _StatusBadge(passed: passed),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Divider(height: 1, color: Colors.grey.shade200),
                        const SizedBox(height: 8),
                        Text(
                          'Lớp: ${first.className.isEmpty ? '—' : first.className}',
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.textGrey,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            const Text(
                              'Average: ',
                              style: TextStyle(
                                fontSize: 14,
                                color: AppColors.textGrey,
                              ),
                            ),
                            Text(
                              avg == null ? '—' : avg.toStringAsFixed(1),
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w800,
                                color: accent,
                              ),
                            ),
                            const Spacer(),
                            Icon(
                              Icons.chevron_right,
                              color: Colors.grey.shade400,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final bool passed;
  const _StatusBadge({required this.passed});

  @override
  Widget build(BuildContext context) {
    final color = passed ? AppColors.success : AppColors.danger;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        passed ? 'Đạt' : 'Chưa đạt',
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

/// Chi tiết đầu điểm form cấp 3.
class GradeDetailView extends StatelessWidget {
  final List<GradeModel> grades;
  const GradeDetailView({super.key, required this.grades});

  @override
  Widget build(BuildContext context) {
    final first = grades.first;
    final byType = {for (final g in grades) g.assessmentType: g};
    final avg = weightedAverage(grades);
    final passed = isPassed(avg);
    final accent = passed ? AppColors.success : AppColors.danger;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Chi tiết điểm'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  first.subjectName,
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 17,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    _StatusBadge(passed: passed),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'Lớp: ${first.className.isEmpty ? '—' : first.className}',
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textGrey,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    const Text(
                      'Average: ',
                      style: TextStyle(fontSize: 15, color: AppColors.textGrey),
                    ),
                    Text(
                      avg == null ? '—' : avg.toStringAsFixed(1),
                      style: TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w800,
                        color: accent,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  first.semesterName,
                  style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Container(
            decoration: BoxDecoration(
              color: AppColors.white,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.grey.shade200),
            ),
            clipBehavior: Clip.antiAlias,
            child: Column(
              children: [
                Container(
                  color: AppColors.primary,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  child: const Row(
                    children: [
                      Expanded(
                        flex: 4,
                        child: Text(
                          'Loại điểm',
                          style: TextStyle(
                            color: AppColors.white,
                            fontWeight: FontWeight.w700,
                            fontSize: 13,
                          ),
                        ),
                      ),
                      Expanded(
                        flex: 2,
                        child: Text(
                          'Hệ số',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: AppColors.white,
                            fontWeight: FontWeight.w700,
                            fontSize: 13,
                          ),
                        ),
                      ),
                      Expanded(
                        flex: 2,
                        child: Text(
                          'Điểm',
                          textAlign: TextAlign.right,
                          style: TextStyle(
                            color: AppColors.white,
                            fontWeight: FontWeight.w700,
                            fontSize: 13,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                for (var i = 0; i < kThptAssessments.length; i++) ...[
                  _DetailRow(
                    label: kThptAssessments[i].label,
                    weight: kThptAssessments[i].weight,
                    score: byType[kThptAssessments[i].type]?.score,
                    striped: i.isOdd,
                  ),
                ],
                Divider(height: 1, color: Colors.grey.shade300),
                _DetailRow(
                  label: 'TB học kỳ',
                  weight: null,
                  score: avg,
                  striped: false,
                  emphasize: true,
                  scoreColor: accent,
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Text(
            'TBHK = Σ (điểm × hệ số) / Σ hệ số — theo sổ điểm THPT.',
            style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
          ),
        ],
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  final String label;
  final int? weight;
  final double? score;
  final bool striped;
  final bool emphasize;
  final Color? scoreColor;

  const _DetailRow({
    required this.label,
    required this.weight,
    required this.score,
    required this.striped,
    this.emphasize = false,
    this.scoreColor,
  });

  @override
  Widget build(BuildContext context) {
    final bg = emphasize
        ? AppColors.primary.withValues(alpha: 0.06)
        : (striped ? Colors.grey.shade50 : AppColors.white);
    final textStyle = TextStyle(
      fontSize: 13,
      fontWeight: emphasize ? FontWeight.w800 : FontWeight.w500,
      color: AppColors.textDark,
    );
    final valueColor = scoreColor ??
        (score == null
            ? AppColors.textGrey
            : (score! >= 5 ? AppColors.success : AppColors.danger));

    return Container(
      color: bg,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 11),
      child: Row(
        children: [
          Expanded(flex: 4, child: Text(label, style: textStyle)),
          Expanded(
            flex: 2,
            child: Text(
              weight?.toString() ?? '—',
              textAlign: TextAlign.center,
              style: textStyle.copyWith(color: AppColors.textGrey),
            ),
          ),
          Expanded(
            flex: 2,
            child: Text(
              score == null ? '—' : score!.toStringAsFixed(1),
              textAlign: TextAlign.right,
              style: textStyle.copyWith(
                color: valueColor,
                fontWeight: FontWeight.w800,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
