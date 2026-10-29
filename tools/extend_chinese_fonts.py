"""Add missing multilingual glyphs to Chinese fallback fonts; preserve all inputs."""
import copy
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen
from merge_multilingual_fonts import outline, transformed

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Fonts/ChineseFallback'

def main():
    OUT.mkdir(exist_ok=True)
    for source in sorted((ROOT / 'Fonts').glob('*.ttf')):
        base = TTFont(source)
        donor = TTFont(ROOT / 'Fonts/Multilingual/Generated' / source.name)
        result = copy.deepcopy(base)
        cmap = base.getBestCmap()
        ratio = base['head'].unitsPerEm / donor['head'].unitsPerEm
        added = 0
        for cp, glyph in sorted(donor.getBestCmap().items()):
            if cp in cmap:
                continue
            name = 'fallback%04X' % cp
            assert name not in result.getGlyphOrder()
            pen = TTGlyphPen(None)
            transformed(outline(donor, glyph), (ratio, 0, 0, ratio, 0, 0)).replay(pen)
            result['glyf'][name] = pen.glyph()
            advance, lsb = donor['hmtx'][glyph]
            result['hmtx'][name] = (round(advance * ratio), round(lsb * ratio))
            for table in result['cmap'].tables:
                if table.isUnicode() and table.format in (4, 12):
                    table.cmap[cp] = name
            added += 1
        result['OS/2'].recalcUnicodeRanges(result)
        result['name'].setName('Additional glyphs: Pixelify Sans, SIL Open Font License 1.1; see crawl-chinese-fonts.OFL.txt. Original font notices retained.', 13, 3, 1, 0x409)
        result.recalcTimestamp = False
        result.save(OUT / source.name)
        check = TTFont(OUT / source.name)
        for cp, glyph in cmap.items():
            assert check.getBestCmap()[cp] == glyph
            assert check['hmtx'][glyph] == base['hmtx'][glyph]
            assert outline(check, glyph).value == outline(base, glyph).value
        for table, fields in [('hhea', ('ascent','descent','lineGap')), ('OS/2', ('sTypoAscender','sTypoDescender','sTypoLineGap','usWinAscent','usWinDescent'))]:
            for field in fields:
                assert getattr(check[table],field) == getattr(base[table],field)
        required = '中文张三你好_ЁёÁÉÍÓÚÜÑáéíóúüñ¿¡' + ''.join(map(chr, range(0x410,0x450)))
        assert all(ord(c) in check.getBestCmap() for c in required)
        print(source.stem, added, 'added; existing glyphs, advances and metrics preserved')

if __name__ == '__main__':
    main()
