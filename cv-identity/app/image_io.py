"""Image loading and conversion helpers for API uploads."""
import io

import cv2
import numpy as np
from PIL import Image


def load_upload_image(file_bytes: bytes) -> np.ndarray:
    """Load image bytes as an OpenCV BGR uint8 image, respecting EXIF orientation."""
    from PIL import ImageOps
    image = Image.open(io.BytesIO(file_bytes))
    image = ImageOps.exif_transpose(image)
    image = image.convert("RGB")
    rgb = np.array(image)
    return cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)


def bgr_to_rgb(image: np.ndarray) -> np.ndarray:
    """Convert an OpenCV BGR image to RGB."""
    return cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
