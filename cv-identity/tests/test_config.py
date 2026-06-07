from app.config import read_face_match_config, read_liveness_threshold


def test_read_liveness_threshold(tmp_path):
    path = tmp_path / "threshold.txt"
    path.write_text("0.05\n", encoding="utf-8")

    assert read_liveness_threshold(path) == 0.05


def test_read_face_match_config(tmp_path):
    path = tmp_path / "face_match_config.txt"
    path.write_text(
        "\n".join([
            "model=ArcFace",
            "score_type=cosine_similarity",
            "threshold=0.2",
            "decision=same_if_score_greater_or_equal",
        ]),
        encoding="utf-8",
    )

    config = read_face_match_config(path)

    assert config.model == "ArcFace"
    assert config.score_type == "cosine_similarity"
    assert config.threshold == 0.2
    assert config.decision == "same_if_score_greater_or_equal"
