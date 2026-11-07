"""Run inside Blender with --disable-autoexec; bake geometry, never execute .blend scripts.

blender --background --factory-startup --disable-autoexec INPUT --python THIS -- OUTPUT
The output is a REFERENCE, deliberately not a printable or mass-bearing solid.
"""
import base64, hashlib, json, math, struct, sys, zlib
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector

out = Path(sys.argv[sys.argv.index('--') + 1])
dg = bpy.context.evaluated_depsgraph_get()
arm = bpy.data.objects['Armature.001']
wrist = bpy.data.objects['Armature.002']
names = [('Armature.001','Bone'),('Armature.001','Bone.001'),
         ('Armature.001','Bone.003'),('Armature.001','Bone.004'),
         ('Armature.002','Bone.001'),('Armature.002','Bone.002')]
mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
points = [o.matrix_world @ v.co for o in mesh_objects for v in o.evaluated_get(dg).data.vertices]
low = [min(p[i] for p in points) for i in range(3)]
high = [max(p[i] for p in points) for i in range(3)]
base = bpy.data.objects['Cylinder.027']
base_points = [base.matrix_world @ v.co for v in base.evaluated_get(dg).data.vertices]
origin = Vector([(min(v[i] for v in base_points)+max(v[i] for v in base_points))/2 for i in (0,1)]+[low[2]])
scale = 1000/(high[2]-low[2])
pivots, axes = [], []
for i,(a,b) in enumerate(names):
    ob = bpy.data.objects[a]; bone = ob.pose.bones[b]
    pivots.append(list((ob.matrix_world @ bone.head-origin)*scale))
    # Demonstration axes follow the rigid source rig. They are not verified machine axes.
    axis = Vector((0,0,1)) if i == 0 else (ob.matrix_world @ bone.matrix).to_3x3().col[0].normalized()
    axes.append(list(axis))
# Short transverse rig bone locates the upper-arm pivot axis; the elbow axis
# is the normal of the author's circular flange, not an arbitrary bone X axis.
shaft=arm.pose.bones['Bone.002'];axes[2]=list((arm.matrix_world.to_3x3()@(shaft.tail-shaft.head)).normalized())
def disc_axis(name):
    ob=bpy.data.objects[name];pp=np.array([list(ob.matrix_world@v.co) for v in ob.evaluated_get(dg).data.vertices]);values,vectors=np.linalg.eigh(np.cov(pp.T));return list(vectors[:,0])
axes[3]=disc_axis('Circle.015')
pivots.append(list((bpy.data.objects['Empty.009'].matrix_world.translation-origin)*scale))
axes.append(disc_axis('Circle.017'))
owners = {'Cube.010':0,'Cylinder.017':1,'Cylinder.023':2,
          'Circle.015':3,'Cube.011':3,'Cube.012':3,'Circle.016':4,'Cylinder.025':5}
parts = []
for ob in sorted(bpy.context.scene.objects,key=lambda o:o.name):
    if ob.type not in {'MESH','CURVE'}: continue
    ev=ob.evaluated_get(dg); mesh=ev.to_mesh()
    try:
        mesh.calc_loop_triangles()
        if not mesh.loop_triangles: continue
        fixed=ob.name=='Cylinder.027'
        owner=0 if fixed else owners.get(ob.name,3 if ob.type=='CURVE' else 6)
        pivot=Vector(pivots[owner]); vs=[]; inds=[]
        for tri in mesh.loop_triangles:
            pp=[(ob.matrix_world @ mesh.vertices[k].co-origin)*scale-pivot for k in tri.vertices]
            normal=(pp[1]-pp[0]).cross(pp[2]-pp[0])
            if normal.length<1e-10:continue
            normal.normalize()
            for p,loop in zip(pp,tri.loops):
                n=ob.matrix_world.to_3x3().inverted().transposed()@mesh.corner_normals[loop].vector
                if n.length<1e-10:n=normal.copy()
                n.normalize();inds.append(len(vs));vs.append((*p,*n))
        data=struct.pack('<3i',0x344D5043,len(vs),len(inds))
        data+=b''.join(struct.pack('<6f',*v) for v in vs)
        data+=struct.pack('<'+'i'*len(inds),*inds)
        c=zlib.compressobj(9,zlib.DEFLATED,-15); preview=base64.b64encode(c.compress(data)+c.flush()).decode('ascii')
        parts.append(dict(Id=ob.name.replace('.','_'),Name=ob.name,Joint=owner,
                          Fixed=fixed,Preview=preview,Triangles=len(inds)//3,SourceType=ob.type))
    finally:ev.to_mesh_clear()
result=dict(Id='author-manipulator-2026-11',Version=2,Height=1000,
            SourceSHA256=hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),
            Bounds=[(v-origin[i%3])*scale for v in low+high],Pivots=pivots,Axes=axes,
            Tip=list((Vector((-2.86,-.12,1.47))-origin)*scale),Parts=parts,
            Status='REFERENCE_NOT_MANUFACTURING_VALIDATED')
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print('REFERENCE',len(parts),'parts;',sum(p['Triangles'] for p in parts),'triangles; height=1000 mm')
