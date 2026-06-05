# cv-identity — data pipeline (Član 1)

Python service for CV identity verification. This part covers data capture,
preprocessing, augmentation, and dataset splitting.

## Setup

```bash
cd cv-identity
py -3.12 -m venv .venv      # Python 3.12 required for mediapipe
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
