# 📦 ePrevzem — Sistem za varen prevzem dokumentov

> Generična (multi-tenant) platforma za **varen prevzem dokumentov in občutljivih
> predmetov iz pametnih paketnikov** — brez čakanja na dostavo in brez vrst.

Razvito v okviru projekta _Pametni paketnik_ na Fakulteti za elektrotehniko,
računalništvo in informatiko Univerze v Mariboru (študijski program Računalništvo
in informacijske tehnologije, VS).

---

## 📑 Kazalo

- [Opis](#-opis)
- [Glavne funkcionalnosti](#-glavne-funkcionalnosti)
- [Arhitektura in repozitorij](#️-arhitektura-in-repozitorij)
- [Tehnologije](#️-tehnologije)
- [Hitri zagon (Docker)](#-hitri-zagon-docker)
- [Storitve in vrata](#-storitve-in-vrata)
- [Razvojno okolje po podprojektih](#-razvojno-okolje-po-podprojektih)
- [Konfiguracija (okoljske spremenljivke)](#-konfiguracija-okoljske-spremenljivke)
- [Testiranje](#-testiranje)
- [Osnovni potek uporabe](#-osnovni-potek-uporabe)
- [Naslednji koraki](#-naslednji-koraki)
- [Ekipa](#-ekipa)
- [Opombe](#-opombe)

---

## 🧾 Opis

Sistem omogoča organizacijam (upravne enote, univerze, podjetja, banke), da
pripravijo občutljive dokumente za prevzem v paketniku. Uporabnik prejme
obvestilo, pride do paketnika, se **varno identificira** in odklene predalček
prek mobilne aplikacije. Vsi dogodki so zabeleženi v revizijski sledi (audit log).

Identifikacija se opravi na en od dva načina:

- **Računalniški vid (cv-identity)** ob registraciji preveri živost obraza in
  ujemanje s sliko na osebnem dokumentu ter prebere podatke z dokumenta (OCR).
- **Simulacija državnih storitev (sitrust-mock)** posnema SI-TRUST / SI-PASS in
  eOsebno (NFC / biometrija).

---

## 🎯 Glavne funkcionalnosti

**Varen proces prevzema**
- Varna identifikacija pred uporabo sistema
- Odklep ustreznega predalčka prek mobilne aplikacije
- Beleženje vseh odklepov in dogodkov (audit log)

**Za uporabnika**
- Pregled aktivnih prevzemov, roka in lokacije paketnika
- Začetek postopka identifikacije in odklep predalčka
- Zgodovina prevzemov

**Za organizacijo (portal)**
- Upravljanje uporabnikov in organizacij
- Kreiranje zahtevkov za prevzem in dodeljevanje dokumentov paketnikom
- Spremljanje statusa prevzema in pregled dnevnika dogodkov

**Integracija paketnikov**
- Dejanska komunikacija s paketniki **Direct4.me** (odklep posameznega predalčka,
  beleženje uspešnosti); arhitektura dopušča zamenjavo integracijskega sloja za
  druge tipe paketnikov.

---

## 🏗️ Arhitektura in repozitorij

**Poliglotni monorepo** — vsak podprojekt ima svojo orodjarno; krovne gradnje ni.
Korenska .NET rešitev (`ePrevzem.sln`) povezuje **samo** `backend/`.

```
ePrevzem/
├── backend/          # ASP.NET Core 9 modularni monolit (Clean Architecture) — produkcijski backend
│   ├── ePrevzem.Api             # tanki kontrolerji, DI, avtentikacija, OpenAPI
│   ├── ePrevzem.Application     # MediatR use case-i, DTO-ji, validatorji, porti
│   ├── ePrevzem.Domain          # agregati, vrednostni objekti, domenski dogodki (brez odvisnosti)
│   ├── ePrevzem.Infrastructure  # EF Core (Npgsql), adapterji
│   └── ePrevzem.Tests           # xUnit + Testcontainers Postgres
├── frontend/         # React 19 + Vite — administracijski portal za organizacije
├── ePrevzemMobile/   # Kotlin Multiplatform / Compose (Android + iOS) — odjemalec za prevzem
├── cv-identity/      # Python servis (FastAPI, OpenCV, MediaPipe) — računalniški vid za identiteto
├── sitrust-mock/     # Simulator državne identitetne infrastrukture (ločena rešitev)
│   ├── backend/        #   ASP.NET Core 9 mock SI-TRUST API
│   ├── frontend/       #   React 19 + Vite — SI-PASS spletna prijava
│   └── eosebna_mobile/ #   Flutter — eOsebna NFC/biometrija
├── docker-compose.yml       # lokalni / razvojni stack
└── docker-compose.prod.yml  # produkcijski stack
```

Backend sledi strogemu enosmernemu toku odvisnosti (`Api → Application → Domain`,
`Infrastructure → Application, Domain`), z modularno delitvijo po funkcionalnostih
(Organizations / Pickups / Lockers / Delegations / Identity / Audit /
Notifications) in komunikacijo med moduli prek domenskih dogodkov.

> Servis računalniškega vida je podrobneje opisan v `cv-identity/README.md`,
> celotno poročilo o njem pa v `cv-identity/docs/porocilo.md`.

---

## 🛠️ Tehnologije

| Področje | Tehnologija |
|----------|-------------|
| Backend | .NET 9 (ASP.NET Core Web API), EF Core 9 + Npgsql, MediatR, FluentValidation, Serilog, JWT |
| Baza | PostgreSQL 18 |
| Administracijski portal | React 19 + Vite + TypeScript |
| Mobilna aplikacija | Kotlin Multiplatform / Compose Multiplatform (Android + iOS) |
| Računalniški vid | Python 3.12, FastAPI, OpenCV, MediaPipe, TensorFlow/Keras, DeepFace (ArcFace), Tesseract OCR |
| Simulator identitete | ASP.NET Core 9 + React 19 + Flutter |
| Kontejnerizacija | Docker / Docker Compose |

---

## 🚀 Hitri zagon (Docker)

Najhitrejši način je celoten stack prek Docker Compose.

**Predpogoji:** Docker in Docker Compose.

```bash
git clone https://github.com/abratanovic/ePrevzem.git
cd ePrevzem

# 1) pripravi okoljske spremenljivke
cp .env.example .env        # nato uredi vrednosti (gesla, JWT skrivnosti …)

# 2) zaženi celoten sistem
docker compose up -d

# 3) preveri delovanje
curl http://localhost:8080/health     # backend
curl http://localhost:8000/health     # cv-identity
```

Servis računalniškega vida potrebuje artefakte modela na gostitelju pod
`cv-identity/app/models/` (`liveness_model.keras`, `threshold.txt`,
`face_match_config.txt`) — montirajo se kot read-only volume. Glej
`cv-identity/README.md`.

Posamezno storitev zaženete z imenom, npr. `docker compose up -d cv-identity`.
Za produkcijo uporabite `docker-compose.prod.yml`.

---

## 🌐 Storitve in vrata

| Storitev | Vloga | Vrata (host) |
|----------|-------|-------------:|
| `frontend` | React administracijski portal | 3000 |
| `backend` | ePrevzem REST API | 8080 |
| `cv-identity` | API računalniškega vida | 8000 |
| `sitrust-backend` | Mock SI-TRUST / SI-PASS API | 5070 |
| `sitrust-frontend` | Mock SI-PASS spletna prijava | 5174 |
| `postgres-db` | PostgreSQL | (interno) |

---

## 💻 Razvojno okolje po podprojektih

Vsak podprojekt je samostojno gradljiv s svojo orodjarno.

### backend — ASP.NET Core 9

```bash
dotnet build ePrevzem.sln
dotnet run --project backend/ePrevzem.Api
dotnet test backend/ePrevzem.Tests          # Testcontainers Postgres (potrebuje Docker)

# EF migracije
dotnet ef migrations add <Name> \
  --project backend/ePrevzem.Infrastructure --startup-project backend/ePrevzem.Api
```

### frontend — React 19 + Vite

```bash
cd frontend
npm install
npm run dev        # razvojni strežnik
npm run build      # produkcijska gradnja
npm run lint
```

### ePrevzemMobile — Kotlin Multiplatform / Compose

```bash
cd ePrevzemMobile
./gradlew :composeApp:assembleDebug                          # Android APK
./gradlew :composeApp:installDebug                           # namestitev na napravo
./gradlew :composeApp:compileCommonMainKotlinMetadata        # hiter cross-platform preizkus
./gradlew :composeApp:allTests
# Windows: uporabite gradlew.bat
```

### cv-identity — Python / FastAPI

```bash
cd cv-identity
py -3.12 -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
pytest -v
```

Podroben opis (artefakti, endpointi, uporaba) je v `cv-identity/README.md`.

### sitrust-mock — simulator identitete

```bash
cd sitrust-mock
dotnet run --project backend/SiTrustMock      # mock API
cd frontend && npm install && npm run dev     # SI-PASS spletna prijava
cd eosebna_mobile && flutter pub get && flutter run   # eOsebna mock
```

---

## 🔑 Konfiguracija (okoljske spremenljivke)

Compose bere spremenljivke iz datoteke `.env` v korenu (predloga: `.env.example`).
Ključne spremenljivke:

| Spremenljivka | Opis |
|---------------|------|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | PostgreSQL poverilnice in ime baze |
| `JWT_SECRET` / `JWT_ISSUER` / `JWT_AUDIENCE` | JWT podpisovanje za backend |
| `BOOTSTRAP_ADMIN_USERNAME` / `BOOTSTRAP_ADMIN_PASSWORD` | začetni administrator |
| `SITRUST_JWT_SECRET` / `SITRUST_BASE_URL` / `SITRUST_PUBLIC_BASE_URL` | mock SI-TRUST |
| `VITE_EPREVZEM_URL` / `VITE_SIPASS_URL` | URL-ji za spletne odjemalce |

Backend kliče cv-identity prek `CvIdentity__BaseUrl` (v Docker omrežju
`http://cv-identity:8000`).

---

## 🧪 Testiranje

```bash
dotnet test backend/ePrevzem.Tests              # backend (xUnit + Testcontainers)
dotnet test sitrust-mock/backend/SiTrustMock.Tests
cd cv-identity && pytest -v                      # računalniški vid
cd ePrevzemMobile && ./gradlew :composeApp:allTests
cd sitrust-mock/eosebna_mobile && flutter test
```

---

## 🔄 Osnovni potek uporabe

1. Organizacija ustvari zahtevek za prevzem dokumenta.
2. Dokument se shrani v predalček paketnika.
3. Uporabnik  pride do paketnika.
4. V aplikaciji se varno identificira.
5. Sistem preveri pravice dostopa.
6. Predalček se odklene.
7. Dogodek se zabeleži (audit log).

---

## 🧭 Naslednji koraki

Načrtovane funkcionalnosti, ki še niso implementirane:

- **Delegacija prevzema drugi osebi** (elektronsko pooblastilo) — uporabnik bo
  lahko pooblastil drugo osebo za prevzem v svojem imenu.

---

## 👥 Ekipa

- Adnan Bratanović (vodja)
- Edvin Bečić
- Emir Ribić

---

## 📝 Opombe

- Projekt je prototip za izobraževalne namene.
- Deli identifikacije (SI-TRUST / eOsebna) so **simulirani** — nikoli niso
  povezani z realnimi državnimi storitvami; v tem repozitoriju se ne uporablja
  resničnih osebnih podatkov.
- Fokus je na arhitekturi, varnosti in uporabniški izkušnji.
