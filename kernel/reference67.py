"""Native CAD layout matching the visual reference; no game vertices/rig/units.
Front stator mounts keep the upper arm flat. Wrist motors live in the forearm.
Drive transmission, material strength and detailed hardware remain unapproved.
"""
import math,time,base64,json
from pathlib import Path
from OCP.gp import gp_Pnt,gp_Vec,gp_Dir,gp_Trsf,gp_Ax1,gp_Ax2
from OCP.BRepAlgoAPI import BRepAlgoAPI_Fuse,BRepAlgoAPI_Cut,BRepAlgoAPI_Common
from OCP.BRepOffsetAPI import BRepOffsetAPI_ThruSections
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeFace,BRepBuilderAPI_MakePolygon
from OCP.BRepPrimAPI import BRepPrimAPI_MakePrism,BRepPrimAPI_MakeCone,BRepPrimAPI_MakeRevol
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeEdge,BRepBuilderAPI_MakeWire
from OCP.GC import GC_MakeArcOfCircle
from OCP.BRepAdaptor import BRepAdaptor_Surface
from OCP.GeomAbs import GeomAbs_Cylinder
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
from procedural_body import cylinder,box,plus,times,vec,norm,cross,dot,frame,point,transform,curve_profile
from joint_geometry import saddle

def number(spec):return int(spec['JointId'].rsplit('j',1)[-1])
def dims(index,spec):
 motor=spec['Motor'];bearing=spec['Bearing'];mechanical=spec['Mechanical'];large=index<4
 outer=max(float(spec['Reducer']['Diameter'])/2+(12 if large else 2),float(bearing['Outer'])/2+float(mechanical.get('FrameWall',4))+3)
 if large:outer=max(outer,float(motor['Frame'])/math.sqrt(2)+float(mechanical.get('FrameWall',4))+3)
 rear=-(float(spec['Reducer']['Length'])+float(mechanical['Plate'])+float(motor['Length'])+9.2) if large else -(2*float(bearing['Width'])+18)
 return dict(outer=outer,rear=rear,stator_pc=outer+(5 if large else -2),stator_r=outer+(14 if large else 5),root_r=outer-(7 if large else 8),root_pc=outer-(16 if large else 14),root_z=24 if large else 14)
def build(spec,folder,k):
 started=time.monotonic();folder=Path(folder);folder.mkdir(parents=True,exist_ok=True);index=number(spec);d=dims(index,spec);m=spec['Mechanical'];bearing=spec['Bearing'];motor=spec['Motor'];wall=float(m['FrameWall']);skin=float(m['SkinWall']);gap=float(m['Clearance']);rho=float(m['Density']);parts=[];issues=[];mates=[]
 def progress(t):print('GEARLAB_PROGRESS:'+t,flush=True)
 def boolean(cls,a,b):
  op=cls(a,b);op.SetFuzzyValue(1e-6);op.Build();return k.checked(op.Shape())
 def fuse(a,b):return boolean(BRepAlgoAPI_Fuse,a,b)
 def cut(a,b):return boolean(BRepAlgoAPI_Cut,a,b)
 def add(pid,label,s,side=False,role='frame',density=None,color='#858c93'):
  s=k.checked(s)
  if len(k.shapes(s,k.TopAbs_SOLID))!=1:
   (folder/'disconnected.json').write_text(json.dumps([dict(volume=k.props(q)[0],bounds=k.bounds(q)) for q in k.shapes(s,k.TopAbs_SOLID)],indent=2))
   raise ValueError(label+': отдельная деталь содержит несвязные тела')
  parts.append(dict(Id=pid,Label=label,Shape=s,InputSide=side,Role=role,Density=density or rho,Color=color));return s
 def issue(code,status,message,pid=None):issues.append(dict(Code=code,Status=status,Message=message,PartId=pid,Position=[0,0,0],Scope='assembly'))
 def pattern(fr,pc):return [k.xyz(point(fr,pc*math.cos(math.pi/4+i*math.pi/2),pc*math.sin(math.pi/4+i*math.pi/2))) for i in range(4)]
 def drill(s,fr,pc,radius,depth):
  for q in pattern(fr,pc):s=cut(s,cylinder(radius,depth,q,fr[3]))
  return s
 def port(pid,part,fr,pc,peer=None):mates.append(dict(Id=pid,PartId=part,PeerJoint=peer,Origin=fr[0],X=fr[1],Y=fr[2],Normal=fr[3],BoltCircle=2*pc,BoltDiameter=3.4,BoltPoints=pattern(fr,pc)))
 def annulus(outer,inner,length,origin,axis=(0,0,1)):return cut(cylinder(outer,length,origin,axis),cylinder(inner,length+2,plus(origin,times(axis,-1)),axis))
 def bolt(pid,origin,axis,length,side=False,size=3):
  s=fuse(cylinder(size/2,length,origin,axis),cylinder(size*.9,2,plus(origin,times(axis,-2)),axis));add(pid,'Винт M'+str(size)+' · '+pid,s,side,'hardware',7850,'#aeb5bb')
 def loft(a,b,widths,half,inset=0):
  u=norm(vec(a,b));n=plus([0,0,1],times(u,-u[2]));n=norm(n) if dot(n,n)>.01 else [0,1,0];v=norm(cross(n,u));op=BRepOffsetAPI_ThruSections(True,True,1e-6);op.CheckCompatibility(False)
  for f,w in widths:
   pos=plus(a,times(vec(a,b),f));x=w-inset;y=half-inset;c=min(x,y)*.22;polygon=BRepBuilderAPI_MakePolygon()
   for px,py in [(-x+c,-y),(x-c,-y),(x,-y+c),(x,y-c),(x-c,y),(-x+c,y),(-x,y-c),(-x,-y+c)]:polygon.Add(gp_Pnt(*plus(plus(pos,times(v,px)),times(n,py))))
   polygon.Close();op.AddWire(polygon.Wire())
  op.Build();return k.checked(op.Shape()),(a,u,v,n)
 def dome(radius,depth,z):
  a0=gp_Pnt(0,0,z);p0=gp_Pnt(radius,0,z);p1=gp_Pnt(radius*.72,0,z-depth*.72);p2=gp_Pnt(0,0,z-depth)
  wire=BRepBuilderAPI_MakeWire();wire.Add(BRepBuilderAPI_MakeEdge(a0,p0).Edge());wire.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(p0,p1,p2).Value()).Edge());wire.Add(BRepBuilderAPI_MakeEdge(p2,a0).Edge())
  return k.checked(BRepPrimAPI_MakeRevol(BRepBuilderAPI_MakeFace(wire.Wire()).Face(),gp_Ax1(gp_Pnt(0,0,0),gp_Dir(0,0,1))).Shape())
 progress('Новая компоновка · опоры и передние монтажные фланцы')
 bi=float(bearing['Inner']);bo=float(bearing['Outer']);bw=float(bearing['Width']);depth=2*bw+12;fit=float(m.get('BearingFit',.2));rootz=d['root_z'];front_hole=(bo-5 if bo>35 else bo-2)/2
 # Fixed actuator housing. Motors of J4–J6 are deliberately absent here.
 if index<4:
  length=-bw-d['rear'];ax=gp_Ax2(gp_Pnt(0,0,d['rear']),gp_Dir(0,0,1));case=cut(BRepPrimAPI_MakeCone(ax,d['outer'],d['stator_r'],length).Shape(),BRepPrimAPI_MakeCone(ax,d['outer']-wall,d['stator_r']-wall,length).Shape())
  mz=-float(spec['Reducer']['Length'])-float(m['Plate']);mount_radius=d['outer']+(d['stator_r']-d['outer'])*(mz-d['rear'])/length-wall+1;plate=cylinder(mount_radius,float(m['Plate']),[0,0,mz]);plate=cut(plate,cylinder(float(motor['Pilot'])/2+gap,float(m['Plate'])+2,[0,0,mz-1]))
  for x in [-1,1]:
   for y in [-1,1]:plate=cut(plate,cylinder(1.7,float(m['Plate'])+2,[x*float(motor['Pitch'])/2,y*float(motor['Pitch'])/2,mz-1]))
  case=fuse(case,plate)
  for q in pattern(frame([0,0,d['rear']],[0,0,1]),d['outer']-8):
   boss=cylinder(7,14,q);boss=cut(boss,cylinder(1.25,16,plus(q,[0,0,-1])));case=fuse(case,boss)
 else:case=annulus(d['outer'],bo/2+wall,-bw-d['rear'],[0,0,d['rear']])
 rear_outer=d['outer']+(d['stator_r']-d['outer'])*(-depth-d['rear'])/(-bw-d['rear']) if index<4 else d['outer'];rear=annulus(rear_outer,front_hole,bw+3,[0,0,-depth]);rear=cut(rear,cylinder(bo/2+fit,bw+.01,[0,0,-depth-.01]));case=fuse(case,rear)
 front=annulus(d['stator_r'],front_hole,bw,[0,0,-bw]);front=cut(front,cylinder(bo/2+fit,bw-.01,[0,0,-bw+.01]));front=drill(front,frame([0,0,-bw-1],[0,0,1]),d['stator_pc'],1.7,bw+2);add('front_support','Передняя неподвижная опора и монтажный фланец',front,True,'mount');port('stator_mount','front_support',frame([0,0,0],[0,0,1]),d['stator_pc'])
 if index<4:
  back=cut(dome(d['outer'],18,d['rear']),dome(d['outer']-wall,18-wall,d['rear']+.1));back=cut(back,cylinder(4,22,[0,0,d['rear']-20]));back=drill(back,frame([0,0,d['rear']-20],[0,0,1]),d['outer']-8,1.7,22);add('rear_cover','Выпуклая съёмная крышка привода',back,True,'mount')
  for i,q in enumerate(pattern(frame([0,0,d['rear']-10],[0,0,1]),d['outer']-8)):bolt('case_back_'+str(i),q,[0,0,1],20,True)
 # The cover bolts attach the split casing to the front support.
 for side,(lo,hi) in enumerate([(-1000,-gap/2),(gap/2,1000)]):add('actuator_'+str(side),'Корпус привода · половина '+str(side+1),k.checked(BRepAlgoAPI_Common(case,box([-1000,lo,-1000,1000,hi,1000])).Shape()),True,'mount')
 # Output journal and flange; spacer prevents a printed shoulder rubbing the inner rings.
 shaft=annulus(bi/2-.02,float(motor['Shaft'])/2+gap+.3,depth+rootz-5,[0,0,-depth]);flange=annulus(d['root_r'],float(motor['Shaft'])/2+gap+.3,6,[0,0,rootz-6]);shaft=fuse(shaft,flange);shaft=fuse(shaft,annulus(bi/2+3,float(motor['Shaft'])/2+gap+.3,rootz-6,[0,0,0]));shaft=drill(shaft,frame([0,0,rootz-7],[0,0,1]),d['root_pc'],1.25,8);add('output_flange','Металлический вал и выходной фланец',shaft,False,'metal',7850,'#a7afb6');port('rotor_output','output_flange',frame([0,0,rootz],[0,0,1]),d['root_pc'])
 add('bearing_spacer','Распорная втулка внутренних колец',annulus(bi/2+3,bi/2+.05,12,[0,0,-depth+bw]),False,'metal',7850)
 # The moving arm connects the front stator face, not the remote back of the motor.
 child=(spec.get('Children') or [None])[0];end=spec['End'];rootplate=drill(annulus(62 if index==2 else d['root_r']+6,bi/2+3.2,8,[0,0,rootz]),frame([0,0,rootz-1],[0,0,1]),d['root_pc'],1.7,10)
 if child:
  cd=dims(int(child['Id'].rsplit('j',1)[-1]),child);tr=child['Transform'];fr=([tr[3],tr[7],tr[11]],[tr[0],tr[4],tr[8]],[tr[1],tr[5],tr[9]],[tr[2],tr[6],tr[10]]);target=fr[0];tipplate=transform(annulus(92 if index==1 else cd['stator_r']+6,cd['root_r']+gap+1.5,8,[0,0,0]),fr);tipplate=drill(tipplate,(plus(target,times(fr[3],-1)),fr[1],fr[2],fr[3]),cd['stator_pc'],1.7,10)
 else:
  fr=frame(end,norm(end));target=end;tipplate=drill(annulus(23,7,8,target,fr[3]),(plus(target,times(fr[3],-1)),fr[1],fr[2],fr[3]),17,1.7,10);cd=None
 a=[0,0,rootz+4];b=plus(target,times(fr[3],4));distance=math.dist(a,b)
 if index==1:
  # Low broad plinth; all skirt sectors are independent solids.
  base_r=float(spec.get('DesignHeight',1000))*.28;base_z=-float(spec['GroundDepth']);h=100
  shell=cut(BRepPrimAPI_MakeCone(gp_Ax2(gp_Pnt(0,0,base_z),gp_Dir(0,0,1)),base_r,base_r-22,h).Shape(),cylinder(base_r-36,h+2,[0,0,base_z-1]))
  for i in range(8):
   poly=BRepBuilderAPI_MakePolygon();angle0=i*math.pi/4;angle1=(i+1)*math.pi/4
   for q in [[0,0,base_z-1],[base_r*2*math.cos(angle0),base_r*2*math.sin(angle0),base_z-1],[base_r*2*math.cos(angle1),base_r*2*math.sin(angle1),base_z-1]]:poly.Add(gp_Pnt(*q))
   poly.Close();wedge=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(poly.Wire()).Face(),gp_Vec(0,0,h+2)).Shape();part=k.checked(BRepAlgoAPI_Common(shell,wedge).Shape());add('base_sector_'+str(i),'Сектор низкого основания '+str(i+1),part,True,'frame')
  top_ring=annulus(base_r-22,54,5,[0,0,base_z+h-5])
  for i in range(8):
   poly=BRepBuilderAPI_MakePolygon();a0=i*math.pi/4;a1=(i+1)*math.pi/4
   for q in [[0,0,base_z+h-6],[base_r*2*math.cos(a0),base_r*2*math.sin(a0),base_z+h-6],[base_r*2*math.cos(a1),base_r*2*math.sin(a1),base_z+h-6]]:poly.Add(gp_Pnt(*q))
   poly.Close();wedge=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(poly.Wire()).Face(),gp_Vec(0,0,7)).Shape();add('base_top_'+str(i),'Крышка сектора основания '+str(i+1),k.checked(BRepAlgoAPI_Common(top_ring,wedge).Shape()),True,'skin')
  # Separate central mounting plate and cardinal anchor feet, with actual bores.
  for i in range(4):
   ang=i*math.pi/2;rot=gp_Trsf();rot.SetRotation(gp_Ax1(gp_Pnt(0,0,0),gp_Dir(0,0,1)),ang)
   foot=box([base_r-40,-38,base_z,base_r+40,38,base_z+9])
   for y in [-22,22]:foot=cut(foot,cylinder(2.75,11,[base_r+22,y,base_z-1]))
   for y in [-25,20]:
    poly=BRepBuilderAPI_MakePolygon()
    for q in [[base_r-12,y,base_z+9],[base_r-12,y,base_z+78],[base_r+28,y,base_z+9]]:poly.Add(gp_Pnt(*q))
    poly.Close();rib=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(poly.Wire()).Face(),gp_Vec(0,5,0)).Shape();foot=fuse(foot,rib)
   add('anchor_foot_'+str(i),'Анкерная лапа с рёбрами '+str(i+1),BRepBuilderAPI_Transform(foot,rot,False).Shape(),True)
  turn=annulus(175,bi/2+3.2,12,[0,0,rootz]);turn=drill(turn,frame([0,0,rootz-1],[0,0,1]),d['root_pc'],1.7,14);rootplate=fuse(rootplate,turn)
  a=[0,0,180];vdir=vec(a,b);u=norm(vdir);v=norm(cross([0,1,0],u));bone=saddle((plus(a,[0,-32,0]),u,v,[0,1,0]),math.dist(a,b),64,88,64);inner=saddle((plus(a,[0,-28,0]),u,v,[0,1,0]),math.dist(a,b),60,84,56);power=cut(bone,inner);power=fuse(power,rootplate);poly=BRepBuilderAPI_MakePolygon()
  for q in [[-145,-40,30],[20,-40,30],[20,-40,160],[-100,-40,160],[-145,-40,105]]:poly.Add(gp_Pnt(*q))
  poly.Close();pedestal=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(poly.Wire()).Face(),gp_Vec(0,80,0)).Shape();pedestal=cut(pedestal,box([-125,-36,34,16,36,140]));power=fuse(power,pedestal)
 elif index==2:
  u=norm(vec(a,b));n=norm(plus([0,0,1],times(u,-u[2])));v=norm(cross(n,u));axes=(a,u,v,n)
  def flat_beam(inset):
   polygon=BRepBuilderAPI_MakePolygon();sections=[(0,54),(.16,49),(.86,39),(1,44)]
   for f,w in sections+[(f,-w) for f,w in reversed(sections)]:
    x=f*distance;y=w-math.copysign(inset,w);q=plus(plus(plus(a,times(u,x)),times(v,y)),times(n,-20+inset));polygon.Add(gp_Pnt(*q))
   polygon.Close();return BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(polygon.Wire()).Face(),gp_Vec(*times(n,40-2*inset))).Shape()
  power=fuse(cut(flat_beam(0),flat_beam(wall)),rootplate)
 elif index==3:
  # Tapered forearm barrel, with an actual removable long cover and three remote motor mounts.
  u=norm(vec(a,b));n=norm(plus([0,0,1],times(u,-u[2])));v=norm(cross(n,u));axes=(a,u,v,n);ax=gp_Ax2(gp_Pnt(*a),gp_Dir(*u));outer=BRepPrimAPI_MakeCone(ax,49,32,distance).Shape();inner=BRepPrimAPI_MakeCone(ax,49-wall,32-wall,distance).Shape();power=fuse(cut(outer,inner),rootplate)
 else:
  outer,axes=loft(a,b,[(0,24),(.35,34),(.8,34),(1,33)],18);inner,_=loft(a,b,[(0,24),(.35,34),(.8,34),(1,33)],18,wall);power=fuse(cut(outer,inner),rootplate)
 power=fuse(power,tipplate)
 # Physical keepouts around the fixed case and the next moving flange.
 obstacles=[cylinder(d['stator_r']+gap,-d['rear'],[0,0,d['rear']]),cylinder(d['root_r']+gap,6,[0,0,rootz-6])]
 if child:
  obstacles+=[transform(cylinder(cd['stator_r']+gap,-cd['rear'],[0,0,cd['rear']]),fr),transform(cylinder(cd['root_r']+gap,80,[0,0,8.2]),fr)]
 for obstacle in obstacles:power=cut(power,obstacle)
 if index==1:power=k.checked(BRepAlgoAPI_Common(power,box([-1000,-1000,rootz,1000,1000,2000])).Shape())
 # Reassert the real mating plates after trimming the cosmetic envelope.
 power=fuse(power,rootplate);power=fuse(power,tipplate)
 if index==2:
  aa,uu,vv,nn=axes;half=20 if index==2 else 38;mid=plus(aa,times(vec(aa,b),.5));length=max(30,distance-150);w=23 if index==2 else 22
  def plate_rect(extra,z,height):
   ff=(plus(mid,times(nn,z)),uu,vv,nn);wire=curve_profile(ff,length/2+extra,w+extra,'rounded_rectangle',.2);return BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(wire).Face(),gp_Vec(*times(nn,height))).Shape()
  opening=plate_rect(0,half-wall-1,wall+skin+gap+3);power=cut(power,opening);panel=plate_rect(6,half+gap,skin)
  for i,(x,y) in enumerate((x,y) for x in [-length/2+8,length/2-8] for y in [-w-3,w+3]):
   face=plus(plus(plus(mid,times(uu,x)),times(vv,y)),times(nn,half));boss=cylinder(5.5,7,plus(face,times(nn,-7)),nn);power=fuse(power,boss);power=cut(power,cylinder(2.3,6,face,times(nn,-1)));panel=cut(panel,cylinder(1.7,skin+2,plus(face,times(nn,gap-1)),nn));add('panel_insert_'+str(i),'Термовставка M3',annulus(2.3,1.25,6,plus(face,times(nn,-6)),nn),False,'insert',8500,'#b79a55');bolt('panel_screw_'+str(i),plus(face,times(nn,gap+skin)),times(nn,-1),8)
  add('service_cover','Плоская съёмная крышка',panel,False,'skin',color='#92999f')
 if index==3:
  # Keep the wrist slim without scaling NEMA17. Each motor retains 42.3 ×42.3×44 mm.
  aa,uu,vv,nn=axes
  for i,fraction in enumerate([.24,.43,.62]):
   origin=plus(aa,times(vec(aa,b),fraction));face=plus(origin,times(uu,44));ff=(origin,vv,nn,uu);body=transform(box([-21.15,-21.15,0,21.15,21.15,44]),ff);add('remote_motor_'+str(i),'NEMA17 · дистанционный привод кисти '+str(i+1),body,False,'motor',.29/(42.3*42.3*44*1e-9),'#454a50')
   shaft=cylinder(2.5,15,face,uu);add('remote_shaft_'+str(i),'Вал NEMA17 Ø5',shaft,False,'metal',7850)
   support=annulus(39,11.3,4,face,uu)
   for x in [-15.5,15.5]:
    for y in [-15.5,15.5]:support=cut(support,cylinder(1.7,6,plus(plus(plus(face,times(vv,x)),times(nn,y)),times(uu,-1)),uu))
   add('remote_mount_'+str(i),'Съёмная монтажная пластина двигателя кисти',support)
   spool=annulus(13,2.6,9,plus(face,times(uu,4)),uu);add('wrist_capstan_'+str(i),'Барабан дистанционного привода · компоновка',spool,False,'metal',7850)
  issue('remote_wrist','pending','Двигатели кисти размещены в предплечье; силовые тяги, направляющие, натяжение и передаточные отношения ещё не рассчитаны.')
 if index==1:
  hatch=transform(box([-24,-32,0,24,32,5]),([0,-37,180],[1,0,0],[0,0,1],[0,-1,0]));add('lower_access_hatch','Нижняя сервисная крышка',hatch)
 if index==3:
  from OCP.Geom import Geom_BezierCurve
  from OCP.collections import Array1_gp_Pnt as TColgp_Array1OfPnt
  from OCP.BRepOffsetAPI import BRepOffsetAPI_MakePipe
  for i,offset in enumerate([-10,0,10]):
   controls=[]
   for along,across,back in [(80,-20,0),(-100,-20,-80),(-120,110,-80),(0,110,-90)]:controls.append(plus(plus(plus(a,times(uu,along)),times(vv,across+offset)),times(nn,back)))
   arr=TColgp_Array1OfPnt(1,4)
   for n0,q in enumerate(controls,1):arr.SetValue(n0,gp_Pnt(*q))
   curve=Geom_BezierCurve(arr);path=BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(curve).Edge()).Wire();direction=norm(vec(controls[0],controls[1]));circle=BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(k.gp_Circ(gp_Ax2(gp_Pnt(*controls[0]),gp_Dir(*direction)),3)).Edge()).Wire();pipe=BRepOffsetAPI_MakePipe(path,BRepBuilderAPI_MakeFace(circle).Face()).Shape();add('cable_guide_'+str(i),'Кабельный рукав · компоновочный маршрут',pipe,False,'guide',1200,'#4d5359')
 power=drill(power,frame([0,0,rootz-1],[0,0,1]),d['root_pc'],1.7,10)
 power=drill(power,(plus(target,times(fr[3],-1)),fr[1],fr[2],fr[3]),cd['stator_pc'] if child else 17,1.7,10)
 if index!=2:
  from OCP.ShapeUpgrade import ShapeUpgrade_UnifySameDomain
  unify=ShapeUpgrade_UnifySameDomain(power,True,True,False);unify.Build();power=k.checked(unify.Shape())
 add('frame_0','Силовой корпус по компоновке визуального образца',power)
 port('link_input','frame_0',frame([0,0,rootz],[0,0,1]),d['root_pc'])
 port('link_child' if child else 'tool_mount','frame_0',fr,cd['stator_pc'] if child else 17,child['Id'] if child else None)
 issue('reference_silhouette','info','Новая компоновка по визуальному образцу. Сходство оценивается сравнением видов; игровые данные не использованы.')
 issue('engineering_review','pending','Прочность, передача момента, осевая фиксация, секционирование и сборочные допуски новой компоновки требуют отдельного завершения.')
 # Export exact STEP and matching viewport meshes. Printing is explicitly not approved.
 bodies=[];meta=[]
 for p in parts:
  progress('CAD по дизайну · '+p['Id']);s=p['Shape'];path=folder/(p['Id']+'.step');writer=STEPControl_Writer();writer.Transfer(s,STEPControl_AsIs)
  if writer.Write(str(path))!=IFSelect_RetDone:raise ValueError('STEP export failed')
  read=k.import_step(base64.b64encode(path.read_bytes()).decode());volume,centre=k.props(read);bounds=k.bounds(read);mesh=str(folder/(p['Id']+'.cadmesh'));faces,edges,triangles=k.mesh_and_topology(read,mesh)
  if not math.isclose(volume,k.props(s)[0],rel_tol=1e-5,abs_tol=.003):
   from OCP.BRepTools import BRepTools
   BRepTools.Write_s(s,str(folder/(p['Id']+'-before.brep')))
   raise ValueError('STEP changed native volume '+p['Id']+': '+str(k.props(s)[0])+' -> '+str(volume))
  bodies.append(dict(Id=p['Id'],Volume=volume,Center=centre,Bounds=bounds,Faces=faces,Edges=edges,Triangles=triangles,Mesh=mesh));meta.append({key:value for key,value in p.items() if key!='Shape'});meta[-1].update(StepPath=str(path),PrintAngles=None)
 for mate in mates:
  # Actual matching bore geometry is independently checked by the C# mating tests.
  shape=next(p['Shape'] for p in parts if p['Id']==mate['PartId']);verified=[]
  for q in mate['BoltPoints']:
   found=False
   for face in k.shapes(shape,k.TopAbs_FACE):
    surface=BRepAdaptor_Surface(k.cast(face,k.TopAbs_FACE))
    if surface.GetType()!=GeomAbs_Cylinder:continue
    cy=surface.Cylinder();axis=k.xyz(cy.Axis().Direction());delta=vec(q,k.xyz(cy.Axis().Location()))
    if abs(cy.Radius()-(1.25 if mate['Id']=='rotor_output' else 1.7))<1e-4 and abs(abs(dot(axis,mate['Normal']))-1)<1e-6 and math.sqrt(dot(cross(delta,mate['Normal']),cross(delta,mate['Normal'])))<1e-4:found=True;break
   verified.append(found)
  # Rotor tapped bores are Ø2.5, not clearance Ø3.4.
  if mate['Id']=='rotor_output':mate['BoltDiameter']=2.5
  mate['BoresVerified']=all(verified)
  if not mate['BoresVerified']:issue('mount_bore','error','Не найдено реальное отверстие стыка '+mate['Id'],mate['PartId'])
 result=dict(Parts=meta,Issues=issues,Mates=mates,Modules={'connected':'reference67-r1','design':'reference-silhouette'},ChangedModules=['reference67'],RepairedParts=[],ProtectedVolumes=[],MassKg=sum(b['Volume']*p['Density']*1e-9 for b,p in zip(bodies,parts)),MinFrameRadius=0,NominalStress=0,Segments=1,GeometryValid=not any(i['Status']=='error' for i in issues),ReadyForManufacture=False,Seconds=time.monotonic()-started)
 return dict(Ok=True,Bodies=bodies,Procedural=result)

def generate(spec,folder,k):
 try:return build(spec,folder,k)
 except Exception:
  import traceback
  (Path(folder)/'reference67-trace.txt').write_text(traceback.format_exc(),encoding='utf-8')
  raise
