import 'package:flutter/material.dart';
import '../models/login_attempt.dart';
import '../models/virtual_id.dart';
import '../services/auth_api.dart';
import '../services/biometric_service.dart';
import '../utils/constants.dart';
import 'primary_button.dart';

class LoginConfirmationSheet extends StatefulWidget {
  const LoginConfirmationSheet({
    super.key,
    required this.attempt,
    required this.virtualId,
  });

  final LoginAttempt attempt;
  final VirtualId virtualId;

  @override
  State<LoginConfirmationSheet> createState() => _LoginConfirmationSheetState();
}

class _LoginConfirmationSheetState extends State<LoginConfirmationSheet> {
  final _biometric = BiometricService();
  final _api = AuthApi();

  bool _loading = false;
  String? _errorMessage;
  bool _biometricsAvailable = true;

  @override
  void initState() {
    super.initState();
    _checkBiometrics();
  }

  Future<void> _checkBiometrics() async {
    final available = await _biometric.isAvailable();
    if (mounted) {
      setState(() => _biometricsAvailable = available);
    }
  }

  Future<void> _onNadaljuj() async {
    setState(() {
      _loading = true;
      _errorMessage = null;
    });

    if (_biometricsAvailable) {
      try {
        final authenticated = await _biometric.authenticate();
        if (!authenticated) {
          setState(() {
            _loading = false;
            _errorMessage = AppStrings.biometrijaNiUspela;
          });
          return;
        }
      } catch (_) {
        setState(() {
          _loading = false;
          _errorMessage = AppStrings.biometrijaNiUspela;
        });
        return;
      }
    }

    try {
      await _api.complete(
        scannedUri: widget.attempt.scannedUri,
        emso: widget.virtualId.emso,
      );
    } on AuthApiException catch (e) {
      if (mounted) {
        final msg = e.message.contains('odzval')
            ? AppStrings.streznikNiOdzval
            : AppStrings.prijaveNiMogoceDokoncati;
        setState(() {
          _loading = false;
          _errorMessage = msg;
        });
      }
      return;
    } catch (_) {
      if (mounted) {
        setState(() {
          _loading = false;
          _errorMessage = AppStrings.prijaveNiMogoceDokoncati;
        });
      }
      return;
    }

    if (mounted) {
      Navigator.of(context).pop();
      Navigator.of(context).pushReplacementNamed('/success');
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                const Spacer(),
                const Text(
                  AppStrings.prijavaVEstoritev,
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
                const Spacer(),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const Divider(height: 1),
            const SizedBox(height: 8),
            _InfoRow(label: 'E-storitev', value: 'SI-PASS'),
            _InfoRow(label: 'Raven prijave', value: 'Visoka raven'),
            _InfoRow(label: 'Brskalnik', value: 'Chrome 147.0'),
            _InfoRow(label: 'Operacijski sistem', value: 'Windows 10'),
            _InfoRow(label: 'Naslov IP', value: '164.8.161.168'),
            const SizedBox(height: 16),
            if (_errorMessage != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(
                  _errorMessage!,
                  style: const TextStyle(color: AppColors.errorRed, fontSize: 14),
                ),
              ),
            PrimaryButton(
              label: AppStrings.nadaljuj,
              isLoading: _loading,
              onPressed: _onNadaljuj,
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 12),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(label, style: TextStyle(color: Colors.grey[600], fontSize: 14)),
              Text(
                value,
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
              ),
            ],
          ),
        ),
        const Divider(height: 1),
      ],
    );
  }
}
