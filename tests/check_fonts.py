"""Optional offline cmap validation; requires fontTools, never starts the game."""
from pathlib import Path
from fontTools.ttLib import TTFont

root = Path(__file__).resolve().parents[1]
failed = False
for language in sorted((root / "Languages").iterdir()):
    characters = set()
    for line in (language / "translations.tsv").read_text(encoding="utf-8-sig").splitlines():
        if line and not line.startswith("#"):
            characters.update(line.split("\t")[1].replace("{0}", "0123456789"))
    for line in (language / "fonts.tsv").read_text(encoding="utf-8-sig").splitlines():
        if not line or line.startswith("#"):
            continue
        original, kind, relative, family, scale = line.split("\t")
        if kind not in ("ttf", "bundle"):
            continue
        font_path = root / relative if kind == "ttf" else root / "Fonts" / (original + ".ttf")
        with TTFont(font_path) as font:
            families = {name.toUnicode() for name in font["name"].names if name.nameID in (1, 16)}
            missing = "".join(sorted(c for c in characters if not c.isspace() and ord(c) not in font.getBestCmap()))
            family_ok = kind == "bundle" or family in families
            ok = not missing and family_ok
            failed |= not ok
            print(f"{language.name}/{original}: {'PASS' if ok else 'FAIL'}" +
                  (f" missing={missing}" if missing else "") +
                  (f" family not found: {family}" if not family_ok else ""))
raise SystemExit(1 if failed else 0)
