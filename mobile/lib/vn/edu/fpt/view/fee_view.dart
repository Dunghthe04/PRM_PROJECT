import 'dart:async';

import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:url_launcher/url_launcher.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/fee_controller.dart';
import '../model/fee_model.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';

/// Màn Học phí (FR2.6) — full-screen, mở từ lối tắt Dashboard.
///
/// 2 tab: "Hóa đơn" (xem + thanh toán) và "Lịch sử" (giao dịch).
/// - Học sinh: xem hóa đơn + biên lai (không thanh toán được — do backend
///   chỉ cho Phụ huynh/Admin tạo giao dịch).
/// - Phụ huynh: thanh toán VNPay/PayOS cho con đang chọn.
class FeeView extends StatelessWidget {
  final UserModel user;
  const FeeView({super.key, required this.user});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Học phí'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Hóa đơn'),
              Tab(text: 'Lịch sử'),
            ],
          ),
        ),
        body: TabBarView(
          children: [
            _InvoicesTab(user: user),
            _HistoryTab(user: user),
          ],
        ),
      ),
    );
  }
}

/// Màu badge theo trạng thái hóa đơn.
Color _invoiceColor(FeeInvoiceModel inv) {
  if (inv.isPaid) return AppColors.success;
  if (inv.isOverdue) return AppColors.danger;
  return AppColors.primary;
}

// ─── Tab Hóa đơn ─────────────────────────────────────────────────────────────

class _InvoicesTab extends StatefulWidget {
  final UserModel user;
  const _InvoicesTab({required this.user});

  @override
  State<_InvoicesTab> createState() => _InvoicesTabState();
}

class _InvoicesTabState extends State<_InvoicesTab> {
  final FeeController _controller = FeeController();
  late Future<(List<FeeInvoiceModel>?, String?)> _future;

  bool get _isParent => widget.user.role == 'Parent';

  @override
  void initState() {
    super.initState();
    _future = _controller.getMyInvoices();
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getMyInvoices());
    await _future;
  }

  /// Lọc theo con đang chọn (PH); giữ nguyên với HS.
  List<FeeInvoiceModel> _applyFilter(List<FeeInvoiceModel> list) {
    if (!_isParent) return list;
    final child = ParentSession.instance.selectedChild.value;
    if (child == null) return list;
    return list.where((e) => e.studentId == child.id).toList();
  }

  @override
  Widget build(BuildContext context) {
    // PH: nghe đổi con để lọc lại.
    if (_isParent) {
      return ValueListenableBuilder<UserModel?>(
        valueListenable: ParentSession.instance.selectedChild,
        builder: (context, _, _) => _buildList(),
      );
    }
    return _buildList();
  }

  Widget _buildList() {
    return FutureBuilder<(List<FeeInvoiceModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        final (list, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');
        if (error != null) {
          return _CenteredRetry(message: error, onRetry: _reload);
        }

        final filtered = _applyFilter(list ?? []);
        // Sắp xếp: chưa đóng lên trước, rồi theo hạn gần nhất.
        filtered.sort((a, b) {
          if (a.isPaid != b.isPaid) return a.isPaid ? 1 : -1;
          return a.dueDate.compareTo(b.dueDate);
        });

        if (filtered.isEmpty) {
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Chưa có hóa đơn nào.')),
              ],
            ),
          );
        }

        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: filtered.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) => _InvoiceCard(
              item: filtered[index],
              showStudent: _isParent,
              onTap: _openDetail,
            ),
          ),
        );
      },
    );
  }

  Future<void> _openDetail(FeeInvoiceModel item) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _InvoiceDetailPage(
          item: item,
          canPay: _isParent, // chỉ PH mới thanh toán
        ),
      ),
    );
    if (changed == true) _reload();
  }
}

/// 1 thẻ hóa đơn trong danh sách.
class _InvoiceCard extends StatelessWidget {
  final FeeInvoiceModel item;
  final bool showStudent;
  final void Function(FeeInvoiceModel) onTap;
  const _InvoiceCard({
    required this.item,
    required this.showStudent,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: _invoiceColor(item).withValues(alpha: 0.15),
          child: Icon(Icons.receipt_long, color: _invoiceColor(item)),
        ),
        title: Text(
          item.feeCategoryName,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (showStudent) Text('HS: ${item.studentName}'),
            Text(FormatUtils.currency(item.amount),
                style: const TextStyle(
                    color: AppColors.textDark, fontWeight: FontWeight.w600)),
            Text('Hạn: ${FormatUtils.date(item.dueDate)}',
                style: const TextStyle(fontSize: 12, color: AppColors.textGrey)),
          ],
        ),
        isThreeLine: true,
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: _invoiceColor(item).withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Text(
            item.statusLabel,
            style: TextStyle(
              color: _invoiceColor(item),
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

// ─── Chi tiết hóa đơn + thanh toán ───────────────────────────────────────────

/// Trả về `true` qua Navigator.pop khi hóa đơn có thay đổi (đã/giả lập thanh toán).
class _InvoiceDetailPage extends StatefulWidget {
  final FeeInvoiceModel item;
  final bool canPay;
  const _InvoiceDetailPage({required this.item, required this.canPay});

  @override
  State<_InvoiceDetailPage> createState() => _InvoiceDetailPageState();
}

class _InvoiceDetailPageState extends State<_InvoiceDetailPage> {
  final FeeController _controller = FeeController();
  bool _processing = false;

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  /// Tạo giao dịch PayOS rồi mở thẳng màn QR (bỏ bước chọn cổng).
  Future<void> _startPayment() async {
    setState(() => _processing = true);
    final (result, error) = await _controller.createPayment(
      invoiceId: widget.item.id,
      provider: 'payos',
    );
    if (!mounted) return;
    setState(() => _processing = false);

    if (error != null) {
      _toast(error);
      return;
    }

    // Mở màn QR: hiển thị QR + số tiền + tự poll trạng thái + tự quay về khi đã trả.
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => _PaymentQrPage(result: result!)),
    );
  }

  /// Mở biên lai điện tử (khi đã thanh toán).
  Future<void> _openReceipt() async {
    setState(() => _processing = true);
    final (receipt, error) = await _controller.getReceipt(widget.item.id);
    if (!mounted) return;
    setState(() => _processing = false);
    if (error != null) {
      _toast(error);
      return;
    }
    if (!mounted) return;
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => _ReceiptPage(receipt: receipt!)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    return Scaffold(
      appBar: AppBar(title: const Text('Chi tiết hóa đơn')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Số tiền lớn + trạng thái.
            Text(
              FormatUtils.currency(item.amount),
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
                color: _invoiceColor(item),
              ),
            ),
            const SizedBox(height: 4),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              decoration: BoxDecoration(
                color: _invoiceColor(item).withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(item.statusLabel,
                  style: TextStyle(
                      color: _invoiceColor(item),
                      fontWeight: FontWeight.bold)),
            ),
            const Divider(height: 28),
            _row('Khoản thu', item.feeCategoryName),
            _row('Học sinh', item.studentName),
            _row('Hạn đóng', FormatUtils.date(item.dueDate)),
            if (item.isPaid && item.paidAt != null)
              _row('Đã đóng lúc', FormatUtils.dateTime(item.paidAt!)),
            if (item.paymentMethod != null)
              _row('Phương thức', item.paymentMethod!),
            if (item.note != null && item.note!.isNotEmpty)
              _row('Ghi chú', item.note!),
            const SizedBox(height: 28),

            // Hành động theo trạng thái + vai trò.
            if (item.isPaid)
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: _processing ? null : _openReceipt,
                  icon: const Icon(Icons.receipt),
                  label: const Text('Xem biên lai'),
                ),
              )
            else if (widget.canPay)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: _processing ? null : _startPayment,
                  icon: const Icon(Icons.payment),
                  label: Text(_processing ? 'Đang xử lý...' : 'Thanh toán'),
                ),
              )
            else
              // HS không thanh toán được → gợi ý liên hệ phụ huynh.
              const Text(
                'Vui lòng liên hệ phụ huynh để thanh toán khoản này.',
                style: TextStyle(color: AppColors.textGrey),
              ),
          ],
        ),
      ),
    );
  }

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

// ─── Màn QR thanh toán (poll tự động + tự quay về) ──────────────────────────

/// Hiển thị QR PayOS + tự động kiểm tra trạng thái mỗi vài giây.
/// Khi phát hiện đã thanh toán → báo thành công → tự quay về màn hình chính.
class _PaymentQrPage extends StatefulWidget {
  final PaymentResultModel result;
  const _PaymentQrPage({required this.result});

  @override
  State<_PaymentQrPage> createState() => _PaymentQrPageState();
}

class _PaymentQrPageState extends State<_PaymentQrPage> {
  final FeeController _controller = FeeController();
  Timer? _poller;
  bool _paid = false;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    // Poll trạng thái mỗi 3 giây (webhook/dev-simulate cập nhật DB → app nhận ra).
    _poller = Timer.periodic(const Duration(seconds: 3), (_) => _checkStatus());
  }

  @override
  void dispose() {
    _poller?.cancel();
    super.dispose();
  }

  Future<void> _checkStatus() async {
    if (_paid || !mounted) return;
    final (isPaid, _) = await _controller.getPaymentStatus(widget.result.orderCode);
    if (isPaid == true) _onPaid();
  }

  /// Xử lý khi đã thanh toán: dừng poll, hiện thành công, tự quay về màn chính.
  void _onPaid() {
    if (_paid) return;
    _poller?.cancel();
    setState(() => _paid = true);
    // Chờ 1.5s cho user thấy thông báo rồi bật hết về màn hình chính.
    Future.delayed(const Duration(milliseconds: 1500), () {
      if (mounted) Navigator.popUntil(context, (route) => route.isFirst);
    });
  }

  /// Mở link cổng thanh toán bằng trình duyệt (khi user muốn thanh toán qua web).
  Future<void> _openGateway() async {
    final uri = Uri.parse(widget.result.paymentUrl);
    final ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!ok && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Không mở được link.')));
    }
  }

  /// (DEV) Giả lập đã đóng — tiện test khi chưa có PayOS thật.
  Future<void> _simulate() async {
    setState(() => _busy = true);
    final (ok, msg) = await _controller.simulatePaid(widget.result.orderCode);
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok) {
      _onPaid();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final r = widget.result;
    // Có QR thật (PayOS) thì vẽ QR đó; nếu dev stub thì vẽ tạm QR của link.
    final qrData = (r.qrCode != null && r.qrCode!.isNotEmpty)
        ? r.qrCode!
        : r.paymentUrl;
    final isRealQr = r.qrCode != null && r.qrCode!.isNotEmpty;

    return Scaffold(
      appBar: AppBar(title: Text('Thanh toán ${r.provider}')),
      body: _paid ? _buildSuccess() : _buildQr(qrData, isRealQr),
    );
  }

  Widget _buildSuccess() {
    return const Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.check_circle, color: AppColors.success, size: 88),
          SizedBox(height: 16),
          Text('Thanh toán thành công!',
              style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: AppColors.success)),
          SizedBox(height: 8),
          Text('Đang quay về trang chính...',
              style: TextStyle(color: AppColors.textGrey)),
        ],
      ),
    );
  }

  Widget _buildQr(String qrData, bool isRealQr) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        children: [
          Text(
            FormatUtils.currency(widget.result.amount),
            style: const TextStyle(
                fontSize: 26,
                fontWeight: FontWeight.bold,
                color: AppColors.primary),
          ),
          const SizedBox(height: 4),
          Text('Mã đơn: ${widget.result.orderCode}',
              style: const TextStyle(fontSize: 12, color: AppColors.textGrey)),
          const SizedBox(height: 20),

          // Khung QR.
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
            ),
            child: QrImageView(
              data: qrData,
              version: QrVersions.auto,
              size: 220,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            isRealQr
                ? 'Mở app ngân hàng, quét mã QR để thanh toán.'
                : '(Dev) Chưa cấu hình PayOS thật — dùng nút "Giả lập đã đóng" để test.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textGrey),
          ),
          const SizedBox(height: 20),

          // Đang chờ thanh toán → hiện loader nhỏ.
          const Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
              SizedBox(width: 8),
              Text('Đang chờ thanh toán...'),
            ],
          ),
          const SizedBox(height: 24),

          // Mở cổng qua trình duyệt (tuỳ chọn).
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: _openGateway,
              icon: const Icon(Icons.open_in_new),
              label: const Text('Mở cổng thanh toán'),
            ),
          ),
          const SizedBox(height: 8),
          // (DEV) Giả lập đã đóng.
          SizedBox(
            width: double.infinity,
            child: TextButton(
              onPressed: _busy ? null : _simulate,
              child: Text(_busy ? 'Đang xử lý...' : 'Giả lập đã đóng (Dev)'),
            ),
          ),
        ],
      ),
    );
  }
}

// ─── Biên lai ────────────────────────────────────────────────────────────────

class _ReceiptPage extends StatelessWidget {
  final FeeReceiptModel receipt;
  const _ReceiptPage({required this.receipt});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Biên lai điện tử')),
      body: Center(
        child: Card(
          margin: const EdgeInsets.all(20),
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Center(
                  child: Icon(Icons.verified,
                      color: AppColors.success, size: 56),
                ),
                const Center(
                  child: Text('THANH TOÁN THÀNH CÔNG',
                      style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: AppColors.success)),
                ),
                const Divider(height: 28),
                _row('Số biên lai', receipt.receiptNumber),
                _row('Học sinh', receipt.studentName),
                _row('Khoản thu', receipt.feeCategoryName),
                _row('Số tiền', FormatUtils.currency(receipt.amount)),
                _row('Phương thức', receipt.paymentMethod),
                if (receipt.transactionId != null)
                  _row('Mã giao dịch', receipt.transactionId!),
                _row('Thời điểm', FormatUtils.dateTime(receipt.paidAt)),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(label,
                style: const TextStyle(color: AppColors.textGrey)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(fontWeight: FontWeight.w600)),
          ),
        ],
      ),
    );
  }
}

// ─── Tab Lịch sử ─────────────────────────────────────────────────────────────

class _HistoryTab extends StatefulWidget {
  final UserModel user;
  const _HistoryTab({required this.user});

  @override
  State<_HistoryTab> createState() => _HistoryTabState();
}

class _HistoryTabState extends State<_HistoryTab> {
  final FeeController _controller = FeeController();
  late Future<(List<PaymentTransactionModel>?, String?)> _future;

  bool get _isParent => widget.user.role == 'Parent';

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<(List<PaymentTransactionModel>?, String?)> _load() {
    // PH: lọc theo con đang chọn (nếu có).
    final childId =
        _isParent ? ParentSession.instance.selectedChild.value?.id : null;
    return _controller.getHistory(studentId: childId);
  }

  Future<void> _reload() async {
    setState(() => _future = _load());
    await _future;
  }

  /// Màu theo trạng thái giao dịch.
  Color _statusColor(String status) {
    switch (status) {
      case 'Paid':
        return AppColors.success;
      case 'Failed':
        return AppColors.danger;
      default:
        return AppColors.textGrey;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isParent) {
      return ValueListenableBuilder<UserModel?>(
        valueListenable: ParentSession.instance.selectedChild,
        builder: (context, _, _) {
          // Đổi con → tải lại lịch sử.
          _future = _load();
          return _buildList();
        },
      );
    }
    return _buildList();
  }

  Widget _buildList() {
    return FutureBuilder<(List<PaymentTransactionModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        final (list, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');
        if (error != null) {
          return _CenteredRetry(message: error, onRetry: _reload);
        }
        if (list == null || list.isEmpty) {
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Chưa có giao dịch nào.')),
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
            itemBuilder: (context, index) {
              final t = list[index];
              return Card(
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor:
                        _statusColor(t.status).withValues(alpha: 0.15),
                    child: Icon(Icons.payments, color: _statusColor(t.status)),
                  ),
                  title: Text('${t.provider} • ${FormatUtils.currency(t.amount)}'),
                  subtitle: Text(
                    'Mã: ${t.orderCode}\n${FormatUtils.dateTime(t.createdAt)}',
                    style: const TextStyle(fontSize: 12),
                  ),
                  isThreeLine: true,
                  trailing: Text(
                    t.status,
                    style: TextStyle(
                      color: _statusColor(t.status),
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              );
            },
          ),
        );
      },
    );
  }
}

// ─── Dùng chung ──────────────────────────────────────────────────────────────

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
