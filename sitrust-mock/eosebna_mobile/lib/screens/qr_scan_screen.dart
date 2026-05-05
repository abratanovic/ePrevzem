import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../models/virtual_id.dart';
import '../services/qr_validator.dart';
import '../utils/constants.dart';
import '../widgets/gov_app_bar.dart';
import '../widgets/login_confirmation_sheet.dart';
import '../widgets/primary_button.dart';
import '../widgets/qr_overlay.dart';

class QrScanScreen extends StatefulWidget {
  const QrScanScreen({super.key});

  @override
  State<QrScanScreen> createState() => _QrScanScreenState();
}

class _QrScanScreenState extends State<QrScanScreen> {
  final _controller = MobileScannerController(
    detectionSpeed: DetectionSpeed.normal,
    formats: [BarcodeFormat.qrCode],
  );
  final _validator = QrValidator();

  bool _sheetOpen = false;
  DateTime? _lastScanTime;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    final raw = capture.barcodes.firstOrNull?.rawValue;
    if (raw == null) return;

    // Throttle duplicate scans within 2 seconds
    final now = DateTime.now();
    if (_lastScanTime != null && now.difference(_lastScanTime!).inSeconds < 2) return;
    if (_sheetOpen) return;
    _lastScanTime = now;

    final result = _validator.validate(raw);

    if (!result.isValid) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result.errorMessage ?? AppStrings.neveljavnaKoda),
          backgroundColor: Colors.red[700],
          duration: const Duration(seconds: 3),
        ),
      );
      return;
    }

    HapticFeedback.lightImpact();
    _controller.stop();
    setState(() => _sheetOpen = true);

    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (_) => LoginConfirmationSheet(
        attempt: result.attempt!,
        virtualId: mockVirtualId,
      ),
    ).whenComplete(() {
      if (mounted) {
        setState(() => _sheetOpen = false);
        _controller.start();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
          ),
          Column(
            children: [
              GovAppBar(
                variant: GovAppBarVariant.scan,
                onBack: () => Navigator.of(context).pop(),
              ),
              Expanded(
                child: Center(
                  child: QrOverlay(helperText: AppStrings.nastaviteKamero),
                ),
              ),
              Padding(
                padding: const EdgeInsets.fromLTRB(24, 0, 24, 40),
                child: PrimaryButton(
                  label: AppStrings.preklic,
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
