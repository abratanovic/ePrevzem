from datetime import date

from app.ocr.mrz import parse_mrz_text


def test_parse_td1_mrz_extracts_document_fields():
    info = parse_mrz_text(
        "\n".join([
            "IDSVNABC1234567<<<<<<<<<<<<<<<",
            "9001011M3001017SVN<<<<<<<<<<<2",
            "NOVAK<<JANEZ<<<<<<<<<<<<<<<<<<",
        ])
    )

    assert info is not None
    assert info.surname == "NOVAK"
    assert info.name == "JANEZ"
    assert info.document_number == "ABC123456"
    assert info.valid_until == date(2030, 1, 1)
    assert info.document_valid is True


def test_parse_mrz_returns_none_for_unusable_text():
    assert parse_mrz_text("not an mrz") is None
