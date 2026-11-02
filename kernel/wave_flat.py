"""Flat four-element strain-wave layout. Prescribed ellipse, not elastic/contact FEA.

Fixed circular spline: zf+2 teeth. Output dynamic spline: zf teeth.
With the fixed spline locked, omega_out/omega_in = -2/zf.
Profiles are conventional involutes for a layout study, not production conjugate
strain-wave tooth profiles. Source: harmonicdrive.net/products/component-sets/pancake.
"""
import math
from OCP.gp import gp_Pnt, gp_Dir, gp_Ax2, gp_Vec, gp_Trsf, gp_Ax1, gp_GTrsf
from OCP.BRepPrimAPI import BRepPrimAPI_MakeCylinder, BRepPrimAPI_MakePrism
from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform, BRepBuilderAPI_GTransform, BRepBuilderAPI_MakeFace

def make_flat(v, ev, k):
    n=lambda key,default:ev(v.get(key,default))
    raw=n('Ratio',50)
    if raw != round(raw): raise ValueError('РџРµСЂРµРґР°С‚РѕС‡РЅРѕРµ С‡РёСЃР»Рѕ РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ С†РµР»С‹Рј.')
    ratio=int(raw);m=n('Module',.6);w=n('Thickness',8);wall=n('Wall',.8);rw=n('RingWall',4);gap=n('Clearance',.15);bw=n('BearingWall',1.5);bore=n('Bore',5);out=n('OutputBore',8);phase=math.radians(n('Phase',0))
    if not all(math.isfinite(x) for x in [raw,m,w,wall,rw,gap,bw,bore,out,phase]): raise ValueError('Р Р°Р·РјРµСЂС‹ РґРѕР»Р¶РЅС‹ Р±С‹С‚СЊ РєРѕРЅРµС‡РЅС‹РјРё С‡РёСЃР»Р°РјРё.')
    if not 20<=ratio<=120 or not .15<=m<=2 or not 3<=w<=40 or not .3<=wall<=3 or not 2<=rw<=15 or not .05<=gap<=.5 or not .5<=bw<=4 or min(bore,out)<=0: raise ValueError('РќРµРґРѕРїСѓСЃС‚РёРјС‹Рµ СЂР°Р·РјРµСЂС‹ РїР»РѕСЃРєРѕРіРѕ РІРѕР»РЅРѕРІРѕРіРѕ СЂРµРґСѓРєС‚РѕСЂР°.')
    z=2*ratio;pitch=m*z/2;root=pitch-1.25*m;inner=root-wall;sx=1+1/ratio;sy=1-1/ratio;outer=m*(z+4.5)/2+rw
    if inner*sy-gap-bw<=max(bore,out)/2+2: raise ValueError('РћС‚РІРµСЂСЃС‚РёРµ РЅРµ РїРѕРјРµС‰Р°РµС‚СЃСЏ РІРЅСѓС‚СЂРё РіРµРЅРµСЂР°С‚РѕСЂР° РІРѕР»РЅ.')
    # Large fixed flange bolts must remain outside the circular spline tooth root.
    bolt=max(1.2,min(2,rw*.30));bolt_r=outer-bolt-1
    if bolt_r-bolt < m*(z+4.5)/2+.25: raise ValueError('РЎС‚РµРЅРєР° РІРµРЅС†Р° РјР°Р»Р° РґР»СЏ Р±РѕР»С‚РѕРІРѕРіРѕ С„Р»Р°РЅС†Р°; СѓРІРµР»РёС‡СЊС‚Рµ РµС‘.')
    def move(s,z):
        t=gp_Trsf();t.SetTranslation(gp_Vec(0,0,z));return BRepBuilderAPI_Transform(s,t,True).Shape()
    def rotate(s,a):
        t=gp_Trsf();t.SetRotation(gp_Ax1(gp_Pnt(),gp_Dir(0,0,1)),a);return BRepBuilderAPI_Transform(s,t,True).Shape()
    def cylinder(radius,z,h,x=0,y=0):return BRepPrimAPI_MakeCylinder(gp_Ax2(gp_Pnt(x,y,z),gp_Dir(0,0,1)),radius,h).Shape()
    def tube(ro,ri,z,h):return k.boolean(cylinder(ro,z,h),cylinder(ri,z-.1,h+.2),'cut')
    def gear(teeth,kind,th):return k.gear({'Type':kind,'Teeth':teeth,'Module':m,'Thickness':th,'Outer':2*outer,'Bore':0,'Pressure':20,'Backlash':m*.12,'_Wave':True},ev)
    def deform(s):
        s=rotate(s,-phase/ratio-phase);a=gp_GTrsf();a.SetValue(1,1,sx);a.SetValue(2,2,sy);a.SetValue(3,3,1)
        return rotate(BRepBuilderAPI_GTransform(s,a,True).Shape(),phase)
    total=2*w+gap
    fixed=gear(z+2,'internal',w)
    # Both axial tooth bands belong to one thin, continuous flexible sleeve.
    flex=k.boolean(gear(z,'spur',total),cylinder(inner,-.1,total+.2),'cut');flex=deform(flex)
    dynamic=move(gear(z,'internal',w),w+gap)
    plate=max(2,rw*.75);hub_r=max(out/2+5,pitch*.30)
    # Spoke plate closes the output ring at a separate axial plane. Six windows
    # and six through-fasteners make the output flange recognisable and accessible.
    cap=tube(outer,out/2,total+gap,plate)
    window_r=max(1,(outer-rw-hub_r)*.40);wr=(outer-rw+hub_r)/2
    for i in range(6):
        a=math.tau*(i+.5)/6
        cap=k.boolean(cap,cylinder(window_r,total+gap-.1,plate+.2,wr*math.cos(a),wr*math.sin(a)),'cut')
    # Structural bridges overlap the dynamic ring, avoiding a separated cap solid.
    bridge=tube(outer,outer-rw*.7,total-.2,gap+.4)
    dynamic=k.boolean(k.boolean(dynamic,bridge,'join'),cap,'join')
    # An exposed toothed output disc and small central hub reproduce the open
    # flange layout of the reference. This rigid auxiliary disc is attached to
    # the dynamic spline; it does not replace either of the two working meshes.
    front_z=total+gap+plate-.1
    front=move(gear(z,'spur',max(2,plate)),front_z)
    for i in range(6):
        a=math.tau*(i+.5)/6
        front=k.boolean(front,cylinder(window_r,front_z-.1,plate+.3,wr*math.cos(a),wr*math.sin(a)),'cut')
    dynamic=k.boolean(dynamic,front,'join')
    hub_z=front_z+max(2,plate)-.1
    dynamic=k.boolean(dynamic,cylinder(hub_r,hub_z,3),'join')
    dynamic=k.boolean(dynamic,cylinder(out/2,hub_z+2.9,12),'join')
    for i in range(4):
        a=math.tau*i/4+math.pi/4;rr=hub_r*.75
        dynamic=k.boolean(dynamic,cylinder(.9,hub_z-.1,3.3,rr*math.cos(a),rr*math.sin(a)),'cut')
    result={'ring':fixed,'flex':flex,'output':rotate(dynamic,-phase/ratio)}
    # Analytic ellipse wires share the kernel's construction convention.
    from OCP.gp import gp_Elips
    from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeWire, BRepBuilderAPI_MakeEdge
    def ellipse(a,b):
        ax=gp_Ax2(gp_Pnt(),gp_Dir(0,0,1),gp_Dir(math.cos(phase),math.sin(phase),0))
        return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(gp_Elips(ax,a,b)).Edge()).Wire()
    major=inner*sx-gap;minor=inner*sy-gap
    face=BRepBuilderAPI_MakeFace(ellipse(major,minor));hole=ellipse(major-bw,minor-bw);hole.Reverse();face.Add(hole)
    result['bearing']=BRepPrimAPI_MakePrism(face.Face(),gp_Vec(0,0,total)).Shape()
    plug=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(ellipse(major-bw-gap,minor-bw-gap)).Face(),gp_Vec(0,0,total)).Shape()
    result['generator']=k.boolean(plug,cylinder(bore/2,-.1,total+.2),'cut')
    # Rear mounting flange and actual six holes through both rings at a common PCD.
    rear=tube(outer,max(out/2+3,root*.45),-plate-gap,plate)
    support=tube(outer,outer-rw*.7,-gap-.1,gap+.3)
    fixed=k.boolean(k.boolean(fixed,support,'join'),rear,'join')
    for i in range(6):
        a=math.tau*i/6;x=bolt_r*math.cos(a);y=bolt_r*math.sin(a)
        fixed=k.boolean(fixed,cylinder(bolt,-plate-gap-.1,plate+gap+w+.2,x,y),'cut')
        # Output flange bolt pattern lies on its own rotating ring, staggered
        # from the stationary fasteners. They are not bolts tying both together.
        oa=a+math.pi/6-phase/ratio;ox=bolt_r*math.cos(oa);oy=bolt_r*math.sin(oa)
        result['output']=k.boolean(result['output'],cylinder(bolt,w+gap-.1,w+gap+plate+.2,ox,oy),'cut')
        output_screw=k.boolean(cylinder(bolt*.85,w+gap+.1,w+gap+plate-.1,ox,oy),cylinder(bolt*1.5,total+gap+plate-.1,1.7,ox,oy),'join')
        output_screw=k.boolean(output_screw,cylinder(bolt*.5,total+gap+plate+.8,1,ox,oy),'cut')
        result['output_bolt'+str(i+1)]=output_screw
        screw=tube(bolt*.85,max(.3,bolt*.32),-plate-gap,w+plate+gap)
        t=gp_Trsf();t.SetTranslation(gp_Vec(x,y,0));result['bolt'+str(i+1)]=BRepBuilderAPI_Transform(screw,t,True).Shape()
    result['ring']=fixed
    return {name:k.checked(s) for name,s in result.items()}
