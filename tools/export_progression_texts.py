"""Audit pause prompts, monster evolution and boss results without executing Crawl."""
import argparse
import json
import re
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode


def collect(game):
    data = game / 'Crawl_Data'
    gen = TypeTreeGenerator('5.4.2f2'); gen.load_local_game(str(game))
    files = {name: {o.path_id: o for o in UnityPy.load((data/name).read_bytes()).objects}
             for name in ['globalgamemanagers.assets', 'sharedassets0.assets', 'sharedassets2.assets', 'sharedassets5.assets', 'sharedassets9.assets', 'sharedassets10.assets', 'level5', 'level9', 'level10']}
    scripts = {k: o.read_typetree()['m_ClassName'] for k, o in files['globalgamemanagers.assets'].items() if o.type.name == 'MonoScript'}
    schemas = {}
    def schema(cls, assembly='Assembly-CSharp'):
        key = (assembly, cls)
        if key not in schemas:
            nodes = []
            for n in gen.get_nodes_up(assembly, cls).traverse():
                row = {k: v for k, v in n.to_dict().items() if k != 'm_Children'}
                if n.m_Type == 'string' and n.m_Children and n.m_Children[0].m_Children[1].m_Type == 'string': row['m_Type'] = 'vector'
                if n.m_Name == 'm_Enabled': row['m_MetaFlag'] = 16384
                nodes.append(row)
            schemas[key] = TypeTreeNode.from_list(nodes)
        return schemas[key]
    records, layouts, pictures = [], [], []
    required_clips, atlases = set(), {}
    for filename, obs in files.items():
        games = {k: o.read_typetree() for k, o in obs.items() if o.type.name == 'GameObject'}
        ts = {k: o.read_typetree() for k, o in obs.items() if o.type.name == 'Transform'}
        by_game = {t['m_GameObject']['m_PathID']: t for t in ts.values()}
        def hierarchy(gid):
            names, scale = [], 1.0
            while gid:
                names.append(games[gid]['m_Name']); t = by_game[gid]; scale *= t['m_LocalScale']['x']
                parent = t['m_Father']['m_PathID']; gid = ts[parent]['m_GameObject']['m_PathID'] if parent else 0
            return '/'.join(reversed(names)), scale
        classes = {}
        for pid, obj in obs.items():
            if obj.type.name != 'MonoBehaviour': continue
            head = obj.read_typetree(check_read=False); cls = scripts[head['m_Script']['m_PathID']]; classes[pid] = cls
            if cls not in ('Boss', 'BossBuriedStatue', 'BossDecomposing', 'BossHydra', 'BossInhabitableBody', 'BossTentacleBlob', 'Constants', 'SystemDeity', 'MenuButtonEvolve', 'MenuButtonEvolveMonster', 'MenuEvolvePlayerEvolve'): continue
            tree = obj.read_typetree(schema(cls))
            if cls.startswith('Boss'):
                tree = {k: tree[k] for k in ['m_displayName','m_heroWinEffect','m_bossWinEffect']}
                for key in ['m_heroWinEffect','m_bossWinEffect']:
                    ref = tree[key]
                    tree[key+'Name'] = games[ref['m_PathID']]['m_Name'] if ref['m_PathID'] and ref['m_FileID']==0 else ''
                    if ref['m_PathID'] and ref['m_FileID']==0:
                        for _, component in games[ref['m_PathID']]['m_Component']:
                            target = obs[component['m_PathID']]
                            if target.type.name != 'MonoBehaviour': continue
                            header = target.read_typetree(check_read=False)
                            if scripts[header['m_Script']['m_PathID']] != 'exSpriteAnimation': continue
                            animation = target.read_typetree(schema('exSpriteAnimation','ex2D.Runtime'))
                            for clip in animation['animations']:
                                assert clip['m_FileID'] == 0
                                required_clips.add((filename, clip['m_PathID']))
            elif cls == 'Constants': tree = {'m_bossKillerNames': tree['m_bossKillerNames']}
            elif cls == 'SystemDeity': tree = {'deities': [{k: d[k] for k in ['m_name','m_text','m_winText']} for d in [tree['m_defaultDeity']] + tree['m_deities']]}
            elif cls == 'MenuEvolvePlayerEvolve': tree = {k: tree[k] for k in ['m_stringLocked','m_stringCantAfford']}
            else: tree = {'m_displayText': tree['m_displayText']}
            records.append(dict(source=f'{filename}:{pid}', cls=cls, data=tree))
        for pid, obj in obs.items():
            if obj.type.name == 'TextMesh':
                t = obj.read_typetree(); gid = t['m_GameObject']['m_PathID']; path, scale = hierarchy(gid)
                if not (filename in ('level5','level9','level10') or path.startswith(('MenuPrompt/','EffectBoss'))): continue
                ref = t['m_Font']; ff = filename if not ref['m_FileID'] else Path(obj.assets_file.externals[ref['m_FileID']-1].path).name
                font = files[ff][ref['m_PathID']].read_typetree()['m_Name'] if ref['m_PathID'] else None
                components = [classes[r['m_PathID']] for _, r in games[gid]['m_Component'] if r['m_PathID'] in classes]
                width = 0
                for _, r in games[gid]['m_Component']:
                    if classes.get(r['m_PathID']) == 'GUIText': width = obs[r['m_PathID']].read_typetree(schema('GUIText'))['m_wrapWidth']
                layouts.append(dict(source=f'{filename}:{pid}', path=path, text=t['m_Text'], font=font, size=t['m_FontSize'], character_size=t['m_CharacterSize'], line_spacing=t['m_LineSpacing'], scale_x=scale, anchor=t['m_Anchor'], alignment=t['m_Alignment'], width=width, components=components))
            if classes.get(pid) == 'exSpriteAnimClip':
                head = obj.read_typetree(check_read=False)
                if (filename,pid) not in required_clips and not re.match(r'^(GUIBgrReady.*|GUIBgrUnready|FX_PeakReached.*|ButtonReady_.*)$', head['m_Name']): continue
                clip = obj.read_typetree(schema('exSpriteAnimClip','ex2D.Runtime'))
                frames = []
                for frame in clip['frameInfos']:
                    ref = frame['atlas']; ff = filename if not ref['m_FileID'] else Path(obj.assets_file.externals[ref['m_FileID']-1].path).name
                    key = (ff,ref['m_PathID'])
                    if key not in atlases: atlases[key] = files[ff][ref['m_PathID']].read_typetree(schema('exAtlas','ex2D.Runtime'))
                    atlas = atlases[key]
                    frames.append(dict(atlas=f'{ff}:{ref["m_PathID"]}',index=frame['index'],duration=frame['length'],element=atlas['elements'][frame['index']]))
                pictures.append(dict(source=f'{filename}:{pid}',name=clip['m_Name'],frames=frames))
    return dict(records=records,layouts=layouts,pictures=pictures)


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game-path',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
    a=p.parse_args(); result=collect(a.game_path)
    a.output.parent.mkdir(parents=True,exist_ok=True)
    a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Exported {len(result["records"])} source components, {len(result["layouts"])} TextMesh layouts, {len(result["pictures"])} candidate picture clips.')
