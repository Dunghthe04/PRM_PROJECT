import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';
import 'assignments_view.dart';
import 'grades_view.dart';
import 'timetable_view.dart';

/// Tab "Học tập" (FR2.3, FR2.4) cho Học sinh & Phụ huynh.
///
/// Gộp 3 màn con qua TabBar: Thời khóa biểu · Bảng điểm · Bài tập.
/// - Học sinh: xem dữ liệu của chính mình, được nộp bài.
/// - Phụ huynh: xem theo con đang chọn (Switch Profile), chỉ theo dõi.
///
/// Là 1 tab trong _MainShell (đã có Scaffold + AppBar) nên KHÔNG tự tạo
/// Scaffold, chỉ trả về nội dung.
class StudyTab extends StatelessWidget {
  final UserModel user;
  const StudyTab({super.key, required this.user});

  bool get _isParent => user.role == 'Parent';

  @override
  Widget build(BuildContext context) {
    // DefaultTabController đặt NGOÀI phần đổi con → khi PH đổi con, chỉ nội dung
    // các tab con nạp lại, không reset về tab đầu.
    return DefaultTabController(
      length: 3,
      child: Column(
        children: [
          const Material(
            color: AppColors.white,
            child: TabBar(
              labelColor: AppColors.primary,
              unselectedLabelColor: AppColors.textGrey,
              indicatorColor: AppColors.primary,
              tabs: [
                Tab(text: 'Thời khóa biểu'),
                Tab(text: 'Bảng điểm'),
                Tab(text: 'Bài tập'),
              ],
            ),
          ),
          Expanded(
            child: TabBarView(
              children: [
                _childScoped((studentId) => TimetableView(studentId: studentId)),
                _childScoped((studentId) => GradesView(studentId: studentId)),
                _childScoped(
                  (studentId) => AssignmentsView(
                    studentId: studentId,
                    // Chỉ Học sinh được nộp bài; Phụ huynh chỉ theo dõi.
                    canSubmit: !_isParent,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Bọc 1 màn con để truyền đúng studentId theo vai trò.
  ///
  /// Nhận: [builder] — hàm dựng màn con từ studentId (null nếu là HS).
  /// - HS: gọi builder(null) trực tiếp.
  /// - PH: nghe ParentSession.selectedChild → dựng lại màn với id con đang chọn;
  ///   chưa chọn/không có con thì hiện thông báo.
  Widget _childScoped(Widget Function(int? studentId) builder) {
    if (!_isParent) return builder(null);

    return ValueListenableBuilder<UserModel?>(
      valueListenable: ParentSession.instance.selectedChild,
      builder: (context, child, _) {
        if (child == null) {
          return const Center(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                'Chưa có học sinh liên kết.\nVui lòng liên hệ nhà trường.',
                textAlign: TextAlign.center,
              ),
            ),
          );
        }
        // ValueKey theo id con → Flutter tạo state mới khi đổi con (nạp lại sạch).
        return KeyedSubtree(key: ValueKey(child.id), child: builder(child.id));
      },
    );
  }
}
