"""Real OCCT regression: variants, occupied spaces, exact export and dependencies."""
import sys,json,copy,base64,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
from procedural_body import generate,PRESETS
folder=Path(sys.argv[1] if len(sys.argv)>1 else 'test-results/procedural-kernel');folder.mkdir(parents=True,exist_ok=True)
checks=[];runs=[]
def check(v,label):
 if not v:raise AssertionError(label)
 checks.append(label)
def run(name,spec):
 result=generate(spec,folder/name,k);r=result['Procedural'];runs.append(dict(name=name,seconds=r['Seconds'],mass=r['MassKg'],parts=len(r['Parts']),valid=r['GeometryValid'],changed=r['ChangedModules']))
 check(r['GeometryValid'],name+': '+str([(i['Code'],i['Message']) for i in r['Issues'] if i['Status']=='error']))
 check(not r['ReadyForManufacture'],name+': no false manufacturing certification')
 check(all(x['Volume']>0 and x['Triangles']>0 for x in result['Bodies']),name+': solid volumes and real meshes')
 check(all(Path(p['StepPath']).stat().st_size>1000 for p in r['Parts']),name+': exact STEP parts')
 return result
try:
 base=dict(Version=1,End=[180,0,0],Variant='Reference',Motor=dict(Frame=41.9,Length=37.5,Pitch=31.3,Pilot=22),Reducer=dict(Diameter=76,Length=32))
 original=run('Reference',base)
 original_report=original['Procedural']
 if '--extra-only' not in sys.argv:
  for name in PRESETS:
   if name=='Reference':continue
   spec=copy.deepcopy(base);spec['Variant']=name;run(name.replace(' ','_'),spec)
  original_report=original['Procedural'];length=copy.deepcopy(base);length['End']=[240,0,0];length['PreviousModules']=original_report['Modules'];long=run('length_240',length)
  check('actuator' not in long['Procedural']['ChangedModules'],'length preserves motor/reducer mount module')
  check('frame_0' in long['Procedural']['ChangedModules'],'length rebuilds load-bearing frame')
  motor=copy.deepcopy(base);motor['Motor']['Length']=60;motor['PreviousModules']=original_report['Modules'];longmotor=run('motor_60',motor)
  check('actuator' in longmotor['Procedural']['ChangedModules'],'long motor locally updates joint housing')
  check('frame_0' not in longmotor['Procedural']['ChangedModules'],'motor length preserves independent link frame')
  cached=copy.deepcopy(base);cached['PreviousModules']=original_report['Modules'];same=run('repeat',cached);check(not same['Procedural']['ChangedModules'],'identical inputs preserve all feature keys')
  high=copy.deepcopy(base);high['End']=[0,-180,0];high['Mechanical']=dict(DesignMomentNmm=14000,DesignTorqueNmm=9800,AxialForceN=60,OutputTop=34);run('loaded_Y_axis',high)
  axial=copy.deepcopy(high);axial['End']=[0,0,180];run('axial_link',axial)
  override=copy.deepcopy(base);pid='rear_cover';source=next(p for p in original_report['Parts'] if p['Id']==pid);override['Overrides']={pid:base64.b64encode(Path(source['StepPath']).read_bytes()).decode()};manual=run('manual_STEP',override);check(any(i['Code']=='manual_override' and i['Status']=='pending' for i in manual['Procedural']['Issues']),'manual exact CAD overrides kept and structural review pending')
 fit=copy.deepcopy(base);fit['Mechanical']=dict(ShaftFit=.25);fit['PreviousModules']=original_report['Modules'];fitted=run('shaft_fit_025',fit)
 check('frame_0' in fitted['Procedural']['ChangedModules'] and 'actuator' not in fitted['Procedural']['ChangedModules'],'shaft fit invalidates hub but preserves independent motor housing')
 frame=next(b for b in fitted['Bodies'] if b['Id']=='frame_0');check(any(f['Kind']=='cylinder' and abs(f['Radius']-4.25)<1e-5 for f in frame['Faces']),'new shaft clearance really changes the native cylindrical bore')
 shape=copy.deepcopy(base);shape['Style']=dict(Slimness=.1);shape['PreviousModules']=original_report['Modules'];styled=run('slimness_01',shape);check('frame_0' in styled['Procedural']['ChangedModules'],'style waist dimension participates in feature dependencies')
 from procedural_body import box
 from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
 component_path=folder/'motor_component.step';writer=STEPControl_Writer();writer.Transfer(box([-20.95,-20.95,-73.5,20.95,20.95,-36]),STEPControl_AsIs);writer.Write(str(component_path))
 physical=copy.deepcopy(base);physical['Components']=[dict(Id='measured_motor',Label='Exact imported motor test',Step=base64.b64encode(component_path.read_bytes()).decode(),Fidelity='imported')];actual=run('physical_STEP',physical);check(any(v['Id']=='measured_motor_clear' for v in actual['Procedural']['ProtectedVolumes']),'imported exact component gets independent mounting-clearance volume')
 (folder/'procedural-results.json').write_text(json.dumps(dict(passed=checks,runs=runs),ensure_ascii=False,indent=2),encoding='utf-8')
 print('PASS',len(checks),'procedural OCCT assertions',flush=True)
except Exception:
 import traceback
 (folder/'procedural-error.txt').write_text(traceback.format_exc(),encoding='utf-8');raise
