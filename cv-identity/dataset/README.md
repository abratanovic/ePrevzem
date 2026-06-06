# Dataset datasheet

## Folder layout

```
dataset/
├── raw/                     # original captures (git-ignored)
│   ├── live/<person>/       # real live faces, multiple angles
│   ├── spoof/<person>/      # printed-photo + screen-replay attacks
│   └── id_cards/<person>/   # mock ID cards (no real PII)
└── splits/{train,val,test}/ # leakage-free splits (git-ignored)
```

`build_dataset.py` preprocesses each image **on the fly** (detect + align +
resize) while writing into `splits/` — there is no separate `processed/` stage,
so raw images are never duplicated to disk between steps.

## Classes

| Class   | Meaning                          | Used for                         |
|---------|----------------------------------|----------------------------------|
| live    | Real face in front of the camera | Liveness CNN positive            |
| spoof   | Printed photo or screen replay   | Liveness CNN negative            |
| id_cards| Mock ID card with face + name    | End-to-end demo (ID <-> selfie)  |

## Capture protocol

### Live
- ~30-50 frames per member across **moderate** angles and lighting.
- Keep the bulk near-frontal (±30°): some left/right (±15-30°) and a little
  up/down for natural phone-holding poses. **Avoid extreme profiles (>45°)** —
  MediaPipe often fails to detect them, and `build_dataset.py` then silently
  skips the image (no face → dropped). Vary lighting/distance rather than angle.

### Spoof
- A "spoof sample" is each **capture**, not each source artifact: 2-3 printed
  photos + 2-3 on-screen photos, each re-shot from several angles/distances and
  lighting, yields plenty of frames (~30-50 per member).
- Vary the **capture conditions** so the model learns real attack cues
  (reflection, moiré, paper edges): tilt the print, change screen brightness,
  move closer/farther.
- Prefer **different media** (printed + phone screen + monitor, matte + glossy
  paper) so the model doesn't latch onto one device as "spoof".
- Re-shoot the **same angles** you used for that person's live frames, otherwise
  the model learns "this pose = spoof" instead of genuine spoof cues.

### Device & conditions (both classes)
- Capture **live and spoof on the same device**, in **similar lighting and
  background** — otherwise the model learns a shortcut (e.g. brightness or
  sensor noise) instead of liveness.
- Ideally capture with the **same kind of device used for the demo** (phone),
  to reduce the train/serve domain gap. `scripts/capture.py` (laptop webcam) is
  fine for a quick pipeline test, but real training data is best shot on a
  phone and copied into `raw/<class>/<person>/`.

### id_cards
- One or more mock cards per member, **no real personal data**. Needed only for
  the end-to-end ID <-> selfie demo (face matching), not for liveness training.

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
