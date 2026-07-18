import '../model/grade_model.dart';

/// Đầu điểm sổ điểm THPT (3 miệng · 3×15p · 2×1 tiết · GK · CK).
class ThptAssessment {
  final String type;
  final String label;
  final int weight;

  const ThptAssessment(this.type, this.label, this.weight);
}

/// Thứ tự cột chuẩn sổ điểm lớp — luôn hiển thị đủ (ô trống = —).
const List<ThptAssessment> kThptAssessments = [
  ThptAssessment('Oral1', 'Miệng 1', 1),
  ThptAssessment('Oral2', 'Miệng 2', 1),
  ThptAssessment('Oral3', 'Miệng 3', 1),
  ThptAssessment('Quiz15_1', '15 phút 1', 1),
  ThptAssessment('Quiz15_2', '15 phút 2', 1),
  ThptAssessment('Quiz15_3', '15 phút 3', 1),
  ThptAssessment('OnePeriod1', '1 tiết 1', 2),
  ThptAssessment('OnePeriod2', '1 tiết 2', 2),
  ThptAssessment('Midterm', 'Giữa kỳ', 2),
  ThptAssessment('Final', 'Cuối kỳ', 3),
];

/// Nhãn tiếng Việt; hỗ trợ mã cũ (Oral / Quiz15 / OnePeriod).
String assessmentLabelVi(String type) {
  for (final a in kThptAssessments) {
    if (a.type == type) return a.label;
  }
  switch (type) {
    case 'Oral':
      return 'Miệng';
    case 'Quiz15':
      return '15 phút';
    case 'OnePeriod':
      return '1 tiết';
    default:
      return type;
  }
}

/// Hệ số theo loại điểm.
int assessmentWeight(String type) {
  for (final a in kThptAssessments) {
    if (a.type == type) return a.weight;
  }
  // Legacy
  if (type == 'Oral' || type.startsWith('Oral')) return 1;
  if (type == 'Quiz15' || type.startsWith('Quiz15')) return 1;
  if (type == 'OnePeriod' || type.startsWith('OnePeriod')) return 2;
  if (type == 'Midterm') return 2;
  if (type == 'Final') return 3;
  return 1;
}

/// TBHK = Σ(điểm × hệ số) / Σ hệ số. Không có điểm → null.
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

bool isPassed(double? average) => average != null && average >= 5.0;

/// Nhóm điểm theo môn; sắp theo thứ tự cột sổ điểm.
Map<int, List<GradeModel>> groupBySubject(List<GradeModel> grades) {
  final map = <int, List<GradeModel>>{};
  for (final g in grades) {
    map.putIfAbsent(g.subjectId, () => []).add(g);
  }
  final order = {
    for (var i = 0; i < kThptAssessments.length; i++)
      kThptAssessments[i].type: i,
  };
  for (final list in map.values) {
    list.sort((a, b) {
      final ia = order[a.assessmentType] ?? 100;
      final ib = order[b.assessmentType] ?? 100;
      final c = ia.compareTo(ib);
      if (c != 0) return c;
      return a.assessmentType.compareTo(b.assessmentType);
    });
  }
  return map;
}
