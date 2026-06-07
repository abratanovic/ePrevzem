# cv-identity

Python service for CV identity verification. It contains the shared data
pipeline plus a FastAPI inference API for ID document OCR, liveness, and face
matching.

## Setup

```bash
cd cv-identity
py -3.12 -m venv .venv      # Python 3.12 required for mediapipe
. .venv/Scripts/activate    # Windows PowerShell: .venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

MRZ OCR also needs the system Tesseract binary installed and available on PATH.

Model artifacts are expected at:

```text
app/models/liveness_model.keras
app/models/threshold.txt
app/models/face_match_config.txt
```

The Keras/TFLite binaries are git-ignored. Copy them from the prepared model
folder before running the API.

## Run API

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

The API fails fast on startup if required liveness artifacts are missing.

## Docker

Build and run the container from the repository root:

```bash
docker compose up -d cv-identity
curl http://localhost:8000/health
```

The image does not contain model artifacts. Put the required files on the host
where Compose can mount them:

```text
cv-identity/app/models/liveness_model.keras
cv-identity/app/models/threshold.txt
cv-identity/app/models/face_match_config.txt
```

In production, the same relative paths must exist under `/opt/eprevzem`:

```text
/opt/eprevzem/cv-identity/app/models/liveness_model.keras
/opt/eprevzem/cv-identity/app/models/threshold.txt
/opt/eprevzem/cv-identity/app/models/face_match_config.txt
/opt/eprevzem/cv-identity/captures/
```

The service is exposed on port `8000`. For the current VPS, the mobile app
should use:

```properties
cv.identity.base.url=http://116.202.15.208:8000
```

## Verify Identity

```bash
curl -X POST http://localhost:8000/verify \
  -F "id_front=@id-front.jpg" \
  -F "id_back=@id-back.jpg" \
  -F "selfie_frames=@selfie-1.jpg" \
  -F "selfie_frames=@selfie-2.jpg"
```

`/verify` returns a decision with OCR fields, liveness score, face-match score,
thresholds, and rejection reasons. A user is verified only when document OCR,
document validity, liveness, and face matching all pass.

## Run tests

```bash
pytest -v
```

## Capture data

```bash
python scripts/capture.py --class live  --person adnan
python scripts/capture.py --class spoof --person adnan
```

See `dataset/README.md` for the capture protocol and folder layout.
