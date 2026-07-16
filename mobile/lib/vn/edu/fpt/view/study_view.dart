import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';
import 'grades_view.dart';
import 'timetable_view.dart';

/// Tab "Học tập" (FR2.3) cho Học sinh & Phụ huynh.
///
/// Gộp 2 màn con: Thời khóa biểu · Bảng điểm.
/// Chỉ tạo widget (và gọi API) khi lần đầu mở tab đó — tránh tải cả hai cùng lúc.
class StudyTab extends StatefulWidget {
  final UserModel user;
  const StudyTab({super.key, required this.user});

  @override
  State<StudyTab> createState() => _StudyTabState();
}

class _StudyTabState extends State<StudyTab>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;
  /// Các tab đã từng mở.
  final Set<int> _visited = {0};
  int _index = 0;

  bool get _isParent => widget.user.role == 'Parent';

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _tabController.addListener(_onTabChanged);
  }

  void _onTabChanged() {
    if (_tabController.indexIsChanging) return;
    final i = _tabController.index;
    if (i == _index && _visited.contains(i)) return;
    setState(() {
      _index = i;
      _visited.add(i);
    });
  }

  @override
  void dispose() {
    _tabController.removeListener(_onTabChanged);
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Material(
          color: AppColors.white,
          child: TabBar(
            controller: _tabController,
            labelColor: AppColors.primary,
            unselectedLabelColor: AppColors.textGrey,
            indicatorColor: AppColors.primary,
            tabs: const [
              Tab(text: 'Thời khóa biểu'),
              Tab(text: 'Bảng điểm'),
            ],
          ),
        ),
        Expanded(
          // Offstage + if: chỉ mount tab đã mở; tab ẩn vẫn giữ state.
          child: Stack(
            fit: StackFit.expand,
            children: [
              if (_visited.contains(0))
                Offstage(
                  offstage: _index != 0,
                  child: _childScoped(
                    (studentId) => TimetableView(studentId: studentId),
                  ),
                ),
              if (_visited.contains(1))
                Offstage(
                  offstage: _index != 1,
                  child: _childScoped(
                    (studentId) => GradesView(studentId: studentId),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }

  /// Bọc 1 màn con để truyền đúng studentId theo vai trò.
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
        return KeyedSubtree(key: ValueKey(child.id), child: builder(child.id));
      },
    );
  }
}
