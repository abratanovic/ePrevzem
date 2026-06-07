# Poročilo — Podatkovni del (Adnan)

## 1. O sistemu ePrevzem

**ePrevzem** je platforma za **varen prevzem dokumentov iz pametnih paketnikov**. Občan najprej **registrira svoj račun in napravo** (tam, kjer
danes deluje SI-TRUST), pri čemer enkratno dokaže svojo identiteto. Dokumente nato
prevzema **izključno prek te registrirane naprave**, identiteto pa ob prevzemu
potrdi z **biometrijo na sami napravi** (npr. prstni odtis / obrazno
odklepanje telefona).

Ker gre za prevzem občutljivih dokumentov, je zanesljiva potrditev identitete
ključna — račun sme registrirati **samo prava oseba**, in to **fizično prisotna**,
ne nekdo s fotografijo ali ukradenim dokumentom. Prav to enkratno preverjanje ob
registraciji je naloga računalniškega vida.

Sistem sestavljajo ločeni deli:

| Del | Tehnologija | Vloga |
|-----|-------------|-------|
| **backend** | ASP.NET Core 9 (modularni monolit) | poslovna logika: prevzemi, paketniki, organizacije, revizijska sled |
| **ePrevzemMobile** | Kotlin Multiplatform / Compose (Android) | uporabniški odjemalec za prevzem dokumenta |
| **cv-identity** | Python servis (OpenCV, NumPy, MediaPipe) | računalniški vid za preverjanje identitete |
| **sitrust-mock** | .NET / React / Flutter | simulator državne identitetne infrastrukture SI-TRUST |

**Cilj projekta** pri predmetu Osnove računalniškega vida je nadomestiti (ponuditi
alternativo za) zunanjo identifikacijo (SI-TRUST) z **lastnim sistemom
računalniškega vida**, ki **ob registraciji računa** preveri, da je oseba res
tista, za katero se izdaja, in da je živa. S tem je dostop do prevzema dokumentov
zaščiten z biometričnim preverjanjem že na vstopni točki.

## 2. Vloga računalniškega vida (cv-identity) v sistemu

Računalniški vid se uporabi **enkratno, ob registraciji računa in naprave** — tam,
kjer danes nastopi SI-TRUST. Po uspešni registraciji se naprava poveže z
identiteto občana; nadaljnji prevzemi računalniškega vida ne potrebujejo več.

**Registracijski tok (računalniški vid):**

1. Občan v aplikaciji **ePrevzemMobile** zažene registracijo računa/naprave.
2. Aplikacija ga pozove, naj s **telefonom** poslika svoj **obraz** (s premikom
   glave) in svoj **osebni dokument**.
3. Slike gredo prek **API-ja** (REST, del integracije) na servis **cv-identity**.
4. cv-identity izvede tri korake računalniškega vida:
   - **a) preverjanje živosti (liveness / anti-spoofing)** — je pred kamero živ
     človek ali le fotografija/zaslon ("spoof")? To opravi **lastno naučena
     konvolucijska nevronska mreža (CNN)**.
   - **b) ujemanje obraza z dokumentom** — primerjava obraza s sliko na dokumentu
     prek vnaprej naučenih obraznih vektorjev (embeddingi) in praga podobnosti.
   - **c) branje dokumenta (OCR/MRZ)** — izluščenje besedila/strojno berljive
     cone.
5. Servis vrne **odločitev** (potrjeno / zavrnjeno z verjetnostmi); ob potrditvi
   se naprava registrira kot zaupanja vredna in poveže z identiteto.

**Prevzemni tok (brez računalniškega vida):**

Dokument je mogoče prevzeti **samo prek registrirane naprave**. Identiteto ob
prevzemu potrdi **biometrija na sami napravi** (prstni odtis / obrazno
odklepanje), ki jo ponuja operacijski sistem telefona — računalniški vid in servis
cv-identity tu nista več vključena. Tako se draga in občutljiva identifikacija
opravi enkrat (ob registraciji), vsakdanji prevzemi pa so hitri in zanesljivi.

To poročilo pokriva **podatkovni temelj za korak (a) — preverjanje živosti**, za
katerega je odgovoren **Adnan**. Modeliranje (CNN) je delo **Emira**, API in
povezovanje sistema v celoto pa delo **Edvina**.

## 3. Podatkovni del (Adnan) — pregled

Podatkovni del pripravi vse, kar potrebuje liveness CNN: **zajem in pripravo slik
živih obrazov (`live`) in napadov (`spoof`)**, predobdelavo, lastno augmentacijo
in razdelitev na učno/validacijsko/testno množico.

Tehnologija: ločen Python servis `cv-identity/` (Python 3.12, OpenCV, NumPy,
MediaPipe). Augmentacija je **lastna implementacija** (brez gotovih knjižnic kot
albumentations).

## 4. Viri podatkov

Uporabili smo **hibridno strategijo** (lastni + javni podatki):

| Vir | Količina | Namen |
|-----|---------:|-------|
| **Lastni zajem ekipe** | 306 slik (3 osebe) | realni `live`/`spoof` vzorci, prilagojeni demu |
| **NUAA Photograph Imposter DB** | 12.614 slik (15 oseb) | glavnina učnih podatkov za liveness |
| **LFW (deepfunneled)** | ~13.000 slik | kalibracija praga za ujemanje obrazov (delo Emira) |

**Zakaj NUAA:** liveness CNN potrebuje veliko in raznoliko zbirko `live`/`spoof`.
Naša ekipa fizično ne more zajeti dovolj raznolikih napadov. NUAA je standardna
anti-spoofing zbirka primerne velikosti — izbrali smo jo namesto **CelebA-Spoof
(~80 GB)**, ki je za naš obseg nepraktičen. NUAA `ClientRaw` = živi obrazi
(`live`), `ImposterRaw` = fotografski napadi (`spoof`).

Velike javne zbirke hranimo **izven repozitorija** in
jih nikoli ne commitamo. Lastne slike so v `dataset/raw/` (git-ignored, brez
PII).

## 5. Zajem podatkov

- **Skripta za zajem** (`scripts/capture.py`): zajem prek spletne kamere; tipka
  SPACE shrani sliko, `q` zaključi; slike se shranjujejo v
  `dataset/raw/<live|spoof>/<oseba>/`. Namenjena hitremu testu cevovoda.
- **Realni zajem ekipe:** ker je demo na telefonu, smo večino učnih slik zajeli
  s **telefonom** in jih ročno
  prekopirali v `dataset/raw/`.
- **Protokol zajema** (dokumentiran v `dataset/README.md`):
  - **Live:** ~30–50 posnetkov na osebo, večinoma blizu frontalni (±30°), nekaj
    levo/desno in gor/dol; izogibanje ekstremnim profilom (>45°), ker jih
    detektor obraza ne najde. Variranje svetlobe in razdalje.
  - **Spoof:** vzorec je vsak **posnetek**, ne vsak vir — 2–3 natisnjene + 2–3
    zaslonske slike, vsako poslikamo iz več kotov/razdalj/osvetlitev. Variiranje
    pogojev (nagib, svetlost zaslona) ustvari prave znake ponaredka (odsev,
    moiré, robovi). Različni mediji (papir + telefon + monitor).
  - **Oboje:** `live` in `spoof` iste osebe zajeti na **isti napravi** in v
    podobnih pogojih, da se model ne nauči bližnjice (npr. naprava/svetlost
    namesto pravih znakov živosti).

**Dejansko zajeto (ekipa):**

| Oseba | live | spoof |
|-------|-----:|------:|
| Adnan | 58 | 32 |
| Edvin | 81 | 26 |
| Emir  | 55 | 54 |
| **Skupaj** | **194** | **112** |

## 6. Organizacija podatkov

```
cv-identity/dataset/
├── raw/                       # originalni zajemi (git-ignored)
│   ├── live/<oseba>/          # živi obrazi
│   ├── spoof/<oseba>/         # napadi (natisnjeno + zaslon)
│   └── id_cards/<oseba>/      # mock dokumenti (brez PII), za end-to-end demo
└── splits/{train,val,test}/   # razdelitev brez prelivanja identitet
    └── {live,spoof}/<oseba>__<ime>.jpg
```

Predobdelava se izvaja **sproti** med gradnjo (`raw → splits`); ločene mape
`processed/` ne uporabljamo, da slik ne podvajamo na disku.

## 7. Predobdelava slik

Predobdelava (`app/preprocessing.py`) je deljena med učenjem in inferenco —
**isti postopek za train in serve** zagotavlja konsistentnost.

| Postopek predobdelave | Implementacija | Funkcija |
|-----------------------|----------------|----------|
| Sprememba velikosti slik | `cv2.resize` z `INTER_AREA` (najboljše za zmanjševanje) | `resize_image` |
| Pretvorba v barvne prostore | RGB / HSV / YCrCb | `to_colorspace` |
| Linearizacija sivinskih vrednosti | pretvorba v sivinsko sliko | `to_grayscale` |
| Normalizacija slikovnih pik | [0,255] uint8 → [0,1] float32 | `normalize_pixels` |
| Odstranjevanje šuma | Gaussov filter (3×3) | `denoise` |
| Izrezovanje relevantnih delov | izrez po bbox (z omejitvijo na robove) | `crop_to_bbox` |
| Detekcija + poravnava obraza | MediaPipe + poravnava po očeh + Haar fallback | `detect_and_crop_face`, `align_by_eyes` |

**Detekcija obraza (`detect_and_crop_face`):**
- Primarni detektor: **MediaPipe FaceDetection** (`model_selection=0`,
  `min_detection_confidence=0.5`) — vrne tudi ključne točke (oči) za poravnavo.
- Najprej izrežemo **bbox z robom +30 %** (`_expand_bbox`), da obdržimo čelo in
  brado, nato poravnamo. Robno polnjenje `BORDER_REFLECT`.
- **Rezervni detektor:** OpenCV Haar kaskada, če MediaPipe ne najde obraza.
- Vrne `None`, če obraza ne najde nobeden (taka slika se v gradnji preskoči).

**Poravnava po očeh (`align_by_eyes`):** sliko zarotira, da je črta med očmi
vodoravna. Očesi **interno uredi po x-koordinati** (levo oko = manjši x), ker
detektorji označujejo ključne točke iz perspektive osebe — neposredna uporaba
vrstnega reda detektorja bi sliko zasukala za ~180°. (To je bil dejanski hrošč,
ki smo ga odpravili — glej §11.)

## 8. Augmentacija podatkov (lastna implementacija)

Augmentacija (`training/augmentation.py`) je napisana **ročno z OpenCV/NumPy**,
brez gotovih augmentacijskih knjižnic. Namen: umetno povečati raznolikost učne
množice, da model bolje generalizira in se ne "nauči na pamet".

| Funkcija | Kaj dela | Zakaj |
|----------|----------|-------|
| `rotate` | rotacija (−15°…+15°) | toleranca na nagib glave |
| `flip_horizontal` | zrcaljenje L–D (verj. 0,5) | obraz je ~simetričen |
| `scale` | zoom 0,9–1,1, nato izrez/oblazinjenje na izvirno velikost | različne razdalje |
| `adjust_brightness` | premik svetlosti (−40…+40), int16 + clip | različna osvetlitev |
| `add_gaussian_noise` | Gaussov šum (σ 0–12), reproducibilen prek `rng` | senzorski šum |
| `gaussian_blur` | zameglitev (verj. 0,3) | posnema nizko ločljivost/tisk |
| `add_jpeg_artifacts` | ponovno JPEG kodiranje nizke kakovosti (20–50) | kompresijski artefakti |

`augment_image(image, count, rng)` naključno sestavi te transformacije v `count`
variant. **ID-degradacija** (blur + JPEG artefakti) se uporabi z verjetnostjo
0,3 — namenoma posnema videz slik na dokumentih in nizkokakovostnih posnetkov.
Vse je reproducibilno prek podanega generatorja `rng` (isti seme → isti izid).

## 9. Priprava učne, validacijske in testne množice

**Razdelitev po identiteti** (`training/split.py`,
`split_by_identity(records, ratios=(0.6,0.2,0.2), seed)`): osebe (ne posamezne
slike) se deterministično premešajo in **celota** vsake osebe gre v enega od
naborov train/val/test.

**Zakaj po identiteti, ne po slikah:** če bi se ista oseba pojavila v train in
test, bi model na testu prepoznaval **že videne obraze** → lažno visoki
rezultati (data leakage). Delitev po osebah zagotovi, da test meri uspešnost na
**novih ljudeh**.

**Vloge naborov:**
- **train** — model se uči (prilagaja uteži),
- **val** — preverjanje napredka med učenjem, izbira hiperparametrov in
  zgodnja ustavitev (Emir); model se na njej ne uči,
- **test** — uporabljen **enkrat na koncu** za pošteno končno meritev.

## 10. Gradnja podatkov in rezultati

Skripta `scripts/build_dataset.py` poveže vse zgornje korake: prebere lastne in
NUAA slike, izvede predobdelavo (za lastne tudi detekcijo+poravnavo; NUAA slike
so že izrezani obrazi), razdeli po identiteti in **augmentira samo train**.

Zagon:
```bash
python scripts/build_dataset.py --raw dataset/raw \
    --nuaa C:/PROJEKTI/datasets/nuaa/raw --out dataset --augment-count 4
```

**Rezultati gradnje** (velikost slik 112×112, 4 augmentirane variante na train
sliko):

| Split | Skupaj | Osnovne | Augmentirane |
|-------|-------:|--------:|-------------:|
| train | 30.270 | 6.054 | 24.216 |
| val   | 2.919 | 2.919 | 0 |
| test  | 3.947 | 3.947 | 0 |
| **Skupaj** | **37.136** | **12.920** | **24.216** |

Po razredih (osnovne slike):

| Split | live | spoof |
|-------|-----:|------:|
| train | 1.474 | 4.580 |
| val   | 1.420 | 1.499 |
| test  | 2.405 | 1.542 |

**Preverjanje kakovosti:**
- **Nič izgube ekipnih slik:** vseh 306 obrazov uspešno zaznanih (0 preskočenih).
- **Brez prelivanja identitet:** ekipne osebe se ne pojavijo v več kot enem
  splitu (train/test/val disjunktni); enako velja za NUAA osebe (`nuaa_<id>`).
- **Augmentacija samo v train** (24.216 `_aug` datotek; val/test = 0).
- **Velikost naborov:** razmerja niso natanko 60/20/20, ker delimo po **celih
  osebah** z različnim številom slik — to je pričakovano pri delitvi po identiteti.
- **Razredna neuravnoteženost** (več spoof kot live v train): naj jo Emir
  obravnava z metrikami po razredih (FAR/FRR, ROC-AUC, matrika zmot), ne s surovo
  natančnostjo.

## 11. Težave in rešitve

- **Napačna orientacija obraza (kritično):** prvi izrezi so bili obrnjeni za
  ~180° in odrezani pod usti. Vzrok: MediaPipe ključne točke so v perspektivi
  osebe, koda pa je oči obravnavala v napačnem vrstnem redu → napačen kot
  rotacije. Rešeno z internim urejanjem oči po x in z izrezom z robom **pred**
  poravnavo. Dodan determinističen regresijski test
  (`test_align_by_eyes_is_order_independent`).
- **Združljivost Pythona:** MediaPipe 0.10.18 nima koles za 3.13/3.14 → projekt
  uporablja **Python 3.12** (`py -3.12 -m venv .venv`).
- **Domain gap (webcam vs telefon):** ker je demo na telefonu, smo učne slike
  zajeli s telefonom; augmentacija (blur/JPEG/svetlost) dodatno premosti razlike
  med napravami.

## 12. Testiranje

Razvoj po načelu **TDD** (test-driven development), ogrodje **pytest**.
**28 testov** (vsi zeleni) pokriva predobdelavo, augmentacijo, razdelitev,
gradnjo in **golden-image integracijski test** (preverja celoten cevovod na
pravi sliki obraza ter pravilno vrne `None`, ko obraza ni).

```
28 passed in 9.00s
```

## 13. Predaja Emiru

Emir lahko delo uporabi samostojno:
- `dataset/splits/{train,val,test}/` — pripravljeni nabori za nalaganje v model.
- `app/preprocessing.py` in `training/split.py` — dokumentirane in uvozljive
  funkcije; **isti preprocessing za učenje in inferenco**.
- `dataset/README.md` — datasheet (layout, razredi, viri, pravilo delitve po
  identiteti, protokol zajema).
- `CHECKLIST.md` — definicija dokončanosti podatkovnega dela.

## 14. Prispevek v sistemu za verzioniranje

Vse delo je na veji `PRVZM-62-cv-identity-podatkovni-del`; vsak commit nosi Jira
ID (Epic PRVZM-72 → podnaloga PRVZM-62 → opravila PRVZM-63…71). Iz git zgodovine
je razviden prispevek podatkovnega dela:

- `PRVZM-63` — skelet projekta + testne podlage (pytest fixtures)
- `PRVZM-66` — predobdelava slik (+ popravek orientacije obraza)
- `PRVZM-67` — lastna augmentacija
- `PRVZM-68` — razdelitev po identiteti
- `PRVZM-65` — skripta za zajem
- `PRVZM-71` — golden-image integracijski test
- `PRVZM-69` — `build_dataset.py` (raw + NUAA → spliti)
- `PRVZM-70` — datasheet in dokumentacija

---

### Datotečna struktura podatkovnega dela

```
cv-identity/
├── app/preprocessing.py        # predobdelava (deljena: train + inferenca)
├── training/
│   ├── augmentation.py         # lastna augmentacija
│   └── split.py                # razdelitev po identiteti
├── scripts/
│   ├── capture.py              # zajem s kamere
│   └── build_dataset.py        # raw + NUAA -> spliti
├── tests/                      # 28 testov (pytest) + golden slike
├── dataset/README.md           # datasheet
└── CHECKLIST.md                # definicija dokončanosti
```
