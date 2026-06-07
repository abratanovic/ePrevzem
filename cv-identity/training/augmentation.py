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
    if factor >= 1.0:
        y0, x0 = (rh - h) // 2, (rw - w) // 2
        return resized[y0:y0 + h, x0:x0 + w]
    canvas = np.zeros_like(image)
    y0, x0 = (h - rh) // 2, (w - rw) // 2
    canvas[y0:y0 + rh, x0:x0 + rw] = resized
    return canvas


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


def gaussian_blur(image: np.ndarray, ksize: int = 5) -> np.ndarray:
    """Blur to mimic a low-resolution / printed ID photo. `ksize` must be odd."""
    if ksize % 2 == 0:
        ksize += 1
    return cv2.GaussianBlur(image, (ksize, ksize), sigmaX=0)


def add_jpeg_artifacts(image: np.ndarray, quality: int = 30) -> np.ndarray:
    """Re-encode as low-quality JPEG to mimic compression artifacts."""
    ok, buf = cv2.imencode(".jpg", image, [cv2.IMWRITE_JPEG_QUALITY, quality])
    if not ok:
        return image
    return cv2.imdecode(buf, cv2.IMREAD_COLOR)


def augment_image(image: np.ndarray, count: int,
                  rng: np.random.Generator) -> list[np.ndarray]:
    """Produce `count` randomly augmented variants of one image.

    Each variant randomly composes rotation, brightness, noise, flip, scale,
    and (sometimes) ID-like degradation (blur + JPEG artifacts). `rng` makes
    the whole expansion reproducible.
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
        if rng.random() < 0.3:
            out = gaussian_blur(out, ksize=int(rng.choice([3, 5])))
            out = add_jpeg_artifacts(out, quality=int(rng.integers(20, 50)))
        variants.append(out)
    return variants
