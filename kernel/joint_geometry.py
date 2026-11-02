"""Tangent-matched link routes and closed, rounded mounting saddles."""
import math
from OCP.gp import gp_Pnt,gp_Vec
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeWire,BRepBuilderAPI_MakeEdge,BRepBuilderAPI_MakeFace
from OCP.BRepPrimAPI import BRepPrimAPI_MakePrism
from OCP.GC import GC_MakeArcOfCircle
from procedural_body import plus,times,vec,norm,dot,cross,point

def route(start,target,end_axis,radius,obstacles=None):
 delta=vec(start,target);distance=math.sqrt(dot(delta,delta));first=[0,0,1];final=times(end_axis,-1 if dot(delta,end_axis)<0 else 1)
 def line(a,b):return dict(Kind='line',Start=a,End=b)
 if dot(cross(delta,first),cross(delta,first))<1e-9 and dot(delta,first)>0 and dot(final,first)>.999999:return [line(start,target)],1e20
 candidates=[];bend=radius*1.55
 for direction_sign in [1,-1]:
  final=times(end_axis,direction_sign)
  aa=dot(delta,first);bb=dot(delta,final);residual=plus(delta,times(plus(times(first,aa),times(final,bb)),-1))
  if abs(dot(first,final))<1e-8 and dot(residual,residual)<1e-8 and min(aa,bb)>radius*1.3+4:
   r=min(bend,aa-4,bb-4);corner=plus(start,times(first,aa));a=plus(corner,times(first,-r));b=plus(corner,times(final,r));center=plus(a,times(final,r));mid=plus(center,times(norm(plus(vec(center,a),vec(center,b))),r));candidates.append((aa+bb-2*r+r*math.pi/2,[line(start,a),dict(Kind='arc',Start=a,Mid=mid,End=b,Center=center,Axis=norm(cross(first,final)),Angle=math.pi/2,Radius=r),line(b,target)]))
  for aa in [.35,.5,.7,.9,1.2,1.6,2]:
   for bb in [.35,.5,.7,.9,1.2,1.6,2]:
    points=[start,plus(start,times(first,distance*aa)),plus(target,times(final,-distance*bb)),target];cuts=[];ok=True
    for i in [1,2]:
     u=norm(vec(points[i-1],points[i]));v=norm(vec(points[i],points[i+1]));cosine=max(-1,min(1,dot(u,v)));angle=math.acos(cosine)
     if angle<1e-6:cuts.append((0,None,None,None,0));continue
     if angle>math.pi-1e-4:ok=False;break
     trim=bend*math.tan(angle/2);a=plus(points[i],times(u,-trim));b=plus(points[i],times(v,trim));center=plus(points[i],times(norm(vec(u,v)),bend/math.cos(angle/2)));mid=plus(center,times(norm(plus(vec(center,a),vec(center,b))),bend));cuts.append((trim,a,mid,b,angle,center,norm(cross(u,v))))
    if not ok:continue
    lengths=[math.sqrt(dot(vec(points[i],points[i+1]),vec(points[i],points[i+1]))) for i in range(3)]
    if lengths[0]<cuts[0][0]+4 or lengths[1]<cuts[0][0]+cuts[1][0]+2 or lengths[2]<cuts[1][0]+4:continue
    segments=[];cursor=start
    for cut in cuts:
     if cut[0]==0:continue
     segments.append(line(cursor,cut[1]));segments.append(dict(Kind='arc',Start=cut[1],Mid=cut[2],End=cut[3],Center=cut[5],Axis=cut[6],Angle=cut[4],Radius=bend));cursor=cut[3]
    segments.append(line(cursor,target));score=sum(lengths)-2*sum(c[0] for c in cuts)+bend*sum(c[4] for c in cuts);candidates.append((score,segments))
 if not candidates:raise ValueError('Не хватает места для круглых осевых переходов. Увеличьте длину звена.')
 def clear(segments):
  if not obstacles:return True
  for segment in segments:
   for sample in range(25):
    t=sample/24
    if segment['Kind']=='line':p=plus(segment['Start'],times(vec(segment['Start'],segment['End']),t));tangent=norm(vec(segment['Start'],segment['End']))
    else:
     e0=norm(vec(segment['Center'],segment['Start']));e1=cross(segment['Axis'],e0);a=t*segment['Angle'];p=plus(segment['Center'],times(plus(times(e0,math.cos(a)),times(e1,math.sin(a))),segment['Radius']));tangent=plus(times(e0,-math.sin(a)),times(e1,math.cos(a)))
    u=norm(cross([0,0,1] if abs(tangent[2])<.9 else [0,1,0],tangent));v=cross(tangent,u)
    for obstacle in obstacles:
     relative=vec(obstacle['Origin'],p);height=dot(relative,obstacle['Axis']);radial=plus(relative,times(obstacle['Axis'],-height))
     if height+radius<obstacle['Min'] or height-radius>obstacle['Max'] or math.sqrt(dot(radial,radial))-radius>obstacle['Radius']:continue
     for ring in [0,.5,1]:
      for n in range(24 if ring else 1):
       angle=math.tau*n/24;point=plus(p,times(plus(times(u,math.cos(angle)),times(v,math.sin(angle))),radius*ring));r=vec(obstacle['Origin'],point);h=dot(r,obstacle['Axis']);r=plus(r,times(obstacle['Axis'],-h))
       if obstacle['Min']+1e-6<h<obstacle['Max']-1e-6 and dot(r,r)<(obstacle['Radius']-1e-6)**2:return False
  return True
 for _,segments in sorted(candidates,key=lambda v:v[0]):
  if clear(segments):return segments,min(s['Radius'] for s in segments if s['Kind']=='arc')
 raise ValueError('Нет осевого перехода с зазором до корпуса привода. Увеличьте расстояние между шарнирами.')

def saddle(fr,distance,big,small,depth):
 """Extruded exact convex hull of two unequal circles (no triangular web)."""
 nx=(big-small)/distance
 if abs(nx)>=1:raise ValueError('Некорректные радиусы переходного седла.')
 ny=math.sqrt(1-nx*nx)
 a=(big*nx,big*ny);b=(distance+small*nx,small*ny);c=(b[0],-b[1]);d=(a[0],-a[1])
 wire=BRepBuilderAPI_MakeWire()
 wire.Add(BRepBuilderAPI_MakeEdge(point(fr,*a),point(fr,*b)).Edge())
 wire.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(point(fr,*b),point(fr,distance+small,0),point(fr,*c)).Value()).Edge())
 wire.Add(BRepBuilderAPI_MakeEdge(point(fr,*c),point(fr,*d)).Edge())
 wire.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(point(fr,*d),point(fr,-big,0),point(fr,*a)).Value()).Edge())
 return BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(wire.Wire()).Face(),gp_Vec(*times(fr[3],depth))).Shape()
