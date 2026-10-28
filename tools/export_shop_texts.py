"""Export shop source components and label layouts; requires UnityPy + TypeTreeGeneratorAPI."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode


def collect(game):
    gen = TypeTreeGenerator('5.4.2f2')
    gen.load_local_game(str(game))
    env = UnityPy.load((game / 'Crawl_Data/sharedassets0.assets').read_bytes())
    objects = {o.path_id: o for o in env.objects}
    scripts = {o.path_id: o.read_typetree()['m_ClassName'] for o in UnityPy.load((game / 'Crawl_Data/globalgamemanagers.assets').read_bytes()).objects if o.type.name == 'MonoScript'}
    schemas = {}
    def schema(cls):
        if cls not in schemas:
            nodes = []
            for node in gen.get_nodes_up('Assembly-CSharp', cls).traverse():
                row = {k: v for k, v in node.to_dict().items() if k != 'm_Children'}
                # Generated string[] nodes incorrectly have root type string.
                if node.m_Type == 'string' and node.m_Children and node.m_Children[0].m_Children[1].m_Type == 'string':
                    row['m_Type'] = 'vector'
                if node.m_Name == 'm_Enabled':
                    row['m_MetaFlag'] = 16384
                nodes.append(row)
            schemas[cls] = TypeTreeNode.from_list(nodes)
        return schemas[cls]
    records = []
    for pid, obj in objects.items():
        if obj.type.name != 'MonoBehaviour':
            continue
        header = obj.read_typetree(check_read=False)
        cls = scripts[header['m_Script']['m_PathID']]
        if cls in ('Constants', 'GUIStore', 'GUIStoreStatsDisplay', 'GUIHeroStats') or cls.startswith(('Weapon', 'Powerup', 'Status')) and cls != 'StatusZone':
            tree = obj.read_typetree(schema(cls))
            if cls == 'Constants':
                tree = {k: v for k, v in tree.items() if k.startswith('m_weaponNames') or k in ('m_weaponMagicDescription', 'm_heroStats')}
            elif cls == 'Weapon':
                tree = {k: v for k, v in tree.items() if k in ('m_weaponName', 'm_weaponDescription', 'm_combos', 'm_shouldBotPurchase')}
                tree['m_combos'] = [{k: c[k] for k in ('m_name', 'm_description')} for c in tree['m_combos']]
            elif cls.startswith('Powerup'):
                tree = {k: v for k, v in tree.items() if k in ('m_displayName', 'm_displayNameHud', 'm_description', 'm_powerupType', 'm_heroStatModifiers', 'm_shouldBotPurchase')}
            elif cls.startswith('Status'):
                tree = {k: v for k, v in tree.items() if k in ('m_description', 'm_weaponNames')}
            elif cls == 'WeaponMagic':
                tree = {'m_displayName': tree['m_displayName']}
            records.append(dict(source=f'sharedassets0.assets:{pid}', cls=cls, data=tree))
    layouts = []
    for filename in ('sharedassets0.assets', 'sharedassets4.assets', 'level4'):
        ui_env = UnityPy.load((game / 'Crawl_Data' / filename).read_bytes())
        ui = {o.path_id: o for o in ui_env.objects}
        games = {pid: o.read_typetree() for pid, o in ui.items() if o.type.name == 'GameObject'}
        transforms = {pid: o.read_typetree() for pid, o in ui.items() if o.type.name == 'Transform'}
        by_game = {t['m_GameObject']['m_PathID']: t for t in transforms.values()}
        roots = {}
        for pid, obj in ui.items():
            if obj.type.name != 'MonoBehaviour':
                continue
            header = obj.read_typetree(check_read=False)
            cls = scripts[header['m_Script']['m_PathID']]
            if cls in ('GUIStore', 'GUIStoreStatsDisplay', 'GUIHeroStats', 'GUIArtifactIcon', 'GUIActionIndicator'):
                tree = obj.read_typetree(schema(cls))
                roots[header['m_GameObject']['m_PathID']] = cls
                records.append(dict(source=f'{filename}:{pid}', cls=cls, data=tree))
        for obj in ui.values():
            if obj.type.name != 'TextMesh':
                continue
            mesh = obj.read_typetree()
            gid, names, scale, scopes = mesh['m_GameObject']['m_PathID'], [], 1.0, []
            while gid:
                names.append(games[gid]['m_Name'])
                if gid in roots:
                    scopes.append(roots[gid])
                t = by_game[gid]
                scale *= t['m_LocalScale']['x']
                parent = t['m_Father']['m_PathID']
                gid = transforms[parent]['m_GameObject']['m_PathID'] if parent else 0
            if not scopes:
                continue
            font_ref = mesh['m_Font']
            font_file = filename if font_ref['m_FileID'] == 0 else Path(obj.assets_file.externals[font_ref['m_FileID'] - 1].path).name
            font_objects = ui if font_file == filename else objects
            font = font_objects[font_ref['m_PathID']].read_typetree()['m_Name'] if font_ref['m_PathID'] else None
            layouts.append(dict(source=f'{filename}:{obj.path_id}', path='/'.join(reversed(names)), text=mesh['m_Text'], font=font, size=mesh['m_FontSize'], character_size=mesh['m_CharacterSize'], line_spacing=mesh['m_LineSpacing'], scale_x=scale, anchor=mesh['m_Anchor'], alignment=mesh['m_Alignment'], scopes=scopes))
    return dict(records=records, layouts=layouts)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-path', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = collect(args.game_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Exported {len(result["records"])} shop-related components and {len(result["layouts"])} label layouts.')
