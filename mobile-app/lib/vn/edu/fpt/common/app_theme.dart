import 'package:flutter/material.dart'; // Thư viện Material của Flutter (ThemeData, AppBar, nút...)
import 'app_colors.dart'; // File màu thương hiệu cùng thư mục (AppColors.primary...)

/// Theme tổng của app — khai báo "bộ áo" chung 1 lần, mọi màn hình tự dùng.
class AppTheme {
  AppTheme._(); // Constructor private: cấm tạo AppTheme() vì chỉ dùng static

  /// Getter trả về ThemeData sáng. Gọi bằng: AppTheme.light
  static ThemeData get light => ThemeData(
    useMaterial3: true, // Bật Material 3 (thiết kế mới: bo góc mềm, màu hài hòa)

    // Màu nền mặc định cho mọi Scaffold (khung màn hình)
    scaffoldBackgroundColor: AppColors.background,

    // Bảng màu tổng thể: đưa 1 màu "hạt giống" (cam), Flutter tự sinh dải màu phối
    colorScheme: ColorScheme.fromSeed(
      seedColor: AppColors.primary, // màu gốc để sinh bảng màu
      primary: AppColors.primary, // ép màu chính đúng cam của mình
    ),

    // ── Định dạng mặc định cho AppBar (thanh trên cùng) ──
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.primary, // nền cam
      foregroundColor: AppColors.white, // chữ + icon trắng
      elevation: 0, // bỏ đổ bóng dưới AppBar (phẳng)
      centerTitle: true, // căn giữa tiêu đề
    ),

    // ── Định dạng mặc định cho nút chính ElevatedButton ──
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.primary, // nền nút cam
        foregroundColor: AppColors.white, // chữ trắng
        minimumSize: const Size.fromHeight(50), // cao tối thiểu 50, rộng hết ngang
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12), // bo góc 12px
        ),
      ),
    ),

    // ── Định dạng mặc định cho ô nhập TextField ──
    inputDecorationTheme: InputDecorationTheme(
      filled: true, // có tô nền
      fillColor: AppColors.white, // nền ô màu trắng
      // Viền lúc bình thường: bo góc 12, màu xám
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.textGrey),
      ),
      // Viền khi đang gõ vào ô (focus): đổi sang cam, dày 2px
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.primary, width: 2),
      ),
    ),
  );
}