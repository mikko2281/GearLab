"""Conservative interval separation + exact positive-volume collision witnesses.

An unresolved interval is never reported as clear. Rigid groups deliberately
exclude their own internal fitting contacts; the body generator checks those.
"""
import math,time,itertools
from OCP.gp import gp_Trsf
from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
from OCP.BRepAlgoAPI import BRepAlgoAPI_Common
from procedural_body import disjoint

def interval(x):return (x,x)
def add(a,b):return (a[0]+b[0]-1e-10,a[1]+b[1]+1e-10)
def mul(a,b):
 values=[x*y for x in a for y in b];return (min(values)-1e-10,max(values)+1e-10)
def trig(a,cos=False):
 lo,hi=a
 if cos:lo+=math.pi/2;hi+=math.pi/2
 if hi-lo>=math.tau:return (-1,1)
 values=[math.sin(lo),math.sin(hi)]
 for i in range(math.ceil((lo-math.pi/2)/math.pi),math.floor((hi-math.pi/2)/math.pi)+1):values.append(math.sin(math.pi/2+i*math.pi))
 return (max(-1,min(values)-1e-12),min(1,max(values)+1e-12))
def eye():return [[interval(float(i==j)) for j in range(4)] for i in range(4)]
def product(a,b):return [[__import__('functools').reduce(add,(mul(a[i][n],b[n][j]) for n in range(4)),interval(0)) for j in range(4)] for i in range(4)]
def translation(v):
 m=eye()
 for i in range(3):m[i][3]=v[i] if isinstance(v[i],tuple) else interval(v[i])
 return m
def rotation(axis,bounds):
 a=tuple(x*math.pi/180 for x in bounds);c=trig(a,True);s=trig(a);negative=(-s[1],-s[0]);m=eye();u=(axis+1)%3;v=(axis+2)%3;m[u][u]=m[v][v]=c;m[u][v]=negative;m[v][u]=s;return m
def local(values):
 m=eye()
 for i in range(3):
  for j in range(4):m[i][j]=interval(values[i*4+j])
 return m
def bounds_transform(bounds,m):
 result=[]
 for row in m[:3]:
  value=row[3]
  for i in range(3):value=add(value,mul(row[i],(bounds[i],bounds[i+3])))
  result.append(value)
 return [v[0] for v in result]+[v[1] for v in result]
def poses(joints,ranges):
 out={};bases={};ends={}
 for j in joints:
  parent=ends[j['Parent']] if j.get('Parent') else eye();basis=product(parent,translation(j['Offset']));bounds=ranges[j['Id']]
  motion=eye() if j['Mode']=='fixed' else translation([bounds if i==j['Axis'] else 0 for i in range(3)]) if j['Mode']=='linear' else rotation(j['Axis'],bounds)
  bases[j['Id']]=basis;out[j['Id']]=product(basis,motion);ends[j['Id']]=product(out[j['Id']],translation(j['End']))
 return bases,out
def check(spec,k):
 start=time.monotonic();joints=spec['Joints'];parts=spec['Parts'];ranges={j['Id']:(j['Minimum'],j['Maximum']) if j['Mode']!='fixed' else (j['Position'],j['Position']) for j in joints};shapes={};matrices={p['Id']:local(p['Local']) for p in parts};collisions=[];unknown=[];visited=0;certified=0;max_nodes=min(4096,max(8,int(spec.get('Budget',256))));deadline=start+min(120,max(5,float(spec.get('Seconds',30))))
 for p in parts:
  if p.get('Step'):
   shapes[p['Id']]=k.import_step(p['Step']);p['Bounds']=k.bounds(shapes[p['Id']])
 def evaluate(box):
  bases,outputs=poses(joints,box);transforms={p['Id']:product((bases if p['InputSide'] else outputs)[p['Joint']],matrices[p['Id']]) for p in parts};bounds={p['Id']:bounds_transform(p['Bounds'],transforms[p['Id']]) for p in parts};pairs=[]
  for i,a in enumerate(parts):
   for b in parts[i+1:]:
    if a.get('Owner')==b.get('Owner'):continue
    if not disjoint(bounds[a['Id']],bounds[b['Id']],.0001):pairs.append((a,b))
  return transforms,bounds,pairs
 queue=[ranges]
 while queue and visited<max_nodes and time.monotonic()<deadline:
  box=queue.pop();visited+=1;transforms,bounds,pairs=evaluate(box)
  if not pairs:certified+=1;continue
  midpoint={key:((v[0]+v[1])/2,)*2 for key,v in box.items()};mid_transform,mid_bounds,mid_pairs=evaluate(midpoint);witness_cache={}
  # A midpoint intersection is a concrete witness, not a finite-sampling proof.
  for a,b in mid_pairs:
   if time.monotonic()>deadline:break
   if a['Id'] not in shapes or b['Id'] not in shapes:continue
   def moved(p):
    pid=p['Id']
    if pid not in witness_cache:
     m=mid_transform[pid];tr=gp_Trsf();tr.SetValues(*(sum(v)/2 for row in m[:3] for v in row));witness_cache[pid]=BRepBuilderAPI_Transform(shapes[pid],tr,True).Shape()
    return witness_cache[pid]
   common=BRepAlgoAPI_Common(moved(a),moved(b)).Shape();volume=abs(k.props(common)[0]) if k.shapes(common,k.TopAbs_SOLID) else 0
   if volume>.1:
    collisions.append(dict(A=a['Id'],B=b['Id'],Position=k.props(common)[1],Volume=volume,Values={key:v[0] for key,v in midpoint.items()}))
    if len(collisions)>=12:break
  if collisions:break
  movable=[j for j in joints if j['Mode']!='fixed' and box[j['Id']][1]-box[j['Id']][0]>(.5 if j['Mode']=='rotate' else .1)]
  if not movable:unknown.append(box);continue
  selected=max(movable,key=lambda j:(box[j['Id']][1]-box[j['Id']][0])/(360 if j['Mode']=='rotate' else 100));key=selected['Id'];lo,hi=box[key];mid=(lo+hi)/2;left=dict(box);right=dict(box);left[key]=(lo,mid);right[key]=(mid,hi);queue.extend([right,left])
 unknown.extend(queue)
 status='collision' if collisions else 'unknown' if unknown else 'clear'
 return dict(Ok=True,Motion=dict(Status=status,Visited=visited,CertifiedRegions=certified,UnresolvedRegions=len(unknown),Seconds=round(time.monotonic()-start,3),Collisions=collisions,Scope='Проверенные CAD-детали различных жёстких групп; крепёж и гибкая проводка исключены. Замкнутые AABB-интервалы учитывают все сочетания заданных углов/ходов. Контакты внутри жёсткой группы, гибкие провода и деформации проверяются отдельно.'))
