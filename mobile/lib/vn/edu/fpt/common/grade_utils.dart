import '../model/grade_model.dart';

/// Đầu điểm form bảng điểm THPT (cấp 3) + hệ số tính TBHK.
class ThptAssessment {
  final String type;
  final String label;
  final int weight; // hệ số

  const ThptAssessment(this.type, this.label, this.weight);
}

/// Thứ tự hiển thị chuẩn sổ điểm lớp.
const List<ThptAssessment> kThptAssessments = [
  ThptAssessment('Oral', 'Miệng', 1),
  ThptAssessment('Quiz15', '15 phút', 1),
  ThptAssessment('OnePeriod', '1 tiết', 2),
  ThptAssessment('Midterm', 'Giữa kỳ', 2),
  ThptAssessment('Final', 'Cuối kỳ', 3),
];

/// Nhãn tiếng Việt; type lạ → trả nguyên chuỗi.
String assessmentLabelVi(String type) {
  for (final a in kThptAssessments) {
    if (a.type == type) return a.label;
  }
  switch (type) {
    case 'Oral2':
      return 'Miệng 2';
    case 'Quiz15_2':
      return '15 phút 2';
    default:
      return type;
  }
}

/// Hệ số; type không chuẩn → 1.
int assessmentWeight(String type) {
  for (final a in kThptAssessments) {
    if (a.type == type) return a.weight;
  }
  if (type.startsWith('Oral') || type.startsWith('Quiz15')) return 1;
  if (type.startsWith('OnePeriod') || type == 'Midterm') return 2;
  if (type == 'Final') return 3;
  return 1;
}

/// TBHK có hệ số: Σ(điểm × hệ số) / Σ(hệ số). Không có điểm → null.
double? weightedAverage(List<GradeModel> grades) {
  if (grades.isEmpty) return null;
  var sum = 0.0;
  var wSum = 0;
  for (final g in grades) {
    final w = assessmentWeight(g.assessmentType);
    sum += g.score * w;
    wSum += w;
  }
  if (wSum == 0) return null;
  return sum / wSum;
}

/// Đạt khi TB ≥ 5.0 (thang 10).
bool isPassed(double? average) => average != null && average >= 5.0;

/// Nhóm điểm theo môn trong 1 kỳ (key = subjectId).
Map<int, List<GradeModel>> groupBySubject(List<GradeModel> grades) {
  final map = <int, List<GradeModel>>{};
  for (final g in grades) {
    map.putIfAbsent(g.subjectId, () => []).add(g);
  }
  for (final list in map.values) {
    list.sort((a, b) {
      final wa = assessmentWeight(a.assessmentType);
      final wb = assessmentWeight(b.assessmentType);
      final c = wa.compareTo(wb);
      if (c != 0) return c;
      return a.assessmentType.compareTo(b.assessmentType);
    });
  }
  return map;
}
