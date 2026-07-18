import 'package:firebase_core/firebase_core.dart'; // Khởi tạo Firebase
import 'package:firebase_messaging/firebase_messaging.dart'; // Push (FCM)
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart'; // Thư viện Material (widget, MaterialApp...)
import 'package:go_router/go_router.dart'; // Package điều hướng trang (routing)
import 'package:mobile_app/vn/edu/fpt/view/forgot_password_view.dart';
import 'package:mobile_app/vn/edu/fpt/view/login_view.dart';
import 'vn/edu/fpt/common/app_theme.dart'; // Theme cam vừa tạo
import 'vn/edu/fpt/service/push_service.dart'; // Dịch vụ push + key SnackBar
import 'vn/edu/fpt/view/home_view.dart'; // Màn hình Home

/// Xử lý message khi app ở NỀN hoặc ĐÃ TẮT.
/// Phải là hàm top-level + @pragma('vm:entry-point') để chạy nền được.
/// Lúc này hệ thống Android tự hiện notification, ta không cần làm gì thêm.
@pragma('vm:entry-point')
Future<void> _firebaseBackgroundHandler(RemoteMessage message) async {
  // Có thể xử lý dữ liệu ngầm ở đây nếu cần.
}

/// main() là điểm bắt đầu chạy của MỌI app Dart/Flutter.
/// async vì phải chờ khởi tạo Firebase trước khi chạy app.
void main() async {
  // Bắt buộc gọi trước khi dùng plugin native trong main().
  WidgetsFlutterBinding.ensureInitialized();

  // Web chưa cấu hình FirebaseOptions (chỉ có google-services.json cho Android).
  // Bỏ qua FCM trên web để vẫn chạy Chrome khi máy ảo nặng.
  if (!kIsWeb) {
    await Firebase.initializeApp();
    FirebaseMessaging.onBackgroundMessage(_firebaseBackgroundHandler);
  }

  runApp(const MyApp()); // Khởi động app, vẽ widget MyApp lên màn hình
}

/// MyApp là widget gốc của toàn app.
/// StatelessWidget = widget "tĩnh", không tự thay đổi trạng thái bên trong.
class MyApp extends StatelessWidget {
  // Constructor. 'key' giúp Flutter phân biệt widget khi vẽ lại.
  // 'super.key' = chuyển key lên lớp cha. 'const' = tạo cố định, tối ưu hiệu năng.
  const MyApp({super.key});

  // @override: báo rằng ta đang GHI ĐÈ hàm build() của lớp cha StatelessWidget.
  @override
  Widget build(BuildContext context) {
    // build() trả về giao diện. context = "vị trí" widget trong cây widget.
    return MaterialApp.router(
      title: 'FSchool', // Tên app (hiện ở trình quản lý app của HĐH)
      debugShowCheckedModeBanner: false, // Ẩn dải chữ "DEBUG" góc phải
      theme: AppTheme.light, // Áp theme cam cho toàn app
      // Key toàn cục để hiện SnackBar từ callback push (foreground).
      scaffoldMessengerKey: scaffoldMessengerKey,
      routerConfig: _router, // Cấu hình điều hướng (khai báo bên dưới)
    );
  }
}

/// _router: khai báo các "đường dẫn" (route) trong app.
/// Dấu _ ở đầu tên = biến private (chỉ dùng trong file này).
/// 'final' = gán 1 lần, không đổi lại sau đó.
final GoRouter _router = GoRouter(
  initialLocation: '/login',
  routes: [
    // Mỗi GoRoute = 1 trang. path '/' là trang chủ (mở app vào đây trước).
    GoRoute(
      path: '/login', // Đường dẫn
      builder: (context, state) => const LoginView(), // Hàm dựng widget cho trang này
    ),
    GoRoute(
      path: '/forgot-password',
      builder: (context, state) => const ForgotPasswordView(),
    ),
    GoRoute(
      path: '/',
      builder: (context, state) => const HomeView(),
    ),
  ],
);