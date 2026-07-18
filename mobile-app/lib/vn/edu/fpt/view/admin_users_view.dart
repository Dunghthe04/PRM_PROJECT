import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/admin_user_controller.dart';
import '../model/user_model.dart';

const _roles = ['Admin', 'Teacher', 'Parent', 'Student'];

/// Màn Admin quản lý người dùng (FR5.1): thêm / sửa / khóa·mở / reset MK.
class AdminUsersPage extends StatefulWidget {
  const AdminUsersPage({super.key});

  @override
  State<AdminUsersPage> createState() => _AdminUsersPageState();
}

class _AdminUsersPageState extends State<AdminUsersPage> {
  final _controller = AdminUserController();
  final _searchCtrl = TextEditingController();
  String? _roleFilter;
  int _page = 1;
  PagedUsers? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final (data, err) = await _controller.list(
      page: _page,
      role: _roleFilter,
      search: _searchCtrl.text,
    );
    if (!mounted) return;
    setState(() {
      _loading = false;
      _data = data;
      _error = err;
    });
  }

  Future<void> _openForm({UserModel? edit}) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => _UserFormDialog(edit: edit),
    );
    if (ok == true) _load();
  }

  Future<void> _toggleLock(UserModel u) async {
    final (ok, msg) =
        u.isLocked ? await _controller.unlock(u.id) : await _controller.lock(u.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (ok) _load();
  }

  Future<void> _resetPassword(UserModel u) async {
    final ctrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Reset MK: ${u.fullName}'),
        content: TextField(
          controller: ctrl,
          obscureText: true,
          decoration: const InputDecoration(
            labelText: 'Mật khẩu mới (≥6 ký tự)',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true), child: const Text('Đặt lại')),
        ],
      ),
    );
    if (ok != true) return;
    final (success, msg) = await _controller.resetPassword(u.id, ctrl.text.trim());
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 220,
                child: TextField(
                  controller: _searchCtrl,
                  decoration: const InputDecoration(
                    hintText: 'Tìm SĐT / tên',
                    prefixIcon: Icon(Icons.search),
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  onSubmitted: (_) {
                    _page = 1;
                    _load();
                  },
                ),
              ),
              DropdownButton<String?>(
                value: _roleFilter,
                hint: const Text('Vai trò'),
                items: [
                  const DropdownMenuItem(value: null, child: Text('Tất cả')),
                  ..._roles.map((r) => DropdownMenuItem(value: r, child: Text(r))),
                ],
                onChanged: (v) {
                  _roleFilter = v;
                  _page = 1;
                  _load();
                },
              ),
              ElevatedButton.icon(
                onPressed: () {
                  _page = 1;
                  _load();
                },
                icon: const Icon(Icons.refresh),
                label: const Text('Tải'),
              ),
              ElevatedButton.icon(
                onPressed: () => _openForm(),
                icon: const Icon(Icons.person_add),
                label: const Text('Thêm tài khoản'),
              ),
            ],
          ),
        ),
        if (_loading)
          const Expanded(child: Center(child: CircularProgressIndicator()))
        else if (_error != null)
          Expanded(child: Center(child: Text(_error!)))
        else
          Expanded(
            child: ListView.separated(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              itemCount: _data?.items.length ?? 0,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, i) {
                final u = _data!.items[i];
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: u.isLocked
                        ? AppColors.danger.withValues(alpha: 0.15)
                        : AppColors.primary.withValues(alpha: 0.15),
                    child: Icon(
                      u.isLocked ? Icons.lock : Icons.person,
                      color: u.isLocked ? AppColors.danger : AppColors.primary,
                    ),
                  ),
                  title: Text(u.fullName,
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text(
                      '${u.phone} • ${u.roleLabel}${u.isLocked ? " • ĐÃ KHÓA" : ""}'),
                  trailing: PopupMenuButton<String>(
                    onSelected: (v) {
                      if (v == 'edit') _openForm(edit: u);
                      if (v == 'lock') _toggleLock(u);
                      if (v == 'reset') _resetPassword(u);
                    },
                    itemBuilder: (_) => [
                      const PopupMenuItem(value: 'edit', child: Text('Sửa')),
                      PopupMenuItem(
                        value: 'lock',
                        child: Text(u.isLocked ? 'Mở khóa' : 'Khóa'),
                      ),
                      const PopupMenuItem(
                          value: 'reset', child: Text('Reset mật khẩu')),
                    ],
                  ),
                );
              },
            ),
          ),
        if (_data != null && _data!.totalPages > 1)
          Padding(
            padding: const EdgeInsets.all(8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                IconButton(
                  onPressed: _page > 1
                      ? () {
                          _page--;
                          _load();
                        }
                      : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('Trang $_page / ${_data!.totalPages} (${_data!.totalCount})'),
                IconButton(
                  onPressed: _page < _data!.totalPages
                      ? () {
                          _page++;
                          _load();
                        }
                      : null,
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _UserFormDialog extends StatefulWidget {
  final UserModel? edit;
  const _UserFormDialog({this.edit});

  @override
  State<_UserFormDialog> createState() => _UserFormDialogState();
}

class _UserFormDialogState extends State<_UserFormDialog> {
  final _formKey = GlobalKey<FormState>();
  final _controller = AdminUserController();
  late final TextEditingController _phone;
  late final TextEditingController _name;
  late final TextEditingController _email;
  late final TextEditingController _password;
  late String _role;
  bool _saving = false;

  bool get _isEdit => widget.edit != null;

  @override
  void initState() {
    super.initState();
    final e = widget.edit;
    _phone = TextEditingController(text: e?.phone ?? '');
    _name = TextEditingController(text: e?.fullName ?? '');
    _email = TextEditingController(text: e?.email ?? '');
    _password = TextEditingController();
    _role = e?.role ?? 'Student';
  }

  @override
  void dispose() {
    _phone.dispose();
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final (user, err) = _isEdit
        ? await _controller.update(
            id: widget.edit!.id,
            fullName: _name.text.trim(),
            role: _role,
            email: _email.text.trim().isEmpty ? null : _email.text.trim(),
          )
        : await _controller.create(
            phone: _phone.text.trim(),
            password: _password.text.trim(),
            fullName: _name.text.trim(),
            role: _role,
            email: _email.text.trim().isEmpty ? null : _email.text.trim(),
          );
    if (!mounted) return;
    setState(() => _saving = false);
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    Navigator.pop(context, true);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
          content: Text(_isEdit
              ? 'Đã cập nhật ${user?.fullName}'
              : 'Đã tạo ${user?.fullName}')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(_isEdit ? 'Sửa tài khoản' : 'Thêm tài khoản'),
      content: SizedBox(
        width: 400,
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: _phone,
                  enabled: !_isEdit,
                  decoration: const InputDecoration(
                    labelText: 'Số điện thoại *',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) =>
                      (v == null || v.trim().length < 9) ? 'SĐT không hợp lệ' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _name,
                  decoration: const InputDecoration(
                    labelText: 'Họ tên *',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) =>
                      (v == null || v.trim().isEmpty) ? 'Nhập họ tên' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _email,
                  decoration: const InputDecoration(
                    labelText: 'Email',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<String>(
                  initialValue: _role,
                  decoration: const InputDecoration(
                    labelText: 'Vai trò',
                    border: OutlineInputBorder(),
                  ),
                  items: _roles
                      .map((r) => DropdownMenuItem(value: r, child: Text(r)))
                      .toList(),
                  onChanged: (v) => setState(() => _role = v ?? 'Student'),
                ),
                if (!_isEdit) ...[
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _password,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'Mật khẩu *',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        (v == null || v.length < 6) ? 'Mật khẩu ≥ 6 ký tự' : null,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy')),
        ElevatedButton(
          onPressed: _saving ? null : _save,
          child: _saving
              ? const SizedBox(
                  width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Lưu'),
        ),
      ],
    );
  }
}
