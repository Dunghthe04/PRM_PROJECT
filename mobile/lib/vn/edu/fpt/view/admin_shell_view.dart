import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../common/app_colors.dart';
import '../controller/auth_controller.dart';
import '../model/user_model.dart';
import '../service/push_service.dart';
import 'admin_catalog_view.dart';
import 'admin_users_view.dart';

/// Shell Admin (Ngày 19): sidebar khi màn rộng, Drawer khi hẹp.
/// Menu Day 19: Người dùng · Danh mục. Day 20 sẽ thêm Tài chính / TB / Báo cáo.
class AdminShell extends StatefulWidget {
  final UserModel user;
  const AdminShell({super.key, required this.user});

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  /// Index mục đang chọn trong sidebar.
  int _index = 0;

  static const _items = <_NavItem>[
    _NavItem('Người dùng', Icons.manage_accounts, _AdminSection.users),
    _NavItem('Danh mục', Icons.category, _AdminSection.catalog),
    _NavItem('Tài chính', Icons.payments, _AdminSection.todoFinance),
    _NavItem('Bảng tin', Icons.campaign, _AdminSection.todoAnnounce),
    _NavItem('Báo cáo', Icons.bar_chart, _AdminSection.todoReport),
  ];

  Widget _bodyFor(_AdminSection section) {
    switch (section) {
      case _AdminSection.users:
        return const AdminUsersPage();
      case _AdminSection.catalog:
        return const AdminCatalogPage();
      case _AdminSection.todoFinance:
        return const _TodoPane(
            title: 'Tài chính', hint: 'Sẽ làm ở Ngày 20 (khoản thu, PayOS…).');
      case _AdminSection.todoAnnounce:
        return const _TodoPane(
            title: 'Bảng tin toàn trường', hint: 'Sẽ làm ở Ngày 20.');
      case _AdminSection.todoReport:
        return const _TodoPane(
            title: 'Báo cáo', hint: 'Sẽ làm ở Ngày 20 (xem trên màn).');
    }
  }

  Future<void> _logout() async {
    await PushService.instance.unregister();
    await AuthController().logout();
    if (!mounted) return;
    context.go('/login');
  }

  Widget _sidebar({required bool drawer}) {
    return Material(
      color: AppColors.primary,
      child: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('FSchool Admin',
                      style: TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.bold)),
                  const SizedBox(height: 4),
                  Text(widget.user.fullName,
                      style: const TextStyle(color: Colors.white70)),
                ],
              ),
            ),
            const Divider(color: Colors.white24, height: 1),
            Expanded(
              child: ListView.builder(
                itemCount: _items.length,
                itemBuilder: (context, i) {
                  final item = _items[i];
                  final selected = _index == i;
                  return ListTile(
                    leading: Icon(item.icon,
                        color: selected ? AppColors.primary : Colors.white),
                    title: Text(item.label,
                        style: TextStyle(
                          color: selected ? AppColors.primary : Colors.white,
                          fontWeight:
                              selected ? FontWeight.bold : FontWeight.normal,
                        )),
                    selected: selected,
                    selectedTileColor: Colors.white,
                    onTap: () {
                      setState(() => _index = i);
                      if (drawer) Navigator.pop(context);
                    },
                  );
                },
              ),
            ),
            ListTile(
              leading: const Icon(Icons.logout, color: Colors.white),
              title: const Text('Đăng xuất', style: TextStyle(color: Colors.white)),
              onTap: _logout,
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final wide = MediaQuery.sizeOf(context).width >= 900;
    final section = _items[_index].section;

    if (wide) {
      return Scaffold(
        body: Row(
          children: [
            SizedBox(width: 260, child: _sidebar(drawer: false)),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Material(
                    elevation: 1,
                    color: Colors.white,
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 20, vertical: 16),
                      child: Text(_items[_index].label,
                          style: const TextStyle(
                              fontSize: 20, fontWeight: FontWeight.bold)),
                    ),
                  ),
                  Expanded(child: _bodyFor(section)),
                ],
              ),
            ),
          ],
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: Text(_items[_index].label)),
      drawer: Drawer(child: _sidebar(drawer: true)),
      body: _bodyFor(section),
    );
  }
}

enum _AdminSection { users, catalog, todoFinance, todoAnnounce, todoReport }

class _NavItem {
  final String label;
  final IconData icon;
  final _AdminSection section;
  const _NavItem(this.label, this.icon, this.section);
}

class _TodoPane extends StatelessWidget {
  final String title;
  final String hint;
  const _TodoPane({required this.title, required this.hint});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.construction, size: 48, color: AppColors.textGrey),
          const SizedBox(height: 12),
          Text(title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Text(hint, style: const TextStyle(color: AppColors.textGrey)),
        ],
      ),
    );
  }
}
