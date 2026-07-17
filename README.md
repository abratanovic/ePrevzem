# 📦 ePrevzem — Secure Document Pickup System

> A generic (multi-tenant) platform for **secure pickup of documents and sensitive
> items from smart lockers** — no waiting for delivery and no queues.

Developed as part of the _Smart Locker_ project at the Faculty of Electrical
Engineering, Computer Science and Informatics, University of Maribor (Computer
Science and Information Technologies study program, VS).

---

## 📑 Table of Contents

- [Description](#-description)
- [Main Features](#-main-features)
- [Architecture and Repository](#️-architecture-and-repository)
- [Technologies](#️-technologies)
- [Quick Start (Docker)](#-quick-start-docker)
- [Services and Ports](#-services-and-ports)
- [Development Environment per Subproject](#-development-environment-per-subproject)
- [Configuration (Environment Variables)](#-configuration-environment-variables)
- [Testing](#-testing)
- [Basic Usage Flow](#-basic-usage-flow)
- [Next Steps](#-next-steps)
- [Team](#-team)
- [Notes](#-notes)

---

## 🧾 Description

The system enables organizations (administrative units, universities, companies,
banks) to prepare sensitive documents for pickup from a locker. The user receives
a notification, comes to the locker, **securely identifies themselves**, and
unlocks the compartment via the mobile application. All events are recorded in an
audit log.

Identification is performed in one of two ways:

- **Computer vision (cv-identity)** verifies face liveness during registration and
  the match with the photo on the identity document, and reads the data from the
  document (OCR).
- **Simulation of state services (sitrust-mock)** mimics SI-TRUST / SI-PASS and
  eOsebna (NFC / biometrics).

---

## 🎯 Main Features

**Secure pickup process**
- Secure identification before using the system
- Unlocking the appropriate compartment via the mobile application
- Logging of all unlocks and events (audit log)

**For the user**
- Overview of active pickups, deadline, and locker location
- Starting the identification process and unlocking the compartment
- Pickup history

**For the organization (portal)**
- Management of users and organizations
- Creating pickup requests and assigning documents to lockers
- Tracking pickup status and reviewing the event log

**Locker integration**
- Actual communication with **Direct4.me** lockers (unlocking an individual
  compartment, logging success); the architecture allows replacing the integration
  layer for other types of lockers.

---

## 🏗️ Architecture and Repository

**Polyglot monorepo** — each subproject has its own toolchain; there is no
top-level build. The root .NET solution (`ePrevzem.sln`) wires **only** `backend/`.

```
ePrevzem/
├── backend/          # ASP.NET Core 9 modular monolith (Clean Architecture) — production backend
│   ├── ePrevzem.Api             # thin controllers, DI, authentication, OpenAPI
│   ├── ePrevzem.Application     # MediatR use cases, DTOs, validators, ports
│   ├── ePrevzem.Domain          # aggregates, value objects, domain events (no dependencies)
│   ├── ePrevzem.Infrastructure  # EF Core (Npgsql), adapters
│   └── ePrevzem.Tests           # xUnit + Testcontainers Postgres
├── frontend/         # React 19 + Vite — administration portal for organizations
├── ePrevzemMobile/   # Kotlin Multiplatform / Compose (Android + iOS) — pickup client
├── cv-identity/      # Python service (FastAPI, OpenCV, MediaPipe) — computer vision for identity
├── sitrust-mock/     # State identity infrastructure simulator (separate solution)
│   ├── backend/        #   ASP.NET Core 9 mock SI-TRUST API
│   ├── frontend/       #   React 19 + Vite — SI-PASS web login
│   └── eosebna_mobile/ #   Flutter — eOsebna NFC/biometrics
├── docker-compose.yml       # local / development stack
└── docker-compose.prod.yml  # production stack
```

The backend follows a strict one-way dependency flow (`Api → Application → Domain`,
`Infrastructure → Application, Domain`), with modular separation by feature
(Organizations / Pickups / Lockers / Delegations / Identity / Audit /
Notifications) and communication between modules via domain events.

> The computer vision service is described in more detail in
> `cv-identity/README.md`, and the full report on it is in
> `cv-identity/docs/porocilo.md`.

---

## 🛠️ Technologies

| Area | Technology |
|----------|-------------|
| Backend | .NET 9 (ASP.NET Core Web API), EF Core 9 + Npgsql, MediatR, FluentValidation, Serilog, JWT |
| Database | PostgreSQL 18 |
| Administration portal | React 19 + Vite + TypeScript |
| Mobile application | Kotlin Multiplatform / Compose Multiplatform (Android + iOS) |
| Computer vision | Python 3.12, FastAPI, OpenCV, MediaPipe, TensorFlow/Keras, DeepFace (ArcFace), Tesseract OCR |
| Identity simulator | ASP.NET Core 9 + React 19 + Flutter |
| Containerization | Docker / Docker Compose |

---

## 🚀 Quick Start (Docker)

The fastest way is the entire stack via Docker Compose.

**Prerequisites:** Docker and Docker Compose.

```bash
git clone https://github.com/abratanovic/ePrevzem.git
cd ePrevzem

# 1) prepare environment variables
cp .env.example .env        # then edit the values (passwords, JWT secrets …)

# 2) start the entire system
docker compose up -d

# 3) verify it works
curl http://localhost:8080/health     # backend
curl http://localhost:8000/health     # cv-identity
```

The computer vision service needs model artifacts on the host under
`cv-identity/app/models/` (`liveness_model.keras`, `threshold.txt`,
`face_match_config.txt`) — they are mounted as a read-only volume. See
`cv-identity/README.md`.

You can start an individual service by name, e.g. `docker compose up -d cv-identity`.
For production, use `docker-compose.prod.yml`.

---

## 🌐 Services and Ports

| Service | Role | Port (host) |
|----------|-------|-------------:|
| `frontend` | React administration portal | 3000 |
| `backend` | ePrevzem REST API | 8080 |
| `cv-identity` | Computer vision API | 8000 |
| `sitrust-backend` | Mock SI-TRUST / SI-PASS API | 5070 |
| `sitrust-frontend` | Mock SI-PASS web login | 5174 |
| `postgres-db` | PostgreSQL | (internal) |

---

## 💻 Development Environment per Subproject

Each subproject is independently buildable with its own toolchain.

### backend — ASP.NET Core 9

```bash
dotnet build ePrevzem.sln
dotnet run --project backend/ePrevzem.Api
dotnet test backend/ePrevzem.Tests          # Testcontainers Postgres (requires Docker)

# EF migrations
dotnet ef migrations add <Name> \
  --project backend/ePrevzem.Infrastructure --startup-project backend/ePrevzem.Api
```

### frontend — React 19 + Vite

```bash
cd frontend
npm install
npm run dev        # development server
npm run build      # production build
npm run lint
```

### ePrevzemMobile — Kotlin Multiplatform / Compose

```bash
cd ePrevzemMobile
./gradlew :composeApp:assembleDebug                          # Android APK
./gradlew :composeApp:installDebug                           # install on device
./gradlew :composeApp:compileCommonMainKotlinMetadata        # fast cross-platform check
./gradlew :composeApp:allTests
# Windows: use gradlew.bat
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

A detailed description (artifacts, endpoints, usage) is in `cv-identity/README.md`.

### sitrust-mock — identity simulator

```bash
cd sitrust-mock
dotnet run --project backend/SiTrustMock      # mock API
cd frontend && npm install && npm run dev     # SI-PASS web login
cd eosebna_mobile && flutter pub get && flutter run   # eOsebna mock
```

---

## 🔑 Configuration (Environment Variables)

Compose reads variables from the `.env` file in the root (template: `.env.example`).
Key variables:

| Variable | Description |
|---------------|------|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | PostgreSQL credentials and database name |
| `JWT_SECRET` / `JWT_ISSUER` / `JWT_AUDIENCE` | JWT signing for the backend |
| `BOOTSTRAP_ADMIN_USERNAME` / `BOOTSTRAP_ADMIN_PASSWORD` | initial administrator |
| `SITRUST_JWT_SECRET` / `SITRUST_BASE_URL` / `SITRUST_PUBLIC_BASE_URL` | mock SI-TRUST |
| `VITE_EPREVZEM_URL` / `VITE_SIPASS_URL` | URLs for web clients |

The backend calls cv-identity via `CvIdentity__BaseUrl` (in the Docker network
`http://cv-identity:8000`).

---

## 🧪 Testing

```bash
dotnet test backend/ePrevzem.Tests              # backend (xUnit + Testcontainers)
dotnet test sitrust-mock/backend/SiTrustMock.Tests
cd cv-identity && pytest -v                      # computer vision
cd ePrevzemMobile && ./gradlew :composeApp:allTests
cd sitrust-mock/eosebna_mobile && flutter test
```

---

## 🔄 Basic Usage Flow

1. The organization creates a document pickup request.
2. The document is stored in a locker compartment.
3. The user comes to the locker.
4. They securely identify themselves in the application.
5. The system verifies access rights.
6. The compartment is unlocked.
7. The event is logged (audit log).

---

## 🧭 Next Steps

Planned features that are not yet implemented:

- **Delegating pickup to another person** (electronic authorization) — the user
  will be able to authorize another person to pick up on their behalf.

---

## 👥 Team

- Adnan Bratanović (lead)
- Edvin Bečić
- Emir Ribić

---

## 📝 Notes

- The project is a prototype for educational purposes.
- The identification parts (SI-TRUST / eOsebna) are **simulated** — they are never
  connected to real state services; no real personal data is used in this
  repository.
- The focus is on architecture, security, and user experience.
