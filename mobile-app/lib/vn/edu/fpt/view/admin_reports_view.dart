import 'package:flutter/material.dart';
import '../common/app_colors.dart';
import '../common/format_utils.dart';
import '../controller/admin_catalog_controller.dart';
import '../controller/admin_report_controller.dart';

/// Dashboard báo cáo Admin (FR5.5) — xem trên màn, không xuất Excel/PDF.
class AdminReportsPage extends StatefulWidget {
  const AdminReportsPage({super.key});

  @override
  State<AdminReportsPage> createState() => _AdminReportsPageState();
}

class _AdminReportsPageState extends State<AdminReportsPage> {
  final _reports = AdminReportController();
  final _catalog = AdminCatalogController();
  ReportDashboardModel? _dash;
  List<ClassModel> _classes = [];
  int? _classId;
  String? _error;
  bool _loading = true;

  // Chi tiết theo tab
  List<GradeReportRow> _grades = [];
  List<AttendanceReportRow> _attendance = [];
  double? _attRate;
  List<FeeReportRow> _fees = [];
  String? _detailError;
  bool _detailLoading = false;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final results = await Future.wait([
      _reports.getDashboard(),
      _catalog.getClasses(),
    ]);
    if (!mounted) return;
    final (dash, dErr) = results[0] as (ReportDashboardModel?, String?);
    final (classes, _) = results[1] as (List<ClassModel>?, String?);
    setState(() {
      _dash = dash;
      _error = dErr;
      _classes = classes ?? [];
      if (_classes.isNotEmpty) _classId ??= _classes.first.id;
      _loading = false;
    });
    if (_classId != null) _loadDetails();
  }

  Future<void> _loadDetails() async {
    final classId = _classId;
    if (classId == null) return;
    setState(() {
      _detailLoading = true;
      _detailError = null;
    });
    final results = await Future.wait([
      _reports.getGrades(classId: classId),
      _reports.getAttendance(classId: classId),
      _reports.getFees(classId: classId),
    ]);
    if (!mounted) return;
    final (grades, gErr) = results[0] as (List<GradeReportRow>?, String?);
    final (att, aErr, rate) =
        results[1] as (List<AttendanceReportRow>?, String?, double?);
    final (fees, fErr) = results[2] as (List<FeeReportRow>?, String?);
    setState(() {
      _grades = grades ?? [];
      _attendance = att ?? [];
      _attRate = rate;
      _fees = fees ?? [];
      _detailError = gErr ?? aErr ?? fErr;
      _detailLoading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!),
            ElevatedButton(onPressed: _bootstrap, child: const Text('Thử lại')),
          ],
        ),
      );
    }
    final d = _dash!;
    return RefreshIndicator(
      onRefresh: _bootstrap,
      child: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _StatCard('Học sinh', '${d.totalStudents}', Icons.school),
              _StatCard('Giáo viên', '${d.totalTeachers}', Icons.person),
              _StatCard('Lớp', '${d.totalClasses}', Icons.class_),
              _StatCard('Phụ huynh', '${d.totalParents}', Icons.family_restroom),
              _StatCard('Điểm đã công bố', '${d.publishedGradeCount}', Icons.grade),
              _StatCard('Chuyên cần TB', d.attendanceLabel, Icons.checklist),
              _StatCard('HĐ chờ', '${d.pendingInvoiceCount}', Icons.pending),
              _StatCard('Đã thu', d.paidLabel, Icons.payments),
              _StatCard('Còn nợ', d.pendingLabel, Icons.money_off),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            'Cập nhật: ${FormatUtils.dateTime(d.generatedAt)}',
            style: const TextStyle(color: AppColors.textGrey, fontSize: 12),
          ),
          const Divider(height: 32),
          Row(
            children: [
              const Text('Chi tiết theo lớp: ',
                  style: TextStyle(fontWeight: FontWeight.w600)),
              if (_classes.isNotEmpty)
                DropdownButton<int>(
                  value: _classId,
                  items: _classes
                      .map((c) =>
                          DropdownMenuItem(value: c.id, child: Text(c.name)))
                      .toList(),
                  onChanged: (v) {
                    setState(() => _classId = v);
                    _loadDetails();
                  },
                ),
            ],
          ),
          if (_detailLoading)
            const Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (_detailError != null)
            Text(_detailError!, style: const TextStyle(color: AppColors.danger))
          else ...[
            const SizedBox(height: 8),
            Text(
              'Chuyên cần lớp: ${_attRate == null ? "—" : "${_attRate!.toStringAsFixed(1)}%"}',
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 12),
            _section('Bảng điểm', _grades.isEmpty
                ? const Text('Không có dữ liệu.')
                : Column(
                    children: _grades
                        .take(50)
                        .map((r) => ListTile(
                              dense: true,
                              title: Text(r.studentName),
                              subtitle: Text(
                                  '${r.subjectName} • ${r.assessmentType}'),
                              trailing: Text(r.score.toStringAsFixed(1),
                                  style: const TextStyle(
                                      fontWeight: FontWeight.bold)),
                            ))
                        .toList(),
                  )),
            _section(
                'Chuyên cần',
                _attendance.isEmpty
                    ? const Text('Không có dữ liệu.')
                    : Column(
                        children: _attendance
                            .map((r) => ListTile(
                                  dense: true,
                                  title: Text(r.studentName),
                                  subtitle: Text(
                                      'P:${r.present} A:${r.absent} L:${r.late} / ${r.totalSessions}'),
                                  trailing: Text(
                                      '${r.attendanceRate.toStringAsFixed(0)}%'),
                                ))
                            .toList(),
                      )),
            _section(
                'Học phí',
                _fees.isEmpty
                    ? const Text('Không có dữ liệu.')
                    : Column(
                        children: _fees
                            .take(50)
                            .map((r) => ListTile(
                                  dense: true,
                                  title: Text(
                                      '${r.studentName} • ${r.categoryName}'),
                                  subtitle: Text(r.status),
                                  trailing: Text(
                                      FormatUtils.currency(r.amount),
                                      style: TextStyle(
                                        color: r.isPaid
                                            ? AppColors.success
                                            : AppColors.textDark,
                                        fontWeight: FontWeight.w600,
                                      )),
                                ))
                            .toList(),
                      )),
          ],
        ],
      ),
    );
  }

  Widget _section(String title, Widget child) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title,
                style:
                    const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            child,
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  const _StatCard(this.label, this.value, this.icon);

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 160,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: AppColors.primary, size: 20),
              const SizedBox(height: 8),
              Text(value,
                  style: const TextStyle(
                      fontSize: 18, fontWeight: FontWeight.bold)),
              Text(label,
                  style:
                      const TextStyle(fontSize: 12, color: AppColors.textGrey)),
            ],
          ),
        ),
      ),
    );
  }
}
