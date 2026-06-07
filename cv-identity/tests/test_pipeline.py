from datetime import date, timedelta

import numpy as np

from app.ocr.mrz import DocumentInfo
from app.pipeline import VerificationPipeline


class FakeLiveness:
    def __init__(self, scores):
        self.scores = scores

    def predict_spoof_probability(self, face_bgr):
        marker = int(face_bgr[0, 0, 0])
        return self.scores[marker]


class FakeEmbedder:
    def __init__(self):
        self.faces = []

    def embedding(self, face_bgr):
        self.faces.append(face_bgr)
        marker = int(face_bgr[0, 0, 0])
        if marker in {1, 2}:
            return np.array([1.0, 0.0], dtype=np.float32)
        return np.array([0.0, 1.0], dtype=np.float32)


class FakeReader:
    def __init__(self, document):
        self.document = document

    def extract(self, image_bgr):
        return self.document


def _img(marker):
    return np.full((4, 4, 3), marker, dtype=np.uint8)


def _document(valid=True):
    valid_until = date.today() + timedelta(days=10)
    if not valid:
        valid_until = date.today() - timedelta(days=1)
    return DocumentInfo(
        name="JANEZ",
        surname="NOVAK",
        document_number="ABC123456",
        valid_until=valid_until,
    )


def test_verify_passes_when_document_liveness_and_match_pass(monkeypatch):
    monkeypatch.setattr("app.pipeline.detect_and_crop_face", lambda image: image)
    embedder = FakeEmbedder()
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({2: 0.01, 9: 0.08}),
        face_embedder=embedder,
        document_reader=FakeReader(_document()),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(9), _img(2)])

    assert result["verified"] is True
    assert result["liveness_score_spoof"] == 0.01
    assert result["face_match_ok"] is True
    assert result["reasons"] == []
    assert int(embedder.faces[1][0, 0, 0]) == 2


def test_verify_rejects_when_ocr_fails(monkeypatch):
    monkeypatch.setattr("app.pipeline.detect_and_crop_face", lambda image: image)
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({2: 0.01}),
        face_embedder=FakeEmbedder(),
        document_reader=FakeReader(None),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(2)])

    assert result["verified"] is False
    assert "document_ocr_failed" in result["reasons"]


def test_verify_rejects_expired_document(monkeypatch):
    monkeypatch.setattr("app.pipeline.detect_and_crop_face", lambda image: image)
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({2: 0.01}),
        face_embedder=FakeEmbedder(),
        document_reader=FakeReader(_document(valid=False)),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(2)])

    assert result["verified"] is False
    assert "document_expired" in result["reasons"]


def test_verify_rejects_missing_selfie_face(monkeypatch):
    def detect(image):
        return image if int(image[0, 0, 0]) == 1 else None

    monkeypatch.setattr("app.pipeline.detect_and_crop_face", detect)
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({}),
        face_embedder=FakeEmbedder(),
        document_reader=FakeReader(_document()),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(2)])

    assert result["verified"] is False
    assert "no_face_in_selfie" in result["reasons"]


def test_verify_rejects_liveness_failure(monkeypatch):
    monkeypatch.setattr("app.pipeline.detect_and_crop_face", lambda image: image)
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({2: 0.09}),
        face_embedder=FakeEmbedder(),
        document_reader=FakeReader(_document()),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(2)])

    assert result["verified"] is False
    assert "liveness_failed" in result["reasons"]


def test_verify_rejects_face_mismatch(monkeypatch):
    monkeypatch.setattr("app.pipeline.detect_and_crop_face", lambda image: image)
    pipeline = VerificationPipeline(
        liveness_model=FakeLiveness({9: 0.01}),
        face_embedder=FakeEmbedder(),
        document_reader=FakeReader(_document()),
        liveness_threshold=0.05,
        match_threshold=0.2,
    )

    result = pipeline.verify(_img(1), _img(7), [_img(9)])

    assert result["verified"] is False
    assert "face_mismatch" in result["reasons"]
