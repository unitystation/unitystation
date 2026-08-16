#!/usr/bin/env python3
"""
generate_genemoji_md.py

Usage:
    python generate_genemoji_md.py
    python generate_genemoji_md.py --repo-root /path/to/repo
    python generate_genemoji_md.py --asset-path UnityProject/Assets/Resources/Icons/GenEmoji.asset
    python generate_genemoji_md.py --out docs/development/GenEmoji.md

What it does:
- Parses the Unity asset file (GenEmoji.asset) for sprite entries (names, and where available, texture rects).
- Looks for an atlas PNG (GenEmoji*.png) in the same folder.
- If rects and atlas image are available, crops each sprite from the atlas and writes per-emoji preview images into docs/development/_emoji_images/.
- If cropping is not possible, tries to find per-sprite PNG files in the icons folder.
- Generates a markdown table with index, name, and preview image, and writes it to the docs/development folder.

Requires:
- Pillow (PIL) for image cropping: pip install pillow
"""

import argparse
import json
import os
import re
import sys
from pathlib import Path

try:
    from PIL import Image
except Exception:
    print("This script requires Pillow. Install with: pip install pillow", file=sys.stderr)
    raise

# Defaults
DEFAULT_ASSET_PATH = Path("UnityProject/Assets/Resources/Icons/GenEmoji.asset")
DEFAULT_ICONS_DIR = DEFAULT_ASSET_PATH.parent
DEFAULT_OUT_MD = Path("docs/development/GenEmoji.md")
DEFAULT_IMAGE_OUT_DIR = Path("docs/development/_emoji_images")

# Regex patterns to find names and rects in various Unity YAML/binary-text forms
NAME_RE = re.compile(r'(?:m_Name|name):\s*(?:"([^"]+)"|([^\r\n]+))')
# match rect or textureRect in forms like:
# rect: {x: 1, y: 2, width: 3, height: 4}
# textureRect: {x: 1, y: 2, width: 3, height: 4}
RECT_PATTERN_1 = re.compile(
    r'(?:rect|textureRect)\s*:\s*\{\s*x\s*:\s*([-+]?\d*\.?\d+)\s*,\s*y\s*:\s*([-+]?\d*\.?\d+)\s*,\s*width\s*:\s*([-+]?\d*\.?\d+)\s*,\s*height\s*:\s*([-+]?\d*\.?\d+)\s*\}'
)
# match rect as array: rect: [x, y, w, h]
RECT_PATTERN_2 = re.compile(
    r'(?:rect|textureRect)\s*:\s*\[\s*([-+]?\d*\.?\d+)\s*,\s*([-+]?\d*\.?\d+)\s*,\s*([-+]?\d*\.?\d+)\s*,\s*([-+]?\d*\.?\d+)\s*\]'
)
# fallback loose pattern for x: ... y: ... width: ... height: ...
RECT_PATTERN_3 = re.compile(
    r'x\s*:\s*([-+]?\d*\.?\d+)\s*[,;\s]\s*y\s*:\s*([-+]?\d*\.?\d+)\s*[,;\s]\s*width\s*:\s*([-+]?\d*\.?\d+)\s*[,;\s]\s*height\s*:\s*([-+]?\d*\.?\d+)'
)

SANITIZE_RE = re.compile(r'[^A-Za-z0-9._-]+')


def find_atlas_image(icons_dir: Path):
    # Prefer the exact atlas file used by the TMP sprite asset.
    exact_candidates = [
        icons_dir / "GenEmoji_Atlas.png",
        icons_dir / "GenEmojiAtlas.png",
        icons_dir / "genemoji_atlas.png",
    ]
    for c in exact_candidates:
        if c.exists():
            return c

    candidates = list(icons_dir.glob("GenEmoji*.png")) + list(icons_dir.glob("genemoji*.png")) + list(icons_dir.glob("*.png"))
    if not candidates:
        return None
    for c in candidates:
        if c.name.lower().startswith("genemoji"):
            return c
    return candidates[0]


def _parse_block_records(asset_text: str):
    """Split Unity YAML sections into record blocks that start with '-'."""
    blocks = []
    lines = asset_text.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.strip().startswith("-"):
            block = [line]
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("-"):
                block.append(lines[i])
                i += 1
            blocks.append("\n".join(block))
        else:
            i += 1
    return blocks


def parse_asset_for_sprites(asset_text: str):
    """
    Returns list of dicts: [{'name': 'foo', 'rect': (x,y,w,h) or None}, ...]
    Maps TMP sprite character names to their corresponding atlas glyph rects.
    """
    names_by_index = {}
    rects_by_index = {}

    for block in _parse_block_records(asset_text):
        glyph_index_match = re.search(r'm_GlyphIndex\s*:\s*(\d+)', block)
        glyph_name_match = re.search(r'm_Name\s*:\s*(?:"([^"]+)"|([^\r\n]+))', block)
        if glyph_index_match and glyph_name_match:
            idx = int(glyph_index_match.group(1))
            name = (glyph_name_match.group(1) or glyph_name_match.group(2) or "").strip()
            if name:
                names_by_index[idx] = name

        glyph_rect_index_match = re.search(r'm_Index\s*:\s*(\d+)', block)
        if glyph_rect_index_match:
            idx = int(glyph_rect_index_match.group(1))
            x_match = re.search(r'm_X\s*:\s*(-?\d+)', block)
            y_match = re.search(r'm_Y\s*:\s*(-?\d+)', block)
            w_match = re.search(r'm_Width\s*:\s*(\d+)', block)
            h_match = re.search(r'm_Height\s*:\s*(\d+)', block)
            if x_match and y_match and w_match and h_match:
                rects_by_index[idx] = (
                    float(x_match.group(1)),
                    float(y_match.group(1)),
                    float(w_match.group(1)),
                    float(h_match.group(1)),
                )

    sprites = []
    for idx in sorted(set(names_by_index) | set(rects_by_index)):
        name = names_by_index.get(idx)
        if not name:
            continue
        rect = rects_by_index.get(idx)
        sprites.append({'name': name, 'rect': rect})

    # Deduplicate preserving order by name.
    seen = set()
    out = []
    for s in sprites:
        if s['name'] in seen:
            continue
        seen.add(s['name'])
        out.append(s)
    return out


def crop_sprite_from_atlas(atlas_path: Path, rect, out_path: Path):
    """
    rect: (x, y, w, h) floats
    Try two coordinate conventions (top-left origin or bottom-left origin) and pick the crop
    that fits entirely inside the image bounds.
    Returns True if saved.
    """
    atlas = Image.open(atlas_path).convert("RGBA")
    aw, ah = atlas.size
    x, y, w, h = rect
    # candidate 1: assume y is top coordinate
    c1 = (int(round(x)), int(round(y)), int(round(x + w)), int(round(y + h)))
    # candidate 2: assume y is bottom coordinate (Unity uses bottom-left for sprites sometimes)
    c2 = (int(round(x)), int(round(ah - y - h)), int(round(x + w)), int(round(ah - y)))
    def valid_box(box):
        x0, y0, x1, y1 = box
        return 0 <= x0 < x1 <= aw and 0 <= y0 < y1 <= ah
    box = None
    if valid_box(c2):
        box = c2
    elif valid_box(c1):
        box = c1
    else:
        # try to clamp into bounds
        x0 = max(0, min(aw - 1, int(round(x))))
        y0 = max(0, min(ah - 1, int(round(y))))
        x1 = max(0, min(aw, int(round(x + w))))
        y1 = max(0, min(ah, int(round(y + h))))
        if x1 > x0 and y1 > y0:
            box = (x0, y0, x1, y1)
    if box is None:
        return False
    cropped = atlas.crop(box)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(out_path)
    return True


def find_per_sprite_file(icons_dir: Path, sprite_name: str):
    # try common extensions and exact name matches or sanitized
    candidates = []
    base = Path(sprite_name)
    possible_names = [sprite_name, sprite_name + ".png", sprite_name + ".jpg", sprite_name + ".jpeg"]
    # sanitized name
    sanitized = SANITIZE_RE.sub("_", sprite_name)
    possible_names += [sanitized, sanitized + ".png"]
    for name in possible_names:
        p = icons_dir / name
        if p.exists():
            return p
    # last-ditch: search for filename that contains the sprite name
    for p in icons_dir.glob("**/*"):
        if p.is_file() and sprite_name.lower() in p.name.lower():
            return p
    return None


def generate_markdown(sprites, md_out_path: Path, atlas_path: Path | None = None):
    atlas_url = None
    if atlas_path:
        atlas_url = os.path.relpath(atlas_path, start=md_out_path.parent).replace(os.sep, "/")

    md_lines = []
    md_lines.append("# Unitystation Emojis Table")
    md_lines.append("")
    md_lines.append(f"Generated from `UnityProject/Assets/Resources/Icons/GenEmoji.asset` -> {len(sprites)} emojis found.")
    md_lines.append("")
    md_lines.append("<style>")
    md_lines.append(".emoji-preview {")
    md_lines.append("  display: inline-block;")
    md_lines.append("  vertical-align: middle;")
    md_lines.append("  background-repeat: no-repeat;")
    md_lines.append("  box-sizing: border-box;")
    md_lines.append("  border: 1px solid rgba(0, 0, 0, 0.1);")
    md_lines.append("  image-rendering: pixelated;")
    md_lines.append("}")
    md_lines.append("</style>")
    md_lines.append("")
    md_lines.append("| Index | Name | Preview |")
    md_lines.append("|---:|---|:---:|")

    for idx, s in enumerate(sprites):
        name = s['name']
        rect = s.get('rect')
        if atlas_url and rect:
            x, y, w, h = rect
            preview = (
                '<span class="emoji-preview" '
                f'data-name="{name}" '
                f'data-x="{int(round(x))}" '
                f'data-y="{int(round(y))}" '
                f'data-w="{int(round(w))}" '
                f'data-h="{int(round(h))}" '
                f'title="{name}" aria-label="{name}"></span>'
            )
        else:
            preview = "(no preview)"
        md_lines.append(f"| {idx} | `{name}` | {preview} |")

    if atlas_url:
        md_lines.append("")
        md_lines.append("<script>")
        md_lines.append("(() => {")
        md_lines.append(f"  const atlasUrl = {json.dumps(atlas_url)};")
        md_lines.append("  document.querySelectorAll('.emoji-preview').forEach((node) => {")
        md_lines.append("    const x = Number(node.dataset.x || 0);")
        md_lines.append("    const y = Number(node.dataset.y || 0);")
        md_lines.append("    const w = Number(node.dataset.w || 0);")
        md_lines.append("    const h = Number(node.dataset.h || 0);")
        md_lines.append("    node.style.backgroundImage = `url('${atlasUrl}')`; ")
        md_lines.append("    node.style.backgroundPosition = `${-x}px ${-y}px`; ")
        md_lines.append("    node.style.width = `${w}px`; ")
        md_lines.append("    node.style.height = `${h}px`; ")
        md_lines.append("    node.style.backgroundSize = 'auto'; ")
        md_lines.append("  });")
        md_lines.append("})();")
        md_lines.append("</script>")

    md_lines.append("")
    md_out_path.parent.mkdir(parents=True, exist_ok=True)
    md_out_path.write_text("\n".join(md_lines), encoding="utf-8")
    print(f"Wrote markdown to: {md_out_path}")


def main():
    p = argparse.ArgumentParser(description="Generate a markdown table of emojis from GenEmoji.asset")
    p.add_argument("--repo-root", default=".", help="Path to repository root (default: current dir)")
    p.add_argument("--asset-path", default=str(DEFAULT_ASSET_PATH), help="Path to GenEmoji.asset relative to repo root")
    p.add_argument("--icons-dir", default=None, help="Directory containing icons (defaults to asset parent dir)")
    p.add_argument("--out", default=str(DEFAULT_OUT_MD), help="Output markdown path relative to repo root")
    p.add_argument("--images-out", default=str(DEFAULT_IMAGE_OUT_DIR), help="Directory to write per-emoji preview images")
    args = p.parse_args()

    repo_root = Path(args.repo_root).resolve()
    asset_path = (repo_root / args.asset_path).resolve()
    if args.icons_dir:
        icons_dir = (repo_root / args.icons_dir).resolve()
    else:
        icons_dir = asset_path.parent if asset_path.exists() else (repo_root / DEFAULT_ICONS_DIR).resolve()
    out_md = (repo_root / args.out).resolve()
    images_out = (repo_root / args.images_out).resolve()

    if not asset_path.exists():
        print(f"Asset file not found: {asset_path}", file=sys.stderr)
        sys.exit(2)
    if not icons_dir.exists():
        print(f"Icons directory not found: {icons_dir}", file=sys.stderr)
        sys.exit(2)

    asset_text = asset_path.read_text(encoding="utf-8", errors="ignore")
    sprites = parse_asset_for_sprites(asset_text)
    if not sprites:
        print("No sprites found by parsing asset; trying to discover names from filenames in the icons folder.")
        # fallback: find image files in icons_dir
        files = [p for p in icons_dir.glob("**/*") if p.is_file() and p.suffix.lower() in (".png", ".jpg", ".jpeg", ".gif")]
        sprites = [{'name': p.stem, 'rect': None} for p in sorted(files, key=lambda x: x.name)]

    atlas_image = find_atlas_image(icons_dir)
    if atlas_image:
        print(f"Found atlas image: {atlas_image}")
    else:
        print("No atlas PNG found in icons directory; falling back to per-sprite files when available.")

    if atlas_image:
        print("Using atlas-based inline cropping in the generated markdown to avoid generating thousands of preview files.")
    else:
        for idx, s in enumerate(sprites):
            name = s['name']
            rect = s.get('rect')
            out_filename = f"{idx:03d}_{SANITIZE_RE.sub('_', name)}.png"
            out_path = images_out / out_filename
            preview_rel = None
            sprite_file = find_per_sprite_file(icons_dir, name)
            if sprite_file:
                out_path.parent.mkdir(parents=True, exist_ok=True)
                try:
                    img = Image.open(sprite_file).convert("RGBA")
                    img.save(out_path)
                    preview_rel = os.path.relpath(out_path, start=out_md.parent)
                    s['preview_relpath'] = preview_rel.replace(os.sep, "/")
                    print(f"Copied sprite file for '{name}' -> {out_path}")
                except Exception as e:
                    print(f"Failed to copy sprite file {sprite_file} for {name}: {e}")
            if preview_rel is None:
                print(f"No preview for '{name}'")

    # generate markdown
    generate_markdown(sprites, out_md, atlas_image)
    print("Done.")


if __name__ == "__main__":
    main()