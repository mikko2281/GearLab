"""Regressions: native CAD overrides and imported occupied STEP volumes."""
import sys,json,copy,base64
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from connected_body import generate
doc=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));folder=Path(sys.argv[2]);folder.mkdir(parents=True,exist_ok=True)
joint=doc['Joints'][-1];generated=joint['GeneratedBody'];spec=json.loads(generated['SpecJson']);parts={p['Id']:p for p in generated['Result']['Parts']};checks=[]
moved=copy.deepcopy(spec);moved['Overrides']={'frame_0':{'Step':parts['frame_0']['Step'],'Shift':[0,0,4]}}
result=generate(moved,folder/'moved-core',k)['Procedural']
assert not result['GeometryValid'] and any(i['Code']=='root_contact' and i['Status']=='error' for i in result['Issues'])
checks.append('native 4 mm structural displacement breaks actual root contact and is rejected')
occupied=copy.deepcopy(spec);occupied['Components']=[dict(Id='test-component',Label='Inserted occupied STEP',Step=parts['rear_cover']['Step'])]
result=generate(occupied,folder/'occupied',k)['Procedural']
assert not result['GeometryValid'] and any(i['Code']=='component_intersection' and i['Status']=='error' for i in result['Issues'])
assert any(v['Id']=='test-component' and len(v['Bounds'])==6 for v in result['ProtectedVolumes'])
checks.append('imported STEP overlapping a real part blocks application and exposes occupied volume')
(folder/'edit-results.json').write_text(json.dumps({'passed':checks},indent=2),encoding='utf-8');print('PASS',len(checks),'connected CAD edit regressions',flush=True)
