import 'package:dio/dio.dart';
import '../model/fee_model.dart';
import '../service/api_client.dart';

/// Controller nghiệp vụ Học phí + Thanh toán (FR2.6) cho HS/PH.
/// Quy ước trả về: record (dữ liệu, lỗi) — chỉ 1 trong 2 khác null.
class FeeController {
  final ApiClient _apiClient = ApiClient();

  /// Lấy hóa đơn của HS (hoặc của các con nếu PH) — GET /fee-invoices/me.
  /// Server tự trả theo vai trò; DTO có studentName để PH phân biệt con.
  Future<(List<FeeInvoiceModel>?, String?)> getMyInvoices() async {
    try {
      final response = await _apiClient.dio.get('/fee-invoices/me');
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) => FeeInvoiceModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được hóa đơn (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Lấy biên lai điện tử của 1 hóa đơn đã thanh toán — GET /fee-invoices/{id}/receipt.
  Future<(FeeReceiptModel?, String?)> getReceipt(int invoiceId) async {
    try {
      final response =
          await _apiClient.dio.get('/fee-invoices/$invoiceId/receipt');
      if (response.statusCode == 200) {
        final r = FeeReceiptModel.fromJson(response.data as Map<String, dynamic>);
        return (r, null);
      }
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Không tải được biên lai (mã ${response.statusCode}).';
      return (null, msg);
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Lấy lịch sử giao dịch — GET /payments/history?studentId=.
  ///
  /// Nhận: [studentId] (chỉ PH) lọc theo con; null = tất cả.
  Future<(List<PaymentTransactionModel>?, String?)> getHistory({
    int? studentId,
  }) async {
    try {
      final response = await _apiClient.dio.get(
        '/payments/history',
        queryParameters: studentId == null ? null : {'studentId': studentId},
      );
      if (response.statusCode == 200) {
        final list = (response.data as List)
            .map((e) =>
                PaymentTransactionModel.fromJson(e as Map<String, dynamic>))
            .toList();
        return (list, null);
      }
      return (null, 'Không tải được lịch sử (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Tạo giao dịch thanh toán qua cổng.
  ///
  /// Nhận:
  ///   - [invoiceId]: id hóa đơn cần thanh toán.
  ///   - [provider]: 'vnpay' hoặc 'payos'.
  /// Trả về `(PaymentResultModel?, String?)` — chứa paymentUrl để mở cổng.
  Future<(PaymentResultModel?, String?)> createPayment({
    required int invoiceId,
    required String provider,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/payments/$provider/create',
        data: {'feeInvoiceId': invoiceId},
      );
      if (response.statusCode == 200) {
        final r =
            PaymentResultModel.fromJson(response.data as Map<String, dynamic>);
        return (r, null);
      }
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Tạo thanh toán thất bại (mã ${response.statusCode}).';
      return (null, msg);
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// Kiểm tra trạng thái giao dịch — GET /payments/status/{orderCode}.
  /// App gọi định kỳ (poll) để tự phát hiện khi hóa đơn đã được thanh toán.
  ///
  /// Nhận: [orderCode] — mã đơn từ kết quả createPayment.
  /// Trả về `(bool?, String?)`: (đã thanh toán chưa?, lỗi).
  Future<(bool?, String?)> getPaymentStatus(String orderCode) async {
    try {
      final response = await _apiClient.dio.get('/payments/status/$orderCode');
      if (response.statusCode == 200) {
        final isPaid = (response.data as Map)['isPaid'] as bool? ?? false;
        return (isPaid, null);
      }
      return (null, 'Không kiểm tra được trạng thái (mã ${response.statusCode}).');
    } on DioException catch (e) {
      return (null, _extractError(e));
    }
  }

  /// (DEV) Giả lập thanh toán thành công — POST /payments/dev/simulate-paid?orderCode=.
  /// Dùng để test luồng không cần cổng thật.
  ///
  /// Nhận: [orderCode] — mã đơn từ kết quả createPayment.
  /// Trả về `(bool, String)`: (thành công?, thông báo).
  Future<(bool, String)> simulatePaid(String orderCode) async {
    try {
      final response = await _apiClient.dio.post(
        '/payments/dev/simulate-paid',
        queryParameters: {'orderCode': orderCode},
      );
      if (response.statusCode == 200) {
        return (true, 'Đã giả lập thanh toán thành công.');
      }
      final data = response.data;
      final msg = (data is Map && data['message'] is String)
          ? data['message'] as String
          : 'Giả lập thất bại (mã ${response.statusCode}).';
      return (false, msg);
    } on DioException catch (e) {
      return (false, _extractError(e));
    }
  }

  String _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map && data['message'] is String) return data['message'];
    return 'Lỗi kết nối: ${e.message}';
  }
}
