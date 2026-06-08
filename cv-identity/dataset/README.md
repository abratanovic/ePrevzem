# Datasheet podatkovne zbirke

## Razporeditev map

```
dataset/
├── raw/                     # originalni zajemi (git-ignored)
│   ├── live/<oseba>/        # živi obrazi, več kotov
│   └── spoof/<oseba>/       # napadi: natisnjena fotografija + zaslon
└── splits/{train,val,test}/ # razdelitev brez prelivanja identitet (git-ignored)
```

`build_dataset.py` vsako sliko predobdela **sproti** (zaznava + poravnava +
sprememba velikosti) med pisanjem v `splits/` — ločene faze `processed/` ni, zato
se surove slike nikoli ne podvajajo na disk.

## Razredi

| Razred | Pomen                              | Uporaba                  |
|--------|------------------------------------|--------------------------|
| live   | Živ obraz pred kamero              | Pozitivni primer liveness CNN |
| spoof  | Natisnjena fotografija ali zaslon  | Negativni primer liveness CNN |

## Protokol zajema

### Live
- ~30–50 posnetkov na osebo pri **zmernih** kotih in osvetlitvi.
- Večina naj bo blizu frontalni (±30°): nekaj levo/desno (±15–30°) in malo
  gor/dol za naravne poze pri držanju telefona. **Izogibajte se ekstremnim
  profilom (>45°)** — MediaPipe jih pogosto ne zazna in `build_dataset.py` tako
  sliko tiho preskoči (ni obraza → izpuščeno). Raje variirajte osvetlitev/razdaljo
  kot kot.

### Spoof
- "Spoof vzorec" je vsak **posnetek**, ne vsak izvorni artefakt: 2–3 natisnjene
  fotografije + 2–3 zaslonske, vsako znova poslikano iz več kotov/razdalj in
  osvetlitev, da nastane dovolj posnetkov (~30–50 na osebo).
- Variirajte **pogoje zajema**, da se model nauči pravih znakov napada (odsev,
  moiré, robovi papirja): nagnite tisk, spremenite svetlost zaslona, se
  približajte/oddaljite.
- Uporabite **različne medije** (tisk + zaslon telefona + monitor, mat + sijajen
  papir), da se model ne navadi ene naprave kot "spoof".
- Znova poslikajte **iste kote**, kot ste jih uporabili za žive posnetke iste
  osebe, sicer se model nauči "ta poza = spoof" namesto pravih znakov ponaredka.

### Naprava in pogoji (oba razreda)
- `live` in `spoof` zajemite na **isti napravi**, v **podobni osvetlitvi in
  ozadju** — sicer se model nauči bližnjice (npr. svetlost ali senzorski šum)
  namesto živosti.
- Idealno zajemajte z **isto vrsto naprave kot za demo** (telefon), da zmanjšate
  razliko med učenjem in uporabo (domain gap). `scripts/capture.py` (spletna
  kamera na prenosniku) je primeren za hiter test cevovoda, a so prave učne slike
  najboljše posnete s telefonom in skopirane v `raw/<razred>/<oseba>/`.

## Javne zbirke (dopolnilno)

Velike javne zbirke hranimo **izven repozitorija** (nikoli commitane) pod:

```
C:\PROJEKTI\datasets\
├── nuaa\raw\
│   ├── ClientRaw\<id>\*.jpg     # živi obrazi    -> live  (5105 slik, 15 oseb)
│   └── ImposterRaw\<id>\*.jpg   # foto napadi    -> spoof (7509 slik, 15 oseb)
└── lfw\
    ├── lfw-deepfunneled\lfw-deepfunneled\<Ime>\<Ime>_NNNN.jpg
    ├── matchpairsDevTrain.csv / matchpairsDevTest.csv      # pozitivni pari (ista oseba)
    ├── mismatchpairsDevTrain.csv / mismatchpairsDevTest.csv# negativni pari (različni osebi)
    └── pairs.csv                                           # vseh 6000 parov skupaj
```

- **NUAA** — liveness (anti-spoofing). Izbrana namesto CelebA-Spoof (~80 GB)
  zaradi obvladljive velikosti. `ClientRaw` = live, `ImposterRaw` = spoof. Id
  osebe je v obeh mapah enak, zato se vsaka oseba preslika v **eno** identiteto
  (`nuaa_<id>`) in nikoli ne prelije med razdelitve.
- **LFW (deepfunneled)** — samo za **kalibracijo praga** ujemanja obrazov (ni del
  liveness gradnje). Pare uporabite neposredno iz CSV-jev:
  - vrstice match: `name, imagenum1, imagenum2` (ista oseba)
  - vrstice mismatch: `name1, imagenum1, name2, imagenum2` (različni osebi)
  - pot slike: `lfw-deepfunneled\lfw-deepfunneled\<name>\<name>_{imagenum:04d}.jpg`

NUAA slike so že izrezani obrazi, zato gradnja zanje preskoči zaznavo MediaPipe;
ekipne slike gredo najprej skozi zaznavo + poravnavo.

## Gradnja zbirke

`scripts/build_dataset.py` pretvori surove podatke v za model pripravljene
razdelitve z lastnimi funkcijami za predobdelavo / augmentacijo / razdelitev:

```bash
python scripts/build_dataset.py --raw dataset/raw \
    --nuaa C:/PROJEKTI/datasets/nuaa/raw --out dataset --augment-count 4
```

Izhod: `dataset/splits/{train,val,test}/{live,spoof}/<oseba>__<ime>.jpg`. Slike v
train dobijo `--augment-count` dodatnih augmentiranih variant; val/test se ne
augmentirata. LFW modelni korak uporabi neposredno prek svojih CSV parov.

## Politika razdelitve

Razdelitve ustvari `training/split.py` in deli **po osebi**: ista identiteta se ne
pojavi v več kot eni razdelitvi (prepreči prelite, napihnjene metrike). Privzeto
razmerje train/val/test = 0,6 / 0,2 / 0,2.

> Opomba (majhna ekipa): z le nekaj ekipnimi identitetami čista 3-smerna
> razdelitev *ekipnih* podatkov ni smiselna. Liveness CNN se uči pretežno na NUAA;
> ekipne podatke je najbolje obdržati kot ločeno realno testno množico.
