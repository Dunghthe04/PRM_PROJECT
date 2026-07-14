import 'package:flutter/material.dart'; // Thư viện Material (widget, MaterialApp...)
import 'package:go_router/go_router.dart'; // Package điều hướng trang (routing)
import 'package:mobile_app/vn/edu/fpt/view/login_view.dart';
import 'vn/edu/fpt/common/app_theme.dart'; // Theme cam vừa tạo
import 'vn/edu/fpt/view/home_view.dart'; // Màn hình Home

/// main() là điểm bắt đầu chạy của MỌI app Dart/Flutter.
void main() {
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
      path: '/',
      builder: (context, state) => const HomeView(),
    ),
  ],
);