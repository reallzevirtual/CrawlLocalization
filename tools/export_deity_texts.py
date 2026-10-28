"""Export deity selection display text and audited layouts; requires UnityPy + TypeTreeGeneratorAPI."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


def collect(game):
    gen = TypeTreeGenerator('5.4.2f2')
    gen.load_local_game(str(game))
    files = {}
    for name in ('globalgamemanagers.assets', 'sharedassets0.assets', 'level2'):
        env = UnityPy.load((game / 'Crawl_Data' / name).read_bytes())
        files[name] = {obj.path_id: obj for obj in env.objects}
    scripts = {pid: obj.read_typetree()['m_ClassName'] for pid, obj in files['globalgamemanagers.assets'].items() if obj.type.name == 'MonoScript'}
    entries, layouts = [], []

    def add(text, kind, source):
        if text:
            entries.append(dict(text=text, kind=kind, source=source))

    for name, objects in files.items():
        games = {pid: o.read_typetree() for pid, o in objects.items() if o.type.name == 'GameObject'}
        transforms = {pid: o.read_typetree() for pid, o in objects.items() if o.type.name == 'Transform'}
        by_game = {t['m_GameObject']['m_PathID']: t for t in transforms.values()}
        for pid, obj in objects.items():
            if obj.type.name != 'MonoBehaviour':
                continue
            header = obj.read_typetree(check_read=False)
            cls = scripts[header['m_Script']['m_PathID']]
            if cls not in ('SystemDeity', 'MenuDeitySelectPlayer'):
                continue
            tree = obj.read_typetree(gen.get_nodes_up('Assembly-CSharp', cls))
            source = f'{name}:{pid}'
            if cls == 'SystemDeity':
                # Default deity is not a choice in the selection menu.
                for index, deity in enumerate(tree['m_deities']):
                    where = f'{source}/m_deities[{index}] ({deity["m_name"]}, {"trial" if deity["m_isTrial"] else "deity"})'
                    for key, kind in (('m_name', 'name'), ('m_text', 'effect'), ('m_textFlavour', 'flavour')):
                        add(deity[key], kind, where + '/' + key)
            else:
                for key, kind in (('m_textRandomName', 'name'), ('m_textRandomDescription', 'effect'), ('m_textTrialName', 'name'), ('m_textTrialDescription', 'effect')):
                    add(tree[key], kind, source + '/' + key)
                # These two strings are literals in MenuDeitySelectPlayer.SetLockedDeity.
                add('Locked', 'name', 'MenuDeitySelectPlayer.SetLockedDeity')
                add('Deity\nnot yet discovered', 'effect', 'MenuDeitySelectPlayer.SetLockedDeity')
                for index, override in enumerate(tree['m_deityOverrides']):
                    for key, kind in (('m_deityName', 'name'), ('m_deityText', 'flavour')):
                        add(override[key], kind, f'{source}/m_deityOverrides[{index}]/{key}')
                for key, kind in (('m_name', 'name'), ('m_text', 'effect'), ('m_textFlavour', 'flavour')):
                    ref = tree[key]
                    assert ref['m_FileID'] == 0
                    target = objects[ref['m_PathID']]
                    data = target.read_typetree(check_read=False)
                    gid = data['m_GameObject']['m_PathID']
                    width = 0
                    if target.type.name == 'MonoBehaviour':
                        width = target.read_typetree(gen.get_nodes_up('Assembly-CSharp', 'GUIText'))['m_wrapWidth']
                    mesh_id = next(r['m_PathID'] for _, r in games[gid]['m_Component'] if objects[r['m_PathID']].type.name == 'TextMesh')
                    mesh = objects[mesh_id].read_typetree()
                    names, scale = [], 1.0
                    while gid:
                        names.append(games[gid]['m_Name'])
                        t = by_game[gid]
                        scale *= t['m_LocalScale']['x']
                        parent = t['m_Father']['m_PathID']
                        gid = transforms[parent]['m_GameObject']['m_PathID'] if parent else 0
                    font_ref = mesh['m_Font']
                    font_file = name if font_ref['m_FileID'] == 0 else Path(obj.assets_file.externals[font_ref['m_FileID'] - 1].path).name
                    font = files[font_file][font_ref['m_PathID']].read_typetree()['m_Name']
                    layouts.append(dict(kind=kind, source=f'{name}:{mesh_id}', path='/'.join(reversed(names)), font=font, size=mesh['m_FontSize'], character_size=mesh['m_CharacterSize'], line_spacing=mesh['m_LineSpacing'], scale_x=scale, width=width, anchor=mesh['m_Anchor'], alignment=mesh['m_Alignment']))
    return dict(entries=entries, layouts=layouts)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-path', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--cases-output', type=Path)
    args = parser.parse_args()
    result = collect(args.game_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    if args.cases_output:
        args.cases_output.parent.mkdir(parents=True, exist_ok=True)
        args.cases_output.write_text(json.dumps([e['text'] for e in result['entries']], ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Exported {len(result["entries"])} display cases and {len(result["layouts"])} label layouts.')
