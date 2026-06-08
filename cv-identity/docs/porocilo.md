# Poročilo — cv-identity (računalniški vid za preverjanje identitete)

Ekipa: Mentis
Člani: Adnan Bratanović (vodja), Emir Ribić, Edvin Bečić

Skupno poročilo treh delov servisa `cv-identity`: **podatkovni del (Adnan)**,
**modelni del (Emir)** ter **API, OCR in integracija (Edvin)**. Sekciji 1–2 sta
skupni opis sistema in vloge računalniškega vida; nato sledijo trije deli po
zadolžitvah.

## 1. O sistemu ePrevzem

**ePrevzem** je platforma za **varen prevzem dokumentov in predmetov iz pametnih paketnikov**. Občan najprej **registrira svoj račun in napravo**, pri čemer enkratno dokaže svojo identiteto. Dokumente in druge predmete nato
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

**Razdelitev dela in zgradba poročila:**

- **Podatkovni del (Adnan)** — zajem, predobdelava, augmentacija in
  razdelitev podatkov za korak (a).
- **Modelni del (Emir)** — učenje in vrednotenje liveness CNN (korak a)
  ter nastavitev algoritma za ujemanje obrazov (korak b).
- **API, OCR in integracija (Edvin)** — FastAPI servis, cevovod
  verifikacije, OCR dokumenta (korak c) ter vključitev v .NET backend in Docker.

---

# Podatkovni del (Adnan)

## 3. Podatkovni del — pregled

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
jih nikoli ne commitamo. Lastne slike so v `dataset/raw/` (git-ignored).

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
│   └── spoof/<oseba>/         # napadi (natisnjeno + zaslon)
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
test, bi model na testu prepoznaval **že videne obraze**, kar bi rezultiralo z lažno visokimi
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
- **Razredna neuravnoteženost** (več spoof kot live v train): naj jo modelni del
  obravnava z metrikami po razredih (FAR/FRR, ROC-AUC, matrika zmot), ne s surovo
  natančnostjo, in z utežmi razredov (glej §13).

## 11. Težave in rešitve (podatkovni del)

- **Napačna orientacija obraza (kritično):** prvi izrezi so bili obrnjeni za
  ~180° in odrezani pod usti. Vzrok: MediaPipe ključne točke so v perspektivi
  osebe, koda pa je oči obravnavala v napačnem vrstnem redu → napačen kot
  rotacije. Rešeno z internim urejanjem oči po x in z izrezom z robom **pred**
  poravnavo. Dodan determinističen regresijski test
  (`test_align_by_eyes_is_order_independent`).
- **Združljivost Pythona:** MediaPipe 0.10.18 nima podpore za 3.13/3.14, zato projekt
  uporablja **Python 3.12** (`py -3.12 -m venv .venv`).
- **Domain gap (webcam vs telefon):** ker je demo na telefonu, smo učne slike
  zajeli s telefonom; augmentacija (blur/JPEG/svetlost) dodatno premosti razlike
  med napravami.

---

# Modelni del (Emir)

## 12. Modelni del — pregled

Modelni del zajema razvoj, učenje, nastavljanje in vrednotenje **modela za
preverjanje živosti** ter nastavitev **algoritma za ujemanje obrazov**. Prispevek
je razdeljen na dva dela:

| Del | Namen | Pristop | Izhod za aplikacijo |
|-----|-------|---------|---------------------|
| **Liveness / anti-spoofing** | Ločiti `live` od `spoof` slik | MobileNetV2 + lastna klasifikacijska glava + fino doučenje | `liveness_model.keras`, `liveness_model.tflite`, `threshold.txt` |
| **Ujemanje obrazov** | Preveriti, ali sta dokument in selfie ista oseba | Pretrained ArcFace embeddingi + kosinusna podobnost + kalibriran prag | `face_match_config.txt` |

Pri liveness delu je uporabljeno **prenosno učenje** in model je dejansko učen na
pripravljenih podatkih. Pri ujemanju obrazov ni učenja novega modela — uporabljen
je vnaprej naučen ArcFace model, dodani pa so izbor modela, metrike, kalibracija
praga in vrednotenje.

## 13. Podatki za učenje liveness modela

Uporabljeni so pripravljeni podatki iz podatkovnega dela (§3–§10): obrazi so že
zaznani, izrezani, poravnani in shranjeni v velikosti **112×112**, razdeljeni v
`splits/{train,val,test}/{live,spoof}/`. Velikosti množic so v §10.

Pri nalaganju z `image_dataset_from_directory` TensorFlow razrede določi po
abecedi, zato model vrača **eno število — verjetnost, da je vhodna slika spoof**:

```text
0 = live
1 = spoof
```

V učni množici je razredna neuravnoteženost (po augmentaciji train množice):

| Razred | Število train slik |
|--------|-------------------:|
| live | 7.370 |
| spoof | 22.900 |

Zato so pri učenju uporabljene **uteži razredov**, da redkejši razred dobi večjo
težo in se model ne nauči prepogosto napovedovati `spoof`:

```text
live  -> 2.0536
spoof -> 0.6609
```

## 14. Izbira in arhitektura liveness modela

Za liveness je izbran **MobileNetV2** z utežmi z zbirke ImageNet, brez originalne
klasifikacijske glave (`include_top=False`), z dodano lastno glavo za binarno
klasifikacijo `live` / `spoof`.

Razlogi za izbiro MobileNetV2:

- majhen in hiter model, primeren za API in morebitno mobilno uporabo,
- že naučen prepoznavati splošne vizualne vzorce na slikah,
- prenosno učenje omogoča dobre rezultate brez učenja celotne CNN od nič,
- podpira izvoz v Keras in TensorFlow Lite format.

Arhitektura modela:

```text
vhodna slika 112×112×3
-> preprocess_input za MobileNetV2
-> MobileNetV2 brez originalnega vrha
-> GlobalAveragePooling2D
-> Dropout(0.3)
-> Dense(1, sigmoid)
-> P(spoof)
```

Model ima skupaj približno **2,26 milijona parametrov**. V prvi fazi se uči samo
dodana glava (učljivih **1.281 parametrov**); preostale uteži MobileNetV2 so
zamrznjene.

## 15. Učenje liveness modela

Učenje je izvedeno v Google Colabu z GPU pospeševanjem; podatki so hranjeni na
Google Drive in v Colabu razpakirani na lokalni disk `/content` (branje več tisoč
majhnih slik neposredno z Drive je počasno).

### 15.1 Prva faza — učenje klasifikacijske glave

Osnovni MobileNetV2 je zamrznjen (`base.trainable = False`), uči se samo glava:

```text
optimizer = Adam(learning_rate=1e-3)
loss = binary_crossentropy
metrics = accuracy, AUC
```

Uporabljena je zgodnja ustavitev (`monitor = val_auc`, `mode = max`,
`patience = 5`, `restore_best_weights = True`): učenje se ustavi, ko se `val_auc`
pet epoh zapored ne izboljša, in obnovi uteži najboljše epohe. Že v prvi fazi je
najvišji `val_auc` ≈ **0,9990**.

### 15.2 Druga faza — fino doučenje

Odtaljenih je zadnjih ~30 plasti osnovne mreže, learning rate znižan na `1e-5`:

```python
base.trainable = True
for layer in base.layers[:-30]:
    layer.trainable = False
```

Nizek learning rate naredi le majhne popravke višjih značilk in ne poruši znanja,
ki ga MobileNetV2 že ima. Po finem doučenju se validacijski rezultati še
izboljšajo:

| Epoha | train accuracy | val accuracy | val AUC | val loss |
|------:|---------------:|-------------:|--------:|---------:|
| 1 | 0,9711 | 0,9925 | 0,9999 | 0,0210 |
| 2 | 0,9911 | 0,9959 | 0,9999 | 0,0134 |
| 3 | 0,9962 | 0,9969 | 1,0000 | 0,0115 |
| 4 | 0,9982 | 0,9979 | 1,0000 | 0,0081 |
| 5 | 0,9992 | 0,9969 | 1,0000 | 0,0088 |
| 6 | 0,9998 | 0,9979 | 1,0000 | 0,0068 |
| 7 | 0,9999 | 0,9979 | 1,0000 | 0,0058 |
| 8 | 1,0000 | 0,9979 | 1,0000 | 0,0057 |
| 9 | 1,0000 | 0,9983 | 1,0000 | 0,0055 |
| 10 | 1,0000 | 0,9983 | 1,0000 | 0,0055 |

## 16. Hiperparametri in optimizacija

| Hiperparameter | Izbrana vrednost | Razlaga |
|----------------|------------------|---------|
| velikost slike | 112×112 | slike so pripravljene v tej velikosti; manjši vhod pomeni hitrejše učenje |
| batch size | 32 | stabilen kompromis med hitrostjo in porabo pomnilnika |
| osnovni model | MobileNetV2 | majhen, hiter, primeren za API in TFLite |
| learning rate, 1. faza | 1e-3 | dovolj velik za hitro učenje nove glave |
| learning rate, fino doučenje | 1e-5 | majhni koraki, da ne pokvarimo pretrained uteži |
| dropout | 0,3 | zmanjševanje overfittinga |
| število odmrznjenih plasti | zadnjih ~30 | prilagoditev višjih značilk naši domeni |
| največ epoh, 1. faza | 30 | dejansko omejeno z EarlyStopping |
| največ epoh, 2. faza | 10 | fino doučenje s počasnimi koraki |
| EarlyStopping patience | 5 | ustavitev ob neizboljševanju `val_auc` |
| class weights | live 2,0536; spoof 0,6609 | popravek za neuravnotežene razrede |

Optimizacija je potekala ročno z opazovanjem validacijskih metrik (predvsem
`val_auc`, `val_loss` in razmerja train/val krivulj). Po učenju je dodatno
optimiziran še **odločitveni prag**, saj model vrača verjetnost `P(spoof)`, ne
neposredno razreda.

## 17. Vrednotenje liveness modela in izbira praga

Za vrednotenje so uporabljeni: **accuracy**, **ROC-AUC** (ločevanje ne glede na
prag), **loss**, **matrika zmot** ter **FAR/FRR** (varnostno pomembni vrsti
napak). Na testni množici po finem doučenju:

```text
ROC-AUC = 0,9968
```

### 17.1 Izbira praga

Prvotni prag po EER na validacijski množici:

```text
THRESHOLD = 0,138   (FAR ≈ 0,004, FRR ≈ 0,007)
```

Ker je nevarnejša napaka, da se spoof sprejme kot live, je preverjenih več pragov.
Model vrača `P(spoof)`, zato **nižji prag pomeni strožji sistem** (hitreje označi
sliko kot spoof):

| Prag | Matrika zmot `[[TN, FP], [FN, TP]]` | Accuracy | live → spoof | spoof → live |
|-----:|-------------------------------------|---------:|-------------:|-------------:|
| 0,20 | `[[1416, 4], [13, 1486]]` | 0,9942 | 0,0028 | 0,0087 |
| 0,15 | `[[1415, 5], [12, 1487]]` | 0,9942 | 0,0035 | 0,0080 |
| 0,14 | `[[1415, 5], [10, 1489]]` | 0,9949 | 0,0035 | 0,0067 |
| 0,12 | `[[1414, 6], [8, 1491]]` | 0,9952 | 0,0042 | 0,0053 |
| 0,10 | `[[1414, 6], [7, 1492]]` | 0,9955 | 0,0042 | 0,0047 |
| 0,08 | `[[1414, 6], [3, 1496]]` | 0,9969 | 0,0042 | 0,0020 |
| 0,05 | `[[1414, 6], [2, 1497]]` | 0,9973 | 0,0042 | 0,0013 |
| 0,03 | `[[1412, 8], [2, 1497]]` | 0,9966 | 0,0056 | 0,0013 |

Za aplikacijo je izbran **`THRESHOLD = 0,05`**. Pri njem se napake `spoof → live`
zmanjšajo na 2 od 1499 spoof slik, napačne zavrnitve živih pa se povečajo le
minimalno — boljši kompromis kot EER prag, ker je nevarnejše sprejeti ponaredek
kot zavrniti uporabnika, ki lahko postopek ponovi.

## 18. Izvoz liveness modela

Artefakti za integracijo:

```text
liveness_model.keras    # za Python/FastAPI servis
liveness_model.tflite   # za morebitno mobilno uporabo
threshold.txt           # izbrani odločitveni prag
```

Pravilo za API:

```text
vhod = 112×112 RGB izrez obraza
izhod = P(spoof)
P(spoof) >= threshold -> spoof, zavrni
P(spoof) <  threshold -> live, nadaljuj
```

Ker je `preprocess_input` že del naučenega modela, API ne izvaja dodatne
normalizacije — uporabi isti obrazni izrez iz `preprocessing.py`, sliko zmanjša na
112×112 in pretvori v RGB.

## 19. Ujemanje obrazov — algoritem, podatki in kalibracija

Cilj ni učenje novega modela, temveč uporaba vnaprej naučenega obraznega
embedding modela in določitev praga podobnosti. Izbran je **ArcFace** s
**kosinusno podobnostjo**:

```text
cosine_similarity >= TAU -> ista oseba
cosine_similarity <  TAU -> različna oseba
```

Pomembno: gre za **cosine similarity** (večja vrednost = bolj podobno), ne za
DeepFace `distance` (manjša vrednost = bolj podobno).

**Podatki:** prag je kalibriran na zbirki **LFW (Labeled Faces in the Wild)** —
pari `match`/`mismatch` (`matchpairsDevTrain/Test.csv`,
`mismatchpairsDevTrain/Test.csv`). Za vsak par je izračunan ArcFace embedding in
kosinusna podobnost; pozitivni razred = ista oseba.

**Kalibracija praga.** Najprej po EER (kjer sta FAR in FRR približno enaka):

```text
TAU (EER) = 0,1391   (FAR = 0,1627, FRR = 0,1655)
```

Na testnih parih je ta prag dal `ROC-AUC = 0,9114`, `accuracy = 0,8370`,
`FAR = 0,1840`, `FRR = 0,1420`. Ker je za identifikacijo nevarneje sprejeti
različni osebi kot isto, so preverjeni strožji pragovi:

| TAU | Accuracy | FAR | FRR | Matrika zmot |
|----:|---------:|----:|----:|--------------|
| 0,10 | 0,780 | 0,306 | 0,134 | `[[347,153],[67,433]]` |
| 0,14 | 0,838 | 0,182 | 0,142 | `[[409,91],[71,429]]` |
| 0,18 | 0,867 | 0,106 | 0,160 | `[[447,53],[80,420]]` |
| 0,20 | 0,880 | 0,070 | 0,170 | `[[465,35],[85,415]]` |
| 0,25 | 0,883 | 0,038 | 0,196 | `[[481,19],[98,402]]` |
| 0,30 | 0,884 | 0,020 | 0,212 | `[[490,10],[106,394]]` |
| 0,35 | 0,873 | 0,014 | 0,240 | `[[493,7],[120,380]]` |
| 0,40 | 0,864 | 0,006 | 0,266 | `[[497,3],[133,367]]` |

Za aplikacijo je izbran **`TAU = 0,20`**, ki zniža FAR z 18,4 % na ~7,0 % (manj
napačnih sprejemov različnih oseb). Cena je višji FRR — sprejemljivo, saj je bolje
uporabnika pozvati k ponovnemu zajemu kot sprejeti napačno osebo.

Izvoz konfiguracije za API (`face_match_config.txt`):

```text
model=ArcFace
score_type=cosine_similarity
threshold=0.2
decision=same_if_score_greater_or_equal
```

**Omejitve in opažanja.** Liveness model doseže zelo dober testni ROC-AUC, a se
realni pogoji lahko razlikujejo od učnih — zato je ključno, da API uporablja isti
`detect_and_crop_face` preprocessing kot učni cevovod. Pri ujemanju obrazov so
rezultati pričakovano slabši, ker primerjava v realnem scenariju vključuje razlike
v osvetlitvi, pozi, kakovosti slike in starosti fotografije na dokumentu; prag
`TAU = 0,20` je zato varnostno usmerjen kompromis z nižjim FAR.

---

# API, OCR in integracija (Edvin)

## 20. API del — pregled

API del je **vezivno tkivo**, ki poveže podatke (§3–§11) in modele (§12–§19) v
delujoč servis ter ga vključi v preostali sistem:

| Naloga | Opis |
|--------|------|
| **FastAPI servis** | REST API, ki sprejme slike in vrne odločitev o identiteti |
| **Cevovod verifikacije** | Orkestracija treh korakov: živost → ujemanje obraza → OCR |
| **OCR dokumenta** | Branje imena, priimka, EMSO in datuma veljavnosti iz dokumenta |
| **Integracija v .NET backend** | Klic cv-identity iz ASP.NET Core, registracija občana |
| **Dockerizacija** | Kontejnerizacija servisa in sestava celotnega sistema |

## 21. FastAPI servis (`app/main.py`)

Servis je napisan v **FastAPI** in teče pod **uvicorn**. Ob zagonu naloži vse
modele v pomnilnik (fail-fast — napaka pri nalaganju zaustavi servis takoj, ne
šele ob prvem klicu).

| Metoda | Pot | Opis |
|--------|-----|------|
| `GET` | `/health` | Preverjanje dosegljivosti (`{"status": "ok"}`) |
| `POST` | `/verify` | Verifikacija identitete (multipart/form-data) |

**Vhod `/verify`:**

- `id_front` — slika sprednje strani dokumenta (JPEG/PNG)
- `selfie_frames` — ena ali več slik obraza (okvirji posnetka)
- `variant` — vrsta dokumenta: `driving_licence` (privzeto) ali `id_card`

Servis sprejme obe obliki polja (`selfie_frames` in `selfie_frames[]`), ker se
obliki razlikujeta med odjemalci (Kotlin multipart vs. spletni odjemalci).

## 22. Cevovod verifikacije (`app/pipeline.py`)

Cevovod je nespremenljiv podatkovni razred (`@dataclass(frozen=True)`) z
zamenljivimi komponentami prek protokolnih vmesnikov. Komponente
(`LivenessPredictor`, `EmbeddingModel`, `DocumentReader`) so Python `Protocol` —
katerikoli objekt s pravilno signaturo deluje brez dedovanja.

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

**Zakaj OCR šele na koncu:** branje dokumenta je najpočasnejši korak. Če obraz ni
zaznan ali je živost dvomljiva, je OCR nepotreben — prihranimo čas in zmanjšamo
površino za napake.

Končna odločitev v `VerificationPipeline`:

```text
liveness_ok = P(spoof) < liveness_threshold        # prag 0,05 (§17)
face_match_ok = cosine_similarity >= match_threshold  # prag 0,20 (§19)
verified = liveness_ok AND face_match_ok AND document_valid
```

**Odgovor pri uspehu:**

```json
{
  "verified": true,
  "first_name": "JANEZ",
  "last_name": "NOVAK",
  "emso": "1010005500426"
}
```

**Odgovor pri neuspehu** vsebuje seznam razlogov: `no_face_in_id`,
`no_face_in_selfie`, `liveness_failed`, `face_mismatch`, `document_ocr_failed`,
`document_expired`, `missing_name`, `missing_surname`, `missing_emso`.

## 23. Ujemanje obrazov v API (`app/face/embed.py`)

Ujemanje je v API izvedeno z **DeepFace/ArcFace** po konfiguraciji iz §19
(`face_match_config.txt`). Razred `FaceEmbedder` za vsak obraz (dokument, selfie)
izračuna embedding in primerja prek **kosinusne podobnosti**; prag je nastavljiv
prek datoteke (`threshold=0.2`). Kalibracija in izbor praga sta opisana v §19.

## 24. OCR dokumenta (`app/ocr/general.py`)

Za sprednje strani dokumentov (vozniška dovoljenja, osebne izkaznice) se izvede
**Tesseract OCR** z lastno predobdelavo slike. Zaženeta se **dve predobdelitveni
strategiji** zaporedoma, rezultati se združijo:

| Prehod | Metoda | Kdaj deluje bolje |
|--------|--------|-------------------|
| **1 (Blackhat)** | Morfološki Blackhat + adaptivni prag | Neenakomerna osvetlitev, guilloche ozadje |
| **2 (Otsu)** | Gaussov filter + Otsu prag | Enakomerna svetloba, visok kontrast |

**Zaznava in poravnava dokumenta:** pred OCR se zazna obris kartice v fotografiji,
izvede **perspektivna transformacija** (štiri točke → pravokotnik) in izrez
besedilnega dela (brez cone s fotografijo).

**PSM strategija po vrsti dokumenta:**

- `driving_licence` — PSM 11 (razpršeni tekst) z grupiranjem žetonov po y-pasovih,
  ker PSM 11 loči oznake polj in vrednosti v ločene bloke kljub vizualni
  poravnavi.
- `id_card` — dvokolonski pristop: levi stolpec z PSM 4 + PSM 6 (PSM 4 ohranja
  strukturo oznak, PSM 6 da boljšo točnost za kratke vrednosti), desni stolpec z
  PSM 6.

**Jezikovna podpora:** Tesseract dobi jezike glede na razpoložljivost
(`hrv+slv+srp_latn+eng` → `slv+hrv+eng` → … → `eng`).

**Zaznana polja:**

| Polje | Opis |
|-------|------|
| Ime / priimek | EU vozniško: polji 1 in 2 (prefix/suffix ujemanje); osebna: oznaka `Priimek/Surname` ali strukturna hevristika |
| EMSO | Regex za točno 13 zaporednih številk |
| Datum veljavnosti | Regex za `DD.MM.YYYY`, `YYYY-MM-DD`; vrne **največji datum** (verjetno datum poteka) |
| Številka dokumenta | Regex za alfanumerični format (`ABC123456`) |

## 25. Integracija v .NET backend

Mobilna aplikacija ne kliče cv-identity **neposredno** — zahtevki gredo prek
**ASP.NET Core backenda**, ki je edinstvena vstopna točka.

**Arhitektura (Clean Architecture):**

```
ePrevzem.Application
└── Common/Abstractions/ICvIdentityClient.cs   ← port (vmesnik)
└── Identity/VerifyDocumentAndRegister/        ← use case

ePrevzem.Infrastructure
└── Identity/CvIdentityClient.cs               ← adapter (HTTP)
└── Identity/IdentityOptions.cs                ← konfiguracija BaseUrl
```

**`ICvIdentityClient`** definira pogodbo v aplikacijski plasti — brez odvisnosti
od HTTP. Parametri: bajti slike dokumenta, seznam okvirjev selfija (`SelfieFrame`
vrednostni objekti), vrsta dokumenta.

**`CvIdentityClient`** v infrastrukturni plasti sestavi `MultipartFormDataContent`
in pošlje POST na `/verify`. Ločeno obravnava omrežne napake
(`CvIdentityUnavailableException`) in neuspešno verifikacijo
(`DocumentVerificationFailedException` z razlogi).

**`VerifyDocumentAndRegisterCommand`** je MediatR use case, ki:

1. pokliče `ICvIdentityClient.VerifyAsync`,
2. ob potrditvi ustvari ali poišče `CitizenUser` po EMSO-ju,
3. izda `CitizenActivationCode` (veljavnost 24 ur),
4. vrne kodo za nadaljevanje registracije naprave.

**Konfiguracija** (`appsettings.json` / env var):

```
CvIdentity__BaseUrl=http://cv-identity:8000
```

## 26. Dockerizacija

**`cv-identity/Dockerfile`** temelji na `python:3.12-slim` in namesti sistemske
odvisnosti:

- `libgl1`, `libglib2.0-0` — OpenCV
- `libgomp1` — paralelizem (NumPy/TF)
- `tesseract-ocr` + jezikovni paketi `eng`, `hrv`, `slv`

Python odvisnosti se namestijo iz `requirements.txt`, koda se kopira v `/app`,
servis posluša na portu `8000` prek `uvicorn`. Poti modelov so nastavljive prek
`CV_IDENTITY_MODELS_DIR` — modeli se montirajo kot zunanji volume, ne pečejo v
sliko.

**`docker-compose.yml`** zažene celoten sistem z eno datoteko. Storitev
`cv-identity` je ločen servis, backend pa ga doseže prek Docker notranjega DNS-a
(`http://cv-identity:8000`). Modeli se montirajo kot read-only volume
(`./cv-identity/app/models:/app/models:ro`), ker so veliki in se ne commitajo v
repozitorij.

**Ključni commiti** (veje `users/edvin/*`, `PRVZM-87*`):

- `PRVZM-87 feat(cv-identity): implement OCR and face matching pipeline` — začetna implementacija cevovoda
- `PRVZM-87 refactor(cv-identity): drop id_back, add EMSO/name extraction, simplify pipeline response` — en dokument namesto dveh, dodano branje EMSO
- `PRVZM-87 feat(identity): containerize cv identity` — Dockerfile in docker-compose
- `PRVZM-86 fix(identity): make OCR robust to phone photos and fix JSON mapping` — robustnost OCR za telefonske slike
- `route cv-identity through .NET backend, remove direct mobile→python calls` — arhitekturni popravek integracije

---

## Datotečna struktura servisa cv-identity

```
cv-identity/
├── app/
│   ├── main.py                 # FastAPI servis (/health, /verify)
│   ├── pipeline.py             # cevovod verifikacije (živost → ujemanje → OCR)
│   ├── preprocessing.py        # predobdelava (deljena: train + inferenca)
│   ├── face/embed.py           # ArcFace embeddingi + kosinusna podobnost
│   ├── ocr/general.py          # Tesseract OCR + predobdelava dokumenta
│   └── models/                 # liveness_model.keras, threshold.txt, face_match_config.txt
├── training/
│   ├── augmentation.py         # lastna augmentacija
│   └── split.py                # razdelitev po identiteti
├── scripts/
│   ├── capture.py              # zajem s kamere
│   └── build_dataset.py        # raw + NUAA -> spliti
├── tests/                      # testi (pytest) + golden slike
├── dataset/README.md           # datasheet
└── Dockerfile                  # python:3.12-slim + Tesseract
```

---

## Viri

Za vsako ključno temo projekta navajamo vire:

- **Temelji nevronskih mrež in vzvratno razširjanje napake.** Bratanović, A.
  (2009). *Implementacija algoritma vzvratnega razširjanja napake na grafičnem
  procesorju.* Diplomsko delo, Univerza v Mariboru, FERI.
- **Prenosno učenje in fino doučenje.** TensorFlow. *Transfer learning and
  fine-tuning* (uradni vodnik).
  https://www.tensorflow.org/tutorials/images/transfer_learning
- **Arhitektura liveness modela (CNN).** Sandler, M., Howard, A., Zhu, M.,
  Zhmoginov, A., & Chen, L.-C. (2018). *MobileNetV2: Inverted Residuals and Linear
  Bottlenecks.* CVPR. https://arxiv.org/abs/1801.04381
- **Preverjanje živosti / anti-spoofing (učna zbirka).** NUAA Imposter Database.
  https://parnec.nuaa.edu.cn/_upload/tpl/02/db/731/template731/pages/xtan/NUAAImposterDB_download.html
- **Zaznava obraza in ključnih točk.** MediaPipe (Google) — knjižnica za zaznavo
  obraza in ključnih točk. https://developers.google.com/mediapipe
- **Ujemanje obrazov (obrazni embeddingi).** Deng, J., Guo, J., Xue, N., &
  Zafeiriou, S. (2019). *ArcFace: Additive Angular Margin Loss for Deep Face
  Recognition.* CVPR. https://arxiv.org/abs/1801.07698
- **Kalibracija praga za ujemanje (zbirka parov).** Huang, G. B., Ramesh, M.,
  Berg, T., & Learned-Miller, E. (2007). *Labeled Faces in the Wild.* UMass TR
  07-49. http://vis-www.cs.umass.edu/lfw/
