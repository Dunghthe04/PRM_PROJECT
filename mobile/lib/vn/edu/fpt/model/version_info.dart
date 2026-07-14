/// Model chứa thông tin phiên bản do API trả về (GET /api/app/version).
/// Dùng cho tính năng Force Update (bắt cập nhật) — FR4.4.
class VersionInfo {
  final String latestVersion; // bản mới nhất hiện có trên store
  final String minSupportedVersion; // bản tối thiểu còn được phép dùng
  final String updateUrl; // link tải/cập nhật (mở khi bấm "Cập nhật ngay")
  final String message; // thông báo hiển thị cho user trong dialog

  VersionInfo({
    required this.latestVersion,
    required this.minSupportedVersion,
    required this.updateUrl,
    required this.message,
  });

  /// factory fromJson: tạo [VersionInfo] từ Map JSON của response.
  ///
  /// Nhận:
  ///   - [json]: Map dữ liệu sau khi Dio parse (vd
  ///     {"latestVersion": "1.0.0", "minSupportedVersion": "1.0.0", ...}).
  /// Trả về: một object [VersionInfo] đã điền đủ field.
  ///
  /// Mọi field đều có giá trị mặc định (?? ...) để tránh crash nếu
  /// server thiếu key nào đó.
  factory VersionInfo.fromJson(Map<String, dynamic> json) {
    return VersionInfo(
      latestVersion: json['latestVersion'] as String? ?? '1.0.0',
      minSupportedVersion: json['minSupportedVersion'] as String? ?? '1.0.0',
      updateUrl: json['updateUrl'] as String? ?? '',
      message: json['message'] as String? ?? 'Vui lòng cập nhật ứng dụng.',
    );
  }
}
