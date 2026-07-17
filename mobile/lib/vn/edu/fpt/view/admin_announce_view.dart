import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/admin_user_controller.dart';
import '../controller/announcement_controller.dart';
import '../model/announcement_model.dart';
import '../model/user_model.dart';
import 'announcement_view.dart';

/// Đối tượng nhận thông báo Admin trên form Bảng tin.
enum _Audience { school, allTeachers, oneTeacher }

/// Admin đăng thông báo (FR5.4) + lịch sử Đã gửi.
/// - Tab Soạn: Toàn trường / Toàn bộ GV / Một GV
/// - Tab Đã gửi: GET /announcements/mine
class AdminAnnouncePage extends StatefulWidget {
  const AdminAnnouncePage({super.key});

  @override
  State<AdminAnnouncePage> createState() => _AdminAnnouncePageState();
}

class _AdminAnnouncePageState extends State<AdminAnnouncePage>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;
  /// Tăng khi gửi thành công → tab Đã gửi tự tải lại.
  int _sentReloadToken = 0;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  void _onSentSuccess() {
    setState(() => _sentReloadToken++);
    _tabs.animateTo(1);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        TabBar(
          controller: _tabs,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textGrey,
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(text: 'Soạn thông báo'),
            Tab(text: 'Đã gửi'),
          ],
        ),
        Expanded(
          child: TabBarView(
            controller: _tabs,
            children: [
              _ComposeTab(onSent: _onSentSuccess),
              _AdminSentList(reloadToken: _sentReloadToken),
            ],
          ),
        ),
      ],
    );
  }
}

/// Form soạn & gửi thông báo Admin.
class _ComposeTab extends StatefulWidget {
  final VoidCallback onSent;
  const _ComposeTab({required this.onSent});

  @override
  State<_ComposeTab> createState() => _ComposeTabState();
}

class _ComposeTabState extends State<_ComposeTab> {
  final _formKey = GlobalKey<FormState>();
  final _announceCtrl = AnnouncementController();
  final _userCtrl = AdminUserController();
  final _title = TextEditingController();
  final _content = TextEditingController();

  _Audience _audience = _Audience.school;
  bool _saving = false;

  List<UserModel> _teachers = [];
  UserModel? _selectedTeacher;
  bool _loadingTeachers = false;
  String? _teachersError;

  @override
  void dispose() {
    _title.dispose();
    _content.dispose();
    super.dispose();
  }

  String get _hint {
    switch (_audience) {
      case _Audience.school:
        return 'Thông báo toàn trường — gửi tới mọi học sinh / phụ huynh / giáo viên (hiện trên Bảng tin).';
      case _Audience.allTeachers:
        return 'Gửi tới toàn bộ giáo viên — chỉ hiện ở Thông báo → Đã nhận của GV (không lên Bảng tin công khai).';
      case _Audience.oneTeacher:
        return 'Gửi tới một giáo viên — chỉ hiện ở Thông báo → Đã nhận của GV đó.';
    }
  }

  String get _submitLabel {
    switch (_audience) {
      case _Audience.school:
        return 'Đăng toàn trường';
      case _Audience.allTeachers:
        return 'Gửi toàn bộ giáo viên';
      case _Audience.oneTeacher:
        return 'Gửi giáo viên đã chọn';
    }
  }

  Future<void> _ensureTeachersLoaded() async {
    if (_teachers.isNotEmpty || _loadingTeachers) return;
    setState(() {
      _loadingTeachers = true;
      _teachersError = null;
    });
    final (page, err) = await _userCtrl.list(
      role: 'Teacher',
      pageSize: 100,
      isLocked: false,
    );
    if (!mounted) return;
    setState(() {
      _loadingTeachers = false;
      if (err != null) {
        _teachersError = err;
      } else {
        _teachers = page?.items ?? [];
        if (_teachers.isEmpty) {
          _teachersError = 'Chưa có giáo viên nào trong hệ thống.';
        }
      }
    });
  }

  Future<void> _onAudienceChanged(_Audience? v) async {
    if (v == null) return;
    setState(() {
      _audience = v;
      if (v != _Audience.oneTeacher) _selectedTeacher = null;
    });
    if (v == _Audience.oneTeacher || v == _Audience.allTeachers) {
      await _ensureTeachersLoaded();
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_audience == _Audience.oneTeacher && _selectedTeacher == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chọn giáo viên nhận thông báo.')),
      );
      return;
    }

    final type = switch (_audience) {
      _Audience.school => 'Global',
      _Audience.allTeachers => 'Teachers',
      _Audience.oneTeacher => 'Teacher',
    };

    setState(() => _saving = true);
    final (count, err) = await _announceCtrl.create(
      title: _title.text.trim(),
      content: _content.text.trim(),
      type: type,
      targetUserId: _selectedTeacher?.id,
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
    _title.clear();
    _content.clear();
    setState(() => _selectedTeacher = null);
    widget.onSent();
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
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Text(
                _hint,
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
            ),
          ),
          const SizedBox(height: 16),
          const Text('Đối tượng nhận *',
              style: TextStyle(fontWeight: FontWeight.w600)),
          const SizedBox(height: 8),
          SegmentedButton<_Audience>(
            segments: const [
              ButtonSegment(
                value: _Audience.school,
                label: Text('Toàn trường'),
                icon: Icon(Icons.campaign, size: 18),
              ),
              ButtonSegment(
                value: _Audience.allTeachers,
                label: Text('Toàn bộ GV'),
                icon: Icon(Icons.groups, size: 18),
              ),
              ButtonSegment(
                value: _Audience.oneTeacher,
                label: Text('Một GV'),
                icon: Icon(Icons.person, size: 18),
              ),
            ],
            selected: {_audience},
            onSelectionChanged: (s) => _onAudienceChanged(s.first),
          ),
          if (_audience == _Audience.oneTeacher) ...[
            const SizedBox(height: 16),
            if (_loadingTeachers)
              const LinearProgressIndicator()
            else if (_teachersError != null)
              Text(_teachersError!,
                  style: const TextStyle(color: AppColors.danger))
            else
              DropdownButtonFormField<UserModel>(
                // ignore: deprecated_member_use
                value: _selectedTeacher,
                decoration: const InputDecoration(
                  labelText: 'Chọn giáo viên *',
                  border: OutlineInputBorder(),
                ),
                items: _teachers
                    .map((t) => DropdownMenuItem(
                          value: t,
                          child: Text('${t.fullName} (${t.phone})'),
                        ))
                    .toList(),
                onChanged: (t) => setState(() => _selectedTeacher = t),
                validator: (_) =>
                    _audience == _Audience.oneTeacher && _selectedTeacher == null
                        ? 'Chọn giáo viên'
                        : null,
              ),
          ],
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
                  : Icon(_audience == _Audience.school
                      ? Icons.campaign
                      : Icons.send),
              label: Text(_saving ? 'Đang gửi...' : _submitLabel),
            ),
          ),
        ],
      ),
    );
  }
}

/// Lịch sử tin Admin đã gửi (GET /announcements/mine).
class _AdminSentList extends StatefulWidget {
  final int reloadToken;
  const _AdminSentList({required this.reloadToken});

  @override
  State<_AdminSentList> createState() => _AdminSentListState();
}

class _AdminSentListState extends State<_AdminSentList> {
  final _controller = AnnouncementController();
  late Future<(List<AnnouncementModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getMine();
  }

  @override
  void didUpdateWidget(covariant _AdminSentList oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.reloadToken != widget.reloadToken) {
      _reload();
    }
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getMine());
    await _future;
  }

  /// Nhãn đối tượng nhận để Admin dễ lọc bằng mắt.
  String _audienceLabel(AnnouncementModel a) {
    switch (a.type) {
      case 'Global':
        return 'Toàn trường';
      case 'Teachers':
        return 'Toàn bộ giáo viên';
      case 'Teacher':
        return a.targetUserName?.isNotEmpty == true
            ? 'GV: ${a.targetUserName}'
            : 'Một giáo viên';
      case 'Class':
        return a.targetClassName ?? 'Theo lớp';
      default:
        return a.type;
    }
  }

  IconData _audienceIcon(AnnouncementModel a) {
    switch (a.type) {
      case 'Global':
        return Icons.campaign;
      case 'Teachers':
        return Icons.groups;
      case 'Teacher':
        return Icons.person;
      default:
        return Icons.send;
    }
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<(List<AnnouncementModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }

        final (list, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');

        if (error != null) {
          return Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(error, textAlign: TextAlign.center),
                const SizedBox(height: 12),
                ElevatedButton.icon(
                  onPressed: _reload,
                  icon: const Icon(Icons.refresh),
                  label: const Text('Thử lại'),
                ),
              ],
            ),
          );
        }

        if (list == null || list.isEmpty) {
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Chưa gửi thông báo nào.')),
              ],
            ),
          );
        }

        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: list.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) {
              final item = list[index];
              return Card(
                child: InkWell(
                  borderRadius: BorderRadius.circular(12),
                  onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => AnnouncementDetailView(item: item),
                    ),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            CircleAvatar(
                              radius: 18,
                              backgroundColor:
                                  AppColors.primary.withValues(alpha: 0.15),
                              child: Icon(
                                _audienceIcon(item),
                                color: AppColors.primary,
                                size: 18,
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                item.title,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                            Icon(Icons.chevron_right,
                                color: Colors.grey.shade400),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Text(
                          item.content,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: AppColors.primary.withValues(alpha: 0.08),
                            borderRadius: BorderRadius.circular(20),
                            border: Border.all(
                                color:
                                    AppColors.primary.withValues(alpha: 0.25)),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(_audienceIcon(item),
                                  size: 12, color: AppColors.primary),
                              const SizedBox(width: 4),
                              Text(
                                _audienceLabel(item),
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.primaryDark,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          FormatUtils.timeAgo(item.createdAt),
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textGrey,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        );
      },
    );
  }
}
