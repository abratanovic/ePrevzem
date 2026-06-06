import numpy as np

from scripts.build_dataset import (
    Record, iter_team_records, iter_nuaa_records, assign_splits, prepare_image,
)


def _touch(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(b"x")


def test_iter_team_records_maps_class_and_person(tmp_path):
    _touch(tmp_path / "live" / "alen" / "a1.jpg")
    _touch(tmp_path / "spoof" / "alen" / "s1.jpg")
    _touch(tmp_path / "id_cards" / "alen" / "id.jpg")  # not a liveness class
    recs = iter_team_records(tmp_path)
    pairs = {(r.label, r.person) for r in recs}
    assert ("live", "alen") in pairs
    assert ("spoof", "alen") in pairs
    assert all(r.label in {"live", "spoof"} for r in recs)
    assert all(r.source == "team" for r in recs)


def test_iter_nuaa_records_maps_client_live_imposter_spoof(tmp_path):
    _touch(tmp_path / "ClientRaw" / "0001" / "x.jpg")
    _touch(tmp_path / "ImposterRaw" / "0001" / "y.jpg")
    recs = iter_nuaa_records(tmp_path)
    pairs = {(r.label, r.person) for r in recs}
    assert ("live", "nuaa_0001") in pairs
    assert ("spoof", "nuaa_0001") in pairs
    # one subject's live + spoof share a single person id (prevents leakage)
    assert {r.person for r in recs} == {"nuaa_0001"}
    assert all(r.source == "nuaa" for r in recs)


def test_assign_splits_no_identity_overlap_and_keeps_labels():
    recs = []
    for i in range(10):
        p = f"p{i}"
        recs.append(Record(p, "live", f"{p}_l.jpg", "nuaa"))
        recs.append(Record(p, "spoof", f"{p}_s.jpg", "nuaa"))
    splits = assign_splits(recs, ratios=(0.6, 0.2, 0.2), seed=1)
    persons = lambda s: {r.person for r in splits[s]}
    assert persons("train").isdisjoint(persons("test"))
    assert persons("train").isdisjoint(persons("val"))
    assert persons("val").isdisjoint(persons("test"))
    assert sum(len(v) for v in splits.values()) == len(recs)
    assert all(r.label in {"live", "spoof"}
               for s in splits for r in splits[s])


def test_prepare_image_without_detection_resizes(rgb_image):
    out = prepare_image(rgb_image, size=(112, 112), detect=False)
    assert out.shape == (112, 112, 3)
    assert out.dtype == np.uint8
