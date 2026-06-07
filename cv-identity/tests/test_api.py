import io

import pytest


def _jpeg_bytes():
    Image = pytest.importorskip("PIL.Image")
    image = Image.new("RGB", (8, 8), color=(255, 255, 255))
    out = io.BytesIO()
    image.save(out, format="JPEG")
    return out.getvalue()


def test_verify_route_accepts_spec_multipart_contract(monkeypatch):
    pytest.importorskip("fastapi")
    pytest.importorskip("httpx")
    from fastapi.testclient import TestClient

    from app.main import app

    class FakePipeline:
        def verify(self, id_front_bgr, selfie_frames_bgr, variant="driving_licence"):
            assert id_front_bgr.shape[2] == 3
            assert len(selfie_frames_bgr) == 2
            return {
                "verified": True,
                "first_name": "JANEZ",
                "last_name": "NOVAK",
                "emso": "1010005500426",
            }

    monkeypatch.setattr(app.state, "pipeline", FakePipeline(), raising=False)
    client = TestClient(app)
    img = _jpeg_bytes()

    response = client.post(
        "/verify",
        files=[
            ("id_front", ("front.jpg", img, "image/jpeg")),
            ("selfie_frames", ("selfie1.jpg", img, "image/jpeg")),
            ("selfie_frames", ("selfie2.jpg", img, "image/jpeg")),
        ],
    )

    assert response.status_code == 200
    assert response.json()["verified"] is True
