/// Trạng thái danh sách tải từ API — thay FutureBuilder để tự cập nhật sau CRUD.
class ListLoadState<T> {
  List<T>? items;
  String? error;
  bool loading;

  ListLoadState({this.items, this.error, this.loading = true});
}

/// Gọi API danh sách rồi setState — pattern thống nhất toàn app.
Future<void> reloadList<T>({
  required void Function(void Function()) setState,
  required bool Function() mounted,
  required ListLoadState<T> state,
  required Future<(List<T>?, String?)> Function() fetch,
  bool clearItemsOnStart = true,
}) async {
  setState(() {
    state.loading = true;
    state.error = null;
    if (clearItemsOnStart) state.items = null;
  });
  final (items, error) = await fetch();
  if (!mounted()) return;
  setState(() {
    state.items = items;
    state.error = error;
    state.loading = false;
  });
}
