import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/announcement_controller.dart';
import '../controller/notification_controller.dart';
import '../model/announcement_model.dart';
import '../model/user_model.dart';
import 'announcement_view.dart';
import 'fee_view.dart';
import 'leave_request_view.dart';

/// Tab Trang chủ / Dashboard (FR2.2): màn tổng quan khi mở app.
/// Hiển thị lời chào, lối tắt theo vai trò, và bảng tin gần đây.
///
/// [user]: user đang đăng nhập (để chào + chọn lối tắt).
/// [onNavigateTab]: callback nhờ _MainShell chuyển sang tab khác theo id.
class DashboardTab extends StatefulWidget {
  final UserModel user;
  final void Function(String tabId) onNavigateTab;
  const DashboardTab({
    super.key,
    required this.user,
    required this.onNavigateTab,
  });

  @override
  State<DashboardTab> createState() => _DashboardTabState();
}

class _DashboardTabState extends State<DashboardTab> {
  final AnnouncementController _announcementController = AnnouncementController();
  final NotificationController _notificationController = NotificationController();

  List<AnnouncementModel> _recent = []; // 3 bảng tin mới nhất
  int _unread = 0; // số thông báo chưa đọc
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  /// Tải song song: bảng tin mới nhất + số thông báo chưa đọc.
  Future<void> _load() async {
    setState(() => _loading = true);
    // Future.wait: chạy 2 request cùng lúc cho nhanh.
    final results = await Future.wait([
      _announcementController.getList(),
      _notificationController.getUnreadCount(),
    ]);

    final (list, _) = results[0] as (List<AnnouncementModel>?, String?);
    final unread = results[1] as int;

    if (!mounted) return;
    setState(() {
      _recent = (list ?? []).take(3).toList(); // chỉ lấy 3 cái đầu
      _unread = unread;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _greeting(),
          const SizedBox(height: 16),
          _quickActions(),
          const SizedBox(height: 24),
          _recentAnnouncements(),
        ],
      ),
    );
  }

  /// Thẻ lời chào: avatar + tên + vai trò.
  Widget _greeting() {
    return Card(
      color: AppColors.primary,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            const CircleAvatar(
              radius: 28,
              backgroundColor: Colors.white,
              child: Icon(Icons.person, color: AppColors.primary, size: 32),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Xin chào,',
                      style: TextStyle(color: Colors.white70)),
                  Text(
                    widget.user.fullName,
                    style: const TextStyle(
                        color: Colors.white,
                        fontSize: 20,
                        fontWeight: FontWeight.bold),
                  ),
                  Text(widget.user.roleLabel,
                      style: const TextStyle(color: Colors.white70)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Lưới lối tắt tùy vai trò.
  Widget _quickActions() {
    final actions = _actionsForRole(widget.user.role);
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true, // vừa nội dung, không cuộn riêng
      physics: const NeverScrollableScrollPhysics(), // để ListView ngoài cuộn
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.6,
      children: actions,
    );
  }

  /// Danh sách thẻ lối tắt theo vai trò.
  List<Widget> _actionsForRole(String role) {
    // Thẻ Thông báo có kèm badge số chưa đọc — mọi vai trò đều có.
    final notiCard = _ActionCard(
      icon: Icons.notifications,
      label: 'Thông báo',
      badge: _unread,
      onTap: () => widget.onNavigateTab('notifications'),
    );

    switch (role) {
      case 'Teacher':
        return [
          _ActionCard(
              icon: Icons.class_,
              label: 'Lớp học',
              onTap: () => widget.onNavigateTab('class')),
          notiCard,
          _ActionCard(
              icon: Icons.checklist,
              label: 'Điểm danh',
              onTap: () => widget.onNavigateTab('class')),
          _ActionCard(
              icon: Icons.assignment,
              label: 'Bài tập',
              onTap: () => widget.onNavigateTab('class')),
          _ActionCard(
              icon: Icons.event_busy,
              label: 'Duyệt đơn',
              onTap: () => widget.onNavigateTab('class')),
          _ActionCard(
              icon: Icons.campaign,
              label: 'Gửi TB',
              onTap: () => widget.onNavigateTab('class')),
        ];
      case 'Admin':
        return [
          _ActionCard(
              icon: Icons.manage_accounts,
              label: 'Quản lý',
              onTap: () => _todo('Quản lý')),
          notiCard,
          _ActionCard(
              icon: Icons.bar_chart,
              label: 'Báo cáo',
              onTap: () => _todo('Báo cáo')),
          _ActionCard(
              icon: Icons.campaign,
              label: 'Bảng tin',
              onTap: () => _todo('Bảng tin')),
        ];
      default: // Parent, Student
        return [
          _ActionCard(
              icon: Icons.school,
              label: 'Học tập',
              onTap: () => widget.onNavigateTab('study')),
          notiCard,
          _ActionCard(
              icon: Icons.article,
              label: 'Bảng tin',
              onTap: () => widget.onNavigateTab('announcements')),
          _ActionCard(
              icon: Icons.event_busy,
              label: 'Đơn xin nghỉ',
              // Màn full-screen (không phải tab) → dùng Navigator.push.
              onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => LeaveRequestView(user: widget.user),
                    ),
                  )),
          _ActionCard(
              icon: Icons.payments,
              label: 'Học phí',
              onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => FeeView(user: widget.user),
                    ),
                  )),
        ];
    }
  }

  /// Chức năng chưa làm (sẽ hoàn thiện Ngày 15–20).
  void _todo(String name) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('"$name" đang phát triển.')),
    );
  }

  /// Phần "Bảng tin gần đây".
  Widget _recentAnnouncements() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text('Bảng tin gần đây',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            TextButton(
              onPressed: () => widget.onNavigateTab('announcements'),
              child: const Text('Xem tất cả'),
            ),
          ],
        ),
        if (_loading)
          const Padding(
            padding: EdgeInsets.all(24),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (_recent.isEmpty)
          const Padding(
            padding: EdgeInsets.all(16),
            child: Text('Chưa có thông báo nào.'),
          )
        else
          ..._recent.map((a) => Card(
                child: ListTile(
                  leading: Icon(
                    a.isGlobal ? Icons.campaign : Icons.class_,
                    color: AppColors.primary,
                  ),
                  title: Text(a.title,
                      maxLines: 1, overflow: TextOverflow.ellipsis),
                  subtitle: Text(FormatUtils.timeAgo(a.createdAt),
                      style: const TextStyle(fontSize: 12)),
                  onTap: () => Navigator.push(
                    context,
                    MaterialPageRoute(
                        builder: (_) => AnnouncementDetailView(item: a)),
                  ),
                ),
              )),
      ],
    );
  }
}

/// 1 thẻ lối tắt trong lưới (icon + nhãn + badge tùy chọn).
class _ActionCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final int badge; // >0 thì hiện số ở góc icon
  final VoidCallback onTap;
  const _ActionCard({
    required this.icon,
    required this.label,
    required this.onTap,
    this.badge = 0,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              badge > 0
                  ? Badge.count(count: badge, child: Icon(icon, size: 32, color: AppColors.primary))
                  : Icon(icon, size: 32, color: AppColors.primary),
              const SizedBox(height: 8),
              Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
            ],
          ),
        ),
      ),
    );
  }
}
