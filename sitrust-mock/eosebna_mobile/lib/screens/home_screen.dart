import 'package:flutter/material.dart';
import '../models/virtual_id.dart';
import '../utils/constants.dart';
import '../widgets/gov_app_bar.dart';
import '../widgets/primary_button.dart';
import '../widgets/virtual_id_card.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const GovAppBar(variant: GovAppBarVariant.home),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // SI-TRUST brand row
                  Row(
                    children: [
                      Container(
                        width: 40,
                        height: 40,
                        decoration: BoxDecoration(
                          color: const Color(0xFFFF6B35).withValues(alpha: 0.15),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Icons.fingerprint,
                          color: Color(0xFFFF6B35),
                          size: 26,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'SI-TRUST',
                            style: TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 16,
                            ),
                          ),
                          Text(
                            'Državni center za storitve zaupanja',
                            style: TextStyle(fontSize: 11, color: Colors.grey[600]),
                          ),
                        ],
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  // Heading
                  const Text(
                    AppStrings.virtualnaOsebnaSlovenija,
                    style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF2C3E50),
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Description with link
                  RichText(
                    text: TextSpan(
                      style: TextStyle(fontSize: 13, color: Colors.grey[700], height: 1.5),
                      children: const [
                        TextSpan(
                          text: 'Z virtualno osebno izkaznico se lahko prijavite v e-storitve, navedene na ',
                        ),
                        TextSpan(
                          text: 'tem seznamu.',
                          style: TextStyle(
                            color: Colors.blue,
                            decoration: TextDecoration.underline,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                  // ID card
                  const VirtualIdCard(virtualId: mockVirtualId, selected: true),
                  const SizedBox(height: 8),
                  // Right-aligned hint link
                  Align(
                    alignment: Alignment.centerRight,
                    child: Text(
                      'Prijava s prislanjanjem osebne izkaznice →',
                      style: TextStyle(fontSize: 12, color: Colors.grey[500]),
                    ),
                  ),
                  const Spacer(),
                  // Primary action
                  PrimaryButton(
                    label: AppStrings.skeniraiKodoQr,
                    onPressed: () => Navigator.of(context).pushNamed('/scan'),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Najdete jo na prijavni strani SI-PASS, ko izberete možnost "Mobilna aplikacija eOsebna".',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 11, color: Colors.grey[500]),
                  ),
                  const SizedBox(height: 8),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
