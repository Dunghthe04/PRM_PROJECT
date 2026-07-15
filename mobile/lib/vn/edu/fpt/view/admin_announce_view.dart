import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/announcement_controller.dart';

/// Admin đăng thông báo toàn trường (FR5.4) — type=Global + sendPush.
class AdminAnnouncePage extends StatefulWidget {
  const AdminAnnouncePage({super.key});

  @override
  State<AdminAnnouncePage> createState() => _AdminAnnouncePageState();
}

class _AdminAnnouncePageState extends State<AdminAnnouncePage> {
  final _formKey = GlobalKey<FormState>();
  final _controller = AnnouncementController();
  final _title = TextEditingController();
  final _content = TextEditingController();
  bool _sendPush = true;
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
    final (count, err) = await _controller.create(
      title: _title.text.trim(),
      content: _content.text.trim(),
      type: 'Global',
      sendPush: _sendPush,
    );
    if (!mounted) return;
    setState(() => _saving = false);
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(_sendPush
            ? 'Đã đăng toàn trường + push tới $count người.'
            : 'Đã đăng bảng tin toàn trường.'),
      ),
    );
    _title.clear();
    _content.clear();
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            color: AppColors.primary.withValues(alpha: 0.08),
            child: const Padding(
              padding: EdgeInsets.all(12),
              child: Text(
                'Thông báo toàn trường — gửi tới mọi học sinh / phụ huynh / giáo viên.',
                style: TextStyle(fontWeight: FontWeight.w600),
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
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('Gửi Push Notification'),
            value: _sendPush,
            activeThumbColor: AppColors.primary,
            onChanged: (v) => setState(() => _sendPush = v),
          ),
          const SizedBox(height: 16),
          SizedBox(
            height: 48,
            child: ElevatedButton.icon(
              onPressed: _saving ? null : _submit,
              icon: _saving
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.campaign),
              label: Text(_saving ? 'Đang gửi...' : 'Đăng toàn trường'),
            ),
          ),
        ],
      ),
    );
  }
}
