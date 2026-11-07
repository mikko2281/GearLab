"""Standalone native CAD fit specimens. NOT manipulator production parts.

python tools/author_design_fitkit.py OUTPUT [--bed 256 256 256]
All clearances are DIAMETRAL; no FDM fit or load rating is asserted.
"""
import argparse, hashlib, json, math, struct, sys
from collections import Counter
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
import cad_kernel as k

def cylinder(d,h,z=0,x=0,y=0):
    return k.BRepPrimAPI_MakeCylinder(k.gp_Ax2(k.gp_Pnt(x,y,z),k.gp_Dir(0,0,1)),d/2,h).Shape()

def cut(a,b):return k.checked(k.BRepAlgoAPI_Cut(a,b).Shape())

def write_stl(shape,path):
    k.BRepMesh_IncrementalMesh(shape,.02,False,.08,True).Perform()
    triangles=[]
    for face in k.shapes(shape,k.TopAbs_FACE):
        face=k.cast(face,k.TopAbs_FACE);loc=k.TopLoc_Location();tri=k.BRep_Tool.Triangulation_s(face,loc)
        if tri is None:continue
        for i in range(1,tri.NbTriangles()+1):
            ids=list(tri.Triangle(i).Get())
            if face.Orientation()==k.TopAbs_REVERSED:ids[1],ids[2]=ids[2],ids[1]
            points=[tri.Node(j).Transformed(loc.Transformation()) for j in ids]
            normal=k.gp_Vec(points[0],points[1]).Crossed(k.gp_Vec(points[0],points[2]))
            if normal.SquareMagnitude()<1e-20:continue
            normal.Normalize();triangles.append(struct.pack('<12fH',*k.xyz(normal),*(v for p in points for v in k.xyz(p)),0))
    path.write_bytes(b'GearLab FIT SPECIMEN ONLY; mm; NOT ROBOT PART'.ljust(80,b'\0')+struct.pack('<I',len(triangles))+b''.join(triangles))

def verify_stl(path):
    data=path.read_bytes();n=struct.unpack_from('<I',data,80)[0]
    assert len(data)==84+50*n and n>0
    edges=Counter();directed=Counter();volume=0;points=[]
    for i in range(n):
        f=struct.unpack_from('<12fH',data,84+50*i);pp=[f[j:j+3] for j in (3,6,9)];points+=pp
        keys=[tuple(round(v,4) for v in p) for p in pp]
        assert len(set(keys))==3,'degenerate STL triangle'
        for a,b in zip(keys,keys[1:]+keys[:1]):edges[tuple(sorted((a,b)))]+=1;directed[(a,b)]+=1
        a,b,c=pp;volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])-a[1]*(b[0]*c[2]-b[2]*c[0])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    assert all(v==2 for v in edges.values()),'open or nonmanifold STL'
    assert all(directed[(a,b)]==directed[(b,a)] for a,b in directed),'inconsistent STL winding'
    assert volume>0 and min(p[2] for p in points)>=-1e-5
    return dict(Triangles=n,Volume=volume,Closed=True,ConsistentWinding=True)

def generate(folder,bed):
    folder=Path(folder)
    if folder.exists() and any(folder.iterdir()):raise ValueError('Choose an empty output folder; preserve previous specimens')
    folder.mkdir(parents=True,exist_ok=True)
    if len(bed)!=3 or any(not math.isfinite(x) or x<80 or x>2000 for x in bed):raise ValueError('Bed dimensions: 80–2000 mm')
    result=[]
    def add(name,shape,dimensions):
        k.checked(shape);assert len(k.shapes(shape,k.TopAbs_SOLID))==1
        box=k.bounds(shape);size=[box[i+3]-box[i] for i in range(3)]
        assert all(s<=b-10 for s,b in zip(size,bed)), 'Specimen exceeds bed with 5 mm margins'
        path=folder/(name+'.step');w=k.STEPControl_Writer();w.Transfer(shape,k.STEPControl_AsIs)
        assert w.Write(str(path))==k.IFSelect_RetDone
        r=k.STEPControl_Reader();assert r.ReadFile(str(path))==k.IFSelect_RetDone;r.TransferRoots();roundtrip=k.checked(r.OneShape())
        volume,centre=k.props(shape);assert abs(k.props(roundtrip)[0]-volume)/volume<1e-7
        stl=folder/(name+'.stl');write_stl(shape,stl);mesh=verify_stl(stl)
        assert abs(mesh['Volume']-volume)/volume<.005
        result.append(dict(Id=name,IntendedUse='FIT_SPECIMEN_NOT_ROBOT_PART',Nominal=dimensions,Bounds=box,Size=size,
                           VolumeMM3=volume,STEPValid=True,STLSolid=mesh,
                           Files={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (path,stl)}))
    for series,d,D,B,ledge,size,boltpos in [('6204',20,47,14,42,67,27),('6001',12,28,8,26,48,19)]:
        for clearance in [0,.1,.2,.3,.4,.5]:
            h=B+2;shape=k.BRepPrimAPI_MakeBox(k.gp_Pnt(-size/2,-size/2,0),size,size,h).Shape()
            shape=cut(shape,cylinder(ledge,h+2,-1));shape=cut(shape,cylinder(D+clearance,B+1,2))
            for x in [-boltpos,boltpos]:
                for y in [-boltpos,boltpos]:shape=cut(shape,cylinder(3.4,h+2,-1,x,y))
            for mark in range(round(clearance*10)):
                notch=k.BRepPrimAPI_MakeBox(k.gp_Pnt(-9+4*mark,size/2-2,-1),1.2,3,h+2).Shape();shape=cut(shape,notch)
            add('FIT_'+series+'_diametral_'+f'{clearance:.2f}'.replace('.','p'),shape,
                dict(Bearing=series,Inner=d,Outer=D,Width=B,HousingBore=D+clearance,DiametralClearance=clearance,SeatDepth=B,ShoulderBore=ledge,MountBore=3.4,NotchCount=round(clearance*10)))
    shape=k.BRepPrimAPI_MakeBox(75,42,6).Shape()
    for row,nominal in enumerate([3,5]):
        for col,gap in enumerate([0,.2,.4,.6]):shape=cut(shape,cylinder(nominal+gap,8,-1,12+17*col,12+18*row))
    shape=cut(shape,k.BRepPrimAPI_MakeBox(k.gp_Pnt(-1,-1,-1),4,4,8).Shape())
    add('FIT_M3_M5_holes',shape,dict(M3=[3,3.2,3.4,3.6],M5=[5,5.2,5.4,5.6],Thickness=6))
    manifest=dict(Version=1,Status='CALIBRATION_ONLY_NOT_A_ROBOT_PRINT_PACK',Units='mm',Printer='Bambu Lab P1S',Material='PETG',NozzleAssumedMM=.4,PrinterBedUserConfirmed=(bed==[256,256,256]),PrinterBedAssumed=bed,ClearanceConvention='diametral',Parts=result)
    (folder/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    (folder/'README.txt').write_text('ПРОВЕРОЧНЫЕ ОБРАЗЦЫ. ЭТО НЕ ДЕТАЛИ ГОТОВОГО МАНИПУЛЯТОРА.\n'
        'STL и STEP — мм, Z=0; 12 образцов посадок и один образец отверстий. Приняты Bambu Lab P1S / PETG; стол 256³ мм; сопло 0,4 мм предполагается, марка PETG не уточнена.\n'
        'Выберите один тип подшипника и несколько зазоров для пробы. Суффикс 0p20 означает увеличение ДИАМЕТРА на 0,20 мм (по 0,10 мм на сторону).\n'
        '6204: 20×47×14 мм; 6001: 12×28×8 мм. Это кандидаты только для проверки печати, выбор опор робота не выполнен.\n'
        'Печатайте при 100% масштабе, отверстиями вверх, тем же материалом и режимом, который планируете для корпуса.\n'
        'Измерьте отверстие после остывания, отметьте номер удачного образца. Не забивайте подшипник молотком; не передавайте усилие через шарики.\n'
        'В manifest.json находятся размеры, CAD/STL-проверки и SHA256. Серия определяется размером блока. Число вырезов у края равно зазору × 10: 0 вырезов = 0,00 мм; 2 = 0,20 мм; 5 = 0,50 мм. На пластинке M3/M5 вырезанный угол — начало координат; отверстия слева направо увеличиваются на 0 / 0,2 / 0,4 / 0,6 мм.\n',encoding='utf-8-sig')
    return manifest

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('output');parser.add_argument('--bed',nargs=3,type=float,default=[256,256,256]);args=parser.parse_args()
    m=generate(args.output,args.bed);print('PASS',len(m['Parts']),'closed native CAD/STL fit specimens; NOT a manipulator production pack')
