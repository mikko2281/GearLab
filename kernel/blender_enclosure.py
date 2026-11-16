"""Analytic L links measured from РОБОТ1.blend; no triangle-to-STEP conversion.

The outside follows the author's alternating elbows. A sleeve surrounds the
next stator and carries its rear mounting plate; it does not intersect the
motor case or pretend that a tangent visual tube is a mechanical connection.
"""
import math
from OCP.gp import gp_Pnt,gp_Dir,gp_Ax2
from OCP.BRepPrimAPI import BRepPrimAPI_MakeSphere,BRepPrimAPI_MakeTorus
from procedural_body import plus,times,dot,cross,norm,vec,cylinder,transform

def terminal(c):
 """Short coaxial tool carrier; a long spline detour is unnecessary here."""
 k=c['k'];cut=c['cut'];fuse=c['fuse'];a=c['root_z'];b=c['target_fr'][0][2];bolt=c['bolt'];r=c['root_r'];plate=c['frame_plate']
 if b<a+4:raise ValueError('Фланец инструмента слишком близко к выходному валу.')
 power=cut(cylinder(r,b-a,[0,0,a]),cylinder(c['bi']/2+.1,b-a+2,[0,0,a-1]))
 power=cut(power,cylinder(c['pilot_r']+.1,2.15,[0,0,a-.01]))
 # The two bolt circles are independent: input screws have a recessed head.
 from procedural_body import frame
 power=c['drill'](power,frame([0,0,a-1],[0,0,1]),c['root_pc'],bolt+.4,plate+2)
 power=c['drill'](power,frame([0,0,a+plate-1.2],[0,0,1]),c['root_pc'],bolt*1.8+.3,b-a-plate+2.2)
 power=c['drill'](power,frame([0,0,b-plate],[0,0,1]),c['tip_pc'],bolt+.4,plate+1)
 power=k.checked(power);c['add']('frame_0','РОБОТ1 · компактный фланец инструмента',power,False,'frame')
 c['port']('link_input','frame_0',frame([0,0,a],[0,0,1]),c['root_pc'],bolt+.4)
 c['port']('tool_mount','frame_0',c['target_fr'],c['tip_pc'],bolt+.4)
 c['screw']('root_bolt',frame([0,0,a+plate],[0,0,-1]),0,c['standard_length'](plate,c['plate']),c['root_pc'],False)
 c['issue']('cover_enclosure','pass','Короткий открытый фланец инструмента сам образует наружный контур.','frame_0')
 c['issue']('shell_continuity','pass','Компактный фланец — единое тело.','frame_0')
 c['issue']('axis_tangency','pass','Короткий фланец соосен выходному валу.','frame_0')
 return dict(power=power,boot=power,mounts=[],reference_anchors=lambda shape:0)

def geometry(c):
 k=c['k'];fuse=c['fuse'];child=c['child'];fr=c['fr'];cd=c['cd'];gap=c['gap'];fw=c['fw'];skin=c['skin']
 a=[0,0,c['root_z']];own=[0,0,1];tip=c['tip_axis'];hub=fr[0]
 requested=float(c['m'].get('ReferenceDiameter',78))/2
 radius=max(c['core_r'],requested-skin-gap, c['root_r']-fw)
 parallel=abs(dot(own,tip))>.95
 if parallel:
  delta=vec(a,hub);u=plus(delta,times(own,-dot(delta,own)));length=math.sqrt(dot(u,u));u=norm(u)
  bend=max(radius+skin+gap+.5,float(c['m'].get('CableBendRadius',16)))
  if length<bend+fw*2:raise ValueError('Недостаточно длины для Г-образного звена и радиуса изгиба.')
  normal=own;v=norm(cross(normal,u));panel_a=plus(a,times(own,bend));panel_length=length-bend
  # Analytic quarter torus + cylinder retain elementary STEP faces. Avoid a
  # separately approximated spline sweep for each wall offset near the bend.
  centre=plus(a,times(u,bend));p2=plus(centre,times(own,bend))
  axes=gp_Ax2(gp_Pnt(*centre),gp_Dir(*norm(cross(own,u))),gp_Dir(*times(u,-1)))
  def tube(r):
   elbow=BRepPrimAPI_MakeTorus(axes,bend,r,math.pi/2).Shape()
   return k.checked(fuse(elbow,cylinder(r,length-bend,p2,u)))
 else:
  # Perpendicular joint barrels meet the straight leg at their true crossing.
  # The end sphere produces a rounded elbow rather than a diagonal bridge.
  axial=dot(vec(a,hub),own)
  if axial<fw*2:raise ValueError('Недостаточно места между перпендикулярными приводами.')
  u=own;normal=norm(tip);v=norm(cross(normal,u));length=axial;panel_a=a;panel_length=length
  p3=plus(a,times(own,axial))
  def tube(r):return fuse(cylinder(r,axial,a,own),BRepPrimAPI_MakeSphere(gp_Pnt(*p3),r).Shape())
 def hull(extra,depth_extra=0):
  r=radius+extra
  sleeve_radius=cd['outer']+gap+fw+extra
  sleeve=transform(cylinder(sleeve_radius,cd['plate']+gap-cd['rear']+c['frame_plate'],[0,0,cd['rear']-c['frame_plate']]),fr)
  return k.checked(fuse(tube(r),sleeve))
 def core(inset):return hull(-inset)
 panel=None
 if not parallel and length<radius*2:
  pr=cd['outer']+gap+fw
  panel=dict(a=plus(hub,times(tip,cd['rear'])),u=tip,normal=own,v=norm(cross(own,tip)),length=cd['plate']-cd['rear'],half=pr,r0=pr,r1=pr)
 return dict(hull=hull,core_hull=core,a=panel_a,u=u,normal=normal,v=v,length=panel_length,
             r0=radius,r1=radius,half=radius,structural_inset=0,tip_plate_radius=cd['outer']+gap+fw,panel=panel)
