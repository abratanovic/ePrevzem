"""Identity verification pipeline orchestration."""
from dataclasses import dataclass
from datetime import date
from typing import Protocol

import numpy as np

from app.face.embed import cosine_similarity
from app.ocr.mrz import DocumentInfo
from app.preprocessing import detect_and_crop_face


class LivenessPredictor(Protocol):
    def predict_spoof_probability(self, face_bgr: np.ndarray) -> float:
        ...


class EmbeddingModel(Protocol):
    def embedding(self, face_bgr: np.ndarray) -> np.ndarray:
        ...


class DocumentReader(Protocol):
    def extract(self, image_bgr: np.ndarray, variant: str = "driving_licence") -> DocumentInfo | None:
        ...


@dataclass(frozen=True)
class VerificationPipeline:
    liveness_model: LivenessPredictor
    face_embedder: EmbeddingModel
    document_reader: DocumentReader
    liveness_threshold: float
    match_threshold: float

    def verify(
        self,
        id_front_bgr: np.ndarray,
        id_back_bgr: np.ndarray,
        selfie_frames_bgr: list[np.ndarray],
        variant: str = "driving_licence",
    ) -> dict:
        reasons: list[str] = []

        document = self.document_reader.extract(id_front_bgr, variant=variant)
        if document is None:
            reasons.append("document_ocr_failed")
            document_valid = False
        else:
            document_valid = document.document_valid
            if not document_valid:
                reasons.append("document_expired")

        id_face = detect_and_crop_face(id_front_bgr)
        if id_face is None:
            reasons.append("no_face_in_id")

        selfie_results = self._rank_selfie_frames(selfie_frames_bgr)
        if not selfie_results:
            reasons.append("no_face_in_selfie")
            best_selfie_face = None
            p_spoof = None
            liveness_ok = False
        else:
            p_spoof, best_selfie_face = selfie_results[0]
            liveness_ok = p_spoof < self.liveness_threshold
            if not liveness_ok:
                reasons.append("liveness_failed")

        if id_face is not None and best_selfie_face is not None:
            id_emb = self.face_embedder.embedding(id_face)
            selfie_emb = self.face_embedder.embedding(best_selfie_face)
            match_score = cosine_similarity(id_emb, selfie_emb)
            face_match_ok = match_score >= self.match_threshold
            if not face_match_ok:
                reasons.append("face_mismatch")
        else:
            match_score = None
            face_match_ok = False

        verified = document_valid and liveness_ok and face_match_ok
        return {
            "verified": verified,
            "name": document.name if document else None,
            "surname": document.surname if document else None,
            "document_number": document.document_number if document else None,
            "valid_until": document.valid_until.isoformat()
            if document and document.valid_until
            else None,
            "document_valid": document_valid,
            "liveness_ok": liveness_ok,
            "liveness_score_spoof": p_spoof,
            "liveness_threshold": self.liveness_threshold,
            "face_match_ok": face_match_ok,
            "match_score": match_score,
            "match_threshold": self.match_threshold,
            "reasons": reasons,
            "raw_ocr_fields": list(document.raw_ocr_fields) if document else [],
        }

    def _rank_selfie_frames(
        self,
        selfie_frames_bgr: list[np.ndarray],
    ) -> list[tuple[float, np.ndarray]]:
        results: list[tuple[float, np.ndarray]] = []
        for frame in selfie_frames_bgr:
            face = detect_and_crop_face(frame)
            if face is None:
                continue
            p_spoof = self.liveness_model.predict_spoof_probability(face)
            results.append((p_spoof, face))
        return sorted(results, key=lambda item: item[0])


class MrzDocumentReader:
    def extract(self, image_bgr: np.ndarray) -> DocumentInfo | None:
        from app.ocr.mrz import extract_document_info

        return extract_document_info(image_bgr)


class TesseractDocumentReader:
    def extract(self, image_bgr: np.ndarray, variant: str = "driving_licence") -> DocumentInfo | None:
        from app.ocr.general import extract_document_info_general

        return extract_document_info_general(image_bgr, variant=variant)
