"""Leakage-free dataset splitting: partition by identity, not by image."""
import numpy as np

Record = tuple[str, str]  # (person, path)


def split_by_identity(
    records: list[Record],
    ratios: tuple[float, float, float] = (0.6, 0.2, 0.2),
    seed: int = 0,
) -> tuple[list[Record], list[Record], list[Record]]:
    """Split records into (train, val, test) so no person spans two splits.

    People are shuffled deterministically (by `seed`) and assigned whole to a
    split according to `ratios`, which must sum to 1.0.
    """
    if abs(sum(ratios) - 1.0) > 1e-6:
        raise ValueError(f"ratios must sum to 1.0, got {ratios}")

    people = sorted({person for person, _ in records})
    rng = np.random.default_rng(seed)
    rng.shuffle(people)

    n = len(people)
    n_train = int(n * ratios[0])
    n_val = int(n * ratios[1])
    train_people = set(people[:n_train])
    val_people = set(people[n_train:n_train + n_val])

    train, val, test = [], [], []
    for person, path in records:
        if person in train_people:
            train.append((person, path))
        elif person in val_people:
            val.append((person, path))
        else:
            test.append((person, path))
    return train, val, test
