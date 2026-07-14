/// Cấu hình chung của app (URL API, version...).
class AppConfig{
  AppConfig._();
/// Địa chỉ gốc API .NET.
/// - Android Emulator: dùng 10.0.2.2 (trỏ về localhost máy tính)
/// - Flutter Web / Windows Desktop: dùng localhost
/// Đổi cổng cho khớp với API đang chạy (xem launchSettings.json).
  static const String apiBaseUrl = 'http://10.0.2.2:5177/api';
  /// Version app hiện tại — dùng cho Force Update (FR4.4) ở Bước 6.
  static const String appVersion = '1.0.0';
}