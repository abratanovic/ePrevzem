import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:eosebna_mobile/screens/success_screen.dart';
import 'package:eosebna_mobile/app/theme.dart';

void main() {
  testWidgets('SuccessScreen renders heading and Nadaljuj button', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: appTheme,
        home: const SuccessScreen(),
      ),
    );
    await tester.pump();

    expect(find.textContaining('Preverjanje identitete'), findsOneWidget);
    expect(find.text('Nadaljuj'), findsOneWidget);
    expect(find.byIcon(Icons.check_circle), findsOneWidget);
  });
}
