"""Exact B-rep validation of compound fixtures emitted by --compound-test."""
import json, sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate
folder=Path(sys.argv[1]).resolve()
spec=json.loads((folder/'compound.assembly.json').read_text(encoding='utf-8-sig'))
request=dict(Spec=spec,Folder=str(folder/'native'),Path=str(folder/'compound.step'),Mode='assembly_parametric')
reply=generate(request,k)
assert reply['Ok']
request['Mode']='assembly_audit'
audit=generate(request,k)['Audit']
(folder/'compound.audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
assert audit['Complete'],audit
assert not audit['Pairs'],audit['Pairs']
print('PASS: compound exact connected solids, STEP and zero intersections')
if '--variants' in sys.argv:
    reports=[]
    for file in sorted(folder.glob('compound-*.assembly.json')):
        fixture=json.loads(file.read_text(encoding='utf-8-sig'))
        result=generate(dict(Spec=fixture,Folder=str(folder/'native'),Mode='assembly_audit'),k)['Audit']
        reports.append(dict(Fixture=file.name,**result))
        (folder/'compound.variants.audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
        # Coincident bearing/journal faces can leave micrometre-scale Boolean residue.
        significant=[p for p in result['Pairs'] if p['Volume']>0.001]
        result['ToleranceMm3']=0.001
        result['SignificantPairs']=significant
        assert result['Complete'] and not significant,(file.name,result)
        print('PASS:',file.name,flush=True)
