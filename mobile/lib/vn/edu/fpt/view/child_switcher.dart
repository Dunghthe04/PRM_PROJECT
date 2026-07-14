import 'package:flutter/material.dart';
import '../model/user_model.dart';
import '../service/parent_session.dart';

/// Nút đổi con (FR2.1) đặt trên AppBar cho phụ huynh.
/// Hiện tên con đang chọn; bấm để mở danh sách chọn con khác.
///
/// Tự lắng nghe [ParentSession] nên khi đổi con, giao diện tự cập nhật.
class ChildSwitcher extends StatelessWidget {
  const ChildSwitcher({super.key});

  @override
  Widget build(BuildContext context) {
    final session = ParentSession.instance;

    // ValueListenableBuilder: vẽ lại khi danh sách con thay đổi.
    return ValueListenableBuilder<List<UserModel>>(
      valueListenable: session.children,
      builder: (context, children, _) {
        // Chưa có con nào (chưa gán) → không hiện gì.
        if (children.isEmpty) return const SizedBox.shrink();

        // Lắng nghe tiếp con đang chọn để hiện đúng tên.
        return ValueListenableBuilder<UserModel?>(
          valueListenable: session.selectedChild,
          builder: (context, selected, _) {
            return TextButton.icon(
              onPressed: () => _openPicker(context, children, selected),
              icon: const Icon(Icons.people, color: Colors.white, size: 20),
              label: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  // Giới hạn bề rộng tên để không tràn AppBar.
                  ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 110),
                    child: Text(
                      selected?.fullName ?? 'Chọn con',
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: Colors.white),
                    ),
                  ),
                  const Icon(Icons.arrow_drop_down, color: Colors.white),
                ],
              ),
            );
          },
        );
      },
    );
  }

  /// Mở bottom sheet liệt kê các con để chọn.
  void _openPicker(
    BuildContext context,
    List<UserModel> children,
    UserModel? selected,
  ) {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text('Chọn hồ sơ con',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ),
            ...children.map((c) => ListTile(
                  leading: const CircleAvatar(child: Icon(Icons.person)),
                  title: Text(c.fullName),
                  subtitle: Text('SĐT: ${c.phone}'),
                  // Con đang chọn → dấu tích.
                  trailing: c.id == selected?.id
                      ? const Icon(Icons.check_circle, color: Colors.green)
                      : null,
                  onTap: () {
                    ParentSession.instance.select(c);
                    Navigator.pop(ctx);
                  },
                )),
          ],
        ),
      ),
    );
  }
}
