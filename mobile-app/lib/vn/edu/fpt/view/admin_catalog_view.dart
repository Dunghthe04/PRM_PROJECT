import 'package:flutter/material.dart';
import '../common/format_utils.dart';
import '../common/list_load_state.dart';
import '../controller/admin_catalog_controller.dart';
import '../controller/admin_user_controller.dart';
import '../model/user_model.dart';

/// Màn danh mục Admin (FR5.2): tab Kỳ học · Môn học · Lớp học.
/// Không có entity Khối riêng — ghi khối trong tên lớp (vd 10A1).
class AdminCatalogPage extends StatelessWidget {
  const AdminCatalogPage({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 3,
      child: Column(
        children: [
          const TabBar(
            tabs: [
              Tab(text: 'Kỳ học'),
              Tab(text: 'Môn học'),
              Tab(text: 'Lớp học'),
            ],
          ),
          const Expanded(
            child: TabBarView(
              children: [
                _SemestersTab(),
                _SubjectsTab(),
                _ClassesTab(),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Kỳ học ────────────────────────────────────────────────────────────────

class _SemestersTab extends StatefulWidget {
  const _SemestersTab();

  @override
  State<_SemestersTab> createState() => _SemestersTabState();
}

class _SemestersTabState extends State<_SemestersTab> {
  final _c = AdminCatalogController();
  final _state = ListLoadState<SemesterModel>();

  @override
  void initState() {
    super.initState();
    _loadList();
  }

  Future<void> _loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _c.getSemesters(),
      );

  Future<void> _openForm({SemesterModel? edit}) async {
    final nameCtrl = TextEditingController(text: edit?.name ?? '');
    var start = edit?.startDate ?? DateTime.now();
    var end = edit?.endDate ?? DateTime.now().add(const Duration(days: 120));

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: Text(edit == null ? 'Thêm kỳ học' : 'Sửa kỳ học'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: nameCtrl,
                decoration: const InputDecoration(
                  labelText: 'Tên kỳ *',
                  border: OutlineInputBorder(),
                ),
              ),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('Bắt đầu: ${FormatUtils.date(start)}'),
                trailing: TextButton(
                  child: const Text('Chọn'),
                  onPressed: () async {
                    final d = await showDatePicker(
                      context: ctx,
                      initialDate: start,
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2035),
                    );
                    if (d != null) setLocal(() => start = d);
                  },
                ),
              ),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('Kết thúc: ${FormatUtils.date(end)}'),
                trailing: TextButton(
                  child: const Text('Chọn'),
                  onPressed: () async {
                    final d = await showDatePicker(
                      context: ctx,
                      initialDate: end,
                      firstDate: DateTime(2020),
                      lastDate: DateTime(2035),
                    );
                    if (d != null) setLocal(() => end = d);
                  },
                ),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
            ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Lưu')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final (_, err) = await _c.saveSemester(
      id: edit?.id,
      name: nameCtrl.text.trim(),
      start: start,
      end: end,
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    _loadList();
  }

  Future<void> _delete(SemesterModel s) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Xóa kỳ học?'),
        content: Text(s.name),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Xóa')),
        ],
      ),
    );
    if (ok != true) return;
    final (success, msg) = await _c.deleteSemester(s.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) _loadList();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: ElevatedButton.icon(
              onPressed: () => _openForm(),
              icon: const Icon(Icons.add),
              label: const Text('Thêm kỳ'),
            ),
          ),
        ),
        Expanded(child: _buildList()),
      ],
    );
  }

  Widget _buildList() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_state.error != null) return Center(child: Text(_state.error!));
    final items = _state.items ?? [];
    if (items.isEmpty) {
      return const Center(child: Text('Chưa có kỳ học.'));
    }
    return ListView.separated(
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final s = items[i];
        return ListTile(
          title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(
              '${FormatUtils.date(s.startDate)} → ${FormatUtils.date(s.endDate)}'),
          trailing: PopupMenuButton<String>(
            onSelected: (v) {
              if (v == 'edit') _openForm(edit: s);
              if (v == 'del') _delete(s);
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'edit', child: Text('Sửa')),
              PopupMenuItem(value: 'del', child: Text('Xóa')),
            ],
          ),
        );
      },
    );
  }
}

// ─── Môn học ───────────────────────────────────────────────────────────────

class _SubjectsTab extends StatefulWidget {
  const _SubjectsTab();

  @override
  State<_SubjectsTab> createState() => _SubjectsTabState();
}

class _SubjectsTabState extends State<_SubjectsTab> {
  final _c = AdminCatalogController();
  final _state = ListLoadState<SubjectModel>();

  @override
  void initState() {
    super.initState();
    _loadList();
  }

  Future<void> _loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _c.getSubjects(),
      );

  Future<void> _openForm({SubjectModel? edit}) async {
    final nameCtrl = TextEditingController(text: edit?.name ?? '');
    final codeCtrl = TextEditingController(text: edit?.code ?? '');
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(edit == null ? 'Thêm môn' : 'Sửa môn'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: nameCtrl,
              decoration: const InputDecoration(
                  labelText: 'Tên môn *', border: OutlineInputBorder()),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: codeCtrl,
              decoration: const InputDecoration(
                  labelText: 'Mã môn *', border: OutlineInputBorder()),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Lưu')),
        ],
      ),
    );
    if (ok != true) return;
    final (_, err) = await _c.saveSubject(
      id: edit?.id,
      name: nameCtrl.text.trim(),
      code: codeCtrl.text.trim(),
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    _loadList();
  }

  Future<void> _delete(SubjectModel s) async {
    final (success, msg) = await _c.deleteSubject(s.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) _loadList();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: ElevatedButton.icon(
              onPressed: () => _openForm(),
              icon: const Icon(Icons.add),
              label: const Text('Thêm môn'),
            ),
          ),
        ),
        Expanded(child: _buildList()),
      ],
    );
  }

  Widget _buildList() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_state.error != null) return Center(child: Text(_state.error!));
    final items = _state.items ?? [];
    return ListView.separated(
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final s = items[i];
        return ListTile(
          title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(s.code),
          trailing: PopupMenuButton<String>(
            onSelected: (v) {
              if (v == 'edit') _openForm(edit: s);
              if (v == 'del') _delete(s);
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'edit', child: Text('Sửa')),
              PopupMenuItem(value: 'del', child: Text('Xóa')),
            ],
          ),
        );
      },
    );
  }
}

// ─── Lớp học ───────────────────────────────────────────────────────────────

class _ClassesTab extends StatefulWidget {
  const _ClassesTab();

  @override
  State<_ClassesTab> createState() => _ClassesTabState();
}

class _ClassesTabState extends State<_ClassesTab> {
  final _c = AdminCatalogController();
  List<SemesterModel> _semesters = [];
  final _state = ListLoadState<ClassModel>();

  @override
  void initState() {
    super.initState();
    _loadList();
    _loadSemesters();
  }

  Future<void> _loadSemesters() async {
    final (list, _) = await _c.getSemesters();
    if (mounted && list != null) setState(() => _semesters = list);
  }

  Future<void> _loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _c.getClasses(),
      );

  Future<void> _openForm({ClassModel? edit}) async {
    if (_semesters.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Cần tạo Kỳ học trước.')),
      );
      return;
    }
    final nameCtrl = TextEditingController(text: edit?.name ?? '');
    var semesterId = edit?.semesterId ?? _semesters.first.id;
    int? homeroomId = edit?.homeroomTeacherId;

    // Tải DS GV để chọn chủ nhiệm.
    final (page, _) = await AdminUserController().list(
      role: 'Teacher',
      pageSize: 100,
      isLocked: false,
    );
    final teachers = page?.items ?? <UserModel>[];

    if (!mounted) return;
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: Text(edit == null ? 'Thêm lớp' : 'Sửa lớp'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: nameCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Tên lớp * (vd 10A1)',
                    border: OutlineInputBorder(),
                    helperText: 'Có thể gắn khối trong tên lớp',
                  ),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  initialValue: semesterId,
                  decoration: const InputDecoration(
                    labelText: 'Kỳ học',
                    border: OutlineInputBorder(),
                  ),
                  items: _semesters
                      .map((s) =>
                          DropdownMenuItem(value: s.id, child: Text(s.name)))
                      .toList(),
                  onChanged: (v) => setLocal(() => semesterId = v ?? semesterId),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int?>(
                  // ignore: deprecated_member_use
                  value: homeroomId,
                  decoration: const InputDecoration(
                    labelText: 'GV chủ nhiệm',
                    border: OutlineInputBorder(),
                    helperText: 'Khác GV bộ môn (phân công dạy môn)',
                  ),
                  items: [
                    const DropdownMenuItem<int?>(
                      value: null,
                      child: Text('(Chưa gán)'),
                    ),
                    ...teachers.map(
                      (t) => DropdownMenuItem<int?>(
                        value: t.id,
                        child: Text(t.fullName),
                      ),
                    ),
                  ],
                  onChanged: (v) => setLocal(() => homeroomId = v),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
            ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Lưu')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final (_, err) = await _c.saveClass(
      id: edit?.id,
      name: nameCtrl.text.trim(),
      semesterId: semesterId,
      homeroomTeacherId: homeroomId,
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    _loadList();
  }

  Future<void> _delete(ClassModel c) async {
    final (success, msg) = await _c.deleteClass(c.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) _loadList();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: ElevatedButton.icon(
              onPressed: () => _openForm(),
              icon: const Icon(Icons.add),
              label: const Text('Thêm lớp'),
            ),
          ),
        ),
        Expanded(child: _buildList()),
      ],
    );
  }

  Widget _buildList() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_state.error != null) return Center(child: Text(_state.error!));
    final items = _state.items ?? [];
    return ListView.separated(
      itemCount: items.length,
      separatorBuilder: (_, _) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final c = items[i];
        return ListTile(
          title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(
            '${c.semesterName ?? "Kỳ #${c.semesterId}"} • ${c.studentCount} HS'
            '${c.homeroomTeacherName != null ? ' • CN: ${c.homeroomTeacherName}' : ''}',
          ),
          trailing: PopupMenuButton<String>(
            onSelected: (v) {
              if (v == 'edit') _openForm(edit: c);
              if (v == 'del') _delete(c);
            },
            itemBuilder: (_) => const [
              PopupMenuItem(value: 'edit', child: Text('Sửa')),
              PopupMenuItem(value: 'del', child: Text('Xóa')),
            ],
          ),
        );
      },
    );
  }
}
