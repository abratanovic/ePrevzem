from app.ocr.general import _find_name_surname_dl


def test_driving_licence_name_from_merged_birth_line_date_before_name():
    name, surname = _find_name_surname_dl([
        "VOZNIŠKO DOVOLJENJE REPUBLIKA SLOVENIJA",
        "1. Bečić",
        "3. 2. 10.10.2005 Edvin Banja Luka (BIH)",
        "4b. 4a. 10.10.2026 10.10.2023 4d. 4c. UE 1010005500426/1308494 MARIBOR",
    ])

    assert name == "Edvin"
    assert surname == "Bečić"


def test_driving_licence_name_from_merged_birth_line_name_before_date():
    name, surname = _find_name_surname_dl([
        "VOZNIŠKO DOVOLJENJE REPUBLIKA SLOVENIJA",
        "1. Bečić",
        "3 2 Edvin 10.10.2005 Banja Luka (BIH)",
        "4b 4a 10.10.2026 10.10.2023 4d 4c UE 1010005500426/1308494 MARIBOR",
    ])

    assert name == "Edvin"
    assert surname == "Bečić"


def test_driving_licence_ignores_other_numbered_fields_as_surname():
    name, surname = _find_name_surname_dl([
        "VOZNIŠKO DOVOLJENJE REPUBLIKA SLOVENIJA",
        "3 1. Bečić",
        "3. 2. 10.10.2005 Edvin Banja Luka (BIH)",
        "1 4b. 4a. 10.10.2026 10.10.2023 4d. 4c. UE 1010005500426/1308494 MARIBOR",
        "1 8. Maribor Ulica Vita Kraigherja 18",
    ])

    assert name == "Edvin"
    assert surname is None


def test_driving_licence_trims_surname_noise_after_digits():
    name, surname = _find_name_surname_dl([
        "VOZNIŠKO DOVOLJENJI REPUBLIKA SLOVENIJA",
        "1. Bečić 4 NOV po",
        "3 2 Edvin 10.10.2005 Banja Luka (BIH)",
    ])

    assert name == "Edvin"
    assert surname == "Bečić"
