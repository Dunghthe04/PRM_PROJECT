import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../common/list_load_state.dart';
import '../controller/announcement_controller.dart';
import '../controller/notification_controller.dart';
import '../model/announcement_model.dart';
import '../model/notification_model.dart';
import 'announcement_view.dart';

/// Tab Trung tâm thông báo (FR1.4).
/// [showSentHistory]: true với GV → 2 sub-tab Đã nhận / Đã gửi.
class NotificationTab extends StatefulWidget {
  final VoidCallback? onUnreadChanged;
  final bool showSentHistory;
  final bool isTabActive;
  const NotificationTab({
    super.key,
    this.onUnreadChanged,
    this.showSentHistory = false,
    this.isTabActive = true,
  });

  @override
  State<NotificationTab> createState() => _NotificationTabState();
}

class _NotificationTabState extends State<NotificationTab>
    with SingleTickerProviderStateMixin {
  TabController? _tabs;
  final _inboxKey = GlobalKey<_InboxListState>();
  final _sentListKey = GlobalKey<_SentListState>();

  @override
  void initState() {
    super.initState();
    if (widget.showSentHistory) {
      _tabs = TabController(length: 2, vsync: this);
      _tabs!.addListener(_onTabChanged);
    }
  }

  void _onTabChanged() {
    if (_tabs == null || _tabs!.indexIsChanging) return;
    if (_tabs!.index == 1) {
      _sentListKey.currentState?.loadList();
    }
  }

  @override
  void didUpdateWidget(covariant NotificationTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!oldWidget.isTabActive && widget.isTabActive) {
      _inboxKey.currentState?.loadList();
      if (_tabs?.index == 1) {
        _sentListKey.currentState?.loadList();
      }
    }
  }

  @override
  void dispose() {
    _tabs?.removeListener(_onTabChanged);
    _tabs?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    // HS/PH/Admin: chỉ hộp thư nhận.
    if (!widget.showSentHistory) {
      return _InboxList(
        key: _inboxKey,
        onUnreadChanged: widget.onUnreadChanged,
      );
    }

    // GV: Đã nhận + Đã gửi (lịch sử TB lớp).
    return Column(
      children: [
        TabBar(
          controller: _tabs,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textGrey,
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(text: 'Đã nhận'),
            Tab(text: 'Đã gửi'),
          ],
        ),
        Expanded(
          child: TabBarView(
            controller: _tabs,
            children: [
              _InboxList(
                key: _inboxKey,
                onUnreadChanged: widget.onUnreadChanged,
              ),
              _SentList(key: _sentListKey),
            ],
          ),
        ),
      ],
    );
  }
}

/// Danh sách thông báo in-app đã nhận.
class _InboxList extends StatefulWidget {
  final VoidCallback? onUnreadChanged;
  const _InboxList({super.key, this.onUnreadChanged});

  @override
  State<_InboxList> createState() => _InboxListState();
}

class _InboxListState extends State<_InboxList> {
  final NotificationController _controller = NotificationController();
  final _state = ListLoadState<NotificationModel>();

  @override
  void initState() {
    super.initState();
    _loadList();
  }

  /// Public để tab cha gọi khi tab được focus lại.
  Future<void> loadList() => _loadList();

  Future<void> _loadList() async {
    await reloadList(
      setState: setState,
      mounted: () => mounted,
      state: _state,
      fetch: () => _controller.getList(),
    );
    widget.onUnreadChanged?.call();
  }

  Future<void> _onTapItem(NotificationModel item) async {
    if (!item.isRead) {
      final ok = await _controller.markRead(item.id);
      if (ok && mounted) {
        setState(() => item.isRead = true);
        widget.onUnreadChanged?.call();
      }
    }
    if (!mounted) return;
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => NotificationDetailView(item: item)),
    );
  }

  Future<void> _markAll() async {
    final ok = await _controller.markAllRead();
    if (ok && mounted) await _loadList();
  }

  Widget _buildListBody() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    final (list, error) = (_state.items, _state.error ?? 'Không tải được dữ liệu.');

    if (_state.error != null) {
      return _ErrorRetry(message: error, onRetry: _loadList);
    }

    if (list == null || list.isEmpty) {
      return RefreshIndicator(
        onRefresh: _loadList,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Center(child: Text('Chưa có thông báo nào.')),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadList,
      child: ListView.separated(
        itemCount: list.length,
        separatorBuilder: (_, _) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final item = list[index];
          return ListTile(
            leading: Icon(
              item.isRead
                  ? Icons.notifications_none
                  : Icons.notifications_active,
              color:
                  item.isRead ? AppColors.textGrey : AppColors.primary,
            ),
            title: Text(
              item.title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontWeight:
                    item.isRead ? FontWeight.normal : FontWeight.bold,
              ),
            ),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.message,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text(
                  FormatUtils.timeAgo(item.createdAt),
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textGrey,
                  ),
                ),
              ],
            ),
            trailing: Icon(
              Icons.chevron_right,
              color: Colors.grey.shade400,
            ),
            isThreeLine: true,
            onTap: () => _onTapItem(item),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _markAll,
            icon: const Icon(Icons.done_all, size: 18),
            label: const Text('Đánh dấu đã đọc tất cả'),
          ),
        ),
        Expanded(child: _buildListBody()),
      ],
    );
  }
}

/// Lịch sử thông báo lớp do GV đã gửi (GET /announcements/mine).
class _SentList extends StatefulWidget {
  const _SentList({super.key});

  @override
  State<_SentList> createState() => _SentListState();
}

class _SentListState extends State<_SentList> {
  final AnnouncementController _controller = AnnouncementController();
  final _state = ListLoadState<AnnouncementModel>();

  @override
  void initState() {
    super.initState();
    loadList();
  }

  /// Public để tab cha gọi khi chuyển sang tab Đã gửi.
  Future<void> loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _controller.getMine(),
      );

  @override
  Widget build(BuildContext context) {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    final (list, error) =
        (_state.items, _state.error ?? 'Không tải được dữ liệu.');

    if (_state.error != null) {
      return _ErrorRetry(message: error, onRetry: loadList);
    }

    if (list == null || list.isEmpty) {
      return RefreshIndicator(
        onRefresh: loadList,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Center(child: Text('Bạn chưa gửi thông báo nào.')),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: loadList,
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
                            item.isGlobal ? Icons.campaign : Icons.send,
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
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        if (item.isGlobal)
                          const _Chip(
                            icon: Icons.campaign,
                            label: 'Toàn trường',
                          )
                        else ...[
                          _Chip(
                            icon: Icons.class_,
                            label: item.targetClassName ?? 'Lớp',
                          ),
                          _Chip(
                            icon: Icons.menu_book,
                            label: (item.subjectName?.isNotEmpty ?? false)
                                ? item.subjectName!
                                : 'Môn học',
                          ),
                        ],
                      ],
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
  }
}

class _Chip extends StatelessWidget {
  final IconData icon;
  final String label;
  const _Chip({required this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.25)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: AppColors.primary),
          const SizedBox(width: 4),
          Text(
            label,
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color: AppColors.primaryDark,
            ),
          ),
        ],
      ),
    );
  }
}

/// Xem nội dung thông báo đầy đủ (có thể dài).
class NotificationDetailView extends StatelessWidget {
  final NotificationModel item;
  const NotificationDetailView({super.key, required this.item});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Chi tiết thông báo'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              item.title,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              FormatUtils.dateTime(item.createdAt),
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
            const Divider(height: 28),
            Text(
              item.message,
              style: const TextStyle(fontSize: 16, height: 1.55),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorRetry extends StatelessWidget {
  final String message;
  final Future<void> Function() onRetry;
  const _ErrorRetry({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48, color: AppColors.danger),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Text(message, textAlign: TextAlign.center),
          ),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh),
            label: const Text('Thử lại'),
          ),
        ],
      ),
    );
  }
}
