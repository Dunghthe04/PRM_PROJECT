import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../common/app_colors.dart';
import '../common/app_config.dart';
import '../common/format_utils.dart';
import '../controller/file_controller.dart';
import '../common/list_load_state.dart';
import '../controller/leave_request_controller.dart';
import '../model/leave_request_model.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';

/// Màn Đơn xin nghỉ (FR2.5) — full-screen, mở từ lối tắt Dashboard.
///
/// - Học sinh: chỉ xem đơn của mình.
/// - Phụ huynh: xem/tạo/hủy đơn cho con đang chọn (ParentSession).
class LeaveRequestView extends StatefulWidget {
  final UserModel user;
  const LeaveRequestView({super.key, required this.user});

  @override
  State<LeaveRequestView> createState() => _LeaveRequestViewState();
}

class _LeaveRequestViewState extends State<LeaveRequestView> {
  final LeaveRequestController _controller = LeaveRequestController();
  final _state = ListLoadState<LeaveRequestModel>();

  bool get _isParent => widget.user.role == 'Parent';

  @override
  void initState() {
    super.initState();
    _loadList();
  }

  Future<void> _loadList() => reloadList(
        setState: setState,
        mounted: () => mounted,
        state: _state,
        fetch: () => _controller.getMy(),
      );

  /// Con đang chọn (chỉ dùng cho PH). Null nếu HS hoặc chưa chọn con.
  UserModel? get _selectedChild =>
      _isParent ? ParentSession.instance.selectedChild.value : null;

  /// Lọc danh sách theo con đang chọn (chỉ với PH — vì API trả tất cả các con).
  List<LeaveRequestModel> _applyChildFilter(List<LeaveRequestModel> list) {
    if (!_isParent) return list;
    final child = _selectedChild;
    if (child == null) return list;
    return list.where((e) => e.studentId == child.id).toList();
  }

  /// Mở form tạo đơn; nếu tạo thành công thì tải lại danh sách.
  Future<void> _openCreate() async {
    // PH bắt buộc phải có con đang chọn để tạo đơn.
    if (_isParent && _selectedChild == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chưa có học sinh để tạo đơn.')),
      );
      return;
    }

    final created = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _CreateLeaveRequestPage(
          studentId: _selectedChild!.id,
          studentName: _selectedChild!.fullName,
        ),
      ),
    );
    if (created == true) await _loadList();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Đơn xin nghỉ'),
      ),
      body: _isParent
          // PH: nghe đổi con để lọc lại danh sách theo con.
          ? ValueListenableBuilder<UserModel?>(
              valueListenable: ParentSession.instance.selectedChild,
              builder: (context, _, _) => _buildBody(),
            )
          : _buildBody(),
      floatingActionButton: _isParent
          ? FloatingActionButton.extended(
              onPressed: _openCreate,
              icon: const Icon(Icons.add),
              label: const Text('Tạo đơn'),
            )
          : null,
    );
  }

  Widget _buildBody() {
    if (_state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_state.error != null) {
      return _CenteredRetry(message: _state.error!, onRetry: _loadList);
    }

    final filtered = _applyChildFilter(_state.items ?? []);

    if (filtered.isEmpty) {
      return RefreshIndicator(
        onRefresh: _loadList,
        child: ListView(
          children: const [
            SizedBox(height: 120),
            Center(child: Text('Chưa có đơn xin nghỉ nào.')),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadList,
      child: ListView.separated(
        padding: const EdgeInsets.all(12),
        itemCount: filtered.length,
        separatorBuilder: (_, _) => const SizedBox(height: 8),
        itemBuilder: (context, index) => _LeaveCard(
          item: filtered[index],
          showStudent: _isParent,
          onTap: _openDetail,
        ),
      ),
    );
  }

  /// Mở chi tiết đơn; nếu hủy đơn thành công thì tải lại danh sách.
  Future<void> _openDetail(LeaveRequestModel item) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _LeaveDetailPage(
          item: item,
          canCancel: _isParent,
        ),
      ),
    );
    if (changed == true) await _loadList();
  }
}

/// Màu badge theo trạng thái đơn.
Color _statusColor(String status) {
  switch (status) {
    case 'Approved':
      return AppColors.success;
    case 'Rejected':
      return AppColors.danger;
    default: // Pending
      return AppColors.primary;
  }
}

/// 1 thẻ đơn xin nghỉ trong danh sách.
class _LeaveCard extends StatelessWidget {
  final LeaveRequestModel item;
  final bool showStudent; // PH: hiện tên con
  final void Function(LeaveRequestModel) onTap;
  const _LeaveCard({
    required this.item,
    required this.showStudent,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppColors.primary.withValues(alpha: 0.15),
          child: const Icon(Icons.event_busy, color: AppColors.primary),
        ),
        title: Text(
          'Nghỉ ngày ${FormatUtils.date(item.date)}',
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (showStudent) Text('HS: ${item.studentName}'),
            Text(item.reason, maxLines: 1, overflow: TextOverflow.ellipsis),
          ],
        ),
        isThreeLine: showStudent,
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: _statusColor(item.status).withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            item.statusLabel,
            style: TextStyle(
              color: _statusColor(item.status),
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

/// Màn chi tiết 1 đơn xin nghỉ + nút Hủy (khi còn Chờ duyệt).
/// Trả về `true` qua Navigator.pop khi hủy thành công.
class _LeaveDetailPage extends StatefulWidget {
  final LeaveRequestModel item;
  final bool canCancel;
  const _LeaveDetailPage({required this.item, required this.canCancel});

  @override
  State<_LeaveDetailPage> createState() => _LeaveDetailPageState();
}

class _LeaveDetailPageState extends State<_LeaveDetailPage> {
  final LeaveRequestController _controller = LeaveRequestController();
  bool _cancelling = false;

  /// Hỏi xác nhận rồi gọi API hủy đơn.
  Future<void> _cancel() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hủy đơn'),
        content: const Text('Bạn chắc chắn muốn hủy đơn xin nghỉ này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Không'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Hủy đơn'),
          ),
        ],
      ),
    );
    if (confirm != true) return;

    setState(() => _cancelling = true);
    final (ok, msg) = await _controller.cancel(widget.item.id);
    if (!mounted) return;
    setState(() => _cancelling = false);
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    if (ok) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết đơn nghỉ')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Trạng thái nổi bật ở đầu.
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: _statusColor(item.status).withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(
                item.statusLabel,
                style: TextStyle(
                  color: _statusColor(item.status),
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(height: 16),
            _row('Học sinh', item.studentName),
            _row('Lớp', item.className),
            _row('Ngày nghỉ', FormatUtils.date(item.date)),
            _row('Người nộp', item.submittedByName),
            _row('Nộp lúc', FormatUtils.dateTime(item.createdAt)),
            const Divider(height: 28),
            const Text('Lý do', style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text(item.reason, style: const TextStyle(fontSize: 16)),
            // Kết quả xử lý của GV (nếu đã duyệt/từ chối).
            if (item.approvedByTeacherName != null) ...[
              const Divider(height: 28),
              _row('GV xử lý', item.approvedByTeacherName!),
              if (item.rejectionReason != null)
                _row('Lý do từ chối', item.rejectionReason!),
            ],
            // Ảnh y tế đính kèm (nếu có).
            if (item.medicalCertificateUrl != null) ...[
              const Divider(height: 28),
              const Text('Ảnh y tế',
                  style: TextStyle(fontWeight: FontWeight.bold)),
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.network(
                  AppConfig.mediaUrl(item.medicalCertificateUrl!),
                  // Hiện icon lỗi nếu không tải được ảnh.
                  errorBuilder: (_, _, _) => const Padding(
                    padding: EdgeInsets.all(16),
                    child: Text('Không tải được ảnh.'),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
      // Chỉ PH được hủy khi đơn còn Chờ duyệt.
      floatingActionButton: widget.canCancel && item.isPending
          ? FloatingActionButton.extended(
              backgroundColor: AppColors.danger,
              onPressed: _cancelling ? null : _cancel,
              icon: const Icon(Icons.delete_outline),
              label: Text(_cancelling ? 'Đang hủy...' : 'Hủy đơn'),
            )
          : null,
    );
  }

  /// 1 dòng nhãn : giá trị.
  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(label,
                style: const TextStyle(color: AppColors.textGrey)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(fontWeight: FontWeight.w500)),
          ),
        ],
      ),
    );
  }
}

/// Form tạo đơn xin nghỉ mới (chỉ PH). Trả về `true` khi tạo thành công.
class _CreateLeaveRequestPage extends StatefulWidget {
  final int studentId;
  final String studentName;
  const _CreateLeaveRequestPage({
    required this.studentId,
    required this.studentName,
  });

  @override
  State<_CreateLeaveRequestPage> createState() =>
      _CreateLeaveRequestPageState();
}

class _CreateLeaveRequestPageState extends State<_CreateLeaveRequestPage> {
  final LeaveRequestController _leaveController = LeaveRequestController();
  final FileController _fileController = FileController();
  final ImagePicker _picker = ImagePicker();
  final TextEditingController _reasonCtrl = TextEditingController();

  DateTime? _date; // ngày xin nghỉ đã chọn
  XFile? _pickedImage; // ảnh y tế đã chọn (chưa upload)
  bool _submitting = false;

  @override
  void dispose() {
    _reasonCtrl.dispose();
    super.dispose();
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  /// Mở lịch chọn ngày nghỉ (chỉ cho từ hôm nay trở đi).
  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date ?? now,
      firstDate: now,
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  /// Chọn ảnh y tế từ thư viện.
  Future<void> _pickImage() async {
    final img = await _picker.pickImage(
      source: ImageSource.gallery,
      imageQuality: 70, // nén nhẹ cho đỡ nặng
    );
    if (img != null) setState(() => _pickedImage = img);
  }

  /// Kiểm tra dữ liệu → (nếu có ảnh) upload → tạo đơn.
  Future<void> _submit() async {
    if (_date == null) {
      _toast('Vui lòng chọn ngày xin nghỉ.');
      return;
    }
    final reason = _reasonCtrl.text.trim();
    if (reason.isEmpty) {
      _toast('Vui lòng nhập lý do.');
      return;
    }

    setState(() => _submitting = true);

    // 1) Upload ảnh y tế trước (nếu có chọn).
    String? medicalUrl;
    if (_pickedImage != null) {
      final (url, uploadErr) =
          await _fileController.upload(_pickedImage!.path, folder: 'medical');
      if (uploadErr != null) {
        if (!mounted) return;
        setState(() => _submitting = false);
        _toast('Lỗi tải ảnh: $uploadErr');
        return;
      }
      medicalUrl = url;
    }

    // 2) Tạo đơn với URL ảnh (nếu có).
    final (created, err) = await _leaveController.create(
      date: _date!,
      reason: reason,
      studentId: widget.studentId,
      medicalCertificateUrl: medicalUrl,
    );

    if (!mounted) return;
    setState(() => _submitting = false);

    if (err != null) {
      _toast(err);
    } else {
      _toast('Tạo đơn thành công.');
      Navigator.pop(context, true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Tạo đơn xin nghỉ')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Text(
                'Học sinh: ${widget.studentName}',
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
            ),

            // Chọn ngày nghỉ.
            const Text('Ngày xin nghỉ',
                style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            OutlinedButton.icon(
              onPressed: _pickDate,
              icon: const Icon(Icons.calendar_today),
              label: Text(
                _date == null ? 'Chọn ngày' : FormatUtils.date(_date!),
              ),
            ),
            const SizedBox(height: 16),

            // Lý do.
            const Text('Lý do', style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            TextField(
              controller: _reasonCtrl,
              maxLines: 3,
              decoration: const InputDecoration(
                hintText: 'Nhập lý do xin nghỉ...',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 16),

            // Ảnh y tế (tùy chọn).
            const Text('Ảnh y tế (tùy chọn)',
                style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            OutlinedButton.icon(
              onPressed: _pickImage,
              icon: const Icon(Icons.image),
              label: Text(_pickedImage == null ? 'Chọn ảnh' : 'Đổi ảnh'),
            ),
            if (_pickedImage != null) ...[
              const SizedBox(height: 8),
              Text(
                'Đã chọn: ${_pickedImage!.name}',
                style: const TextStyle(color: AppColors.textGrey),
              ),
            ],
            const SizedBox(height: 24),

            // Nút gửi.
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _submitting ? null : _submit,
                child: Text(_submitting ? 'Đang gửi...' : 'Gửi đơn'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Widget lỗi + nút thử lại.
class _CenteredRetry extends StatelessWidget {
  final String message;
  final Future<void> Function() onRetry;
  const _CenteredRetry({required this.message, required this.onRetry});

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
