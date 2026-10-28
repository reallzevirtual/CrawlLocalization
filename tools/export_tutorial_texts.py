"""Read tutorial UI layouts and image references without executing the game."""
import argparse
import json
import re
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode


def collect(game):
    data = game / 'Crawl_Data'
    obs = {o.path_id: o for o in UnityPy.load((data/'sharedassets0.assets').read_bytes()).objects}
    scripts = {o.path_id: o.read_typetree()['m_ClassName'] for o in UnityPy.load((data/'globalgamemanagers.assets').read_bytes()).objects if o.type.name == 'MonoScript'}
    gen = TypeTreeGenerator('5.4.2f2'); gen.load_local_game(str(game))
    schemas = {}
    def schema(cls, assembly='Assembly-CSharp'):
        if cls not in schemas:
            nodes = []
            for n in gen.get_nodes_up(assembly, cls).traverse():
                row = {k:v for k,v in n.to_dict().items() if k != 'm_Children'}
                if n.m_Type == 'string' and n.m_Children and n.m_Children[0].m_Children[1].m_Type == 'string': row['m_Type'] = 'vector'
                if n.m_Name == 'm_Enabled': row['m_MetaFlag'] = 16384
                nodes.append(row)
            schemas[cls] = TypeTreeNode.from_list(nodes)
        return schemas[cls]
    games = {k:o.read_typetree() for k,o in obs.items() if o.type.name == 'GameObject'}
    transforms = {k:o.read_typetree() for k,o in obs.items() if o.type.name == 'Transform'}
    by_game = {t['m_GameObject']['m_PathID']:t for t in transforms.values()}
    classes = {k:scripts.get(o.read_typetree(check_read=False)['m_Script']['m_PathID'],'') for k,o in obs.items() if o.type.name == 'MonoBehaviour'}
    def hierarchy(gid):
        names, scale = [], 1.0
        while gid in games:
            names.append(games[gid]['m_Name']); t = by_game[gid]; scale *= t['m_LocalScale']['x']
            parent = t['m_Father']['m_PathID']; gid = transforms[parent]['m_GameObject']['m_PathID'] if parent in transforms else 0
        return '/'.join(reversed(names)), scale
    components, layouts, pictures = [], [], []
    for pid, cls in classes.items():
        if cls in ('SystemTutorial','TutorialSequence','TutorialSequenceEvolveMenu','TutorialSequenceLevel2','GuiTutorialMessageBox'):
            tree = obs[pid].read_typetree(schema(cls))
            components.append(dict(source=f'sharedassets0.assets:{pid}', cls=cls, data=tree))
        elif cls == 'exSpriteAnimClip':
            name = obs[pid].read_typetree(check_read=False)['m_Name']
            if not re.search(r'TitleCardPrologue|GameFormat|DialogBoxImage|Wrath.*Tut|Level.*Tut', name, re.I): continue
            tree = obs[pid].read_typetree(schema(cls,'ex2D.Runtime'))
            pictures.append(dict(source=f'sharedassets0.assets:{pid}', name=name, frames=tree['frameInfos']))
    for pid, o in obs.items():
        if o.type.name != 'TextMesh': continue
        tree = o.read_typetree(); gid = tree['m_GameObject']['m_PathID']; path, scale = hierarchy(gid)
        if not re.search(r'GuiMessageBox|TuteRoom|FX_GameFormat',path): continue
        width = 0
        for _, ref in games[gid]['m_Component']:
            if classes.get(ref['m_PathID']) == 'GUIText': width = obs[ref['m_PathID']].read_typetree(schema('GUIText'))['m_wrapWidth']
        font = obs[tree['m_Font']['m_PathID']].read_typetree()['m_Name']
        layouts.append(dict(source=f'sharedassets0.assets:{pid}',path=path,text=tree['m_Text'],font=font,size=tree['m_FontSize'],character_size=tree['m_CharacterSize'],line_spacing=tree['m_LineSpacing'],width=width,scale_x=scale))
    return dict(components=components,layouts=layouts,pictures=pictures)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game-path',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
    a = p.parse_args(); catalog = collect(a.game_path)
    a.output.parent.mkdir(parents=True,exist_ok=True)
    a.output.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Exported {len(catalog["components"])} tutorial components, {len(catalog["layouts"])} text layouts and {len(catalog["pictures"])} image clips.')
