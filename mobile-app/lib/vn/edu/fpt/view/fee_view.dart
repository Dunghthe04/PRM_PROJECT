import 'dart:async';

import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:url_launcher/url_launcher.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/fee_controller.dart';
import '../controller/semester_controller.dart';
import '../model/fee_model.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';

/// Màn Học phí (FR2.6) — danh sách hóa đơn + lọc theo kỳ.
/// - Học sinh: xem hóa đơn + biên lai (không thanh toán).
/// - Phụ huynh: thanh toán VNPay/PayOS cho con đang chọn.
class FeeView extends StatefulWidget {
  final UserModel user;
  const FeeView({super.key, required this.user});

  @override
  State<FeeView> createState() => _FeeViewState();
}

class _FeeViewState extends State<FeeView> {
  final FeeController _controller = FeeController();
  final SemesterController _semestersApi = SemesterController();

  List<FeeInvoiceModel> _all = [];
  List<SemesterItem> _semesters = [];
  /// null = Tất cả kỳ.
  int? _semesterId;
  bool _loading = true;
  String? _error;

  bool get _isParent => widget.user.role == 'Parent';

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final results = await Future.wait([
      _controller.getMyInvoices(),
      _semestersApi.list(),
    ]);
    if (!mounted) return;

    final (invoices, invErr) =
        results[0] as (List<FeeInvoiceModel>?, String?);
    final (semesters, semErr) = results[1] as (List<SemesterItem>?, String?);

    if (invErr != null) {
      setState(() {
        _loading = false;
        _error = invErr;
      });
      return;
    }

    _all = invoices ?? [];
    _semesters = semesters ?? [];
    // Mặc định: kỳ demo / đang diễn ra; không có thì "Tất cả".
    _semesterId = _pickDefaultSemester()?.id;
    setState(() => _loading = false);
    if (semErr != null && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(semErr)));
    }
  }

  SemesterItem? _pickDefaultSemester() {
    if (_semesters.isEmpty) return null;
    final now = DateTime.now();
    final current = _semesters.where((s) => s.contains(now));
    if (current.isNotEmpty) return current.first;
    final demo =
        _semesters.where((s) => s.name.contains('Học kỳ 1 (2026-2027)'));
    if (demo.isNotEmpty) return demo.first;
    return null; // Tất cả
  }

  Future<void> _reload() => _bootstrap();

  /// Lọc theo con (PH) + theo kỳ đã chọn.
  List<FeeInvoiceModel> _filtered() {
    var list = List<FeeInvoiceModel>.from(_all);
    if (_isParent) {
      final child = ParentSession.instance.selectedChild.value;
      if (child != null) {
        list = list.where((e) => e.studentId == child.id).toList();
      }
    }
    if (_semesterId != null) {
      final sem = _semesters.where((s) => s.id == _semesterId).firstOrNull;
      if (sem != null) {
        list = list.where((e) => _matchesSemester(e, sem)).toList();
      }
    }
    list.sort((a, b) {
      if (a.isPaid != b.isPaid) return a.isPaid ? 1 : -1;
      return a.dueDate.compareTo(b.dueDate);
    });
    return list;
  }

  /// Khớp kỳ: hạn đóng nằm trong kỳ, hoặc tên/ghi chú chứa tên kỳ / năm học.
  bool _matchesSemester(FeeInvoiceModel inv, SemesterItem sem) {
    if (sem.contains(inv.dueDate)) return true;
    final hay =
        '${inv.feeCategoryName} ${inv.note ?? ''}'.toLowerCase();
    final name = sem.name.toLowerCase();
    if (hay.contains(name)) return true;
    final year = RegExp(r'\((\d{4}-\d{4})\)').firstMatch(sem.name);
    final hk = RegExp(r'học kỳ\s*(\d)', caseSensitive: false).firstMatch(sem.name);
    if (year != null && hay.contains(year.group(1)!.toLowerCase())) {
      if (hk == null) return true;
      final n = hk.group(1)!;
      return hay.contains('học kỳ $n') ||
          hay.contains('hoc ky $n') ||
          hay.contains('hk$n');
    }
    return false;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Học phí'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: _isParent
          ? ValueListenableBuilder<UserModel?>(
              valueListenable: ParentSession.instance.selectedChild,
              builder: (context, _, _) => _body(),
            )
          : _body(),
    );
  }

  Widget _body() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return _CenteredRetry(message: _error!, onRetry: _reload);
    }

    final filtered = _filtered();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _semesterChips(),
        Expanded(
          child: RefreshIndicator(
            color: AppColors.primary,
            onRefresh: _reload,
            child: filtered.isEmpty
                ? ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: const [
                      SizedBox(height: 120),
                      Center(
                        child: Text(
                          'Không có hóa đơn trong kỳ này.',
                          style: TextStyle(
                            color: AppColors.textDark,
                            fontSize: 15,
                          ),
                        ),
                      ),
                    ],
                  )
                : ListView.separated(
                    padding: const EdgeInsets.fromLTRB(12, 4, 12, 16),
                    itemCount: filtered.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 8),
                    itemBuilder: (context, index) => _InvoiceCard(
                      item: filtered[index],
                      showStudent: _isParent,
                      onTap: _openDetail,
                    ),
                  ),
          ),
        ),
      ],
    );
  }

  Widget _semesterChips() {
    // Chip "Tất cả" + danh sách kỳ.
    final items = <({int? id, String label})>[
      (id: null, label: 'Tất cả'),
      ..._semesters.map((s) => (id: s.id as int?, label: s.name)),
    ];
    if (items.length <= 1) return const SizedBox.shrink();

    return SizedBox(
      height: 48,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 0),
        itemCount: items.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (context, i) {
          final item = items[i];
          final selected = item.id == _semesterId;
          return ChoiceChip(
            label: Text(
              item.label,
              style: TextStyle(
                fontSize: 12,
                color: selected ? AppColors.white : AppColors.textDark,
                fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
              ),
            ),
            selected: selected,
            selectedColor: AppColors.primary,
            backgroundColor: AppColors.white,
            side: BorderSide(
              color: selected ? AppColors.primary : Colors.grey.shade300,
            ),
            onSelected: (_) => setState(() => _semesterId = item.id),
          );
        },
      ),
    );
  }

  Future<void> _openDetail(FeeInvoiceModel item) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => _InvoiceDetailPage(
          item: item,
          canPay: _isParent,
        ),
      ),
    );
    if (changed == true) _reload();
  }
}

/// Màu badge theo trạng thái hóa đơn.
Color _invoiceColor(FeeInvoiceModel inv) {
  if (inv.isPaid) return AppColors.success;
  if (inv.isOverdue) return AppColors.danger;
  return AppColors.primary;
}

/// 1 thẻ hóa đơn — màu chữ rõ trên nền trắng.
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
    final accent = _invoiceColor(item);
    return Card(
      color: AppColors.white,
      elevation: 0.5,
      child: InkWell(
        onTap: () => onTap(item),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                backgroundColor: accent.withValues(alpha: 0.15),
                child: Icon(Icons.receipt_long, color: accent),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Text(
                            item.feeCategoryName,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 15,
                              color: AppColors.textDark,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: accent.withValues(alpha: 0.12),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(
                            item.statusLabel,
                            style: TextStyle(
                              color: accent,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (showStudent) ...[
                      const SizedBox(height: 4),
                      Text(
                        'HS: ${item.studentName}',
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textDark,
                        ),
                      ),
                    ],
                    const SizedBox(height: 4),
                    Text(
                      'Hạn: ${FormatUtils.date(item.dueDate)}',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppColors.textDark,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      FormatUtils.currency(item.amount),
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textDark,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ─── Chi tiết hóa đơn + thanh toán ───────────────────────────────────────────

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

    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => _PaymentQrPage(result: result!)),
    );
  }

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
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Chi tiết hóa đơn'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
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
              const Text(
                'Vui lòng liên hệ phụ huynh để thanh toán khoản này.',
                style: TextStyle(color: AppColors.textDark, fontSize: 14),
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
                style: const TextStyle(
                    color: AppColors.textDark, fontSize: 14)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    color: AppColors.textDark,
                    fontSize: 14)),
          ),
        ],
      ),
    );
  }
}

// ─── Màn QR thanh toán ───────────────────────────────────────────────────────

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
    _poller = Timer.periodic(const Duration(seconds: 3), (_) => _checkStatus());
  }

  @override
  void dispose() {
    _poller?.cancel();
    super.dispose();
  }

  Future<void> _checkStatus() async {
    if (_paid || !mounted) return;
    final (isPaid, _) =
        await _controller.getPaymentStatus(widget.result.orderCode);
    if (isPaid == true) _onPaid();
  }

  void _onPaid() {
    if (_paid) return;
    _poller?.cancel();
    setState(() => _paid = true);
    Future.delayed(const Duration(milliseconds: 1500), () {
      if (mounted) Navigator.popUntil(context, (route) => route.isFirst);
    });
  }

  Future<void> _openGateway() async {
    final uri = Uri.parse(widget.result.paymentUrl);
    final ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!ok && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('Không mở được link.')));
    }
  }

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
    final qrData = (r.qrCode != null && r.qrCode!.isNotEmpty)
        ? r.qrCode!
        : r.paymentUrl;
    final isRealQr = r.qrCode != null && r.qrCode!.isNotEmpty;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text('Thanh toán ${r.provider}'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
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
              style: TextStyle(color: AppColors.textDark)),
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
              style: const TextStyle(fontSize: 13, color: AppColors.textDark)),
          const SizedBox(height: 20),
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border:
                  Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
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
            style: const TextStyle(color: AppColors.textDark, fontSize: 14),
          ),
          const SizedBox(height: 20),
          const Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
              SizedBox(width: 8),
              Text('Đang chờ thanh toán...',
                  style: TextStyle(color: AppColors.textDark)),
            ],
          ),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: _openGateway,
              icon: const Icon(Icons.open_in_new),
              label: const Text('Mở cổng thanh toán'),
            ),
          ),
          const SizedBox(height: 8),
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
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Biên lai điện tử'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: Center(
        child: Card(
          color: AppColors.white,
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
                style: const TextStyle(color: AppColors.textDark)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(
                    fontWeight: FontWeight.w600, color: AppColors.textDark)),
          ),
        ],
      ),
    );
  }
}

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
            child: Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.textDark),
            ),
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
