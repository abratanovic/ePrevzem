"""Capture webcam frames into the dataset folder layout.

Usage:
    python scripts/capture.py --class live  --person alen
    python scripts/capture.py --class spoof --person alen

Keys while the window is open:
    SPACE — save the current frame
    q     — quit
"""
import argparse
from pathlib import Path

import cv2


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--class", dest="cls", required=True,
                        choices=["live", "spoof", "id_cards"])
    parser.add_argument("--person", required=True)
    parser.add_argument("--camera", type=int, default=0)
    args = parser.parse_args()

    out_dir = Path("dataset/raw") / args.cls / args.person
    out_dir.mkdir(parents=True, exist_ok=True)
    existing = len(list(out_dir.glob("*.jpg")))

    cap = cv2.VideoCapture(args.camera)
    if not cap.isOpened():
        raise SystemExit(f"Cannot open camera {args.camera}")

    saved = existing
    print("SPACE = save, q = quit")
    while True:
        ok, frame = cap.read()
        if not ok:
            break
        cv2.imshow(f"capture: {args.cls}/{args.person}", frame)
        key = cv2.waitKey(1) & 0xFF
        if key == ord("q"):
            break
        if key == ord(" "):
            path = out_dir / f"{args.person}_{saved:03d}.jpg"
            cv2.imwrite(str(path), frame)
            print(f"saved {path}")
            saved += 1

    cap.release()
    cv2.destroyAllWindows()
    print(f"Done. {saved - existing} new images in {out_dir}")


if __name__ == "__main__":
    main()
