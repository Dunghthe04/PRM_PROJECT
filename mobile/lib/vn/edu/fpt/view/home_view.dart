import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../common/app_colors.dart';
import '../controller/account_controller.dart';
import '../controller/auth_controller.dart';
import '../controller/notification_controller.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';
import '../service/push_service.dart';
import 'announcement_view.dart';
import 'child_switcher.dart';
import 'dashboard_view.dart';
import 'notification_view.dart';
import 'study_view.dart';
import 'admin_shell_view.dart';
import 'teacher_class_view.dart';

/// HomeView: sau login, tải hồ sơ (/account/me) rồi dựng khung chính.
class HomeView extends StatefulWidget {
  const HomeView({super.key});

  @override
  State<HomeView> createState() => _HomeViewState();
}

class _HomeViewState extends State<HomeView> {
  final AccountController _accountController = AccountController();

  // Future giữ kết quả gọi /account/me. Khai báo late vì gán trong initState.
  late Future<(UserModel?, String?)> _profileFuture;

  // initState: chạy 1 LẦN khi widget được tạo (trước build).
  @override
  void initState() {
    super.initState();
    _profileFuture = _accountController.getProfile(); // bắt đầu tải hồ sơ
    // Đã đăng nhập → khởi tạo push, đăng ký FCM token với server (FR1.4).
    PushService.instance.init();
  }

  @override
  Widget build(BuildContext context) {
    // FutureBuilder: tự vẽ lại UI theo trạng thái của Future (đang chờ / xong / lỗi).
    return FutureBuilder<(UserModel?, String?)>(
      future: _profileFuture,
      builder: (context, snapshot) {
        // 1) Đang tải → vòng xoay
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Scaffold(
            body: Center(child: CircularProgressIndicator()),
          );
        }

        // 2) Lấy kết quả (user, error)
        final (user, error) = snapshot.data ?? (null, 'Không tải được hồ sơ.');

        // 3) Không có user (token hỏng) → hiện lỗi + nút về Login
        if (user == null) {
          return Scaffold(
            body: Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(error ?? 'Lỗi không xác định'),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () => context.go('/login'),
                    child: const Text('Về đăng nhập'),
                  ),
                ],
              ),
            ),
          );
        }

        // 4) Admin → shell sidebar (Ngày 19); các role khác → bottom nav
        if (user.role == 'Admin') {
          return AdminShell(user: user);
        }
        return _MainShell(user: user);
      },
    );
  }
}

/// _MainShell: khung chính có BottomNavigationBar, đổi tab theo role.
class _MainShell extends StatefulWidget {
  final UserModel user;
  const _MainShell({required this.user});

  @override
  State<_MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<_MainShell> {
  int _currentIndex = 0; // tab đang chọn
  int _unreadCount = 0; // số thông báo chưa đọc → hiện badge trên tab Thông báo
  final NotificationController _notificationController = NotificationController();
  final AccountController _accountController = AccountController();

  @override
  void initState() {
    super.initState();
    _refreshUnread(); // đếm số chưa đọc ngay khi vào app
    _loadChildrenIfParent(); // nạp danh sách con nếu là phụ huynh (FR2.1)
  }

  /// Chỉ phụ huynh mới có "con" → nạp danh sách vào ParentSession.
  Future<void> _loadChildrenIfParent() async {
    if (widget.user.role != 'Parent') return;
    final (children, _) = await _accountController.getChildren();
    if (children != null) ParentSession.instance.setChildren(children);
  }

  /// Gọi API đếm lại số thông báo chưa đọc, cập nhật badge.
  Future<void> _refreshUnread() async {
    final count = await _notificationController.getUnreadCount();
    if (mounted) setState(() => _unreadCount = count);
  }

  /// Chuyển sang tab có id cho trước (Dashboard nhờ điều hướng).
  /// Nhận: [id] — id tab đích (vd 'notifications', 'announcements').
  void _goToTabId(String id) {
    final tabs = _buildTabs(widget.user, _refreshUnread, _goToTabId);
    final idx = tabs.indexWhere((t) => t.id == id);
    if (idx >= 0) setState(() => _currentIndex = idx);
    _refreshUnread();
  }

  @override
  Widget build(BuildContext context) {
    // Lấy danh sách tab theo vai trò; truyền callback để tab Thông báo
    // báo lại khi số chưa đọc thay đổi, và callback điều hướng cho Dashboard.
    final tabs = _buildTabs(widget.user, _refreshUnread, _goToTabId);

    return Scaffold(
      appBar: AppBar(
        title: Text(tabs[_currentIndex].label),
        // Phụ huynh: hiện nút đổi con ở góc phải AppBar (Switch Profile).
        actions: [
          if (widget.user.role == 'Parent') const ChildSwitcher(),
        ],
      ),
      // IndexedStack: giữ nguyên trạng thái mọi tab, chỉ hiện tab đang chọn.
      body: IndexedStack(
        index: _currentIndex,
        children: tabs.map((t) => t.body).toList(),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        type: BottomNavigationBarType.fixed, // >3 tab vẫn hiện đủ chữ
        selectedItemColor: AppColors.primary,
        unselectedItemColor: AppColors.textGrey,
        onTap: (i) {
          setState(() => _currentIndex = i); // đổi tab
          _refreshUnread(); // đổi tab → cập nhật lại badge cho chắc
        },
        items: tabs
            .map((t) => BottomNavigationBarItem(
                  // Nếu là tab Thông báo và có chưa đọc → bọc icon bằng Badge.
                  icon: (t.id == 'notifications' && _unreadCount > 0)
                      ? Badge.count(count: _unreadCount, child: Icon(t.icon))
                      : Icon(t.icon),
                  label: t.label,
                ))
            .toList(),
      ),
    );
  }
}

/// _TabItem: mô tả 1 tab (icon + nhãn + nội dung + id nhận diện).
class _TabItem {
  final IconData icon;
  final String label;
  final Widget body;
  final String? id; // dùng để nhận diện tab đặc biệt (vd 'notifications')
  _TabItem({required this.icon, required this.label, required this.body, this.id});
}

/// Trả về danh sách tab tùy theo vai trò user.
///
/// Nhận:
///   - [user]: user đang đăng nhập (quyết định bộ tab).
///   - [onUnreadChanged]: callback để tab Thông báo báo cập nhật badge.
///   - [onNavigateTab]: callback để Dashboard nhờ chuyển sang tab khác.
List<_TabItem> _buildTabs(
  UserModel user,
  VoidCallback onUnreadChanged,
  void Function(String tabId) onNavigateTab,
) {
  final home = _TabItem(
      id: 'home',
      icon: Icons.home,
      label: 'Trang chủ',
      body: DashboardTab(user: user, onNavigateTab: onNavigateTab));
  final noti = _TabItem(
      id: 'notifications',
      icon: Icons.notifications,
      label: 'Thông báo',
      body: NotificationTab(onUnreadChanged: onUnreadChanged));
  final profile = _TabItem(
      id: 'profile',
      icon: Icons.person,
      label: 'Hồ sơ',
      body: _ProfileTab(user: user));

  switch (user.role) {
    case 'Teacher':
      return [
        home,
        _TabItem(id: 'class', icon: Icons.class_, label: 'Lớp học', body: TeacherClassTab(user: user)),
        noti,
        profile,
      ];
    case 'Admin':
      return [
        home,
        _TabItem(id: 'manage', icon: Icons.manage_accounts, label: 'Quản lý', body: const _Placeholder(title: 'Quản lý')),
        _TabItem(id: 'report', icon: Icons.bar_chart, label: 'Báo cáo', body: const _Placeholder(title: 'Báo cáo')),
        profile,
      ];
    default: // Parent, Student
      return [
        home,
        _TabItem(id: 'study', icon: Icons.school, label: 'Học tập', body: StudyTab(user: user)),
        _TabItem(id: 'announcements', icon: Icons.article, label: 'Bảng tin', body: const AnnouncementTab()),
        noti,
        profile,
      ];
  }
}

/// Trang tạm cho các tab chưa làm (sẽ hoàn thiện ở Ngày 14–18).
class _Placeholder extends StatelessWidget {
  final String title;
  const _Placeholder({required this.title});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Text('$title\n(đang phát triển)', textAlign: TextAlign.center),
    );
  }
}

/// Tab Hồ sơ: hiện thông tin user + nút Đăng xuất (chi tiết hoàn thiện Bước 6).
/// Tab Hồ sơ: xem thông tin + sửa hồ sơ + đổi mật khẩu + đăng xuất.
/// StatefulWidget vì sau khi sửa hồ sơ cần cập nhật lại thông tin hiển thị.
class _ProfileTab extends StatefulWidget {
  final UserModel user;
  const _ProfileTab({required this.user});

  @override
  State<_ProfileTab> createState() => _ProfileTabState();
}

class _ProfileTabState extends State<_ProfileTab> {
  final AccountController _accountController = AccountController();
  late UserModel _user; // bản đang hiển thị (đổi sau khi sửa hồ sơ)

  @override
  void initState() {
    super.initState();
    _user = widget.user;
  }

  /// Hiện SnackBar thông báo ngắn ở đáy màn hình.
  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  /// Mở dialog sửa họ tên + email.
  Future<void> _openEditProfile() async {
    final nameCtrl = TextEditingController(text: _user.fullName);
    final emailCtrl = TextEditingController(text: _user.email ?? '');

    final saved = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sửa hồ sơ'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: nameCtrl,
              decoration: const InputDecoration(labelText: 'Họ và tên'),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: emailCtrl,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: 'Email'),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Lưu')),
        ],
      ),
    );

    if (saved != true) return;

    final (updated, error) = await _accountController.updateProfile(
      fullName: nameCtrl.text.trim(),
      email: emailCtrl.text.trim().isEmpty ? null : emailCtrl.text.trim(),
    );
    if (!mounted) return;
    if (error != null) {
      _toast(error);
    } else {
      setState(() => _user = updated!); // cập nhật thông tin hiển thị
      _toast('Cập nhật hồ sơ thành công.');
    }
  }

  /// Mở dialog đổi mật khẩu (MK cũ + MK mới + xác nhận).
  Future<void> _openChangePassword() async {
    final currentCtrl = TextEditingController();
    final newCtrl = TextEditingController();
    final confirmCtrl = TextEditingController();
    String? localError;

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        // StatefulBuilder: cho phép setState RIÊNG bên trong dialog (hiện lỗi).
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Đổi mật khẩu'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: currentCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Mật khẩu hiện tại'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: newCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Mật khẩu mới'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: confirmCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Nhập lại mật khẩu mới'),
              ),
              if (localError != null) ...[
                const SizedBox(height: 12),
                Text(localError!, style: const TextStyle(color: AppColors.danger)),
              ],
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
            ElevatedButton(
              onPressed: () {
                // Validate cơ bản trước khi gọi API.
                if (newCtrl.text.length < 6) {
                  setDialogState(() => localError = 'Mật khẩu mới tối thiểu 6 ký tự.');
                  return;
                }
                if (newCtrl.text != confirmCtrl.text) {
                  setDialogState(() => localError = 'Mật khẩu nhập lại không khớp.');
                  return;
                }
                Navigator.pop(ctx, true);
              },
              child: const Text('Đổi'),
            ),
          ],
        ),
      ),
    );

    if (ok != true) return;

    final (success, message) = await _accountController.changePassword(
      currentPassword: currentCtrl.text,
      newPassword: newCtrl.text,
    );
    if (!mounted) return;
    _toast(message); // thành công hay lỗi đều hiện thông báo từ backend
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: 16),
          const CircleAvatar(
            radius: 40,
            backgroundColor: AppColors.primary,
            child: Icon(Icons.person, size: 48, color: Colors.white),
          ),
          const SizedBox(height: 16),
          Text(_user.fullName,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text('SĐT: ${_user.phone}', textAlign: TextAlign.center),
          if ((_user.email ?? '').isNotEmpty)
            Text('Email: ${_user.email}', textAlign: TextAlign.center),
          Text('Vai trò: ${_user.role}', textAlign: TextAlign.center),
          const SizedBox(height: 32),

          OutlinedButton.icon(
            onPressed: _openEditProfile,
            icon: const Icon(Icons.edit),
            label: const Text('Sửa hồ sơ'),
          ),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            onPressed: _openChangePassword,
            icon: const Icon(Icons.lock_reset),
            label: const Text('Đổi mật khẩu'),
          ),
          const SizedBox(height: 12),
          ElevatedButton.icon(
            onPressed: () async {
              await PushService.instance.unregister(); // ngừng nhận push
              await AuthController().logout();
              ParentSession.instance.clear(); // xóa danh sách con đã chọn
              if (context.mounted) context.go('/login');
            },
            icon: const Icon(Icons.logout),
            label: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
  }
}