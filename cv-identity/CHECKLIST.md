# Član 1 — Definition of Done (data pipeline)

Hand off to Član 2 only when every box below is checked.

## 1. Code works (automatic)

- [ ] `cd cv-identity && pytest -v` → all tests green (preprocessing, augmentation, split, golden-image integration).
- [ ] `git status` → clean tree, everything committed (each member's contribution must show in git history).

## 2. Dataset is actually collected & correct (manual)

Tests check *code*, not *data*. Verify by hand:

- [ ] All classes captured for every team member: `live/`, `spoof/`, `id_cards/` (~30–50 live images per person + matching spoof).
- [ ] Spoof truly matches each person — printed-photo / screen-replay of *their* face, not random images.
- [ ] **No data leakage** — after running `split.py`, no person appears in more than one split:
  ```python
  people = lambda rs: {p for p, _ in rs}
  assert people(train).isdisjoint(people(test))
  assert people(train).isdisjoint(people(val))
  assert people(val).isdisjoint(people(test))
  ```
- [ ] Preprocessing output looks sane — open a few images from `processed/`: face cropped, eyes level (aligned), correct size.
- [ ] Augmentation is visibly diverse but not destructive — save 5–10 variants of one image and eyeball them (rotation, brightness, blur present; face still recognizable).

## 3. Handoff interface is clear (manual)

Član 2 must be able to use your work without you:

- [ ] `dataset/README.md` documents folder layout, classes, and the split-by-identity rule.
- [ ] Splits exist (`splits/train|val|test/`), ready to load into the model.
- [ ] `preprocessing` and `split` functions are documented and importable (same preprocessing used for train and inference → train/serve consistency).

## 4. Quality smell test (manual)

The most common silent problem is data, not code:

- [ ] Live and spoof captured in **similar conditions** (lighting, camera, background) — otherwise the model learns shortcuts (e.g. brightness) instead of real spoof cues.
- [ ] At least a few people have **both an ID card and a selfie** (needed for the end-to-end ID ↔ selfie demo).

---

When all boxes are checked, the data pipeline is ready for Član 2 (model training).
