import 'package:flutter_test/flutter_test.dart';
import 'package:eosebna_mobile/app/app.dart';
import 'package:eosebna_mobile/models/virtual_id.dart';

void main() {
  testWidgets('HomeScreen renders virtual ID name and scan button', (tester) async {
    await tester.pumpWidget(const App());
    await tester.pump();

    expect(find.text(mockVirtualId.name), findsOneWidget);
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
