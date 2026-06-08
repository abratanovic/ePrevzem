"""Keras liveness model wrapper."""
import os
from pathlib import Path

import numpy as np

from app.image_io import bgr_to_rgb
from app.preprocessing import resize_image

os.environ.setdefault("TF_USE_LEGACY_KERAS", "1")

IMG_SIZE = 112


class LivenessModel:
    """Loads the team-trained anti-spoofing model and runs inference."""

    def __init__(self, model_path: str | Path):
        model_path = Path(model_path)
        if not model_path.is_file():
            raise FileNotFoundError(f"Liveness model not found: {model_path}")
        import keras

        self._model = keras.models.load_model(model_path)

    def predict_spoof_probability(self, face_bgr: np.ndarray) -> float:
        """Return P(spoof) for a detected/cropped face image.

        The trained model already contains its preprocess_input layer, so this
        method only resizes, converts to RGB, casts to float32, and batches.
        """
        resized = resize_image(face_bgr, (IMG_SIZE, IMG_SIZE))
        rgb = bgr_to_rgb(resized).astype("float32")
        batch = np.expand_dims(rgb, axis=0)
        pred = self._model.predict(batch, verbose=0)
        return float(np.asarray(pred).reshape(-1)[0])
