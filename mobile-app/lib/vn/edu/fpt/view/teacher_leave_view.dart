import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/app_config.dart';
import '../common/format_utils.dart';
import '../common/list_load_state.dart';
import '../controller/leave_request_controller.dart';
import '../model/leave_request_model.dart';

/// Màn GV duyệt đơn xin nghỉ theo lớp (FR3.3).
/// Lọc Pending/Approved/Rejected → Duyệt / Từ chối (có lý do).
class TeacherLeaveReviewPage extends StatefulWidget {
  final int classId;
  final String className;
  const TeacherLeaveReviewPage({
    super.key,
    required this.classId,
    required this.className,
  });

  @override
  State<TeacherLeaveReviewPage> createState() => _TeacherLeaveReviewPageState();
}

class _TeacherLeaveReviewPageState extends State<TeacherLeaveReviewPage> {
  final LeaveRequestController _controller = LeaveRequestController();
  /// null = tất cả; Pending / Approved / Rejected.
  String? _statusFilter = 'Pending';
  final _state = ListLoadState<LeaveRequestModel>();

  @override
  void initState() {
    super.initState();
    _loadList();
  }

  Future<void> _loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _controller.getList(
          classId: widget.classId,
          status: _statusFilter,
        ),
      );

  void _setFilter(String? status) {
    setState(() => _statusFilter = status);
    _loadList();
  }

  Future<void> _approve(LeaveRequestModel item) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Duyệt đơn?'),
        content: Text('${item.studentName}\nNgày: ${FormatUtils.date(item.date)}'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Hủy')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Duyệt')),
        ],
      ),
    );
    if (ok != true) return;
    final (success, msg) = await _controller.approve(item.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) await _loadList();
  }

  Future<void> _reject(LeaveRequestModel item) async {
    final reasonCtrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Từ chối đơn'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${item.studentName} • ${FormatUtils.date(item.date)}'),
            const SizedBox(height: 12),
            TextField(
              controller: reasonCtrl,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'Lý do từ chối',
                border: OutlineInputBorder(),
                alignLabelWithHint: true,
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Hủy')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Từ chối')),
        ],
      ),
    );
    if (ok != true) return;
    final (success, msg) = await _controller.reject(
      item.id,
      reason: reasonCtrl.text.trim().isEmpty ? null : reasonCtrl.text.trim(),
    );
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (success) await _loadList();
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Pending':
        return Colors.orange;
      case 'Approved':
        return Colors.green;
      case 'Rejected':
        return AppColors.danger;
      default:
        return AppColors.textGrey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Đơn nghỉ • ${widget.className}')),
      body: Column(
        children: [
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            child: Row(
              children: [
                _chip('Chờ duyệt', 'Pending'),
                _chip('Đã duyệt', 'Approved'),
                _chip('Từ chối', 'Rejected'),
                _chip('Tất cả', null),
              ],
            ),
          ),
          Expanded(child: _buildList()),
        ],
      ),
    );
  }

  Widget _buildList() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_state.error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_state.error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            ElevatedButton(
              onPressed: _loadList,
              child: const Text('Thử lại'),
            ),
          ],
        ),
      );
    }
    final items = _state.items ?? [];
    if (items.isEmpty) {
      return RefreshIndicator(
        onRefresh: _loadList,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Center(child: Text('Không có đơn nào.')),
          ],
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _loadList,
      child: ListView.separated(
        padding: const EdgeInsets.all(12),
        itemCount: items.length,
        separatorBuilder: (_, _) => const SizedBox(height: 8),
        itemBuilder: (context, i) => _buildCard(items[i]),
      ),
    );
  }

  Widget _chip(String label, String? status) {
    final selected = _statusFilter == status;
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: ChoiceChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => _setFilter(status),
        selectedColor: AppColors.primary.withValues(alpha: 0.2),
      ),
    );
  }

  Widget _buildCard(LeaveRequestModel item) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(item.studentName,
                      style: const TextStyle(
                          fontWeight: FontWeight.bold, fontSize: 16)),
                ),
                Chip(
                  label: Text(item.statusLabel,
                      style: const TextStyle(fontSize: 12, color: Colors.white)),
                  backgroundColor: _statusColor(item.status),
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text('Ngày nghỉ: ${FormatUtils.date(item.date)}'),
            Text('Nộp bởi: ${item.submittedByName} • ${FormatUtils.timeAgo(item.createdAt)}'),
            const SizedBox(height: 8),
            Text(item.reason),
            if (item.medicalCertificateUrl != null) ...[
              const SizedBox(height: 8),
              TextButton.icon(
                onPressed: () {
                  final url = AppConfig.mediaUrl(item.medicalCertificateUrl!);
                  showDialog(
                    context: context,
                    builder: (ctx) => Dialog(
                      child: InteractiveViewer(
                        child: Image.network(url, fit: BoxFit.contain,
                            errorBuilder: (_, _, _) =>
                                const Padding(
                                  padding: EdgeInsets.all(24),
                                  child: Text('Không tải được ảnh.'),
                                )),
                      ),
                    ),
                  );
                },
                icon: const Icon(Icons.image),
                label: const Text('Xem giấy y tế'),
              ),
            ],
            if (item.rejectionReason != null &&
                item.rejectionReason!.isNotEmpty) ...[
              const SizedBox(height: 8),
              Text('Lý do từ chối: ${item.rejectionReason}',
                  style: const TextStyle(color: AppColors.danger)),
            ],
            if (item.isPending) ...[
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => _reject(item),
                      child: const Text('Từ chối'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: () => _approve(item),
                      child: const Text('Duyệt'),
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}
