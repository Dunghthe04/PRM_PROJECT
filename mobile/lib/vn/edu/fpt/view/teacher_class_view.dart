import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/teacher_controller.dart';
import '../model/teacher_class_model.dart';
import '../model/user_model.dart';
import 'attendance_entry_view.dart';

/// Tab "Lớp học" cho Giáo viên (FR3.1, FR3.2).
/// Liệt kê lớp+môn được phân công; mỗi lớp có 2 lối tắt: Nhập điểm & Điểm danh.
class TeacherClassTab extends StatefulWidget {
  final UserModel user;
  const TeacherClassTab({super.key, required this.user});

  @override
  State<TeacherClassTab> createState() => _TeacherClassTabState();
}

class _TeacherClassTabState extends State<TeacherClassTab> {
  final TeacherController _controller = TeacherController();
  late Future<(List<TeacherClassModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _controller.getMyClasses(widget.user.id);
  }

  Future<void> _reload() async {
    setState(() => _future = _controller.getMyClasses(widget.user.id));
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<(List<TeacherClassModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        final (list, error) =
            snapshot.data ?? (null, 'Không tải được dữ liệu.');
        if (error != null) {
          return Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.error_outline,
                    color: AppColors.danger, size: 40),
                const SizedBox(height: 12),
                Text(error, textAlign: TextAlign.center),
                const SizedBox(height: 12),
                ElevatedButton(onPressed: _reload, child: const Text('Thử lại')),
              ],
            ),
          );
        }
        final classes = list ?? [];
        if (classes.isEmpty) {
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Bạn chưa được phân công lớp nào.')),
              ],
            ),
          );
        }
        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView.separated(
            padding: const EdgeInsets.all(12),
            itemCount: classes.length,
            separatorBuilder: (_, _) => const SizedBox(height: 8),
            itemBuilder: (context, index) => _ClassCard(item: classes[index]),
          ),
        );
      },
    );
  }
}

/// Thẻ 1 lớp+môn với 2 nút hành động.
class _ClassCard extends StatelessWidget {
  final TeacherClassModel item;
  const _ClassCard({required this.item});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                  child: const Icon(Icons.class_, color: AppColors.primary),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${item.className} • ${item.subjectName}',
                          style: const TextStyle(
                              fontWeight: FontWeight.bold, fontSize: 16)),
                      if (item.semesterName != null)
                        Text(item.semesterName!,
                            style: const TextStyle(
                                fontSize: 12, color: AppColors.textGrey)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => AttendanceEntryView(teacherClass: item),
                  ),
                ),
                icon: const Icon(Icons.checklist),
                label: const Text('Điểm danh'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
