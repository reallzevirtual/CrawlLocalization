"""Build extended Crawl TTFs without changing any existing cmap glyph or advance.
Inputs are kept in Fonts/Multilingual; no game files are ever modified.
"""
from pathlib import Path
import argparse, copy, hashlib, json, unicodedata, shutil
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.reverseContourPen import ReverseContourPen
from fontTools.pens.boundsPen import BoundsPen

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'Fonts/Multilingual'
WEIGHTS = {
    'CrawlLarge':'Bold', 'CrawlInvLarge':'Bold', 'CrawlMedium':'Bold',
    'CrawlSmall':'Bold', 'CrawlTiny':'Bold', 'CrawlTextBodyTeensy':'SemiBold',
    'CrawlTextBodyThin':'Medium', 'CrawlTextBodyWide':'Medium',
    '04B_04-Black':'Medium', '04B_03-White':'Medium',
}

def outline(font, name):
    pen = DecomposingRecordingPen(font.getGlyphSet())
    font.getGlyphSet()[name].draw(pen)
    return pen

def bounds(record):
    pen = BoundsPen(None); record.replay(pen)
    return pen.bounds

def transformed(record, matrix):
    pen = DecomposingRecordingPen({}); record.replay(TransformPen(pen, matrix)); return pen

def merge(name):
    original = TTFont(DATA/'Original'/(name+'.ttf'))
    result = copy.deepcopy(original)
    donor = TTFont(DATA/'Donor'/('PixelifySans-'+WEIGHTS[name]+'.ttf'))
    # The inverse style is generated from positive outlines and surrounded with
    # an opposite-winding background, matching Crawl's original inverse glyphs.
    reference = TTFont(DATA/'Original'/'CrawlLarge.ttf') if name=='CrawlInvLarge' else original
    cmap, refmap, donor_map = result.getBestCmap(), reference.getBestCmap(), dict(donor.getBestCmap())
    # This release omits Cyrillic O/Pe/I despite containing identical O/Pi/I.
    for cp, equivalent in {0x041e:0x004f, 0x041f:0x03a0, 0x0406:0x0049}.items():
        donor_map.setdefault(cp, donor_map[equivalent])
    bh, bx = bounds(outline(reference,refmap[ord('H')])), bounds(outline(reference,refmap[ord('x')]))
    dh, dx = bounds(outline(donor,donor_map[ord('H')])), bounds(outline(donor,donor_map[ord('x')]))
    sx = (bh[2]-bh[0])/(dh[2]-dh[0])
    sy = (bh[3]-bh[1])/(dh[3]-dh[1])
    # A lowercase body uses the original x-height; ascenders/accents stay intact.
    lower_sy = (bx[3]-bx[1])/(dx[3]-dx[1])
    gap = reference['hmtx'][refmap[ord('H')]][0] - bh[2]
    pixel = reference['head'].unitsPerEm / (8 if name.startswith('04B_') else 16)
    added = {}
    for cp, donor_name in sorted(donor_map.items()):
        if cp in cmap: continue
        char = chr(cp)
        decomposition = unicodedata.normalize('NFD',char)
        # Accented Latin letters retain the game's base letter, width and spacing.
        composed = (len(decomposition)>1 and ord(decomposition[0]) in refmap
                    and 'LATIN' in unicodedata.name(char,'')
                    and all(ord(c) in donor_map for c in decomposition[1:]))
        if composed:
            base_name = refmap[ord(decomposition[0])]
            rec = outline(reference,base_name)
            base_bounds = bounds(rec)
            advance = reference['hmtx'][base_name][0]
            top, bottom = base_bounds[3], base_bounds[1]
            for mark in decomposition[1:]:
                mr = outline(donor,donor_map[ord(mark)]); mb = bounds(mr)
                if not mb: continue
                ms = pixel/(mb[3]-mb[1])
                mx = min(sx, max(pixel,base_bounds[2]-base_bounds[0])/(mb[2]-mb[0]))
                tx = (base_bounds[0]+base_bounds[2])/2 - (mb[0]+mb[2])*mx/2
                if unicodedata.combining(mark) in (202,220):
                    ty=bottom-pixel*0.5-mb[3]*ms; bottom-=pixel*1.5
                else:
                    ty=top+pixel*0.5-mb[1]*ms; top+=pixel*1.5
                transformed(mr,(mx,0,0,ms,tx,ty)).replay(rec)
        else:
            rec = outline(donor,donor_name); bb=bounds(rec)
            ys = lower_sy if char.islower() else sy
            # Keep unaccented ascenders (e.g. Russian be/ef) within the original
            # cap height, especially in tiny fonts whose x-height equals caps.
            if bb and char.islower() and len(decomposition)==1 and bb[3] > dx[3]+1:
                ys = sy
            # Use the original baseline rather than the donor's -11/-12 overshoot.
            yoffset = bx[1]-dx[1]*ys if char.islower() else bh[1]-dh[1]*ys
            if bb:
                rec = transformed(rec,(sx,0,0,ys,-bb[0]*sx,yoffset))
                advance = round((bb[2]-bb[0])*sx+gap)
            else:
                advance = round(donor['hmtx'][donor_name][0]*sx)
            if unicodedata.combining(char): advance=0
        bb=bounds(rec)
        if name=='CrawlInvLarge' and bb and not unicodedata.combining(char):
            inverse=DecomposingRecordingPen({})
            x0,y0,x1,y1=bb
            # Clockwise backdrop, counterclockwise letter: nonzero winding holes.
            inverse.moveTo((x0-pixel,y0-pixel));inverse.lineTo((x0-pixel,y1+pixel))
            inverse.lineTo((x1+pixel,y1+pixel));inverse.lineTo((x1+pixel,y0-pixel));inverse.closePath()
            rec.replay(ReverseContourPen(inverse));rec=inverse;bb=bounds(rec)
        glyph_name='ml%04X'%cp
        pen=TTGlyphPen(None);rec.replay(pen)
        result['glyf'][glyph_name]=pen.glyph()
        result['hmtx'][glyph_name]=(advance,round(bb[0]) if bb else 0)
        for table in result['cmap'].tables:
            if table.isUnicode() and (table.format in (4,12)):
                table.cmap[cp]=glyph_name
        added[cp]=glyph_name
    family='Crawl Multi '+name
    for nid,value in [(1,family),(2,'Regular'),(3,family+' 1.0'),(4,family),(6,'CrawlMulti-'+name)]:
        for rec in result['name'].names:
            if rec.nameID==nid: rec.string=value.encode(rec.getEncoding())
    result['name'].setName('Original font: '+(original['name'].getDebugName(0) or '')+'; additional glyphs: Copyright 2021 The Pixelify Sans Project Authors.',0,3,1,0x409)
    result['name'].setName('Additional Pixelify Sans glyphs: SIL Open Font License 1.1; see PixelifySans-OFL.txt. Original Crawl glyph notices retained.',13,3,1,0x409)
    result['OS/2'].recalcUnicodeRanges(result)
    output=DATA/'Generated'/(name+'.ttf');result.recalcTimestamp=False;result.save(output)
    check=TTFont(output)
    assert set(original.getBestCmap()).issubset(check.getBestCmap())
    for cp,gn in original.getBestCmap().items():
        assert check.getBestCmap()[cp]==gn
        assert check['hmtx'][gn]==original['hmtx'][gn]
        assert outline(check,gn).value==outline(original,gn).value, (name,cp)
    for key in ['ascent','descent','lineGap']:
        assert getattr(check['hhea'],key)==getattr(original['hhea'],key)
    for key in ['sTypoAscender','sTypoDescender','sTypoLineGap','usWinAscent','usWinDescent']:
        assert getattr(check['OS/2'],key)==getattr(original['OS/2'],key)
    required='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 _'+''.join(chr(c) for c in range(0x410,0x450))+'ЁёÁÉÍÓÚÜÑáéíóúüñ¿¡'
    assert all(ord(c) in check.getBestCmap() for c in required)
    print(name,WEIGHTS[name],len(added),'added; original glyphs/advances/metrics preserved')
    return {'weight':WEIGHTS[name],'added':len(added),'original_sha256':hashlib.sha256((DATA/'Original'/(name+'.ttf')).read_bytes()).hexdigest(),'scale_x':sx,'scale_y_caps':sy,'scale_y_lower':lower_sy,'sha256':hashlib.sha256(output.read_bytes()).hexdigest()}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-data',type=Path,help='Explicitly refresh original fonts from Crawl_Data')
    parser.add_argument('--pixelify',type=Path,help='Explicitly refresh static donor fonts and OFL.txt')
    args=parser.parse_args()
    if args.game_data:
        import UnityPy
        metadata={}
        for filename, ids in [('sharedassets0.assets',[1076,1077,1078,1079,1080,1081,1082,1083,1085]),('sharedassets2.assets',[18])]:
            for obj in UnityPy.load(str(args.game_data/filename)).objects:
                if obj.path_id not in ids: continue
                tree=obj.read_typetree();name=tree['m_Name']
                if name not in WEIGHTS: raise ValueError('Unexpected font at asset ID: '+name)
                (DATA/'Original'/(name+'.ttf')).write_bytes(bytes(tree['m_FontData']))
                metadata[name]={'asset':filename,'path_id':obj.path_id,'fields':{k:v for k,v in tree.items() if isinstance(v,(str,int,float,bool))}}
        if set(metadata)!=set(WEIGHTS): raise ValueError('Missing original fonts')
        (DATA/'original-layout.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')
    if args.pixelify:
        for p in (args.pixelify/'static').glob('*.ttf'): shutil.copy2(p,DATA/'Donor'/p.name)
        shutil.copy2(args.pixelify/'OFL.txt',DATA/'PixelifySans-OFL.txt')
    (DATA/'Generated').mkdir(exist_ok=True)
    report={name:merge(name) for name in WEIGHTS}
    (DATA/'merge-report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')

if __name__=='__main__':main()
