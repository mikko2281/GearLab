"""Independent checks of exported prototype STL: units, envelope and closed mesh."""
import sys,json,zipfile,struct,math,itertools
from collections import Counter,defaultdict
from pathlib import Path
source=Path(sys.argv[1]);output=Path(sys.argv[2]);reports=[]
for_mesh=struct.Struct('<12fH');neighbours=list(itertools.product((-1,0,1),repeat=3));tol=float(sys.argv[3]) if len(sys.argv)>3 else 0.0
with zipfile.ZipFile(source) as archive:
 for item in archive.infolist():
  if not item.filename.lower().endswith('.stl'):continue
  raw=archive.read(item);count=struct.unpack_from('<I',raw,80)[0];assert len(raw)==84+50*count and count>0,item.filename
  cells=defaultdict(list);exact={};vertices=[];edges=Counter();directions=Counter();volume=0.0;lo=[float('inf')]*3;hi=[-float('inf')]*3;degenerate=0
  def weld(point):
   if tol==0:
    if point not in exact:exact[point]=len(vertices);vertices.append(point)
    return exact[point]
   key=tuple(math.floor(v/tol) for v in point)
   for shift in neighbours:
    for index in cells.get(tuple(key[i]+shift[i] for i in range(3)),()):
     if sum((point[i]-vertices[index][i])**2 for i in range(3))<=tol*tol:return index
   index=len(vertices);vertices.append(point);cells[key].append(index);return index
  for t in range(count):
   record=for_mesh.unpack_from(raw,84+50*t);points=[record[3+3*v:6+3*v] for v in range(3)]
   assert all(math.isfinite(v) for p in points for v in p),item.filename
   for point in points:
    for axis in range(3):lo[axis]=min(lo[axis],point[axis]);hi[axis]=max(hi[axis],point[axis])
   ids=[weld(p) for p in points]
   if len(set(ids))<3:degenerate+=1;continue
   a,b,c=points
   volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
   for a,b in ((0,1),(1,2),(2,0)):
    key=tuple(sorted((ids[a],ids[b])));edges[key]+=1;directions[key]+=1 if ids[a]<ids[b] else -1
  bad=sum(n!=2 for n in edges.values());size=[hi[i]-lo[i] for i in range(3)]
  reports.append(dict(name=item.filename,triangles=count,welded_vertices=len(vertices),degenerate_triangles=degenerate,bad_edges=bad,orientation_errors=sum(v!=0 for v in directions.values()),volume_mm3=volume,size_mm=size,bad_edge_details=[dict(count=n,a=vertices[a],b=vertices[b]) for (a,b),n in edges.items() if n!=2][:30]))
assert reports,'No prototype STL files'
result=dict(files=len(reports),all_fit=all(max(p['size_mm'])<=250 for p in reports),all_closed=all(p['bad_edges']==0 for p in reports),weld_tolerance_mm=tol,parts=reports)
result['all_oriented']=all(p['orientation_errors']==0 and p['volume_mm3']>0 for p in reports)
project=Path(__file__).resolve().parents[1]/'examples/Industrial_1000mm.robot.json'
if project.exists():
 d=json.loads(project.read_text(encoding='utf-8-sig'));volumes={j['Id']+'_proc_'+p['Id']+'.stl':p['Volume'] for j in d['Joints'] for p in j['GeneratedBody']['Result']['Parts']}
 for p in reports:
  if p['name'] in volumes:p['native_relative_volume_error']=abs(p['volume_mm3']-volumes[p['name']])/volumes[p['name']]
 result['native_volume_matches']=all(p.get('native_relative_volume_error',0)<=.005 for p in reports)
output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
assert result['all_fit'],'STL exceeds the reserved P1S envelope'
assert result['all_oriented'],'Inconsistent STL winding or volume'
assert result.get('native_volume_matches',True),'STL differs from native CAD volume'
assert result['all_closed'],'Non-manifold STL; see '+str(output)
print('PASS',len(reports),'closed prototype STL parts with reserved P1S envelope')
