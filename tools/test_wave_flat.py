"""Flat wave reducer: solids, STEP roundtrip, axial ownership and phase geometry."""
import sys,json,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
from wave_kernel import make_wave
from OCP.TopAbs import TopAbs_SOLID
from OCP.BRepCheck import BRepCheck_Analyzer
from OCP.STEPControl import STEPControl_Writer,STEPControl_Reader,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
folder=Path(sys.argv[1]);folder.mkdir(parents=True,exist_ok=True);checks=[]
def check(ok,name):
 if not ok:raise AssertionError(name)
 checks.append(name)
v=dict(Layout='flat',Ratio=50,Module=.6,Thickness=8,Wall=.8,RingWall=4,Bore=5,OutputBore=8,Clearance=.15,BearingWall=1.5,Phase=0)
bodies=make_wave(v,float,k)
check(set(bodies)=={'ring','flex','output','generator','bearing',*[f'bolt{i}' for i in range(1,7)],*[f'output_bolt{i}' for i in range(1,7)]},'all flat components')
for name,shape in bodies.items():
 check(BRepCheck_Analyzer(shape).IsValid(),name+' valid topology')
 check(len(k.shapes(shape,TopAbs_SOLID))==1,name+' single connected solid')
 check(k.props(shape)[0]>0,name+' positive volume')
check(k.bounds(bodies['ring'])[2]<0,'rear mounting flange present')
check(k.bounds(bodies['output'])[5]>2*v['Thickness']+10,'central output shaft present')
check(k.bounds(bodies['flex'])[2]>-1e-5,'flex is a flat sleeve, no long cup')
original=sum(k.props(s)[0] for s in bodies.values())
# Preserve all separate solids, including hardware, through STEP serialization.
writer=STEPControl_Writer()
for shape in bodies.values():writer.Transfer(shape,STEPControl_AsIs)
step=folder/'Wave_flat_50.step';check(writer.Write(str(step))==IFSelect_RetDone,'STEP write')
reader=STEPControl_Reader();check(reader.ReadFile(str(step))==IFSelect_RetDone,'STEP read');reader.TransferRoots()
solids=k.shapes(reader.OneShape(),TopAbs_SOLID);check(len(solids)==17,'STEP preserves seventeen solids')
volume=sum(k.props(s)[0] for s in solids);check(abs(volume-original)/original<1e-7,'STEP roundtrip volume')
# Failure before expensive tooth generation, rather than an invalid half-model.
for key,value in [('Ratio',50.5),('Module',float('nan')),('Bore',200),('RingWall',2)]:
 invalid=dict(v);invalid[key]=value
 try:make_wave(invalid,float,k)
 except ValueError:check(True,'reject invalid '+key)
 else:check(False,'reject invalid '+key)
(folder/'wave-flat-results.json').write_text(json.dumps(dict(passed=checks,volume_mm3=original,step_bytes=step.stat().st_size),indent=2),encoding='utf-8')
print('PASS',len(checks),'flat wave native checks',flush=True)
