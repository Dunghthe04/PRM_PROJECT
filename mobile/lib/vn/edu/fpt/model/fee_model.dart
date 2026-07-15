// Model học phí (FR2.6) — khớp FeeDtos.cs bên API .NET.
//
// Gồm 3 lớp:
//   - FeeInvoiceModel: 1 hóa đơn khoản thu.
//   - FeeReceiptModel: biên lai điện tử (khi đã thanh toán).
//   - PaymentResultModel: kết quả tạo giao dịch (chứa link thanh toán).

/// Một hóa đơn khoản thu của học sinh.
class FeeInvoiceModel {
  final int id;
  final int studentId;
  final String studentName;
  final int feeCategoryId;
  final String feeCategoryName; // tên khoản thu (Học phí, BHYT…)
  final double amount; // số tiền
  final DateTime dueDate; // hạn đóng
  final String status; // Pending | Paid | Overdue…
  final bool isPaid;
  final DateTime? paidAt;
  final String? paymentMethod; // VNPAY | PAYOS
  final String? receiptNumber; // số biên lai (khi đã đóng)
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

  /// Nhãn trạng thái tiếng Việt.
  String get statusLabel {
    if (isPaid) return 'Đã đóng';
    if (DateTime.now().isAfter(dueDate)) return 'Quá hạn';
    return 'Chưa đóng';
  }

  /// True nếu chưa đóng và đã quá hạn.
  bool get isOverdue => !isPaid && DateTime.now().isAfter(dueDate);

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
  final String receiptNumber;
  final int invoiceId;
  final String studentName;
  final String feeCategoryName;
  final double amount;
  final String paymentMethod;
  final String? transactionId;
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

/// Kết quả tạo giao dịch thanh toán (chứa link checkout + mã đơn + QR).
class PaymentResultModel {
  final String provider; // VNPAY | PAYOS
  final String orderCode; // mã đơn (dùng cho simulate-paid ở dev + poll trạng thái)
  final String paymentUrl; // link mở cổng thanh toán
  final double amount;
  final String? qrCode; // chuỗi VietQR (PayOS) để vẽ QR trong app; null nếu dev stub

  PaymentResultModel({
    required this.provider,
    required this.orderCode,
    required this.paymentUrl,
    required this.amount,
    this.qrCode,
  });

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

/// Một giao dịch trong lịch sử thanh toán.
class PaymentTransactionModel {
  final int id;
  final int feeInvoiceId;
  final String provider;
  final String orderCode;
  final double amount;
  final String status; // Pending | Paid | Failed…
  final DateTime createdAt;

  PaymentTransactionModel({
    required this.id,
    required this.feeInvoiceId,
    required this.provider,
    required this.orderCode,
    required this.amount,
    required this.status,
    required this.createdAt,
  });

  factory PaymentTransactionModel.fromJson(Map<String, dynamic> json) {
    return PaymentTransactionModel(
      id: json['id'] as int,
      feeInvoiceId: json['feeInvoiceId'] as int? ?? 0,
      provider: json['provider'] as String? ?? '',
      orderCode: json['orderCode'] as String? ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0,
      status: json['status'] as String? ?? '',
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}
