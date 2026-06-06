import numpy as np
import pytest


@pytest.fixture
def rgb_image():
    """A small deterministic 3-channel image (H=40, W=60, BGR uint8)."""
    rng = np.random.default_rng(seed=42)
    return rng.integers(0, 256, size=(40, 60, 3), dtype=np.uint8)


@pytest.fixture
def rng():
    """A seeded NumPy generator so augmentation tests are deterministic."""
    return np.random.default_rng(seed=123)
