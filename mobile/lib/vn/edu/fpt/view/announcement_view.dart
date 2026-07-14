import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/announcement_controller.dart';
import '../model/announcement_model.dart';

/// Tab Bảng tin (FR2.2): danh sách thông báo chung, kéo để làm mới,
/// bấm 1 mục để xem chi tiết.
///
/// Là 1 tab nằm trong _MainShell (đã có Scaffold + AppBar) nên widget này
/// KHÔNG tự tạo Scaffold, chỉ trả về phần nội dung.
class AnnouncementTab extends StatefulWidget {
  const AnnouncementTab({super.key});

  @override
  State<AnnouncementTab> createState() => _AnnouncementTabState();
}

class _AnnouncementTabState extends State<AnnouncementTab> {
  final AnnouncementController _controller = AnnouncementController();

  // Future giữ kết quả gọi API (danh sách, lỗi). Gán lại mỗi lần làm mới.
  late Future<(List<AnnouncementModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getList(); // bắt đầu tải khi mở tab
  }

  /// Tải lại danh sách (dùng cho nút thử lại + kéo làm mới).
  Future<void> _reload() async {
    setState(() => _future = _controller.getList());
    await _future; // để RefreshIndicator biết khi nào tải xong
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<(List<AnnouncementModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        // 1) Đang tải
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }

        final (list, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');

        // 2) Lỗi → hiện thông báo + nút thử lại
        if (error != null) {
          return _ErrorRetry(message: error, onRetry: _reload);
        }

        // 3) Rỗng → thông báo trống (vẫn cho kéo làm mới)
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

        // 4) Có dữ liệu → danh sách card
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: list.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) =>
                _AnnouncementCard(item: list[index]),
          ),
        );
      },
    );
  }
}

/// 1 thẻ bảng tin trong danh sách.
class _AnnouncementCard extends StatelessWidget {
  final AnnouncementModel item;
  const _AnnouncementCard({required this.item});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        // Icon phân biệt: toàn trường (campaign) vs theo lớp (class).
        leading: CircleAvatar(
          backgroundColor: AppColors.primary.withValues(alpha: 0.15),
          child: Icon(
            item.isGlobal ? Icons.campaign : Icons.class_,
            color: AppColors.primary,
          ),
        ),
        title: Text(
          item.title,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(item.content, maxLines: 2, overflow: TextOverflow.ellipsis),
            const SizedBox(height: 4),
            Text(
              // Nhãn: toàn trường / tên lớp + thời gian tương đối.
              '${item.isGlobal ? 'Toàn trường' : (item.targetClassName ?? 'Lớp')} • ${FormatUtils.timeAgo(item.createdAt)}',
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
          ],
        ),
        isThreeLine: true,
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => AnnouncementDetailView(item: item)),
        ),
      ),
    );
  }
}

/// Màn chi tiết 1 bảng tin (mở khi bấm vào 1 mục).
/// Đây là màn riêng nên tự tạo Scaffold + AppBar.
/// Public để Dashboard cũng tái sử dụng được.
class AnnouncementDetailView extends StatelessWidget {
  final AnnouncementModel item;
  const AnnouncementDetailView({super.key, required this.item});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết thông báo')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              item.title,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(
                  item.isGlobal ? Icons.campaign : Icons.class_,
                  size: 16,
                  color: AppColors.textGrey,
                ),
                const SizedBox(width: 4),
                Text(
                  item.isGlobal
                      ? 'Toàn trường'
                      : (item.targetClassName ?? 'Theo lớp'),
                  style: const TextStyle(color: AppColors.textGrey),
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Đăng bởi ${item.createdByName} • ${FormatUtils.dateTime(item.createdAt)}',
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
            const Divider(height: 32),
            // Nội dung đầy đủ (có thể dài).
            Text(item.content, style: const TextStyle(fontSize: 16, height: 1.5)),
          ],
        ),
      ),
    );
  }
}

/// Widget hiển thị lỗi + nút thử lại (dùng chung trong tab này).
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
