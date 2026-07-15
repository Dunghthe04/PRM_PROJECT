import 'dart:convert';
import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/admin_catalog_controller.dart';
import '../controller/admin_fee_controller.dart';

/// Màn Tài chính Admin (FR4.1, FR4.2): loại khoản thu · hóa đơn · cổng · lịch sử GD.
class AdminFinancePage extends StatelessWidget {
  const AdminFinancePage({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 4,
      child: Column(
        children: const [
          TabBar(
            isScrollable: true,
            tabs: [
              Tab(text: 'Loại khoản thu'),
              Tab(text: 'Hóa đơn'),
              Tab(text: 'Cổng PayOS/VNPay'),
              Tab(text: 'Đối soát GD'),
            ],
          ),
          Expanded(
            child: TabBarView(
              children: [
                _CategoriesTab(),
                _InvoicesTab(),
                _GatewayTab(),
                _HistoryTab(),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Categories ─────────────────────────────────────────────────────────────

class _CategoriesTab extends StatefulWidget {
  const _CategoriesTab();
  @override
  State<_CategoriesTab> createState() => _CategoriesTabState();
}

class _CategoriesTabState extends State<_CategoriesTab> {
  final _c = AdminFeeController();
  late Future<(List<FeeCategoryModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _c.getCategories();
  }

  Future<void> _reload() async {
    setState(() => _future = _c.getCategories());
    await _future;
  }

  Future<void> _openForm({FeeCategoryModel? edit}) async {
    final name = TextEditingController(text: edit?.name ?? '');
    final desc = TextEditingController(text: edit?.description ?? '');
    final amount = TextEditingController(
        text: edit == null ? '' : edit.defaultAmount.toStringAsFixed(0));
    var active = edit?.isActive ?? true;

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: Text(edit == null ? 'Thêm loại khoản thu' : 'Sửa loại khoản thu'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                    controller: name,
                    decoration: const InputDecoration(
                        labelText: 'Tên *', border: OutlineInputBorder())),
                const SizedBox(height: 8),
                TextField(
                    controller: desc,
                    decoration: const InputDecoration(
                        labelText: 'Mô tả', border: OutlineInputBorder())),
                const SizedBox(height: 8),
                TextField(
                  controller: amount,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                      labelText: 'Số tiền mặc định *',
                      border: OutlineInputBorder()),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Đang dùng'),
                  value: active,
                  onChanged: (v) => setLocal(() => active = v),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text('Hủy')),
            ElevatedButton(
                onPressed: () => Navigator.pop(ctx, true),
                child: const Text('Lưu')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final amt = double.tryParse(amount.text.trim()) ?? 0;
    final (_, err) = await _c.saveCategory(
      id: edit?.id,
      name: name.text.trim(),
      description: desc.text.trim().isEmpty ? null : desc.text.trim(),
      defaultAmount: amt,
      isActive: active,
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: ElevatedButton.icon(
              onPressed: () => _openForm(),
              icon: const Icon(Icons.add),
              label: const Text('Thêm loại'),
            ),
          ),
        ),
        Expanded(
          child: FutureBuilder(
            future: _future,
            builder: (context, snap) {
              if (snap.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }
              final (list, err) = snap.data ?? (null, 'Lỗi');
              if (err != null) return Center(child: Text(err));
              final items = list ?? [];
              return ListView.separated(
                itemCount: items.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final c = items[i];
                  return ListTile(
                    title: Text(c.name,
                        style: const TextStyle(fontWeight: FontWeight.w600)),
                    subtitle: Text(
                        '${c.amountLabel}${c.isActive ? "" : " • NGƯNG"}'),
                    trailing: IconButton(
                      icon: const Icon(Icons.edit),
                      onPressed: () => _openForm(edit: c),
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}

// ── Invoices ───────────────────────────────────────────────────────────────

class _InvoicesTab extends StatefulWidget {
  const _InvoicesTab();
  @override
  State<_InvoicesTab> createState() => _InvoicesTabState();
}

class _InvoicesTabState extends State<_InvoicesTab> {
  final _fee = AdminFeeController();
  final _catalog = AdminCatalogController();
  late Future<(List<AdminFeeInvoiceModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _fee.getInvoices();
  }

  Future<void> _reload() async {
    setState(() => _future = _fee.getInvoices());
    await _future;
  }

  Future<void> _batchCreate() async {
    final (classes, cErr) = await _catalog.getClasses();
    final (cats, catErr) = await _fee.getCategories(isActive: true);
    if (!mounted) return;
    if (cErr != null || catErr != null || classes == null || cats == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(cErr ?? catErr ?? 'Thiếu dữ liệu lớp/loại phí.')),
      );
      return;
    }
    if (classes.isEmpty || cats.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Cần có lớp và loại khoản thu trước.')),
      );
      return;
    }
    var classId = classes.first.id;
    var catId = cats.first.id;
    var due = DateTime.now().add(const Duration(days: 30));

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: const Text('Tạo hóa đơn cả lớp'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<int>(
                initialValue: classId,
                decoration: const InputDecoration(
                    labelText: 'Lớp', border: OutlineInputBorder()),
                items: classes
                    .map((c) =>
                        DropdownMenuItem(value: c.id, child: Text(c.name)))
                    .toList(),
                onChanged: (v) => setLocal(() => classId = v ?? classId),
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<int>(
                initialValue: catId,
                decoration: const InputDecoration(
                    labelText: 'Loại khoản thu', border: OutlineInputBorder()),
                items: cats
                    .map((c) => DropdownMenuItem(
                        value: c.id,
                        child: Text('${c.name} (${c.amountLabel})')))
                    .toList(),
                onChanged: (v) => setLocal(() => catId = v ?? catId),
              ),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('Hạn: ${FormatUtils.date(due)}'),
                trailing: TextButton(
                  child: const Text('Chọn'),
                  onPressed: () async {
                    final d = await showDatePicker(
                      context: ctx,
                      initialDate: due,
                      firstDate: DateTime.now(),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                    );
                    if (d != null) setLocal(() => due = d);
                  },
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
                child: const Text('Tạo')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final (count, err) = await _fee.batchCreateInvoices(
      classId: classId,
      feeCategoryId: catId,
      dueDate: due,
    );
    if (!mounted) return;
    if (err != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err)));
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Đã tạo $count hóa đơn.')),
    );
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Align(
          alignment: Alignment.centerRight,
          child: Padding(
            padding: const EdgeInsets.all(8),
            child: ElevatedButton.icon(
              onPressed: _batchCreate,
              icon: const Icon(Icons.receipt_long),
              label: const Text('Tạo hóa đơn cả lớp'),
            ),
          ),
        ),
        Expanded(
          child: FutureBuilder(
            future: _future,
            builder: (context, snap) {
              if (snap.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }
              final (list, err) = snap.data ?? (null, 'Lỗi');
              if (err != null) return Center(child: Text(err));
              final items = list ?? [];
              if (items.isEmpty) {
                return const Center(child: Text('Chưa có hóa đơn.'));
              }
              return ListView.separated(
                itemCount: items.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (_, i) {
                  final inv = items[i];
                  return ListTile(
                    title: Text('${inv.studentName} • ${inv.feeCategoryName}'),
                    subtitle: Text(
                      '${FormatUtils.currency(inv.amount)} • Hạn ${FormatUtils.date(inv.dueDate)}\n'
                      '${inv.isPaid ? "ĐÃ TT" : inv.status}'
                      '${inv.receiptNumber != null ? " • ${inv.receiptNumber}" : ""}',
                    ),
                    isThreeLine: true,
                    trailing: Icon(
                      inv.isPaid ? Icons.check_circle : Icons.schedule,
                      color: inv.isPaid ? AppColors.success : Colors.orange,
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}

// ── Gateway ────────────────────────────────────────────────────────────────

class _GatewayTab extends StatefulWidget {
  const _GatewayTab();
  @override
  State<_GatewayTab> createState() => _GatewayTabState();
}

class _GatewayTabState extends State<_GatewayTab> {
  final _c = AdminFeeController();
  late Future<(List<PaymentGatewayConfigModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _c.getPaymentConfigs();
  }

  Future<void> _reload() async {
    setState(() => _future = _c.getPaymentConfigs());
    await _future;
  }

  Future<void> _editPayOs(PaymentGatewayConfigModel? existing) async {
    final cfg = existing?.parseConfig() ?? {};
    final clientId = TextEditingController(text: '${cfg['ClientId'] ?? ''}');
    final apiKey = TextEditingController(text: '${cfg['ApiKey'] ?? ''}');
    final checksum =
        TextEditingController(text: '${cfg['ChecksumKey'] ?? ''}');
    final returnUrl =
        TextEditingController(text: '${cfg['ReturnUrl'] ?? ''}');
    final cancelUrl =
        TextEditingController(text: '${cfg['CancelUrl'] ?? ''}');
    var enabled = existing?.isEnabled ?? true;

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: const Text('Cấu hình PayOS'),
          content: SizedBox(
            width: 420,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Bật PayOS'),
                    value: enabled,
                    onChanged: (v) => setLocal(() => enabled = v),
                  ),
                  _field(clientId, 'ClientId'),
                  _field(apiKey, 'ApiKey'),
                  _field(checksum, 'ChecksumKey'),
                  _field(returnUrl, 'ReturnUrl'),
                  _field(cancelUrl, 'CancelUrl'),
                  const Text(
                    'Nếu API trả *** (đã mask), nhập lại giá trị đầy đủ khi đổi.',
                    style: TextStyle(fontSize: 12, color: AppColors.textGrey),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text('Hủy')),
            ElevatedButton(
                onPressed: () => Navigator.pop(ctx, true),
                child: const Text('Lưu')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final json = jsonEncode({
      'ClientId': clientId.text.trim(),
      'ApiKey': apiKey.text.trim(),
      'ChecksumKey': checksum.text.trim(),
      'ReturnUrl': returnUrl.text.trim(),
      'CancelUrl': cancelUrl.text.trim(),
    });
    final (_, err) = await _c.savePaymentConfig(
      provider: 'PayOS',
      isEnabled: enabled,
      configJson: json,
    );
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(err ?? 'Đã lưu cấu hình PayOS.')),
    );
    if (err == null) _reload();
  }

  Future<void> _editVnPay(PaymentGatewayConfigModel? existing) async {
    final cfg = existing?.parseConfig() ?? {};
    final tmn = TextEditingController(text: '${cfg['TmnCode'] ?? ''}');
    final secret = TextEditingController(text: '${cfg['HashSecret'] ?? ''}');
    final url = TextEditingController(text: '${cfg['PaymentUrl'] ?? ''}');
    final ret = TextEditingController(text: '${cfg['ReturnUrl'] ?? ''}');
    var enabled = existing?.isEnabled ?? false;

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: const Text('Cấu hình VNPay'),
          content: SizedBox(
            width: 420,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Bật VNPay'),
                    value: enabled,
                    onChanged: (v) => setLocal(() => enabled = v),
                  ),
                  _field(tmn, 'TmnCode'),
                  _field(secret, 'HashSecret'),
                  _field(url, 'PaymentUrl'),
                  _field(ret, 'ReturnUrl'),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text('Hủy')),
            ElevatedButton(
                onPressed: () => Navigator.pop(ctx, true),
                child: const Text('Lưu')),
          ],
        ),
      ),
    );
    if (ok != true) return;
    final json = jsonEncode({
      'TmnCode': tmn.text.trim(),
      'HashSecret': secret.text.trim(),
      'PaymentUrl': url.text.trim(),
      'ReturnUrl': ret.text.trim(),
    });
    final (_, err) = await _c.savePaymentConfig(
      provider: 'VNPay',
      isEnabled: enabled,
      configJson: json,
    );
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(err ?? 'Đã lưu cấu hình VNPay.')),
    );
    if (err == null) _reload();
  }

  Widget _field(TextEditingController c, String label) => Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: TextField(
          controller: c,
          decoration: InputDecoration(
            labelText: label,
            border: const OutlineInputBorder(),
          ),
        ),
      );

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: _future,
      builder: (context, snap) {
        if (snap.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        final (list, err) = snap.data ?? (null, 'Lỗi');
        if (err != null) return Center(child: Text(err));
        final items = list ?? [];
        PaymentGatewayConfigModel? payos;
        PaymentGatewayConfigModel? vnpay;
        for (final c in items) {
          if (c.provider.toLowerCase() == 'payos') payos = c;
          if (c.provider.toLowerCase() == 'vnpay') vnpay = c;
        }
        return ListView(
          padding: const EdgeInsets.all(12),
          children: [
            Card(
              child: ListTile(
                leading: Icon(Icons.qr_code,
                    color: payos?.isEnabled == true
                        ? AppColors.success
                        : AppColors.textGrey),
                title: const Text('PayOS',
                    style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: Text(payos == null
                    ? 'Chưa cấu hình'
                    : (payos.isEnabled ? 'Đang bật' : 'Đang tắt')),
                trailing: ElevatedButton(
                  onPressed: () => _editPayOs(payos),
                  child: const Text('Cấu hình'),
                ),
              ),
            ),
            Card(
              child: ListTile(
                leading: Icon(Icons.account_balance,
                    color: vnpay?.isEnabled == true
                        ? AppColors.success
                        : AppColors.textGrey),
                title: const Text('VNPay',
                    style: TextStyle(fontWeight: FontWeight.bold)),
                subtitle: Text(vnpay == null
                    ? 'Chưa cấu hình'
                    : (vnpay.isEnabled ? 'Đang bật' : 'Đang tắt')),
                trailing: ElevatedButton(
                  onPressed: () => _editVnPay(vnpay),
                  child: const Text('Cấu hình'),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

// ── History ────────────────────────────────────────────────────────────────

class _HistoryTab extends StatefulWidget {
  const _HistoryTab();
  @override
  State<_HistoryTab> createState() => _HistoryTabState();
}

class _HistoryTabState extends State<_HistoryTab> {
  final _c = AdminFeeController();
  late Future<(List<AdminPaymentTxnModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _c.getPaymentHistory();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: _future,
      builder: (context, snap) {
        if (snap.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        final (list, err) = snap.data ?? (null, 'Lỗi');
        if (err != null) return Center(child: Text(err));
        final items = list ?? [];
        if (items.isEmpty) {
          return const Center(child: Text('Chưa có giao dịch.'));
        }
        return RefreshIndicator(
          onRefresh: () async {
            setState(() => _future = _c.getPaymentHistory());
            await _future;
          },
          child: ListView.separated(
            itemCount: items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (_, i) {
              final t = items[i];
              return ListTile(
                title: Text('${t.provider} • ${t.orderCode}'),
                subtitle: Text(
                    'HĐ #${t.feeInvoiceId} • ${FormatUtils.dateTime(t.createdAt)}'),
                trailing: Text(
                  '${FormatUtils.currency(t.amount)}\n${t.status}',
                  textAlign: TextAlign.right,
                  style: TextStyle(
                    color: t.status.toLowerCase() == 'paid'
                        ? AppColors.success
                        : AppColors.textDark,
                    fontWeight: FontWeight.w600,
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
