"""Native reference-wrist variants: real parts, mounting bores and connected covers."""
import sys,json,copy
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
from connected_body import generate
from procedural_body import PRESETS
root=Path(__file__).resolve().parents[1]
doc=json.loads((root/'examples/SixAxis_Connected.robot.json').read_text(encoding='utf-8-sig'))
base=json.loads(doc['Joints'][3]['GeneratedBody']['SpecJson']);dest=Path(sys.argv[1]);dest.mkdir(parents=True,exist_ok=True);passed=[]
styles=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else PRESETS
for name in ['High Strength','Easy Print']:
 spec=copy.deepcopy(base);spec['Variant']=name;spec['Style']=styles[name]
 result=generate(spec,dest/name.replace(' ','_'),k)['Procedural']
 assert result['GeometryValid'],[(x['Code'],x['Message']) for x in result['Issues'] if x['Status']=='error']
 assert all(k.props(k.import_step(__import__('base64').b64encode(Path(p['StepPath']).read_bytes()).decode()))[0]>0 for p in result['Parts'])
 assert result['Modules']['design']=='reference-cassette'
 assert not result['ReadyForManufacture']
 passed.append(dict(variant=name,parts=len(result['Parts']),seconds=result['Seconds'],mass_kg=result['MassKg']))
(dest/'reference-variants-results.json').write_text(json.dumps(dict(passed=passed),indent=2),encoding='utf-8');print('PASS reference wrist variants',len(passed))
