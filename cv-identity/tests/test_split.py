from training.split import split_by_identity


def _records():
    # (person, path) — 3 people, 2 images each.
    people = ["adnan", "edvin", "emir"]
    return [(p, f"{p}_{i}.jpg") for p in people for i in range(2)]


def test_split_partitions_all_records():
    train, val, test = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=1)
    total = len(train) + len(val) + len(test)
    assert total == len(_records())


def test_split_has_no_identity_overlap():
    train, val, test = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=1)
    people = lambda rs: {p for p, _ in rs}
    assert people(train).isdisjoint(people(val))
    assert people(train).isdisjoint(people(test))
    assert people(val).isdisjoint(people(test))


def test_split_is_deterministic_with_seed():
    a = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=42)
    b = split_by_identity(_records(), ratios=(0.6, 0.2, 0.2), seed=42)
    assert a == b
