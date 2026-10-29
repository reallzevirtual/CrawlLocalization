"""Write hash-bound cmap coverage for a built bundle (Unity may report OS fallback glyphs)."""
import argparse, hashlib, io
from pathlib import Path
import UnityPy
from fontTools.ttLib import TTFont

def coverage(bundle):
    rows=['sha256\t'+hashlib.sha256(bundle.read_bytes()).hexdigest()]
    env=UnityPy.load(str(bundle))
    for path,obj in sorted(env.container.items(), key=lambda item: item[0]):
        if obj.type.name!='Font': continue
        font=TTFont(io.BytesIO(bytes(obj.read_typetree()['m_FontData'])))
        points=sorted(cp for cp in font.getBestCmap() if cp<=0xffff)
        ranges=[]
        for cp in points:
            if ranges and cp==ranges[-1][1]+1: ranges[-1][1]=cp
            else: ranges.append([cp,cp])
        rows.append(path+'\t'+','.join('%04X-%04X'%tuple(r) for r in ranges))
    return '\n'.join(rows)+'\n'

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('bundles',nargs='+',type=Path)
    parser.add_argument('--check',action='store_true')
    args=parser.parse_args()
    for bundle in args.bundles:
        target=bundle.with_suffix('.coverage.tsv');text=coverage(bundle)
        if args.check:
            assert target.read_text(encoding='utf-8')==text, target
        else: target.write_text(text,encoding='utf-8')
        print('PASS coverage:',target)
