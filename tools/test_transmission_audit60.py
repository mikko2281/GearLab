import sys,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate
folder=Path(sys.argv[1]) if len(sys.argv)>1 else root/'docs/verification/6.0'
folder.mkdir(parents=True,exist_ok=True)
def box(name,x):return {'Id':name,'Transform':[1,0,0,x,0,1,0,0,0,0,1,0], 'Sections':[{'Z0':0,'Z1':10,'Outer':{'Points':[[0,0],[10,0],[10,10],[0,10]]},'Holes':[]}]}
reports=[]
for x,volume in [(5,500),(10,0),(11,0)]:
    reply=generate({'Mode':'assembly_audit','Spec':[box('A',0),box('B',x)],'Folder':str(folder/'audit-toy')},k)
    audit=reply['Audit'];actual=sum(pair['Volume'] for pair in audit['Pairs'])
    assert abs(actual-volume)<1e-7,(actual,volume)
    assert audit['Complete']
    reports.append({'Offset':x,'ExpectedVolume':volume,'ActualVolume':actual,'Complete':audit['Complete']})
(folder/'audit60-results.json').write_text(json.dumps({'Passed':True,'Cases':reports},indent=2),encoding='utf-8')
print('PASS: exact common volume, face contact, separated cubes')
