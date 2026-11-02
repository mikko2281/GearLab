"""Measure supplied motor reference STEP without inferring electrical ratings."""
import json, sys
from pathlib import Path
from OCP.STEPControl import STEPControl_Reader
from OCP.IFSelect import IFSelect_RetDone
from OCP.Bnd import Bnd_Box
from OCP.BRepBndLib import BRepBndLib
from OCP.BRepAdaptor import BRepAdaptor_Surface
from OCP.GeomAbs import GeomAbs_Cylinder, GeomAbs_Plane
from OCP.TopExp import TopExp_Explorer
from OCP.TopAbs import TopAbs_FACE
from OCP.TopoDS import TopoDS
from OCP.BRepCheck import BRepCheck_Analyzer

def xyz(p): return [round(p.X(), 5), round(p.Y(), 5), round(p.Z(), 5)]
for filename in sys.argv[1:]:
    reader = STEPControl_Reader()
    assert reader.ReadFile(filename) == IFSelect_RetDone
    reader.TransferRoots()
    shape = reader.OneShape()
    bounds = Bnd_Box()
    BRepBndLib.AddOptimal_s(shape, bounds, False, False)
    faces, cylinders, planes = TopExp_Explorer(shape, TopAbs_FACE), [], []
    while faces.More():
        face = TopoDS.Face(faces.Current())
        surf = BRepAdaptor_Surface(face)
        box = Bnd_Box()
        BRepBndLib.AddOptimal_s(face, box, False, False)
        extent = [round(x, 5) for x in xyz(box.CornerMin()) + xyz(box.CornerMax())]
        if surf.GetType() == GeomAbs_Cylinder:
            c = surf.Cylinder()
            cylinders.append(dict(radius=round(c.Radius(), 5), origin=xyz(c.Location()), axis=xyz(c.Axis().Direction()), bounds=extent))
        elif surf.GetType() == GeomAbs_Plane:
            p = surf.Plane()
            planes.append(dict(origin=xyz(p.Location()), normal=xyz(p.Axis().Direction()), bounds=extent))
        faces.Next()
    result = dict(file=filename, valid=BRepCheck_Analyzer(shape).IsValid(), bounds=xyz(bounds.CornerMin()) + xyz(bounds.CornerMax()), cylinders=cylinders, planes=planes)
    target = Path('tmp') / (Path(filename).stem + '.dimensions.json')
    target.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result))


