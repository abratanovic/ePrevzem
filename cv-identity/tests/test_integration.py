from pathlib import Path

import cv2
import numpy as np

from app.preprocessing import (
    resize_image, normalize_pixels, detect_and_crop_face,
)

GOLDEN = Path(__file__).parent / "golden"


def _load(name: str) -> np.ndarray:
    img = cv2.imread(str(GOLDEN / name))
    assert img is not None, f"missing golden image: {name}"
    return img


def test_full_preprocess_on_real_face():
    img = _load("face_frontal.jpg")
    face = detect_and_crop_face(img)
    assert face is not None and face.size > 0
    out = normalize_pixels(resize_image(face, (112, 112)))
    assert out.shape == (112, 112, 3)
    assert out.dtype == np.float32
    assert out.min() >= 0.0 and out.max() <= 1.0


def test_detect_returns_none_when_no_face():
    img = _load("no_face.jpg")
    assert detect_and_crop_face(img) is None
