import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/assignment_controller.dart';
import '../model/assignment_model.dart';
import '../model/teacher_class_model.dart';

/// Màn quản lý bài tập của GV cho 1 lớp+môn (FR3.5).
/// Danh sách → tạo/sửa/xóa → xem bài nộp → chấm điểm + feedback.
class TeacherAssignmentListPage extends StatefulWidget {
  final TeacherClassModel teacherClass;
  const TeacherAssignmentListPage({super.key, required this.teacherClass});

  @override
  State<TeacherAssignmentListPage> createState() =>
      _TeacherAssignmentListPageState();
}

class _TeacherAssignmentListPageState extends State<TeacherAssignmentListPage> {
  final AssignmentController _controller = AssignmentController();
  late Future<(List<AssignmentModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<(List<AssignmentModel>?, String?)> _load() =>
      _controller.getClassAssignments(
        classId: widget.teacherClass.classId,
        subjectId: widget.teacherClass.subjectId,
      );

  Future<void> _reload() async {
    setState(() => _future = _load());
    await _future;
  }

  /// Mở form tạo/sửa; nếu lưu thành công thì tải lại DS.
  Future<void> _openForm({AssignmentModel? edit}) async {
    final saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _AssignmentFormPage(
          teacherClass: widget.teacherClass,
          edit: edit,
        ),
      ),
    );
    if (saved == true) _reload();
  }

  Future<void> _confirmDelete(AssignmentModel item) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xóa bài tập?'),
        content: Text('Xóa "${item.title}"? Không hoàn tác được.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Hủy')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Xóa')),
        ],
      ),
    );
    if (ok != true) return;
    final (success, msg) = await _controller.delete(item.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) _reload();
  }

  @override
  Widget build(BuildContext context) {
    final tc = widget.teacherClass;
    return Scaffold(
      appBar: AppBar(
        title: Text('Bài tập • ${tc.className}'),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _openForm(),
        icon: const Icon(Icons.add),
        label: const Text('Tạo bài tập'),
      ),
      body: FutureBuilder<(List<AssignmentModel>?, String?)>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          final (list, error) =
              snapshot.data ?? (null, 'Không tải được dữ liệu.');
          if (error != null) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(error, textAlign: TextAlign.center),
                  const SizedBox(height: 12),
                  ElevatedButton(
                      onPressed: _reload, child: const Text('Thử lại')),
                ],
              ),
            );
          }
          final items = list ?? [];
          if (items.isEmpty) {
            return RefreshIndicator(
              onRefresh: _reload,
              child: ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có bài tập nào. Bấm + để tạo.')),
                ],
              ),
            );
          }
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 80),
              itemCount: items.length,
              separatorBuilder: (_, _) => const SizedBox(height: 8),
              itemBuilder: (context, i) {
                final a = items[i];
                return Card(
                  child: ListTile(
                    title: Text(a.title,
                        style: const TextStyle(fontWeight: FontWeight.bold)),
                    subtitle: Text(
                      'Hạn: ${FormatUtils.dateTime(a.dueDate)}\n'
                      'Đã nộp: ${a.submissionCount} • Điểm tối đa: ${a.maxScore}',
                    ),
                    isThreeLine: true,
                    trailing: PopupMenuButton<String>(
                      onSelected: (v) {
                        if (v == 'edit') _openForm(edit: a);
                        if (v == 'delete') _confirmDelete(a);
                        if (v == 'subs') {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => _SubmissionsPage(assignment: a),
                            ),
                          );
                        }
                      },
                      itemBuilder: (_) => const [
                        PopupMenuItem(value: 'subs', child: Text('Xem bài nộp')),
                        PopupMenuItem(value: 'edit', child: Text('Sửa')),
                        PopupMenuItem(value: 'delete', child: Text('Xóa')),
                      ],
                    ),
                    onTap: () => Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => _SubmissionsPage(assignment: a),
                      ),
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}

/// Form tạo / sửa bài tập.
class _AssignmentFormPage extends StatefulWidget {
  final TeacherClassModel teacherClass;
  final AssignmentModel? edit;
  const _AssignmentFormPage({required this.teacherClass, this.edit});

  @override
  State<_AssignmentFormPage> createState() => _AssignmentFormPageState();
}

class _AssignmentFormPageState extends State<_AssignmentFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _controller = AssignmentController();
  late final TextEditingController _title;
  late final TextEditingController _desc;
  late final TextEditingController _maxScore;
  late DateTime _dueDate;
  bool _saving = false;

  bool get _isEdit => widget.edit != null;

  @override
  void initState() {
    super.initState();
    final e = widget.edit;
    _title = TextEditingController(text: e?.title ?? '');
    _desc = TextEditingController(text: e?.description ?? '');
    _maxScore = TextEditingController(
        text: (e?.maxScore ?? 10).toString().replaceAll(RegExp(r'\.0$'), ''));
    _dueDate = e?.dueDate ?? DateTime.now().add(const Duration(days: 7));
  }

  @override
  void dispose() {
    _title.dispose();
    _desc.dispose();
    _maxScore.dispose();
    super.dispose();
  }

  Future<void> _pickDueDate() async {
    final date = await showDatePicker(
      context: context,
      initialDate: _dueDate,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(_dueDate),
    );
    if (time == null) return;
    setState(() {
      _dueDate = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    });
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final max = double.tryParse(_maxScore.text.trim()) ?? 10;
    final tc = widget.teacherClass;
    final (item, err) = _isEdit
        ? await _controller.update(
            id: widget.edit!.id,
            classId: tc.classId,
            subjectId: tc.subjectId,
            title: _title.text.trim(),
            description: _desc.text.trim(),
            dueDate: _dueDate,
            maxScore: max,
          )
        : await _controller.create(
            classId: tc.classId,
            subjectId: tc.subjectId,
            title: _title.text.trim(),
            description: _desc.text.trim(),
            dueDate: _dueDate,
            maxScore: max,
          );
    if (!mounted) return;
    setState(() => _saving = false);
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(_isEdit ? 'Đã cập nhật bài tập.' : 'Đã tạo bài tập.')),
    );
    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? 'Sửa bài tập' : 'Tạo bài tập')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              '${widget.teacherClass.className} • ${widget.teacherClass.subjectName}',
              style: const TextStyle(color: AppColors.textGrey),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _title,
              decoration: const InputDecoration(
                labelText: 'Tiêu đề *',
                border: OutlineInputBorder(),
              ),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Nhập tiêu đề' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _desc,
              maxLines: 5,
              decoration: const InputDecoration(
                labelText: 'Đề bài / mô tả *',
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
              ),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Nhập mô tả' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _maxScore,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Điểm tối đa',
                border: OutlineInputBorder(),
              ),
              validator: (v) {
                final n = double.tryParse(v?.trim() ?? '');
                if (n == null || n <= 0) return 'Điểm phải > 0';
                return null;
              },
            ),
            const SizedBox(height: 12),
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Hạn nộp'),
              subtitle: Text(FormatUtils.dateTime(_dueDate)),
              trailing: OutlinedButton(
                onPressed: _pickDueDate,
                child: const Text('Chọn'),
              ),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton(
                onPressed: _saving ? null : _save,
                child: _saving
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(_isEdit ? 'Lưu thay đổi' : 'Tạo bài tập'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Danh sách bài nộp + chấm điểm (FR3.5).
class _SubmissionsPage extends StatefulWidget {
  final AssignmentModel assignment;
  const _SubmissionsPage({required this.assignment});

  @override
  State<_SubmissionsPage> createState() => _SubmissionsPageState();
}

class _SubmissionsPageState extends State<_SubmissionsPage> {
  final AssignmentController _controller = AssignmentController();
  late Future<(List<SubmissionModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getSubmissions(widget.assignment.id);
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getSubmissions(widget.assignment.id));
    await _future;
  }

  Future<void> _grade(SubmissionModel sub) async {
    final scoreCtrl = TextEditingController(
      text: sub.score?.toString().replaceAll(RegExp(r'\.0$'), '') ?? '',
    );
    final fbCtrl = TextEditingController(text: sub.feedback ?? '');
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Chấm: ${sub.studentName}'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: scoreCtrl,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(
                labelText: 'Điểm (tối đa ${widget.assignment.maxScore})',
                border: const OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: fbCtrl,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'Nhận xét',
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Hủy')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Lưu')),
        ],
      ),
    );
    if (ok != true) return;
    final score = double.tryParse(scoreCtrl.text.trim());
    if (score == null) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Điểm không hợp lệ.')),
      );
      return;
    }
    final (result, err) = await _controller.gradeSubmission(
      submissionId: sub.id,
      score: score,
      feedback: fbCtrl.text.trim().isEmpty ? null : fbCtrl.text.trim(),
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
          content: Text(
              'Đã chấm ${result?.studentName ?? sub.studentName}: $score')),
    );
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    final a = widget.assignment;
    return Scaffold(
      appBar: AppBar(title: Text('Bài nộp • ${a.title}')),
      body: FutureBuilder<(List<SubmissionModel>?, String?)>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }
          final (list, error) =
              snapshot.data ?? (null, 'Không tải được bài nộp.');
          if (error != null) {
            return Center(child: Text(error));
          }
          final items = list ?? [];
          if (items.isEmpty) {
            return RefreshIndicator(
              onRefresh: _reload,
              child: ListView(
                children: const [
                  SizedBox(height: 120),
                  Center(child: Text('Chưa có học sinh nào nộp bài.')),
                ],
              ),
            );
          }
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView.separated(
              padding: const EdgeInsets.all(12),
              itemCount: items.length,
              separatorBuilder: (_, _) => const SizedBox(height: 8),
              itemBuilder: (context, i) {
                final s = items[i];
                return Card(
                  child: ListTile(
                    title: Text(s.studentName,
                        style: const TextStyle(fontWeight: FontWeight.bold)),
                    subtitle: Text(
                      'Nộp: ${FormatUtils.dateTime(s.submittedAt)}'
                      '${s.isLate ? " • Muộn" : ""}\n'
                      '${s.isGraded ? "Điểm: ${s.score}" : "Chưa chấm"}'
                      '${s.linkUrl != null ? "\nLink: ${s.linkUrl}" : ""}'
                      '${s.fileUrl != null ? "\nFile: ${s.fileUrl}" : ""}',
                    ),
                    isThreeLine: true,
                    trailing: ElevatedButton(
                      onPressed: () => _grade(s),
                      child: Text(s.isGraded ? 'Sửa' : 'Chấm'),
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
