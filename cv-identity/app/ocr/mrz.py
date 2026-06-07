"""MRZ extraction and parsing for identity documents."""
from dataclasses import dataclass, field
from datetime import date
import re
import tempfile
from pathlib import Path

import cv2
import numpy as np


@dataclass(frozen=True)
class DocumentInfo:
    name: str | None
    surname: str | None
    document_number: str | None
    valid_until: date | None
    raw_ocr_fields: tuple[str, ...] = field(default_factory=tuple)

    @property
    def document_valid(self) -> bool:
        if self.valid_until is None:
            return True  # no expiry parseable — assume not expired
        return self.valid_until >= date.today()


def extract_document_info(image_bgr: np.ndarray) -> DocumentInfo | None:
    """Extract document fields from the ID back image using MRZ OCR."""
    mrz = _read_mrz(image_bgr)
    if mrz is None:
        return None
    info = _document_info_from_passporteye_mrz(mrz)
    if info is not None:
        return info
    raw_text = mrz.to_dict().get("raw_text") if hasattr(mrz, "to_dict") else None
    if raw_text:
        return parse_mrz_text(raw_text)
    return None


def _read_mrz(image_bgr: np.ndarray):
    """Use PassportEye/Tesseract to OCR MRZ text from an image."""
    try:
        from passporteye import read_mrz
    except ImportError as exc:
        raise RuntimeError("passporteye is required for MRZ OCR") from exc

    with tempfile.NamedTemporaryFile(suffix=".jpg", delete=False) as tmp:
        tmp_path = Path(tmp.name)
    try:
        cv2.imwrite(str(tmp_path), image_bgr)
        return read_mrz(str(tmp_path))
    finally:
        tmp_path.unlink(missing_ok=True)


def _document_info_from_passporteye_mrz(mrz) -> DocumentInfo | None:
    if not hasattr(mrz, "to_dict"):
        return None
    data = mrz.to_dict()
    if not data.get("mrz_type"):
        return None
    document_number = (data.get("number") or "").replace("<", "") or None
    valid_until = _parse_mrz_date(data.get("expiration_date") or "")
    name = data.get("names") or None
    surname = data.get("surname") or None
    if not any([document_number, valid_until, name, surname]):
        return None
    return DocumentInfo(
        name=name,
        surname=surname,
        document_number=document_number,
        valid_until=valid_until,
    )


def parse_mrz_text(text: str) -> DocumentInfo | None:
    """Parse common ID-card MRZ text into document fields.

    Supports TD1-style 3-line IDs and a permissive TD3-style fallback. The
    Slovenian mock/demo documents are expected to use MRZ lines with fillers
    (`<`) and an expiry date in YYMMDD format.
    """
    lines = _clean_mrz_lines(text)
    if len(lines) >= 3:
        return _parse_td1(lines[:3])
    if len(lines) >= 2:
        return _parse_td3(lines[:2])
    return None


def _clean_mrz_lines(text: str) -> list[str]:
    lines: list[str] = []
    for raw in text.upper().splitlines():
        line = re.sub(r"[^A-Z0-9<]", "", raw)
        if len(line) >= 20 and "<" in line:
            lines.append(line)
    return lines


def _parse_td1(lines: list[str]) -> DocumentInfo | None:
    line1, line2, line3 = lines
    document_number = line1[5:14].replace("<", "") or None
    valid_until = _parse_mrz_date(line2[8:14])
    surname, name = _parse_names(line3)
    if not any([document_number, valid_until, surname, name]):
        return None
    return DocumentInfo(
        name=name,
        surname=surname,
        document_number=document_number,
        valid_until=valid_until,
    )


def _parse_td3(lines: list[str]) -> DocumentInfo | None:
    line1, line2 = lines
    surname, name = _parse_names(line1[5:])
    document_number = line2[:9].replace("<", "") or None
    valid_until = _parse_mrz_date(line2[21:27])
    if not any([document_number, valid_until, surname, name]):
        return None
    return DocumentInfo(
        name=name,
        surname=surname,
        document_number=document_number,
        valid_until=valid_until,
    )


def _parse_names(field: str) -> tuple[str | None, str | None]:
    parts = field.split("<<", 1)
    surname = parts[0].replace("<", " ").strip() if parts else ""
    given = parts[1].replace("<", " ").strip() if len(parts) > 1 else ""
    return surname or None, given or None


def _parse_mrz_date(value: str) -> date | None:
    if not re.fullmatch(r"\d{6}", value):
        return None
    year = int(value[:2])
    month = int(value[2:4])
    day = int(value[4:6])
    current_two_digit_year = date.today().year % 100
    century = 2000 if year < current_two_digit_year + 10 else 1900
    try:
        return date(century + year, month, day)
    except ValueError:
        return None
