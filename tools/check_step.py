import json, sys, time
from pathlib import Path
from OCP.STEPControl import STEPControl_Reader
from OCP.IFSelect import IFSelect_RetDone
from OCP.TopExp import TopExp_Explorer
from OCP.TopAbs import TopAbs_SOLID, TopAbs_FACE, TopAbs_EDGE
from OCP.TopoDS import TopoDS
from OCP.BRepCheck import BRepCheck_Analyzer
from OCP.BRepGProp import BRepGProp
from OCP.GProp import GProp_GProps

def children(shape, kind):
    explorer = TopExp_Explorer(shape, kind)
    while explorer.More():
        yield explorer.Current()
        explorer.Next()

def check(path):
    started = time.perf_counter()
    reader = STEPControl_Reader()
    if reader.ReadFile(str(path)) != IFSelect_RetDone:
        raise RuntimeError(f'Cannot read {path}')
    reader.TransferRoots()
    shape = reader.OneShape()
    parts = []
    for index, solid in enumerate(children(shape, TopAbs_SOLID)):
        props = GProp_GProps()
        BRepGProp.VolumeProperties_s(solid, props)
        faces = list(children(solid, TopAbs_FACE))
        invalid_faces = sum(not BRepCheck_Analyzer(face).IsValid() for face in faces)
        parts.append(dict(index=index, valid=BRepCheck_Analyzer(solid).IsValid(),
                          volume=props.Mass(), faces=len(faces), invalid_faces=invalid_faces))
    result = dict(file=str(path), seconds=round(time.perf_counter()-started, 3),
                  valid=BRepCheck_Analyzer(shape).IsValid(), solids=len(parts),
                  faces=sum(p['faces'] for p in parts),
                  invalid_solids=sum(not p['valid'] for p in parts),
                  invalid_faces=sum(p['invalid_faces'] for p in parts), parts=parts)
    Path(str(path)+'.audit.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='parts'}), flush=True)

for arg in sys.argv[1:]:
    check(Path(arg).resolve())
