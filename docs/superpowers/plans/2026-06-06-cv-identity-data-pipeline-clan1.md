# CV Identity — Data Pipeline (Član 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the data layer of the `cv-identity/` Python service — capture, preprocessing, augmentation, and leakage-free train/val/test splitting — so Član 2 has a clean, consistent dataset to train the liveness CNN and calibrate the face-match threshold.

**Architecture:** A standalone Python subproject `cv-identity/`. Član 1 owns three pure, unit-tested modules (`preprocessing`, `augmentation`, `split`) plus a webcam capture script and a dataset datasheet. Pure image functions operate on `numpy` arrays so they are deterministic and testable with synthetic data; face detection is isolated behind a thin wrapper so the crop logic stays unit-testable. The same preprocessing + augmentation pipeline is applied to both team-captured and public datasets for consistency.

**Tech Stack:** Python 3.11, `numpy`, `opencv-python` (cv2), `pytest`. No `albumentations` — augmentation is implemented by hand (course requirement: own augmentation procedures).

**Spec:** `docs/superpowers/specs/2026-06-06-cv-identity-verification-design.md` (§6 Data & training).

---

## File Structure

```
cv-identity/
├── app/
│   ├── __init__.py
│   └── preprocessing.py        # resize, grayscale, normalize, denoise, colorspace, crop_to_bbox, detect_and_crop_face
├── training/
│   ├── __init__.py
│   ├── augmentation.py         # rotate, adjust_brightness, add_gaussian_noise, flip_horizontal, scale, augment_image
│   └── split.py                # split_by_identity
├── scripts/
│   └── capture.py              # webcam capture into dataset/raw/<class>/<person>/
├── dataset/
│   ├── README.md               # datasheet: classes, counts, capture protocol, split policy
│   └── .gitkeep
├── tests/
│   ├── __init__.py
│   ├── conftest.py             # synthetic-image fixtures
│   ├── test_preprocessing.py
│   ├── test_augmentation.py
│   └── test_split.py
├── requirements.txt
├── .gitignore
└── README.md                   # how to set up + run the data pipeline
```

**Why this shape:** `app/preprocessing.py` is shared by training *and* inference (the live `/verify` request preprocesses images the same way), so it lives under `app/`. `augmentation.py` and `split.py` are training-only, so they live under `training/`. Each module has one responsibility and is small enough to reason about at once.

---

## Task 0: Project skeleton & dependencies

**Files:**
- Create: `cv-identity/requirements.txt`
- Create: `cv-identity/.gitignore`
- Create: `cv-identity/README.md`
- Create: `cv-identity/app/__init__.py`, `cv-identity/training/__init__.py`, `cv-identity/tests/__init__.py`
- Create: `cv-identity/dataset/.gitkeep`

- [ ] **Step 1: Create the directory skeleton and empty package files**

```bash
mkdir -p cv-identity/app cv-identity/training cv-identity/scripts cv-identity/dataset cv-identity/tests
touch cv-identity/app/__init__.py cv-identity/training/__init__.py cv-identity/tests/__init__.py cv-identity/dataset/.gitkeep
```

- [ ] **Step 2: Write `cv-identity/requirements.txt`**

```
numpy==2.1.3
opencv-python==4.10.0.84
pytest==8.3.4
```

- [ ] **Step 3: Write `cv-identity/.gitignore`**

```
.venv/
__pycache__/
*.pyc
.pytest_cache/
# Raw images are large and may contain team likenesses — keep them out of git.
dataset/raw/
dataset/processed/
dataset/splits/
!dataset/.gitkeep
```

- [ ] **Step 4: Write `cv-identity/README.md`**

````markdown
# cv-identity — data pipeline (Član 1)

Python service for CV identity verification. This part covers data capture,
preprocessing, augmentation, and dataset splitting.

## Setup

```bash
cd cv-identity
python -m venv .venv
. .venv/Scripts/activate    # Windows PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

## Run tests

```bash
pytest -v
```

## Capture data

```bash
python scripts/capture.py --class live  --person alen
python scripts/capture.py --class spoof --person alen
```

See `dataset/README.md` for the capture protocol and folder layout.
````

- [ ] **Step 5: Create and verify the virtual environment**

Run:
```bash
cd cv-identity && python -m venv .venv && .venv/Scripts/python -m pip install -r requirements.txt
```
Expected: pip installs numpy, opencv-python, pytest with no errors.

- [ ] **Step 6: Commit**

```bash
git add cv-identity/
git commit -m "chore(cv-identity): scaffold data-pipeline project skeleton"
```

---

## Task 1: Test fixtures (synthetic images)

**Files:**
- Create: `cv-identity/tests/conftest.py`

- [ ] **Step 1: Write synthetic-image fixtures**

```python
import numpy as np
import pytest


@pytest.fixture
def rgb_image():
    """A small deterministic 3-channel image (H=40, W=60, BGR uint8)."""
    rng = np.random.default_rng(seed=42)
    return rng.integers(0, 256, size=(40, 60, 3), dtype=np.uint8)


@pytest.fixture
def rng():
    """A seeded NumPy generator so augmentation tests are deterministic."""
    return np.random.default_rng(seed=123)
```

- [ ] **Step 2: Commit**

```bash
git add cv-identity/tests/conftest.py
git commit -m "test(cv-identity): add synthetic-image fixtures"
```

---

## Task 2: Preprocessing — resize

**Files:**
- Create: `cv-identity/app/preprocessing.py`
- Test: `cv-identity/tests/test_preprocessing.py`

- [ ] **Step 1: Write the failing test**

```python
import numpy as np
from app.preprocessing import resize_image


def test_resize_image_returns_target_shape(rgb_image):
    out = resize_image(rgb_image, (32, 32))
    assert out.shape == (32, 32, 3)
    assert out.dtype == np.uint8
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd cv-identity && pytest tests/test_preprocessing.py::test_resize_image_returns_target_shape -v`
Expected: FAIL with `ModuleNotFoundError` / `ImportError: cannot import name 'resize_image'`.

- [ ] **Step 3: Write minimal implementation**

```python
"""Image preprocessing shared by training and inference."""
import cv2
import numpy as np


def resize_image(image: np.ndarray, size: tuple[int, int]) -> np.ndarray:
    """Resize to (width, height). `size` is (w, h)."""
    return cv2.resize(image, size, interpolation=cv2.INTER_AREA)
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd cv-identity && pytest tests/test_preprocessing.py::test_resize_image_returns_target_shape -v`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add cv-identity/app/preprocessing.py cv-identity/tests/test_preprocessing.py
git commit -m "feat(cv-identity): add image resize"
```

---

## Task 3: Preprocessing — grayscale & pixel normalization

**Files:**
- Modify: `cv-identity/app/preprocessing.py`
- Test: `cv-identity/tests/test_preprocessing.py`

- [ ] **Step 1: Write the failing tests**

```python
from app.preprocessing import to_grayscale, normalize_pixels


def test_to_grayscale_drops_channels(rgb_image):
    out = to_grayscale(rgb_image)
    assert out.shape == (40, 60)


def test_normalize_pixels_scales_to_unit_range(rgb_image):
    out = normalize_pixels(rgb_image)
    assert out.dtype == np.float32
    assert out.min() >= 0.0 and out.max() <= 1.0
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "grayscale or normalize" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Add implementations to `app/preprocessing.py`**

```python
def to_grayscale(image: np.ndarray) -> np.ndarray:
    """Convert a BGR image to single-channel grayscale."""
    return cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)


def normalize_pixels(image: np.ndarray) -> np.ndarray:
    """Scale pixel values from [0, 255] uint8 to [0, 1] float32."""
    return image.astype(np.float32) / 255.0
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "grayscale or normalize" -v`
Expected: PASS (2 passed).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/app/preprocessing.py cv-identity/tests/test_preprocessing.py
git commit -m "feat(cv-identity): add grayscale and pixel normalization"
```

---

## Task 4: Preprocessing — denoise & color-space conversion

**Files:**
- Modify: `cv-identity/app/preprocessing.py`
- Test: `cv-identity/tests/test_preprocessing.py`

- [ ] **Step 1: Write the failing tests**

```python
from app.preprocessing import denoise, to_colorspace


def test_denoise_preserves_shape(rgb_image):
    out = denoise(rgb_image)
    assert out.shape == rgb_image.shape
    assert out.dtype == np.uint8


def test_to_colorspace_rgb_keeps_three_channels(rgb_image):
    out = to_colorspace(rgb_image, "RGB")
    assert out.shape == rgb_image.shape
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "denoise or colorspace" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Add implementations to `app/preprocessing.py`**

```python
_COLORSPACES = {
    "RGB": cv2.COLOR_BGR2RGB,
    "HSV": cv2.COLOR_BGR2HSV,
    "YCRCB": cv2.COLOR_BGR2YCrCb,
}


def denoise(image: np.ndarray) -> np.ndarray:
    """Reduce noise with a Gaussian blur (3x3 kernel)."""
    return cv2.GaussianBlur(image, (3, 3), sigmaX=0)


def to_colorspace(image: np.ndarray, target: str) -> np.ndarray:
    """Convert a BGR image to the named color space (RGB, HSV, YCRCB)."""
    code = _COLORSPACES[target.upper()]
    return cv2.cvtColor(image, code)
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "denoise or colorspace" -v`
Expected: PASS (2 passed).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/app/preprocessing.py cv-identity/tests/test_preprocessing.py
git commit -m "feat(cv-identity): add denoise and color-space conversion"
```

---

## Task 5: Preprocessing — crop to bbox & face crop wrapper

**Files:**
- Modify: `cv-identity/app/preprocessing.py`
- Test: `cv-identity/tests/test_preprocessing.py`

**Note:** `crop_to_bbox` is pure and unit-tested. `detect_and_crop_face` wraps OpenCV's bundled Haar cascade face detector and returns `None` when no face is found — detection itself is not unit-tested (it needs real faces), only the crop math is.

- [ ] **Step 1: Write the failing test**

```python
from app.preprocessing import crop_to_bbox


def test_crop_to_bbox_returns_region(rgb_image):
    # bbox = (x, y, w, h)
    out = crop_to_bbox(rgb_image, (10, 5, 20, 15))
    assert out.shape == (15, 20, 3)


def test_crop_to_bbox_clamps_to_image_bounds(rgb_image):
    # Width 100 exceeds the image width (60); result must clamp, not overflow.
    out = crop_to_bbox(rgb_image, (50, 0, 100, 10))
    assert out.shape[1] == 10  # 60 - 50
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "crop_to_bbox" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Add implementations to `app/preprocessing.py`**

```python
def crop_to_bbox(image: np.ndarray, bbox: tuple[int, int, int, int]) -> np.ndarray:
    """Crop region (x, y, w, h), clamped to the image bounds."""
    x, y, w, h = bbox
    h_img, w_img = image.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(w_img, x + w), min(h_img, y + h)
    return image[y0:y1, x0:x1]


def detect_and_crop_face(image: np.ndarray) -> np.ndarray | None:
    """Detect the largest face with a Haar cascade and crop to it.

    Returns None if no face is detected. Detection is isolated here so the
    rest of the pipeline can be unit-tested without real face images.
    """
    cascade_path = cv2.data.haarcascades + "haarcascade_frontalface_default.xml"
    cascade = cv2.CascadeClassifier(cascade_path)
    gray = to_grayscale(image)
    faces = cascade.detectMultiScale(gray, scaleFactor=1.1, minNeighbors=5)
    if len(faces) == 0:
        return None
    # Largest detected face by area.
    x, y, w, h = max(faces, key=lambda f: f[2] * f[3])
    return crop_to_bbox(image, (int(x), int(y), int(w), int(h)))
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -k "crop_to_bbox" -v`
Expected: PASS (2 passed).

- [ ] **Step 5: Run the whole preprocessing suite**

Run: `cd cv-identity && pytest tests/test_preprocessing.py -v`
Expected: PASS (all preprocessing tests).

- [ ] **Step 6: Commit**

```bash
git add cv-identity/app/preprocessing.py cv-identity/tests/test_preprocessing.py
git commit -m "feat(cv-identity): add bbox crop and Haar face-crop wrapper"
```

---

## Task 6: Augmentation — geometric transforms (rotate, flip, scale)

**Files:**
- Create: `cv-identity/training/augmentation.py`
- Test: `cv-identity/tests/test_augmentation.py`

- [ ] **Step 1: Write the failing tests**

```python
import numpy as np
from training.augmentation import rotate, flip_horizontal, scale


def test_rotate_preserves_shape(rgb_image):
    out = rotate(rgb_image, angle=15)
    assert out.shape == rgb_image.shape


def test_flip_horizontal_reverses_columns(rgb_image):
    out = flip_horizontal(rgb_image)
    assert np.array_equal(out, rgb_image[:, ::-1])


def test_scale_then_fit_preserves_shape(rgb_image):
    out = scale(rgb_image, factor=1.2)
    assert out.shape == rgb_image.shape
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_augmentation.py -k "rotate or flip or scale" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Write `training/augmentation.py`**

```python
"""Hand-written data augmentation for the liveness dataset."""
import cv2
import numpy as np


def rotate(image: np.ndarray, angle: float) -> np.ndarray:
    """Rotate around the center, keeping the original frame size."""
    h, w = image.shape[:2]
    matrix = cv2.getRotationMatrix2D((w / 2, h / 2), angle, scale=1.0)
    return cv2.warpAffine(image, matrix, (w, h), borderMode=cv2.BORDER_REFLECT)


def flip_horizontal(image: np.ndarray) -> np.ndarray:
    """Mirror left-right."""
    return cv2.flip(image, 1)


def scale(image: np.ndarray, factor: float) -> np.ndarray:
    """Zoom by `factor`, then center-crop/pad back to the original size."""
    h, w = image.shape[:2]
    resized = cv2.resize(image, None, fx=factor, fy=factor,
                         interpolation=cv2.INTER_LINEAR)
    rh, rw = resized.shape[:2]
    if factor >= 1.0:  # crop center
        y0, x0 = (rh - h) // 2, (rw - w) // 2
        return resized[y0:y0 + h, x0:x0 + w]
    # pad to size (factor < 1.0)
    canvas = np.zeros_like(image)
    y0, x0 = (h - rh) // 2, (w - rw) // 2
    canvas[y0:y0 + rh, x0:x0 + rw] = resized
    return canvas
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_augmentation.py -k "rotate or flip or scale" -v`
Expected: PASS (3 passed).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/training/augmentation.py cv-identity/tests/test_augmentation.py
git commit -m "feat(cv-identity): add geometric augmentations"
```

---

## Task 7: Augmentation — photometric transforms (brightness, noise)

**Files:**
- Modify: `cv-identity/training/augmentation.py`
- Test: `cv-identity/tests/test_augmentation.py`

- [ ] **Step 1: Write the failing tests**

```python
from training.augmentation import adjust_brightness, add_gaussian_noise


def test_adjust_brightness_increases_mean(rgb_image):
    brighter = adjust_brightness(rgb_image, delta=40)
    assert brighter.mean() > rgb_image.mean()
    assert brighter.max() <= 255  # no overflow wraparound


def test_add_gaussian_noise_is_deterministic_with_seed(rgb_image, rng):
    a = add_gaussian_noise(rgb_image, sigma=10, rng=rng)
    rng2 = np.random.default_rng(seed=123)
    b = add_gaussian_noise(rgb_image, sigma=10, rng=rng2)
    assert np.array_equal(a, b)
    assert a.shape == rgb_image.shape
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_augmentation.py -k "brightness or noise" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Add implementations to `training/augmentation.py`**

```python
def adjust_brightness(image: np.ndarray, delta: int) -> np.ndarray:
    """Add `delta` to every pixel, clipped to [0, 255]."""
    shifted = image.astype(np.int16) + delta
    return np.clip(shifted, 0, 255).astype(np.uint8)


def add_gaussian_noise(image: np.ndarray, sigma: float,
                       rng: np.random.Generator) -> np.ndarray:
    """Add zero-mean Gaussian noise. `rng` makes it reproducible."""
    noise = rng.normal(0.0, sigma, size=image.shape)
    noisy = image.astype(np.float32) + noise
    return np.clip(noisy, 0, 255).astype(np.uint8)
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_augmentation.py -k "brightness or noise" -v`
Expected: PASS (2 passed).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/training/augmentation.py cv-identity/tests/test_augmentation.py
git commit -m "feat(cv-identity): add brightness and gaussian-noise augmentations"
```

---

## Task 8: Augmentation — pipeline that expands one image into N variants

**Files:**
- Modify: `cv-identity/training/augmentation.py`
- Test: `cv-identity/tests/test_augmentation.py`

- [ ] **Step 1: Write the failing test**

```python
from training.augmentation import augment_image


def test_augment_image_returns_requested_count(rgb_image, rng):
    variants = augment_image(rgb_image, count=5, rng=rng)
    assert len(variants) == 5
    assert all(v.shape == rgb_image.shape for v in variants)


def test_augment_image_is_deterministic_with_seed(rgb_image):
    a = augment_image(rgb_image, count=3, rng=np.random.default_rng(7))
    b = augment_image(rgb_image, count=3, rng=np.random.default_rng(7))
    assert all(np.array_equal(x, y) for x, y in zip(a, b))
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_augmentation.py -k "augment_image" -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Add implementation to `training/augmentation.py`**

```python
def augment_image(image: np.ndarray, count: int,
                  rng: np.random.Generator) -> list[np.ndarray]:
    """Produce `count` randomly augmented variants of one image.

    Each variant randomly composes rotation, brightness, noise, flip, and
    scale. `rng` makes the whole expansion reproducible.
    """
    variants: list[np.ndarray] = []
    for _ in range(count):
        out = image
        out = rotate(out, angle=float(rng.uniform(-15, 15)))
        out = adjust_brightness(out, delta=int(rng.integers(-40, 41)))
        out = add_gaussian_noise(out, sigma=float(rng.uniform(0, 12)), rng=rng)
        if rng.random() < 0.5:
            out = flip_horizontal(out)
        out = scale(out, factor=float(rng.uniform(0.9, 1.1)))
        variants.append(out)
    return variants
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_augmentation.py -v`
Expected: PASS (all augmentation tests).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/training/augmentation.py cv-identity/tests/test_augmentation.py
git commit -m "feat(cv-identity): add augmentation pipeline"
```

---

## Task 9: Split — leakage-free train/val/test by identity

**Files:**
- Create: `cv-identity/training/split.py`
- Test: `cv-identity/tests/test_split.py`

**Note:** The critical rule (spec §6): images of the same person must never span two splits, or metrics are falsely inflated. The split operates on `(person, path)` records and partitions by **person**.

- [ ] **Step 1: Write the failing tests**

```python
from training.split import split_by_identity


def _records():
    # (person, path) — 5 people, 2 images each.
    people = ["alen", "edvin", "maja", "luka", "ana"]
    return [(p, f"{p}_{i}.jpg") for p in people for i in range(2)]


def test_split_partitions_all_records():
    train, val, test = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=1)
    total = len(train) + len(val) + len(test)
    assert total == len(_records())


def test_split_has_no_identity_overlap():
    train, val, test = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=1)
    people = lambda rs: {p for p, _ in rs}
    assert people(train).isdisjoint(people(val))
    assert people(train).isdisjoint(people(test))
    assert people(val).isdisjoint(people(test))


def test_split_is_deterministic_with_seed():
    a = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=42)
    b = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=42)
    assert a == b
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd cv-identity && pytest tests/test_split.py -v`
Expected: FAIL with `ImportError`.

- [ ] **Step 3: Write `training/split.py`**

```python
"""Leakage-free dataset splitting: partition by identity, not by image."""
import numpy as np

Record = tuple[str, str]  # (person, path)


def split_by_identity(
    records: list[Record],
    ratios: tuple[float, float, float] = (0.6, 0.2, 0.2),
    seed: int = 0,
) -> tuple[list[Record], list[Record], list[Record]]:
    """Split records into (train, val, test) so no person spans two splits.

    People are shuffled deterministically (by `seed`) and assigned whole to a
    split according to `ratios`, which must sum to 1.0.
    """
    if abs(sum(ratios) - 1.0) > 1e-6:
        raise ValueError(f"ratios must sum to 1.0, got {ratios}")

    people = sorted({person for person, _ in records})
    rng = np.random.default_rng(seed)
    rng.shuffle(people)

    n = len(people)
    n_train = int(n * ratios[0])
    n_val = int(n * ratios[1])
    train_people = set(people[:n_train])
    val_people = set(people[n_train:n_train + n_val])

    train, val, test = [], [], []
    for person, path in records:
        if person in train_people:
            train.append((person, path))
        elif person in val_people:
            val.append((person, path))
        else:
            test.append((person, path))
    return train, val, test
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd cv-identity && pytest tests/test_split.py -v`
Expected: PASS (3 passed).

- [ ] **Step 5: Commit**

```bash
git add cv-identity/training/split.py cv-identity/tests/test_split.py
git commit -m "feat(cv-identity): add leakage-free split by identity"
```

---

## Task 10: Webcam capture script (manual verification)

**Files:**
- Create: `cv-identity/scripts/capture.py`

**Note:** This script reads a live webcam, so it is verified manually, not with a unit test. It saves frames into the canonical dataset layout `dataset/raw/<class>/<person>/`.

- [ ] **Step 1: Write `cv-identity/scripts/capture.py`**

```python
"""Capture webcam frames into the dataset folder layout.

Usage:
    python scripts/capture.py --class live  --person alen
    python scripts/capture.py --class spoof --person alen

Keys while the window is open:
    SPACE — save the current frame
    q     — quit
"""
import argparse
from pathlib import Path

import cv2


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--class", dest="cls", required=True,
                        choices=["live", "spoof", "id_cards"])
    parser.add_argument("--person", required=True)
    parser.add_argument("--camera", type=int, default=0)
    args = parser.parse_args()

    out_dir = Path("dataset/raw") / args.cls / args.person
    out_dir.mkdir(parents=True, exist_ok=True)
    existing = len(list(out_dir.glob("*.jpg")))

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        raise SystemExit(f"Cannot open camera {args.camera}")

    saved = existing
    print("SPACE = save, q = quit")
    while True:
        ok, frame = cap.read()
        if not ok:
            break
        cv2.imshow(f"capture: {args.cls}/{args.person}", frame)
        key = cv2.waitKey(1) & 0xFF
        if key == ord("q"):
            break
        if key == ord(" "):
            path = out_dir / f"{args.person}_{saved:03d}.jpg"
            cv2.imwrite(str(path), frame)
            print(f"saved {path}")
            saved += 1

    cap.release()
    cv2.destroyAllWindows()
    print(f"Done. {saved - existing} new images in {out_dir}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Manually verify the capture script**

Run: `cd cv-identity && .venv/Scripts/python scripts/capture.py --class live --person test`
Expected: a webcam window opens; pressing SPACE saves `dataset/raw/live/test/test_000.jpg`; pressing `q` exits and prints the count. Delete the `test` folder afterward.

- [ ] **Step 3: Commit**

```bash
git add cv-identity/scripts/capture.py
git commit -m "feat(cv-identity): add webcam capture script"
```

---

## Task 11: Dataset datasheet

**Files:**
- Create: `cv-identity/dataset/README.md`

- [ ] **Step 1: Write `cv-identity/dataset/README.md`**

````markdown
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
| id_cards| Mock ID card with face + name    | End-to-end demo (ID ↔ selfie)    |

## Capture protocol

- Each team member: ~30–50 **live** frames across angles (front, left, right,
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
````

- [ ] **Step 2: Commit**

```bash
git add cv-identity/dataset/README.md
git commit -m "docs(cv-identity): add dataset datasheet"
```

---

## Task 12: Full suite green

**Files:** none (verification only)

- [ ] **Step 1: Run the entire test suite**

Run: `cd cv-identity && pytest -v`
Expected: all tests PASS (preprocessing, augmentation, split). No failures, no errors.

- [ ] **Step 2: Confirm clean tree**

Run: `git status`
Expected: clean working tree; all work committed.

---

## Notes for handoff to Član 2

- `app/preprocessing.py` is the canonical preprocessing — call it from both training and the inference pipeline so train/serve stay consistent.
- `training/augmentation.augment_image` expands the training set; do **not** augment val/test.
- `training/split.split_by_identity` must be the only way splits are produced — never split by raw image.
- Public datasets (CelebA-Spoof, LFW) feed the same `preprocessing` + `augmentation` functions before training.
