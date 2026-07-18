import 'package:flutter/foundation.dart' show kIsWeb;

/// Cấu hình chung của app (URL API, version...).
class AppConfig {
  AppConfig._();

  /// Địa chỉ gốc API .NET.
  /// - Flutter Web / Desktop: localhost
  /// - Android Emulator: 10.0.2.2 (trỏ về máy host)
  /// Cổng khớp launchSettings.json → http://localhost:5177
  static String get apiBaseUrl {
    if (kIsWeb) return 'http://localhost:5177/api';
    return 'http://10.0.2.2:5177/api';
  }

  /// Version app hiện tại — dùng cho Force Update (FR4.4) ở Bước 6.
  static const String appVersion = '1.0.0';

  /// Gốc server (bỏ hậu tố "/api") — dùng để ghép URL file tĩnh (ảnh y tế…).
  static String get serverOrigin =>
      apiBaseUrl.endsWith('/api')
          ? apiBaseUrl.substring(0, apiBaseUrl.length - 4)
          : apiBaseUrl;

  /// Ghép URL đầy đủ cho tài nguyên tĩnh từ đường dẫn tương đối API trả về.
  ///
  /// Nhận: [relative] — vd "/uploads/abc.jpg".
  /// Trả về (String): URL đầy đủ.
  /// Nếu đã là URL tuyệt đối (http...) thì giữ nguyên.
  static String mediaUrl(String relative) {
    if (relative.startsWith('http')) return relative;
    return '$serverOrigin$relative';
  }
}
