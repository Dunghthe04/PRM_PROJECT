/// Thông tin phiên bản app từ API (`GET /api/app/version`) — Force Update FR4.4.
///
/// Không có quan hệ entity DB; response cấu hình từ `appsettings`.
class VersionInfo {
  /// Bản mới nhất trên store.
  final String latestVersion;

  /// Bản tối thiểu còn được phép chạy.
  final String minSupportedVersion;

  /// Link mở store / trang cập nhật.
  final String updateUrl;

  /// Thông báo hiển thị trong dialog bắt cập nhật.
  final String message;

  VersionInfo({
    required this.latestVersion,
    required this.minSupportedVersion,
    required this.updateUrl,
    required this.message,
  });

  /// Parse JSON response version check (mọi field có fallback an toàn).
  factory VersionInfo.fromJson(Map<String, dynamic> json) {
    return VersionInfo(
      latestVersion: json['latestVersion'] as String? ?? '1.0.0',
      minSupportedVersion: json['minSupportedVersion'] as String? ?? '1.0.0',
      updateUrl: json['updateUrl'] as String? ?? '',
      message: json['message'] as String? ?? 'Vui lòng cập nhật ứng dụng.',
    );
  }
}
