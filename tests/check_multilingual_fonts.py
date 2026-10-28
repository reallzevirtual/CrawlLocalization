"""Check the actual portable bundle, original geometry and any generated demo packs."""
import argparse, hashlib, io, json, sys
from pathlib import Path
import UnityPy
from fontTools.ttLib import TTFont
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from merge_multilingual_fonts import outline
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--plugin-root',type=Path,default=root)
args=parser.parse_args()
data=root/'Fonts/Multilingual'
metadata=json.loads((data/'original-layout.json').read_text())
env=UnityPy.load((args.plugin_root/'Fonts/crawl-multilingual.bundle').read_bytes())
assets={path:obj for path,obj in env.container.items() if obj.type.name=='Font'}
fonts={obj.read_typetree()['m_Name']:obj for obj in env.objects if obj.type.name=='Font'}
assert len(fonts)==10 and len(assets)==10
required=set((data/'coverage.txt').read_text(encoding='utf-8'))
for locale in ('ru','es'):
    for table in (args.plugin_root/'Languages'/locale).glob('*translations.tsv'):
        for line in table.read_text(encoding='utf-8-sig').splitlines():
            if line and not line.startswith('#'):
                target=line.split('\t')[1]
                if target!='@TODO': required.update(target)
for name,obj in fonts.items():
    t=obj.read_typetree()
    assert str(obj.assets_file.unity_version)=='5.4.2f2'
    assert int(obj.assets_file.target_platform)==5
    assert t['m_FontData'] and not t['m_CharacterRects']
    assert bytes(t['m_FontData'])==(data/'Generated'/(name+'.ttf')).read_bytes()
    for key,value in metadata[name]['fields'].items():
        assert t[key]==value,(name,key,t[key],value)
    assert assets['assets/fonts/'+name.lower()+'.ttf'].path_id==obj.path_id
    font=TTFont(io.BytesIO(bytes(t['m_FontData'])))
    old=TTFont(data/'Original'/(name+'.ttf'))
    for cp,gn in old.getBestCmap().items():
        assert font.getBestCmap()[cp]==gn
        assert font['hmtx'][gn]==old['hmtx'][gn]
        assert outline(font,gn).value==outline(old,gn).value
    missing=[hex(ord(c)) for c in required if not c.isspace() and ord(c) not in font.getBestCmap()]
    assert not missing,(name,missing)
    for table,keys in [('head',['unitsPerEm']),('hhea',['ascent','descent','lineGap']),('OS/2',['sTypoAscender','sTypoDescender','sTypoLineGap','usWinAscent','usWinDescent'])]:
        for key in keys: assert getattr(font[table],key)==getattr(old[table],key),(name,table,key)
print('PASS: 10 portable fonts; original outlines/advances/TTF metrics and all Unity scalar properties preserved; Russian/Spanish coverage verified.')
