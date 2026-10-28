"""Export actual Vault display strings from Crawl assets; requires UnityPy + TypeTreeGeneratorAPI."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator


def collect(game):
    data = game / 'Crawl_Data'
    gen = TypeTreeGenerator('5.4.2f2')
    gen.load_local_game(str(game))
    files = {}
    for name in ('globalgamemanagers.assets', 'sharedassets0.assets', 'level2'):
        env = UnityPy.load((data / name).read_bytes())
        files[name] = {obj.path_id: obj for obj in env.objects}
    scripts = {key: obj.read_typetree()['m_ClassName'] for key, obj in files['globalgamemanagers.assets'].items() if obj.type.name == 'MonoScript'}
    cache = {}

    def resolve(owner, ref):
        name = owner if ref['m_FileID'] == 0 else Path(next(iter(files[owner].values())).assets_file.externals[ref['m_FileID'] - 1].path).name
        return name, ref['m_PathID']

    def read(owner, key):
        if (owner, key) in cache:
            return cache[owner, key]
        obj = files[owner][key]
        tree = obj.read_typetree(check_read=False)
        if obj.type.name == 'MonoBehaviour':
            cls = scripts[tree['m_Script']['m_PathID']]
            tree = obj.read_typetree(gen.get_nodes_up('Assembly-CSharp', cls))
        cache[owner, key] = tree
        return tree

    def components(owner, key):
        go = read(owner, key)
        for _, ref in go['m_Component']:
            name, pid = resolve(owner, ref)
            obj = files[name][pid]
            if obj.type.name != 'MonoBehaviour':
                continue
            header = obj.read_typetree(check_read=False)
            yield scripts[header['m_Script']['m_PathID']], name, pid

    def singleton(owner, cls):
        for obj in files[owner].values():
            if obj.type.name == 'MonoBehaviour':
                header = obj.read_typetree(check_read=False)
                if scripts[header['m_Script']['m_PathID']] == cls:
                    return obj.path_id, read(owner, obj.path_id)
        raise ValueError(cls)

    lib_id, library = singleton('sharedassets0.assets', 'SystemLibrary')
    entries = []
    for group, field in [('monsters', 'm_monsters'), ('weapons', 'm_weapons'), ('items', 'm_items'), ('objects', 'm_props')]:
        for index, entry in enumerate(library[field], 1):
            owner, pid = resolve('sharedassets0.assets', entry['m_prefab'])
            go = read(owner, pid)
            name, description = '', ''
            if group in ('weapons', 'items'):
                wanted = 'Weapon' if group == 'weapons' else 'Powerup'
                cls, comp_owner, comp_id = next(c for c in components(owner, pid) if c[0].startswith(wanted))
                comp = read(comp_owner, comp_id)
                if group == 'weapons':
                    name = entry['m_type'] or comp['m_weaponName']
                    combo = next((c for c in comp['m_combos'] if c['m_name'] == entry['m_type']), {})
                    description = combo.get('m_description') or comp['m_weaponDescription']
                    if description:
                        description += '.'
                else:
                    name, description = comp['m_displayName'], comp['m_description']
            name = entry['m_name'] or name or go['m_Name']
            description = entry['m_description'] or description
            entries.append(dict(category=group, index=index, name=name, description=description, source=f'sharedassets0.assets:{lib_id}/{field}[{index-1}]', prefab=go['m_Name']))
    ach_id, achievements = singleton('sharedassets0.assets', 'SystemAchievements')
    for index, ref in enumerate(achievements['m_achievementData'], 1):
        owner, pid = resolve('sharedassets0.assets', ref)
        entry = read(owner, pid)
        entries.append(dict(category='achievements', index=index, name=entry['m_displayName'], description=entry['m_description'], source=f'{owner}:{pid}', identifier=entry['m_name'], platforms=entry['m_platforms'], hidden=entry['m_hidden']))
    menu_id, menu = singleton('level2', 'MenuLibrary')
    templates = {key: value for key, value in menu.items() if key.startswith('m_string')}
    return dict(entries=entries, templates=templates, template_source=f'level2:{menu_id}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-path', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--cases-output', type=Path, help='Write runtime display cases for tests/test.ps1 -VaultCases')
    args = parser.parse_args()
    result = collect(args.game_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Exported {len(result["entries"])} Vault entries and {len(result["templates"])} templates.')

    if args.cases_output:
        cases = ['Undiscovered']
        for entry in result['entries']:
            if entry['category'] == 'monsters':
                if entry['description']:
                    cases.append(entry['description'])
                continue  # Monster names are internal prefab identifiers, not displayed.
            cases.extend([entry['name'], entry['description']])
            if entry['category'] != 'achievements':
                name = entry['name']
                if len(name) > 22:
                    name = name[:19] + '...'
                cases.append(f"{entry['index']:02d} {name}")
        for template in result['templates'].values():
            for values in ((0, 61, 0), (12, 66, 123), (1, 45, 9)):
                cases.append(template.format(*values))
        args.cases_output.parent.mkdir(parents=True, exist_ok=True)
        args.cases_output.write_text(json.dumps([case for case in cases if case], ensure_ascii=False, indent=2), encoding='utf-8')
        print(f'Wrote {len(cases)} runtime display cases.')
