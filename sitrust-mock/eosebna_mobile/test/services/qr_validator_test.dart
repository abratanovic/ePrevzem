import 'package:flutter_test/flutter_test.dart';
import 'package:eosebna_mobile/services/qr_validator.dart';

void main() {
  final validator = QrValidator();

  group('QrValidator — valid inputs', () {
    test('accepts http URL with GUID attemptId', () {
      const url =
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000';
      final result = validator.validate(url);
      expect(result.isValid, isTrue);
      expect(result.attempt?.attemptId, '123e4567-e89b-12d3-a456-426614174000');
    });

    test('accepts https URL', () {
      const url =
          'https://example.com/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000';
      final result = validator.validate(url);
      expect(result.isValid, isTrue);
    });

    test('accepts URL with trailing slash on path', () {
      const url =
          'http://localhost:5070/api/auth/complete/?attemptId=123e4567-e89b-12d3-a456-426614174000';
      final result = validator.validate(url);
      expect(result.isValid, isTrue);
    });

    test('preserves original URI in attempt', () {
      const url =
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000';
      final result = validator.validate(url);
      expect(result.attempt?.scannedUri.host, 'localhost');
      expect(result.attempt?.scannedUri.port, 5070);
    });
  });

  group('QrValidator — invalid inputs', () {
    test('rejects null', () {
      expect(validator.validate(null).isValid, isFalse);
    });

    test('rejects empty string', () {
      expect(validator.validate('').isValid, isFalse);
    });

    test('rejects plain text', () {
      expect(validator.validate('hello world').isValid, isFalse);
    });

    test('rejects non-http scheme', () {
      const url =
          'ftp://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000';
      expect(validator.validate(url).isValid, isFalse);
    });

    test('rejects wrong path', () {
      const url =
          'http://localhost:5070/api/auth/login?attemptId=123e4567-e89b-12d3-a456-426614174000';
      expect(validator.validate(url).isValid, isFalse);
    });

    test('rejects missing attemptId', () {
      const url = 'http://localhost:5070/api/auth/complete?foo=bar';
      expect(validator.validate(url).isValid, isFalse);
    });

    test('rejects non-GUID attemptId', () {
      const url = 'http://localhost:5070/api/auth/complete?attemptId=not-a-guid';
      expect(validator.validate(url).isValid, isFalse);
    });

    test('returns Slovenian error message', () {
      final result = validator.validate('invalid');
      expect(result.errorMessage, contains('Neveljavna'));
    });
  });
}
