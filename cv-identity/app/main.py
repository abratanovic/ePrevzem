"""FastAPI entrypoint for CV identity verification."""
import os
import time
from pathlib import Path

from fastapi import FastAPI, File, Form, HTTPException, UploadFile

os.environ.setdefault("TF_USE_LEGACY_KERAS", "1")

from app.config import read_face_match_config, read_liveness_threshold
from app.face.embed import FaceEmbedder
from app.image_io import load_upload_image
from app.liveness.model import LivenessModel
from app.pipeline import TesseractDocumentReader, VerificationPipeline

APP_DIR = Path(__file__).resolve().parent
MODELS_DIR = Path(os.getenv("CV_IDENTITY_MODELS_DIR", APP_DIR / "models"))
CAPTURES_DIR = Path(os.getenv("CV_IDENTITY_CAPTURES_DIR", APP_DIR.parent / "captures"))
LIVENESS_MODEL_PATH = MODELS_DIR / "liveness_model.keras"
LIVENESS_THRESHOLD_PATH = MODELS_DIR / "threshold.txt"
FACE_MATCH_CONFIG_PATH = MODELS_DIR / "face_match_config.txt"

app = FastAPI(title="cv-identity", version="0.1.0")


def create_pipeline() -> VerificationPipeline:
    """Create the production verification pipeline and fail fast on setup bugs."""
    liveness_threshold = read_liveness_threshold(LIVENESS_THRESHOLD_PATH)
    face_config = read_face_match_config(FACE_MATCH_CONFIG_PATH)
    return VerificationPipeline(
        liveness_model=LivenessModel(LIVENESS_MODEL_PATH),
        face_embedder=FaceEmbedder(face_config.model),
        document_reader=TesseractDocumentReader(),
        liveness_threshold=liveness_threshold,
        match_threshold=face_config.threshold,
    )


@app.on_event("startup")
async def startup() -> None:
    app.state.pipeline = create_pipeline()
    CAPTURES_DIR.mkdir(exist_ok=True)


@app.get("/health")
async def health() -> dict:
    return {"status": "ok"}


@app.post("/verify")
async def verify(
    id_front: UploadFile = File(...),
    selfie_frames: list[UploadFile] | None = File(default=None),
    selfie_frames_bracket: list[UploadFile] | None = File(
        default=None,
        alias="selfie_frames[]",
    ),
    variant: str = Form(default="driving_licence"),
) -> dict:
    pipeline: VerificationPipeline = app.state.pipeline
    selfie_uploads = selfie_frames or selfie_frames_bracket or []
    if not selfie_uploads:
        raise HTTPException(
            status_code=422,
            detail="At least one selfie frame is required.",
        )

    # --- Debug Logging ---
    ts = int(time.time())
    id_bytes = await id_front.read()
    with open(CAPTURES_DIR / f"{ts}_id_front.jpg", "wb") as f:
        f.write(id_bytes)
    
    selfies_data = []
    for i, selfie in enumerate(selfie_uploads):
        s_bytes = await selfie.read()
        with open(CAPTURES_DIR / f"{ts}_selfie_{i}.jpg", "wb") as f:
            f.write(s_bytes)
        selfies_data.append(s_bytes)
    
    print(f"[DEBUG] Received verify request. Variant: {variant}, ID size: {len(id_bytes)}")
    # ---------------------

    id_front_bgr = load_upload_image(id_bytes)
    selfies_bgr = [
        load_upload_image(s_bytes)
        for s_bytes in selfies_data
    ]
    return pipeline.verify(id_front_bgr, selfies_bgr, variant=variant)
