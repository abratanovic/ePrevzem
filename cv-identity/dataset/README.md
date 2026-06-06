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

Large public datasets are kept **outside the repo** (never committed) under:

```
C:\PROJEKTI\datasets\
├── nuaa\raw\
│   ├── ClientRaw\<id>\*.jpg     # real faces      -> live  (5105 imgs, 15 subjects)
│   └── ImposterRaw\<id>\*.jpg   # photo attacks   -> spoof (7509 imgs, 15 subjects)
└── lfw\
    ├── lfw-deepfunneled\lfw-deepfunneled\<Name>\<Name>_NNNN.jpg
    ├── matchpairsDevTrain.csv / matchpairsDevTest.csv      # positive pairs (same person)
    ├── mismatchpairsDevTrain.csv / mismatchpairsDevTest.csv# negative pairs (different people)
    └── pairs.csv                                           # all 6000 pairs combined
```

- **NUAA** — liveness (anti-spoofing). Chosen over CelebA-Spoof (~80 GB) for a
  manageable size. `ClientRaw` = live, `ImposterRaw` = spoof. A subject's id is
  identical in both folders, so each subject maps to **one** person id
  (`nuaa_<id>`) and never leaks across splits.
- **LFW (deepfunneled)** — face-match **threshold calibration** only (not part
  of the liveness build). Use the match/mismatch CSVs directly:
  - match rows: `name, imagenum1, imagenum2` (same person)
  - mismatch rows: `name1, imagenum1, name2, imagenum2` (different people)
  - image path: `lfw-deepfunneled\lfw-deepfunneled\<name>\<name>_{imagenum:04d}.jpg`

NUAA images are already face crops, so the build skips MediaPipe detection for
them; team images go through detection + alignment first.

## Building the dataset

`scripts/build_dataset.py` turns raw data into model-ready splits using the
project's own preprocessing / augmentation / split functions:

```bash
python scripts/build_dataset.py --raw dataset/raw \
    --nuaa C:/PROJEKTI/datasets/nuaa/raw --out dataset --augment-count 4
```

Output: `dataset/splits/{train,val,test}/{live,spoof}/<person>__<name>.jpg`.
Train images get `--augment-count` extra augmented variants each; val/test are
not augmented. LFW is consumed directly by the model step via its pair CSVs.

## Split policy

Splits are produced by `training/split.py` and partition **by person**: the
same identity never appears in more than one split (prevents leaked, inflated
metrics). Default ratio train/val/test = 0.6 / 0.2 / 0.2.

> Note (small team): with only a few team identities, a clean 3-way split of
> *team* data is not meaningful. The liveness CNN trains mainly on NUAA; team
> data is best kept as a held-out real-world test set. See the spec §6.
