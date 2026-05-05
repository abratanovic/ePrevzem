import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:eosebna_mobile/app/app.dart';

void main() {
  testWidgets('HomeScreen renders virtual ID name and scan button', (tester) async {
    await tester.pumpWidget(const App());
    await tester.pump();

    expect(find.text('Adnan Bratanović'), findsOneWidget);
    expect(find.text('Skeniraj kodo QR'), findsOneWidget);
    expect(find.text('Virtualna osebna izkaznica'), findsOneWidget);
  });

  testWidgets('HomeScreen tapping scan button navigates to QR screen', (tester) async {
    await tester.pumpWidget(const App());
    await tester.pump();

    await tester.tap(find.text('Skeniraj kodo QR'));
    await tester.pumpAndSettle();

    expect(find.text('Prekliči'), findsOneWidget);
  });
}
