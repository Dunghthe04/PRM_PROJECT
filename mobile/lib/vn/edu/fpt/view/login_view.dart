import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../common/app_colors.dart';
import '../controller/auth_controller.dart';
import '../common/app_config.dart';
import '../controller/app_controller.dart';
import '../model/version_info.dart';

/// Màn hình Đăng nhập (SĐT + mật khẩu).
/// StatefulWidget vì màn này có TRẠNG THÁI thay đổi:
/// nội dung ô nhập, đang loading hay không, thông báo lỗi.
class LoginView extends StatefulWidget {
  const LoginView({super.key});

  @override
  State<LoginView> createState() => _LoginViewState();
}

// _LoginViewState: nơi chứa state + hàm build giao diện.
class _LoginViewState extends State<LoginView> {
  // Controller đọc/điều khiển nội dung ô nhập liệu.
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();

  final AuthController _authController = AuthController();

  bool _isLoading = false; // true khi đang gọi API (hiện vòng xoay)
  String? _errorMessage; // null = không lỗi; có chuỗi = hiện lỗi đỏ


  /// Xử lý khi bấm nút Đăng nhập.
  Future<void> _handleLogin() async {
    // setState: báo Flutter "state đổi rồi, vẽ lại giao diện".
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    // Gọi controller (trả record (user, error) như đã viết Bước 3)
    final (user, error) = await _authController.login(
      _phoneController.text.trim(), // .trim() bỏ khoảng trắng thừa
      _passwordController.text,
    );

    // Nếu widget đã bị gỡ khỏi màn hình thì dừng (tránh lỗi).
    if (!mounted) return;

    if (error != null) {
      // Có lỗi → tắt loading, hiện thông báo
      setState(() {
        _isLoading = false;
        _errorMessage = error;
      });
    } else {
      // Thành công → chuyển sang Home
      // (Bước 5 sẽ điều hướng theo vai trò; giờ tạm về '/')
      context.go('/');
    }
  }

  // dispose: dọn dẹp controller khi rời màn (giải phóng bộ nhớ).
  @override
  void dispose() {
    _phoneController.dispose();
    _passwordController.dispose();
    super.dispose();
  }
  // initState: chạy 1 LẦN khi màn Login được tạo (trước build).
  // Đây là màn đầu tiên của app nên đặt việc kiểm tra version ở đây.
  @override
  void initState() {
    super.initState();
    // addPostFrameCallback: đợi build xong khung đầu tiên rồi mới chạy,
    // để có sẵn context hợp lệ cho việc hiện dialog (Force Update - FR4.4).
    WidgetsBinding.instance.addPostFrameCallback((_) => _checkVersion());
  }

  /// Kiểm tra phiên bản lúc mở app.
  /// Nhận: không tham số. Trả về: `Future<void>` (không trả dữ liệu, chỉ tạo tác dụng phụ).
  /// Luồng: gọi API lấy version → nếu app quá cũ thì bật dialog bắt cập nhật.
  Future<void> _checkVersion() async {
    // fetchVersion trả VersionInfo? — null nghĩa là lỗi mạng, bỏ qua.
    final info = await AppController().fetchVersion();
    if (info == null || !mounted) return;
    // So version app (AppConfig.appVersion) với min từ server.
    if (AppController.needForceUpdate(AppConfig.appVersion, info)) {
      _showForceUpdateDialog(info);
    }
  }

  /// Hiện dialog "Cần cập nhật" không thể đóng (chặn dùng app bản cũ).
  /// Nhận: [info] — thông tin version từ server (message, min, url...).
  /// Trả về: void (chỉ hiển thị UI, không trả dữ liệu).
  void _showForceUpdateDialog(VersionInfo info) {
    showDialog(
      context: context,
      barrierDismissible: false, // không cho tắt bằng cách bấm ra ngoài
      builder: (ctx) => PopScope(
        canPop: false, // chặn nút back
        child: AlertDialog(
          title: const Text('Cần cập nhật'),
          content: Text(
            '${info.message}\n\n'
                'Phiên bản hiện tại: ${AppConfig.appVersion}\n'
                'Yêu cầu tối thiểu: ${info.minSupportedVersion}',
          ),
          actions: [
            ElevatedButton(
              onPressed: () {
                // TODO: mở store bằng url_launcher (làm sau).
                // Tạm thời chỉ hiển thị, không cho đóng.
              },
              child: const Text('Cập nhật ngay'),
            ),
          ],
        ),
      ),
    );
  }
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      // SafeArea: tránh tai thỏ / thanh trạng thái che nội dung.
      body: SafeArea(
        // Padding: chừa lề 24px quanh nội dung.
        child: Padding(
          padding: const EdgeInsets.all(24),
          // Center: căn giữa theo chiều dọc.
          child: Center(
            // SingleChildScrollView: cho cuộn khi bàn phím bật lên.
            child: SingleChildScrollView(
              // Column: xếp các widget theo chiều DỌC.
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch, // kéo rộng hết ngang
                children: [
                  // Logo tạm bằng icon
                  const Icon(Icons.school, size: 72, color: AppColors.primary),
                  const SizedBox(height: 16), // khoảng cách 16px

                  const Text(
                    'FSchool',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Sổ liên lạc điện tử',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: AppColors.textGrey),
                  ),
                  const SizedBox(height: 32),

                  // Ô nhập số điện thoại
                  TextField(
                    controller: _phoneController,
                    keyboardType: TextInputType.phone, // bàn phím số
                    decoration: const InputDecoration(
                      labelText: 'Số điện thoại',
                      prefixIcon: Icon(Icons.phone),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Ô nhập mật khẩu
                  TextField(
                    controller: _passwordController,
                    obscureText: true, // ẩn ký tự (hiện ●●●)
                    decoration: const InputDecoration(
                      labelText: 'Mật khẩu',
                      prefixIcon: Icon(Icons.lock),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Vùng hiện lỗi (chỉ hiện khi có _errorMessage)
                  if (_errorMessage != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(
                        _errorMessage!,
                        style: const TextStyle(color: AppColors.danger),
                        textAlign: TextAlign.center,
                      ),
                    ),

                  // Nút Đăng nhập — khi loading thì disable + hiện vòng xoay
                  ElevatedButton(
                    onPressed: _isLoading ? null : _handleLogin,
                    child: _isLoading
                        ? const SizedBox(
                      height: 22,
                      width: 22,
                      child: CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2,
                      ),
                    )
                        : const Text('Đăng nhập'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}