# Dataset datasheet

## Folder layout

```
dataset/
├── raw/                     # original captures (git-ignored)
│   ├── live/<person>/       # real live faces, multiple angles
│   ├── spoof/<person>/      # printed-photo + screen-replay attacks
│   └── id_cards/<person>/   # mock ID cards (no real PII)
├── processed/               # after preprocessing (git-ignored)
└── splits/{train,val,test}/ # leakage-free splits (git-ignored)
```

## Classes

| Class   | Meaning                          | Used for                         |
|---------|----------------------------------|----------------------------------|
| live    | Real face in front of the camera | Liveness CNN positive            |
| spoof   | Printed photo or screen replay   | Liveness CNN negative            |
| id_cards| Mock ID card with face + name    | End-to-end demo (ID <-> selfie)  |

## Capture protocol

- Each team member: ~30-50 **live** frames across angles (front, left, right,
  up, down) and lighting conditions.
- For each member, create matching **spoof** frames: print their photo and/or
  show it on a phone screen, then recapture with another device.
- Mock **id_cards**: one or more per member, no real personal data.

## Public datasets (supplementary)

- Liveness training: **CelebA-Spoof** (verify academic-use license).
- Face-match threshold calibration: **LFW**.
Public data is run through the same preprocessing + augmentation pipeline.

## Split policy

Splits are produced by `training/split.py` and partition **by person**: the
same identity never appears in more than one split (prevents leaked, inflated
metrics). Default ratio train/val/test = 0.6 / 0.2 / 0.2.
