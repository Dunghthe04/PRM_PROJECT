import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/notification_controller.dart';
import '../model/notification_model.dart';

/// Tab Trung tâm thông báo (FR1.4): danh sách thông báo cá nhân,
/// bấm để đánh dấu đã đọc, nút "đọc tất cả", kéo để làm mới.
///
/// [onUnreadChanged]: gọi lại mỗi khi số chưa đọc có thể thay đổi,
/// để _MainShell cập nhật badge trên bottom navigation.
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

  /// Tải lại danh sách.
  Future<void> _reload() async {
    setState(() => _future = _controller.getList());
    await _future;
    widget.onUnreadChanged?.call(); // badge có thể đổi sau khi tải lại
  }

  /// Bấm 1 thông báo → nếu chưa đọc thì gọi API đánh dấu đã đọc + đổi UI.
  Future<void> _onTapItem(NotificationModel item) async {
    if (item.isRead) return; // đã đọc rồi thì thôi
    final ok = await _controller.markRead(item.id);
    if (ok && mounted) {
      setState(() => item.isRead = true); // đổi tại chỗ (isRead không final)
      widget.onUnreadChanged?.call(); // giảm badge
    }
  }

  /// Đánh dấu tất cả đã đọc.
  Future<void> _markAll() async {
    final ok = await _controller.markAllRead();
    if (ok && mounted) {
      await _reload(); // tải lại để mọi item về trạng thái đã đọc
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Thanh nút "Đánh dấu tất cả đã đọc" ở đầu tab.
        Align(
          alignment: Alignment.centerRight,
          child: TextButton.icon(
            onPressed: _markAll,
            icon: const Icon(Icons.done_all, size: 18),
            label: const Text('Đánh dấu đã đọc tất cả'),
          ),
        ),
        // Danh sách chiếm phần còn lại.
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
                      // Chấm cam = chưa đọc; xám nhạt = đã đọc.
                      leading: Icon(
                        item.isRead
                            ? Icons.notifications_none
                            : Icons.notifications_active,
                        color:
                            item.isRead ? AppColors.textGrey : AppColors.primary,
                      ),
                      title: Text(
                        item.title,
                        style: TextStyle(
                          // Chưa đọc → in đậm cho nổi bật.
                          fontWeight: item.isRead
                              ? FontWeight.normal
                              : FontWeight.bold,
                        ),
                      ),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(item.message),
                          const SizedBox(height: 2),
                          Text(
                            FormatUtils.timeAgo(item.createdAt),
                            style: const TextStyle(
                                fontSize: 12, color: AppColors.textGrey),
                          ),
                        ],
                      ),
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

/// Widget hiển thị lỗi + nút thử lại.
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
