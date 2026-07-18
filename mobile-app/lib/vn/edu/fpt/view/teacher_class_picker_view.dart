import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/semester_controller.dart';
import '../controller/teacher_controller.dart';
import '../model/teacher_class_model.dart';
import '../model/user_model.dart';
import 'attendance_entry_view.dart';
import 'teacher_announce_view.dart';
import 'teacher_leave_view.dart';

/// Hành động sau khi GV chọn lớp.
enum TeacherClassAction { attendance, leave, announce }

/// Màn chọn lớp (theo kỳ) rồi mở đúng chức năng — không gom vào lịch dạy.
class TeacherClassPickerPage extends StatefulWidget {
  final UserModel user;
  final TeacherClassAction action;
  const TeacherClassPickerPage({
    super.key,
    required this.user,
    required this.action,
  });

  @override
  State<TeacherClassPickerPage> createState() => _TeacherClassPickerPageState();
}

class _TeacherClassPickerPageState extends State<TeacherClassPickerPage> {
  final _teacherApi = TeacherController();
  final _semestersApi = SemesterController();

  List<TeacherClassModel> _all = [];
  List<SemesterItem> _semesters = [];
  int? _semesterId;
  bool _loading = true;
  String? _error;

  String get _title => switch (widget.action) {
        TeacherClassAction.attendance => 'Điểm danh',
        TeacherClassAction.leave => 'Duyệt đơn nghỉ',
        TeacherClassAction.announce => 'Gửi thông báo',
      };

  String get _hint => switch (widget.action) {
        TeacherClassAction.attendance =>
          'Chỉ lớp bạn là chủ nhiệm — chọn lớp để điểm danh',
        TeacherClassAction.leave =>
          'Chỉ lớp bạn là chủ nhiệm — chọn lớp để duyệt đơn nghỉ',
        TeacherClassAction.announce =>
          'CN: tin chủ nhiệm lớp + tin môn (nếu dạy). Bộ môn: chỉ tin môn mình dạy.',
      };

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final results = await Future.wait([
      _teacherApi.getMyClasses(widget.user.id),
      _semestersApi.list(),
    ]);
    if (!mounted) return;

    final (classes, cErr) = results[0] as (List<TeacherClassModel>?, String?);
    final (semesters, _) = results[1] as (List<SemesterItem>?, String?);

    if (cErr != null) {
      setState(() {
        _loading = false;
        _error = cErr;
      });
      return;
    }

    _all = classes ?? [];
    _semesters = semesters ?? [];
    final now = DateTime.now();
    final current = _semesters.where((s) => s.contains(now));
    final demo =
        _semesters.where((s) => s.name.contains('Học kỳ 1 (2026-2027)'));
    _semesterId = current.isNotEmpty
        ? current.first.id
        : (demo.isNotEmpty
            ? demo.first.id
            : (_semesters.isNotEmpty ? _semesters.first.id : null));
    setState(() => _loading = false);
  }

  List<TeacherClassModel> get _filtered {
    var list = _all;
    if (_semesterId != null) {
      list = list.where((e) => e.semesterId == _semesterId).toList();
    }
    // Điểm danh / duyệt đơn: chỉ lớp chủ nhiệm, 1 dòng / lớp.
    if (widget.action == TeacherClassAction.attendance ||
        widget.action == TeacherClassAction.leave) {
      list = list.where((e) => e.isHomeroom).toList();
      final seen = <int>{};
      final unique = <TeacherClassModel>[];
      for (final e in list) {
        if (seen.add(e.classId)) unique.add(e);
      }
      return unique;
    }

    // Gửi TB: CN có thêm 1 dòng «Tin chủ nhiệm»; mọi dòng môn = Bộ môn.
    if (widget.action == TeacherClassAction.announce) {
      final result = <TeacherClassModel>[];
      final seenHomeroom = <int>{};
      // Tin chủ nhiệm trước (mỗi lớp CN 1 dòng).
      for (final e in list.where((x) => x.isHomeroom)) {
        if (seenHomeroom.add(e.classId)) {
          result.add(TeacherClassModel.homeroomChannel(e));
        }
      }
      // Tin bộ môn theo từng phân công dạy.
      result.addAll(list);
      return result;
    }

    return list;
  }

  void _open(TeacherClassModel item) {
    final page = switch (widget.action) {
      TeacherClassAction.attendance =>
        AttendanceEntryView(teacherClass: item),
      TeacherClassAction.leave => TeacherLeaveReviewPage(
          classId: item.classId,
          className: item.className,
        ),
      TeacherClassAction.announce => TeacherAnnouncePage(teacherClass: item),
    };
    Navigator.push(context, MaterialPageRoute(builder: (_) => page));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(_title),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!,
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppColors.textDark)),
                      const SizedBox(height: 12),
                      ElevatedButton(
                          onPressed: _bootstrap, child: const Text('Thử lại')),
                    ],
                  ),
                )
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _semesterChips(),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
                      child: Text(
                        _hint,
                        style: const TextStyle(
                          color: AppColors.textDark,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    Expanded(
                      child: RefreshIndicator(
                        color: AppColors.primary,
                        onRefresh: _bootstrap,
                        child: _filtered.isEmpty
                            ? ListView(
                                physics: const AlwaysScrollableScrollPhysics(),
                                children: const [
                                  SizedBox(height: 120),
                                  Center(
                                    child: Text(
                                      'Không có lớp trong kỳ này.',
                                      style: TextStyle(
                                          color: AppColors.textDark),
                                    ),
                                  ),
                                ],
                              )
                            : ListView.separated(
                                padding: const EdgeInsets.all(12),
                                itemCount: _filtered.length,
                                separatorBuilder: (_, _) =>
                                    const SizedBox(height: 8),
                                itemBuilder: (context, i) {
                                  final item = _filtered[i];
                                  final isClassOnly =
                                      widget.action == TeacherClassAction.leave ||
                                          widget.action ==
                                              TeacherClassAction.attendance;
                                  final isAnnounce =
                                      widget.action == TeacherClassAction.announce;

                                  // Nhãn kênh gửi TB: tách rõ CN vs bộ môn.
                                  final roleLabel = isAnnounce
                                      ? (item.isHomeroomChannel
                                          ? 'Chủ nhiệm'
                                          : 'Bộ môn')
                                      : (item.isHomeroom
                                          ? 'Chủ nhiệm'
                                          : 'Bộ môn');

                                  final title = isClassOnly
                                      ? item.className
                                      : isAnnounce && item.isHomeroomChannel
                                          ? '${item.className} • Tin chủ nhiệm'
                                          : '${item.className} • ${item.subjectName}';

                                  final subtitle = isClassOnly
                                      ? '${item.semesterName ?? ''}'
                                          '${item.semesterName != null ? ' • ' : ''}$roleLabel'
                                      : isAnnounce && item.isHomeroomChannel
                                          ? '${item.semesterName ?? 'Lớp'} • $roleLabel'
                                          : '${item.subjectName}'
                                              '${item.semesterName != null ? ' • ${item.semesterName}' : ''}'
                                              ' • $roleLabel';

                                  return Card(
                                    color: AppColors.white,
                                    child: ListTile(
                                      leading: CircleAvatar(
                                        backgroundColor: AppColors.primary
                                            .withValues(alpha: 0.15),
                                        child: Icon(
                                          switch (widget.action) {
                                            TeacherClassAction.attendance =>
                                              Icons.checklist,
                                            TeacherClassAction.leave =>
                                              Icons.event_busy,
                                            TeacherClassAction.announce =>
                                              item.isHomeroomChannel
                                                  ? Icons.groups
                                                  : Icons.menu_book,
                                          },
                                          color: AppColors.primary,
                                        ),
                                      ),
                                      title: Text(
                                        title,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.w700,
                                          color: AppColors.textDark,
                                        ),
                                      ),
                                      subtitle: Text(
                                        subtitle,
                                        style: const TextStyle(
                                          color: AppColors.textDark,
                                          fontSize: 13,
                                        ),
                                      ),
                                      trailing: const Icon(
                                        Icons.chevron_right,
                                        color: AppColors.textDark,
                                      ),
                                      onTap: () => _open(item),
                                    ),
                                  );
                                },
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
            onSelected: (_) => setState(() => _semesterId = s.id),
          );
        },
      ),
    );
  }
}
