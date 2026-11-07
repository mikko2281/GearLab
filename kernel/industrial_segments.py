"""Bolted manufacturing splits for the independent industrial bridge.

The split is a structural flange joint, not a grid cut of a visual mesh.
All results are checked native solids; cover pieces receive actual mounts.
"""
import math
from OCP.BRepAlgoAPI import BRepAlgoAPI_Common
from OCP.IntCurvesFace import IntCurvesFace_ShapeIntersector
from OCP.gp import gp_Pnt,gp_Vec,gp_Dir,gp_Lin
from procedural_body import plus,times,dot,vec,box,transform,cylinder,curve_profile

def apply(c):
    if not c['spec'].get('IndustrialDesign') or not c.get('reference'):return 1
    g=(c.get('reference_result') or {}).get('industrial_geometry')
    if not g:return 1
    k=c['k'];parts=c['parts'];mates=c['mates'];a=g['a'];u=g['u'];v=g['v'];n=g['normal'];length=g['length'];fw=c['fw'];gap=c['gap'];skin=c['skin']
    frame=next(p for p in parts if p['Id']=='frame_0');bounds=k.bounds(frame['Shape'])
    if c['spec'].get('Root'):
        # Avoid a cross flange through the neighbouring barrel when the complete
        # root link and service shells fit the printer in a supported orientation.
        from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
        from OCP.gp import gp_Trsf,gp_Ax1
        group=[p for p in parts if p is frame or p['Role'] in {'skin','accent'}]
        found=None
        for rx in [0,90,45,30,60,15,75,105,120,135,150,165]:
            if found:break
            for ry in [0,90,45,30,60]:
                if found:break
                for rz in [0,90,45,30,60,15,75,105,120,135,150,165]:
                    boxes=[]
                    for part in group:
                        shape=part['Shape']
                        for angle,axis in [(rx,[1,0,0]),(ry,[0,1,0]),(rz,[0,0,1])]:
                            if angle:
                                tr=gp_Trsf();tr.SetRotation(gp_Ax1(gp_Pnt(0,0,0),gp_Dir(*axis)),angle*math.pi/180);shape=BRepBuilderAPI_Transform(shape,tr,False).Shape()
                        boxes.append(k.bounds(shape))
                    dimensions=[max(b[i+3] for b in boxes)-min(b[i] for b in boxes) for i in range(3)]
                    if max(dimensions)<=244:found=[rx,ry,rz];break
        if found:
            for part in group:part['_print_angles']=found
            c['issue']('manufacturing_orientation','pass','Корневое силовое звено помещается целиком; ориентация печати '+str(found)+' градусов.','frame_0')
            return 1
    if max(bounds[i+3]-bounds[i] for i in range(3))<=235:return 1
    count=max(2,math.ceil((length+g['r0']+g['r1'])/200));count=min(4,count)
    waist=g['waist'];half=g['half'];B=5000
    sites=[dot(vec(a,k.props(p['Shape'])[1]),u) for p in parts if p['Role'] in {'hardware','insert'}]
    seams=[]
    for i in range(1,count):
        ideal=length*i/count;candidates=[ideal+length*f for f in [0,-.03,.03,-.06,.06]]
        candidates=[s for s in candidates if length*.23<=s<=length*.77 and all(abs(s-p)>70 for p in seams)]
        acceptable=[s for s in candidates if all(abs(s-p)>14 for p in sites)]
        if not acceptable:raise ValueError('Технологический стык попадает в существующее крепление крышки.')
        seams.append(min(acceptable,key=lambda s:abs(s-ideal)))
    seams.sort()
    def cut(x,y):return k.checked(c['cut'](x,y))
    def fuse(x,y):return k.checked(c['fuse'](x,y))
    def centre(s):return plus(a,times(u,s))
    def region(lo,hi):return transform(box([-B,-B,lo,B,B,hi]),(a,v,n,u))
    def projection(point):return dot(vec(a,point),u)
    def prism(wire,height):return k.BRepPrimAPI_MakePrism(k.BRepBuilderAPI_MakeFace(wire).Face(),gp_Vec(*times(u,height))).Shape()
    def flange(s,start):
        fr=(centre(s+start),v,n,u)
        outer=prism(curve_profile(fr,waist+16,half+16,'rounded_rectangle',.3),8)
        inner_fr=(centre(s+start-1),v,n,u);corner=min(waist,half)*.6-fw
        inner=prism(curve_profile(inner_fr,waist-fw,half-fw,'rounded_rectangle',corner/min(waist-fw,half-fw)),10)
        shape=cut(outer,inner)
        for x in [-waist*.5,waist*.5]:
            for y in [-half-8,half+8]:shape=cut(shape,cylinder(2.75,24,plus(plus(centre(s-12),times(v,x)),times(n,y)),u))
        for x in [-waist-8,waist+8]:shape=cut(shape,cylinder(1.6,24,plus(centre(s-12),times(v,x)),u))
        if c['spec'].get('Root'):
            for obstacle in c['reference_result'].get('obstacles',[]):shape=cut(shape,obstacle)
        for x in [-waist*.5,waist*.5]:
            for y in [-half-8,half+8]:
                point=plus(plus(centre(s-12),times(v,x)),times(n,y))
                if not c['bore_present'](shape,point,u,2.75,24):raise ValueError('Технологический фланец обрезает отверстие M5: нужна другая ориентация или положение стыка.')
        return shape
    # Preserve all old parts until the complete replacement validates.
    replacement=[];power=[];edges=[-B]+seams+[B]
    for index,(lo,hi) in enumerate(zip(edges,edges[1:])):
        shape=k.checked(BRepAlgoAPI_Common(frame['Shape'],region(lo,hi)).Shape())
        if index:shape=fuse(shape,flange(lo,0))
        if index<count-1:shape=fuse(shape,flange(hi,-8))
        if len(k.shapes(shape,k.TopAbs_SOLID))!=1:raise ValueError('Промышленный стык разорвал силовую секцию.')
        item=dict(frame,Id='frame_0_s'+str(index),Label='Промышленное звено · силовая секция '+str(index+1),Shape=shape);power.append(item);replacement.append(item)
    skin_roles={'skin','accent'}
    for original in parts:
        if original is frame:continue
        if original['Role'] not in skin_roles:replacement.append(original);continue
        # Service covers stop short of the structural split flange.
        skin_shape=original['Shape']
        for seam in seams:skin_shape=c['cut'](skin_shape,region(seam-8.5,seam+8.5))
        for index,(lo,hi) in enumerate(zip(edges,edges[1:])):
            clipped=BRepAlgoAPI_Common(skin_shape,region(lo,hi)).Shape()
            for number,solid in enumerate(k.shapes(clipped,k.TopAbs_SOLID)):
                if k.props(solid)[0]<.1:continue
                item=dict(original,Id=original['Id']+'_s'+str(index)+'_'+str(number),Label=original['Label']+' · секция '+str(index+1),Shape=k.checked(solid));replacement.append(item)
    def hardware(pid,label,shape,role='hardware',rho=7850):
        replacement.append(dict(Id=pid,Label=label,Shape=k.checked(shape),InputSide=False,Role=role,Color='#8d959e',Density=rho))
    # Four through bolts, nuts and two locating dowels per structural seam.
    for number,seam in enumerate(seams):
        for index,(x,y) in enumerate((x,y) for x in [-waist*.5,waist*.5] for y in [-half-8,half+8]):
            point=plus(plus(centre(seam-9),times(v,x)),times(n,y));shaft=cylinder(2.5,25,point,u);head=cylinder(4.25,5,plus(point,times(u,-5)),u)
            hardware('splice_'+str(number)+'_bolt_'+str(index),'Стык · M5 × 25 (резьба условная)',fuse(shaft,head))
            nut=cut(cylinder(4.6,5,plus(point,times(u,18)),u),cylinder(2.5,7,plus(point,times(u,17)),u));hardware('splice_'+str(number)+'_nut_'+str(index),'Стык · гайка M5',nut)
        for index,x in enumerate([-waist-8,waist+8]):hardware('splice_'+str(number)+'_dowel_'+str(index),'Стык · установочный штифт 3 × 16',cylinder(1.5,16,plus(centre(seam-8),times(v,x)),u))
    # Each resulting cover is screwed to a real boss in its own structural section.
    for cover in [p for p in replacement if p['Role'] in skin_roles]:
        shape=cover['Shape'];bb=k.bounds(shape)
        values=[projection([bb[(bits&1)*3],bb[1+((bits>>1)&1)*3],bb[2+((bits>>2)&1)*3]]) for bits in range(8)]
        lo=max(0,min(values));hi=min(length,max(values))
        attached=[p for p,axis in cover.get('_anchors',[]) if c['bore_present'](shape,p,axis,1.7,skin+2*gap+2)]
        for fraction,across in ((f,x) for f in [.25,.75,.4,.6,.15,.85,.05,.95] for x in [0,-waist*.25,waist*.25,-waist*.5,waist*.5,-waist*.75,waist*.75]):
            if len(attached)>=2:break
            s=lo+(hi-lo)*fraction;index=sum(s>x for x in seams);structural=power[index]
            for sign in [1,-1]:
                origin=plus(centre(s),times(v,across));direction=times(n,sign)
                ray=IntCurvesFace_ShapeIntersector();ray.Load(shape,1e-7);ray.Perform(gp_Lin(gp_Pnt(*origin),gp_Dir(*direction)),0,half+skin+gap+2)
                if not ray.NbPnt():continue
                distance=max(ray.WParameter(i) for i in range(1,ray.NbPnt()+1));front=plus(origin,times(direction,distance))
                if any(math.dist(front,p)<15 for p in attached):continue
                ray.Load(structural['Shape'],1e-7);ray.Perform(gp_Lin(gp_Pnt(*origin),gp_Dir(*direction)),0,distance-gap*.5)
                if not ray.NbPnt():continue
                core_distance=max(ray.WParameter(i) for i in range(1,ray.NbPnt()+1))
                if distance-core_distance>skin+gap+2:continue
                face=plus(origin,times(direction,core_distance));boss=cylinder(5.5,8,plus(face,times(direction,-8)),direction)
                contact=BRepAlgoAPI_Common(structural['Shape'],boss).Shape()
                if not k.shapes(contact,k.TopAbs_SOLID) or k.props(contact)[0]<1:continue
                blocked=False
                for other in replacement:
                    if other['Role'] in {'hardware','insert','skin','accent'} or other in power:continue
                    overlap=BRepAlgoAPI_Common(other['Shape'],boss).Shape()
                    if k.shapes(overlap,k.TopAbs_SOLID) and k.props(overlap)[0]>.05:blocked=True;break
                if blocked:continue
                structural['Shape']=fuse(structural['Shape'],boss);structural['Shape']=cut(structural['Shape'],cylinder(2.3,6,face,times(direction,-1)))
                shape=cut(shape,cylinder(1.7,skin+gap+3,plus(front,direction),times(direction,-1)))
                pid='section_cover_'+cover['Id']+'_'+str(len(attached));screw=fuse(cylinder(1.5,10,front,times(direction,-1)),cylinder(2.7,1,front,direction));hardware(pid,'Секционная крышка · M3 × 10',screw)
                insert=cut(cylinder(2.3,6,face,times(direction,-1)),cylinder(1.25,8,plus(face,direction),times(direction,-1)));hardware(pid+'_insert','Секционная крышка · вставка M3 × 6',insert,'insert',8500)
                cover.setdefault('_added_anchors',[]).append((front,direction));attached.append(front);break
        if len(attached)<2:raise ValueError('Нет двух доступных винтов для секционной крышки '+cover['Id'])
        cover['Shape']=shape
    for cover in [p for p in replacement if p['Role'] in skin_roles]:
        for structural in power:cover['Shape']=cut(cover['Shape'],structural['Shape'])
    for mate in mates:
        if mate['PartId']=='frame_0':mate['PartId']='frame_0_s'+str(sum(projection(mate['Origin'])>x for x in seams))
    for p in replacement:
        k.checked(p['Shape'])
        if len(k.shapes(p['Shape'],k.TopAbs_SOLID))!=1:raise ValueError('Секционная деталь содержит несвязные тела: '+p['Id'])
    parts[:]=replacement
    c['issue']('manufacturing_sections','pass','Силовое звено разделено на '+str(count)+' секций с фланцами, M5 и установочными штифтами.','frame_0_s0')
    return count
