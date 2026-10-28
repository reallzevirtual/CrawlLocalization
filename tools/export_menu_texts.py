"""Export menu TextMesh evidence without running Crawl; requires UnityPy."""
import argparse
import json
from pathlib import Path
import UnityPy


def collect(game):
    rows = []
    for filename in ('sharedassets0.assets', 'sharedassets2.assets', 'level2', 'level3'):
        env = UnityPy.load((game / 'Crawl_Data' / filename).read_bytes())
        objects = {o.path_id: o for o in env.objects}
        games = {i: o.read_typetree() for i, o in objects.items() if o.type.name == 'GameObject'}
        transforms = {i: o.read_typetree() for i, o in objects.items() if o.type.name == 'Transform'}
        by_game = {t['m_GameObject']['m_PathID']: t for t in transforms.values()}
        for obj in objects.values():
            if obj.type.name != 'TextMesh':
                continue
            tree = obj.read_typetree()
            gid = tree['m_GameObject']['m_PathID']
            names, seen = [], set()
            while gid and gid not in seen:
                seen.add(gid)
                names.append(games.get(gid, {}).get('m_Name', str(gid)))
                parent = by_game.get(gid, {}).get('m_Father', {}).get('m_PathID', 0)
                gid = transforms.get(parent, {}).get('m_GameObject', {}).get('m_PathID', 0)
            hierarchy = '/'.join(reversed(names))
            if hierarchy.startswith('Menu') and tree['m_Text'].strip():
                rows.append(dict(asset=filename, path_id=obj.path_id, path=hierarchy, text=tree['m_Text']))
    return rows


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-path', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    rows = collect(args.game_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Exported {len(rows)} menu TextMeshes to {args.output}')
