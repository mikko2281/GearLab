"""Blender background: build a new normalized review workspace from the baked reference.
No original .blend is modified and no source scripts/constraints are executed.
blender --background --factory-startup --python THIS -- ASSET OUTPUT
"""
import base64,json,struct,sys,zlib
from pathlib import Path
import bpy
from mathutils import Matrix,Vector
args=sys.argv[sys.argv.index('--')+1:];asset=json.loads(Path(args[0]).read_text('utf-8'));out=Path(args[1]).resolve();out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.unit_settings.length_unit='MILLIMETERS'
collection=bpy.data.collections.new('AUTHOR DESIGN — REFERENCE ONLY');scene.collection.children.link(collection)
rig=bpy.data.collections.new('DEMONSTRATION JOINTS — NOT VERIFIED LIMITS');scene.collection.children.link(rig)
pivots=[]
for i,p in enumerate(asset['Pivots']):
    ob=bpy.data.objects.new('J'+str(i+1),None);rig.objects.link(ob);ob.empty_display_type='PLAIN_AXES';ob.empty_display_size=.04
    ob.location=Vector(p)/1000;ob.rotation_euler=Vector(asset['Axes'][i]).to_track_quat('Z','Y').to_euler()
    ob['LocalMotionAxis']='Z (axis inferred from source geometry; not physically validated)';ob['EngineeringStatus']='UNVERIFIED_REFERENCE';ob['Axis']=asset['Axes'][i];ob['Limits']='Demonstration only: ±20 degrees'
    pivots.append(ob)
bpy.context.view_layer.update()
for i in range(len(pivots)-1,0,-1):
    child=pivots[i];parent=pivots[i-1];matrix=child.matrix_world.copy();child.parent=parent;child.matrix_world=matrix
for part in asset['Parts']:
    data=zlib.decompress(base64.b64decode(part['Preview']),-15);magic,n,c=struct.unpack_from('<3i',data,0);assert magic==0x344D5043
    pivot=Vector(asset['Pivots'][part['Joint']]);vertices=[];normals=[]
    for i in range(n):
        vertices.append(tuple((Vector(struct.unpack_from('<3f',data,12+i*24))+pivot)/1000));normals.append(struct.unpack_from('<3f',data,24+i*24))
    ids=struct.unpack_from('<'+'i'*c,data,12+24*n);faces=[ids[i:i+3] for i in range(0,c,3)]
    mesh=bpy.data.meshes.new(part['Name']);mesh.from_pydata(vertices,[],faces);mesh.update()
    for poly in mesh.polygons:poly.use_smooth=True
    mesh.normals_split_custom_set([normals[i] for face in faces for i in face])
    ob=bpy.data.objects.new(part['Name'],mesh);collection.objects.link(ob);ob['EngineeringStatus']='REFERENCE — NOT FOR PRINTING';ob['OriginalObject']=part['Name'];ob['Mass']='Unknown: source may be open/nonmanifold'
    if not part['Fixed']:
        parent=pivots[part['Joint']];ob.parent=parent;ob.matrix_parent_inverse=parent.matrix_world.inverted()
    ob.color=(.46,.49,.54,1) if part['SourceType']=='MESH' else (.1,.11,.12,1)
scene['Height_mm']=1000;scene['Payload_kg']=.3;scene['Status']='REFERENCE_NOT_MANUFACTURING_VALIDATED';scene['SourceSHA256']=asset['SourceSHA256']
text=bpy.data.texts.new('ПРОЧИТАТЬ — СТАТУС ПРОВЕРКИ')
text.write('Высота в исходной позе: 1000 мм. Груз: 300 г.\nЭто нормализованный референс, а не готовый к печати робот.\n'
           '13 из 17 исходных сеток открыты/некорректны. Визуальные подшипники и кабели не являются выбранной механикой.\n'
           'Семь групп модели и их оси — демонстрация. Пределы движения, столкновения и захват не проверены.\n'
           'Приняты Bambu Lab P1S / PETG / прежние NEMA17; сопло 0,4 мм и длина мотора 44 мм предполагаются. Нужны масса звеньев, рабочие характеристики мотора и передачи.\n'
           'См. ИНЖЕНЕРНЫЙ_ОТЧЁТ.md и fit-specimens в соседних папках. Исходный .blend сохранён отдельно без изменения.\n')
scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
shade=scene.display.shading;shade.light='STUDIO';shade.color_type='OBJECT';shade.show_shadows=True;shade.show_cavity=True;shade.cavity_type='BOTH';shade.background_type='VIEWPORT';shade.background_color=(.18,.19,.21)
centre=Vector((.25,0,.5));camera=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));scene.collection.objects.link(camera);camera.data.type='ORTHO';camera.data.ortho_scale=1.65;camera.location=centre+Vector((2,-4,2));camera.rotation_euler=(centre-camera.location).to_track_quat('-Z','Y').to_euler();scene.camera=camera
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=1.8;area.spaces.active.region_3d.view_location=centre
            area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion();area.spaces.active.clip_end=100
            area.spaces.active.shading.color_type='OBJECT'
scene.render.filepath=str(out.with_suffix('.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out));print('SAVED NORMALIZED REFERENCE',out)
