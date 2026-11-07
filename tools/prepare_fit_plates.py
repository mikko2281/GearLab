"""Generic 3MF layout for P1S fit tests; no G-code and no automatic print start.
Profiles must be selected in Bambu Studio. This is not a sliced project.
"""
import json,struct,zipfile
from pathlib import Path
from xml.etree.ElementTree import Element,SubElement,tostring
ROOT=Path(__file__).resolve().parents[1]
NS='http://schemas.microsoft.com/3dmanufacturing/core/2015/02'
def stl(path):
    data=path.read_bytes();n=struct.unpack_from('<I',data,80)[0];points=[];faces=[];index={}
    for i in range(n):
        raw=struct.unpack_from('<12fH',data,84+50*i);face=[]
        for j in [3,6,9]:
            p=raw[j:j+3]
            if p not in index:index[p]=len(points);points.append(p)
            face.append(index[p])
        faces.append(face)
    return points,faces
def plate(folder,name,files,positions):
    model=Element('model',xmlns=NS,unit='millimeter');SubElement(model,'metadata',name='Title').text=name
    SubElement(model,'metadata',name='Description').text='FIT SPECIMENS ONLY. P1S / PETG / 0.4 mm nozzle assumed. Select actual profiles in Bambu Studio; unsliced.'
    resources=SubElement(model,'resources');build=SubElement(model,'build')
    for number,(file,(x,y)) in enumerate(zip(files,positions),1):
        points,faces=stl(file);lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
        # 5 mm edge margin, plus avoid P1S lower-left 18×28 mm exclusion.
        assert x>=5 and y>=5 and x+hi[0]-lo[0]<=251 and y+hi[1]-lo[1]<=251
        assert not(x<18 and y<28)
        ob=SubElement(resources,'object',id=str(number),type='model',name=file.stem)
        mesh=SubElement(ob,'mesh');vertices=SubElement(mesh,'vertices');triangles=SubElement(mesh,'triangles')
        for p in points:SubElement(vertices,'vertex',**dict(zip(['x','y','z'],[f'{p[i]-lo[i]:.6f}' for i in range(3)])))
        for f in faces:SubElement(triangles,'triangle',v1=str(f[0]),v2=str(f[1]),v3=str(f[2]))
        SubElement(build,'item',objectid=str(number),transform=f'1 0 0 0 1 0 0 0 1 {x} {y} 0')
    path=folder/(name+'.3mf')
    with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED) as z:
        z.writestr('[Content_Types].xml','<?xml version="1.0"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="model" ContentType="application/vnd.ms-package.3dmanufacturing-3dmodel+xml"/></Types>')
        z.writestr('_rels/.rels','<?xml version="1.0"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Target="/3D/3dmodel.model" Id="rel0" Type="http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel"/></Relationships>')
        z.writestr('3D/3dmodel.model',tostring(model,encoding='utf-8',xml_declaration=True))
    with zipfile.ZipFile(path) as z:assert z.testzip() is None
    return path
if __name__=='__main__':
    folder=ROOT/'docs/author-design-65/fit-specimens'
    a=plate(folder,'P1S_PETG_plate1_FIT_6204',sorted(folder.glob('FIT_6204*.stl')),[(24+74*(i%3),38+74*(i//3)) for i in range(6)])
    b=plate(folder,'P1S_PETG_plate2_FIT_6001_and_bolts',sorted(folder.glob('FIT_6001*.stl'))+[folder/'FIT_M3_M5_holes.stl'],[(24+55*(i%3),38+55*(i//3)) for i in range(6)]+[(24,155)])
    print('PASS: two unsliced generic 3MF plates; all parts inside 256 mm bed and outside P1S exclusion:',a.name,b.name)
