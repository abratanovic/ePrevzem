# ePrevzem cv-identity

Servis računalniškega vida za **preverjanje identitete** v sistemu ePrevzem. Ob
registraciji občana sprejme sliko osebnega dokumenta in nekaj posnetkov obraza
(selfie) ter v enem klicu preveri:

1. **živost (liveness / anti-spoofing)** — je pred kamero živ človek ali le
   fotografija/zaslon (lastno naučen CNN na osnovi MobileNetV2),
2. **ujemanje obraza** — ali se obraz s selfija ujema z obrazom na dokumentu
   (ArcFace embeddingi + kosinusna podobnost),
3. **branje dokumenta (OCR)** — izlušči ime, priimek, EMSO in datum veljavnosti
   (Tesseract).

Servis je samostojen Python projekt: vsebuje skupni podatkovni cevovod (zajem,
predobdelava, augmentacija, razdelitev) in **FastAPI** inferenčni API. V
produkciji ga kliče .NET backend, ne mobilna aplikacija neposredno.

> Podroben opis delovanja je v `docs/porocilo.md`; protokol zajema in zbirke v
> `dataset/README.md`.

## Zgradba projekta

```
cv-identity/
├── app/
│   ├── main.py              # FastAPI vstopna točka (/health, /verify)
│   ├── pipeline.py          # cevovod verifikacije (živost → ujemanje → OCR)
│   ├── config.py            # branje pragov in konfiguracije modela
│   ├── preprocessing.py     # zaznava + poravnava + sprememba velikosti obraza
│   ├── image_io.py          # nalaganje naloženih slik
│   ├── liveness/model.py    # ovoj liveness CNN (Keras) → P(spoof)
│   ├── face/embed.py        # ArcFace embeddingi + kosinusna podobnost
│   ├── ocr/                 # general.py (Tesseract), mrz.py (DocumentInfo)
│   └── models/              # artefakti modela (glej spodaj)
├── training/                # augmentation.py, split.py (po identiteti)
├── scripts/                 # capture.py (zajem), build_dataset.py (gradnja)
├── tests/                   # pytest
├── dataset/                 # podatki + datasheet (git-ignored vsebina)
├── docs/                    # poročilo, teorija in viri
├── Dockerfile
└── requirements.txt
```

## Zahteve

- **Python 3.12** — obvezno (`mediapipe==0.10.18` nima koles za 3.13/3.14).
- **Tesseract OCR** nameščen v sistemu in dosegljiv na `PATH`, z jezikovnimi
  paketi `eng`, `slv`, `hrv` (za OCR dokumenta).
- **Artefakti modela** v `app/models/` (glej naslednji razdelek). Keras/TFLite
  binarne datoteke so git-ignored — pred zagonom jih skopirajte iz pripravljene
  mape z modeli.

## Vzpostavitev (lokalno)

```bash
cd cv-identity
py -3.12 -m venv .venv
.venv\Scripts\Activate.ps1     # Windows PowerShell
# . .venv/Scripts/activate     # Git Bash
# source .venv/bin/activate    # Linux/macOS
pip install -r requirements.txt
```

### Artefakti modela

API ob zagonu pričakuje naslednje datoteke (pot je nastavljiva prek
`CV_IDENTITY_MODELS_DIR`, privzeto `app/models/`):

```text
app/models/liveness_model.keras    # naučen liveness CNN
app/models/threshold.txt           # prag za P(spoof), npr. 0.05
app/models/face_match_config.txt   # konfiguracija ujemanja obrazov
```

`face_match_config.txt`:

```text
model=ArcFace
score_type=cosine_similarity
threshold=0.2
decision=same_if_score_greater_or_equal
```

API je **fail-fast**: če liveness artefakti manjkajo ali so neveljavni, se servis
ob zagonu ne zažene.

> Ob prvi uporabi ujemanja obrazov DeepFace prenese uteži ArcFace modela
> (potreben je internetni dostop).

### Okoljske spremenljivke

| Spremenljivka | Privzeto | Pomen |
|---------------|----------|-------|
| `CV_IDENTITY_MODELS_DIR` | `app/models` | mapa z artefakti modela |
| `CV_IDENTITY_CAPTURES_DIR` | `app/captures` | mapa za shranjene posnetke |

## Zagon API

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Preverjanje dosegljivosti:

```bash
curl http://localhost:8000/health
# {"status": "ok"}
```

## Uporaba API

### `GET /health`

Vrne `{"status": "ok"}`, ko servis teče in so modeli naloženi.

### `POST /verify`

Verifikacija identitete; oblika `multipart/form-data`.

| Polje | Obveznost | Opis |
|-------|-----------|------|
| `id_front` | obvezno | slika sprednje strani dokumenta (JPEG/PNG) |
| `selfie_frames` | obvezno (≥1) | ena ali več slik obraza; sprejet je tudi `selfie_frames[]` |
| `variant` | neobvezno | vrsta dokumenta: `driving_licence` (privzeto) ali `id_card` |

```bash
curl -X POST http://localhost:8000/verify \
  -F "id_front=@id-front.jpg" \
  -F "selfie_frames=@selfie-1.jpg" \
  -F "selfie_frames=@selfie-2.jpg" \
  -F "variant=driving_licence"
```

**Uspešna verifikacija** (uporabnik je potrjen le, ko uspejo OCR, veljavnost
dokumenta, živost in ujemanje obraza):

```json
{
  "verified": true,
  "first_name": "JANEZ",
  "last_name": "NOVAK",
  "emso": "1010005500426"
}
```

**Neuspešna verifikacija** vsebuje seznam razlogov:

```json
{
  "verified": false,
  "reasons": ["liveness_failed"],
  "match_score": 0.41,
  "liveness_score": 0.82,
  "liveness_threshold": 0.05
}
```

Možni razlogi: `no_face_in_id`, `no_face_in_selfie`, `liveness_failed`,
`face_mismatch`, `document_ocr_failed`, `document_expired`, `missing_name`,
`missing_surname`, `missing_emso`.

> Interaktivna dokumentacija (Swagger UI) je na voljo na
> `http://localhost:8000/docs`.

## Docker

Servis se gradi in zažene prek `docker-compose.yml` v korenu repozitorija:

```bash
# iz korena repozitorija (ePrevzem/)
docker compose up -d cv-identity
curl http://localhost:8000/health
```

Slika **ne vsebuje** artefaktov modela — ti se montirajo z gostitelja kot
read-only volume (kot je nastavljeno v compose datoteki):

```yaml
volumes:
  - ./cv-identity/app/models:/app/models:ro
  - ./cv-identity/captures:/app/captures
```

Datoteke modela morajo torej obstajati na gostitelju pod
`cv-identity/app/models/` (`liveness_model.keras`, `threshold.txt`,
`face_match_config.txt`). Servis posluša na portu `8000`; backend ga znotraj
Docker omrežja doseže prek `http://cv-identity:8000`.

## Testi

```bash
pytest -v
```

## Priprava podatkov (učni cevovod)

Zajem lastnih slik s spletno kamero (SPACE shrani, `q` zaključi):

```bash
python scripts/capture.py --class live  --person adnan
python scripts/capture.py --class spoof --person adnan
```

Gradnja za model pripravljenih razdelitev iz surovih + NUAA slik:

```bash
python scripts/build_dataset.py --raw dataset/raw \
    --nuaa C:/PROJEKTI/datasets/nuaa/raw --out dataset --augment-count 4
```

Protokol zajema, razporeditev map in opis javnih zbirk so v `dataset/README.md`.
