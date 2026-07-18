import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/announcement_controller.dart';
import '../model/teacher_class_model.dart';

/// Màn GV soạn & gửi thông báo / push cho 1 lớp (FR3.4).
/// Gọi POST /announcements với type=Class + targetClassId (luôn gửi push).
class TeacherAnnouncePage extends StatefulWidget {
  final TeacherClassModel teacherClass;
  const TeacherAnnouncePage({super.key, required this.teacherClass});

  @override
  State<TeacherAnnouncePage> createState() => _TeacherAnnouncePageState();
}

class _TeacherAnnouncePageState extends State<TeacherAnnouncePage> {
  final _formKey = GlobalKey<FormState>();
  final _controller = AnnouncementController();
  final _title = TextEditingController();
  final _content = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _title.dispose();
    _content.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final tc = widget.teacherClass;
    // Tin chủ nhiệm: không gửi subjectId. Tin bộ môn: gắn môn dạy.
    final (count, err) = await _controller.create(
      title: _title.text.trim(),
      content: _content.text.trim(),
      type: 'Class',
      targetClassId: tc.classId,
      subjectId: tc.isHomeroomChannel || tc.subjectId <= 0
          ? null
          : tc.subjectId,
      sendPush: true,
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Đã gửi.')),
    );
    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final tc = widget.teacherClass;
    final channelLabel = tc.isHomeroomChannel
        ? 'Tin chủ nhiệm lớp ${tc.className}'
        : 'Tin bộ môn ${tc.subjectName} • lớp ${tc.className}';
    final appBarTitle = tc.isHomeroomChannel
        ? 'TB chủ nhiệm • ${tc.className}'
        : 'TB ${tc.subjectName} • ${tc.className}';

    return Scaffold(
      appBar: AppBar(title: Text(appBarTitle)),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Card(
              color: AppColors.primary.withValues(alpha: 0.08),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    Icon(
                      tc.isHomeroomChannel ? Icons.groups : Icons.menu_book,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        channelLabel,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
              ),
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
              controller: _content,
              maxLines: 8,
              decoration: const InputDecoration(
                labelText: 'Nội dung *',
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
              ),
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Nhập nội dung' : null,
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton.icon(
                onPressed: _saving ? null : _submit,
                icon: _saving
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.send),
                label: Text(_saving ? 'Đang gửi...' : 'Gửi thông báo'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
