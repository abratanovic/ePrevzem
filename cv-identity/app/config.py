"""Runtime configuration parsing for the identity verification service."""
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class FaceMatchConfig:
    model: str
    score_type: str
    threshold: float
    decision: str


def read_liveness_threshold(path: str | Path) -> float:
    """Read the liveness spoof-probability threshold from a text file."""
    text = Path(path).read_text(encoding="utf-8").strip()
    if not text:
        raise ValueError(f"Empty liveness threshold file: {path}")
    return float(text)


def read_face_match_config(path: str | Path) -> FaceMatchConfig:
    """Read face matching config from key=value lines.

    The expected current format is:

        model=ArcFace
        score_type=cosine_similarity
        threshold=0.2
        decision=same_if_score_greater_or_equal
    """
    values: dict[str, str] = {}
    for raw_line in Path(path).read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line:
            raise ValueError(f"Invalid face-match config line: {raw_line}")
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()

    return FaceMatchConfig(
        model=values.get("model", "ArcFace"),
        score_type=values.get("score_type", values.get("metric", "cosine_similarity")),
        threshold=float(values.get("threshold", "0.2")),
        decision=values.get("decision", "same_if_score_greater_or_equal"),
    )
