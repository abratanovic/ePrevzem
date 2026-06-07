"""Face embedding and cosine-similarity matching."""
import numpy as np

from app.image_io import bgr_to_rgb


class FaceEmbedder:
    """DeepFace/ArcFace embedding wrapper."""

    def __init__(self, model_name: str = "ArcFace"):
        self.model_name = model_name

    def embedding(self, face_bgr: np.ndarray) -> np.ndarray:
        """Return an embedding for an already detected/cropped face."""
        from deepface import DeepFace

        reps = DeepFace.represent(
            img_path=bgr_to_rgb(face_bgr),
            model_name=self.model_name,
            enforce_detection=False,
        )
        return np.array(reps[0]["embedding"], dtype="float32")


def cosine_similarity(a: np.ndarray, b: np.ndarray) -> float:
    """Compute cosine similarity where larger means more similar."""
    a = np.asarray(a, dtype="float32").reshape(-1)
    b = np.asarray(b, dtype="float32").reshape(-1)
    denom = float(np.linalg.norm(a) * np.linalg.norm(b))
    if denom == 0.0:
        return 0.0
    return float(np.dot(a, b) / denom)
