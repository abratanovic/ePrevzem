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
        selfie_frames_bgr: list[np.ndarray],
        variant: str = "driving_licence",
    ) -> dict:
        reasons: list[str] = []
        match_score: float | None = None
        p_spoof: float | None = None

        # Face checks first — skip OCR entirely if they fail.
        id_face = detect_and_crop_face(id_front_bgr)
        if id_face is None:
            reasons.append("no_face_in_id")

        selfie_results = self._rank_selfie_frames(selfie_frames_bgr)
        if not selfie_results:
            reasons.append("no_face_in_selfie")
            best_selfie_face = None
        else:
            p_spoof, best_selfie_face = selfie_results[0]
            if p_spoof >= self.liveness_threshold:
                reasons.append("liveness_failed")

        if id_face is not None and best_selfie_face is not None:
            id_emb = self.face_embedder.embedding(id_face)
            selfie_emb = self.face_embedder.embedding(best_selfie_face)
            match_score = cosine_similarity(id_emb, selfie_emb)
            if match_score < self.match_threshold:
                reasons.append("face_mismatch")

        if reasons:
            return {
                "verified": False,
                "reasons": reasons,
                "match_score": match_score,
                "liveness_score": p_spoof,
                "liveness_threshold": self.liveness_threshold,
            }

        # OCR only after face checks pass.
        document = self.document_reader.extract(id_front_bgr, variant=variant)
        if document is None:
            return {"verified": False, "reasons": ["document_ocr_failed"]}

        if not document.document_valid:
            return {"verified": False, "reasons": ["document_expired"]}

        missing = [
            field
            for field, value in [
                ("missing_name", document.name),
                ("missing_surname", document.surname),
                ("missing_emso", document.emso),
            ]
            if not value
        ]
        if missing:
            res = {"verified": False, "reasons": missing}
            print(f"[DEBUG] Verification failed. Reasons: {missing}")
            return res

        res = {
            "verified": True,
            "first_name": document.name,
            "firstName": document.name,
            "last_name": document.surname,
            "lastName": document.surname,
            "emso": document.emso,
            "EMSO": document.emso,
        }
        import json
        print(f"[DEBUG] Verification success! Final JSON: {json.dumps(res, ensure_ascii=False)}")
        return res

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
