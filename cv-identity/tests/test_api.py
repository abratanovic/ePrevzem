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
        def verify(self, id_front_bgr, id_back_bgr, selfie_frames_bgr):
            assert id_front_bgr.shape[2] == 3
            assert id_back_bgr.shape[2] == 3
            assert len(selfie_frames_bgr) == 2
            return {
                "verified": True,
                "name": "JANEZ",
                "surname": "NOVAK",
                "document_number": "ABC123456",
                "valid_until": "2030-01-01",
                "document_valid": True,
                "liveness_ok": True,
                "liveness_score_spoof": 0.01,
                "liveness_threshold": 0.05,
                "face_match_ok": True,
                "match_score": 0.31,
                "match_threshold": 0.2,
                "reasons": [],
            }

    monkeypatch.setattr(app.state, "pipeline", FakePipeline(), raising=False)
    client = TestClient(app)
    img = _jpeg_bytes()

    response = client.post(
        "/verify",
        files=[
            ("id_front", ("front.jpg", img, "image/jpeg")),
            ("id_back", ("back.jpg", img, "image/jpeg")),
            ("selfie_frames", ("selfie1.jpg", img, "image/jpeg")),
            ("selfie_frames", ("selfie2.jpg", img, "image/jpeg")),
        ],
    )

    assert response.status_code == 200
    assert response.json()["verified"] is True
