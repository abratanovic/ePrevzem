# CV-based Identity Verification — Design Spec

**Date:** 2026-06-06
**Status:** Approved (pending implementation plan)
**Context:** University project for *Osnove računalniškega vida* (Computer Vision Basics). A computer-vision identity-verification system that stands in for SI-TRUST: the user photographs their ID card and their face (with guided head movement), and the system decides whether the live person is the person on the ID.

---

## 1. Goal

Build an applicative computer-vision system that:

1. Reads identity data (name, surname, document number, validity) from a photographed ID card.
2. Confirms the person in front of the camera is **live** (not a printed photo or a screen) using a **team-trained anti-spoof CNN**.
3. Confirms the live face **matches** the face on the ID card (1:1 face verification).
4. Returns a verification decision to a mobile client, acting as a 2FA / additional identity check.

The system must satisfy the course requirements: data capture & augmentation, a CV model/algorithm with its own trained contribution, application integration (API + mobile), run instructions, and a report. Each team member's contribution must be visible in git history.

## 2. Scope & non-goals

**In scope**
- Python CV service exposing a `/verify` HTTP API, containerized with Docker.
- A trained passive anti-spoof (liveness) CNN — the team's own model with hyperparameter optimization and evaluation.
- Face detection + 1:1 face matching using a pretrained embedding model, with a team-optimized decision threshold and full evaluation (FAR/FRR, ROC-AUC).
- OCR/MRZ reading of ID fields.
- New `feature/identity/` capture flow in `ePrevzemMobile` (Android only).
- A collected dataset with augmentation and train/val/test splits.

**Non-goals (YAGNI)**
- iOS camera capture (stub the `actual`; out of demo scope).
- Real PII or integration with real Slovenian state identity services — mock/team ID cards only.
- Production-grade security, key management, or persistence.
- Active head-pose liveness as a trained component. Head movement is used **only** as on-screen guidance to capture a good frontal frame; liveness verdict comes from the passive CNN.

## 3. User flow

1. User starts **"Potrditev identitete"** in ePrevzemMobile.
2. **Step A — ID card:** captures the front (face + printed data) and the back (MRZ zone — `IDSVN...<<<`, far more OCR-reliable than printed fields).
3. **Step B — face / liveness:** the app guides *"poglej naravnost → obrni glavo levo → obrni desno"* and captures several frames.
4. The app uploads ID image(s) + selfie frames to the Python service.
5. The service runs the pipeline and returns a JSON decision.
6. The app shows the result (verified / rejected with reasons) and proceeds with the login/pickup flow.

## 4. Architecture

```
┌────────────────────────┐         ┌──────────────────────────────┐
│   ePrevzemMobile        │  HTTP   │   cv-identity/ (Python)       │
│   (Compose, Android)    │ ──────▶ │   FastAPI + Docker           │
│                         │multipart│                              │
│  feature/identity/      │         │  POST /verify                │
│   - capture ID          │         │   1. OCR / MRZ → name, ...   │
│   - capture face        │         │   2. face detect + crop      │
│     (head movement)     │         │   3. liveness (team CNN)     │
│   - show result         │ ◀────── │   4. face match (embedding)  │
│                         │  JSON   │   5. decision                │
└────────────────────────┘         └──────────────────────────────┘
```

The Python service is a **separate, independently runnable subproject** (`cv-identity/`), not part of `ePrevzem.sln` or the Gradle build. The mobile app talks to it over HTTP using the existing Ktor 3 client and kotlinx.serialization.

## 5. Python service — `cv-identity/`

**Stack:** Python 3.11, FastAPI + Uvicorn, OpenCV, PyTorch, `facenet-pytorch` or `insightface` (embeddings), MediaPipe or MTCNN (detection), Tesseract + PassportEye (OCR/MRZ), scikit-learn (metrics), Docker.

**API**
- `POST /verify` — multipart form: `id_front` (file), `id_back` (file, optional), `selfie_frames[]` (1..N files).
- `GET /health` — liveness probe.

**Response JSON**
```json
{
  "verified": true,
  "name": "...",
  "surname": "...",
  "document_number": "...",
  "valid_until": "2030-01-01",
  "document_valid": true,
  "match_score": 0.87,
  "match_threshold": 0.62,
  "liveness_score": 0.95,
  "liveness_ok": true,
  "reasons": []
}
```
On rejection, `verified` is `false` and `reasons[]` lists the failed gates (e.g. `"liveness_failed"`, `"face_mismatch"`, `"document_expired"`, `"no_face_in_id"`).

**Module layout**
```
cv-identity/
├── app/
│   ├── main.py              # FastAPI app, routes, request validation
│   ├── pipeline.py          # orchestrates OCR → detect → liveness → match → decision
│   ├── ocr/
│   │   └── mrz.py           # MRZ parse (PassportEye/regex) + Tesseract fallback
│   ├── face/
│   │   ├── detect.py        # detection + crop + alignment
│   │   └── embed.py         # embedding + cosine similarity, threshold τ
│   ├── liveness/
│   │   ├── model.py         # CNN architecture + inference wrapper
│   │   └── weights/         # trained model weights
│   └── preprocessing.py     # resize, grayscale normalization, color-space, denoise, ROI crop
├── dataset/                 # collected + augmented data (see §6)
├── training/                # training & evaluation scripts/notebooks (see §6)
├── requirements.txt
├── Dockerfile
└── README.md                # run & usage instructions
```

**Pipeline / decision logic**
```
liveness_ok   = liveness_score >= liveness_threshold      (passive CNN over best frame[s])
face_match    = match_score   >= τ                        (cosine sim of embeddings)
document_valid= expiry_date    > today
verified      = liveness_ok AND face_match AND document_valid
```

**Preprocessing (course requirement)** applied per input: resize, grayscale normalization, color-space conversion where useful, pixel normalization, denoising, ROI cropping of the detected face.

## 6. Data & training — `cv-identity/dataset/`, `cv-identity/training/`

**Hybrid data strategy.** Team-only data (3–4 people, a few hundred images) is too small to train a liveness CNN from scratch — it would overfit to the team's faces instead of learning general live-vs-spoof cues. So the team's own captured data is **required** (it is Član 1's core deliverable and the visible contribution) but is combined with public datasets for training robustness. This is compliant with the course rules, which explicitly permit pretrained models and transfer learning as long as the team's own contribution (capture, augmentation, fine-tuning, evaluation) is clearly visible.

**Collected data (own — required)**
- Team-member faces from multiple angles (the "live / real" class), ~30–50 images per person.
- Spoof samples: printed photos and phone-screen replays of the same faces (the "spoof" class).
- Mock ID cards for the team members (no real PII).

**Public datasets (supplementary)**
- **Liveness CNN training:** a public face anti-spoofing dataset — **CelebA-Spoof** (large, openly available on GitHub, easiest to start) is the primary candidate; NUAA, CASIA-FASD, Replay-Attack, or Rose-Youtu are alternatives (most require an academic-use request). Confirm license for academic use before using.
- **Face-match threshold calibration:** **LFW (Labeled Faces in the Wild)** — thousands of labeled same/different pairs for a robust ROC without needing many team photos.

**Data-source roles**

| Purpose | Data source |
|---|---|
| Train liveness CNN | public anti-spoof dataset (CelebA-Spoof) |
| Fine-tune + test liveness | **team-captured live + spoof images** |
| Calibrate/evaluate face-match threshold τ | LFW (public pairs) |
| End-to-end demo (ID ↔ selfie) | **team mock ID cards + selfies** |

Public and own data go through the **same** preprocessing and augmentation pipeline so they are consistent.

**Augmentation (own scripts):** rotation, brightness/contrast shift, Gaussian noise, horizontal flip, scaling.

**Splits:** train / validation / test, organized in a clear folder structure. **Split by identity, not by image** — the same person's images must never appear in both train and test, or liveness/face metrics will be falsely inflated by leakage.

**Training & tuning**
- Liveness CNN: trained on the public anti-spoof dataset with **transfer learning** (e.g. a MobileNet/ResNet backbone), then **fine-tuned** on the team-captured real-vs-spoof data. Hyperparameters (learning rate, batch size, epochs, backbone choice, frozen-layer count, augmentation strength) tuned and documented.
- Face-match threshold τ: optimized on LFW pairs (positive = same person, negative = different people) by sweeping τ over the ROC; validated on the team's ID↔selfie pairs.

**Evaluation metrics:** accuracy, FAR/FRR, ROC-AUC, confusion matrix, reported on the held-out test set. Cross-domain note: the real ID-photo↔live-selfie scenario differs from public datasets, so final liveness/match evaluation uses the team's own held-out data.

## 7. Mobile integration — `ePrevzemMobile` `feature/identity/`

Follows the existing design-system and layering rules (`E*` components, token-only styling, Painter icons, state+event split, Slovenian UI text / English code, no Android-only APIs in `commonMain`).

- **Screens (Compose, Slovenian UI):** ID capture, face/liveness capture with on-screen prompts, result screen.
- **Camera:** extend `core/camera` with photo + frame capture via `expect/actual`. Android uses CameraX (already a dependency); iOS provides a stub `actual` that throws/no-ops (out of scope).
- **Data layer:** `data/identity/IdentityVerificationRepository` (+ `FakeIdentityVerificationRepository` for previews/tests), Ktor client posting multipart to `/verify`, request/response DTOs with kotlinx.serialization.
- **DI:** register the repository following the existing `di/` pattern.

## 8. Team split (maps to course roles)

- **Član 1 — data:** capture procedure & scripts, dataset organization, preprocessing, augmentation, train/val/test split; assists with the mobile capture screens.
- **Član 2 — model:** liveness CNN training + hyperparameter optimization + evaluation; face-match threshold tuning + ROC; prepare models for inference.
- **Član 3 — integration:** FastAPI API, Docker, model serving; mobile data layer + Ktor integration; run instructions.

Each member commits their own work so contributions are visible in git history.

## 9. Run & usage (to be detailed in READMEs)

- `cv-identity/`: `docker build` + `docker run` (or `uvicorn app.main:app`), exposing `/verify` and `/health`.
- `ePrevzemMobile`: build the Android debug APK, point the identity repository at the service URL.
- Report covers: chosen model/algorithm and why, training/tuning process, optimized hyperparameters, evaluation metrics, and test results.

## 10. Open questions / risks

- **OCR reliability:** printed-field OCR on Slovenian eID is noisy; MRZ from the back is the primary path, printed-field OCR is a fallback.
- **Small dataset:** team-only data is small; augmentation and possibly transfer learning mitigate overfitting for the liveness CNN. Document this limitation in the report.
- **Embedding model licensing:** confirm the chosen pretrained embedding model's license is acceptable for academic use.
