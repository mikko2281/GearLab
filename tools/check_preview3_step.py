import json,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
folder=root/'test-results-fixes'
cases=[(f'legacy-{i}',folder/f'regression/step/step-{i}.step',n) for i,n in enumerate([55,24,25,34])]
cases.extend([('wave-full',folder/'wave-exact/Wave56-full.step',19),('wave-print',folder/'wave-exact/Wave56-print.step',3)])
rows=[]
for name,path,expected in cases:
 reader=k.STEPControl_Reader()
 assert reader.ReadFile(str(path))==k.IFSelect_RetDone,str(path)
 reader.TransferRoots();solids=k.shapes(reader.OneShape(),k.TopAbs_SOLID)
 assert len(solids)==expected,(name,len(solids),expected)
 assert all(k.BRepCheck_Analyzer(s).IsValid() and k.props(s)[0]>1e-6 for s in solids),name
 rows.append({'Name':name,'Solids':len(solids),'Valid':True})
 print(name,len(solids),'PASS',flush=True)
(folder/'step-reimport.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
