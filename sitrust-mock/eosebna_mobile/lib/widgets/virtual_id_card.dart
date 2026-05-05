import 'package:flutter/material.dart';
import '../models/virtual_id.dart';
import '../utils/constants.dart';

class VirtualIdCard extends StatelessWidget {
  const VirtualIdCard({super.key, required this.virtualId, this.selected = true});

  final VirtualId virtualId;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.cardBackground,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: AppColors.tealPrimary.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(Icons.credit_card, color: AppColors.tealPrimary, size: 28),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Text(
              virtualId.name,
              style: TextStyle(
                color: AppColors.tealPrimary.withValues(alpha: 0.85),
                fontSize: 17,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          if (selected)
            Icon(Icons.check, color: AppColors.tealPrimary, size: 24),
        ],
      ),
    );
  }
}
