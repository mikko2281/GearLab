"""Parametric strain-wave concept geometry, not a flex/contact stress model."""
import math,json,hashlib
from OCP.gp import gp_Pnt,gp_Dir,gp_Ax2,gp_Elips,gp_GTrsf,gp_Trsf,gp_Ax1,gp_Vec
from OCP.BRepBuilderAPI import BRepBuilderAPI_MakeWire,BRepBuilderAPI_MakeEdge,BRepBuilderAPI_MakeFace,BRepBuilderAPI_GTransform,BRepBuilderAPI_Transform
from OCP.BRepPrimAPI import BRepPrimAPI_MakePrism,BRepPrimAPI_MakeCylinder
from OCP.BRepOffsetAPI import BRepOffsetAPI_ThruSections
CACHE={}

def make_wave(values,ev,k):
    if values.get('Layout')=='flat':
        from wave_flat import make_flat
        flat_key='flat:'+hashlib.sha256(json.dumps({key:(value if key=='Layout' else ev(value)) for key,value in values.items()},sort_keys=True).encode()).hexdigest()
        if flat_key not in CACHE:
            CACHE[flat_key]=make_flat(values,ev,k)
            if len(CACHE)>10:CACHE.pop(next(iter(CACHE)))
        return CACHE[flat_key]
    value=lambda key,default:ev(values.get(key,default))
    ratio=int(value('Ratio',50));module=value('Module',.6);depth=value('CupDepth',25);width=value('Thickness',8);wall=value('Wall',.8);ringwall=value('RingWall',4);bore=value('Bore',5);output=value('OutputBore',8);gap=value('Clearance',.15);bearing=value('BearingWall',1.5);phase=math.radians(value('Phase',0))
    if not 20<=ratio<=120 or not .15<=module<=2 or not 10<=depth<=100 or not 3<=width<=40 or not .3<=wall<=3 or not 2<=ringwall<=15 or not .05<=gap<=.5 or not .5<=bearing<=4:raise ValueError('Волновой редуктор: i 20–120, модуль 0,15–2, глубина чашки 10–100, венец 3–40, стенка чашки 0,3–3 мм.')
    zf=2*ratio;zc=zf+2;pitch=module*zf/2;root=pitch-1.25*module;inner=root-wall;deformation=module;kappa=deformation/pitch;sx,sy=1+kappa,1-kappa;floor=max(1.5,2*wall)
    if min(inner*sx,inner*sy)-gap-bearing<=max(bore,output)/2+2 or bore<=0 or output<=0:raise ValueError('Отверстие или стенки слишком велики для выбранного модуля и числа зубьев.')
    key=hashlib.sha256(json.dumps([ratio,module,depth,width,wall,ringwall,bore,output,gap,bearing,phase]).encode()).hexdigest()
    if key in CACHE:return CACHE[key]
    angle_output=-phase/ratio
    def ellipse(a,b,z,angle=phase):
        axis=gp_Ax2(gp_Pnt(0,0,z),gp_Dir(0,0,1),gp_Dir(math.cos(angle),math.sin(angle),0))
        return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(gp_Elips(axis,max(a,b),min(a,b))).Edge()).Wire()
    def loft(a0,b0,z0,a1,b1,z1):
        builder=BRepOffsetAPI_ThruSections(True,False,1e-7);builder.AddWire(ellipse(a0,b0,z0));builder.AddWire(ellipse(a1,b1,z1));builder.Build();return k.checked(builder.Shape())
    def rotate(shape,angle):
        tr=gp_Trsf();tr.SetRotation(gp_Ax1(gp_Pnt(),gp_Dir(0,0,1)),angle);return BRepBuilderAPI_Transform(shape,tr,True).Shape()
    ring=k.gear({'Type':'internal','Teeth':zc,'Module':module,'Thickness':width,'Outer':module*(zc+2.5)+2*ringwall,'Bore':0,'Pressure':20,'Backlash':module*.12,'_Wave':True},ev)
    rim=k.gear({'Type':'spur','Teeth':zf,'Module':module,'Thickness':width,'Bore':0,'Pressure':20,'Backlash':module*.12,'_Wave':True},ev)
    rim=rotate(rim,angle_output-phase)
    affine=gp_GTrsf();affine.SetValue(1,1,sx);affine.SetValue(2,2,sy);affine.SetValue(3,3,1)
    rim=BRepBuilderAPI_GTransform(rim,affine,True).Shape();rim=rotate(rim,phase)
    # Stay inside the tooth-root circle: tangent coincidence at the root created
    # tiny split faces which did not survive STEP serialization.
    neck=root-.1*module
    outside=loft(root,root,-depth+floor,neck*sx,neck*sy,.15)
    # Cut the cavity after joining the tooth rim. A single cavity tool avoids
    # nearly coincident elliptical bore edges at the cup/rim junction.
    cavity=BRepOffsetAPI_ThruSections(True,True,1e-7)
    for wire in [ellipse(inner,inner,-depth+floor),ellipse(inner*sx,inner*sy,0),ellipse(inner*sx,inner*sy,width+1)]:cavity.AddWire(wire)
    cavity.Build();inside=k.checked(cavity.Shape())
    cup=outside
    base=BRepPrimAPI_MakeCylinder(gp_Ax2(gp_Pnt(0,0,-depth),gp_Dir(0,0,1)),root,floor+.1).Shape()
    base=k.boolean(base,BRepPrimAPI_MakeCylinder(gp_Ax2(gp_Pnt(0,0,-depth-.1),gp_Dir(0,0,1)),output/2,floor+.3).Shape(),'cut')
    # Output mounting pattern is attached to the material rotation of the cup bottom.
    radius=max(output/2+4,root*.66)
    for i in range(4):
        angle=math.tau*i/4+angle_output;tool=BRepPrimAPI_MakeCylinder(gp_Ax2(gp_Pnt(radius*math.cos(angle),radius*math.sin(angle),-depth-.1),gp_Dir(0,0,1)),1.6,floor+.3).Shape();base=k.boolean(base,tool,'cut')
    cup=k.boolean(k.boolean(k.boolean(cup,base,'join'),rim,'join'),inside,'cut')
    major,minor=inner*sx-gap,inner*sy-gap
    bearing_face=BRepBuilderAPI_MakeFace(ellipse(major,minor,0));hole=ellipse(major-bearing,minor-bearing,0);hole.Reverse();bearing_face.Add(hole)
    race=k.checked(BRepPrimAPI_MakePrism(bearing_face.Face(),gp_Vec(0,0,width)).Shape())
    plug=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(ellipse(major-bearing-gap,minor-bearing-gap,0)).Face(),gp_Vec(0,0,width)).Shape()
    plug=k.boolean(plug,BRepPrimAPI_MakeCylinder(bore/2,width).Shape(),'cut')
    result={'ring':k.checked(ring),'flex':k.checked(cup),'generator':k.checked(plug),'bearing':race}
    CACHE[key]=result
    if len(CACHE)>10:CACHE.pop(next(iter(CACHE)))
    return result
