import 'package:flutter/material.dart';
import '../screens/home_screen.dart';
import '../screens/qr_scan_screen.dart';
import '../screens/success_screen.dart';

class AppRoutes {
  static const home = '/';
  static const scan = '/scan';
  static const success = '/success';

  static Map<String, WidgetBuilder> get routes => {
        home: (_) => const HomeScreen(),
        scan: (_) => const QrScanScreen(),
        success: (_) => const SuccessScreen(),
      };
}
