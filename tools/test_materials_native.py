import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate
folder=Path(sys.argv[1]).resolve();reports=[]
for fixture in ['materials','clutch']:
 spec=json.loads((folder/(fixture+'.assembly.json')).read_text(encoding='utf-8-sig'))
 path=folder/(fixture+'.step');request=dict(Spec=spec,Folder=str(folder/'native'),Path=str(path),Mode='assembly_parametric')
 generate(request,k)
 text=path.read_text(encoding='utf-8',errors='replace')
 assert 'material property' in text and 'Density_kg_m3' in text
 reader=k.STEPControl_Reader();assert reader.ReadFile(str(path))==k.IFSelect_RetDone;reader.TransferRoots()
 shape=k.checked(reader.OneShape());solids=k.shapes(shape,k.TopAbs_SOLID)
 assert len(solids)==len(spec),(fixture,len(solids),len(spec))
 audit=generate(dict(request,Mode='assembly_audit'),k)['Audit']
 significant=[p for p in audit['Pairs'] if p['Volume']>.001]
 reports.append(dict(Fixture=fixture,Solids=len(solids),MaterialsStored=True,ToleranceMm3=.001,SignificantPairs=significant,**audit))
 (folder/'materials-native-results.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
 assert audit['Complete'] and not significant,(fixture,significant)
 print('PASS:',fixture,'STEP materials, connected solids, no significant intersections',flush=True)
