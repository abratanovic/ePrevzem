import 'package:http/http.dart' as http;

class AuthApiException implements Exception {
  const AuthApiException(this.message);
  final String message;

  @override
  String toString() => 'AuthApiException: $message';
}

class AuthApi {
  AuthApi([http.Client? client]) : _client = client ?? http.Client();

  final http.Client _client;

  Future<void> complete({required Uri scannedUri, required String emso}) async {
    final finalUri = scannedUri.replace(
      queryParameters: {
        ...scannedUri.queryParameters,
        'emso': emso,
      },
    );

    final http.Response res;
    try {
      res = await _client.post(finalUri).timeout(const Duration(seconds: 10));
    } on Exception catch (e) {
      throw AuthApiException('Strežnik se ni odzval. Poskusite znova. ($e)');
    }

    if (res.statusCode < 200 || res.statusCode >= 300) {
      throw AuthApiException('HTTP ${res.statusCode}: ${res.body}');
    }
  }
}
