# Poročilo — API, OCR in integracija (Edvin)

## 1. Vloga v sistemu

Edvin je odgovoren za **API del servisa cv-identity** — to je vezivno tkivo, ki poveže podatke in CNN Model v delujoč servis, ter ga vključi v preostali sistem.

Konkretno to obsega:

| Naloga                         | Opis                                                          |
| ------------------------------ | ------------------------------------------------------------- |
| **FastAPI servis**             | REST API, ki sprejme slike in vrne odločitev o identiteti     |
| **Cevovod verifikacije**       | Orchestracija treh korakov: živost → ujemanje obraza → OCR    |
| **OCR dokumenta**              | Branje imen, priimka, EMSO in datuma veljavnosti iz dokumenta |
| **Integracija v .NET backend** | Klic cv-identity iz ASP.NET Core, registracija občana         |
| **Dockerizacija**              | Kontejnerizacija servisa in sestava celotnega sistema         |

---

## 2. FastAPI servis (`app/main.py`)

Servis je napisan v **FastAPI** in teče pod **uvicorn**. Ob zagonu naloži vse modele v pomnilnik (fail-fast — napaka pri nalaganju zaustavi servis takoj, ne šele ob prvem klicu).

**Razpoložljivi končni točki:**

| Metoda | Pot       | Opis                                           |
| ------ | --------- | ---------------------------------------------- |
| `GET`  | `/health` | Preverjanje dosegljivosti (`{"status": "ok"}`) |
| `POST` | `/verify` | Verifikacija identitete (multipart/form-data)  |

**Vhodna oblika zahtevka `/verify`:**

- `id_front` — slika sprednje strani dokumenta (JPEG/PNG)
- `selfie_frames` — ena ali več slik obraza (okvirji posnetka)
- `variant` — vrsta dokumenta: `driving_licence` (privzeto) ali `id_card`

Servis sprejme obe obliki `selfie_frames` polja (`selfie_frames` in `selfie_frames[]`), ker se obliki razlikujeta med odjemalci (Kotlin multipart vs. spletni odjemalci).

---

## 3. Cevovod verifikacije (`app/pipeline.py`)

Cevovod je implementiran kot nespremenljiv podatkovni razred (`@dataclass(frozen=True)`) z zamenljivimi komponentami prek protokolnih vmesnikov. Komponente (`LivenessPredictor`, `EmbeddingModel`, `DocumentReader`) so definirane kot Python `Protocol` — katerikoli objekt s pravilno signaturo deluje brez dedovanja.

**Zaporedje korakov:**

```
1. Zaznaj obraz na dokumentu (MediaPipe/Haar)
2. Za vsak okvir selfija: zaznaj obraz, izračunaj verjetnost spoofa (liveness CNN)
3. Izberi najboljši okvir (najnižja verjetnost spoofa)
4. Preveri živost: p_spoof < prag
5. Primerjaj embedinga obraza na dokumentu in selfija (kosinus)
6. Šele po uspešnih korakih 1–5: izvedi OCR dokumenta
7. Preveri veljavnost dokumenta in prisotnost zahtevanih polj
```

**Zakaj OCR šele na koncu:** branje dokumenta je najpočasnejši korak. Če obraz ni zaznan ali je živost dvomljiva, je OCR nepotreben — prihranimo čas in zmanjšamo površino za napake.

**Odgovor pri uspešni verifikaciji:**

```json
{
  "verified": true,
  "first_name": "JANEZ",
  "last_name": "NOVAK",
  "emso": "1010005500426"
}
```

**Odgovor pri neuspešni verifikaciji** vsebuje seznam razlogov: `no_face_in_id`, `no_face_in_selfie`, `liveness_failed`, `face_mismatch`, `document_ocr_failed`, `document_expired`, `missing_name`, `missing_surname`, `missing_emso`.

---

## 4. Ujemanje obrazov (`app/face/embed.py`)

Ujemanje obrazov je izvedeno z **DeepFace/ArcFace** (vnaprej naučen model za obrazne vektorje). Cevovod za vsak obraz (dokument, selfie) izračuna vgraditveni vektor (embedding) in primerja prek **kosinusne podobnosti**. Prag je nastavljiv prek `face_match_config.txt`.

```
model=ArcFace
score_type=cosine_similarity
threshold=0.2
decision=same_if_score_greater_or_equal
```

---

## 5. OCR dokumenta (`app/ocr/general.py`)

Za sprednje strani dokumentov (vozniška dovoljenja, osebne izkaznice) se izvede **Tesseract OCR** z lastno predobdelavo slike. Implementirani sta **dve predobdelitveni strategiji**, ki se zaženeta zaporedoma — rezultati se združijo:

| Prehod           | Metoda                               | Kdaj deluje bolje                         |
| ---------------- | ------------------------------------ | ----------------------------------------- |
| **1 (Blackhat)** | Morfološki Blackhat + adaptivni prag | Neenakomerna osvetlitev, guilloche ozadje |
| **2 (Otsu)**     | Gaussov filter + Otsu prag           | Enakomerna svetloba, visok kontrast       |

**Zaznava in poravnava dokumenta:** pred OCR se zazna obris kartice v fotografiji, izvede **perspektivna transformacija** (štiri točke → pravokotnik) in izrez besedilnega dela (brez cone s fotografijo).

**PSM strategija po vrsti dokumenta:**

- `driving_licence` — PSM 11 (razpršeni tekst) z grupiranjem žetonov po y-pasovih, ker PSM 11 loči oznake polj in vrednosti v ločene bloke kljub vizualni poravnavi.
- `id_card` — dvokolonski pristop: levi stolpec z PSM 4 + PSM 6 (PSM 4 ohranja strukturo oznak, PSM 6 da boljšo točnost za kratke vrednosti), desni stolpec z PSM 6.

**Jezikovna podpora:** Tesseract dobi jezike glede na razpoložljivost (`hrv+slv+srp_latn+eng` → `slv+hrv+eng` → … → `eng`).

**Zaznana polja:**

| Polje              | Opis                                                                                                           |
| ------------------ | -------------------------------------------------------------------------------------------------------------- |
| Ime / priimek      | EU vozniško: polja 1 in 2 (prefix/suffix ujemanje); osebna: oznaka `Priimek/Surname` ali strukturna hevristika |
| EMSO               | Regex za točno 13 zaporednih številk                                                                           |
| Datum veljavnosti  | Regex za `DD.MM.YYYY`, `YYYY-MM-DD`; vrne **največji datum** (verjetno datum poteka)                           |
| Številka dokumenta | Regex za alfanumerični format (`ABC123456`)                                                                    |

---

## 6. Integracija v .NET backend

Mobilna aplikacija ne kliče cv-identity **neposredno** — zahtevki gredo prek **ASP.NET Core backenda**, ki je edinstvena vstopna točka.

**Arhitektura integracije (Clean Architecture):**

```
ePrevzem.Application
└── Common/Abstractions/ICvIdentityClient.cs   ← port (vmesnik)
└── Identity/VerifyDocumentAndRegister/        ← use case

ePrevzem.Infrastructure
└── Identity/CvIdentityClient.cs              ← adapter (HTTP)
└── Identity/IdentityOptions.cs               ← konfiguracija BaseUrl
```

**`ICvIdentityClient`** definira pogodbo v aplikacijski plasti — brez odvisnosti od HTTP. Parametri: bajti slike dokumenta, seznam okvirjev selfija (kot `SelfieFrame` vrednostni objekti), vrsta dokumenta.

**`CvIdentityClient`** v infrastrukturni plasti sestavi `MultipartFormDataContent` in pošlje POST na `/verify`. Ločeno obravnava omrežne napake (`CvIdentityUnavailableException`) in neuspešno verifikacijo (`DocumentVerificationFailedException` z razlogi).

**`VerifyDocumentAndRegisterCommand`** je MediatR use case, ki:

1. pokliče `ICvIdentityClient.VerifyAsync`,
2. ob potrditvi ustvari ali poišče `CitizenUser` po EMSO-ju,
3. izda `CitizenActivationCode` (veljavnost 24 ur),
4. vrne kodo za nadaljevanje registracije naprave.

**Konfiguracija** (`appsettings.json` / env var):

```
CvIdentity__BaseUrl=http://cv-identity:8000
```

---

## 7. Dockerizacija

### `cv-identity/Dockerfile`

Temelji na `python:3.12-slim`. Namesti sistemske odvisnosti:

- `libgl1`, `libglib2.0-0` — OpenCV
- `libgomp1` — paralelizem (NumPy/TF)
- `tesseract-ocr` + jezikovni paketi `eng`, `hrv`, `slv`

Python odvisnosti se namestijo iz `requirements.txt`, aplikacijska koda se kopira v `/app`. Servis posluša na portu `8000` prek `uvicorn`.

Poti modelov so nastavljive prek env spremenljivke `CV_IDENTITY_MODELS_DIR` — modeli se montirajo kot zunanji volume, ne pečejo v sliko.

### `docker-compose.yml`

Celoten sistem se zažene z eno samo datoteko. Storitev `cv-identity` je ločen servis, backend pa ga doseže prek Docker notranjega DNS-a:

```
CvIdentity__BaseUrl=http://cv-identity:8000
```

Modeli se montirajo kot read-only volume (`./cv-identity/app/models:/app/models:ro`), da jih ni treba vključiti v Docker sliko (modeli so veliki in se ne commitajo v repozitorij).

## 10. Prispevek v sistemu za verzioniranje

Ključni commiti na vejah `users/edvin/*` in `PRVZM-87*`:

- `PRVZM-87 feat(cv-identity): implement OCR and face matching pipeline` — začetna implementacija cevovoda
- `PRVZM-87 refactor(cv-identity): drop id_back, add EMSO/name extraction, simplify pipeline response` — poenostavitev: en dokument namesto dveh, dodano branje EMSO
- `PRVZM-87 feat(identity): containerize cv identity` — Dockerfile in docker-compose integracija
- `PRVZM-86 fix(identity): make OCR robust to phone photos and fix JSON mapping` — robustnost OCR za telefonske slike
- `route cv-identity through .NET backend, remove direct mobile→python calls` — arhitekturni popravek integracije
