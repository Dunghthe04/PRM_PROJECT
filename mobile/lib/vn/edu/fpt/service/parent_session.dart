import 'package:flutter/foundation.dart';
import '../model/user_model.dart';

/// Giữ trạng thái "phụ huynh đang xem con nào" (FR2.1 — Switch Profile).
///
/// Dùng singleton + ValueNotifier (đều là lõi Flutter, không cần thư viện
/// state-management) để nhiều màn có thể cùng đọc/đổi con đang chọn và tự
/// cập nhật giao diện qua ValueListenableBuilder.
class ParentSession {
  // Constructor riêng tư + instance tĩnh = mẫu Singleton (chỉ 1 thể hiện).
  ParentSession._();
  static final ParentSession instance = ParentSession._();

  /// Danh sách con của phụ huynh (lấy từ API 1 lần khi vào app).
  final ValueNotifier<List<UserModel>> children = ValueNotifier([]);

  /// Con đang được chọn để xem dữ liệu (điểm, TKB, học phí... ở các ngày sau).
  final ValueNotifier<UserModel?> selectedChild = ValueNotifier(null);

  /// Nạp danh sách con. Nếu chưa chọn con nào thì mặc định chọn con đầu tiên.
  ///
  /// Nhận: [list] — danh sách con lấy từ API.
  /// Trả về: void (chỉ cập nhật state nội bộ).
  void setChildren(List<UserModel> list) {
    children.value = list;
    if (selectedChild.value == null && list.isNotEmpty) {
      selectedChild.value = list.first;
    }
  }

  /// Chọn 1 con để xem.
  /// Nhận: [child] — con được chọn. Trả về: void.
  void select(UserModel child) => selectedChild.value = child;

  /// Xóa toàn bộ khi đăng xuất.
  void clear() {
    children.value = [];
    selectedChild.value = null;
  }
}
