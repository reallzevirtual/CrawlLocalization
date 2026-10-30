"""Read-only ex2D frame audit and contact sheets; never alters game resources."""
import argparse,json,math
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from UnityPy.helpers.TypeTreeNode import TypeTreeNode
from PIL import Image,ImageDraw

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--game-path',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--clip-ids',type=int,nargs='+',default=[43859,40843,40765,43845,40735,42380],
                    help='exSpriteAnimClip PathIDs in sharedassets0.assets; defaults to the representative audit set')
a=parser.parse_args();a.output.mkdir(parents=True,exist_ok=True)
gen=TypeTreeGenerator('5.4.2f2');gen.load_local_game(str(a.game_path))
files={};schemas={};atlases={};textures={}
def objects(filename):
 if filename not in files:files[filename]={o.path_id:o for o in UnityPy.load(str(a.game_path/'Crawl_Data'/filename)).objects}
 return files[filename]
def resolve(obj,ref):
 fn=Path(obj.assets_file.name).name
 if ref['m_FileID']:fn=Path(obj.assets_file.externals[ref['m_FileID']-1].path.replace('\\','/')).name
 return objects(fn)[ref['m_PathID']]
def schema(cls):
 if cls not in schemas:
  rows=[]
  for n in gen.get_nodes_up('ex2D.Runtime',cls).traverse():
   row={k:v for k,v in n.to_dict().items() if k!='m_Children'}
   if n.m_Type=='string' and n.m_Children and n.m_Children[0].m_Children[1].m_Type=='string':row['m_Type']='vector'
   if n.m_Name=='m_Enabled':row['m_MetaFlag']=16384
   rows.append(row)
  schemas[cls]=TypeTreeNode.from_list(rows)
 return schemas[cls]
report=[]
for pid in a.clip_ids:
 obj=objects('sharedassets0.assets')[pid];clip=obj.read_typetree(schema('exSpriteAnimClip'))
 frames=[];info={'path_id':pid,'name':clip['m_Name'],'length':clip['length'],'wrap_mode':clip['wrapMode'],'stop_action':clip['stopAction'],'events':clip['eventInfos'],'frames':[]}
 for frame in clip['frameInfos']:
  ao=resolve(obj,frame['atlas']);key=(ao.assets_file.name,ao.path_id)
  if key not in atlases:atlases[key]=ao.read_typetree(schema('exAtlas'))
  atlas=atlases[key];element=atlas['elements'][frame['index']]
  to=resolve(ao,atlas['texture']);tk=(to.assets_file.name,to.path_id)
  if tk not in textures:textures[tk]=to.read().image.convert('RGBA')
  image=textures[tk];uv=element['coords'];rect=element['trimRect'];w,h=image.size
  left=round(uv['x']*w);top=round((1-uv['y']-uv['height'])*h)
  crop=image.crop((left,top,left+round(uv['width']*w),top+round(uv['height']*h)))
  if element['rotated']:crop=crop.transpose(Image.Transpose.ROTATE_90)
  canvas=Image.new('RGBA',(element['originalWidth'],element['originalHeight']))
  if crop.width and crop.height:canvas.paste(crop,(round(rect['x']),round(rect['y'])))
  frames.append(canvas)
  info['frames'].append({'atlas':atlas['m_Name'],'index':frame['index'],'length':frame['length'],'element':element})
 # Export restored canvases, not atlas-coordinate fragments, so motion is visible.
 sub=a.output/clip['m_Name'];sub.mkdir(exist_ok=True)
 for i,im in enumerate(frames):im.save(sub/f'{i:02}.png')
 cw=max(im.width for im in frames);ch=max(im.height for im in frames)
 zoom=3 if cw<100 else 2
 cellw=max(180,cw*zoom+12);cellh=ch*zoom+42;cols=min(4,len(frames))
 sheet=Image.new('RGB',(cellw*cols,cellh*math.ceil(len(frames)/cols)),(25,25,31));d=ImageDraw.Draw(sheet)
 for i,im in enumerate(frames):
  x=(i%cols)*cellw;y=(i//cols)*cellh
  scaled=im.resize((im.width*zoom,im.height*zoom),Image.Resampling.NEAREST)
  sheet.paste(scaled,(x+6,y+30),scaled)
  e=info['frames'][i]
  d.text((x+6,y+3),f"{i}: {e['element']['name']}",fill='white')
  d.text((x+6,y+15),f"{e['length']*1000:.1f} ms",fill='#aaaacc')
 sheet.save(a.output/(clip['m_Name']+'.png'));report.append(info)
 print(clip['m_Name'],len(frames),'frames',round(clip['length'],4),'seconds','events',[(e['time'],e['methodName']) for e in clip['eventInfos']])
(a.output/'clips.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('Audit saved:',a.output)
