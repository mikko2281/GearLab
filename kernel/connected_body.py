"""Bolted stator/rotor/link interfaces generated from kinematic frames.

All body parts are exact solids. Interface checks include native surface contact
and matching cylindrical bores; strength and the chosen reducer remain separate.
"""
import math,time,json,base64
from pathlib import Path
from OCP.gp import gp_Pnt,gp_Trsf,gp_Ax1,gp_Dir,gp_Vec
from OCP.Geom import Geom_BezierCurve,Geom_TrimmedCurve
from OCP.BRepPrimAPI import BRepPrimAPI_MakeSphere,BRepPrimAPI_MakeCone
from OCP.gp import gp_Ax2
from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
from OCP.BRepAlgoAPI import BRepAlgoAPI_Fuse,BRepAlgoAPI_Cut,BRepAlgoAPI_Common
from OCP.BRepCheck import BRepCheck_Analyzer
from OCP.BRepExtrema import BRepExtrema_DistShapeShape
from OCP.BRepOffsetAPI import BRepOffsetAPI_MakePipeShell
from OCP.BRepAdaptor import BRepAdaptor_Surface
from OCP.GeomAbs import GeomAbs_Cylinder
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
from joint_geometry import route,saddle
from procedural_body import frame,point,plus,times,norm,cross,dot,vec,cylinder,box,loft,transform,disjoint,curve_profile

REVISION='blender-elbow-2'
def dimensions(component):
 m=component.get('Mechanical') or {};motor=component.get('Motor') or {};reducer=component.get('Reducer') or {};bearing=component.get('Bearing') or {}
 wall=max(2.4,float(m.get('MinWall',2.4)));plate=max(wall,float(m.get('Plate',6)));gap=float(m.get('Clearance',.35));insert=float(m.get('InsertDiameter',4.6));boss=insert/2+wall
 inner=max(float(reducer.get('Diameter',76))/2+gap,math.hypot(float(motor.get('Frame',41.9))/2+abs(float(motor.get('X',0))),float(motor.get('Frame',41.9))/2+abs(float(motor.get('Y',0))))+gap)
 pc=inner+boss;outer=pc+boss;back=-float(reducer.get('Length',32))-plate-float(motor.get('Length',37.5))-wall;rear=back-plate
 if motor.get('ShaftLength') is not None and -float(reducer.get('Length',32))-plate+float(motor['ShaftLength'])+gap>-float(bearing.get('Width',7)):
  raise ValueError('Вал мотора пересекает выходную шейку. Увеличьте осевую длину передачи; исходную длину вала мотора изменять не требуется.')
 return dict(wall=wall,plate=plate,gap=gap,pc=pc,outer=outer,inner=inner,back=back,rear=rear,bw=float(bearing.get('Width',7)),bi=float(bearing.get('Inner',8)),bo=float(bearing.get('Outer',22)),service=math.sqrt(2)*float(motor.get('Frame',41.9))/2+gap+1)

def generate(spec,folder,k):
 started=time.monotonic();folder=Path(folder);folder.mkdir(parents=True,exist_ok=True);d=dimensions(spec);m=spec.get('Mechanical') or {};style=spec.get('Style') or {};wall=d['wall'];plate=d['plate'];gap=d['gap'];bolt=float(m.get('Bolt',3));skin=float(m.get('SkinWall',2));fw=max(wall,float(m.get('FrameWall',3.2)))*float(style.get('Strength',1));density=float(m.get('Density',1270));insert_d=float(m.get('InsertDiameter',4.6));insert_l=float(m.get('InsertLength',5.2));frame_plate=max(10,2*wall+insert_d);motor=spec.get('Motor') or {};reducer=spec.get('Reducer') or {};end=spec.get('End',[180,0,0]);parts=[];issues=[];mates=[];protections=[]
 def progress(text):
  (folder/'progress.txt').write_text(text,encoding='utf-8');print('GEARLAB_PROGRESS:'+text,flush=True)
 def cut(a,b):return BRepAlgoAPI_Cut(a,b).Shape()
 def fuse(a,b):return BRepAlgoAPI_Fuse(a,b).Shape()
 def add(pid,label,shape,side,role='frame',color='#30343b',rho=None):
  shape=k.checked(shape)
  if len(k.shapes(shape,k.TopAbs_SOLID))!=1:raise ValueError(label+': несвязный результат соединения ('+str(len(k.shapes(shape,k.TopAbs_SOLID)))+' тел).')
  parts.append(dict(Id=pid,Label=label,Shape=shape,InputSide=side,Role=role,Color=color,Density=rho or density));return shape
 def issue(code,status,message,part=None,pos=None):issues.append(dict(Code=code,Status=status,Message=message,PartId=part,Position=pos or [0,0,0],Scope='assembly'))
 def pattern(fr,pc,count=4):return [k.xyz(point(fr,pc*math.cos(math.pi/4+i*math.tau/count),pc*math.sin(math.pi/4+i*math.tau/count))) for i in range(count)]
 def drill(shape,fr,pc,diameter,depth):
  for p in pattern(fr,pc):shape=cut(shape,cylinder(diameter/2,depth,p,fr[3]))
  return shape
 def port(pid,part,fr,pc,diameter=3.4,peer=None):
  mates.append(dict(Id=pid,PartId=part,PeerJoint=peer,Origin=fr[0],X=fr[1],Y=fr[2],Normal=fr[3],BoltCircle=pc*2,BoltDiameter=diameter,BoltPoints=pattern(fr,pc)))
 def standard_length(stack,engagement):
  options=[6,8,10,12,14,16,18,20,25,30,35,40]
  valid=[v for v in options if stack+3<=v<=stack+engagement-.4]
  if not valid:raise ValueError('Нет стандартной длины M3 для данного пакета фланцев.')
  return valid[0]
 def screw(pid,fr,z0,length,pc,side):
  for i,p in enumerate(pattern(frame(plus(fr[0],times(fr[3],z0)),fr[3]),pc)):
   shaft=cylinder(bolt/2,length,p,fr[3]);head=cylinder(bolt*.9,bolt*.8,plus(p,times(fr[3],-bolt*.8)),fr[3]);add(pid+'_'+str(i),'Винт M'+str(int(bolt))+' × '+str(length)+' · '+pid,fuse(shaft,head),side,'hardware','#9aa1a9',7850)
 def bore_present(shape,origin,axis,radius,max_axial=None):
  for face in k.shapes(shape,k.TopAbs_FACE):
   surface=BRepAdaptor_Surface(k.cast(face,k.TopAbs_FACE))
   if surface.GetType()!=GeomAbs_Cylinder:continue
   cy=surface.Cylinder();ax=cy.Axis();direction=k.xyz(ax.Direction());location=k.xyz(ax.Location());delta=vec(origin,location)
   if max_axial is not None and abs(dot(vec(origin,k.props(face,True)[1]),axis))>max_axial:continue
   if abs(cy.Radius()-radius)<1e-4 and abs(abs(dot(direction,axis))-1)<1e-6 and math.sqrt(dot(cross(delta,axis),cross(delta,axis)))<1e-4:return True
  return False
 progress('Соединения · корпус привода, посадка подшипника и фланцы')
 pc=d['pc'];outer=d['outer'];back=d['back'];rear=d['rear'];bw=d['bw'];bi=d['bi'];bo=d['bo'];bearing_fit=.075
 case=cut(cylinder(outer,-bw-back,[0,0,back]),cylinder(d['inner'],-bw-back+2,[0,0,back-1]))
 motor_z=-float(reducer.get('Length',32))-plate;mx=float(motor.get('X',0));my=float(motor.get('Y',0));pitch=float(motor.get('Pitch',31.3));pilot=float(motor.get('Pilot',22))
 motorplate=cylinder(d['inner']+wall,plate,[0,0,motor_z]);motorplate=cut(motorplate,cylinder(pilot/2+gap,plate+2,[mx,my,motor_z-1]))
 for x in [-1,1]:
  for y in [-1,1]:motorplate=cut(motorplate,cylinder(float(motor.get('Bolt',3))/2+.15,plate+2,[mx+x*pitch/2,my+y*pitch/2,motor_z-1]))
 case=fuse(case,motorplate)
 if spec.get('ReferenceDesign'):
  # Real access windows open into the free volume outside the motor envelope.
  window_z=motor_z-float(motor.get('Length',37.5))*.4
  for side in [-1,1]:
   window=box([d['inner']-wall,-5,window_z-3.5,outer+1,5,window_z+3.5]) if side==1 else box([-outer-1,-5,window_z-3.5,-d['inner']+wall,5,window_z+3.5])
   case=cut(case,window)
 case=drill(case,frame([0,0,back-1],[0,0,1]),pc,bolt-.5,-bw-back+2)
 for end_z,direction in [(back,[0,0,1]),(-bw,[0,0,-1])]:
  for i,p in enumerate(pattern(frame([0,0,end_z],direction),pc)):
   case=cut(case,cylinder(insert_d/2,insert_l,p,direction));sleeve=cut(cylinder(insert_d/2,insert_l,p,direction),cylinder((bolt-.5)/2,insert_l+2,plus(p,times(direction,-1)),direction));add('case_insert_'+str(end_z)+'_'+str(i),'Термовставка M3 · корпус',sleeve,True,'insert','#b99542',8500)
 half_span=1000
 for name,lo,hi in [('left',-half_span,-gap/2),('right',gap/2,half_span)]:add('actuator_'+name,'Несущий корпус привода · '+name,BRepAlgoAPI_Common(case,box([-half_span,lo,-half_span,half_span,hi,half_span])).Shape(),True,'mount')
 front=cylinder(outer,bw+plate,[0,0,-bw]);front=cut(front,cylinder(bo/2+bearing_fit,bw+.01,[0,0,-bw-.01]));front=cut(front,cylinder(bi/2+.05,bw+plate+2,[0,0,-bw-1]));front=drill(front,frame([0,0,-bw-1],[0,0,1]),pc,bolt+.4,bw+plate+2);add('front_support','Опора подшипника · стопорный уступ',front,True,'mount')
 cover=cut(cylinder(outer,plate,[0,0,rear]),cylinder(float(m.get('CableDiameter',4))/2+gap,plate+2,[0,0,rear-1]));cover=drill(cover,frame([0,0,rear-1],[0,0,1]),pc,bolt+.4,plate+2);add('rear_cover','Монтажная крышка · общий крепёж с предыдущим звеном',cover,True,'mount');port('stator_mount','rear_cover',frame([0,0,rear],[0,0,1]),pc,bolt+.4)
 parent_plate=float(spec.get('ParentMountThickness',frame_plate));screw('stator_rear',frame([0,0,0],[0,0,1]),rear-parent_plate,standard_length(parent_plate+plate,insert_l),pc,True)
 screw('stator_front',frame([0,0,0],[0,0,-1]),-plate,standard_length(plate+bw,insert_l),pc,True)
 # The output flange and journal are one metal solid, positively bolted to the link.
 nominal=max(18,fw+float(m.get('CableDiameter',4))+4)*(1+.12*(1-float(style.get('Slimness',.78))));moment=abs(float(m.get('DesignMomentNmm',3000)));allow=float(m.get('AllowableStressMPa',12))
 from body_section import equivalent_stress,size_section
 torque=abs(float(m.get('DesignTorqueNmm',2000)));axial=abs(float(m.get('AxialForceN',20)))
 def stress(r):return equivalent_stress(r,fw,moment,torque,axial)
 core_r,_=size_section(nominal,fw,moment,torque,axial,allow)
 rounded=max(.1,min(.95,float(style.get('Roundness',.8))));profile_factor=1 if style.get('Profile','oval')=='oval' else math.sqrt(2)*(1-rounded)+rounded;core_extent=core_r*profile_factor;outer_extent=(core_r+gap+skin)*profile_factor
 root_pc=core_r+3;root_r=root_pc+bolt*2;rotor_z=plate+gap;root_z=rotor_z+plate;pilot_r=max(bi/2+3,7)
 rotor=fuse(cylinder(bi/2-.02,bw+rotor_z+plate,[0,0,-bw]),cylinder(root_r,plate,[0,0,rotor_z]));rotor=fuse(rotor,cylinder(pilot_r,2,[0,0,root_z]));rotor=drill(rotor,frame([0,0,rotor_z+.8],[0,0,1]),root_pc,bolt-.5,plate+2);add('output_flange','Выходной вал и фланец · единая металлическая деталь',rotor,False,'metal','#626b76',7850);port('rotor_output','output_flange',frame([0,0,root_z],[0,0,1]),root_pc,bolt-.5)
 if spec.get('ReferenceDesign') and outer-.7>root_r+gap+.5:
  trim=cut(cylinder(outer-.7,skin,[0,0,plate]),cylinder(root_r+gap,skin+2,[0,0,plate-1]));trim=drill(trim,frame([0,0,plate-1],[0,0,1]),pc,bolt*1.8+.3,skin+2)
  add('joint_trim','Кольцевая накладка сустава · референс',trim,True,'trim','#ed7627');port('trim_mount','joint_trim',frame([0,0,plate],[0,0,1]),pc,bolt*1.8+.3)
 rootplate=cut(cylinder(root_r,frame_plate,[0,0,root_z]),cylinder(bi/2+.1,frame_plate+2,[0,0,root_z-1]));rootplate=cut(rootplate,cylinder(pilot_r+.1,2.15,[0,0,root_z-.01]));rootplate=drill(rootplate,frame([0,0,root_z-1],[0,0,1]),root_pc,bolt+.4,frame_plate+2)
 start=[0,0,root_z+frame_plate+max(2*fw,root_r-core_r+2*skin)];children=spec.get('Children') or []
 if len(children)>1:raise ValueError('Соединённый корпус ветвления требует отдельного разветвляющего фланца; используйте отдельные звенья.')
 child=children[0] if children else None
 if child:
  cd=dimensions(child);values=child['Transform'];fr=([values[3],values[7],values[11]],[values[0],values[4],values[8]],[values[1],values[5],values[9]],[values[2],values[6],values[10]])
  target_fr=(plus(fr[0],times(fr[3],cd['rear'])),fr[1],fr[2],fr[3]);tip_axis=fr[3];tip_center=plus(target_fr[0],times(tip_axis,-frame_plate));towards=vec(target_fr[0],start);side=plus(towards,times(tip_axis,-dot(towards,tip_axis)));side=norm(side) if dot(side,side)>1 else fr[1];tip_r=cd['outer'];tip_pc=cd['pc'];tip_bore=cd['service'];target=plus(plus(tip_center,times(tip_axis,frame_plate/2)),times(side,tip_r+outer_extent+gap))
 else:
  target=end;tip_center=end;tip_axis=norm(end);target_fr=frame(plus(target,times(tip_axis,plate)),tip_axis);tip_r=max(core_r,20);tip_pc=tip_r-5;tip_bore=6;cd=None
 reference=bool(spec.get('ReferenceDesign',False)) and (child is not None or spec.get('BlenderDesign'))
 if spec.get('ReferenceDesign',False) and not child:target_fr=frame(plus(tip_center,times(tip_axis,frame_plate)),tip_axis)
 if reference:
  if spec.get('BlenderDesign') and not child:
   from blender_enclosure import terminal as reference_build
  else:
   from reference_enclosure import build as reference_build
  reference_result=reference_build(locals());power=reference_result['power'];boot=reference_result['boot'];mounts=[]
 else:
  distance=math.sqrt(dot(vec(start,target),vec(start,target)))
  if distance<10:raise ValueError('Недостаточно места между фланцами; увеличьте расстояние между суставами.')
  radial=[target[0],target[1],0]
  if dot(radial,radial)<1:radial=[1,0,0]
  radial=norm(radial);escape=plus(start,times(radial,min(max(outer+core_r+8,60),distance*.65)));service=cd['service'] if cd else core_r-fw
  straight=min(float(child['Motor']['Length'])+12 if child else 18,distance*.4);pre=plus(target,times(tip_axis,-straight))
  # Three G1 cubic Bezier spans avoid the occupied root barrel and enter the next
  # stator at its actual rear mounting plane, never at an arbitrary tube offset.
  def bezier(a,b,c,e,t):return [a[i]*(1-t)**3+3*b[i]*(1-t)**2*t+3*c[i]*(1-t)*t*t+e[i]*t**3 for i in range(3)]
  obstacles=[dict(Origin=[0,0,0],Axis=[0,0,1],Min=back,Max=plate+gap,Radius=outer+gap),dict(Origin=[0,0,0],Axis=[0,0,1],Min=plate+gap,Max=root_z-.001,Radius=root_r+gap)]
  if child:obstacles.append(dict(Origin=fr[0],Axis=tip_axis,Min=cd['rear']-gap,Max=cd['plate']+gap,Radius=cd['outer']+gap))
  spans,minimum_bend=route(start,target,tip_axis,outer_extent,obstacles)
  (folder/'route.json').write_text(json.dumps(dict(Spans=spans,Radius=core_r,MinimumBend=minimum_bend,EndAxis=tip_axis),indent=2),encoding='utf-8')
  def axes(t):return frame(start,[0,0,1]) if t==0 else frame(target,vec(spans[-1]['Start'],spans[-1]['End']))
  wire_builder=k.BRepBuilderAPI_MakeWire();trimmed_builder=k.BRepBuilderAPI_MakeWire();native_edges=[]
  from OCP.GC import GC_MakeArcOfCircle
  for index,segment in enumerate(spans):
   a=segment['Start'];b=segment['End']
   if segment['Kind']=='arc':edge=k.BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(gp_Pnt(*a),gp_Pnt(*segment['Mid']),gp_Pnt(*b)).Value()).Edge();trimmed_builder.Add(edge)
   else:
    edge=k.BRepBuilderAPI_MakeEdge(gp_Pnt(*a),gp_Pnt(*b)).Edge();trim_end=plus(b,times(norm(vec(a,b)),-skin)) if index==len(spans)-1 else b;trimmed_builder.Add(k.BRepBuilderAPI_MakeEdge(gp_Pnt(*a),gp_Pnt(*trim_end)).Edge())
   wire_builder.Add(edge)
   native_edges.append(edge)
  wire=wire_builder.Wire();trimmed_wire=trimmed_builder.Wire()
  first_curve=k.BRepAdaptor_Curve(native_edges[0]);last_curve=k.BRepAdaptor_Curve(native_edges[-1]);first_tangent=gp_Vec();last_tangent=gp_Vec();sample_point=gp_Pnt();first_curve.D1(first_curve.FirstParameter(),sample_point,first_tangent);last_curve.D1(last_curve.LastParameter(),sample_point,last_tangent);first_tangent.Normalize();last_tangent.Normalize();aligned=dot(k.xyz(first_tangent),[0,0,1])>1-1e-8 and abs(dot(k.xyz(last_tangent),tip_axis))>1-1e-8
  issue('axis_tangency','pass' if aligned else 'error','Касательные CAD-траектории совпадают с осями посадок. '+('Прямой осевой переход.' if minimum_bend>1e10 else 'Радиус круглого изгиба '+str(round(minimum_bend,2))+' мм.'))
  def pipe(radius,trim=False):
   sweep=BRepOffsetAPI_MakePipeShell(trimmed_wire if trim else wire);normal=cross(vec(start,target),[0,0,1])
   if dot(normal,normal)>1e-8 and abs(dot(norm(normal),tip_axis))<1e-6:sweep.SetMode(gp_Dir(*norm(normal)))
   else:sweep.SetMode(False)
   from OCP.BRepTools import BRepTools_WireExplorer
   sweep.Add(curve_profile(axes(0),radius,radius,style.get('Profile','oval'),float(style.get('Roundness',.8))),BRepTools_WireExplorer(trimmed_wire if trim else wire).CurrentVertex(),False,False);sweep.Build()
   if not sweep.IsDone() or not sweep.MakeSolid():raise ValueError('Не удалось замкнуть осевой переход звена.')
   return k.checked(sweep.Shape())
  outside=pipe(core_r);inside=pipe(core_r-fw)
  power=cut(outside,inside);neck=cut(cylinder(core_r,start[2]+fw-root_z,[0,0,root_z]),cylinder(max(3,float(m.get('CableDiameter',4))/2+gap),start[2]+fw-root_z+2,[0,0,root_z-1]));power=fuse(fuse(power,neck),rootplate)
  tipplate=transform(cylinder(tip_r,frame_plate), (tip_center, target_fr[1],target_fr[2],tip_axis));tipplate=cut(tipplate,cylinder(tip_bore,frame_plate+2,plus(tip_center,times(tip_axis,-1)),tip_axis));tipplate=drill(tipplate,(plus(tip_center,times(tip_axis,-1)),target_fr[1],target_fr[2],tip_axis),tip_pc,bolt+.4,frame_plate+2);
  if child:
   web_frame=(tip_center,side,norm(cross(tip_axis,side)),tip_axis);reach=math.sqrt(dot(vec(plus(tip_center,times(tip_axis,frame_plate/2)),target),vec(plus(tip_center,times(tip_axis,frame_plate/2)),target)));tipplate=saddle(web_frame,reach,tip_r,core_extent,frame_plate);tipplate=cut(tipplate,cylinder(tip_bore,frame_plate+2,plus(tip_center,times(tip_axis,-1)),tip_axis));tipplate=drill(tipplate,(plus(tip_center,times(tip_axis,-1)),target_fr[1],target_fr[2],tip_axis),tip_pc,bolt+.4,frame_plate+2)
  power=fuse(power,tipplate)
  power=cut(power,cylinder(pilot_r+.1,2.15,[0,0,root_z-.01]))
  for p in pattern(frame([0,0,root_z-1],[0,0,1]),root_pc):power=cut(power,cylinder((bolt+.4)/2,frame_plate+2,p))
  # Explicit fixed clearances; failure is reported rather than hidden by translating parts.
  own_fixed=cylinder(outer,plate-back,[0,0,back]);clearance=BRepAlgoAPI_Common(power,own_fixed).Shape()
  if not clearance.IsNull() and abs(k.props(clearance)[0])>.05:issue('fixed_interference','error','Несущий элемент пересекает неподвижный корпус привода; измените компоновку.', 'frame_0',k.props(clearance)[1])
  if child:
   child_tool=transform(cylinder(cd['outer']+.1,cd['plate']-cd['rear'],[0,0,cd['rear']+.001]),fr);common=BRepAlgoAPI_Common(power,child_tool).Shape()
   if k.shapes(common,k.TopAbs_SOLID) and abs(k.props(common)[0])>.05:issue('child_interference','error','Несущий переходник пересекает следующий привод.','frame_0',k.props(common)[1])
  power=k.checked(power);add('frame_0','Соединённый несущий элемент · оба ответных фланца',power,False,'frame');port('link_input','frame_0',frame([0,0,root_z],[0,0,1]),root_pc,bolt+.4);port('link_child' if child else 'tool_mount','frame_0',target_fr,tip_pc,bolt+.4,child.get('Id') if child else None)
  screw('root_bolt',frame([0,0,root_z+frame_plate],[0,0,-1]),0,standard_length(frame_plate,plate),root_pc,False)
  progress('Соединения · замкнутый кожух углового перехода')
  # A fused boot closes the former open pipe/hub junction. Its void is generated
  # around the actual load-bearing element, not by chopping off the whole end.
  boot_outer=pipe(core_r+gap+skin);boot_inner=pipe(core_r+gap,True)
  taper_z=root_z+frame_plate;taper_height=start[2]-taper_z;taper_skin=skin*math.sqrt(1+((root_r-core_r)/taper_height)**2)
  def root_envelope(extra,bottom,tip_radius):
   base=cylinder(root_r+extra,taper_z-bottom,[0,0,bottom]);cone=BRepPrimAPI_MakeCone(gp_Ax2(gp_Pnt(0,0,taper_z),gp_Dir(0,0,1)),root_r+extra,tip_radius+extra,taper_height).Shape();return fuse(base,cone)
  boot_outer=fuse(boot_outer,root_envelope(gap+taper_skin,root_z,core_extent));boot_inner=fuse(boot_inner,root_envelope(gap,root_z-1,core_r))
  if child:
   reach=math.sqrt(dot(vec(plus(tip_center,times(tip_axis,frame_plate/2)),target),vec(plus(tip_center,times(tip_axis,frame_plate/2)),target)));saddle_depth=skin+gap
   saddle_frame=(plus(tip_center,times(tip_axis,-saddle_depth)),side,norm(cross(tip_axis,side)),tip_axis)
   outer_socket=saddle(saddle_frame,reach,tip_r+gap+skin,core_extent+gap+skin,saddle_depth+frame_plate+gap+skin)
   inner_frame=(plus(tip_center,times(tip_axis,-saddle_depth+skin)),side,norm(cross(tip_axis,side)),tip_axis)
   inner_socket=saddle(inner_frame,reach,tip_r+gap,core_extent+gap,saddle_depth+frame_plate+gap-skin)
   boot_outer=fuse(boot_outer,outer_socket);boot_inner=fuse(boot_inner,inner_socket)
  else:
   boot_outer=fuse(boot_outer,cylinder(tip_r+gap+skin,frame_plate+2*skin,plus(tip_center,times(tip_axis,-skin)),tip_axis));boot_inner=fuse(boot_inner,cylinder(tip_r+gap,frame_plate+2*skin+2 if spec.get('ReferenceDesign') else frame_plate+2,plus(tip_center,times(tip_axis,-1)),tip_axis))
  exposed=cut(power,boot_outer);uncovered=abs(k.props(exposed)[0]) if k.shapes(exposed,k.TopAbs_SOLID) else 0
  issue('cover_enclosure','pass' if uncovered<.05 else 'error','Несущий элемент вне наружного контура кожуха: '+str(round(uncovered,4))+' мм³.','frame_0')
  boot=cut(boot_outer,boot_inner)
  # Bolted service covers seat on structural pads with real insert pockets.
  mounts=[(frame([0,0,root_z+frame_plate/2],[0,0,1]),root_r)]
  if child:mounts.append(((plus(tip_center,times(tip_axis,frame_plate/2)),target_fr[1],target_fr[2],tip_axis),tip_r))
  for group,(mount_fr,mount_r) in enumerate(mounts):
   for i in range(4):
    angle=math.pi/8+i*math.pi/2;outward=plus(times(mount_fr[1],math.cos(angle)),times(mount_fr[2],math.sin(angle)));inward=times(outward,-1);start_pad=plus(mount_fr[0],times(outward,mount_r-wall));pad=cylinder(insert_d/2+wall,wall+gap+.5,start_pad,outward)
    boundary=cylinder(mount_r+gap,frame_plate+2,plus(mount_fr[0],times(mount_fr[3],-frame_plate/2-1)),mount_fr[3]);pad=BRepAlgoAPI_Common(pad,boundary).Shape();power=fuse(power,pad)
    insert_start=plus(mount_fr[0],times(outward,mount_r-.1));power=cut(power,cylinder(insert_d/2,insert_l,insert_start,inward));thread=cut(cylinder(insert_d/2,insert_l,insert_start,inward),cylinder((bolt-.5)/2,insert_l+2,plus(insert_start,outward),inward));add('skin_insert_'+str(group)+'_'+str(i),'Термовставка M3 · сервисный кожух',thread,False,'insert','#b99542',8500)
    screw_start=plus(mount_fr[0],times(outward,mount_r+gap+skin));shaft=cylinder(bolt/2,standard_length(skin+gap,insert_l),screw_start,inward);head=cylinder(bolt*.9,bolt*.8,screw_start,outward);add('skin_screw_'+str(group)+'_'+str(i),'Винт M3 × '+str(standard_length(skin+gap,insert_l))+' · сервисный кожух',fuse(shaft,head),False,'hardware','#9aa1a9',7850);boot=cut(boot,cylinder((bolt+.4)/2,skin+gap+2,plus(screw_start,outward),inward))
  next(p for p in parts if p['Id']=='frame_0')['Shape']=power
  # Remove the exact structural tube bore as well as its material. Otherwise
  # an end cap can leave isolated plugs inside the cable passage after a bend.
  boot=cut(cut(boot,power),inside)
  if child:
   # The swept skin can overshoot the mounting plane at an elbow. Seat it
   # around the next stator with the same assembly clearance as the flange.
   next_case=transform(cylinder(cd['outer']+gap,cd['plate']-cd['rear']+2*gap,[0,0,cd['rear']-gap]),fr);boot=cut(boot,next_case)
  # Choose a service split which does not leave unfastened islands at a 3D elbow.
  def anchor_count(shape):
   count=0
   for mount_fr,mount_r in mounts:
    for i in range(4):
     angle=math.pi/8+i*math.pi/2;axis=plus(times(mount_fr[1],math.cos(angle)),times(mount_fr[2],math.sin(angle)));origin=plus(mount_fr[0],times(axis,mount_r+gap+skin))
     if bore_present(shape,origin,axis,(bolt+.4)/2,skin+gap+2):count+=1
   return count
  normals=[]
  if child:
   common=cross([0,0,1],tip_axis)
   if dot(common,common)>1e-8:normals.append(norm(common))
  normals.append(norm(cross(radial,[0,0,1])))
  normals.extend([[math.cos(a*math.pi/180),math.sin(a*math.pi/180),0] for a in range(0,180,30)])
  accent=max(.1,min(.8,float(style.get('AccentColorCoverage',.32))));split_at=(1-2*accent)*core_r;selected=None
  # Boolean end trimming can leave isolated sub-millimetre sweep fragments.
  # Remove only tiny, unfastened residual solids before splitting the real shell.
  retained=[];residuals=[]
  for solid in k.shapes(boot,k.TopAbs_SOLID):
   if abs(k.props(solid)[0])<4*skin**3 and anchor_count(solid)==0:residuals.append(solid)
   else:retained.append(solid)
  if residuals:
   if not retained:raise ValueError('После обрезки не осталось связного кожуха.')
   boot=k.compound(retained);issue('sweep_trim','pass','Удалены изолированные остатки обрезки протяжки: '+str(len(residuals))+'.')
  k.checked(boot);continuous=len(k.shapes(boot,k.TopAbs_SOLID))==1
  issue('shell_continuity','pass' if continuous else 'error','Оболочка до сервисного разъёма: '+str(len(k.shapes(boot,k.TopAbs_SOLID)))+' связных тел. Разрывы кожуха недопустимы.')
  split_diagnostics=[]
  for normal in normals:
   u=norm(cross(normal,[0,0,1]));split=([0,0,0],u,normal,[0,0,1]);pieces=[];span=2000
   for side,lo,hi in [('main',-span,split_at-gap/2),('panel',split_at+gap/2,span)]:
    clipped=BRepAlgoAPI_Common(boot,transform(box([-span,lo,-span,span,hi,span]),split)).Shape()
    for solid in k.shapes(clipped,k.TopAbs_SOLID):
     if k.props(solid)[0]>.01:pieces.append((side,solid,anchor_count(solid)))
   split_diagnostics.append(dict(Normal=normal,Pieces=[dict(Side=p[0],Anchors=p[2],Volume=k.props(p[1])[0]) for p in pieces]))
   if pieces and all(item[2]>=2 for item in pieces):
    if selected is None or len(pieces)<len(selected):selected=pieces
    if len(pieces)==2:break
  if selected is None:
   (folder/'split-diagnostics.json').write_text(json.dumps(split_diagnostics,indent=2),encoding='utf-8');writer=STEPControl_Writer();writer.Transfer(boot,STEPControl_AsIs);writer.Write(str(folder/'boot-debug.step'));raise ValueError('Не удалось разделить кожух на закреплённые сервисные панели. Измените направление звена.')
  for index,(side,solid,anchors) in enumerate(selected):add('skin_'+side+'_'+str(index),'Закреплённый кожух перехода · '+side,solid,False,'accent' if side=='panel' else 'skin','#ed7627' if side=='panel' else '#30343b')
 if spec.get('GroundDepth',0)>0:
  if float(spec['GroundDepth'])+rear<frame_plate+plate:raise ValueError('Между нижней крышкой привода и столом недостаточно места для опоры основания. Увеличьте высоту основания или уменьшите осевую длину передачи.')
  ground=-float(spec['GroundDepth']);top=rear;column_r=max(outer-3,32);column=cut(cylinder(column_r,top-ground,[0,0,ground]),cylinder(column_r-wall,top-ground+2,[0,0,ground-1]));floor=cut(cylinder(outer+8,plate,[0,0,ground]),cylinder(6,plate+2,[0,0,ground-1]));basecap=cut(cylinder(outer,frame_plate,[0,0,top-frame_plate]),cylinder(d['service'],frame_plate+2,[0,0,top-frame_plate-1]));basecap=drill(basecap,frame([0,0,top-frame_plate-1],[0,0,1]),pc,bolt+.4,frame_plate+2);
  for p in pattern(frame([0,0,top-frame_plate-18],[0,0,1]),pc):column=cut(column,cylinder(6,18,p))
  floor=drill(floor,frame([0,0,ground-1],[0,0,1]),outer+2.25,5.5,plate+2)
  add('base','Основание · соединённая опора статора',fuse(fuse(column,floor),basecap),True,'frame');port('base_mount','base',frame([0,0,top],[0,0,1]),pc,bolt+.4)
 if spec.get('Root') and not spec.get('GroundDepth',0):
  mount=cut(cylinder(outer,frame_plate,[0,0,rear-frame_plate]),cylinder(d['service'],frame_plate+2,[0,0,rear-frame_plate-1]));mount=drill(mount,frame([0,0,rear-frame_plate-1],[0,0,1]),pc,bolt+.4,frame_plate+2);add('base_adapter','Монтажный адаптер основания',mount,True,'frame');port('base_mount','base_adapter',frame([0,0,rear],[0,0,1]),pc,bolt+.4)
 # Apply user edits before collision and interface validation.
 for p in parts:
  override=(spec.get('Overrides') or {}).get(p['Id'])
  if override:
   p['Shape']=k.import_step(override if isinstance(override,str) else override['Step'])
   if isinstance(override,dict) and override.get('Shift'):
    tr=gp_Trsf();tr.SetValues(1,0,0,override['Shift'][0],0,1,0,override['Shift'][1],0,0,1,override['Shift'][2]);p['Shape']=BRepBuilderAPI_Transform(p['Shape'],tr,True).Shape()
 # Imported components occupy real CAD volume, including after manual moves.
 for component in spec.get('Components') or []:
  occupied=k.import_step(component['Step'])
  if component.get('Transform'):
   tr=gp_Trsf();tr.SetValues(*component['Transform']);occupied=BRepBuilderAPI_Transform(occupied,tr,True).Shape()
  cb=k.bounds(occupied);protections.append(dict(Id=component['Id'],Kind='occupied',Bounds=cb,Label=component.get('Label',component['Id'])))
  for p in parts:
   if p['Role'] in {'hardware','insert'} or disjoint(cb,k.bounds(p['Shape'])):continue
   common=BRepAlgoAPI_Common(occupied,p['Shape']).Shape()
   amount=abs(k.props(common)[0]) if k.shapes(common,k.TopAbs_SOLID) else 0
   if amount>.05:issue('component_intersection','error','STEP-компонент '+component.get('Label',component['Id'])+' пересекает '+p['Id']+': '+str(round(amount,3))+' мм³.',p['Id'],k.props(common)[1])
 # Positive-volume intersections matter; shared seating faces are intentional.
 structural=[p for p in parts if p['Role'] not in {'hardware','insert'}]
 for i,a in enumerate(structural):
  for b in structural[i+1:]:
   if disjoint(k.bounds(a['Shape']),k.bounds(b['Shape'])):continue
   common=BRepAlgoAPI_Common(a['Shape'],b['Shape']).Shape()
   amount=abs(k.props(common)[0]) if k.shapes(common,k.TopAbs_SOLID) else 0
   if amount>.05:issue('body_intersection','error','Пересечение '+a['Id']+' / '+b['Id']+': '+str(round(amount,3))+' мм³.',a['Id'],k.props(common)[1])
 for p in mates:
  shape=next(x['Shape'] for x in parts if x['Id']==p['PartId']);ok=all(bore_present(shape,point,p['Normal'],p['BoltDiameter']/2) for point in p['BoltPoints']);p['BoresVerified']=ok
  issue('bolt_pattern','pass' if ok else 'error','Реальные отверстия фланца '+p['Id']+': '+str(len(p['BoltPoints']))+' × Ø'+str(p['BoltDiameter'])+' мм.',p['PartId'],p['Origin'])
 joint_distance=BRepExtrema_DistShapeShape(next(p['Shape'] for p in parts if p['Id']=='frame_0'),next(p['Shape'] for p in parts if p['Id']=='output_flange'));joint_distance.Perform();distance=joint_distance.Value();issue('root_contact','pass' if distance<1e-5 else 'error','Контакт несущего фланца с выходным: '+str(round(distance,6))+' мм.', 'frame_0',[0,0,root_z])
 issue('drive_selection','pending','Фланцы соединены геометрически. Внутреннюю передачу и её выходной носитель нужно согласовать с выбранным редуктором; передаточное число само по себе не задаёт механизм.')
 issue('cable_routing','pending','Внутренние проходы в каркасе сохранены. Гибкая проводка через шарниры, разъёмы и допустимый перекрут требуют отдельной трассировки; условные наружные стержни не показываются как готовые кабели.')
 issue('strength','pending','Прочность стыков, вставок, валов и печатного материала требует расчёта по реальным данным.')
 bodies=[];meta=[]
 for p in parts:
  progress('Подготовка STEP и сцены: '+p['Id'])
  shape=k.checked(p['Shape']);pid=p['Id'];mesh=str(folder/(pid+'.cadmesh'));volume,center=k.props(shape);bounds=k.bounds(shape);step=folder/(pid+'.step');writer=STEPControl_Writer();writer.Transfer(shape,STEPControl_AsIs)
  if writer.Write(str(step))!=IFSelect_RetDone:raise ValueError('STEP: '+pid)
  backshape=k.import_step(base64.b64encode(step.read_bytes()).decode());
  tolerance=1e-5
  if len(k.shapes(backshape,k.TopAbs_SOLID))!=1 or not BRepCheck_Analyzer(backshape).IsValid() or not math.isclose(k.props(backshape)[0],volume,rel_tol=tolerance,abs_tol=.001):raise ValueError('Проверка STEP: '+pid+' '+str(volume)+' / '+str(k.props(backshape)[0]))
  shape=backshape;p['Shape']=shape;volume,center=k.props(shape);bounds=k.bounds(shape);faces,edges,triangles=k.mesh_and_topology(shape,mesh)
  if p['Role'] in {'skin','accent'}:
   anchors=reference_result['reference_anchors'](shape) if reference else 0
   for mount_fr,mount_r in mounts:
    for i in range(4):
     angle=math.pi/8+i*math.pi/2;axis=plus(times(mount_fr[1],math.cos(angle)),times(mount_fr[2],math.sin(angle)));origin=plus(mount_fr[0],times(axis,mount_r+gap+skin))
     if bore_present(shape,origin,axis,(bolt+.4)/2,skin+gap+2):anchors+=1
   issue('cover_anchors','pass' if anchors>=2 else 'error','Крепёжные отверстия в замкнутой панели: '+str(anchors)+'.',pid,center)
  print_angles=None
  if p['Role'] not in {'hardware','insert','metal'}:
   best=None
   for rx in [0,90]:
    for ry in [0,90]:
     for rz in range(0,180,15):
      rotated=shape
      for angle,axis in [(rx,[1,0,0]),(ry,[0,1,0]),(rz,[0,0,1])]:
       if angle:
        rotation=gp_Trsf();rotation.SetRotation(gp_Ax1(gp_Pnt(0,0,0),gp_Dir(*axis)),angle*math.pi/180);rotated=BRepBuilderAPI_Transform(rotated,rotation,False).Shape()
      rb=k.bounds(rotated);rd=[rb[n+3]-rb[n] for n in range(3)];score=(max(rd)>244,rd[2],max(rd))
      if best is None or score<best[0]:best=(score,rd,[rx,ry,rz])
   dims=best[1];print_angles=best[2]
  else:dims=[bounds[i+3]-bounds[i] for i in range(3)]
  issue('printer','warning' if max(dims)>244 else 'pass',('Нужна технологическая разрезка: ' if max(dims)>244 else 'Габариты детали: ')+' × '.join(str(round(x,1)) for x in dims)+' мм.',pid,center)
  bodies.append(dict(Id=pid,Volume=volume,Center=center,Bounds=bounds,Faces=faces,Edges=edges,Triangles=triangles,Mesh=mesh));meta.append({key:value for key,value in p.items() if key!='Shape'});meta[-1]['StepPath']=str(step);meta[-1]['PrintAngles']=print_angles
 for mate in mates:
  shape=next(p['Shape'] for p in parts if p['Id']==mate['PartId']);mate['BoresVerified']=all(bore_present(shape,point,mate['Normal'],mate['BoltDiameter']/2) for point in mate['BoltPoints'])
  if not mate['BoresVerified']:issue('step_interface','error','Обмен STEP изменил отверстия стыка.',mate['PartId'],mate['Origin'])
 result=dict(Parts=meta,Issues=issues,Mates=mates,Modules={'connected':REVISION,'design':'blender-elbow' if spec.get('BlenderDesign') and reference else 'reference-cassette' if reference else 'reference-terminal' if spec.get('ReferenceDesign') else 'tangent-tube'},ChangedModules=['connected'],RepairedParts=[],ProtectedVolumes=protections,MassKg=sum(b['Volume']*p['Density']*1e-9 for b,p in zip(bodies,parts)),MinFrameRadius=core_r,NominalStress=stress(core_r),Segments=1,GeometryValid=not any(i['Status']=='error' for i in issues),ReadyForManufacture=False,Seconds=round(time.monotonic()-started,3));(folder/'generation-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');progress('Соединённые детали готовы');return dict(Ok=True,Bodies=bodies,Procedural=result)
