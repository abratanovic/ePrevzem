"""General-purpose Tesseract OCR for any identity document front image."""
import re
from datetime import date

import cv2
import numpy as np

from app.ocr.mrz import DocumentInfo

_DATE_PATTERNS = [
    # DD.MM.YYYY or DD/MM/YYYY or DD-MM-YYYY
    (re.compile(r"\b(\d{2})[.\-/](\d{2})[.\-/](\d{4})\b"), "dmy"),
    # YYYY-MM-DD
    (re.compile(r"\b(\d{4})-(\d{2})-(\d{2})\b"), "ymd"),
]

_DOC_NUMBER_PATTERN = re.compile(r"\b([A-Z]{1,3}\d{5,9}|\d{7,9})\b")

# Field numbers like "1", "1.", "4b.", "4c.", "5,"
_FIELD_NUM_RE = re.compile(r"^\d{1,2}[a-dA-D]?[.,:]?$")

# EMSO is always exactly 13 consecutive digits
_EMSO_PATTERN = re.compile(r"(?<!\d)(\d{13})(?!\d)")

# EU driving licence: field 1 = surname, field 2 = given name
_DL_SURNAME_RE = re.compile(r"^1\s+(.+)")
_DL_NAME_RE = re.compile(r"^2\s+(.+)")

# ID card structural fallback: a clean all-uppercase name line (letters, hyphens, spaces; ≥3 chars)
_ID_NAME_LINE_RE = re.compile(r"^[A-ZČŠŽĆĐÁÉÍÓÚÀÈÌÒÙÂÊÎÔÛ\-]{2}[A-ZČŠŽĆĐÁÉÍÓÚÀÈÌÒÙÂÊÎÔÛ\s\-]*$")
# Words that identify header / card-type lines to skip
_ID_HEADER_WORDS = {"OSEBNA", "IZKAZNICA", "REPUBLIKA", "SLOVENIJA", "IDENTITY", "CARD", "REPUBLIC"}


def extract_document_info_general(
    image_bgr: np.ndarray,
    variant: str = "driving_licence",
) -> DocumentInfo | None:
    """Extract all visible text from a document image using Tesseract.

    variant: "driving_licence" (default) or "id_card"
    Returns text fields sorted top-to-bottom, left-to-right so callers can
    map indices to semantic fields (name, number, expiry…) by position.
    Returns None only if Tesseract finds no text at all.
    """
    lines = _ocr_lines_sorted(image_bgr, variant)
    if not lines:
        return None

    valid_until = _find_expiry(lines)
    document_number = _find_document_number(lines)
    emso = _find_emso(lines)

    if variant == "driving_licence":
        name, surname = _find_name_surname_dl(lines)
    else:
        name, surname = _find_name_surname_id(lines)

    return DocumentInfo(
        name=name,
        surname=surname,
        document_number=document_number,
        valid_until=valid_until,
        emso=emso,
        raw_ocr_fields=tuple(lines),
    )


# Face photo occupies different fractions depending on document layout.
_FACE_ZONE = {"driving_licence": 0.15, "id_card": 0.38}


def _ocr_lines_sorted(image_bgr: np.ndarray, variant: str) -> list[str]:
    """Crop the document, preprocess, run Tesseract, return spatially sorted lines."""
    try:
        import pytesseract
    except ImportError as exc:
        raise RuntimeError("pytesseract is required for general OCR") from exc

    cropped = _crop_document(image_bgr)
    card = cropped if cropped is not None else image_bgr
    card = _ensure_landscape(card)

    face_pct = _FACE_ZONE.get(variant, 0.15)
    face_zone = int(card.shape[1] * face_pct)
    text_card = card[:, face_zone:]

    if variant == "id_card":
        preprocessed = _preprocess_id_card(text_card)
    else:
        preprocessed = _preprocess_for_ocr(text_card)

    lang = _available_lang(["hrv+slv+srp_latn+eng", "slv+hrv+eng", "slv+eng", "slv", "eng"])

    if variant == "id_card":
        # Split into left (labels+values) and right (dates/EMSO) columns.
        # Left column: PSM 4 (variable-size single column) preserves all labels;
        # PSM 6 on the same column gives better accuracy for short values like "SI"
        # because the narrower context prevents LSTM digit/letter confusion.
        # We take PSM 4's structure (labels intact) and replace each value line
        # with the matching PSM 6 reading via label-context normalisation.
        w = preprocessed.shape[1]
        left_col  = preprocessed[:, :w // 2]
        right_col = preprocessed[:, w // 2:]

        def _raw_lines(img: np.ndarray, psm: int) -> list[str]:
            raw = pytesseract.image_to_string(img, lang=lang,
                                              config=f"--psm {psm} --oem 3")
            return [ln.strip() for ln in raw.splitlines() if ln.strip()]

        def _is_label(line: str) -> bool:
            return "/" in line and len(line) >= 8

        lines4 = _raw_lines(left_col, psm=4)
        lines6 = _raw_lines(left_col, psm=6)

        # Build an ordered list of PSM-6 value lines (non-labels, normalised)
        # to substitute into the PSM-4 structure.
        values6 = [
            _normalize_id_value(ln)
            for ln in lines6
            if not _is_label(ln) and _is_meaningful_line(ln)
        ]
        val_idx = 0
        merged: list[str] = []
        prev_label = ""
        for line in lines4:
            if _is_label(line):
                prev_label = line.lower()
                merged.append(_normalize_id_value(line))
            else:
                if val_idx < len(values6):
                    val = values6[val_idx]
                    val_idx += 1
                else:
                    val = _normalize_id_value(line)
                # Nationality must be letters only; fallback digit→letter coercion.
                if "nationality" in prev_label and not any(c.isalpha() for c in val):
                    val = val.translate(str.maketrans("01589", "OISIS"))
                merged.append(val)

        left_lines = [ln for ln in merged if _is_meaningful_line(ln)]

        right_lines = [
            _normalize_id_value(ln.strip())
            for ln in _raw_lines(right_col, psm=6)
            if ln.strip() and _is_meaningful_line(ln.strip())
        ]
        return left_lines + right_lines

    # --- driving_licence path (PSM 11 + y-band grouping) ---
    # Use preprocessed dimensions — preprocessing may upscale small images,
    # so token pixel coordinates (from Tesseract) are in the processed frame.
    text_w = preprocessed.shape[1]

    data = pytesseract.image_to_data(
        preprocessed,
        lang=lang,
        config="--psm 11 --oem 3",
        output_type=pytesseract.Output.DICT,
    )

    # Filter leftmost 10% (card-border noise) and rightmost 20%.
    noise_edge = int(text_w * 0.10)
    right_edge = int(text_w * 0.80)

    # Group tokens by y-bands rather than Tesseract block identity.
    # PSM 11 assigns field labels and their values to separate blocks even when
    # they sit on the same visual line, so block-based keys would split them.
    Y_BAND = 100
    band_words: dict[int, list[tuple[int, int, str]]] = {}  # band → [(top, left, text)]

    for i, text in enumerate(data["text"]):
        text = text.strip()
        if not text or int(data["conf"][i]) < 40:
            continue
        if data["left"][i] < noise_edge or data["left"][i] > right_edge:
            continue
        if not _is_meaningful_token(text):
            continue
        band = (data["top"][i] // Y_BAND) * Y_BAND
        band_words.setdefault(band, []).append(
            (data["top"][i], data["left"][i], text)
        )

    lines = [
        " ".join(t for _, _, t in sorted(words, key=lambda x: x[1]))
        for _, words in sorted(band_words.items())
    ]
    return [ln for ln in lines if _is_meaningful_line(ln)]


def _is_meaningful_token(token: str) -> bool:
    """Keep field numbers (1, 4b., …) and words with ≥2 alphanumeric chars."""
    alnum_count = sum(c.isalnum() for c in token)
    if alnum_count == 0:
        return False
    if _FIELD_NUM_RE.match(token):
        return True
    return alnum_count >= 2


def _is_meaningful_line(line: str) -> bool:
    alnum = sum(c.isalnum() for c in line)
    return alnum >= 1


def _preprocess_for_ocr(card_bgr: np.ndarray) -> np.ndarray:
    """Convert card image to a clean binary image suited for Tesseract."""
    h, w = card_bgr.shape[:2]
    scale = max(1.0, 1600 / max(h, w))
    if scale > 1.0:
        card_bgr = cv2.resize(card_bgr, None, fx=scale, fy=scale,
                              interpolation=cv2.INTER_CUBIC)

    gray = cv2.cvtColor(card_bgr, cv2.COLOR_BGR2GRAY)
    gray = cv2.fastNlMeansDenoising(gray, h=10)

    # Different areas of the card have slightly different text sizes, so a single
    # kernel size misses some fields. Taking the pixel-wise max of two BLACKHAT
    # passes (small + large kernel) captures text across the full size range.
    bh_small = cv2.morphologyEx(
        gray, cv2.MORPH_BLACKHAT,
        cv2.getStructuringElement(cv2.MORPH_RECT, (28, 28)),
    )
    bh_large = cv2.morphologyEx(
        gray, cv2.MORPH_BLACKHAT,
        cv2.getStructuringElement(cv2.MORPH_RECT, (38, 38)),
    )
    blackhat = cv2.max(bh_small, bh_large)

    binary = cv2.adaptiveThreshold(
        blackhat, 255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY,
        blockSize=31, C=-5,
    )
    # Tesseract expects dark text on a light background; BLACKHAT produces the
    # inverse (light text on dark), so flip before handing off to OCR.
    inverted = cv2.bitwise_not(binary)

    # The face-photo border leaves a tall vertical bar that bleeds into adjacent
    # text (e.g. the U in "Ulica") and causes misreads. Detect any continuous
    # vertical black stripe taller than 200 px and white it out.
    # On a black-on-white binary image OpenCV's erode=min shrinks white/grows black,
    # so MORPH_CLOSE (dilate→erode) is the standard "opening" for black objects:
    # it removes thin/short black features while preserving tall continuous bars.
    vbar_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (3, 200))
    vbars = cv2.morphologyEx(inverted, cv2.MORPH_CLOSE, vbar_kernel)
    inverted = cv2.add(inverted, cv2.bitwise_not(vbars))

    return inverted


def _normalize_id_value(s: str) -> str:
    """Fix common OCR confusions introduced by the ID card's security font.

    £ is consistently misread for E; short non-label values are uppercased so
    that "Si"→"SI". Leading "1" before a letter (e.g. "1E…") is the letter I.
    """
    s = s.replace("£", "E").replace("€", "E")
    # Bilingual labels always contain "/"; skip them.
    if "/" not in s and len(s) <= 20:
        s = s.upper()
        # Security-font I looks like "1": fix leading digit before letter.
        if len(s) >= 2 and s[0] == "1" and s[1].isalpha():
            s = "I" + s[1:]
    return s


def _preprocess_id_card(card_bgr: np.ndarray) -> np.ndarray:
    """Preprocess a colour-background ID card (e.g. Slovenian osebna izkaznica).

    The ID card has a teal/blue guilloche gradient that BLACKHAT cannot suppress
    (pattern scale too large). Instead we boost saturated pixels (background) so
    Otsu threshold cleanly separates them from desaturated dark text.
    Returns black text on white background — no inversion needed.
    """
    h, w = card_bgr.shape[:2]
    scale = max(1.0, 1600 / max(h, w))
    if scale > 1.0:
        card_bgr = cv2.resize(card_bgr, None, fx=scale, fy=scale,
                              interpolation=cv2.INTER_CUBIC)

    gray = cv2.cvtColor(card_bgr, cv2.COLOR_BGR2GRAY)
    gray = cv2.fastNlMeansDenoising(gray, h=10)

    sat = cv2.cvtColor(card_bgr, cv2.COLOR_BGR2HSV)[:, :, 1].astype(np.float32) / 255.0
    # Background (saturated teal) → pushed toward 255; text (black, low sat) → unchanged.
    modified = np.clip(gray.astype(np.float32) + sat * 128, 0, 255).astype(np.uint8)
    _, binary = cv2.threshold(modified, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    return binary  # already black text on white — correct for Tesseract


def _crop_document(image_bgr: np.ndarray) -> np.ndarray | None:
    """Detect the card region in the photo and return a deskewed, cropped version."""
    gray = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2GRAY)
    blurred = cv2.GaussianBlur(gray, (7, 7), 0)
    _, thresh = cv2.threshold(blurred, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (20, 20))
    closed = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel)

    contours, _ = cv2.findContours(closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours:
        return None

    img_area = image_bgr.shape[0] * image_bgr.shape[1]
    large = [c for c in contours if cv2.contourArea(c) > img_area * 0.15]
    if not large:
        return None

    largest = max(large, key=cv2.contourArea)
    rect = cv2.minAreaRect(largest)
    box = cv2.boxPoints(rect).astype(np.float32)
    return _four_point_transform(image_bgr, box)


def _ensure_landscape(image_bgr: np.ndarray) -> np.ndarray:
    h, w = image_bgr.shape[:2]
    if h > w:
        return cv2.rotate(image_bgr, cv2.ROTATE_90_CLOCKWISE)
    return image_bgr


def _four_point_transform(image: np.ndarray, pts: np.ndarray) -> np.ndarray:
    rect = _order_points(pts)
    tl, tr, br, bl = rect
    width = int(max(np.linalg.norm(br - bl), np.linalg.norm(tr - tl)))
    height = int(max(np.linalg.norm(tr - br), np.linalg.norm(tl - bl)))
    if width == 0 or height == 0:
        return image
    dst = np.array(
        [[0, 0], [width - 1, 0], [width - 1, height - 1], [0, height - 1]],
        dtype=np.float32,
    )
    M = cv2.getPerspectiveTransform(rect, dst)
    return cv2.warpPerspective(image, M, (width, height))


def _order_points(pts: np.ndarray) -> np.ndarray:
    rect = np.zeros((4, 2), dtype=np.float32)
    s = pts.sum(axis=1)
    rect[0] = pts[np.argmin(s)]
    rect[2] = pts[np.argmax(s)]
    diff = np.diff(pts, axis=1)
    rect[1] = pts[np.argmin(diff)]
    rect[3] = pts[np.argmax(diff)]
    return rect


def _available_lang(candidates: list[str]) -> str:
    try:
        import pytesseract
        installed = set(pytesseract.get_languages())
    except Exception:
        return "eng"
    for lang in candidates:
        if all(p in installed for p in lang.split("+")):
            return lang
    return "eng"


def _find_expiry(lines: list[str]) -> date | None:
    """Return the latest date found in OCR lines (most likely the expiry date)."""
    found: list[date] = []
    for line in lines:
        for pattern, fmt in _DATE_PATTERNS:
            for m in pattern.finditer(line):
                try:
                    if fmt == "dmy":
                        d = date(int(m.group(3)), int(m.group(2)), int(m.group(1)))
                    else:
                        d = date(int(m.group(1)), int(m.group(2)), int(m.group(3)))
                    found.append(d)
                except ValueError:
                    pass
    return max(found) if found else None


def _find_document_number(lines: list[str]) -> str | None:
    # Collect all matches and return the longest one — field 5 (9-digit number)
    # tends to be longer than serials embedded in field 4d.
    best: str | None = None
    for line in lines:
        for m in _DOC_NUMBER_PATTERN.finditer(line.upper()):
            candidate = m.group(1)
            if best is None or len(candidate) > len(best):
                best = candidate
    return best


def _find_emso(lines: list[str]) -> str | None:
    """Return the first 13-digit sequence found — the Slovenian EMSO."""
    for line in lines:
        m = _EMSO_PATTERN.search(line)
        if m:
            return m.group(1)
    return None


def _find_name_surname_dl(lines: list[str]) -> tuple[str | None, str | None]:
    """Extract name and surname from EU driving licence fields 1 and 2."""
    name: str | None = None
    surname: str | None = None
    for line in lines:
        m = _DL_SURNAME_RE.match(line)
        if m:
            surname = m.group(1).strip()
            continue
        m = _DL_NAME_RE.match(line)
        if m:
            name = m.group(1).strip()
    return name, surname


def _find_name_surname_id(lines: list[str]) -> tuple[str | None, str | None]:
    """Extract name and surname from Slovenian ID card OCR lines.

    Tries two strategies in order:
    1. Label-based: looks for 'Priimek' / 'Ime' label lines and grabs the
       following value (works when Tesseract reads the small label text).
    2. Structural fallback: when labels are too small to OCR, the first two
       clean all-uppercase alphabetic lines after the card header are the
       surname and name respectively.
    """
    name, surname = _find_name_surname_id_by_labels(lines)
    if name and surname:
        return name, surname
    return _find_name_surname_id_structural(lines)


def _find_name_surname_id_by_labels(lines: list[str]) -> tuple[str | None, str | None]:
    name: str | None = None
    surname: str | None = None
    next_is_surname = False
    next_is_name = False
    for line in lines:
        ll = line.lower()
        if "priimek" in ll:
            next_is_surname = True
            next_is_name = False
            continue
        if "given name" in ll or (ll.startswith("ime") and "/" in ll):
            next_is_name = True
            next_is_surname = False
            continue
        if next_is_surname and "/" not in line:
            surname = line.strip()
            next_is_surname = False
            continue
        if next_is_name and "/" not in line:
            name = line.strip()
            next_is_name = False
    return name, surname


def _find_name_surname_id_structural(lines: list[str]) -> tuple[str | None, str | None]:
    """Fallback: first two clean all-caps alphabetic lines = surname, name."""
    candidates: list[str] = []
    for line in lines:
        stripped = line.strip()
        if not _ID_NAME_LINE_RE.match(stripped):
            continue
        words = stripped.split()
        if any(w in _ID_HEADER_WORDS for w in words):
            continue
        candidates.append(stripped)
        if len(candidates) == 2:
            break
    surname = candidates[0] if len(candidates) > 0 else None
    name = candidates[1] if len(candidates) > 1 else None
    return name, surname
