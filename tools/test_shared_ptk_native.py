"""Check connected native solids, STEP roundtrip, and all contacts for PTK compound fixtures."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate
folder=Path(sys.argv[1]).resolve()
reports=[]
files=sorted(folder.glob('ptk-*.assembly.json'))
assert len(files)==9, len(files)
for file in files:
    spec=json.loads(file.read_text(encoding='utf-8-sig'))
    request=dict(Spec=spec,Folder=str(folder/'native'),Mode='assembly_audit')
    audit=generate(request,k)['Audit']
    significant=[p for p in audit['Pairs'] if p['Volume']>.001]
    reports.append(dict(Fixture=file.name,SignificantPairs=significant,**audit))
    (folder/'native-results.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
    assert audit['Complete'] and not significant, (file.name,audit)
    if file.name=='ptk-CommonShaft.assembly.json':
        request.update(Mode='assembly_parametric',Path=str(folder/'ptk-common.step'))
        assert generate(request,k)['Ok']
    print('PASS:',file.name,flush=True)
