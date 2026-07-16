import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/notification_controller.dart';
import '../model/notification_model.dart';

/// Tab Trung tâm thông báo (FR1.4).
class NotificationTab extends StatefulWidget {
  final VoidCallback? onUnreadChanged;
  const NotificationTab({super.key, this.onUnreadChanged});

  @override
  State<NotificationTab> createState() => _NotificationTabState();
}

class _NotificationTabState extends State<NotificationTab> {
  final NotificationController _controller = NotificationController();
  late Future<(List<NotificationModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getList();
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getList());
    await _future;
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
    if (ok && mounted) await _reload();
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
        Expanded(
          child: FutureBuilder<(List<NotificationModel>?, String?)>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }

              final (list, error) =
                  snapshot.data ?? (null, 'Không tải được dữ liệu.');

              if (error != null) {
                return _ErrorRetry(message: error, onRetry: _reload);
              }

              if (list == null || list.isEmpty) {
                return RefreshIndicator(
                  onRefresh: _reload,
                  child: ListView(
                    children: const [
                      SizedBox(height: 120),
                      Center(child: Text('Chưa có thông báo nào.')),
                    ],
                  ),
                );
              }

              return RefreshIndicator(
                onRefresh: _reload,
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
                        color: item.isRead
                            ? AppColors.textGrey
                            : AppColors.primary,
                      ),
                      title: Text(
                        item.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          fontWeight: item.isRead
                              ? FontWeight.normal
                              : FontWeight.bold,
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
            },
          ),
        ),
      ],
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
