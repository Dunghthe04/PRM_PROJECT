import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/attendance_controller.dart';
import '../controller/teacher_controller.dart';
import '../model/attendance_model.dart';
import '../model/teacher_class_model.dart';

/// Màn điểm danh P/A/L theo lớp + ngày (FR3.1, online-only ở Ngày 17).
/// GV chọn ngày → chọn trạng thái từng HS → Lưu.
class AttendanceEntryView extends StatefulWidget {
  final TeacherClassModel teacherClass;
  const AttendanceEntryView({super.key, required this.teacherClass});

  @override
  State<AttendanceEntryView> createState() => _AttendanceEntryViewState();
}

class _AttendanceEntryViewState extends State<AttendanceEntryView> {
  final TeacherController _teacherController = TeacherController();
  final AttendanceController _attendanceController = AttendanceController();

  DateTime _date = DateTime.now();
  List<ClassStudentModel> _roster = [];
  // Trạng thái điểm danh hiện chọn của từng HS.
  final Map<int, AttendanceStatus> _statuses = {};
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadRosterThenAttendance();
  }

  /// Nạp roster (1 lần) rồi nạp điểm danh theo ngày.
  Future<void> _loadRosterThenAttendance() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final (roster, err) =
        await _teacherController.getClassStudents(widget.teacherClass.classId);
    if (!mounted) return;
    if (err != null) {
      setState(() {
        _loading = false;
        _error = err;
      });
      return;
    }
    _roster = roster ?? [];
    await _loadAttendance();
  }

  /// Nạp điểm danh của ngày đang chọn → điền trạng thái (mặc định Có mặt).
  Future<void> _loadAttendance() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final (records, err) = await _attendanceController.getAttendance(
      classId: widget.teacherClass.classId,
      date: _date,
    );
    if (!mounted) return;
    if (err != null) {
      setState(() {
        _loading = false;
        _error = err;
      });
      return;
    }

    // Mặc định tất cả Có mặt; ghi đè bằng bản ghi đã có.
    _statuses.clear();
    for (final s in _roster) {
      _statuses[s.studentId] = AttendanceStatus.present;
    }
    for (final AttendanceModel r in records ?? []) {
      _statuses[r.studentId] = r.status;
    }
    setState(() => _loading = false);
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  /// Chọn ngày điểm danh (không cho chọn ngày tương lai).
  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      setState(() => _date = picked);
      _loadAttendance();
    }
  }

  /// Đặt nhanh tất cả HS về 1 trạng thái.
  void _markAll(AttendanceStatus status) {
    setState(() {
      for (final s in _roster) {
        _statuses[s.studentId] = status;
      }
    });
  }

  /// Lưu điểm danh.
  Future<void> _save() async {
    if (_roster.isEmpty) return;
    setState(() => _saving = true);
    final (ok, msg) = await _attendanceController.submitBatch(
      classId: widget.teacherClass.classId,
      date: _date,
      entries: _statuses,
    );
    if (!mounted) return;
    setState(() => _saving = false);
    _toast(msg);
  }

  @override
  Widget build(BuildContext context) {
    final tc = widget.teacherClass;
    return Scaffold(
      appBar: AppBar(title: Text('Điểm danh • ${tc.className}')),
      body: Column(
        children: [
          // Thanh chọn ngày + đánh dấu nhanh.
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            color: AppColors.primary.withValues(alpha: 0.06),
            child: Row(
              children: [
                const Icon(Icons.calendar_today, size: 18),
                const SizedBox(width: 8),
                Text(FormatUtils.date(_date),
                    style: const TextStyle(fontWeight: FontWeight.w600)),
                TextButton(onPressed: _saving ? null : _pickDate, child: const Text('Đổi ngày')),
                const Spacer(),
                TextButton(
                  onPressed: _saving ? null : () => _markAll(AttendanceStatus.present),
                  child: const Text('Tất cả có mặt'),
                ),
              ],
            ),
          ),
          Expanded(child: _buildBody()),
        ],
      ),
      bottomNavigationBar: _loading
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: _saving ? null : _save,
                    icon: const Icon(Icons.save),
                    label: Text(_saving ? 'Đang lưu...' : 'Lưu điểm danh'),
                  ),
                ),
              ),
            ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, color: AppColors.danger, size: 40),
            const SizedBox(height: 12),
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            ElevatedButton(
                onPressed: _loadRosterThenAttendance,
                child: const Text('Thử lại')),
          ],
        ),
      );
    }
    if (_roster.isEmpty) {
      return const Center(child: Text('Lớp chưa có học sinh.'));
    }

    return ListView.separated(
      padding: const EdgeInsets.all(12),
      itemCount: _roster.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final s = _roster[index];
        final current = _statuses[s.studentId] ?? AttendanceStatus.present;
        return Padding(
          padding: const EdgeInsets.symmetric(vertical: 6),
          child: Row(
            children: [
              Expanded(child: Text('${index + 1}. ${s.fullName}')),
              _statusButtons(s.studentId, current),
            ],
          ),
        );
      },
    );
  }

  /// 3 nút P/A/L cho 1 HS.
  Widget _statusButtons(int studentId, AttendanceStatus current) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: AttendanceStatus.values.map((st) {
        final selected = st == current;
        final color = _colorOf(st);
        return Padding(
          padding: const EdgeInsets.only(left: 6),
          child: InkWell(
            onTap: _saving
                ? null
                : () => setState(() => _statuses[studentId] = st),
            borderRadius: BorderRadius.circular(20),
            child: Container(
              width: 34,
              height: 34,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? color : Colors.transparent,
                shape: BoxShape.circle,
                border: Border.all(color: color),
              ),
              child: Text(
                st.symbol,
                style: TextStyle(
                  color: selected ? Colors.white : color,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
        );
      }).toList(),
    );
  }

  Color _colorOf(AttendanceStatus st) {
    switch (st) {
      case AttendanceStatus.present:
        return AppColors.success;
      case AttendanceStatus.absent:
        return AppColors.danger;
      case AttendanceStatus.late:
        return AppColors.primary;
    }
  }
}
