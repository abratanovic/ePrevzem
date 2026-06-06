import numpy as np
from training.augmentation import (
    rotate, flip_horizontal, scale, adjust_brightness, add_gaussian_noise,
    augment_image, gaussian_blur, add_jpeg_artifacts,
)


def test_rotate_preserves_shape(rgb_image):
    out = rotate(rgb_image, angle=15)
    assert out.shape == rgb_image.shape


def test_flip_horizontal_reverses_columns(rgb_image):
    out = flip_horizontal(rgb_image)
    assert np.array_equal(out, rgb_image[:, ::-1])


def test_scale_then_fit_preserves_shape(rgb_image):
    out = scale(rgb_image, factor=1.2)
    assert out.shape == rgb_image.shape


def test_adjust_brightness_increases_mean(rgb_image):
    brighter = adjust_brightness(rgb_image, delta=40)
    assert brighter.mean() > rgb_image.mean()
    assert brighter.max() <= 255


def test_add_gaussian_noise_is_deterministic_with_seed(rgb_image, rng):
    a = add_gaussian_noise(rgb_image, sigma=10, rng=rng)
    rng2 = np.random.default_rng(seed=123)
    b = add_gaussian_noise(rgb_image, sigma=10, rng=rng2)
    assert np.array_equal(a, b)
    assert a.shape == rgb_image.shape


def test_augment_image_returns_requested_count(rgb_image, rng):
    variants = augment_image(rgb_image, count=5, rng=rng)
    assert len(variants) == 5
    assert all(v.shape == rgb_image.shape for v in variants)


def test_augment_image_is_deterministic_with_seed(rgb_image):
    a = augment_image(rgb_image, count=3, rng=np.random.default_rng(7))
    b = augment_image(rgb_image, count=3, rng=np.random.default_rng(7))
    assert all(np.array_equal(x, y) for x, y in zip(a, b))


def test_gaussian_blur_preserves_shape(rgb_image):
    out = gaussian_blur(rgb_image, ksize=5)
    assert out.shape == rgb_image.shape
    assert out.dtype == np.uint8


def test_add_jpeg_artifacts_preserves_shape(rgb_image):
    out = add_jpeg_artifacts(rgb_image, quality=30)
    assert out.shape == rgb_image.shape
    assert out.dtype == np.uint8
