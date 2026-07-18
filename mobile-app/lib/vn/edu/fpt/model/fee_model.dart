/// Model học phí (FR2.6) — khớp `FeeDtos` bên API .NET.
///
/// Quan hệ server:
/// - [FeeInvoiceModel] ← `FeeInvoice` → Student, FeeCategory.
/// - [FeeReceiptModel] ← biên lai sinh khi Paid.
/// - [PaymentResultModel] ← kết quả tạo giao dịch cổng (chưa phải entity DB).

/// Một hóa đơn khoản thu của học sinh.
class FeeInvoiceModel {
  /// Id hóa đơn.
  final int id;

  /// FK → học sinh được thu.
  final int studentId;

  /// Tên HS (PH phân biệt con).
  final String studentName;

  /// FK → loại khoản thu.
  final int feeCategoryId;

  /// Tên khoản thu (Học phí, BHYT…).
  final String feeCategoryName;

  /// Số tiền phải thu.
  final double amount;

  /// Hạn thanh toán.
  final DateTime dueDate;

  /// Trạng thái server: Pending | Paid | Failed | Cancelled.
  final String status;

  /// True khi đã thanh toán thành công.
  final bool isPaid;

  /// Thời điểm thanh toán — null nếu chưa đóng.
  final DateTime? paidAt;

  /// Cổng: VNPAY | PAYOS | Manual.
  final String? paymentMethod;

  /// Số biên lai điện tử — có khi Paid.
  final String? receiptNumber;

  /// Ghi chú (tuỳ chọn).
  final String? note;

  FeeInvoiceModel({
    required this.id,
    required this.studentId,
    required this.studentName,
    required this.feeCategoryId,
    required this.feeCategoryName,
    required this.amount,
    required this.dueDate,
    required this.status,
    required this.isPaid,
    this.paidAt,
    this.paymentMethod,
    this.receiptNumber,
    this.note,
  });

  /// Nhãn trạng thái tiếng Việt cho UI.
  String get statusLabel {
    if (isPaid) return 'Đã đóng';
    if (DateTime.now().isAfter(dueDate)) return 'Quá hạn';
    return 'Chưa đóng';
  }

  /// Chưa đóng và đã quá hạn thanh toán.
  bool get isOverdue => !isPaid && DateTime.now().isAfter(dueDate);

  /// Parse từ JSON `FeeInvoiceDto`.
  factory FeeInvoiceModel.fromJson(Map<String, dynamic> json) {
    return FeeInvoiceModel(
      id: json['id'] as int,
      studentId: json['studentId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      feeCategoryId: json['feeCategoryId'] as int? ?? 0,
      feeCategoryName: json['feeCategoryName'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      dueDate: DateTime.tryParse(json['dueDate'] as String? ?? '') ??
          DateTime.now(),
      status: json['status'] as String? ?? '',
      isPaid: json['isPaid'] as bool? ?? false,
      paidAt: DateTime.tryParse(json['paidAt'] as String? ?? ''),
      paymentMethod: json['paymentMethod'] as String?,
      receiptNumber: json['receiptNumber'] as String?,
      note: json['note'] as String?,
    );
  }
}

/// Biên lai điện tử của 1 hóa đơn đã thanh toán.
class FeeReceiptModel {
  /// Số biên lai.
  final String receiptNumber;

  /// FK → hóa đơn gốc.
  final int invoiceId;

  /// Tên học sinh.
  final String studentName;

  /// Tên khoản thu.
  final String feeCategoryName;

  /// Số tiền đã thu.
  final double amount;

  /// Phương thức thanh toán.
  final String paymentMethod;

  /// Mã giao dịch cổng (nếu có).
  final String? transactionId;

  /// Thời điểm thanh toán.
  final DateTime paidAt;

  FeeReceiptModel({
    required this.receiptNumber,
    required this.invoiceId,
    required this.studentName,
    required this.feeCategoryName,
    required this.amount,
    required this.paymentMethod,
    this.transactionId,
    required this.paidAt,
  });

  /// Parse từ JSON biên lai (`GET /fee-invoices/{id}/receipt`).
  factory FeeReceiptModel.fromJson(Map<String, dynamic> json) {
    return FeeReceiptModel(
      receiptNumber: json['receiptNumber'] as String? ?? '',
      invoiceId: json['invoiceId'] as int? ?? 0,
      studentName: json['studentName'] as String? ?? '',
      feeCategoryName: json['feeCategoryName'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      paymentMethod: json['paymentMethod'] as String? ?? '',
      transactionId: json['transactionId'] as String?,
      paidAt: DateTime.tryParse(json['paidAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}

/// Kết quả tạo giao dịch thanh toán (chứa link checkout).
class PaymentResultModel {
  /// Nhà cung cấp: VNPAY | PAYOS.
  final String provider;

  /// Mã đơn nội bộ — dùng poll trạng thái / simulate-paid (dev).
  final String orderCode;

  /// URL mở cổng thanh toán.
  final String paymentUrl;

  /// Số tiền giao dịch.
  final double amount;

  /// Chuỗi VietQR (PayOS) — null nếu stub/dev.
  final String? qrCode;

  PaymentResultModel({
    required this.provider,
    required this.orderCode,
    required this.paymentUrl,
    required this.amount,
    this.qrCode,
  });

  /// Parse từ JSON kết quả `POST /payments/{provider}/create`.
  factory PaymentResultModel.fromJson(Map<String, dynamic> json) {
    return PaymentResultModel(
      provider: json['provider'] as String? ?? '',
      orderCode: json['orderCode'] as String? ?? '',
      paymentUrl: json['paymentUrl'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      qrCode: json['qrCode'] as String?,
    );
  }
}
