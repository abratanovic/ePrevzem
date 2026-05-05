import 'package:flutter/material.dart';

class QrOverlay extends StatelessWidget {
  const QrOverlay({super.key, this.helperText});
  final String? helperText;

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.of(context).size;
    final boxSize = size.width * 0.65;

    return Stack(
      alignment: Alignment.center,
      children: [
        CustomPaint(
          size: Size(boxSize, boxSize),
          painter: _CornerBracketPainter(),
        ),
        if (helperText != null)
          Positioned(
            top: size.height * 0.5 + boxSize / 2 + 20,
            left: 24,
            right: 24,
            child: Text(
              helperText!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white, fontSize: 14),
            ),
          ),
      ],
    );
  }
}

class _CornerBracketPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFF5CC8C8)
      ..strokeWidth = 4
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    const length = 28.0;
    final w = size.width;
    final h = size.height;

    // top-left
    canvas.drawLine(Offset(0, length), Offset(0, 0), paint);
    canvas.drawLine(Offset(0, 0), Offset(length, 0), paint);
    // top-right
    canvas.drawLine(Offset(w - length, 0), Offset(w, 0), paint);
    canvas.drawLine(Offset(w, 0), Offset(w, length), paint);
    // bottom-left
    canvas.drawLine(Offset(0, h - length), Offset(0, h), paint);
    canvas.drawLine(Offset(0, h), Offset(length, h), paint);
    // bottom-right
    canvas.drawLine(Offset(w - length, h), Offset(w, h), paint);
    canvas.drawLine(Offset(w, h), Offset(w, h - length), paint);
  }

  @override
  bool shouldRepaint(_CornerBracketPainter oldDelegate) => false;
}
