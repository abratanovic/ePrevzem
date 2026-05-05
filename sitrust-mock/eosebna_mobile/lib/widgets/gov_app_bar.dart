import 'package:flutter/material.dart';
import '../utils/constants.dart';

enum GovAppBarVariant { home, scan, plain }

class GovAppBar extends StatelessWidget {
  const GovAppBar({super.key, this.variant = GovAppBarVariant.home, this.onBack});

  final GovAppBarVariant variant;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    return ClipPath(
      clipper: _WaveClipper(),
      child: Container(
        color: AppColors.govDark,
        height: 120,
        padding: const EdgeInsets.only(top: 40, left: 16, right: 16, bottom: 20),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            if (variant == GovAppBarVariant.scan) _CircleIconButton(icon: Icons.arrow_back, onTap: onBack ?? () => Navigator.of(context).pop()),
            const _LogoAndText(),
            const Spacer(),
            if (variant == GovAppBarVariant.home) _CircleIconButton(icon: Icons.settings_outlined, onTap: () {}),
          ],
        ),
      ),
    );
  }
}

class _LogoAndText extends StatelessWidget {
  const _LogoAndText();

  @override
  Widget build(BuildContext context) {
    return Image.asset('assets/images/rs-logo.png', width: 100);
  }
}

class _CircleIconButton extends StatelessWidget {
  const _CircleIconButton({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.15), shape: BoxShape.circle),
        child: Icon(icon, color: Colors.white, size: 20),
      ),
    );
  }
}

class _WaveClipper extends CustomClipper<Path> {
  @override
  Path getClip(Size size) {
    final path = Path();
    path.lineTo(0, size.height - 20);
    path.quadraticBezierTo(size.width / 2, size.height + 10, size.width, size.height - 20);
    path.lineTo(size.width, 0);
    path.close();
    return path;
  }

  @override
  bool shouldReclip(_WaveClipper oldClipper) => false;
}
