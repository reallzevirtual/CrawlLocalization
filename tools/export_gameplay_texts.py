"""Audit gameplay TextMeshes, action strings and sprite-only feedback (no game execution)."""
import argparse
import json
import re
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode


def collect(game):
    data = game / 'Crawl_Data'
    gen = TypeTreeGenerator('5.4.2f2')
    gen.load_local_game(str(game))
    scripts = {o.path_id: o.read_typetree() for o in UnityPy.load((data/'globalgamemanagers.assets').read_bytes()).objects if o.type.name == 'MonoScript'}
    schemas, objects_by_file = {}, {}
    def schema(cls, assembly="Assembly-CSharp"):
        key = (assembly, cls)
        if key not in schemas:
            nodes = []
            for n in gen.get_nodes_up(assembly, cls).traverse():
                row = {k:v for k,v in n.to_dict().items() if k != 'm_Children'}
                if n.m_Type == 'string' and n.m_Children and n.m_Children[0].m_Children[1].m_Type == 'string': row['m_Type'] = 'vector'
                if n.m_Name == 'm_Enabled': row['m_MetaFlag'] = 16384
                nodes.append(row)
            schemas[key] = TypeTreeNode.from_list(nodes)
        return schemas[key]
    meshes, actions, pictures = [], [], []
    atlases = {}
    for filename in ['sharedassets0.assets', 'sharedassets4.assets', 'level4']:
        obs = {o.path_id:o for o in UnityPy.load((data/filename).read_bytes()).objects}
        objects_by_file[filename] = obs
        games = {k:o.read_typetree() for k,o in obs.items() if o.type.name == 'GameObject'}
        ts = {k:o.read_typetree() for k,o in obs.items() if o.type.name == 'Transform'}
        by_game = {t['m_GameObject']['m_PathID']:t for t in ts.values()}
        classes = {}
        for pid, obj in obs.items():
            if obj.type.name != 'MonoBehaviour': continue
            h = obj.read_typetree(check_read=False)
            cls = scripts[h['m_Script']['m_PathID']]['m_ClassName']
            classes[pid] = cls
            if cls in ('PropActionable', 'PropSign', 'PropStoreTeleporter'):
                tree = obj.read_typetree(schema(cls))
                actions.append(dict(source=f'{filename}:{pid}', cls=cls, owner=games[h['m_GameObject']['m_PathID']]['m_Name'], text=tree['m_actionText']))
        def hierarchy(gid):
            names, scale = [], 1.0
            while gid:
                names.append(games[gid]['m_Name'])
                t = by_game[gid]; scale *= t['m_LocalScale']['x']
                parent = t['m_Father']['m_PathID']; gid = ts[parent]['m_GameObject']['m_PathID'] if parent else 0
            return '/'.join(reversed(names)), scale
        for obj in obs.values():
            if obj.type.name != 'TextMesh': continue
            tree = obj.read_typetree(); gid = tree['m_GameObject']['m_PathID']; path, scale = hierarchy(gid)
            if path.startswith('Menu'): continue
            ref = tree['m_Font']
            ff = filename if not ref['m_FileID'] else Path(obj.assets_file.externals[ref['m_FileID']-1].path).name
            font = objects_by_file[ff][ref['m_PathID']].read_typetree()['m_Name'] if ref['m_PathID'] else None
            meshes.append(dict(source=f'{filename}:{obj.path_id}', path=path, text=tree['m_Text'], font=font, size=tree['m_FontSize'], character_size=tree['m_CharacterSize'], line_spacing=tree['m_LineSpacing'], scale_x=scale, anchor=tree['m_Anchor'], alignment=tree['m_Alignment'], components=[classes[p['m_PathID']] for _, p in games[gid]['m_Component'] if p['m_PathID'] in classes]))
        for gid, g in games.items():
            if not re.match(r'^(LevelUp(Feedback|L\d.*)|StatUp(Health|Speed|Strength|Magic)|PlusWrathSmall.*|HealFeedback|FX_Collect_AlteredBeast.*)$', g['m_Name']): continue
            path, _ = hierarchy(gid)
            components = [dict(source=f'{filename}:{p["m_PathID"]}', cls=classes[p['m_PathID']]) for _,p in g['m_Component'] if p['m_PathID'] in classes]
            clips = []
            for _, ref in g['m_Component']:
                cid = ref['m_PathID']
                if classes.get(cid) != 'exSpriteAnimation': continue
                animation = obs[cid].read_typetree(schema('exSpriteAnimation', 'ex2D.Runtime'))
                for clip_ref in animation['animations']:
                    assert clip_ref['m_FileID'] == 0
                    clip_id = clip_ref['m_PathID']
                    clip = obs[clip_id].read_typetree(schema('exSpriteAnimClip', 'ex2D.Runtime'))
                    clips.append(dict(source=f'{filename}:{clip_id}', name=clip['m_Name'], frames=[dict(atlas=frame['atlas'], index=frame['index'], duration=frame['length']) for frame in clip['frameInfos']]))
            sprites = []
            for child_gid, child in games.items():
                child_path, _ = hierarchy(child_gid)
                if child_path != path and not child_path.startswith(path + '/'):
                    continue
                for _, ref in child['m_Component']:
                    cid = ref['m_PathID']
                    if classes.get(cid) != 'exSprite':
                        continue
                    sprite = obs[cid].read_typetree(schema('exSprite', 'ex2D.Runtime'))
                    sprites.append(dict(source=f'{filename}:{cid}', path=child_path,
                                        atlas=sprite['atlas_'], index=sprite['index_']))
            pictures.append(dict(source=f'{filename}:{gid}', path=path, components=components, clips=clips, sprites=sprites))
    for picture in pictures:
        refs = list(picture['sprites']) + [frame for clip in picture['clips'] for frame in clip['frames']]
        for ref in refs:
            atlas_ref = ref['atlas']
            assert atlas_ref['m_FileID'] == 0
            filename = picture['source'].split(':')[0]
            atlas_id = atlas_ref['m_PathID']
            key = (filename, atlas_id)
            if key not in atlases:
                atlases[key] = objects_by_file[filename][atlas_id].read_typetree(schema('exAtlas', 'ex2D.Runtime'))
            ref['element'] = atlases[key]['elements'][ref['index']]
    return dict(meshes=meshes, actions=actions, pictures=pictures)


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game-path',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
    a=p.parse_args(); result=collect(a.game_path)
    a.output.parent.mkdir(parents=True,exist_ok=True)
    a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Exported {len(result["meshes"])} non-menu TextMeshes, {len(result["actions"])} action sources, {len(result["pictures"])} sprite feedback objects.')
