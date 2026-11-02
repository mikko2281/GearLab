import sys,json,base64,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
from body_motion import check,trig
from procedural_body import box,cylinder
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut
folder=Path(sys.argv[1] if len(sys.argv)>1 else 'test-results/motion');folder.mkdir(parents=True,exist_ok=True);passed=[]
def assert_true(v,label):
 if not v:raise AssertionError(label)
 passed.append(label)
def encode(shape,name):
 path=folder/(name+'.step');w=STEPControl_Writer();w.Transfer(shape,STEPControl_AsIs);assert w.Write(str(path))==IFSelect_RetDone;return base64.b64encode(path.read_bytes()).decode()
try:
 identity=[1,0,0,0,0,1,0,0,0,0,1,0]
 joints=[dict(Id='a',Mode='rotate',Axis=2,Minimum=-180,Maximum=180,Position=0,Offset=[0,0,0],End=[100,0,0]),dict(Id='b',Mode='fixed',Axis=2,Minimum=0,Maximum=0,Position=0,Offset=[0,0,80],End=[100,0,0])]
 shape=box([40,-5,-5,50,5,5]);step=encode(shape,'box');parts=[dict(Id=id,Owner=id,Joint=id,InputSide=False,Local=identity,Bounds=k.bounds(shape),Step=step) for id in ['a','b']]
 spec=dict(Joints=joints,Parts=parts,Budget=64,Seconds=10);report=check(spec,k)['Motion'];assert_true(report['Status']=='clear' and report['CertifiedRegions']>0,'full rotating interval proves separated height planes')
 joints[1]['Offset']=[0,0,0];joints[0]['Minimum']=joints[0]['Maximum']=0;report=check(spec,k)['Motion'];assert_true(report['Status']=='collision' and report['Collisions'][0]['Volume']>999,'OCCT intersection gives actual colliding pose')
 ring=BRepAlgoAPI_Cut(cylinder(20,10),cylinder(10,12,[0,0,-1])).Shape();core=cylinder(5,10);parts[0].update(Step=encode(ring,'ring'),Bounds=k.bounds(ring));parts[1].update(Step=encode(core,'core'),Bounds=k.bounds(core));report=check(spec,k)['Motion'];assert_true(report['Status']=='unknown' and not report['Collisions'],'overlapping conservative bounds are not falsely called an actual collision or a full-range pass')
 for lo,hi in [(-8,-5),(-1,4),(0,.1),(3.1,3.2),(0,8)]:
  bounds=trig((lo,hi));assert_true(all(bounds[0]<=math.sin(lo+(hi-lo)*i/100)<=bounds[1] for i in range(101)),'sin interval includes interior extrema '+str((lo,hi)))
 (folder/'motion-results.json').write_text(json.dumps(dict(passed=passed),ensure_ascii=False,indent=2),encoding='utf-8');print('PASS',len(passed),'interval/exact CAD motion assertions')
except Exception:
 import traceback
 (folder/'motion-error.txt').write_text(traceback.format_exc(),encoding='utf-8');raise
