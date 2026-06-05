import numpy as np
from app.preprocessing import (
    resize_image, to_grayscale, normalize_pixels, denoise, to_colorspace,
    crop_to_bbox, align_by_eyes,
)


def test_resize_image_returns_target_shape(rgb_image):
    out = resize_image(rgb_image, (32, 32))
    assert out.shape == (32, 32, 3)
    assert out.dtype == np.uint8


def test_to_grayscale_drops_channels(rgb_image):
    out = to_grayscale(rgb_image)
    assert out.shape == (40, 60)


def test_normalize_pixels_scales_to_unit_range(rgb_image):
    out = normalize_pixels(rgb_image)
    assert out.dtype == np.float32
    assert out.min() >= 0.0 and out.max() <= 1.0


def test_denoise_preserves_shape(rgb_image):
    out = denoise(rgb_image)
    assert out.shape == rgb_image.shape
    assert out.dtype == np.uint8


def test_to_colorspace_rgb_keeps_three_channels(rgb_image):
    out = to_colorspace(rgb_image, "RGB")
    assert out.shape == rgb_image.shape


def test_crop_to_bbox_returns_region(rgb_image):
    out = crop_to_bbox(rgb_image, (10, 5, 20, 15))
    assert out.shape == (15, 20, 3)


def test_crop_to_bbox_clamps_to_image_bounds(rgb_image):
    out = crop_to_bbox(rgb_image, (50, 0, 100, 10))
    assert out.shape[1] == 10  # 60 - 50


def test_align_by_eyes_preserves_shape(rgb_image):
    out = align_by_eyes(rgb_image, (15.0, 10.0), (45.0, 20.0))
    assert out.shape == rgb_image.shape


def test_align_by_eyes_level_eyes_is_noop(rgb_image):
    out = align_by_eyes(rgb_image, (15.0, 10.0), (45.0, 10.0))
    assert np.array_equal(out, rgb_image)
