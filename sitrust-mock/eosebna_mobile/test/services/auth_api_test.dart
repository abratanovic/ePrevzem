import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:eosebna_mobile/services/auth_api.dart';

void main() {
  group('AuthApi.complete', () {
    test('appends emso to query params', () async {
      Uri? capturedUri;
      final mockClient = MockClient((request) async {
        capturedUri = request.url;
        return http.Response('', 200);
      });

      final api = AuthApi(mockClient);
      final uri = Uri.parse(
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000');
      await api.complete(scannedUri: uri, emso: '1234567890123');

      expect(capturedUri?.queryParameters['emso'], '1234567890123');
      expect(capturedUri?.queryParameters['attemptId'],
          '123e4567-e89b-12d3-a456-426614174000');
    });

    test('preserves existing query parameters', () async {
      Uri? capturedUri;
      final mockClient = MockClient((request) async {
        capturedUri = request.url;
        return http.Response('', 200);
      });

      final api = AuthApi(mockClient);
      final uri = Uri.parse(
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000&extra=value');
      await api.complete(scannedUri: uri, emso: '1234567890123');

      expect(capturedUri?.queryParameters['extra'], 'value');
    });

    test('throws AuthApiException on non-2xx response', () async {
      final mockClient = MockClient((_) async => http.Response('Unauthorized', 401));
      final api = AuthApi(mockClient);
      final uri = Uri.parse(
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000');

      expect(
        () => api.complete(scannedUri: uri, emso: '1234567890123'),
        throwsA(isA<AuthApiException>()),
      );
    });

    test('throws AuthApiException on 500', () async {
      final mockClient = MockClient((_) async => http.Response('Server Error', 500));
      final api = AuthApi(mockClient);
      final uri = Uri.parse(
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000');

      expect(
        () => api.complete(scannedUri: uri, emso: '1234567890123'),
        throwsA(isA<AuthApiException>()),
      );
    });

    test('succeeds silently on 200', () async {
      final mockClient = MockClient((_) async => http.Response('', 200));
      final api = AuthApi(mockClient);
      final uri = Uri.parse(
          'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000');

      await expectLater(
        api.complete(scannedUri: uri, emso: '1234567890123'),
        completes,
      );
    });
  });
}
