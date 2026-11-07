"""GearLab CAD worker. JSON commands, OCCT B-rep operations, no script eval."""
import ast, base64, hashlib, io, json, math, os, re, struct, sys, tempfile, time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from OCP.gp import gp_Pnt, gp_Vec, gp_Dir, gp_Ax1, gp_Ax2, gp_Pln, gp_Trsf, gp_Circ
from OCP.BRep import BRep_Tool, BRep_Builder
from OCP.BRepBuilderAPI import (BRepBuilderAPI_MakePolygon, BRepBuilderAPI_MakeWire,
 BRepBuilderAPI_MakeEdge, BRepBuilderAPI_MakeFace, BRepBuilderAPI_Transform, BRepBuilderAPI_Sewing, BRepBuilderAPI_MakeSolid)
from OCP.BRepPrimAPI import BRepPrimAPI_MakeBox, BRepPrimAPI_MakeCylinder, BRepPrimAPI_MakeSphere, BRepPrimAPI_MakePrism, BRepPrimAPI_MakeRevol
from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut, BRepAlgoAPI_Fuse, BRepAlgoAPI_Common, BRepAlgoAPI_Splitter
from OCP.BRepFilletAPI import BRepFilletAPI_MakeFillet, BRepFilletAPI_MakeChamfer
from OCP.BRepOffsetAPI import BRepOffsetAPI_ThruSections, BRepOffsetAPI_MakePipe, BRepOffsetAPI_MakeThickSolid, BRepOffsetAPI_DraftAngle
from OCP.BRepOffset import BRepOffset_MakeOffset
from OCP.BRepCheck import BRepCheck_Analyzer
from OCP.BRepMesh import BRepMesh_IncrementalMesh
from OCP.BRepAdaptor import BRepAdaptor_Surface, BRepAdaptor_Curve
from OCP.BRepGProp import BRepGProp
from OCP.BRepBndLib import BRepBndLib
from OCP.BRepTools import BRepTools
from OCP.BRepExtrema import BRepExtrema_DistShapeShape
from OCP.Bnd import Bnd_Box
from OCP.GProp import GProp_GProps
from OCP.TopAbs import TopAbs_FACE, TopAbs_EDGE, TopAbs_SOLID, TopAbs_REVERSED
from OCP.TopExp import TopExp
from OCP.collections import IndexedMap_TopoDS_Shape_TopTools_ShapeMapHasher as TopTools_IndexedMapOfShape, List_TopoDS_Shape as TopTools_ListOfShape
from OCP.TopoDS import TopoDS, TopoDS_Compound
from OCP.TopLoc import TopLoc_Location
from OCP.GeomAbs import GeomAbs_Plane, GeomAbs_Cylinder
from OCP.GC import GC_MakeArcOfCircle
from OCP.GeomAPI import GeomAPI_PointsToBSpline, GeomAPI_Interpolate
from OCP.collections import Array1_gp_Pnt as TColgp_Array1OfPnt, HArray1_gp_Pnt
from OCP.STEPControl import STEPControl_Reader, STEPControl_Writer, STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
from OCP.StlAPI import StlAPI_Writer
from sketch_solver import solve as solve_sketch

VERSION = '4.0'
CACHE = {}
GEOMETRY_CACHE = {}
TAU = math.tau

def xyz(p): return [p.X(), p.Y(), p.Z()]
def shapes(s, kind):
    m = TopTools_IndexedMapOfShape(); TopExp.MapShapes_s(s, kind, m)
    return [m.FindKey(i) for i in range(1, m.Extent()+1)]
def cast(s, kind): return getattr(TopoDS, 'Face' if kind==TopAbs_FACE else 'Edge' if kind==TopAbs_EDGE else 'Solid')(s)
def bounds(s):
    b=Bnd_Box();BRepBndLib.AddOptimal_s(s,b,False,False)
    return xyz(b.CornerMin())+xyz(b.CornerMax())
def props(s, surface=False):
    p=GProp_GProps()
    if surface:BRepGProp.SurfaceProperties_s(s,p)
    else:BRepGProp.VolumeProperties_s(s,p,1e-11)
    return p.Mass(),xyz(p.CentreOfMass())
def checked(s):
    if s.IsNull() or not shapes(s,TopAbs_SOLID) or not BRepCheck_Analyzer(s).IsValid():
        raise ValueError('Операция не создала корректное твёрдое тело. Проверьте размеры, пересечения и выбранные элементы.')
    if props(s)[0] < 1e-8: raise ValueError('Нулевой объём тела.')
    return s
def compound(items):
    c=TopoDS_Compound();b=BRep_Builder();b.MakeCompound(c)
    for s in items:b.Add(c,s)
    return c

class Parameters:
    def __init__(self, items):self.raw=items or {};self.done={};self.busy=set()
    def value(self, expression):
        if isinstance(expression,(int,float)):
            v=float(expression)
        else:v=self.node(ast.parse(str(expression or '0'),mode='eval').body)
        if not math.isfinite(v) or abs(v)>1e9:raise ValueError('Число вне допустимого диапазона.')
        return v
    def name(self,n):
        if n in {'pi','π'}:return math.pi
        if n in self.done:return self.done[n]
        if n not in self.raw:raise ValueError('Неизвестный параметр: '+n)
        if n in self.busy:raise ValueError('Цикл в формулах параметров: '+n)
        self.busy.add(n);v=self.value(self.raw[n]);self.busy.remove(n);self.done[n]=v;return v
    def node(self,n):
        if isinstance(n,ast.Constant) and isinstance(n.value,(int,float)):return float(n.value)
        if isinstance(n,ast.Name):return self.name(n.id)
        if isinstance(n,ast.UnaryOp) and isinstance(n.op,(ast.UAdd,ast.USub)):return self.node(n.operand)*(1 if isinstance(n.op,ast.UAdd) else -1)
        if isinstance(n,ast.BinOp):
            a,b=self.node(n.left),self.node(n.right)
            if isinstance(n.op,ast.Add):return a+b
            if isinstance(n.op,ast.Sub):return a-b
            if isinstance(n.op,ast.Mult):return a*b
            if isinstance(n.op,ast.Div):return a/b
            if isinstance(n.op,ast.Pow) and abs(b)<=8:return a**b
        if isinstance(n,ast.Call) and isinstance(n.func,ast.Name) and len(n.args)==1:
            f={'sqrt':math.sqrt,'sin':lambda x:math.sin(math.radians(x)),'cos':lambda x:math.cos(math.radians(x)),'tan':lambda x:math.tan(math.radians(x)),'abs':abs}.get(n.func.id)
            if f:return f(self.node(n.args[0]))
        raise ValueError('В формулах доступны числа, имена, + − × /, степень до 8, sqrt/sin/cos/tan/abs.')

def frame(sketch, bodies):
    origin=list(sketch.get('Origin') or [0,0,0]);u=[1,0,0];v=[0,1,0];n=[0,0,1]
    plane=sketch.get('Plane','XY')
    if plane=='XZ':v=[0,0,1];n=[0,-1,0]
    elif plane=='YZ':u=[0,1,0];v=[0,0,1];n=[1,0,0]
    if sketch.get('FaceBody'):
        s=bodies[sketch['FaceBody']];f=face(s,sketch.get('Face',0));a=BRepAdaptor_Surface(f)
        if a.GetType()!=GeomAbs_Plane:raise ValueError('Для эскиза выберите плоскую грань.')
        p=a.Plane();n=xyz(p.Axis().Direction())
        if f.Orientation()==TopAbs_REVERSED:n=[-x for x in n]
        u=xyz(p.XAxis().Direction());nv=gp_Vec(*n).Crossed(gp_Vec(*u));v=xyz(nv)
        # Keep the clicked position projected onto the current face after a rebuild.
        base=xyz(p.Location());d=sum((origin[i]-base[i])*n[i] for i in range(3))
        origin=[origin[i]-d*n[i] for i in range(3)]
    return origin,u,v,n
def place2(p, fr):
    o,u,v,_=fr;return gp_Pnt(*(o[i]+float(p[0])*u[i]+float(p[1])*v[i] for i in range(3)))
def polygon(points, close=True):
    b=BRepBuilderAPI_MakePolygon()
    for p in points:b.Add(p if isinstance(p,gp_Pnt) else gp_Pnt(*p))
    if close:b.Close()
    return b.Wire()

def entity_wire(e, fr, ev):
    p=e.get('Points') or [];kind=e['Kind'];r=ev(e.get('Radius',10));w=ev(e.get('Width',40));h=ev(e.get('Height',30))
    center=p[0] if p else [0,0]
    if kind=='circle':
        if r<=0:raise ValueError('Радиус должен быть положительным.')
        return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(gp_Circ(gp_Ax2(place2(center,fr),gp_Dir(*fr[3])),r)).Edge()).Wire(),True
    if kind=='rectangle':
        x,y=center;p=[[x,y],[x+w,y],[x+w,y+h],[x,y+h]]
    elif kind=='polygon':
        count=int(ev(e.get('Count',6)))
        if not 3<=count<=128:raise ValueError('3–128 сторон многоугольника.')
        p=[[center[0]+r*math.cos(TAU*i/count),center[1]+r*math.sin(TAU*i/count)] for i in range(count)]
    elif kind=='slot':
        if w<2*r:raise ValueError('Длина паза должна быть не меньше диаметра.')
        x,y=center;span=w-2*r;p=[]
        for i in range(25):a=-math.pi/2+math.pi*i/24;p.append([x+span+r*math.cos(a),y+r*math.sin(a)])
        for i in range(25):a=math.pi/2+math.pi*i/24;p.append([x+r*math.cos(a),y+r*math.sin(a)])
    if kind=='arc':
        if len(p)!=3:raise ValueError('Дуга задаётся тремя точками.')
        return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(*(place2(q,fr) for q in p)).Value()).Edge()).Wire(),False
    if kind=='spline':
        if len(p)<3:raise ValueError('Нужно минимум три точки сплайна.')
        arr=TColgp_Array1OfPnt(1,len(p))
        for i,q in enumerate(p):arr.SetValue(i+1,place2(q,fr))
        return BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(GeomAPI_PointsToBSpline(arr).Curve()).Edge()).Wire(),False
    close=kind in {'rectangle','polygon','slot'} or e.get('Closed',False)
    if len(p)<(3 if close else 2):raise ValueError('Недостаточно точек эскиза.')
    return polygon([place2(q,fr) for q in p],close),close

def sketch_geometry(sketch,bodies,ev):
    fr=frame(sketch,bodies);wires=[];open_edges=[]
    for e in sketch.get('Entities',[]):
        if e.get('Construction'):continue
        wire,closed=entity_wire(e,fr,ev)
        if closed:wires.append(wire)
        else:open_edges.extend(shapes(wire,TopAbs_EDGE))
    if open_edges:
        builder=BRepBuilderAPI_MakeWire()
        # GUI polyline chains are ordered; OCCT checks connection and closure.
        for edge in open_edges:builder.Add(cast(edge,TopAbs_EDGE))
        if not builder.IsDone():raise ValueError('Линии эскиза не соединены.')
        wire=builder.Wire()
        if wire.Closed():wires.append(wire)
        elif not wires:return [],wire,fr
        else:raise ValueError('В эскизе есть незамкнутая цепочка.')
    faces=[]
    # Nesting uses exact OCCT classification, retaining circles as analytic curves.
    from OCP.BRepClass import BRepClass_FaceClassifier
    from OCP.TopAbs import TopAbs_IN
    planar=[BRepBuilderAPI_MakeFace(w).Face() for w in wires]
    areas=[props(f,True)[0] for f in planar]
    parent={}
    from OCP.BRepTools import BRepTools_WireExplorer
    from OCP.gp import gp_Pnt2d
    for i,w in enumerate(wires):
        ex=BRepTools_WireExplorer(w);pt=BRep_Tool.Pnt_s(ex.CurrentVertex())
        candidates=[j for j,f in enumerate(planar) if areas[j]>areas[i]+1e-6 and BRepClass_FaceClassifier(f,pt,1e-7).State()==TopAbs_IN]
        parent[i]=min(candidates,key=lambda j:areas[j]) if candidates else None
    def depth(i):return 0 if parent[i] is None else depth(parent[i])+1
    for i,w in enumerate(wires):
        if depth(i)%2:continue
        build=BRepBuilderAPI_MakeFace(w)
        for k,owner in parent.items():
            if owner==i:
                hole=wires[k];hole.Reverse();build.Add(hole)
        f=build.Face()
        if not BRepCheck_Analyzer(f).IsValid():raise ValueError('Контуры эскиза пересекаются или вырождены.')
        faces.append(f)
    return faces,None,fr

def face(s,index):
    fs=shapes(s,TopAbs_FACE)
    if not 0<=int(index)<len(fs):raise ValueError('Грань изменилась. Выберите её заново.')
    return cast(fs[int(index)],TopAbs_FACE)
def face_normal(f):
    a=BRepAdaptor_Surface(f)
    if a.GetType()!=GeomAbs_Plane:raise ValueError('Эта операция требует плоскую грань.')
    n=xyz(a.Plane().Axis().Direction())
    return [-v for v in n] if f.Orientation()==TopAbs_REVERSED else n
def boolean(a,b,kind,glue=False):
    op={'cut':BRepAlgoAPI_Cut,'join':BRepAlgoAPI_Fuse,'intersect':BRepAlgoAPI_Common}[kind](a,b)
    if glue:
        from OCP.BOPAlgo import BOPAlgo_GlueShift
        op.SetGlue(BOPAlgo_GlueShift)
    op.SetFuzzyValue(1e-7);op.Build()
    if not op.IsDone():raise ValueError('Не удалось выполнить булеву операцию.')
    return checked(op.Shape())
def moved(s,values,ev):
    result=s
    for name,axis in [('RX',(1,0,0)),('RY',(0,1,0)),('RZ',(0,0,1))]:
        angle=ev(values.get(name,0))
        if angle:
            t=gp_Trsf();t.SetRotation(gp_Ax1(gp_Pnt(),gp_Dir(*axis)),math.radians(angle));result=BRepBuilderAPI_Transform(result,t,True).Shape()
    t=gp_Trsf();t.SetTranslation(gp_Vec(*(ev(values.get(k,0)) for k in ['X','Y','Z'])))
    return BRepBuilderAPI_Transform(result,t,True).Shape()

def join_halves(a,b,mid):
    # The two known sections meet on one identical planar profile. Sewing their
    # outer faces avoids an expensive general Boolean intersection of every tooth.
    sewing=BRepBuilderAPI_Sewing(1e-5)
    for s in [a,b]:
        for raw in shapes(s,TopAbs_FACE):
            f=cast(raw,TopAbs_FACE);bb=bounds(f)
            if abs(bb[2]-mid)<1e-6 and abs(bb[5]-mid)<1e-6:continue
            sewing.Add(f)
    sewing.Perform();shell=TopoDS.Shell(sewing.SewedShape());solid=BRepBuilderAPI_MakeSolid(shell).Solid()
    from OCP.BRepLib import BRepLib
    BRepLib.OrientClosedSolid_s(solid)
    return checked(solid)

def involute_points(z,m,angle=20,backlash=0,internal=False):
    pitch=z*m/2;alpha=math.radians(angle);base=pitch*math.cos(alpha)
    root=pitch-(1 if internal else 1.25)*m;tip=pitch+(1.25 if internal else 1)*m
    def inv(rad):
        if rad<=base:return 0
        t=math.sqrt((rad/base)**2-1);return t-math.atan(t)
    half=math.pi/(2*z)-backlash/(2*pitch)
    if half<=0:raise ValueError('Зазор превышает толщину зуба.')
    offset=half+inv(pitch)
    out=[]
    for tooth in range(z):
        centre=TAU*tooth/z
        flank=[]
        for i in range(9):
            rr=root+(tip-root)*i/8
            a=offset-inv(rr);flank.append((rr,a))
        # Internal cutout uses reflected tooth/space width at the pitch circle.
        if internal:flank=[(rr, math.pi/z-a) for rr,a in flank]
        for rr,a in flank:out.append([rr*math.cos(centre-a),rr*math.sin(centre-a)])
        aa=flank[-1][1]
        for k in range(1,5):
            a=centre-aa+2*aa*k/4;out.append([tip*math.cos(a),tip*math.sin(a)])
        for rr,a in reversed(flank[:-1]):out.append([rr*math.cos(centre+a),rr*math.sin(centre+a)])
        start=centre+flank[0][1];end=centre+TAU/z-flank[0][1]
        for k in range(1,4):
            a=start+(end-start)*k/4;out.append([root*math.cos(a),root*math.sin(a)])
    return out

def involute_wire(z,m,angle,backlash,internal,height,rotation):
    pitch=z*m/2;base=pitch*math.cos(math.radians(angle));root=pitch-(1 if internal else 1.25)*m;tip=pitch+(1.25 if internal else 1)*m
    def inv(r):
        t=math.sqrt(max(0,(r/base)**2-1));return t-math.atan(t)
    half=math.pi/(2*z)-backlash/(2*pitch)
    if half<=0:raise ValueError('Зазор превышает толщину зуба.')
    offset=half+inv(pitch)
    def a(r):return math.pi/z-(offset-inv(r)) if internal else offset-inv(r)
    def p(r,angle):return gp_Pnt(r*math.cos(angle+rotation),r*math.sin(angle+rotation),height)
    def spline(points):
        arr=HArray1_gp_Pnt(1,len(points))
        for i,point in enumerate(points):arr.SetValue(i+1,point)
        curve=GeomAPI_Interpolate(arr,False,1e-8);curve.Perform();return BRepBuilderAPI_MakeEdge(curve.Curve()).Edge()
    builder=BRepBuilderAPI_MakeWire()
    for tooth in range(z):
        c=TAU*tooth/z;start=max(root,base)
        if root<base-1e-8:builder.Add(BRepBuilderAPI_MakeEdge(p(root,c-a(root)),p(base,c-a(base))).Edge())
        radii=[start+(tip-start)*i/12 for i in range(13)]
        builder.Add(spline([p(r,c-a(r)) for r in radii]))
        builder.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(p(tip,c-a(tip)),p(tip,c),p(tip,c+a(tip))).Value()).Edge())
        builder.Add(spline([p(r,c+a(r)) for r in reversed(radii)]))
        if root<base-1e-8:builder.Add(BRepBuilderAPI_MakeEdge(p(base,c+a(base)),p(root,c+a(root))).Edge())
        end=c+TAU/z-a(root);begin=c+a(root)
        builder.Add(BRepBuilderAPI_MakeEdge(GC_MakeArcOfCircle(p(root,begin),p(root,(begin+end)/2),p(root,end)).Value()).Edge())
    if not builder.IsDone():raise ValueError('Не удалось построить эвольвентный контур.')
    return builder.Wire()

def gear(values,ev):
    kind=values.get('Type','spur');z=int(ev(values.get('Teeth',24)));m=ev(values.get('Module',1));th=ev(values.get('Thickness',10));bore=ev(values.get('Bore',5));pressure=ev(values.get('Pressure',20));backlash=ev(values.get('Backlash',.05));helix=ev(values.get('Helix',20))
    if not 8<=z<=(242 if values.get('_Wave') else 160) or not (.15 if values.get('_Wave') else .2)<=m<=10 or not .5<=th<=200 or not 14<=pressure<=35 or not 0<=bore<m*(z-2.5):raise ValueError('Некорректные размеры шестерни: 8–160 зубьев, модуль 0,2–10, толщина 0,5–200, угол 14–35°; отверстие внутри впадин.')
    if kind=='rack':
        pitch=math.pi*m;height=ev(values.get('Base',8));half=pitch/4-backlash/2
        if height<=1.25*m:raise ValueError('Высота основания рейки должна превышать 1,25 модуля.')
        outline=[[0,-height],[z*pitch,-height],[z*pitch,-1.25*m]]
        for i in range(z-1,-1,-1):
            c=(i+.5)*pitch
            for y,side in [(-1.25*m,1),(m,1),(m,-1),(-1.25*m,-1)]:outline.append([c+side*(half-y*math.tan(math.radians(pressure))),y])
        outline.append([0,-1.25*m])
        return checked(BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(polygon([[x,y,0] for x,y in outline])).Face(),gp_Vec(0,0,th)).Shape())
    def wire_at(height,rotation):
        return involute_wire(z,m,pressure,backlash,kind=='internal',height,rotation)
    if kind in {'helical','herringbone'}:
        if not 1<=abs(helix)<=40:raise ValueError('Угол наклона зуба 1–40°.')
        twist=th*math.tan(math.radians(helix))/(z*m/2)
        if abs(twist)>math.pi:raise ValueError('Для этой ширины угол закручивания превышает 180°. Уменьшите ширину или наклон зубьев.')
        steps=max(4,min(16,int(abs(twist)/.15)+1))
        def segment(z0,z1,r0,r1):
            loft=BRepOffsetAPI_ThruSections(True,False,1e-6)
            for i in range(steps+1):t=i/steps;loft.AddWire(wire_at(z0+(z1-z0)*t,r0+(r1-r0)*t))
            loft.CheckCompatibility(False);loft.Build();return checked(loft.Shape())
        s=segment(0,th,0,twist) if kind=='helical' else join_halves(segment(0,th/2,0,twist/2),segment(th/2,th,twist/2,0),th/2)
    else:s=BRepPrimAPI_MakePrism(BRepBuilderAPI_MakeFace(wire_at(0,0)).Face(),gp_Vec(0,0,th)).Shape()
    if kind=='internal':
        outer=ev(values.get('Outer',m*(z+8)))
        if outer<=m*(z+2.5)+2:raise ValueError('Наружный диаметр кольца слишком мал.')
        s=boolean(BRepPrimAPI_MakeCylinder(outer/2,th).Shape(),s,'cut')
    elif bore>0:s=boolean(s,BRepPrimAPI_MakeCylinder(bore/2,th).Shape(),'cut')
    return checked(s)

def import_step(data):
    with tempfile.TemporaryDirectory(prefix='GearLab-step-') as folder:
        p=Path(folder)/'part.step';p.write_bytes(base64.b64decode(data,validate=True));reader=STEPControl_Reader()
        if reader.ReadFile(str(p))!=IFSelect_RetDone:raise ValueError('Не удалось прочитать STEP.')
        reader.TransferRoots();return checked(reader.OneShape())

def execute_feature(f,bodies,sketches,ev):
    kind=f['Kind'];v=f.get('Values') or {};target=f.get('Target');s=bodies.get(target);out=None;operation=f.get('Operation','new')
    value=lambda name,default=0:ev(v.get(name,default))
    geometry_key=None
    if kind in {'box','cylinder','sphere','gear','import'}:
        evaluated={}
        for key,expression in v.items():
            try:evaluated[key]=ev(expression)
            except Exception:evaluated[key]=expression
        geometry_key=hashlib.sha256(json.dumps([kind,evaluated,f.get('Data')],sort_keys=True).encode()).hexdigest()
    if kind=='wave':
        from wave_kernel import make_wave
        for suffix,part in make_wave(v,ev,sys.modules[__name__]).items():bodies[f['Id']+'_'+suffix]=part
        return
    if geometry_key in GEOMETRY_CACHE:out=GEOMETRY_CACHE[geometry_key]
    elif kind=='sketch':sketches[f['Id']]=solve_sketch(f['Sketch'],ev)[0];return
    if kind=='import':out=import_step(f['Data'])
    elif kind=='box':out=BRepPrimAPI_MakeBox(value('Width',40),value('Depth',30),value('Height',20)).Shape()
    elif kind=='cylinder':out=BRepPrimAPI_MakeCylinder(value('Radius',15),value('Height',30)).Shape()
    elif kind=='sphere':out=BRepPrimAPI_MakeSphere(value('Radius',15)).Shape()
    elif kind=='gear':out=gear(v,ev)
    elif kind in {'extrude','revolve','loft','sweep'}:
        ids=f.get('Sketches') or [];profiles=[sketch_geometry(sketches[k],bodies,ev) for k in ids]
        if not profiles:raise ValueError('Выберите эскиз.')
        faces,openwire,fr=profiles[0]
        if kind in {'extrude','revolve'}:
            if not faces:raise ValueError('Нужен замкнутый профиль.')
            solids=[]
            for ff in faces:
                if kind=='extrude':solids.append(BRepPrimAPI_MakePrism(ff,gp_Vec(*(q*value('Distance',20) for q in fr[3]))).Shape())
                else:solids.append(BRepPrimAPI_MakeRevol(ff,gp_Ax1(place2([value('AxisX'),0],fr),gp_Dir(*fr[2])),math.radians(value('Angle',360))).Shape())
            out=compound(solids) if len(solids)>1 else solids[0]
        elif kind=='loft':
            if len(profiles)<2:raise ValueError('Для лофта выберите минимум два эскиза.')
            loft=BRepOffsetAPI_ThruSections(True,False,1e-6)
            from OCP.BRepTools import BRepTools
            for faces,_,_ in profiles:
                if len(faces)!=1:raise ValueError('Каждое сечение лофта должно содержать один профиль.')
                from OCP.TopAbs import TopAbs_WIRE
                if len(shapes(faces[0],TopAbs_WIRE))!=1:raise ValueError('Лофт с внутренними контурами пока не поддерживается. Выполните отверстие отдельной операцией после лофта.')
                loft.AddWire(BRepTools.OuterWire_s(faces[0]))
            loft.Build();out=loft.Shape()
        else:
            if len(profiles)!=2 or not profiles[1][1] or len(faces)!=1:raise ValueError('Для протяжки нужен один замкнутый профиль и один открытый путь.')
            out=BRepOffsetAPI_MakePipe(profiles[1][1],faces[0]).Shape()
    elif kind in {'fillet','chamfer'}:
        if s is None:raise ValueError('Выберите тело.')
        builder=(BRepFilletAPI_MakeFillet if kind=='fillet' else BRepFilletAPI_MakeChamfer)(s)
        edges=shapes(s,TopAbs_EDGE);ids=f.get('Edges') or []
        if not ids:raise ValueError('Выберите рёбра.')
        for i in ids:builder.Add(value('Radius',2),cast(edges[int(i)],TopAbs_EDGE))
        builder.Build();out=builder.Shape();operation='replace'
    elif kind in {'hole','presspull','offset','shell','draft'}:
        if s is None:raise ValueError('Выберите тело.')
        ids=f.get('Faces') or [int(value('Face',0))];ff=face(s,ids[0])
        if kind=='hole':
            n=face_normal(ff);point=f.get('Point') or props(ff,True)[1];depth=value('Depth',20)
            base=xyz(BRepAdaptor_Surface(ff).Plane().Location());shift=sum((point[i]-base[i])*n[i] for i in range(3));point=[point[i]-shift*n[i] for i in range(3)]
            if v.get('Through')=='true':b=bounds(s);depth=math.dist(b[:3],b[3:])*2+1
            start=gp_Pnt(*(point[i]+n[i]*.01 for i in range(3)))
            out=boolean(s,BRepPrimAPI_MakeCylinder(gp_Ax2(start,gp_Dir(*[-q for q in n])),value('Diameter',5)/2,depth+.02).Shape(),'cut')
        elif kind in {'presspull','offset'}:
            d=value('Distance',5);adaptor=BRepAdaptor_Surface(ff)
            if adaptor.GetType()==GeomAbs_Plane:
                n=face_normal(ff);tool=BRepPrimAPI_MakePrism(ff,gp_Vec(*(q*d for q in n))).Shape();out=boolean(s,tool,'join' if d>0 else 'cut')
            elif adaptor.GetType()==GeomAbs_Cylinder:
                if adaptor.LastUParameter()-adaptor.FirstUParameter()<TAU-1e-5:raise ValueError('Радиальное смещение поддерживает полную цилиндрическую грань. Для сектора измените исходный эскиз.')
                cylinder=adaptor.Cylinder();inward=ff.Orientation()==TopAbs_REVERSED;r0=cylinder.Radius();r1=r0+(-d if inward else d)
                if r1<=.01:raise ValueError('Радиус после смещения должен оставаться положительным.')
                lo,hi=adaptor.FirstVParameter(),adaptor.LastVParameter();axis=cylinder.Axis();point=cylinder.Location().Translated(gp_Vec(axis.Direction())*lo);ax=gp_Ax2(point,axis.Direction())
                ring=boolean(BRepPrimAPI_MakeCylinder(ax,max(r0,r1),hi-lo).Shape(),BRepPrimAPI_MakeCylinder(ax,min(r0,r1),hi-lo).Shape(),'cut')
                adding=(r1>r0)!=inward;out=boolean(s,ring,'join' if adding else 'cut')
            else:raise ValueError('Нажать/вытянуть поддерживает плоские и цилиндрические грани. Для других поверхностей измените исходную операцию.')
        elif kind=='shell':
            close=TopTools_ListOfShape()
            for i in ids:close.Append(face(s,i))
            maker=BRepOffsetAPI_MakeThickSolid();maker.MakeThickSolidByJoin(s,close,-value('Thickness',2),1e-4);out=maker.Shape()
        else:
            maker=BRepOffsetAPI_DraftAngle(s)
            for i in ids:maker.Add(face(s,i),gp_Dir(0,0,1),math.radians(value('Angle',3)),gp_Pln(gp_Pnt(0,0,value('NeutralZ')),gp_Dir(0,0,1)))
            maker.Build();out=maker.Shape()
        operation='replace'
    elif kind=='combine':
        out=s
        for key in f.get('Tools') or []:out=boolean(out,bodies[key],v.get('Mode','join'))
        if v.get('KeepTools')!='true':
            for key in f.get('Tools') or []:bodies.pop(key,None)
        operation='replace'
    elif kind=='transform':out=s;operation='replace'
    elif kind in {'move','scale','mirror','linear_pattern','circular_pattern'}:
        if s is None:raise ValueError('Выберите тело.')
        if kind=='move':out=moved(s,v,ev);operation='new' if v.get('Copy')=='true' else 'replace'
        elif kind in {'scale','mirror'}:
            tr=gp_Trsf()
            if kind=='scale':tr.SetScale(gp_Pnt(value('X'),value('Y'),value('Z')),value('Factor',1.1))
            else:tr.SetMirror(gp_Ax2(gp_Pnt(value('X'),value('Y'),value('Z')),gp_Dir(*( {'XY':(0,0,1),'XZ':(0,1,0),'YZ':(1,0,0)}[v.get('Plane','YZ')]))))
            out=BRepBuilderAPI_Transform(s,tr,True).Shape();operation='new' if v.get('Copy')=='true' else 'replace'
        else:
            count=int(value('Count',4))
            if not 2<=count<=64:raise ValueError('Количество элементов массива 2–64.')
            for i in range(1,count):
                tr=gp_Trsf()
                if kind=='linear_pattern':tr.SetTranslation(gp_Vec(value('X',30)*i,value('Y')*i,value('Z')*i))
                else:tr.SetRotation(gp_Ax1(gp_Pnt(value('X'),value('Y'),value('Z')),gp_Dir(0,0,1)),math.radians(value('Angle',360))*i/count)
                bodies[f['Id']+'_'+str(i)]=checked(BRepBuilderAPI_Transform(s,tr,True).Shape())
            return
    elif kind=='split':
        fr=frame({'Plane':v.get('Plane','XY'),'Origin':[value('X'),value('Y'),value('Z')]},bodies)
        plane=gp_Pln(gp_Pnt(*fr[0]),gp_Dir(*fr[3]));tool=BRepBuilderAPI_MakeFace(plane,-100000,100000,-100000,100000).Face()
        args=TopTools_ListOfShape();args.Append(s);tools=TopTools_ListOfShape();tools.Append(tool);split=BRepAlgoAPI_Splitter();split.SetArguments(args);split.SetTools(tools);split.Build()
        pieces=shapes(split.Shape(),TopAbs_SOLID)
        if len(pieces)<2:raise ValueError('Плоскость не разделяет тело.')
        bodies.pop(target)
        for i,piece in enumerate(pieces):bodies[f['Id']+'_'+str(i)]=checked(piece)
        return
    else:raise ValueError('Неизвестная CAD-операция: '+kind)
    if out is None:raise ValueError('Операция не дала результата.')
    if geometry_key and geometry_key not in GEOMETRY_CACHE:
        GEOMETRY_CACHE[geometry_key]=checked(out)
        if len(GEOMETRY_CACHE)>48:GEOMETRY_CACHE.pop(next(iter(GEOMETRY_CACHE)))
    if f.get('Transform'):
        tr=gp_Trsf();tr.SetValues(*f['Transform']);out=BRepBuilderAPI_Transform(out,tr,True).Shape()
    out=checked(out)
    if operation in {'join','cut','intersect'}:
        if s is None:raise ValueError('Для операции выберите целевое тело.')
        out=boolean(s,out,operation);bodies[target]=out
    elif operation=='replace':bodies[target]=out
    else:bodies[f['Id']]=out

def surface_info(f,i):
    area,center=props(f,True);a=BRepAdaptor_Surface(f);kind='surface';normal=[];radius=0
    if a.GetType()==GeomAbs_Plane:kind='plane';normal=face_normal(f)
    elif a.GetType()==GeomAbs_Cylinder:kind='cylinder';radius=a.Cylinder().Radius()
    return dict(Id=i,Kind=kind,Center=center,Normal=normal,Area=area,Radius=radius)
def mesh_and_topology(s,path,quality=.15):
    BRepMesh_IncrementalMesh(s,quality,False,.18,False).Perform();points=[];normals=[];face_ids=[];info=[]
    for fi,fs in enumerate(shapes(s,TopAbs_FACE)):
        f=cast(fs,TopAbs_FACE);info.append(surface_info(f,fi));loc=TopLoc_Location();tri=BRep_Tool.Triangulation_s(f,loc)
        if tri is None:continue
        tr=loc.Transformation()
        for i in range(1,tri.NbTriangles()+1):
            indices=list(tri.Triangle(i).Get())
            if f.Orientation()==TopAbs_REVERSED:indices[1],indices[2]=indices[2],indices[1]
            pp=[tri.Node(j).Transformed(tr) for j in indices];n=gp_Vec(pp[0],pp[1]).Crossed(gp_Vec(pp[0],pp[2]))
            if n.SquareMagnitude()<1e-20:continue
            n.Normalize()
            # Smooth analytic surface normals from parameter-space derivatives.
            for node,p in zip(indices,pp):
                nn=n
                try:
                    uv=tri.UVNode(node);du,dv=gp_Vec(),gp_Vec();q=gp_Pnt();surf=BRepAdaptor_Surface(f);surf.D1(uv.X(),uv.Y(),q,du,dv);nn=du.Crossed(dv);nn.Normalize()
                    if f.Orientation()==TopAbs_REVERSED:nn.Reverse()
                except Exception:pass
                points.append(xyz(p));normals.append(xyz(nn))
            face_ids.append(fi)
    with open(path,'wb') as w:
        w.write(struct.pack('<ii',0x34444143,len(face_ids)))
        for ti,fi in enumerate(face_ids):
            w.write(struct.pack('<i',fi))
            for k in range(3):w.write(struct.pack('<6f',*points[3*ti+k],*normals[3*ti+k]))
    edges=[]
    for i,edge in enumerate(shapes(s,TopAbs_EDGE)):
        edge=cast(edge,TopAbs_EDGE);a=BRepAdaptor_Curve(edge);lo,hi=a.FirstParameter(),a.LastParameter();p=GProp_GProps();BRepGProp.LinearProperties_s(edge,p)
        count=2 if str(a.GetType()).endswith('GeomAbs_Line') else 25
        samples=[xyz(a.Value(lo+(hi-lo)*j/(count-1))) for j in range(count)]
        edges.append(dict(Id=i,Length=p.Mass(),Points=samples))
    return info,edges,len(face_ids)

def build_document(doc,folder,marker=None,display=True):
    if doc.get('FormatVersion',1)!=1:raise ValueError('Неизвестная версия CAD-проекта.')
    features=doc.get('Features') or []
    if len(features)>300:raise ValueError('Максимум 300 операций в CAD-проекте.')
    params=Parameters(doc.get('Parameters'));ev=params.value
    for key in params.raw:params.name(key)
    bodies={};sketches={};prefix=hashlib.sha256(json.dumps(params.raw,sort_keys=True).encode()).hexdigest();history=[];blocked=[]
    for index,f in enumerate(features):
        if not re.fullmatch(r'[A-Za-z0-9_]{1,64}',f.get('Id','')):raise ValueError('Некорректный идентификатор операции.')
        if marker is not None and index>=marker:break
        prefix=hashlib.sha256((prefix+json.dumps(f,sort_keys=True)).encode()).hexdigest()
        refs=[f.get('Target')]+(f.get('Sketches') or [])+(f.get('Tools') or [])
        dependent=any(ref and any(ref==key or ref.startswith(key+'_') for key in blocked) for ref in refs)
        if f.get('Suppressed') or dependent:
            blocked.append(f['Id']);history.append(dict(Id=f['Id'],State='suppressed' if f.get('Suppressed') else 'dependent_suppressed'));continue
        try:
            if prefix in CACHE:bodies,sketches=(dict(x) for x in CACHE[prefix])
            else:
                execute_feature(f,bodies,sketches,ev);CACHE[prefix]=(dict(bodies),dict(sketches))
                if len(CACHE)>80:CACHE.pop(next(iter(CACHE)))
            history.append(dict(Id=f['Id'],State='ok'))
        except Exception as ex:raise ValueError('Операция '+str(index+1)+' «'+f.get('Name',f['Kind'])+'»: '+str(ex)) from ex
    result=[];folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    for key,s in bodies.items():
        mesh=str(folder/(key+'.cadmesh'));faces,edges,triangles=mesh_and_topology(s,mesh) if display else ([],[],0)
        vol,center=props(s);result.append(dict(Id=key,Volume=vol,Center=center,Bounds=bounds(s),Faces=faces,Edges=edges,Mesh=mesh,Triangles=triangles))
    frames={}
    for key,sketch in sketches.items():
        try:frames[key]=frame(sketch,bodies)
        except (KeyError,IndexError,ValueError):pass
    return bodies,dict(Ok=True,Bodies=result,History=history,Parameters=params.done,SketchFrames=frames)

def handle(request):
    mode=request.get('Mode','build');folder=request.get('Folder') or tempfile.mkdtemp(prefix='GearLab-CAD-')
    if mode=='assembly_contacts':
        from assembly_contacts import check
        return check(request['Spec'],sys.modules[__name__])
    if mode=='body_motion':
        from body_motion import check
        return check(request['Spec'],sys.modules[__name__])
    if mode in {'assembly_parametric','assembly_audit'}:
        from transmission_assembly import generate
        return generate(request,sys.modules[__name__])
    if mode=='body_generate' and request['Spec'].get('Variant')=='Reference67':
        from reference67 import generate
        return generate(request['Spec'],folder,sys.modules[__name__])
    if mode=='body_generate' and request['Spec'].get('Connected'):
        from connected_body import generate
        return generate(request['Spec'],folder,sys.modules[__name__])
    if mode=='body_generate':
        from procedural_body import generate
        return generate(request['Spec'],folder,sys.modules[__name__])
    if mode=='solve_sketch':
        sketch,status=solve_sketch(request['Sketch'],Parameters(request.get('Parameters')).value)
        return dict(Ok=True,Sketch=sketch,ConstraintStatus=status)
    bodies,result=build_document(request['Document'],folder,request.get('Marker'),mode in {'build','native'})
    if mode in {'export_step','export_stl','native'}:
        selected=request.get('Body')
        if request.get('Parts') is not None:
            keys=request['Parts']
            if not keys or any(key not in bodies for key in keys):raise ValueError('Недопустимый состав STEP-экспорта.')
            s=compound([bodies[key] for key in keys])
        else:s=bodies[selected] if selected else compound(list(bodies.values()))
        path=request.get('Path') or str(Path(folder)/'result.step')
        if mode=='export_stl':
            # OCCT's Windows STL writer does not reliably accept Unicode paths.
            # Python writes the binary container while OCCT supplies triangles.
            if request.get('PrepareForPrint'):
                from OCP.BRepBuilderAPI import BRepBuilderAPI_Sewing,BRepBuilderAPI_MakeSolid
                from OCP.ShapeUpgrade import ShapeUpgrade_UnifySameDomain
                from OCP.TopAbs import TopAbs_SHELL
                from OCP.BRepTools import BRepTools
                old_volume=props(s)[0]
                if len(shapes(s,TopAbs_SOLID))!=1:raise ValueError('Печатная заготовка должна содержать одно CAD-тело.')
                sew=BRepBuilderAPI_Sewing(1e-6);sew.Load(s);sew.Perform();repaired=sew.SewedShape()
                if not shapes(repaired,TopAbs_SOLID):
                    maker=BRepBuilderAPI_MakeSolid()
                    for shell in shapes(repaired,TopAbs_SHELL):maker.Add(TopoDS.Shell(shell))
                    repaired=maker.Solid()
                unify=ShapeUpgrade_UnifySameDomain(checked(repaired),True,True,False);unify.Build();s=checked(unify.Shape())
                if not math.isclose(props(s)[0],old_volume,rel_tol=1e-6,abs_tol=.003):raise ValueError('Подготовка STL изменила объём CAD-тела.')
                BRepTools.Clean_s(s)
            BRepMesh_IncrementalMesh(s,.005 if request.get('PrepareForPrint') else .03,False,.05 if request.get('PrepareForPrint') else .12,False if request.get('PrepareForPrint') else True).Perform()
            count=0
            with open(path,'wb') as stream:
                stream.write(b'GearLab native CAD STL'.ljust(80,b'\0'));stream.write(struct.pack('<I',0))
                for face in shapes(s,TopAbs_FACE):
                    face=cast(face,TopAbs_FACE);loc=TopLoc_Location();tri=BRep_Tool.Triangulation_s(face,loc)
                    if tri is None:continue
                    transform=loc.Transformation()
                    for i in range(1,tri.NbTriangles()+1):
                        ids=list(tri.Triangle(i).Get())
                        if face.Orientation()==TopAbs_REVERSED:ids[1],ids[2]=ids[2],ids[1]
                        points=[tri.Node(j).Transformed(transform) for j in ids]
                        normal=gp_Vec(points[0],points[1]).Crossed(gp_Vec(points[0],points[2]))
                        if normal.SquareMagnitude()<1e-24:continue
                        normal.Normalize();stream.write(struct.pack('<12fH',*xyz(normal),*(v for p in points for v in xyz(p)),0));count+=1
                if count==0:raise ValueError('Нет треугольников для STL.')
                stream.seek(80);stream.write(struct.pack('<I',count))
        else:
            w=STEPControl_Writer();w.Transfer(s,STEPControl_AsIs)
            if w.Write(path)!=IFSelect_RetDone:raise ValueError('Не удалось записать STEP.')
        result['Path']=path
        if mode=='native':result['Step']=base64.b64encode(Path(path).read_bytes()).decode('ascii')
    if mode=='distance':
        a,b=(bodies[k] for k in request['Bodies']);distance=BRepExtrema_DistShapeShape(a,b);distance.Perform()
        if not distance.IsDone():raise ValueError('Не удалось измерить расстояние.')
        result['Distance']=distance.Value()
    return result

def main():
    if len(sys.argv)>1 and sys.argv[1]=='--serve':
        # Native OCCT diagnostics must never share the JSON reply channel.
        # Python keeps a duplicate of the original pipe; C/C++ stdout goes to stderr.
        protocol=io.TextIOWrapper(os.fdopen(os.dup(sys.stdout.fileno()),'wb'),encoding='utf-8',write_through=True)
        sys.stdout.flush();os.dup2(sys.stderr.fileno(),1);sys.stdout=protocol
        for line in sys.stdin:
            try:result=handle(json.loads(line))
            except Exception as ex:
                import traceback
                try:
                    diagnostic=Path(json.loads(line)['Folder']);diagnostic.mkdir(parents=True,exist_ok=True);(diagnostic/'kernel-error.txt').write_text(traceback.format_exc(),encoding='utf-8')
                except Exception:pass
                result=dict(Ok=False,Error=str(ex))
            print('GEARLAB_REPLY:'+json.dumps(result,ensure_ascii=True),flush=True)
    else:
        try:result=handle(json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig')))
        except Exception as ex:result=dict(Ok=False,Error=str(ex))
        Path(sys.argv[2]).write_text(json.dumps(result,ensure_ascii=False),encoding='utf-8')
        return 0 if result['Ok'] else 1
if __name__=='__main__':sys.exit(main() or 0)
