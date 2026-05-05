import '../models/login_attempt.dart';
import '../utils/constants.dart';

class QrValidationResult {
  const QrValidationResult.valid({required this.attempt})
      : isValid = true,
        errorMessage = null;

  const QrValidationResult.invalid({required String message})
      : isValid = false,
        attempt = null,
        errorMessage = message;

  final bool isValid;
  final LoginAttempt? attempt;
  final String? errorMessage;
}

class QrValidator {
  static final _guidRegex = RegExp(r'^[0-9a-fA-F\-]{36}$');

  QrValidationResult validate(String? raw) {
    if (raw == null || raw.isEmpty) {
      return QrValidationResult.invalid(message: AppStrings.neveljavnaKoda);
    }

    final uri = Uri.tryParse(raw);
    if (uri == null) {
      return QrValidationResult.invalid(message: AppStrings.neveljavnaKoda);
    }

    if (uri.scheme != 'http' && uri.scheme != 'https') {
      return QrValidationResult.invalid(message: AppStrings.neveljavnaKoda);
    }

    final normalizedPath = uri.path.toLowerCase().trimRight().replaceAll(RegExp(r'/$'), '');
    if (!normalizedPath.endsWith('/api/auth/complete')) {
      return QrValidationResult.invalid(message: AppStrings.neveljavnaKoda);
    }

    final attemptId = uri.queryParameters['attemptId'];
    if (attemptId == null || attemptId.isEmpty || !_guidRegex.hasMatch(attemptId)) {
      return QrValidationResult.invalid(message: AppStrings.neveljavnaKoda);
    }

    return QrValidationResult.valid(
      attempt: LoginAttempt(attemptId: attemptId, scannedUri: uri),
    );
  }
}
