"""Export chapter headings and floor-name pools from Crawl 5.4 assets; requires UnityPy."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.streams import EndianBinaryReader


def collect(game):
    data = game / 'Crawl_Data'
    env = UnityPy.load((data / 'sharedassets0.assets').read_bytes())
    objects = {o.path_id: o for o in env.objects}
    scripts_env = UnityPy.load((data / 'globalgamemanagers.assets').read_bytes())
    scripts = {o.path_id: o.read_typetree()['m_ClassName'] for o in scripts_env.objects if o.type.name == 'MonoScript'}
    games = {pid: o.read_typetree() for pid, o in objects.items() if o.type.name == 'GameObject'}
    transforms = {pid: o.read_typetree() for pid, o in objects.items() if o.type.name == 'Transform'}
    by_game = {t['m_GameObject']['m_PathID']: t for t in transforms.values()}
    entries, layouts, legacy, environments = [], [], [], []

    def strings(reader):
        count = reader.read_int()
        if not 0 <= count <= 1000:
            raise ValueError('Invalid string array length')
        return [reader.read_aligned_string() for _ in range(count)]

    def pointer(reader):
        return dict(m_FileID=reader.read_int(), m_PathID=reader.read_long())

    for pid, obj in objects.items():
        if obj.type.name != 'MonoBehaviour':
            continue
        header = obj.read_typetree(check_read=False)
        cls = scripts[header['m_Script']['m_PathID']]
        if cls not in ('Environment', 'LevelTitle'):
            continue
        # TypeTreeGenerator mislabels string[] as string in these classes.
        # Read the audited serialized field order, with aligned strings and full consumption checks.
        raw = obj.get_raw_data()
        reader = EndianBinaryReader(raw, endian='<')
        assert pointer(reader) == header['m_GameObject']
        reader.read_boolean()
        reader.align_stream()
        assert pointer(reader) == header['m_Script']
        assert reader.read_aligned_string() == header['m_Name']
        source = f'sharedassets0.assets:{pid}'
        if cls == 'Environment':
            environment = reader.read_aligned_string()
            # music pointer, Color, five room groups, store and portal pointers
            reader.read_bytes(12 + 16 + 7 * 12)
            doors = reader.read_int()
            if not 0 <= doors <= 1000:
                raise ValueError('Invalid door count')
            # doors, stairs/traps/locks, four props, store teleporter, sign tips
            reader.read_bytes(doors * 12 + 9 * 12)
            names = strings(reader)
            environments.append(dict(source=source, name=environment, count=len(names)))
            for index, name in enumerate(names):
                entries.append(dict(text=name, kind='floor', source=f'{source}/m_floorNames[{index}]', environment=environment))
        else:
            entries.append(dict(text=reader.read_aligned_string(), kind='chapter', source=source + '/m_stringLevelNum'))
            unused_player_text = strings(reader)  # Not read by LevelTitle.Start.
            legacy_names = strings(reader)  # Editor-only name pool; current names come from Environment.
            legacy.append(dict(source=source, player_text=unused_player_text, floor_names=legacy_names))
            for kind in ('chapter', 'floor'):
                ref = pointer(reader)
                assert ref['m_FileID'] == 0
                mesh = objects[ref['m_PathID']].read_typetree()
                mesh_game = games[mesh['m_GameObject']['m_PathID']]
                component_classes = []
                for _, component in mesh_game['m_Component']:
                    obj_component = objects[component['m_PathID']]
                    if obj_component.type.name == 'MonoBehaviour':
                        component_header = obj_component.read_typetree(check_read=False)
                        component_classes.append(scripts[component_header['m_Script']['m_PathID']])
                gid, names, scale = mesh['m_GameObject']['m_PathID'], [], 1.0
                while gid:
                    names.append(games[gid]['m_Name'])
                    t = by_game[gid]
                    scale *= t['m_LocalScale']['x']
                    parent = t['m_Father']['m_PathID']
                    gid = transforms[parent]['m_GameObject']['m_PathID'] if parent else 0
                font_ref = mesh['m_Font']
                assert font_ref['m_FileID'] == 0
                font = objects[font_ref['m_PathID']].read_typetree()['m_Name']
                layouts.append(dict(components=component_classes, kind=kind, source=f'sharedassets0.assets:{ref["m_PathID"]}', path='/'.join(reversed(names)), font=font, size=mesh['m_FontSize'], character_size=mesh['m_CharacterSize'], line_spacing=mesh['m_LineSpacing'], scale_x=scale, anchor=mesh['m_Anchor'], alignment=mesh['m_Alignment']))
        assert reader.Position == len(raw), f'{source}: serialized layout mismatch'
    return dict(entries=entries, layouts=layouts, environments=environments, legacy=legacy)


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
        cases = []
        for entry in result['entries']:
            cases.extend([entry['text'].format(n) for n in (1, 10, 99)] if entry['kind'] == 'chapter' else [entry['text']])
        args.cases_output.parent.mkdir(parents=True, exist_ok=True)
        args.cases_output.write_text(json.dumps(cases, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Exported {len(result["environments"])} environments, {len(result["entries"])} source entries and {len(result["layouts"])} layouts.')
