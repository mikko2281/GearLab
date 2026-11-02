"""Skeleton/component-driven, editable solid housing generator.

All final parts are OCCT solids. This is a sizing/geometry tool, not FEA or a
fatigue/assembly certification. Unproved checks remain explicit in the report.
"""
import math,json,hashlib,base64,time
from pathlib import Path
from OCP.gp import gp_Pnt,gp_Vec,gp_Dir,gp_Ax2,gp_Ax1,gp_Circ,gp_Elips,gp_Trsf
from OCP.BRepPrimAPI import BRepPrimAPI_MakeBox,BRepPrimAPI_MakeCylinder,BRepPrimAPI_MakePrism
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeWire,BRepBuilderAPI_MakeEdge,BRepBuilderAPI_MakeFace,BRepBuilderAPI_Transform
from OCP.BRepOffsetAPI import BRepOffsetAPI_ThruSections,BRepOffsetAPI_MakePipe
from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut,BRepAlgoAPI_Fuse,BRepAlgoAPI_Common
from OCP.GC import GC_MakeArcOfCircle
from OCP.BRepCheck import BRepCheck_Analyzer
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone

ENGINE_VERSION="gearlab-procedural-1"
SHAPE_CACHE={}
PRESETS={
 'Reference':dict(Slimness=.78,JointCompactness=.85,Smoothness=.7,Roundness=.85,Taper=.72,Symmetry=1,PanelCoverage=.72,HiddenFasteners=True,MechanicalExposure=0,AccentColorCoverage=.32,Profile='oval',Strength=1),
 'Ultra Slim':dict(Slimness=1,JointCompactness=1,Smoothness=.65,Roundness=.9,Taper=1,Symmetry=1,PanelCoverage=.5,HiddenFasteners=True,MechanicalExposure=.1,AccentColorCoverage=.18,Profile='oval',Strength=1),
 'Lightweight':dict(Slimness=.95,JointCompactness=.9,Smoothness=.6,Roundness=.7,Taper=.9,Symmetry=1,PanelCoverage=.4,HiddenFasteners=False,MechanicalExposure=.2,AccentColorCoverage=.2,Profile='oval',Strength=1),
 'High Strength':dict(Slimness=.4,JointCompactness=.5,Smoothness=.8,Roundness=.8,Taper=.35,Symmetry=1,PanelCoverage=.8,HiddenFasteners=False,MechanicalExposure=0,AccentColorCoverage=.2,Profile='rounded_rectangle',Strength=1.4),
 'Easy Print':dict(Slimness=.65,JointCompactness=.7,Smoothness=.3,Roundness=.5,Taper=.5,Symmetry=1,PanelCoverage=.65,HiddenFasteners=False,MechanicalExposure=.05,AccentColorCoverage=.3,Profile='rounded_rectangle',Strength=1),
}

def vec(a,b):return [b[i]-a[i] for i in range(3)]
def plus(a,b):return [a[i]+b[i] for i in range(3)]
def times(a,t):return [x*t for x in a]
def norm(a):
 n=math.sqrt(sum(x*x for x in a))
 if n<1e-9:raise ValueError('Нулевая ось или длина звена.')
 return [x/n for x in a]
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def digest(value):return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def memo(key,make):
 if key not in SHAPE_CACHE:
  SHAPE_CACHE[key]=make()
  if len(SHAPE_CACHE)>100:SHAPE_CACHE.pop(next(iter(SHAPE_CACHE)))
 return SHAPE_CACHE[key]
def frame(origin,normal):
 n=norm(normal);u=norm(cross([0,0,1] if abs(n[2])<.9 else [0,1,0],n));v=cross(n,u);return origin,u,v,n
def point(fr,x,y,z=0):return gp_Pnt(*(fr[0][i]+fr[1][i]*x+fr[2][i]*y+fr[3][i]*z for i in range(3)))
def transform(shape,fr):
 tr=gp_Trsf();tr.SetValues(fr[1][0],fr[2][0],fr[3][0],fr[0][0],fr[1][1],fr[2][1],fr[3][1],fr[0][1],fr[1][2],fr[2][2],fr[3][2],fr[0][2]);return BRepBuilderAPI_Transform(shape,tr,True).Shape()
def cylinder(radius,length,origin=(0,0,0),axis=(0,0,1)):
 if min(radius,length)<=0:raise ValueError('Положительные размеры цилиндра обязательны.')
 return BRepPrimAPI_MakeCylinder(gp_Ax2(gp_Pnt(*origin),gp_Dir(*axis)),radius,length).Shape()
def box(bounds):return BRepPrimAPI_MakeBox(gp_Pnt(*bounds[:3]),*(bounds[i+3]-bounds[i] for i in range(3))).Shape()
def curve_profile(fr,a,b,kind,roundness=.8):
 if min(a,b)<=.05:raise ValueError('Слишком малое сечение.')
 if kind=='oval':
  u=fr[1] if a>=b else fr[2];return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(gp_Elips(gp_Ax2(gp_Pnt(*fr[0]),gp_Dir(*fr[3]),gp_Dir(*u)),max(a,b),min(a,b))).Edge()).Wire()
 r=min(a,b)*max(.1,min(.95,roundness));wire=BRepBuilderAPI_MakeWire()
 starts=[(a-r,b),(-a+r,b),(-a,b-r),(-a,-b+r),(-a+r,-b),(a-r,-b),(a,-b+r),(a,b-r)]
 # Four straight segments and exact quarter-circle corners.
 centers=[(-a+r,b-r),(-a+r,-b+r),(a-r,-b+r),(a-r,b-r)]
 for i in range(4):
  p,q=starts[2*i],starts[(2*i+1)%8];wire.Add(BRepBuilderAPI_MakeEdge(point(fr,*p),point(fr,*q)).Edge())
  c=centers[i];end=starts[(2*i+2)%8];mid=norm([q[0]-c[0]+end[0]-c[0],q[1]-c[1]+end[1]-c[1],0]);m=[c[0]+mid[0]*r,c[1]+mid[1]*r]
  wire.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(point(fr,*q),point(fr,*m),point(fr,*end)).Value()).Edge())
 return wire.Wire()
def loft(sections,kind,roundness):
 b=BRepOffsetAPI_ThruSections(True,False,1e-6)
 for fr,a,bb in sections:b.AddWire(curve_profile(fr,a,bb,kind,roundness))
 b.Build()
 if not b.IsDone():raise ValueError('Не удалось построить плавный лофт.')
 return b.Shape()
def disjoint(a,b,pad=0):return any(a[i+3]+pad<b[i] or b[i+3]+pad<a[i] for i in range(3))

def generate(spec,folder,k):
 started=time.monotonic();folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
 if spec.get('Version',1)!=1:raise ValueError('Неизвестная версия процедурного проекта.')
 end=spec.get('End',[180,0,0]);length=math.sqrt(dot(end,end))
 if not 20<=length<=1500:raise ValueError('Длина звена должна быть 20–1500 мм.')
 motor=spec.get('Motor') or {};reducer=spec.get('Reducer') or {};bearing=spec.get('Bearing') or {};m=spec.get('Mechanical') or {};style=dict(PRESETS.get(spec.get('Variant','Reference'),PRESETS['Reference']));style.update(spec.get('Style') or {})
 for key in ['Slimness','JointCompactness','Smoothness','Roundness','Taper','Symmetry','PanelCoverage','MechanicalExposure','AccentColorCoverage']:
  if not 0<=float(style[key])<=1:raise ValueError('Параметр '+key+' должен быть от 0 до 1.')
 f=float(motor.get('Frame',41.9));ml=float(motor.get('Length',37.5));rd=float(reducer.get('Diameter',76));rl=float(reducer.get('Length',32));pitch=float(motor.get('Pitch',31.3));pilot=float(motor.get('Pilot',22));shaft=float(motor.get('Shaft',4.65));bo=float(bearing.get('Outer',22));bi=float(bearing.get('Inner',8));bw=float(bearing.get('Width',7))
 wall=max(float(m.get('MinWall',2.4)),float(m.get('Wall',2.4)));fw=max(wall,float(m.get('FrameWall',3.2)))*float(style.get('Strength',1));gap=float(m.get('Clearance',.35));density=float(m.get('Density',1270));skin=float(m.get('SkinWall',2));plate=max(wall+2*gap,float(m.get('Plate',4)));bolt=float(m.get('Bolt',3));insert=float(m.get('InsertDiameter',4.6));insert_length=float(m.get('InsertLength',5.2));cable=float(m.get('CableDiameter',4));bend=max(cable*3,float(m.get('CableBendRadius',16)));printer=m.get('Printer',[256,256,256]);margin=float(m.get('PrintMargin',6));allow=float(m.get('AllowableStressMPa',12));moment=abs(float(m.get('DesignMomentNmm',3000)));torque=abs(float(m.get('DesignTorqueNmm',2000)));axial=abs(float(m.get('AxialForceN',20)))
 if not (15<=f<=150 and 10<=ml<=300 and 20<=rd<=250 and 10<=rl<=200 and .05<=gap<=2 and 1.2<=wall<=20 and 1<=skin<=8 and 2<=bolt<=8 and insert>bolt and 3<=insert_length<=20 and .1<=allow<=250 and 100<=density<=20000):raise ValueError('Некорректные габариты, материал, зазор или крепёж генератора.')
 report=[];parts=[];protections=[];modules={};changed=[]
 def progress(stage):print('GEARLAB_PROGRESS:'+stage,flush=True)
 def issue(code,status,message,part=None,position=None,scope=None):
  if position is None and part:
   target=next((p for p in parts if p['Id']==part),None)
   if target:position=k.props(target['Shape'])[1]
  report.append(dict(Code=code,Status=status,Message=message,PartId=part,Position=position or [0,0,0],Scope=scope or 'geometry'))
 def checked(s):return k.checked(s)
 def cut(s,tool):return checked(BRepAlgoAPI_Cut(s,tool).Shape())
 def fuse(a,b):return checked(BRepAlgoAPI_Fuse(a,b).Shape())
 def module(name,inputs,make):
  key=digest([ENGINE_VERSION,name,inputs]);modules[name]=key
  if (spec.get('PreviousModules') or {}).get(name)!=key:changed.append(name)
  return memo(key,make)
 def protect(pid,kind,shape,label):protections.append(dict(Id=pid,Kind=kind,Shape=shape,Label=label,Bounds=k.bounds(shape)))
 def add(pid,label,shape,side,role='frame',color=None):
  s=checked(shape)
  if len(k.shapes(s,k.TopAbs_SOLID))!=1:raise ValueError(label+': деталь распалась на несколько тел.')
  parts.append(dict(Id=pid,Label=label,Shape=s,InputSide=side,Role=role,Color=color or ('#ee7628' if role=='accent' else '#30343b'),Density=density))
 progress('A · Скелет, локальные оси, компоненты и нагрузка')
 motor_z=-rl-plate-ml;mx=float(motor.get('X',0));my=float(motor.get('Y',0));motor_bounds=[mx-f/2,my-f/2,motor_z,mx+f/2,my+f/2,motor_z+ml]
 occupied_motor=box(motor_bounds);motor_clear=box([motor_bounds[i]-gap if i<3 else motor_bounds[i]+gap for i in range(6)])
 protect('motor','occupied',occupied_motor,'Двигатель NEMA · физический объём');protect('motor_clear','clearance',motor_clear,'Зазор корпуса двигателя')
 protect('motor_service','service',box([motor_bounds[0]-gap,motor_bounds[1]-gap,motor_z-ml-plate,motor_bounds[3]+gap,motor_bounds[4]+gap,motor_z+ml-.2]),'Извлечение мотора после снятия задней крышки')
 reducer_shape=cylinder(rd/2,rl,[0,0,-rl]);protect('reducer','occupied',reducer_shape,'Габарит передачи')
 protect('reducer_clear','clearance',cylinder(rd/2+gap,rl+2*gap,[0,0,-rl-gap]),'Монтажный зазор передачи')
 protect('bearing','occupied',cylinder(bo/2,bw,[0,0,-bw]),'Подшипник')
 protect('bearing_clear','clearance',cylinder(bo/2+gap,bw+2*gap,[0,0,-bw-gap]),'Посадка подшипника')
 output_top=float(m.get('OutputTop',plate+15));protect('output_shaft','occupied',cylinder(bi/2,rl+output_top,[0,0,-rl]),'Выходной вал')
 protect('output_clear','clearance',cylinder(bi/2+float(m.get('ShaftFit',.1)),rl+output_top+2,[0,0,-rl-1]),'Зазор выходного вала')
 progress('B · Занятые, монтажные, сервисные и кабельные объёмы')
 physical_exact=False;physical_radius=0;physical_min_z=motor_z
 for component in spec.get('Components') or []:
  shape=k.import_step(component['Step']) if component.get('Step') else box(component['Bounds'])
  if component.get('Transform'):
   tr=gp_Trsf();tr.SetValues(*component['Transform']);shape=BRepBuilderAPI_Transform(shape,tr,True).Shape()
  protect(component['Id'],'occupied',shape,component.get('Label',component['Id']));cb=k.bounds(shape);protect(component['Id']+'_clear','clearance',box([cb[i]-gap if i<3 else cb[i]+gap for i in range(6)]),'Консервативный монтажный габарит STEP-компонента');physical_exact=True
  b=k.bounds(shape);physical_radius=max(physical_radius,math.hypot(max(abs(b[0]),abs(b[3])),max(abs(b[1]),abs(b[4])))+gap);physical_min_z=min(physical_min_z,b[2]-gap)
 # Closed beam section sized before styling. Stresses are nominal beam/Bredt
 # estimates, not a prediction for printed junctions or insert pull-out.
 def stress(radius):
  inner=max(.1,radius-fw);area=math.pi*(radius*radius-inner*inner);inertia=math.pi*(radius**4-inner**4)/4;normal=moment*radius/inertia+axial/area;median=math.pi*(radius-fw/2)**2;tau=torque/(2*median*fw);return math.sqrt(normal*normal+3*tau*tau)
 low=max(fw+3,cable/2+gap+fw,bend/2+gap+fw);high=max(12,low*1.5)
 while stress(high)>allow and high<150:high*=1.2
 if high>=150 and stress(high)>allow:raise ValueError('Для заданной нагрузки требуется слишком большое сечение. Измените материал или компоновку.')
 for _ in range(35):
  mid=(low+high)/2
  if stress(mid)>allow:low=mid
  else:high=mid
 min_radius=max(high,fw+cable/2+gap+2);motor_z=min(motor_z,physical_min_z);case_inner=max(physical_radius,rd/2+gap,math.hypot(abs(mx)+f/2,abs(my)+f/2)+gap+2);case_outer=case_inner+wall+(1-float(style['JointCompactness']))*3
 start=[0,0,max(output_top,plate*3+2*gap+max(min_radius,bo/2+fw+2))];finish=spec.get('Target') or end;direction=norm(vec(start,finish));path_length=math.sqrt(dot(vec(start,finish),vec(start,finish)));up=frame(start,direction)[2];sag=min(path_length*.08,12)*float(style['Smoothness']);root_radius=max(min_radius,bo/2+fw+2);tip_radius=max(min_radius,float(spec.get('TipRadius',18)));waist=max(min_radius,(max(root_radius,tip_radius)*(1-.48*float(style['Slimness']))))
 def path(t):return plus(plus(start,times(vec(start,finish),t)),times(up,sag*math.sin(math.pi*t)))
 def axes(t):return frame(path(t),plus(vec(start,finish),times(up,sag*math.pi*math.cos(math.pi*t))))
 def radius(t):
  blend=(abs(t-.5)*2)**(1+2*float(style['Smoothness']));end_radius=root_radius*(1-t)+tip_radius*t;return max(min_radius,waist+(end_radius-waist)*blend)
 # Reserve a cable route inside the independently sized closed frame. The
 # nominal route is straight/curved with explicit connector bend reserve.
 arc_t=max(.1,1-bend/path_length);cable_sections=[(axes(t),cable/2+gap,cable/2+gap) for t in [0,arc_t*.2,arc_t*.5,arc_t*.8,arc_t]];cable_tool=checked(loft(cable_sections,'oval',1));protect('cable_route','cable',cable_tool,'Кабельный канал');bend_start=path(arc_t);bend_direction=axes(arc_t)[3];bend_axis=axes(arc_t)[2];bend_mid=plus(plus(bend_start,times(bend_direction,bend/math.sqrt(2))),times(bend_axis,bend*(1-1/math.sqrt(2))));bend_end=plus(plus(bend_start,times(bend_direction,bend)),times(bend_axis,bend));arc=GC_MakeArcOfCircle(gp_Pnt(*bend_start),gp_Pnt(*bend_mid),gp_Pnt(*bend_end)).Value();wire=BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(arc).Edge()).Wire();profile=BRepBuilderAPI_MakeFace(curve_profile(frame(bend_start,bend_direction),cable/2+gap,cable/2+gap,'oval')).Face();bend_tool=checked(BRepOffsetAPI_MakePipe(wire,profile).Shape());protect('cable_bend','cable',bend_tool,'Выход проводки: четверть окружности с радиусом '+str(bend)+' мм')
 module('cable',[start,finish,sag,cable,gap,bend],lambda:(cable_tool,bend_tool))
 progress('C · Несущий каркас и монтажные интерфейсы')
 boss_radius=insert/2+wall;mount_radius=case_inner+boss_radius
 bolt_sites=[(mount_radius*math.cos(math.pi/4+i*math.pi/2),mount_radius*math.sin(math.pi/4+i*math.pi/2)) for i in range(4)]
 def holes(shape,fr,rad,count,diameter,depth,z0=0):
  for i in range(count):
   angle=math.tau*i/count+math.pi/4;centre=point(fr,rad*math.cos(angle),rad*math.sin(angle),z0);shape=cut(shape,cylinder(diameter/2,depth,k.xyz(centre),fr[3]))
  return shape
 def ears(shape,z,height):
  # Radial ties overlap both the plate and the external load-bearing columns.
  for x,y in bolt_sites:
   a=math.atan2(y,x);fr=frame([0,0,z],[0,0,1]);u=[math.cos(a),math.sin(a),0];v=[-u[1],u[0],0];fr=([0,0,z],u,v,[0,0,1]);shape=fuse(shape,transform(box([case_inner-wall,-boss_radius,0,mount_radius+boss_radius,boss_radius,height]),fr))
  return shape
 def actuator():
  z=motor_z-wall;depth=-gap-z;outer=cylinder(case_outer,depth,[0,0,z]);body=cut(outer,cylinder(case_inner,depth+2,[0,0,z-1]))
  for x,y in bolt_sites:
   rail=cylinder(boss_radius,depth,[x,y,z]);rail=cut(rail,cylinder((bolt+2*gap)/2,depth+2,[x,y,z-1]));rail=cut(rail,cylinder(insert/2,insert_length,[x,y,z]));rail=cut(rail,cylinder(insert/2,insert_length,[x,y,-gap-insert_length]));body=fuse(body,rail)
  # The motor plate rests on a separate shoulder; screws do not float outside it.
  ledge=cut(ears(cylinder(case_outer,wall,[0,0,-rl-plate-wall-gap]),-rl-plate-wall-gap,wall),motor_clear)
  return fuse(body,ledge)
 actor=module('actuator',[motor,reducer,wall,gap,plate,insert,insert_length,case_outer,case_inner,motor_z,bolt],actuator)
 reach=max(500,case_outer*5)
 for side,lo,hi in [('left',-reach,-gap/2),('right',gap/2,reach)]:
  half=checked(BRepAlgoAPI_Common(actor,box([-reach,lo,-reach,reach,hi,reach])).Shape());add('actuator_'+side,'Разъёмный кожух привода с силовыми стойками · '+side,half,True,'skin')
 rear_z=motor_z-wall-gap
 def end_plate(z,height,bore):
  shape=ears(cylinder(case_outer,height,[0,0,z]),z,height);shape=cut(shape,cylinder(bore,height+2,[0,0,z-1]));return holes(shape,frame([0,0,z-1],[0,0,1]),mount_radius,4,bolt+2*gap,height+2)
 rear=end_plate(rear_z-wall,wall,cable/2+gap);add('rear_cover','Съёмная задняя крышка · четыре винта / извлечение мотора',rear,True,'cover')
 motor_plate=cut(ears(cylinder(case_inner-gap,plate,[0,0,-rl-plate]),-rl-plate,plate),cylinder(pilot/2+gap,plate+2,[mx,my,-rl-plate-1]))
 for x in [-1,1]:
  for y in [-1,1]:motor_plate=cut(motor_plate,cylinder(float(motor.get('Bolt',3))/2+gap,plate+2,[mx+x*pitch/2,my+y*pitch/2,-rl-plate-1]))
 motor_plate=cut(motor_plate,motor_clear);motor_plate=cut(motor_plate,next(v['Shape'] for v in protections if v['Id']=='reducer_clear'))
 # Clearance around the columns preserves separate, removable motor mounting plate.
 for x,y in bolt_sites:motor_plate=cut(motor_plate,cylinder(insert/2,plate+2,[x,y,-rl-plate-1]))
 # Plates terminate at the sleeve bore, with external screw ears separated from rails.
 for pid in ['actuator_left','actuator_right']:
  target=next(p for p in parts if p['Id']==pid);target['Shape']=cut(target['Shape'],motor_plate)
 add('motor_plate','Силовая плита мотора · бурт и четыре винта',motor_plate,True,'mount')
 support=end_plate(gap,plate,bi/2+float(m.get('ShaftFit',.1)));add('front_support','Силовая опора выходного узла · четыре винта',support,True,'mount')
 protect('motor_screw_access','service',cylinder(pilot/2,ml,[mx,my,motor_z-ml]),'Доступ инструмента после снятия мотора')
 # A conservative rotational envelope for the output hub. Link-to-link motion is
 # checked separately on the assembled robot, never certified from this cylinder.
 protect('output_hub_motion','motion',cylinder(root_radius+gap,start[2]+fw-plate-gap,[0,0,plate+gap]),'Круговой габарит ступицы выходного звена')
 frame_inputs=[start,finish,min_radius,fw,root_radius,tip_radius,waist,sag,plate,bolt,gap,printer,output_top,bi,float(m.get("ShaftFit",.1))]
 maximum=max(60,min(printer)-2*margin-2*(max(root_radius,tip_radius)+bolt*2));segments=max(1,int(math.ceil(path_length/maximum)))
 if segments>12:raise ValueError('Слишком длинное звено для выбранной рабочей области принтера.')
 for index in range(segments):
  t0=index/segments+(gap/(2*path_length) if index else 0);t1=(index+1)/segments-(gap/(2*path_length) if index+1<segments else 0);ts=[t0+(t1-t0)*j/4 for j in range(5)]
  def power_segment(ts=ts,t0=t0,t1=t1):
   outside=checked(loft([(axes(t),radius(t),radius(t)) for t in ts],'oval',1));inside=checked(loft([(frame(plus(path(t),times(axes(t)[3],-.3 if t==t0 else .3 if t==t1 else 0)),axes(t)[3]),radius(t)-fw,radius(t)-fw) for t in ts],'oval',1));body=cut(outside,inside)
   for t,sign in ([(t0,1)] if index else [])+[(t1,-1)]:
    fr=axes(t);rad=radius(t);origin=plus(fr[0],times(fr[3],0 if sign==1 else -plate));bore=bi/2+float(m.get('ShaftFit',.1)) if index==0 and t==t0 else rad-fw;flange=cut(cylinder(rad+bolt*2,plate,origin,fr[3]),cylinder(bore,plate+2,plus(origin,times(fr[3],-1)),fr[3]));flange=holes(flange,frame(plus(origin,times(fr[3],-1)),fr[3]),rad+bolt,4,bolt+2*gap,plate+2);body=fuse(body,flange)
   if index==0:
    z=plate+2*gap;hub=cut(cylinder(root_radius,start[2]+fw-z,[0,0,z]),cylinder(bi/2+float(m.get('ShaftFit',.1)),start[2]+fw-z+2,[0,0,z-1]));body=fuse(body,hub)
    body=cut(body,cylinder((bolt+gap)/2,root_radius+2,[0,0,z+plate],[1,0,0]))
   return body
  power=module('frame_'+str(index),frame_inputs+[index,segments],power_segment);add('frame_'+str(index),'Несущий замкнутый каркас · сегмент '+str(index+1),power,False,'frame')
 progress('D/E · Параметрический силуэт и независимый Style Engine')
 profile=style.get('Profile','oval');roundness=float(style['Roundness']);cover_gap=max(gap,.4);shell_extra=cover_gap+skin+bolt*2.5
 for index in range(segments):
  coverage=float(style['PanelCoverage']);exposure=float(style['MechanicalExposure']);trim=(1-coverage)*.12+exposure*.08;t0=(index+trim)/segments+gap/(2*path_length);t1=min((index+1-trim)/segments-gap/(2*path_length),(index+1)/segments-(plate+gap)/path_length);t0=max(t0,index/segments+(plate+gap)/path_length) if index else t0;ts=[t0+(t1-t0)*j/5 for j in range(6)]
  def shell_segment(ts=ts,inner_only=False):
   sec=[];inside=[]
   for t in ts:
    a=radius(t)+shell_extra;waist_factor=1-float(style['Taper'])*.12*math.sin(math.pi*t);a=max(radius(t)+cover_gap+skin+bolt*1.8,a*waist_factor);b=a*(.72+.28*roundness);b=max(b,radius(t)+cover_gap+skin)
    fr=axes(t)
    if float(style['Symmetry'])<1:fr=frame(plus(fr[0],times(fr[1],(1-float(style['Symmetry']))*a*.12)),fr[3])
    sec.append((fr,a,b));inside.append((fr,a-skin-(gap if inner_only else 0),b-skin-(gap if inner_only else 0)))
   out=checked(loft(sec,profile,roundness));inner=checked(loft([(frame(plus(fr[0],times(fr[3],-.4 if n==0 else .4 if n==len(inside)-1 else 0)),fr[3]),a,b) for n,(fr,a,b) in enumerate(inside)],profile,roundness));return inner if inner_only else cut(out,inner)
  shell=module('skin_'+str(index),frame_inputs+[index,style,skin,cover_gap],shell_segment);base_frame=axes((t0+t1)/2)
  mount_ts=[t0+(t1-t0)*f for f in [.12,.32,.5,.68,.88]]
  def mount_adapters():
   target=next(p for p in parts if p['Id']=='frame_'+str(index));body=target['Shape'];envelope=shell_segment(inner_only=True)
   for t in mount_ts:
    fr=axes(t);height=(radius(t)+shell_extra)*3
    for u in [-1,1]:
     centre=k.xyz(point(fr,u*(radius(t)-fw/2),0));origin=k.xyz(point(fr,u*(radius(t)-fw/2),0,-height/2));boss=BRepAlgoAPI_Common(cylinder(insert/2+wall,height,origin,fr[2]),envelope).Shape();body=fuse(body,boss);body=cut(body,cylinder((bolt+2*gap)/2,height+2,plus(origin,times(fr[2],-1)),fr[2]))
     # Independent insert pockets entered from each side of the beam. A long
     # screw passes through the cover and the stand-off to these measured seats.
     for sign in [-1,1]:
      seat=plus(centre,times(fr[2],sign*(insert_length+fw/2)));body=cut(body,cylinder(insert/2,insert_length,seat,times(fr[2],-sign)))
   return body
  adapter=module('panel_mount_'+str(index),frame_inputs+[index,style,skin,insert,insert_length],mount_adapters);next(p for p in parts if p['Id']=='frame_'+str(index))['Shape']=adapter
  clip_span=max(2000,path_length*3)
  for side,lo,hi in [('main',-clip_span,-gap/2),('lid',gap/2,clip_span)]:
   slab=transform(box([-clip_span,lo,-clip_span,clip_span,hi,clip_span]),base_frame);piece=checked(BRepAlgoAPI_Common(shell,slab).Shape())
   # Two longitudinal screw lines, outside the reserved cable core.
   for t in mount_ts:
    fr=axes(t)
    for u in [-1,1]:
     origin=k.xyz(point(fr,u*(radius(t)-fw/2),0,-clip_span/2));piece=cut(piece,cylinder((bolt+2*gap)/2,clip_span,origin,fr[2]))
     if not style['HiddenFasteners']:
      caporigin=k.xyz(point(fr,u*(radius(t)-fw/2),0,(radius(t)+shell_extra)*(.6 if side=='lid' else -.9)));piece=cut(piece,cylinder(bolt,bolt+skin,caporigin,fr[2]))
   # Split the accent out as its own editable solid; fraction changes its extent.
   fraction=float(style['AccentColorCoverage']) if side=='lid' else 0
   if .02<fraction<.98:
    half=(t1-t0)*fraction/2;mid=(t0+t1)/2;fr=axes(mid);span=path_length*(t1-t0);slabs=[('front',-clip_span,-span*fraction/2-gap/2),('accent',-span*fraction/2+gap/2,span*fraction/2-gap/2),('back',span*fraction/2+gap/2,clip_span)]
    for suffix,z0,z1 in slabs:
     shape=BRepAlgoAPI_Common(piece,transform(box([-clip_span,-clip_span,z0,clip_span,clip_span,z1]),fr)).Shape()
     if not shape.IsNull() and k.props(shape)[0]>.1:add('skin_'+str(index)+'_'+side+'_'+suffix,'Съёмная панель '+str(index+1)+' · '+side+' · '+suffix,shape,False,'accent' if suffix=='accent' else 'skin')
   else:add('skin_'+str(index)+'_'+side,'Съёмная панель '+str(index+1)+' · '+side,piece,False,'accent' if fraction>=.98 else 'skin')
 for part in parts:
  if part['Id'] in (spec.get('Overrides') or {}):
   override=spec['Overrides'][part['Id']];part['Shape']=checked(k.import_step(override if isinstance(override,str) else override['Step']))
   if isinstance(override,dict) and override.get('Shift'):
    tr=gp_Trsf();tr.SetTranslation(gp_Vec(*override['Shift']));part['Shape']=BRepBuilderAPI_Transform(part['Shape'],tr,True).Shape()
   issue('manual_override','pending','Ручная CAD-геометрия сохранена. Прочность изменённой детали требует повторной инженерной оценки.',part['Id'],scope='structural')
 progress('F · Защита механики, локальные исправления и проверки')
 # Fixed-side safety envelope trims only decorative output covers.
 repaired=[]
 fixed_envelope=cylinder(mount_radius+boss_radius+gap,plate+2*gap-(motor_z-wall-2),[0,0,motor_z-wall-2])
 for part in parts:
  if not part['InputSide'] and part['Role'] in {'skin','accent'}:
   hub_envelope=cylinder(root_radius+gap,start[2]+fw+gap-(plate+gap),[0,0,plate+gap]);part['Shape']=cut(part['Shape'],hub_envelope);repaired.append(part['Id'])
   common=BRepAlgoAPI_Common(part['Shape'],fixed_envelope).Shape()
   if not common.IsNull() and abs(k.props(common)[0])>.001:part['Shape']=cut(part['Shape'],fixed_envelope);issue('local_skin_repair','warning','Оболочка локально укорочена для зазора с неподвижной опорой.',part['Id'])
 forbidden=[p for p in protections if p['Kind'] in {'occupied','clearance','cable'}]
 # Exact intersection tests/cuts operate only on affected parts. Structural
 # repairs which remove too much material are rejected rather than called safe.
 for part in parts:
  for volume in forbidden:
   if disjoint(k.bounds(part['Shape']),volume['Bounds']):continue
   common=BRepAlgoAPI_Common(part['Shape'],volume['Shape']).Shape();amount=abs(k.props(common)[0]) if not common.IsNull() else 0
   if amount<=.001:continue
   before=k.props(part['Shape'])[0]
   if (part['Role'] in {'frame','mount'} or part['Id'].startswith('actuator_')) and amount/before>.08:
    issue('frame_interference','error','Механика или кабель пересекает силовую деталь; менять компоновку, автоматическое ослабление запрещено.',part['Id'],k.props(common)[1]);continue
   try:part['Shape']=cut(part['Shape'],volume['Shape']);repaired.append(part['Id'])
   except Exception:issue('repair_failed','error','Локальный вырез нарушает целостность детали. Измените компоновку.',part['Id'])
  bounds=k.bounds(part['Shape']);dims=[bounds[i+3]-bounds[i] for i in range(3)];fits=any(all(d<=p-2*margin+1e-6 for d,p in zip(order,printer)) for order in __import__('itertools').permutations(dims))
  issue('printer','pass' if fits else 'error',('Вписывается в область печати' if fits else 'Не помещается на принтере с заданным полем; требуется дополнительное разделение')+' · '+ ' × '.join(str(round(x,1)) for x in dims)+' мм',part['Id'],k.props(part['Shape'])[1],'manufacturing')
  if len(k.shapes(part['Shape'],k.TopAbs_SOLID))!=1 or not BRepCheck_Analyzer(part['Shape']).IsValid():issue('solid','error','Повреждённое или несвязное CAD-тело.',part['Id'])
 # Recheck after repair, never infer success only from a Boolean returning.
 intersections=0
 for part in parts:
  for volume in forbidden:
   if disjoint(k.bounds(part['Shape']),volume['Bounds']):continue
   common=BRepAlgoAPI_Common(part['Shape'],volume['Shape']).Shape()
   if not common.IsNull() and abs(k.props(common)[0])>.001:intersections+=1
 overlaps=0
 for i,a in enumerate(parts):
  for b in parts[i+1:]:
   if disjoint(k.bounds(a['Shape']),k.bounds(b['Shape'])):continue
   common=BRepAlgoAPI_Common(a['Shape'],b['Shape']).Shape();amount=abs(k.props(common)[0]) if not common.IsNull() else 0
   if amount>.05:
    overlaps+=1;issue('part_overlap','error','Детали пересекаются: '+a['Label']+' / '+b['Label']+' ('+str(round(amount,2))+' мм³).',a['Id'],k.props(common)[1],'assembly')
 service_hits=0
 service=next(v for v in protections if v['Id']=='motor_service')
 for part in parts:
  if not part['InputSide'] or part['Id']=='rear_cover' or disjoint(k.bounds(part['Shape']),service['Bounds']):continue
  overlap=BRepAlgoAPI_Common(part['Shape'],service['Shape']).Shape()
  if not overlap.IsNull() and abs(k.props(overlap)[0])>.01:service_hits+=1;issue('motor_removal','error','Деталь перекрывает прямое извлечение мотора после снятия задней крышки.',part['Id'],k.props(overlap)[1],'service')
 issue('motor_removal_path','pass' if service_hits==0 else 'error','Прямой путь извлечения размерного габарита мотора после снятия задней крышки: '+str(service_hits)+' препятствий. Разъём и фактическая проводка проверяются отдельно.',scope='service')
 issue('part_interfaces','pass' if overlaps==0 else 'error','Объёмные пересечения отдельных деталей корпуса: '+str(overlaps),scope='assembly')
 issue('panel_mounts','warning','Панели: отверстия, несущие стойки и гнёзда вставок созданы. Подберите длины винтов, глубину установки вставок и ориентацию печати по физическому крепежу.',scope='assembly')
 issue('protected_spaces','pass' if intersections==0 else 'error','Пересечения с монтажными/кабельными объёмами: '+str(intersections))
 issue('section','warning','Номинальное сечение по балочной модели: R ≥ '+str(round(min_radius,2))+' мм, стенка '+str(round(fw,2))+' мм, σэкв ≈ '+str(round(stress(min_radius),2))+' МПа при допускаемом '+str(allow)+' МПа. Узлы, крепёж, анизотропия и усталость требуют отдельной проверки.',scope='structural')
 issue('wall','warning','Стенки заданы параметрами: каркас '+str(round(fw,2))+' мм, оболочка '+str(skin)+' мм. Глобальный поиск минимальной толщины после всех вырезов не сертифицирован.')
 issue('motion','pending','Проверку движений нужно выполнить на собранном роботе; один локальный корпус не подтверждает полный диапазон.',scope='kinematics')
 issue('downstream_mount','pending','Концевой фланец является заготовкой интерфейса. Совместимость с креплениями следующего шарнира, реакцией редуктора и сборочная последовательность не подтверждены.',scope='assembly')
 issue('assembly','pending','Проверьте вставки, длины винтов и доступ инструмента по реальным комплектующим. Задняя крышка и панели снимаются отдельно.',scope='assembly')
 issue('component_fidelity','warning','STEP-компоненты проверены по импортированным телам с консервативными габаритными зазорами; остальные компоненты защищены размерными шаблонами. Проверьте полноту состава и положение реальных разъёмов.' if physical_exact else 'Защищены параметрические монтажные габариты. Для точной проверки реальных компонентов подключите STEP и положение каждой детали.')
 # Persist exact native parts and previews; assemblies remain separate solids.
 bodies=[];meta=[];solid_shapes={}
 for part in parts:
  pid=part['Id'];shape=part['Shape'];mesh=str(folder/(pid+'.cadmesh'));faces,edges,triangles=k.mesh_and_topology(shape,mesh);volume,centre=k.props(shape);path=folder/(pid+'.step');writer=STEPControl_Writer();writer.Transfer(shape,STEPControl_AsIs)
  if writer.Write(str(path))!=IFSelect_RetDone:raise ValueError('Не удалось сохранить CAD-деталь '+pid)
  # Roundtrip validation catches serialization defects, not only in-memory B-rep.
  imported=k.import_step(base64.b64encode(path.read_bytes()).decode('ascii'))
  if not math.isclose(k.props(imported)[0],volume,rel_tol=1e-5,abs_tol=.001):raise ValueError('STEP изменил объём детали '+pid)
  info=dict(Id=pid,Volume=volume,Center=centre,Bounds=k.bounds(shape),Mesh=mesh,Faces=faces,Edges=edges,Triangles=triangles);bodies.append(info);solid_shapes[pid]=shape;meta.append(dict(Id=pid,Label=part['Label'],InputSide=part['InputSide'],Role=part['Role'],Color=part['Color'],Density=part['Density'],StepPath=str(path)))
 valid=not any(x['Status']=='error' for x in report);output=dict(Ok=True,Bodies=bodies,Parameters={},Procedural=dict(Parts=meta,Issues=report,Modules=modules,ChangedModules=changed,RepairedParts=sorted(set(repaired)),MassKg=sum(b['Volume']*density*1e-9 for b in bodies),MinFrameRadius=min_radius,NominalStress=stress(min_radius),Segments=segments,GeometryValid=valid,ReadyForManufacture=False,Seconds=round(time.monotonic()-started,3),Style=style,ProtectedVolumes=[{key:v for key,v in p.items() if key!='Shape'} for p in protections]))
 (folder/'generation-report.json').write_text(json.dumps(output['Procedural'],ensure_ascii=False,indent=2),encoding='utf-8');progress('Готово · '+str(len(parts))+' отдельных CAD-деталей');return output
