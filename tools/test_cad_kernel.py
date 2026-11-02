"""Numerical checks for construction tools and Unicode-path export."""
import sys,math,json,tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
folder=Path(sys.argv[1] if len(sys.argv)>1 else tempfile.mkdtemp());folder.mkdir(parents=True,exist_ok=True)
checks=[]
def check(condition,name):
 if not condition:raise AssertionError(name)
 checks.append(name)
def sketch(id,entities,plane='XY',origin=None):return dict(Id=id,Kind='sketch',Sketch=dict(Plane=plane,Origin=origin or [0,0,0],Entities=entities))
def circle(r):return dict(Id='c',Kind='circle',Points=[[0,0]],Radius=r)
def build(features):return k.build_document(dict(Features=features),folder,display=False)[0]
def volume(bodies):return sum(k.props(s)[0] for s in bodies.values())
a=sketch('a',[dict(Kind='rectangle',Points=[[10,0]],Width=5,Height=20)],'XZ')
v=volume(build([a,dict(Id='r',Kind='revolve',Sketches=['a'],Values={'Angle':360})]));check(abs(v-math.pi*(225-100)*20)<1e-5,'revolve annular solid')
a=sketch('a',[circle(10)]);b=sketch('b',[circle(5)],origin=[0,0,20]);v=volume(build([a,b,dict(Id='l',Kind='loft',Sketches=['a','b'],Values={})]));check(abs(v-math.pi*20*(100+50+25)/3)<1e-4,'loft frustum volume')
a=sketch('a',[circle(3)],'YZ');b=sketch('b',[dict(Kind='line',Points=[[0,0],[30,0]])]);v=volume(build([a,b,dict(Id='s',Kind='sweep',Sketches=['a','b'],Values={})]));check(abs(v-math.pi*9*30)<1e-4,'sweep circular profile along path')
doc=dict(Features=[dict(Id='c',Kind='cylinder',Values={'Radius':10,'Height':20})]);b,r=k.build_document(doc,folder);face=next(f for f in r['Bodies'][0]['Faces'] if f['Kind']=='cylinder');doc['Features'].append(dict(Id='p',Kind='presspull',Target='c',Faces=[face['Id']],Values={'Distance':2}));check(abs(volume(build(doc['Features']))-math.pi*144*20)<1e-4,'radial push pull')
box=dict(Id='a',Kind='box',Values={'Width':20,'Depth':20,'Height':20});box2=dict(Id='b',Kind='box',Values={'Width':20,'Depth':20,'Height':20});move=dict(Id='m',Kind='move',Target='b',Values={'X':10})
for mode,expected in [('join',12000),('cut',4000),('intersect',4000)]:check(abs(volume(build([box,box2,move,dict(Id='o',Kind='combine',Target='a',Tools=['b'],Values={'Mode':mode})]))-expected)<1e-4,'boolean '+mode)
path=folder/'Проверка экспорта.stl';k.handle(dict(Mode='export_stl',Document=dict(Features=[box]),Folder=str(folder),Path=str(path)));check(path.stat().st_size>84,'Unicode binary STL export')
(folder/'kernel-results.json').write_text(json.dumps({'passed':checks},ensure_ascii=False,indent=2),encoding='utf-8');print('PASS',len(checks),'native kernel numerical checks')
