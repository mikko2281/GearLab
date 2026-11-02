"""Audit all catalog presets across real STEP serialization, not only memory."""
import sys,json,base64,math,time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k
presets=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'));out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True);rows=[]
for i,p in enumerate(presets):
 start=time.monotonic();doc={'Features':[{'Id':'gear','Kind':'gear','Name':p['Name'],'Values':p['Values']}]};path=out/('gear-'+str(i)+'.step')
 result=k.handle({'Mode':'export_step','Document':doc,'Path':str(path),'Folder':str(out)})
 imported=k.import_step(base64.b64encode(path.read_bytes()).decode('ascii'));before=sum(b['Volume'] for b in result['Bodies']);after=k.props(imported)[0]
 assert math.isclose(before,after,rel_tol=1e-5,abs_tol=.001),p['Name']
 assert len(k.shapes(imported,k.TopAbs_SOLID))==1,p['Name']
 rows.append({'Name':p['Name'],'Type':p['Values']['Type'],'Volume':after,'Faces':len(k.shapes(imported,k.TopAbs_FACE)),'Seconds':round(time.monotonic()-start,3),'Valid':True})
 print('PASS',i+1,p['Values']['Type'],flush=True)
(out/'gear-step-results.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8');print('PASS',len(rows),'STEP preset roundtrips',flush=True)
