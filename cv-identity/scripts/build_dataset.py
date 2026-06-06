"""Build processed train/val/test splits from raw team data and NUAA.

Turns raw images into a model-ready dataset using the project's own
preprocessing / augmentation / split functions:

    python scripts/build_dataset.py --raw dataset/raw \
        --nuaa C:/PROJEKTI/datasets/nuaa/raw --out dataset --augment-count 4

Output layout:
    <out>/splits/{train,val,test}/{live,spoof}/<person>__<name>.jpg
Train images are additionally augmented (`--augment-count` variants each).
Splits are partitioned BY PERSON (no identity spans two splits). NUAA images
are already face crops, so detection is skipped for them; team images go
through MediaPipe face detection + alignment first.
"""
import argparse
import sys
from collections import namedtuple
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import cv2  # noqa: E402
import numpy as np  # noqa: E402

from app.preprocessing import detect_and_crop_face, resize_image  # noqa: E402
from training.augmentation import augment_image  # noqa: E402
from training.split import split_by_identity  # noqa: E402

Record = namedtuple("Record", "person label path source")

LIVENESS_CLASSES = ("live", "spoof")
IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".bmp"}


def _images_in(folder: Path) -> list[Path]:
    return [p for p in folder.rglob("*") if p.suffix.lower() in IMAGE_EXTS]


def iter_team_records(raw_root) -> list[Record]:
    """Records from dataset/raw/<class>/<person>/*. Only live/spoof classes."""
    raw_root = Path(raw_root)
    records: list[Record] = []
    for label in LIVENESS_CLASSES:
        class_dir = raw_root / label
        if not class_dir.is_dir():
            continue
        for person_dir in sorted(p for p in class_dir.iterdir() if p.is_dir()):
            for img in _images_in(person_dir):
                records.append(Record(person_dir.name, label, str(img), "team"))
    return records


def iter_nuaa_records(nuaa_root) -> list[Record]:
    """Records from NUAA: ClientRaw=live, ImposterRaw=spoof, person=nuaa_<id>.

    A subject's real and photo images share one person id so they never split
    across train/test.
    """
    nuaa_root = Path(nuaa_root)
    mapping = {"ClientRaw": "live", "ImposterRaw": "spoof"}
    records: list[Record] = []
    for sub, label in mapping.items():
        sub_dir = nuaa_root / sub
        if not sub_dir.is_dir():
            continue
        for person_dir in sorted(p for p in sub_dir.iterdir() if p.is_dir()):
            person = f"nuaa_{person_dir.name}"
            for img in _images_in(person_dir):
                records.append(Record(person, label, str(img), "nuaa"))
    return records


def assign_splits(records, ratios=(0.6, 0.2, 0.2), seed=0) -> dict:
    """Split records into train/val/test by person (reuses split_by_identity)."""
    by_path = {r.path: r for r in records}
    pairs = [(r.person, r.path) for r in records]
    train, val, test = split_by_identity(pairs, ratios, seed)
    return {
        "train": [by_path[p] for _, p in train],
        "val": [by_path[p] for _, p in val],
        "test": [by_path[p] for _, p in test],
    }


def prepare_image(image, size=(112, 112), detect=True):
    """Crop+align a face (if `detect`) then resize. Returns None if no face."""
    if detect:
        face = detect_and_crop_face(image)
        if face is None:
            return None
    else:
        face = image
    return resize_image(face, size)


def build(raw, nuaa, out, size, augment_count, seed) -> dict:
    records = iter_team_records(raw)
    if nuaa:
        records += iter_nuaa_records(nuaa)
    if not records:
        raise SystemExit("No records found. Check --raw / --nuaa paths.")

    splits = assign_splits(records, seed=seed)
    out_root = Path(out) / "splits"
    rng = np.random.default_rng(seed)
    counts: dict = {}

    for split, recs in splits.items():
        for r in recs:
            image = cv2.imread(r.path)
            if image is None:
                continue
            prepared = prepare_image(image, size=(size, size),
                                     detect=(r.source == "team"))
            if prepared is None:
                continue  # no face detected in a team image — skip it
            dst_dir = out_root / split / r.label
            dst_dir.mkdir(parents=True, exist_ok=True)
            stem = f"{r.person}__{Path(r.path).stem}"
            cv2.imwrite(str(dst_dir / f"{stem}.jpg"), prepared)
            counts[(split, r.label)] = counts.get((split, r.label), 0) + 1
            if split == "train" and augment_count > 0:
                for i, v in enumerate(augment_image(prepared, augment_count, rng)):
                    cv2.imwrite(str(dst_dir / f"{stem}_aug{i}.jpg"), v)
    return counts


def main() -> None:
    p = argparse.ArgumentParser(description="Build processed liveness splits.")
    p.add_argument("--raw", default="dataset/raw", help="team raw root")
    p.add_argument("--nuaa", default=None, help="NUAA raw root (optional)")
    p.add_argument("--out", default="dataset", help="output root")
    p.add_argument("--size", type=int, default=112, help="output square size")
    p.add_argument("--augment-count", type=int, default=4,
                   help="augmented variants per train image (0 disables)")
    p.add_argument("--seed", type=int, default=0)
    args = p.parse_args()

    counts = build(args.raw, args.nuaa, args.out, args.size,
                   args.augment_count, args.seed)
    for (split, label), n in sorted(counts.items()):
        print(f"{split}/{label}: {n} base images")
    print(f"Output: {Path(args.out) / 'splits'}")


if __name__ == "__main__":
    main()
