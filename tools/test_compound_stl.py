import collections,json,struct,sys,zipfile
from pathlib import Path
folder=Path(sys.argv[1]);reports=[]
with zipfile.ZipFile(folder/'compound-print.zip') as zip:
 assert 'BOM_all_parts.csv' in zip.namelist()
 for name in zip.namelist():
  if not name.endswith('.stl'):continue
  data=zip.read(name);count=struct.unpack_from('<I',data,80)[0];assert len(data)==84+count*50
  edges=collections.Counter();orientation=collections.Counter();points=[]
  for i in range(count):
   v=struct.unpack_from('<12fH',data,84+50*i);tri=[tuple(round(c,5) for c in v[j:j+3]) for j in (3,6,9)];points+=tri
   for a,b in [(tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])]:
    key=tuple(sorted((a,b)));edges[key]+=1;orientation[key]+=1 if a<b else -1
  bad=sum(n!=2 or orientation[key]!=0 for key,n in edges.items());dims=[max(v[k] for v in points)-min(v[k] for v in points) for k in range(3)]
  reports.append(dict(File=name,Triangles=count,BadEdges=bad,Dimensions=dims))
  assert bad==0,(name,bad)
  assert max(dims)<=256,(name,dims)
(folder/'compound.stl.audit.json').write_text(json.dumps(dict(Passed=True,Parts=reports),indent=2),encoding='utf-8')
print('PASS: closed oriented STL, printer envelope and BOM;',len(reports),'parts')
