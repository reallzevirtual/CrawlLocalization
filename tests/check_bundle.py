"""Verify Unity version, asset paths and exact embedded TTF bytes; requires UnityPy/fontTools."""
import hashlib
import io
import argparse
from pathlib import Path
import UnityPy
from fontTools.ttLib import TTFont
from translation_text import visible_text

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--game-path", type=Path, help="Also compare layout with the original game's serialized Font assets")
parser.add_argument("--bundle", type=Path, help="Check another built or installed bundle")
parser.add_argument("--combined", action="store_true", help="Check the optional combined font")
options = parser.parse_args()
layout = {}
for line in (root / "FontBundleProject" / "font-layout.tsv").read_text(encoding="utf-8-sig").splitlines():
    if line and not line.startswith("#"):
        name, ascent, descent, spacing = line.split("\t")
        layout[name] = dict(zip(("m_Ascent", "m_Descent", "m_LineSpacing"), map(float, (ascent, descent, spacing))))
bundle_path = options.bundle or root / "Fonts" / ("crawl-combined.bundle" if options.combined else "crawl-chinese-fonts.bundle")
env = UnityPy.load(str(bundle_path))
# Font, Material and Texture subassets share the same container key. Mirror
# LoadAsset<Font> by filtering by type rather than using the untyped key lookup.
font_assets = {path: obj for path, obj in env.container.items() if obj.type.name == "Font"}
fonts = {obj.read_typetree()["m_Name"]: obj for obj in env.objects if obj.type.name == "Font"}
assert len(fonts) == 10, f"Expected 10 font assets, got {len(fonts)}"
for name, obj in sorted(fonts.items()):
    version = str(obj.assets_file.unity_version)
    assert version == "5.4.2f2", f"Wrong Unity version: {version}"
    assert int(obj.assets_file.target_platform) == 5, "Expected StandaloneWindows (x86)"
    data = obj.read_typetree()
    embedded = bytes(data["m_FontData"])
    original = ((root / "Fonts" / "ChineseFallback" if options.combined else root / "Fonts") / (name + ".ttf")).read_bytes()
    if options.combined:
        cmap = TTFont(io.BytesIO(embedded)).getBestCmap()
        required = '中文张三你好_ЁёÁÉÍÓÚÜÑáéíóúüñ¿¡' + ''.join(map(chr, range(0x410,0x450)))
        assert all(ord(c) in cmap for c in required), name
    assert embedded == original, f"Font data differs from source: {name}"
    expected_size = 200 if name == "04B_04-Black" else 150 if name == "04B_03-White" else 16
    assert data["m_FontSize"] == expected_size, f"Unexpected default font size: {name}"
    assert data["m_FontRenderingMode"] == (2 if name in ("04B_04-Black", "CrawlTiny") else 0), name
    assert not data["m_CharacterRects"], f"Expected dynamic font: {name}"
    for field, expected in layout[name].items():
        assert data[field] == expected, f"Layout changed: {name}/{field}: {data[field]} != {expected}"
    print(f"PASS embedded {name}: {len(embedded)} bytes, SHA256 {hashlib.sha256(embedded).hexdigest()}")

checks = 0
for locale in ("zh-Hans",):
    chars = set()
    for line in (line for table in (root / "Languages" / locale).glob("*translations.tsv") for line in table.read_text(encoding="utf-8-sig").splitlines()):
        if line and not line.startswith("#"):
            chars.update(visible_text(line.split("\t")[1]))
    for line in (root / "Languages" / locale / "fonts.tsv").read_text(encoding="utf-8-sig").splitlines():
        if not line or line.startswith("#"):
            continue
        original, kind, path, asset, scale = line.split("\t")
        assert kind == "bundle" and path == "Fonts/crawl-chinese-fonts.bundle", f"Non-bundle mapping: {original}"
        assert asset in font_assets, f"Unresolvable Font asset path: {asset}"
        tree = font_assets[asset].read_typetree()
        assert tree["m_Name"] == original, f"Wrong mapped asset: {original}"
        with TTFont(io.BytesIO(bytes(tree["m_FontData"]))) as font:
            missing = sorted(c for c in chars if not c.isspace() and ord(c) not in font.getBestCmap())
            # Preserve the requested original Chinese bundle byte-for-byte. Its
            # historical Tiny underscore gap is not a regression in this release.
            known = ['_'] if not options.combined and original == 'CrawlTiny' else []
            assert not set(missing) - set(known), f"Missing glyphs in {locale}/{original}: {''.join(missing)}"
            if missing: print(f"NOTE retained original {original} glyph gap: {''.join(missing)}")
        checks += 1
if options.game_path:
    compared = set()
    for filename in ("sharedassets0.assets", "sharedassets2.assets"):
        for obj in UnityPy.load(str(options.game_path / "Crawl_Data" / filename)).objects:
            if obj.type.name != "Font":
                continue
            original = obj.read_typetree()
            name = original["m_Name"]
            if name not in fonts:
                continue
            built = fonts[name].read_typetree()
            for field, value in original.items():
                if isinstance(value, (str, int, float, bool)):
                    assert built[field] == value, f"Game layout mismatch: {name}/{field}: {built[field]} != {value}"
            compared.add(name)
    assert compared == set(fonts), "Not all fonts matched the game assets"
    print("PASS: all 10 fonts preserve every scalar layout/style property from the original game assets.")
print(f"PASS: Unity 5.4.2f2 Windows x86 bundle; 10 byte-exact embedded fonts with original layout; {checks} language/font mappings with checked glyph coverage (known original Tiny underscore gap reported separately).")
