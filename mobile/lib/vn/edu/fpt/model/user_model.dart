/// Model tài khoản người dùng — khớp `UserDto` bên API .NET.
///
/// Quan hệ phía server: bảng `Users`; liên kết PH↔HS qua `StudentParents`
/// (Switch Profile dùng API `/account/children`, không nhúng trong model này).
class UserModel {
  /// Khóa chính user trên server.
  final int id;

  /// SĐT đăng nhập (định danh chính).
  final String phone;

  /// Họ tên hiển thị.
  final String fullName;

  /// URL avatar (có thể rỗng).
  final String avatarUrl;

  /// Email liên hệ — tuỳ chọn, không dùng để login.
  final String? email;

  /// Đã xác thực OTP đăng ký hay chưa.
  final bool isPhoneVerified;

  /// Tài khoản bị Admin khóa → không cho đăng nhập.
  final bool isLocked;

  /// Vai trò RBAC: `Admin` | `Teacher` | `Parent` | `Student`.
  final String role;

  UserModel({
    required this.id,
    required this.phone,
    required this.fullName,
    required this.avatarUrl,
    this.email,
    required this.isPhoneVerified,
    required this.isLocked,
    required this.role,
  });

  /// Tạo [UserModel] từ JSON response API (`UserDto`).
  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as int,
      phone: json['phone'] as String,
      fullName: json['fullName'] as String,
      avatarUrl: json['avatarUrl'] as String? ?? '',
      email: json['email'] as String?,
      isPhoneVerified: json['isPhoneVerified'] as bool,
      isLocked: json['isLocked'] as bool? ?? false,
      role: json['role'] as String,
    );
  }

  /// Nhãn vai trò tiếng Việt để hiển thị UI.
  String get roleLabel {
    switch (role) {
      case 'Admin':
        return 'Quản trị viên';
      case 'Teacher':
        return 'Giáo viên';
      case 'Parent':
        return 'Phụ huynh';
      case 'Student':
        return 'Học sinh';
      default:
        return role;
    }
  }
}
