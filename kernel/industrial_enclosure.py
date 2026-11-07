"""Independent dimensioned industrial link; no Blender meshes, bones or units.

Circular joint cheeks and a tapered rounded rectangular bridge are built as
native OCCT solids around the existing mechanical flange interfaces.
"""
import math
from OCP.BRepOffsetAPI import BRepOffsetAPI_ThruSections
from procedural_body import plus,times,cylinder,curve_profile

def geometry(c,a,b,u,v,normal,r0,r1,half):
    k=c['k'];length=math.dist(a,b);fw=c['fw']
    waist=max(c['core_r']+fw+3,min(r0,r1)*.72)
    half=max(half,c['core_r']+fw+3)
    root=max(r0,waist+fw);tip=max(r1,waist+fw)
    stations=[(0,root),(.10,root*.98),(.23,waist),(.77,waist),(.90,tip*.98),(1,tip)]
    def hull(extra,depth_extra=0):
        builder=BRepOffsetAPI_ThruSections(True,True,1e-6)
        builder.CheckCompatibility(False)
        for fraction,width in stations:
            x=width+extra;y=half+extra
            radius=min(width,half)*.60+extra
            if min(x,y,radius)<=.2:raise ValueError('Стенка промышленного звена превышает размер сечения.')
            fr=(plus(a,times(u,length*fraction)),v,normal,u)
            builder.AddWire(curve_profile(fr,x,y,'rounded_rectangle',radius/min(x,y)))
        builder.Build()
        if not builder.IsDone():raise ValueError('Не построен параметрический промышленный профиль.')
        bridge=builder.Shape()
        for centre,radius in [(a,root),(b,tip)]:
            cheek=cylinder(radius+extra,2*(half+extra),plus(centre,times(normal,-half-extra)),normal)
            bridge=c['fuse'](bridge,cheek)
        return k.checked(bridge)
    return dict(hull=hull,core_hull=lambda inset:hull(-inset),r0=root,r1=tip,half=half,
                waist=waist,a=a,b=b,u=u,v=v,normal=normal,length=length,structural_inset=0)

def finish(c,e):
    """Service clamshell pieces are individually fastened, never floating art."""
    from OCP.BRepAlgoAPI import BRepAlgoAPI_Common
    from procedural_body import box,transform,frame
    k=c['k'];power=e['power'];boot=e['boot'];a=e['a'];u=e['u'];v=e['v'];normal=e['normal'];gap=c['gap'];skin=c['skin'];plate=c['frame_plate'];bolt=c['bolt'];anchors=[]
    covers=[];debug=[]
    for side,(lo,hi) in enumerate([(-5000,-gap/2),(gap/2,5000)]):
        tool=transform(box([-5000,-5000,lo,5000,5000,hi]),(a,u,v,normal))
        clipped=BRepAlgoAPI_Common(boot,tool).Shape()
        for index,solid in enumerate(k.shapes(clipped,k.TopAbs_SOLID)):
            if abs(k.props(solid)[0])<.1:continue
            records=e['find_mounts'](solid,normal,v,-e['r0'],e['length']+e['r0']+e['r1'],max(e['r0'],e['r1']),0,-1 if side==0 else 1)
            debug.append(dict(side=side,index=index,bounds=k.bounds(solid),volume=k.props(solid)[0],mounts=len(records)))
            if len(records)<2:
                c['issue']('service_opening','warning','Малая свободная накладка исключена: оставлен доступ к механическому узлу.');continue
            covers.append((side,index,solid,records))
    import json
    from pathlib import Path
    (Path(c['folder'])/'industrial-covers.json').write_text(json.dumps(dict(geometry={key:val for key,val in c['industrial_geometry'].items() if not callable(val)},covers=debug,shapes={key:[dict(volume=k.props(q)[0],bounds=k.bounds(q)) for q in k.shapes(e[key],k.TopAbs_SOLID)] for key in ['core_outer','core_inner','power','outer','inner','boot']}),indent=2))
    if len(covers)<2:raise ValueError('Нет двух закрепляемых промышленных кожухов.')
    for side,index,shape,records in covers:
        for number,(front,direction,core_face,boss) in enumerate(records):
            power=c['fuse'](power,boss);power=c['cut'](power,cylinder(c['insert_d']/2,c['insert_l'],core_face,times(direction,-1)))
            insert=c['cut'](cylinder(c['insert_d']/2,c['insert_l'],core_face,times(direction,-1)),cylinder((bolt-.5)/2,c['insert_l']+2,plus(core_face,direction),times(direction,-1)))
            key='industrial_cover_'+str(side)+'_'+str(index)+'_'+str(number)
            c['add'](key+'_insert','Промышленный кожух · вставка M3',insert,False,'insert','#b99542',8500)
            shape=c['cut'](shape,cylinder((bolt+.4)/2,skin+2*gap+2,plus(core_face,times(direction,-gap)),direction))
            screw=c['fuse'](cylinder(bolt/2,c['standard_length'](skin+gap,c['insert_l']),front,times(direction,-1)),cylinder(bolt*.9,1,front,direction))
            c['add'](key+'_screw','Промышленный кожух · винт M3',screw,False,'hardware','#9199a2',7850);anchors.append((front,direction))
        shape=k.checked(c['cut'](shape,power));c['add']('skin_industrial_'+str(side)+'_'+str(index),'Промышленный кожух · съёмная секция',shape,False,'skin','#626a73');c['parts'][-1]['_anchors']=[(front,direction) for front,direction,_,_ in records]
    power=k.checked(power);c['add']('frame_0','Промышленное звено · силовая оболочка между фланцами',power,False,'frame')
    c['port']('link_input','frame_0',frame([0,0,c['root_z']],[0,0,1]),c['root_pc'],bolt+.4)
    child=c['child'];c['port']('link_child' if child else 'tool_mount','frame_0',c['target_fr'],c['tip_pc'],bolt+.4,child.get('Id') if child else None)
    c['screw']('root_bolt',frame([0,0,c['root_z']+plate],[0,0,-1]),0,c['standard_length'](plate,c['plate']),c['root_pc'],False)
    c['issue']('industrial_construction','pass','Силовая деталь и отдельные кожухи построены независимо от визуального примера.','frame_0')
    return dict(power=power,boot=boot,mounts=[],reference_anchors=lambda shape:sum(c['bore_present'](shape,p,n,(bolt+.4)/2,skin+2*gap+2) for p,n in anchors),industrial_geometry=c.get('industrial_geometry'),obstacles=e['obstacles'])
