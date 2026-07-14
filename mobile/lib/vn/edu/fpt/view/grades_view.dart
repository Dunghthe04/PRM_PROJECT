import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../controller/grade_controller.dart';
import '../model/grade_model.dart';

/// Màn Bảng điểm chi tiết (FR2.3).
///
/// Dùng cho Học sinh (điểm của mình) và Phụ huynh (điểm của con đang chọn).
/// - [studentId]: null nếu là HS; id con nếu là PH.
class GradesView extends StatefulWidget {
  final int? studentId;
  const GradesView({super.key, this.studentId});

  @override
  State<GradesView> createState() => _GradesViewState();
}

class _GradesViewState extends State<GradesView> {
  final GradeController _controller = GradeController();
  late Future<(List<GradeModel>?, String?)> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  @override
  void didUpdateWidget(covariant GradesView oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.studentId != widget.studentId) {
      setState(() => _future = _load());
    }
  }

  Future<(List<GradeModel>?, String?)> _load() {
    return _controller.getMyGrades(studentId: widget.studentId);
  }

  Future<void> _reload() async {
    setState(() => _future = _load());
    await _future;
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<(List<GradeModel>?, String?)>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }

        final (list, error) = snapshot.data ?? (null, 'Không tải được dữ liệu.');

        if (error != null) {
          return _buildRetry(error);
        }

        if (list == null || list.isEmpty) {
          return RefreshIndicator(
            onRefresh: _reload,
            child: ListView(
              children: const [
                SizedBox(height: 120),
                Center(child: Text('Chưa có điểm nào được công bố.')),
              ],
            ),
          );
        }

        // Nhóm điểm theo môn (subjectName) để hiển thị từng khối.
        final Map<String, List<GradeModel>> bySubject = {};
        for (final g in list) {
          bySubject.putIfAbsent(g.subjectName, () => []).add(g);
        }
        final subjects = bySubject.keys.toList()..sort();

        return RefreshIndicator(
          onRefresh: _reload,
          child: ListView.builder(
            padding: const EdgeInsets.all(12),
            itemCount: subjects.length,
            itemBuilder: (context, index) {
              final subject = subjects[index];
              return _SubjectGradeCard(
                subjectName: subject,
                grades: bySubject[subject]!,
              );
            },
          ),
        );
      },
    );
  }

  Widget _buildRetry(String message) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48, color: AppColors.danger),
          const SizedBox(height: 12),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Text(message, textAlign: TextAlign.center),
          ),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            onPressed: _reload,
            icon: const Icon(Icons.refresh),
            label: const Text('Thử lại'),
          ),
        ],
      ),
    );
  }
}

/// Thẻ điểm của 1 môn: tên môn + điểm trung bình + danh sách đầu điểm.
class _SubjectGradeCard extends StatelessWidget {
  final String subjectName;
  final List<GradeModel> grades;
  const _SubjectGradeCard({required this.subjectName, required this.grades});

  /// Điểm trung bình (cộng đơn giản) các đầu điểm của môn.
  double get _average {
    if (grades.isEmpty) return 0;
    final sum = grades.fold<double>(0, (acc, g) => acc + g.score);
    return sum / grades.length;
  }

  /// Màu theo mức điểm: >=8 xanh, >=5 cam, còn lại đỏ.
  Color _scoreColor(double score) {
    if (score >= 8) return AppColors.success;
    if (score >= 5) return AppColors.primary;
    return AppColors.danger;
  }

  @override
  Widget build(BuildContext context) {
    final avg = _average;
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Hàng đầu: tên môn + điểm TB.
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    subjectName,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: _scoreColor(avg).withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    'TB: ${avg.toStringAsFixed(1)}',
                    style: TextStyle(
                      color: _scoreColor(avg),
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const Divider(),
            // Từng đầu điểm: loại điểm + điểm số.
            ...grades.map(
              (g) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 3),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('${g.assessmentLabel}  •  ${g.semesterName}'),
                    Text(
                      g.score.toStringAsFixed(1),
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        color: _scoreColor(g.score),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
