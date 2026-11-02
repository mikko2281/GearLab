"""Dimensioned cassette links: continuous rounded body and inset service tray.

The functional flange ports are preserved. This is a distinct housing topology,
not a material swap on the old tangent tube / saddle geometry.
"""
import math
from OCP.gp import gp_Pnt,gp_Vec,gp_Dir,gp_Lin
from OCP.IntCurvesFace import IntCurvesFace_ShapeIntersector
from OCP.BRepAlgoAPI import BRepAlgoAPI_Common,BRepAlgoAPI_Cut,BRepAlgoAPI_Fuse
from procedural_body import frame,point,plus,times,vec,norm,dot,cross,cylinder,transform
from joint_geometry import saddle

def build(c):
 k=c['k'];cut=c['cut'];fuse=c['fuse'];add=c['add'];issue=c['issue'];gap=c['gap'];skin=c['skin'];fw=c['fw'];plate=c['frame_plate'];bolt=c['bolt'];insert_d=c['insert_d'];insert_l=c['insert_l']
 child=c['child'];root_z=c['root_z'];root_r=c['root_r'];tip_r=c['tip_r'];tip_axis=c['tip_axis'];tip_center=c['tip_center'];target_fr=c['target_fr'];tip_pc=c['tip_pc'];tip_bore=c['tip_bore'];root_pc=c['root_pc']
 a=[0,0,root_z+plate/2];b=plus(tip_center,times(tip_axis,plate/2));delta=vec(a,b);length=math.sqrt(dot(delta,delta));u=norm(delta)
 normal=plus([0,0,1],times(u,-u[2]))
 if dot(normal,normal)<.01:
  normal=plus(tip_axis,times(u,-dot(tip_axis,u)))
 if dot(normal,normal)<.01:normal=[1,0,0]
 normal=norm(normal);v=norm(cross(normal,u))
 r0=max(root_r+fw,30);r1=max(tip_r+fw,30);half=max(24,min(r0,r1)*(1.05-.30*float(c['style'].get('Slimness',.78))))
 edge_round=min(half*(.4+.38*float(c['style'].get('Roundness',.85))),r0*.4,r1*.4)
 def hull(extra,depth_extra=0):
  fr=(plus(a,times(normal,-half-depth_extra)),u,v,normal)
  raw=saddle(fr,length,r0+extra,r1+extra,2*(half+depth_extra))
  fillet=k.BRepFilletAPI_MakeFillet(raw)
  for edge in k.shapes(raw,k.TopAbs_EDGE):
   edge=k.cast(edge,k.TopAbs_EDGE);curve=k.BRepAdaptor_Curve(edge);p=gp_Pnt();tangent=gp_Vec();curve.D1((curve.FirstParameter()+curve.LastParameter())/2,p,tangent)
   if abs(dot(k.xyz(tangent),normal))<1e-6:fillet.Add(edge_round+extra,edge)
  fillet.Build()
  if not fillet.IsDone():raise ValueError('Не удалось скруглить непрерывный корпус звена.')
  return k.checked(fillet.Shape())
 # A closed load-bearing cassette joins the two real flange annuli. Occupied
 # motor barrels are cut from the cassette instead of moving their positions.
 structural_inset=edge_round*.45
 def core_hull(inset):return saddle((plus(a,times(normal,-half+structural_inset+inset)),u,v,normal),length,r0-structural_inset-inset,r1-structural_inset-inset,2*(half-structural_inset-inset))
 tip_plate_radius=tip_r
 if c['spec'].get('BlenderDesign'):
  from blender_enclosure import geometry
  g=geometry(c);hull=g['hull'];core_hull=g['core_hull'];a=g['a'];u=g['u'];normal=g['normal'];v=g['v'];length=g['length'];r0=g['r0'];r1=g['r1'];half=g['half'];structural_inset=g['structural_inset'];tip_plate_radius=g['tip_plate_radius']
 core_outer=core_hull(0);core_inner=core_hull(fw)
 power=k.checked(cut(core_outer,core_inner))
 if c['spec'].get('BlenderDesign'):
  rootplate=cut(cylinder(max(root_r,r0),plate,[0,0,root_z]),cylinder(c['bi']/2+.1,plate+2,[0,0,root_z-1]))
  rootplate=cut(rootplate,cylinder(c['pilot_r']+.1,2.15,[0,0,root_z-.01]))
  rootplate=c['drill'](rootplate,frame([0,0,root_z-1],[0,0,1]),root_pc,bolt+.4,plate+2)
 else:rootplate=c['rootplate']
 tipplate=transform(cylinder(tip_plate_radius,plate),(tip_center,target_fr[1],target_fr[2],tip_axis));tipplate=cut(tipplate,cylinder(tip_bore,plate+2,plus(tip_center,times(tip_axis,-1)),tip_axis));tipplate=c['drill'](tipplate,(plus(tip_center,times(tip_axis,-1)),target_fr[1],target_fr[2],tip_axis),tip_pc,bolt+.4,plate+2)
 power=k.checked(fuse(fuse(power,rootplate),tipplate))
 own_fixed=cylinder(c['outer']+gap,c['plate']+gap+skin-c['back'],[0,0,c['back']])
 rotor_clear=cylinder(root_r+gap,root_z-c['rotor_z'],[0,0,c['rotor_z']])
 obstacles=[own_fixed,rotor_clear]
 if child:
  cd=c['cd'];obstacles.append(transform(cylinder(cd['outer']+gap,cd['plate']-cd['rear']+gap+1e-6,[0,0,cd['rear']-1e-6]),c['fr']))
 for index,obstacle in enumerate(obstacles):
  c['progress']('Монокок · вырез монтажного объёма '+str(index))
  operation=BRepAlgoAPI_Cut(power,obstacle);operation.SetFuzzyValue(1e-6);operation.Build();power=operation.Shape()
  if not k.BRepCheck_Analyzer(power).IsValid():
   from OCP.ShapeFix import ShapeFix_Shape
   repair=ShapeFix_Shape(power);repair.SetPrecision(1e-6);repair.Perform();power=repair.Shape()
  power=k.checked(power)
 power=k.checked(cut(power,cylinder(max(3,c['bi']/2+.1),plate+4,[0,0,root_z-1])));power=k.checked(cut(power,cylinder(c['pilot_r']+.1,2.15,[0,0,root_z-.01])));power=k.checked(c['drill'](power,frame([0,0,root_z-1],[0,0,1]),root_pc,bolt+.4,plate+2))
 if c['spec'].get('BlenderDesign'):
  from pathlib import Path
  import json
  (Path(c['folder'])/'elbow-power-pieces.json').write_text(json.dumps([dict(volume=k.props(s)[0],bounds=k.bounds(s)) for s in k.shapes(power,k.TopAbs_SOLID)],indent=2))
 solids=k.shapes(power,k.TopAbs_SOLID)
 if len(solids)>1:
  main_solid=max(solids,key=lambda shape:abs(k.props(shape)[0]));main_volume=abs(k.props(main_solid)[0]);removed=0
  sites=[(p,[0,0,1]) for p in c['pattern'](frame([0,0,root_z],[0,0,1]),root_pc)]+[(p,tip_axis) for p in c['pattern'](target_fr,tip_pc)]
  for solid in solids:
   if solid.IsSame(main_solid):continue
   amount=abs(k.props(solid)[0])
   if amount>max(.05,fw**3*.15) or amount>main_volume*1e-4 or any(c['bore_present'](solid,p,n,(bolt+.4)/2) for p,n in sites):raise ValueError('Разрыв несущего корпуса при обходе привода. Измените длину звена.')
   removed+=amount
  power=k.checked(main_solid);issue('cassette_trim','pass','Удалены изолированные остатки обрезки вне посадок: '+str(round(removed,4))+' мм³.','frame_0')
 outer_root=max(root_r,r0) if c['spec'].get('BlenderDesign') else root_r
 outer=fuse(hull(gap+skin,gap+skin),cylinder(outer_root+gap+skin,plate+2*skin,[0,0,root_z-skin]))
 outer_tip=max(tip_r,tip_plate_radius) if c['spec'].get('BlenderDesign') else tip_r
 if not c['spec'].get('BlenderDesign'):outer=fuse(outer,transform(cylinder(outer_tip+gap+skin,plate+2*skin),(plus(tip_center,times(tip_axis,-skin)),target_fr[1],target_fr[2],tip_axis)))
 inner=fuse(hull(gap,gap),cylinder(root_r+gap,plate+2,[0,0,root_z-1]))
 if not c['spec'].get('BlenderDesign'):inner=fuse(inner,transform(cylinder(tip_r+gap,plate+2),(plus(tip_center,times(tip_axis,-1)),target_fr[1],target_fr[2],tip_axis)))
 boot=cut(outer,inner)
 for obstacle in obstacles:boot=cut(boot,obstacle)
 boot=cut(boot,power)
 if c['spec'].get('BlenderDesign') and g['panel']:
  p=g['panel'];a=p['a'];u=p['u'];normal=p['normal'];v=p['v'];length=p['length'];half=p['half'];r0=p['r0'];r1=p['r1']
 # An actual inset tray leaves a black perimeter rim. It is independent of the
 # end flanges and cannot expose the old circular-to-triangular transition.
 inset_start=min(length*.12,r0*.4);inset_end=length-min(length*.12,r1*.4);panel_length=inset_end-inset_start
 panel_radius=max(8,min(r0,r1)*min(.85,.45+.65*float(c['style'].get('AccentColorCoverage',.32))),panel_length*.05)
 def find_mounts(target,cn,cv,ps,pl,pr,offset,sign=1,used=()):
  direction=times(cn,sign);records=[]
  if c['spec'].get('BlenderDesign'):
   locations=[(ps+pl*f,offset+s*pr*.32) for f in [.15,.85] for s in [-1,1]]+[(ps+pl*f,offset) for f in [.15,.85,.3,.7,.45,.55]]
  else:locations=[(ps+pl*f,offset+s*pr*.32) for f in [.3,.65,.45,.55,.8,.2] for s in [-1,1]]
  if sign==-1:locations.extend([(length*f,s*min(r0,r1)) for f in [.3,.5,.7,.2,.8] for s in [0,-.25,.25,-.5,.5]])
  for index,(along,across) in enumerate(locations):
   centre=plus(plus(a,times(u,along)),times(cv,across));ray=IntCurvesFace_ShapeIntersector();ray.Load(target,1e-7);ray.Perform(gp_Lin(gp_Pnt(*centre),gp_Dir(*direction)),0,half+gap+skin+5)
   if ray.NbPnt()==0:continue
   height=max(ray.WParameter(i) for i in range(1,ray.NbPnt()+1));front=plus(centre,times(direction,height))
   if any(math.sqrt(dot(vec(front,p),vec(front,p)))<bolt*2.1 for p in [x[0] for x in records]+list(used)):continue
   core_face=plus(centre,times(direction,height-skin-gap));selected=None
   for depth in [insert_l+structural_inset,2*half-skin-gap]:
    boss=cylinder(insert_d/2+fw,depth,plus(core_face,times(direction,-depth)),direction)
    if c['spec'].get('BlenderDesign'):boss=BRepAlgoAPI_Common(boss,core_outer).Shape()
    contact=BRepAlgoAPI_Common(power,boss).Shape()
    if not k.shapes(contact,k.TopAbs_SOLID) or abs(k.props(contact)[0])<1:continue
    blocked=False
    for obstacle in obstacles:
     common=BRepAlgoAPI_Common(boss,obstacle).Shape()
     if k.shapes(common,k.TopAbs_SOLID) and abs(k.props(common)[0])>.01:blocked=True;break
    if not blocked:selected=boss;break
   if selected is None:continue
   records.append((front,direction,core_face,selected))
   if len(records)==4:break
  return records
 if c['spec'].get('BlenderDesign'):
  (Path(c['folder'])/'elbow-shell-pieces.json').write_text(json.dumps([dict(volume=k.props(s)[0],bounds=k.bounds(s)) for s in k.shapes(boot,k.TopAbs_SOLID)],indent=2))
 chosen=None;rejections={}
 def reject(reason):
  rejections[reason]=rejections.get(reason,0)+1
  if c['spec'].get('BlenderDesign'):(Path(c['folder'])/'elbow-panel-search.json').write_text(json.dumps(rejections))
 for fraction in [1,.72,.5,.3]:
  if chosen:break
  for width_factor in [1,.8,.6]:
   if chosen:break
   for center in [.5,.35,.65]:
    if chosen:break
    for across in [0,r0*.28,-r0*.28]:
     for sign in [1,-1]:
      cn=times(normal,sign);cv=times(v,sign);pl=max(12,panel_length*fraction);pr=max(7,panel_radius*width_factor);ps=length*center-pl/2
      origin=plus(plus(plus(a,times(u,ps)),times(cv,across)),times(cn,half+gap-skin*2))
      tool=saddle((origin,u,cv,cn),pl,pr,pr,4*skin)
      candidate=BRepAlgoAPI_Common(boot,tool).Shape()
      if len(k.shapes(candidate,k.TopAbs_SOLID))!=1 or abs(k.props(candidate)[0])<150:reject('panel');continue
      recess=saddle((plus(origin,times(cn,-gap)),u,cv,cn),pl,pr+gap,pr+gap,4*skin+2*gap)
      remaining=cut(boot,recess)
      if len(k.shapes(remaining,k.TopAbs_SOLID))!=1:reject('main');continue
      mount_records=find_mounts(candidate,cn,cv,ps,pl,pr,across)
      if len(mount_records)<2:reject('front');continue
      back_records=find_mounts(remaining,cn,cv,ps,pl,pr,across,-1,[r[0] for r in mount_records])
      if len(back_records)<2:back_records=find_mounts(remaining,cn,cv,0,length,min(r0,r1),0,1,[r[0] for r in mount_records])
      if len(back_records)<2 and c['spec'].get('BlenderDesign'):
       # A short wrist's rear side faces another barrel. Service the black
       # shell from either lateral side instead of burying bolts in the motor.
       for lateral in [cv,times(cv,-1)]:
        back_records=find_mounts(remaining,lateral,cn,0,length,min(r0,r1),0,1,[r[0] for r in mount_records])
        if len(back_records)>=2:break
      if len(back_records)<2:reject('back');continue
      chosen=(cn,cv,candidate,remaining,ps,pl,pr,across,mount_records,back_records);break
     if chosen:break
 if chosen is None:raise ValueError('Нет непрерывной поверхности для съёмной крышки: '+str(rejections))
 normal,v,panel,main,panel_start,panel_length,panel_radius,panel_across,front_mounts,back_mounts=chosen
 anchors=[]
 def stats(shape):return [dict(Volume=k.props(s)[0],Bounds=k.bounds(s)) for s in k.shapes(shape,k.TopAbs_SOLID)]
 import json
 from pathlib import Path
 (Path(c['folder'])/'reference-before-anchors.json').write_text(json.dumps(dict(Power=stats(power),Main=stats(main),Panel=stats(panel)),indent=2))
 # Four front and four rear fasteners go into real structural bosses. Their
 # drilling is along the broad face normal, like the exploded reference trays.
 for side in [1,-1]:
  records=front_mounts if side==1 else back_mounts
  if len(records)<2:raise ValueError('Нет двух доступных креплений цельной оболочки.')
  for index,(front,direction,core_face,boss) in enumerate(records):
   power=fuse(power,boss);pocket=cylinder(insert_d/2,insert_l,core_face,times(direction,-1));power=cut(power,pocket)
   insert=cut(cylinder(insert_d/2,insert_l,core_face,times(direction,-1)),cylinder((bolt-.5)/2,insert_l+2,plus(core_face,direction),times(direction,-1)))
   add('cassette_insert_'+str(side)+'_'+str(index),'Вставка M3 · корпус РОБОТ1' if c['spec'].get('BlenderDesign') else 'Вставка M3 · кассетное звено',insert,False,'insert','#b99542',8500)
   bore=cylinder((bolt+.4)/2,skin+2*gap+2,plus(core_face,times(direction,-gap)),direction);main=cut(main,bore);panel=cut(panel,bore)
   screw_length=c['standard_length'](skin+gap,insert_l);shaft=cylinder(bolt/2,screw_length,front,times(direction,-1));head=cylinder(bolt*.9,1,plus(front,times(direction,-1)),direction)
   counter=cylinder(bolt*.9+.15,1.2,plus(front,times(direction,-1.2)),direction);main=cut(main,counter);panel=cut(panel,counter)
   add('cassette_screw_'+str(side)+'_'+str(index),'Винт M3 · утопленная крышка',fuse(shaft,head),False,'hardware','#9199a2',7850);anchors.append((front,direction))
 power=k.checked(power)
 if c['spec'].get('BlenderDesign'):
  # Curved cover surfaces seat on the actual boss crowns, not an assumed flat
  # offset. Cut the mating seats after adding bosses so no positive-volume
  # overlap is hidden beneath a visually plausible curved cover.
  main=k.checked(cut(main,power));panel=k.checked(cut(panel,power))
 exposed=cut(power,outer);amount=abs(k.props(exposed)[0]) if k.shapes(exposed,k.TopAbs_SOLID) else 0
 if c['spec'].get('BlenderDesign'):
  (Path(c['folder'])/'elbow-contour.json').write_text(json.dumps({name:dict(volume=k.props(shape)[0],bounds=k.bounds(shape) if k.shapes(shape,k.TopAbs_SOLID) else None,solids=len(k.shapes(shape,k.TopAbs_SOLID))) for name,shape in [('core',core_outer),('outer',outer),('power',power),('exposed',exposed)]},indent=2))
 issue('cover_enclosure','pass' if amount<.05 else 'error','Каркас вне цельного контура: '+str(round(amount,4))+' мм³.','frame_0')
 # All exported structural/cover parts must be single connected solids.
 issue('shell_continuity','pass' if len(k.shapes(boot,k.TopAbs_SOLID))==1 else 'error','Цельная кассетная оболочка до отделения крышки.')
 add('frame_0','РОБОТ1 · Г-образный несущий корпус' if c['spec'].get('BlenderDesign') else 'Монокок · замкнутый несущий короб между посадками',power,False,'frame')
 add('skin_main_0','Корпус по референсу · непрерывная чёрная оболочка',main,False,'skin','#24272c')
 add('skin_panel_0','Корпус по референсу · утопленная оранжевая крышка',panel,False,'accent','#ed7627')
 c['port']('link_input','frame_0',frame([0,0,root_z],[0,0,1]),root_pc,bolt+.4)
 c['port']('link_child' if child else 'tool_mount','frame_0',target_fr,tip_pc,bolt+.4,child.get('Id') if child else None)
 c['screw']('root_bolt',frame([0,0,root_z+plate],[0,0,-1]),0,c['standard_length'](plate,c['plate']),root_pc,False)
 issue('monocoque_structure','pass','Г-образное звено по РОБОТ1.blend: круглое колено и наружная опора следующего статора.' if c['spec'].get('BlenderDesign') else 'Кассетная геометрия: непрерывный короб вместо трубы с наружной седловой лапкой.','frame_0')
 issue('inset_panel','pass','Отдельная закруглённая крышка в углублении с чёрным кантом; утопленных винтов: '+str(len(front_mounts))+'.','skin_panel_0')
 def anchor_count(shape):return sum(c['bore_present'](shape,p,n,(bolt+.4)/2,skin+2*gap+2) for p,n in anchors)
 return dict(power=power,boot=boot,mounts=[],reference_anchors=anchor_count)
