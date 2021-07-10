#!/usr/bin/python3

# removes .meta files that do not point to anything
#
# requires python 3
#
# usage:
#   automatic: yes | python Tools/housekeeping/purge_dangling_meta_files.py
#   manual:    python Tools/housekeeping/purge_dangling_meta_files.py

from pathlib import Path


def main():
    removed = 0

    for meta in Path(".").rglob("*.meta"):
        if not (meta.parent / Path(meta.stem)).exists():
            if input(f"remove? {meta} ").lower() == "y":
                meta.unlink()

                removed += 1

    print("removed", removed, "files")


if __name__ == "__main__":
    main()
