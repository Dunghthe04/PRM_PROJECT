import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/assignment_controller.dart';
import '../model/assignment_model.dart';

/// Màn Bài tập (FR2.4): danh sách To-Do / Done / Overdue + nộp bài.
///
/// - [studentId]: null nếu là HS; id con nếu là PH (chỉ xem).
/// - [canSubmit]: true = cho phép nộp bài (chỉ Học sinh). PH chỉ theo dõi.
class AssignmentsView extends StatefulWidget {
  final int? studentId;
  final bool canSubmit;
  const AssignmentsView({super.key, this.studentId, required this.canSubmit});

  @override
  State<AssignmentsView> createState() => _AssignmentsViewState();
}

class _AssignmentsViewState extends State<AssignmentsView> {
  final AssignmentController _controller = AssignmentController();
  late Future<(List<AssignmentModel>?, String?)> _future;

  // Bộ lọc trạng thái đang chọn: null = Tất cả.
  String? _statusFilter;

  // Các lựa chọn lọc: (giá trị API, nhãn hiển thị).
  static const List<(String?, String)> _filters = [
    (null, 'Tất cả'),
    ('ToDo', 'Cần làm'),
    ('Done', 'Đã nộp'),
    ('Overdue', 'Quá hạn'),
  ];

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  @override
  void didUpdateWidget(covariant AssignmentsView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.studentId != widget.studentId) {
      setState(() => _future = _load());
    }
  }

  Future<(List<AssignmentModel>?, String?)> _load() {
    return _controller.getMyAssignments(
      status: _statusFilter,
      studentId: widget.studentId,
    );
  }

  Future<void> _reload() async {
    setState(() => _future = _load());
    await _future;
  }

  /// Đổi bộ lọc trạng thái → tải lại danh sách.
  void _selectFilter(String? status) {
    setState(() {
      _statusFilter = status;
      _future = _load();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _buildFilterBar(),
        Expanded(
          child: FutureBuilder<(List<AssignmentModel>?, String?)>(
            future: _future,
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }

              final (list, error) =
                  snapshot.data ?? (null, 'Không tải được dữ liệu.');

              if (error != null) {
                return _buildRetry(error);
              }

              if (list == null || list.isEmpty) {
                return RefreshIndicator(
                  onRefresh: _reload,
                  child: ListView(
                    children: const [
                      SizedBox(height: 120),
                      Center(child: Text('Không có bài tập nào.')),
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
                      _AssignmentCard(item: list[index], onTap: _openDetail),
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  /// Thanh chip lọc trạng thái.
  Widget _buildFilterBar() {
    return SizedBox(
      height: 52,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        children: _filters.map((f) {
          final selected = _statusFilter == f.$1;
          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              label: Text(f.$2),
              selected: selected,
              onSelected: (_) => _selectFilter(f.$1),
              selectedColor: AppColors.primary.withValues(alpha: 0.18),
            ),
          );
        }).toList(),
      ),
    );
  }

  Widget _buildRetry(String message) {
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
            onPressed: _reload,
            icon: const Icon(Icons.refresh),
            label: const Text('Thử lại'),
          ),
        ],
      ),
    );
  }

  /// Mở màn chi tiết bài tập. Nếu HS nộp bài thành công → tải lại DS.
  Future<void> _openDetail(AssignmentModel item) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => AssignmentDetailView(
          assignmentId: item.id,
          canSubmit: widget.canSubmit,
        ),
      ),
    );
    if (changed == true) _reload();
  }
}

/// 1 thẻ bài tập trong danh sách.
class _AssignmentCard extends StatelessWidget {
  final AssignmentModel item;
  final void Function(AssignmentModel) onTap;
  const _AssignmentCard({required this.item, required this.onTap});

  /// Màu badge theo trạng thái: ToDo cam, Done xanh, Overdue đỏ.
  Color _statusColor() {
    switch (item.status) {
      case 'Done':
        return AppColors.success;
      case 'Overdue':
        return AppColors.danger;
      default:
        return AppColors.primary;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppColors.primary.withValues(alpha: 0.15),
          child: const Icon(Icons.assignment, color: AppColors.primary),
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
            Text('${item.subjectName} • ${item.className}'),
            const SizedBox(height: 2),
            Text(
              'Hạn: ${FormatUtils.dateTime(item.dueDate)}',
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey),
            ),
          ],
        ),
        isThreeLine: true,
        trailing: item.status == null
            ? null
            : Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: _statusColor().withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  item.statusLabel,
                  style: TextStyle(
                    color: _statusColor(),
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
        onTap: () => onTap(item),
      ),
    );
  }
}

/// Màn chi tiết 1 bài tập + (nếu là HS) nút Nộp bài.
///
/// Trả về `true` qua Navigator.pop khi nộp bài thành công (để DS làm mới).
class AssignmentDetailView extends StatefulWidget {
  final int assignmentId;
  final bool canSubmit;
  const AssignmentDetailView({
    super.key,
    required this.assignmentId,
    required this.canSubmit,
  });

  @override
  State<AssignmentDetailView> createState() => _AssignmentDetailViewState();
}

class _AssignmentDetailViewState extends State<AssignmentDetailView> {
  final AssignmentController _controller = AssignmentController();
  late Future<(AssignmentModel?, String?)> _future;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    _future = _controller.getById(widget.assignmentId);
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  /// Mở dialog nhập link/file rồi gọi API nộp bài.
  Future<void> _openSubmitDialog(AssignmentModel item) async {
    final linkCtrl = TextEditingController();
    final fileCtrl = TextEditingController();

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Nộp bài'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: linkCtrl,
              decoration: const InputDecoration(
                labelText: 'Link bài làm',
                hintText: 'https://drive.google.com/...',
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: fileCtrl,
              decoration: const InputDecoration(
                labelText: 'URL file (tùy chọn)',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Nộp'),
          ),
        ],
      ),
    );

    if (ok != true) return;

    final link = linkCtrl.text.trim();
    final file = fileCtrl.text.trim();
    // Cần ít nhất 1 trong 2 (link hoặc file).
    if (link.isEmpty && file.isEmpty) {
      _toast('Vui lòng nhập link hoặc URL file bài làm.');
      return;
    }

    setState(() => _submitting = true);
    final (sub, error) = await _controller.submit(
      assignmentId: item.id,
      linkUrl: link.isEmpty ? null : link,
      fileUrl: file.isEmpty ? null : file,
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (error != null) {
      _toast(error);
    } else {
      _toast(sub!.isLate ? 'Đã nộp (trễ hạn).' : 'Nộp bài thành công.');
      Navigator.pop(context, true); // báo màn trước làm mới danh sách
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết bài tập')),
      body: FutureBuilder<(AssignmentModel?, String?)>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: CircularProgressIndicator());
          }

          final (item, error) =
              snapshot.data ?? (null, 'Không tải được dữ liệu.');

          if (item == null) {
            return Center(child: Text(error ?? 'Lỗi không xác định'));
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                Text('${item.subjectName} • ${item.className}',
                    style: const TextStyle(color: AppColors.textGrey)),
                const SizedBox(height: 4),
                Text('GV: ${item.teacherName}',
                    style: const TextStyle(color: AppColors.textGrey)),
                const SizedBox(height: 4),
                Text(
                  'Hạn nộp: ${FormatUtils.dateTime(item.dueDate)} • Điểm tối đa: ${item.maxScore.toStringAsFixed(0)}',
                  style: const TextStyle(color: AppColors.textGrey),
                ),
                const Divider(height: 32),
                const Text('Đề bài',
                    style: TextStyle(fontWeight: FontWeight.bold)),
                const SizedBox(height: 6),
                Text(
                  item.description.isEmpty ? '(Không có mô tả)' : item.description,
                  style: const TextStyle(fontSize: 16, height: 1.5),
                ),
              ],
            ),
          );
        },
      ),
      // Nút nộp bài chỉ hiện với Học sinh.
      floatingActionButton: widget.canSubmit
          ? FutureBuilder<(AssignmentModel?, String?)>(
              future: _future,
              builder: (context, snapshot) {
                final item = snapshot.data?.$1;
                if (item == null) return const SizedBox.shrink();
                return FloatingActionButton.extended(
                  onPressed:
                      _submitting ? null : () => _openSubmitDialog(item),
                  icon: const Icon(Icons.upload_file),
                  label: Text(_submitting ? 'Đang nộp...' : 'Nộp bài'),
                );
              },
            )
          : null,
    );
  }
}
