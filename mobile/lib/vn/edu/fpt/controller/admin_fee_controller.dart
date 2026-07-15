import 'dart:convert';
import 'package:dio/dio.dart';
import '../common/format_utils.dart';
import '../service/api_client.dart';

/// Loại khoản thu — FeeCategoryDto.
class FeeCategoryModel {
  final int id;
  final String name;
  final String? description;
  final double defaultAmount;
  final bool isActive;

  FeeCategoryModel({
    required this.id,
    required this.name,
    this.description,
    required this.defaultAmount,
    required this.isActive,
  });

  factory FeeCategoryModel.fromJson(Map<String, dynamic> json) =>
      FeeCategoryModel(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        description: json['description'] as String?,
        defaultAmount: (json['defaultAmount'] as num?)?.toDouble() ?? 0,
        isActive: json['isActive'] as bool? ?? true,
      );

  String get amountLabel => FormatUtils.currency(defaultAmount);
}

/// Hóa đơn — FeeInvoiceDto (admin list).
class AdminFeeInvoiceModel {
  final int id;
  final int studentId;
  final String studentName;
  final String studentPhone;
  final int feeCategoryId;
  final String feeCategoryName;
  final double amount;
  final DateTime dueDate;
  final String status;
  final bool isPaid;
  final DateTime? paidAt;
  final String? receiptNumber;

  AdminFeeInvoiceModel({
    required this.id,
    required this.studentId,
    required this.studentName,
    required this.studentPhone,
    required this.feeCategoryId,
    required this.feeCategoryName,
    required this.amount,
    required this.dueDate,
    required this.status,
    required this.isPaid,
    this.paidAt,
    this.receiptNumber,
  });

  factory AdminFeeInvoiceModel.fromJson(Map<String, dynamic> json) =>
      AdminFeeInvoiceModel(
        id: json['id'] as int,
        studentId: json['studentId'] as int? ?? 0,
        studentName: json['studentName'] as String? ?? '',
        studentPhone: json['studentPhone'] as String? ?? '',
        feeCategoryId: json['feeCategoryId'] as int? ?? 0,
        feeCategoryName: json['feeCategoryName'] as String? ?? '',
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        dueDate: DateTime.tryParse(json['dueDate'] as String? ?? '') ??
            DateTime.now(),
        status: json['status'] as String? ?? '',
        isPaid: json['isPaid'] as bool? ?? false,
        paidAt: DateTime.tryParse(json['paidAt'] as String? ?? ''),
        receiptNumber: json['receiptNumber'] as String?,
      );
}

/// Cấu hình cổng thanh toán (secret có thể bị mask).
class PaymentGatewayConfigModel {
  final int id;
  final String provider;
  final bool isEnabled;
  final String configJson;
  final DateTime updatedAt;

  PaymentGatewayConfigModel({
    required this.id,
    required this.provider,
    required this.isEnabled,
    required this.configJson,
    required this.updatedAt,
  });

  factory PaymentGatewayConfigModel.fromJson(Map<String, dynamic> json) =>
      PaymentGatewayConfigModel(
        id: json['id'] as int,
        provider: json['provider'] as String? ?? '',
        isEnabled: json['isEnabled'] as bool? ?? false,
        configJson: json['configJson'] as String? ?? '{}',
        updatedAt: DateTime.tryParse(json['updatedAt'] as String? ?? '') ??
            DateTime.now(),
      );

  Map<String, dynamic> parseConfig() {
    try {
      final m = jsonDecode(configJson);
      if (m is Map<String, dynamic>) return m;
      if (m is Map) return Map<String, dynamic>.from(m);
    } catch (_) {}
    return {};
  }
}

/// Giao dịch thanh toán — đối soát đơn giản (lịch sử).
class AdminPaymentTxnModel {
  final int id;
  final int feeInvoiceId;
  final String provider;
  final String orderCode;
  final double amount;
  final String status;
  final DateTime createdAt;

  AdminPaymentTxnModel({
    required this.id,
    required this.feeInvoiceId,
    required this.provider,
    required this.orderCode,
    required this.amount,
    required this.status,
    required this.createdAt,
  });

  factory AdminPaymentTxnModel.fromJson(Map<String, dynamic> json) =>
      AdminPaymentTxnModel(
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

/// Controller tài chính Admin (FR4.1, FR4.2).
class AdminFeeController {
  final ApiClient _api = ApiClient();

  Future<(List<FeeCategoryModel>?, String?)> getCategories(
      {bool? isActive}) async {
    try {
      final res = await _api.dio.get(
        '/fee-categories',
        queryParameters: isActive == null ? null : {'isActive': isActive},
      );
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) => FeeCategoryModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được loại khoản thu.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(FeeCategoryModel?, String?)> saveCategory({
    int? id,
    required String name,
    String? description,
    required double defaultAmount,
    bool isActive = true,
  }) async {
    try {
      final body = {
        'name': name,
        'description': description,
        'defaultAmount': defaultAmount,
        'isActive': isActive,
      };
      final res = id == null
          ? await _api.dio.post('/fee-categories', data: body)
          : await _api.dio.put('/fee-categories/$id', data: body);
      if (res.statusCode == 200 || res.statusCode == 201) {
        return (
          FeeCategoryModel.fromJson(res.data as Map<String, dynamic>),
          null
        );
      }
      return (null, _msg(res.data, 'Lưu loại khoản thu thất bại.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(bool, String)> deleteCategory(int id) async {
    try {
      final res = await _api.dio.delete('/fee-categories/$id');
      if (res.statusCode == 200) return (true, 'Đã xóa loại khoản thu.');
      return (false, _msg(res.data, 'Xóa thất bại.'));
    } on DioException catch (e) {
      return (false, _dio(e));
    }
  }

  Future<(List<AdminFeeInvoiceModel>?, String?)> getInvoices({
    bool? isPaid,
    String? status,
  }) async {
    try {
      final q = <String, dynamic>{};
      if (isPaid != null) q['isPaid'] = isPaid;
      if (status != null) q['status'] = status;
      final res = await _api.dio.get(
        '/fee-invoices',
        queryParameters: q.isEmpty ? null : q,
      );
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) =>
                AdminFeeInvoiceModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được hóa đơn.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  /// Tạo hóa đơn hàng loạt cho cả lớp.
  Future<(int?, String?)> batchCreateInvoices({
    required int classId,
    required int feeCategoryId,
    required DateTime dueDate,
    double? amount,
    String? note,
  }) async {
    try {
      final res = await _api.dio.post('/fee-invoices/batch', data: {
        'classId': classId,
        'feeCategoryId': feeCategoryId,
        'amount': amount,
        'dueDate': dueDate.toIso8601String(),
        'note': note,
      });
      if (res.statusCode == 200 || res.statusCode == 201) {
        final data = res.data;
        final count = (data is Map && data['createdCount'] is int)
            ? data['createdCount'] as int
            : 0;
        return (count, null);
      }
      return (null, _msg(res.data, 'Tạo hóa đơn thất bại.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(List<PaymentGatewayConfigModel>?, String?)> getPaymentConfigs() async {
    try {
      final res = await _api.dio.get('/payment-config');
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) =>
                PaymentGatewayConfigModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được cấu hình cổng.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(PaymentGatewayConfigModel?, String?)> savePaymentConfig({
    required String provider,
    required bool isEnabled,
    required String configJson,
  }) async {
    try {
      final res = await _api.dio.put('/payment-config', data: {
        'provider': provider,
        'isEnabled': isEnabled,
        'configJson': configJson,
      });
      if (res.statusCode == 200) {
        return (
          PaymentGatewayConfigModel.fromJson(
              res.data as Map<String, dynamic>),
          null
        );
      }
      return (null, _msg(res.data, 'Lưu cấu hình thất bại.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  Future<(List<AdminPaymentTxnModel>?, String?)> getPaymentHistory() async {
    try {
      final res = await _api.dio.get('/payments/history');
      if (res.statusCode == 200) {
        final list = (res.data as List)
            .map((e) =>
                AdminPaymentTxnModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, _msg(res.data, 'Không tải được lịch sử GD.'));
    } on DioException catch (e) {
      return (null, _dio(e));
    }
  }

  String _msg(dynamic data, String fallback) {
    if (data is Map && data['message'] is String) {
      return data['message'] as String;
    }
    return fallback;
  }

  String _dio(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) {
      return data['message'] as String;
    }
    return 'Lỗi kết nối: ${e.message}';
  }
}
