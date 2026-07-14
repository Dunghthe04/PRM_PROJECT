import '../model/version_info.dart';
import '../service/api_client.dart';

/// Controller cho các nghiệp vụ hệ thống của app.
/// Hiện tại phục vụ tính năng Force Update (bắt cập nhật) — FR4.4.
///
/// Luồng tổng quát:
///   App mở → gọi [fetchVersion] lấy quy định version từ server
///          → dùng [needForceUpdate] so version app với quy định
///          → nếu cần thì màn Login hiện dialog bắt cập nhật.
class AppController {
  // Client HTTP dùng chung (đã tự gắn token qua interceptor).
  final ApiClient _apiClient = ApiClient();

  /// Gọi API lấy thông tin phiên bản (GET /api/app/version).
  ///
  /// Nhận: không tham số.
  /// Trả về: [Future] chứa
  ///   - [VersionInfo] nếu server trả 200 (kèm min/latest version...).
  ///   - null nếu lỗi mạng / server không phản hồi / status khác 200.
  ///
  /// Ghi chú: cố tình "nuốt" lỗi và trả null để KHÔNG chặn người dùng
  /// khi không lấy được version (ví dụ mất mạng lúc mở app).
  Future<VersionInfo?> fetchVersion() async {
    try {
      final response = await _apiClient.dio.get('/app/version');
      if (response.statusCode == 200) {
        // response.data là Map JSON → chuyển thành object VersionInfo.
        return VersionInfo.fromJson(response.data as Map<String, dynamic>);
      }
    } catch (_) {
      // Không lấy được version thì bỏ qua, cho dùng bình thường.
    }
    return null;
  }

  /// So sánh 2 chuỗi version dạng "1.2.3" theo từng số (major.minor.patch).
  ///
  /// Nhận:
  ///   - [a]: version thứ nhất (vd version app đang chạy).
  ///   - [b]: version thứ hai (vd version tối thiểu từ server).
  /// Trả về (int):
  ///   - -1  nếu a < b   (a cũ hơn b)
  ///   -  0  nếu a == b  (bằng nhau)
  ///   -  1  nếu a > b   (a mới hơn b)
  ///
  /// Cách làm: tách chuỗi theo dấu '.', đổi mỗi phần sang số rồi so lần lượt.
  /// Phần thiếu coi như 0 (vd "1.2" so với "1.2.0" là bằng nhau).
  static int compareVersion(String a, String b) {
    final pa = a.split('.').map((e) => int.tryParse(e) ?? 0).toList();
    final pb = b.split('.').map((e) => int.tryParse(e) ?? 0).toList();
    final len = pa.length > pb.length ? pa.length : pb.length;
    for (var i = 0; i < len; i++) {
      final x = i < pa.length ? pa[i] : 0; // thiếu số → coi là 0
      final y = i < pb.length ? pb[i] : 0;
      if (x != y) return x < y ? -1 : 1; // gặp chỗ khác nhau đầu tiên là quyết định
    }
    return 0; // duyệt hết mà bằng nhau
  }

  /// Kiểm tra có phải BẮT BUỘC cập nhật hay không.
  ///
  /// Nhận:
  ///   - [current]: version app đang chạy (thường là AppConfig.appVersion).
  ///   - [info]: thông tin version lấy từ server ([fetchVersion]).
  /// Trả về (bool):
  ///   - true  nếu current < info.minSupportedVersion → phải cập nhật.
  ///   - false nếu bằng hoặc mới hơn → dùng bình thường.
  static bool needForceUpdate(String current, VersionInfo info) {
    return compareVersion(current, info.minSupportedVersion) < 0;
  }
}
