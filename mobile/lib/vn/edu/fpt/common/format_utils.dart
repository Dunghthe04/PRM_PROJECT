/// Các hàm định dạng hiển thị dùng chung (ngày giờ...).
class FormatUtils {
  /// Định dạng DateTime thành "dd/MM/yyyy HH:mm" (giờ địa phương).
  ///
  /// Nhận: [dt] — thời điểm cần hiển thị.
  /// Trả về (String): chuỗi đã format, vd "14/07/2026 08:05".
  static String dateTime(DateTime dt) {
    final local = dt.toLocal();
    final d = _pad2(local.day);
    final mo = _pad2(local.month);
    final y = local.year;
    final h = _pad2(local.hour);
    final mi = _pad2(local.minute);
    return '$d/$mo/$y $h:$mi';
  }

  /// Định dạng DateTime thành "dd/MM/yyyy" (chỉ ngày, không giờ).
  ///
  /// Nhận: [dt] — thời điểm cần hiển thị.
  /// Trả về (String): vd "14/07/2026".
  static String date(DateTime dt) {
    final local = dt.toLocal();
    return '${_pad2(local.day)}/${_pad2(local.month)}/${local.year}';
  }

  /// Định dạng "cách đây bao lâu" ngắn gọn (vd "5 phút trước").
  ///
  /// Nhận: [dt] — mốc thời gian trong quá khứ.
  /// Trả về (String): mô tả tương đối; quá 7 ngày thì trả về ngày đầy đủ.
  static String timeAgo(DateTime dt) {
    final diff = DateTime.now().difference(dt.toLocal());
    if (diff.inMinutes < 1) return 'Vừa xong';
    if (diff.inMinutes < 60) return '${diff.inMinutes} phút trước';
    if (diff.inHours < 24) return '${diff.inHours} giờ trước';
    if (diff.inDays < 7) return '${diff.inDays} ngày trước';
    return dateTime(dt);
  }

  /// Thêm số 0 phía trước cho số < 10 (vd 5 -> "05").
  static String _pad2(int n) => n.toString().padLeft(2, '0');

  /// Định dạng số tiền kiểu Việt Nam (dấu chấm ngăn cách nghìn) + " ₫".
  ///
  /// Nhận: [amount] — số tiền.
  /// Trả về (String): vd 1500000 -> "1.500.000 ₫".
  static String currency(double amount) {
    // Bỏ phần thập phân (học phí thường là số nguyên đồng).
    final whole = amount.round().toString();
    final buffer = StringBuffer();
    // Chèn dấu '.' sau mỗi 3 chữ số tính từ phải sang.
    for (int i = 0; i < whole.length; i++) {
      if (i > 0 && (whole.length - i) % 3 == 0) buffer.write('.');
      buffer.write(whole[i]);
    }
    return '$buffer ₫';
  }
}
