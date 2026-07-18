import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../common/app_colors.dart';
import '../controller/auth_controller.dart';

/// Màn quên mật khẩu (FR1.2): Email → OTP → đặt MK mới.
/// Login vẫn bằng SĐT; OTP quên MK gửi về email (Dev: log API `[DEV OTP EMAIL]`).
class ForgotPasswordView extends StatefulWidget {
  const ForgotPasswordView({super.key});

  @override
  State<ForgotPasswordView> createState() => _ForgotPasswordViewState();
}

class _ForgotPasswordViewState extends State<ForgotPasswordView> {
  final _auth = AuthController();
  final _email = TextEditingController();
  final _otp = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();

  /// 0 = nhập email · 1 = nhập OTP · 2 = đặt MK mới
  int _step = 0;
  bool _loading = false;
  String? _error;
  String? _info;
  String? _resetToken;
  String? _maskedEmail;
  bool _obscure = true;

  @override
  void dispose() {
    _email.dispose();
    _otp.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _sendOtp() async {
    final email = _email.text.trim();
    if (email.isEmpty || !email.contains('@')) {
      setState(() => _error = 'Vui lòng nhập email hợp lệ.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
      _info = null;
    });
    final (ok, err, masked) = await _auth.forgotPassword(email);
    if (!mounted) return;
    setState(() => _loading = false);
    if (err != null) {
      setState(() => _error = err);
      return;
    }
    setState(() {
      _step = 1;
      _maskedEmail = masked;
      _info = ok ?? 'Đã gửi OTP về email. Kiểm tra hộp thư Gmail (kể cả Spam).';
    });
  }

  Future<void> _verifyOtp() async {
    final code = _otp.text.trim();
    if (code.length < 4) {
      setState(() => _error = 'Nhập mã OTP đã nhận qua email.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    final (token, err) = await _auth.verifyResetOtp(_email.text.trim(), code);
    if (!mounted) return;
    setState(() => _loading = false);
    if (err != null || token == null) {
      setState(() => _error = err ?? 'Xác thực OTP thất bại.');
      return;
    }
    setState(() {
      _resetToken = token;
      _step = 2;
      _info = 'OTP đúng. Nhập mật khẩu mới.';
    });
  }

  Future<void> _resetPassword() async {
    final p1 = _password.text;
    final p2 = _confirm.text;
    if (p1.length < 6) {
      setState(() => _error = 'Mật khẩu mới tối thiểu 6 ký tự.');
      return;
    }
    if (p1 != p2) {
      setState(() => _error = 'Xác nhận mật khẩu không khớp.');
      return;
    }
    final token = _resetToken;
    if (token == null) {
      setState(() => _error = 'Thiếu resetToken. Quay lại bước OTP.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    final (ok, err) = await _auth.resetPassword(token, p1);
    if (!mounted) return;
    setState(() => _loading = false);
    if (err != null) {
      setState(() => _error = err);
      return;
    }
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(ok ?? 'Đặt lại mật khẩu thành công.')),
    );
    context.go('/login');
  }

  Future<void> _resend() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final (ok, err, masked) = await _auth.resendResetOtp(_email.text.trim());
    if (!mounted) return;
    setState(() => _loading = false);
    if (err != null) {
      setState(() => _error = err);
      return;
    }
    setState(() {
      _maskedEmail = masked ?? _maskedEmail;
      _info = ok ?? 'Đã gửi lại OTP.';
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Quên mật khẩu'),
        backgroundColor: AppColors.primary,
        foregroundColor: AppColors.white,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  _step == 0
                      ? 'Nhập email Gmail đã gắn trên hồ sơ. Hệ thống sẽ gửi mã OTP vào hộp thư thật.'
                      : _step == 1
                          ? 'Nhập mã OTP đã gửi tới ${_maskedEmail ?? 'email của bạn'} (kiểm tra cả Spam).'
                          : 'Đặt mật khẩu mới (tối thiểu 6 ký tự).',
                  style: const TextStyle(color: AppColors.textGrey),
                ),
                const SizedBox(height: 20),
                if (_step == 0) ...[
                  TextField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    autofillHints: const [AutofillHints.email],
                    decoration: const InputDecoration(
                      labelText: 'Email *',
                      prefixIcon: Icon(Icons.email_outlined),
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  _PrimaryButton(
                    loading: _loading,
                    label: 'Gửi OTP',
                    onPressed: _sendOtp,
                  ),
                ] else if (_step == 1) ...[
                  TextField(
                    controller: _otp,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Mã OTP *',
                      prefixIcon: Icon(Icons.pin),
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  _PrimaryButton(
                    loading: _loading,
                    label: 'Xác thực OTP',
                    onPressed: _verifyOtp,
                  ),
                  TextButton(
                    onPressed: _loading ? null : _resend,
                    child: const Text('Gửi lại OTP'),
                  ),
                ] else ...[
                  TextField(
                    controller: _password,
                    obscureText: _obscure,
                    decoration: InputDecoration(
                      labelText: 'Mật khẩu mới *',
                      prefixIcon: const Icon(Icons.lock_outline),
                      border: const OutlineInputBorder(),
                      suffixIcon: IconButton(
                        onPressed: () => setState(() => _obscure = !_obscure),
                        icon: Icon(
                          _obscure ? Icons.visibility_off : Icons.visibility,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _confirm,
                    obscureText: _obscure,
                    decoration: const InputDecoration(
                      labelText: 'Xác nhận mật khẩu *',
                      prefixIcon: Icon(Icons.lock),
                      border: OutlineInputBorder(),
                    ),
                  ),
                  const SizedBox(height: 16),
                  _PrimaryButton(
                    loading: _loading,
                    label: 'Đặt mật khẩu mới',
                    onPressed: _resetPassword,
                  ),
                ],
                if (_info != null) ...[
                  const SizedBox(height: 12),
                  Text(
                    _info!,
                    style: const TextStyle(color: AppColors.primaryDark),
                    textAlign: TextAlign.center,
                  ),
                ],
                if (_error != null) ...[
                  const SizedBox(height: 12),
                  Text(
                    _error!,
                    style: const TextStyle(color: AppColors.danger),
                    textAlign: TextAlign.center,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _PrimaryButton extends StatelessWidget {
  final bool loading;
  final String label;
  final VoidCallback onPressed;
  const _PrimaryButton({
    required this.loading,
    required this.label,
    required this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      child: ElevatedButton(
        onPressed: loading ? null : onPressed,
        child: loading
            ? const SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(
                  strokeWidth: 2,
                  color: Colors.white,
                ),
              )
            : Text(label),
      ),
    );
  }
}
