import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:eosebna_mobile/app/theme.dart';
import 'package:eosebna_mobile/models/login_attempt.dart';
import 'package:eosebna_mobile/models/virtual_id.dart';
import 'package:eosebna_mobile/widgets/login_confirmation_sheet.dart';

void main() {
  final attempt = LoginAttempt(
    attemptId: '123e4567-e89b-12d3-a456-426614174000',
    scannedUri: Uri.parse(
        'http://localhost:5070/api/auth/complete?attemptId=123e4567-e89b-12d3-a456-426614174000'),
  );
  const virtualId = VirtualId(name: 'Adnan Bratanović', emso: '1234567890123');

  testWidgets('LoginConfirmationSheet renders all five info rows and Nadaljuj button',
      (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: appTheme,
        home: Scaffold(
          body: LoginConfirmationSheet(attempt: attempt, virtualId: virtualId),
        ),
      ),
    );
    await tester.pump();

    expect(find.text('Prijava v e-storitev'), findsOneWidget);
    expect(find.text('SI-PASS'), findsOneWidget);
    expect(find.text('Visoka raven'), findsOneWidget);
    expect(find.text('Chrome 147.0'), findsOneWidget);
    expect(find.text('Windows 10'), findsOneWidget);
    expect(find.text('164.8.161.168'), findsOneWidget);
    expect(find.text('Nadaljuj'), findsOneWidget);
  });
}
