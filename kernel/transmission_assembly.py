"""B-rep from parametric sections. Never fits a triangulated preview mesh."""
import base64, hashlib, json, math, tempfile
from pathlib import Path

def generate(request, k):
    spec=request['Spec']
    if not isinstance(spec,list) or not 1<=len(spec)<=3000: raise ValueError('Assembly size outside bounds')
    cache=getattr(k,'TRANSMISSION_CACHE',None)
    if cache is None: cache={}; k.TRANSMISSION_CACHE=cache
    folder=Path(request['Folder']);folder.mkdir(parents=True,exist_ok=True)
    def wire(loop,z):
        if 'Circle' in loop:
            x,y,r=loop['Circle']
            if not all(math.isfinite(v) for v in (x,y,r,z)) or r<=0: raise ValueError('Invalid circle')
            return k.BRepBuilderAPI_MakeWire(k.BRepBuilderAPI_MakeEdge(k.gp_Circ(k.gp_Ax2(k.gp_Pnt(x,y,z),k.gp_Dir(0,0,1)),r)).Edge()).Wire()
        pts=loop['Points']
        if not 3<=len(pts)<=20000:raise ValueError('Invalid polygon')
        p=k.BRepBuilderAPI_MakePolygon()
        for x,y in pts:
            if not all(math.isfinite(v) and abs(v)<=100000 for v in (x,y,z)):raise ValueError('Invalid point')
            p.Add(k.gp_Pnt(x,y,z))
        p.Close();return p.Wire()
    def extrusion(section):
        z,h=section['Z0'],section['Z1']-section['Z0']
        if h<=0: raise ValueError('Non-positive section thickness')
        face=k.BRepBuilderAPI_MakeFace(wire(section['Outer'],z))
        for hole in section.get('Holes',[]): face.Add(k.TopoDS.Wire(wire(hole,z).Reversed()))
        return k.checked(k.BRepPrimAPI_MakePrism(face.Face(),k.gp_Vec(0,0,h)).Shape())
    def loft(levels,outer=True,index=0):
        op=k.BRepOffsetAPI_ThruSections(True,False,1e-6)
        op.SetMaxDegree(3)
        op.CheckCompatibility(False)
        for level in levels:op.AddWire(wire(level['Outer'] if outer else level['Holes'][index],level['Z']))
        op.Build()
        if not op.IsDone(): raise ValueError('Loft failed')
        return k.checked(op.Shape())
    results=[]; report=[]
    for item in spec:
        key=hashlib.sha256(json.dumps(item,sort_keys=True,separators=(',',':')).encode()).hexdigest()
        shape=cache.get(key)
        if shape is None:
            if item.get('Native'):
                path=folder/'native.step';path.write_bytes(base64.b64decode(item['Native'],validate=True))
                reader=k.STEPControl_Reader()
                if reader.ReadFile(str(path))!=k.IFSelect_RetDone:raise ValueError('Native STEP import failed')
                reader.TransferRoots();shape=k.checked(reader.OneShape())
            elif item.get('Loft'):
                levels=item['Loft']
                if not 2<=len(levels)<=512:raise ValueError('Too many loft sections')
                shape=loft(levels)
                nh=len(levels[0]['Holes'])
                if any(len(level['Holes'])!=nh for level in levels):raise ValueError('Loft hole mismatch')
                for index in range(nh):shape=k.checked(k.BRepAlgoAPI_Cut(shape,loft(levels,False,index)).Shape())
            else:
                sections=item['Sections']
                if not 1<=len(sections)<=1000:raise ValueError('Invalid section count')
                shape=extrusion(sections[0])
                for section in sections[1:]:shape=k.checked(k.BRepAlgoAPI_Fuse(shape,extrusion(section)).Shape())
            tr=item['Transform']
            if len(tr)!=12 or not all(math.isfinite(v) for v in tr):raise ValueError('Invalid placement')
            transform=k.gp_Trsf();transform.SetValues(*tr)
            shape=k.checked(k.BRepBuilderAPI_Transform(shape,transform,True).Shape())
            if len(cache)>=256:cache.pop(next(iter(cache)))
            cache[key]=shape
        solids=k.shapes(shape,k.TopAbs_SOLID)
        if len(solids)!=1:raise ValueError(item['Id']+': expected one connected solid, got '+str(len(solids)))
        results.append(shape);report.append({'Id':item['Id'],'Volume_mm3':k.props(shape)[0],'Valid':True,'Solids':len(solids)})
    if request.get('Mode')=='assembly_audit':
        boxes=[k.bounds(s) for s in results];pairs=[];tested=0;complete=True
        for i,a in enumerate(results):
            for j in range(i+1,len(results)):
                aa,bb=boxes[i],boxes[j]
                if any(min(aa[n+3],bb[n+3])-max(aa[n],bb[n])<=1e-6 for n in range(3)):continue
                if tested>=250:complete=False;break
                tested+=1;common=k.BRepAlgoAPI_Common(a,results[j]);common.Build()
                if not common.IsDone():raise ValueError('Intersection operation failed: '+spec[i]['Id']+' / '+spec[j]['Id'])
                volume=k.props(common.Shape())[0]
                if volume>1e-5:pairs.append({'First':spec[i]['Id'],'Second':spec[j]['Id'],'Volume':volume})
            if not complete:break
        return {'Ok':True,'Audit':{'Complete':complete,'TestedPairs':tested,'Pairs':pairs}}
    writer=k.STEPControl_Writer();writer.Transfer(k.compound(results),k.STEPControl_AsIs)
    if writer.Write(str(request['Path']))!=k.IFSelect_RetDone:raise ValueError('STEP write failed')
    Path(str(request['Path'])+'.validation.json').write_text(json.dumps({'Ok':True,'Parts':report},indent=2),encoding='utf-8')
    return {'Ok':True,'Notes':['Parametric B-rep; tooth conjugacy is not validated.']}
