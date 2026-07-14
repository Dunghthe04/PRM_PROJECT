/// Model đại diện 1 user — khớp với UserDto bên API .NET.
class UserModel {
  final int id;
  final String phone;
  final String fullName;
  final String avatarUrl;
  final String? email; // có thể null
  final bool isPhoneVerified;
  final bool isLocked;
  final String role; // "Admin" | "HeadOfDept" | "Teacher" | "Parent" | "Student"

  // Constructor: 'required' = bắt buộc truyền khi tạo object.
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

/// factory fromJson: "nhà máy" tạo UserModel từ Map JSON mà API trả về.
/// json là dữ liệu dạng {key: value} sau khi Dio parse response.
/// Dio parse json -> map -> chuyển sang model
  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as int,
      phone: json['phone'] as String,
      fullName: json['fullName'] as String,
      // ?? '' : nếu null thì dùng chuỗi rỗng (tránh crash)
      avatarUrl: json['avatarUrl'] as String? ?? '',
      email: json['email'] as String?,
      isPhoneVerified: json['isPhoneVerified'] as bool,
      isLocked: json['isLocked'] as bool,
      role: json['role'] as String,
    );
  }

  /// Nhãn vai trò bằng tiếng Việt để hiển thị (role gốc là tiếng Anh).
  String get roleLabel {
    switch (role) {
      case 'Admin':
        return 'Quản trị viên';
      case 'HeadOfDept':
        return 'Trưởng bộ môn';
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