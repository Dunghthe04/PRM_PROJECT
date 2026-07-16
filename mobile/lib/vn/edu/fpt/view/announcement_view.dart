import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/announcement_controller.dart';
import '../model/announcement_model.dart';

/// Tab Bảng tin (FR2.2): danh sách + bấm xem chi tiết đầy đủ.
class AnnouncementTab extends StatefulWidget {
  const AnnouncementTab({super.key});

  @override
  State<AnnouncementTab> createState() => _AnnouncementTabState();
}

class _AnnouncementTabState extends State<AnnouncementTab> {
  final AnnouncementController _controller = AnnouncementController();
  late Future<(List<AnnouncementModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getList();
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getList());
    await _future;
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

class _AnnouncementCard extends StatelessWidget {
  final AnnouncementModel item;
  const _AnnouncementCard({required this.item});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => AnnouncementDetailView(item: item)),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                child: Icon(
                  item.isGlobal ? Icons.campaign : Icons.class_,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.title,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 4),
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
                        // Toàn trường: chỉ nhãn phạm vi (không hiện Admin).
                        // Lớp: nhãn lớp + môn (thay cho tên GV).
                        _MetaChip(
                          icon: item.isGlobal ? Icons.campaign : Icons.class_,
                          label: item.isGlobal
                              ? 'Toàn trường'
                              : (item.targetClassName ?? 'Lớp'),
                        ),
                        if (!item.isGlobal)
                          _MetaChip(
                            icon: Icons.menu_book,
                            label: (item.subjectName?.isNotEmpty ?? false)
                                ? item.subjectName!
                                : 'Môn học',
                          ),
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
              Icon(Icons.chevron_right, color: Colors.grey.shade400),
            ],
          ),
        ),
      ),
    );
  }
}

class _MetaChip extends StatelessWidget {
  final IconData icon;
  final String label;
  const _MetaChip({required this.icon, required this.label});

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

/// Chi tiết bảng tin (nội dung đầy đủ).
class AnnouncementDetailView extends StatelessWidget {
  final AnnouncementModel item;
  const AnnouncementDetailView({super.key, required this.item});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Chi tiết bảng tin'),
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
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                _MetaChip(
                  icon: item.isGlobal ? Icons.campaign : Icons.class_,
                  label: item.isGlobal
                      ? 'Toàn trường'
                      : (item.targetClassName ?? 'Theo lớp'),
                ),
                if (!item.isGlobal)
                  _MetaChip(
                    icon: Icons.menu_book,
                    label: (item.subjectName?.isNotEmpty ?? false)
                        ? item.subjectName!
                        : 'Môn học',
                  ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              FormatUtils.dateTime(item.createdAt),
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
            const Divider(height: 32),
            Text(
              item.content,
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
