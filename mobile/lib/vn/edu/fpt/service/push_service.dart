import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'api_client.dart';

/// Key toàn cục cho ScaffoldMessenger — cho phép hiện SnackBar từ bất cứ đâu
/// (kể cả từ callback FCM nằm ngoài cây widget). Gắn vào MaterialApp ở main.dart.
final GlobalKey<ScaffoldMessengerState> scaffoldMessengerKey =
    GlobalKey<ScaffoldMessengerState>();

/// Dịch vụ xử lý push notification qua Firebase Cloud Messaging (FR1.4).
/// Singleton: chỉ 1 thể hiện dùng chung toàn app.
class PushService {
  PushService._();
  static final PushService instance = PushService._();

  final ApiClient _apiClient = ApiClient();
  // Chỉ tạo FirebaseMessaging trên mobile — web chưa cấu hình FCM.
  FirebaseMessaging? get _fm => kIsWeb ? null : FirebaseMessaging.instance;

  /// Khởi tạo SAU KHI đăng nhập thành công.
  ///
  /// Nhận: không tham số.
  /// Trả về: `Future<void>`.
  /// Việc làm: xin quyền hiện thông báo → lấy FCM token của thiết bị →
  /// gửi token lên server (để server bắn push đúng máy) → lắng nghe message.
  Future<void> init() async {
    // Web: bỏ qua FCM (chưa có FirebaseOptions). Push vẫn test trên Android.
    if (kIsWeb || _fm == null) return;

    final fm = _fm!;
    // Android 13+ / iOS cần xin quyền hiển thị thông báo.
    await fm.requestPermission();

    // Mỗi thiết bị có 1 token định danh để FCM gửi tới đúng máy.
    final token = await fm.getToken();
    // In token ra console (chỉ debug) để test gửi thử từ Firebase Console.
    debugPrint('[FCM TOKEN] $token');
    if (token != null) await _registerToken(token);

    // Token có thể bị làm mới → đăng ký lại token mới.
    fm.onTokenRefresh.listen(_registerToken);

    // App đang MỞ (foreground): FCM không tự hiện notification,
    // nên ta chủ động hiện SnackBar cho user thấy.
    FirebaseMessaging.onMessage.listen((message) {
      final n = message.notification;
      if (n != null) {
        scaffoldMessengerKey.currentState?.showSnackBar(
          SnackBar(content: Text('${n.title ?? 'Thông báo'}: ${n.body ?? ''}')),
        );
      }
    });
  }

  /// Gửi FCM token lên backend để lưu (POST /devices/register).
  ///
  /// Nhận: [token] — FCM token của thiết bị.
  /// Trả về: `Future<void>`. Lỗi mạng thì bỏ qua (lần init sau thử lại).
  Future<void> _registerToken(String token) async {
    try {
      await _apiClient.dio.post('/devices/register', data: {
        'fcmToken': token,
        'platform': 'android',
      });
    } catch (_) {
      // Không chặn luồng app nếu đăng ký token thất bại.
    }
  }

  /// Hủy đăng ký token khi ĐĂNG XUẤT (để không nhận push cho tài khoản cũ).
  ///
  /// Nhận: không tham số. Trả về: `Future<void>`.
  Future<void> unregister() async {
    if (kIsWeb || _fm == null) return;
    try {
      final token = await _fm!.getToken();
      if (token != null) {
        // token có ký tự đặc biệt → encode trước khi đưa vào URL.
        await _apiClient.dio.delete('/devices/${Uri.encodeComponent(token)}');
      }
    } catch (_) {
      // Bỏ qua lỗi khi hủy.
    }
  }
}
