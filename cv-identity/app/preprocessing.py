"""Image preprocessing shared by training and inference."""
import math

import cv2
import numpy as np

_COLORSPACES = {
    "RGB": cv2.COLOR_BGR2RGB,
    "HSV": cv2.COLOR_BGR2HSV,
    "YCRCB": cv2.COLOR_BGR2YCrCb,
}


def resize_image(image: np.ndarray, size: tuple[int, int]) -> np.ndarray:
    """Resize to (width, height). `size` is (w, h)."""
    return cv2.resize(image, size, interpolation=cv2.INTER_AREA)


def to_grayscale(image: np.ndarray) -> np.ndarray:
    """Convert a BGR image to single-channel grayscale."""
    return cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)


def normalize_pixels(image: np.ndarray) -> np.ndarray:
    """Scale pixel values from [0, 255] uint8 to [0, 1] float32."""
    return image.astype(np.float32) / 255.0


def denoise(image: np.ndarray) -> np.ndarray:
    """Reduce noise with a Gaussian blur (3x3 kernel)."""
    return cv2.GaussianBlur(image, (3, 3), sigmaX=0)


def to_colorspace(image: np.ndarray, target: str) -> np.ndarray:
    """Convert a BGR image to the named color space (RGB, HSV, YCRCB)."""
    code = _COLORSPACES[target.upper()]
    return cv2.cvtColor(image, code)


def crop_to_bbox(image: np.ndarray, bbox: tuple[int, int, int, int]) -> np.ndarray:
    """Crop region (x, y, w, h), clamped to the image bounds."""
    x, y, w, h = bbox
    h_img, w_img = image.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(w_img, x + w), min(h_img, y + h)
    return image[y0:y1, x0:x1]


def align_by_eyes(
    image: np.ndarray,
    eye_a: tuple[float, float],
    eye_b: tuple[float, float],
) -> np.ndarray:
    """Rotate the image so the line between the two eyes is horizontal.

    The two eyes may be given in either order; they are ordered by image x
    (left eye = smaller x) internally. This matters because face detectors
    label keypoints from the subject's perspective, so the "right eye" sits on
    the image's left — using detector order directly would mis-rotate ~180°.
    """
    left_eye, right_eye = sorted([eye_a, eye_b], key=lambda p: p[0])
    dx = right_eye[0] - left_eye[0]
    dy = right_eye[1] - left_eye[1]
    angle = math.degrees(math.atan2(dy, dx))
    if abs(angle) < 1e-6:
        return image
    h, w = image.shape[:2]
    matrix = cv2.getRotationMatrix2D((w / 2, h / 2), angle, scale=1.0)
    return cv2.warpAffine(image, matrix, (w, h), borderMode=cv2.BORDER_REFLECT)


def _expand_bbox(
    bbox: tuple[int, int, int, int], margin: float = 0.3
) -> tuple[int, int, int, int]:
    """Grow a (x, y, w, h) bbox by `margin` on every side.

    Detector face boxes are tight (eyebrows to chin); a margin keeps the
    forehead and chin and tolerates the small shift introduced by alignment.
    `crop_to_bbox` clamps the result to the image bounds.
    """
    x, y, w, h = bbox
    mx, my = int(w * margin), int(h * margin)
    return (x - mx, y - my, w + 2 * mx, h + 2 * my)


def _haar_face_bbox(image: np.ndarray) -> tuple[int, int, int, int] | None:
    """Fallback face detection using OpenCV's bundled Haar cascade."""
    cascade_path = cv2.data.haarcascades + "haarcascade_frontalface_default.xml"
    cascade = cv2.CascadeClassifier(cascade_path)
    faces = cascade.detectMultiScale(to_grayscale(image), scaleFactor=1.1,
                                     minNeighbors=5)
    if len(faces) == 0:
        return None
    x, y, w, h = max(faces, key=lambda f: f[2] * f[3])
    return int(x), int(y), int(w), int(h)


def detect_and_crop_face(image: np.ndarray) -> np.ndarray | None:
    """Detect the largest face, align by eyes, and crop to it.

    Primary detector is MediaPipe (returns eye keypoints used for alignment);
    if MediaPipe finds nothing, fall back to the Haar cascade (no alignment).
    Returns None if no face is found by either.
    """
    import mediapipe as mp

    h_img, w_img = image.shape[:2]
    rgb = to_colorspace(image, "RGB")
    try:
        with mp.solutions.face_detection.FaceDetection(
            model_selection=0, min_detection_confidence=0.5
        ) as detector:
            result = detector.process(rgb)
    except RuntimeError:
        result = None

    if result and result.detections:
        det = max(
            result.detections,
            key=lambda d: d.location_data.relative_bounding_box.width
            * d.location_data.relative_bounding_box.height,
        )
        box = det.location_data.relative_bounding_box
        kp = det.location_data.relative_keypoints  # kp[0], kp[1] are the eyes
        eye_a = (kp[0].x * w_img, kp[0].y * h_img)
        eye_b = (kp[1].x * w_img, kp[1].y * h_img)
        bbox = (
            int(box.xmin * w_img), int(box.ymin * h_img),
            int(box.width * w_img), int(box.height * h_img),
        )
        # Crop a margin-padded face region first, then align around the crop's
        # own centre. Eye dx/dy (and thus the angle) are translation-invariant,
        # so the full-image eye coords are fine; align_by_eyes orders them by x.
        face = crop_to_bbox(image, _expand_bbox(bbox))
        return align_by_eyes(face, eye_a, eye_b)

    haar_bbox = _haar_face_bbox(image)
    if haar_bbox is None:
        return None
    return crop_to_bbox(image, _expand_bbox(haar_bbox))
