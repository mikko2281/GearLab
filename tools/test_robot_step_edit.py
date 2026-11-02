"""Regression: drill the actual exported robot B-rep, keeping analytic faces."""
import sys,json,math,base64
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from procedural_body import cylinder
from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut,BRepAlgoAPI_Common
from OCP.BRepAdaptor import BRepAdaptor_Surface
from OCP.GeomAbs import GeomAbs_Torus
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
from OCP.IFSelect import IFSelect_RetDone
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True)
doc=json.loads((root/'examples/Robot1_800mm.robot.json').read_text(encoding='utf-8-sig'))
part=next(p for p in doc['Joints'][1]['GeneratedBody']['Result']['Parts'] if p['Id']=='frame_0')
shape=k.checked(k.import_step(part['Step']));faces=k.shapes(shape,k.TopAbs_FACE)
assert len(faces)<1000,'Unexpected faceted STEP'
assert any(BRepAdaptor_Surface(k.cast(f,k.TopAbs_FACE)).GetType()==GeomAbs_Torus for f in faces),'Analytic elbow lost'
b=k.bounds(shape);tool=cylinder(2,b[3]-b[0]+4,[b[0]-2,(b[1]+b[4])/2,(b[2]+b[5])/2],[1,0,0])
removed=abs(k.props(BRepAlgoAPI_Common(shape,tool).Shape())[0]);assert removed>10,'Drill must actually cross the structure'
cut=k.checked(BRepAlgoAPI_Cut(shape,tool).Shape());assert len(k.shapes(cut,k.TopAbs_SOLID))==1,'Drilling detached the body'
before=k.props(shape)[0];after=k.props(cut)[0]
assert math.isclose(before-after,removed,rel_tol=1e-5,abs_tol=.001),'Cut volume does not match the real hole'
file=out/'Robot1_link_drilled.step';writer=STEPControl_Writer();writer.Transfer(cut,STEPControl_AsIs)
assert writer.Write(str(file))==IFSelect_RetDone
back=k.checked(k.import_step(base64.b64encode(file.read_bytes()).decode()))
assert math.isclose(k.props(back)[0],after,rel_tol=1e-5,abs_tol=.001),'Edited STEP roundtrip'
report=dict(passed=6,faces_before=len(faces),volume_before_mm3=before,volume_after_mm3=after,drilled_mm3=removed,scope='Native OCCT STEP import, analytic torus, new hole, solid continuity and STEP roundtrip; Fusion UI was not automated.')
(out/'step-edit-results.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('PASS 6 exported robot STEP edit checks')
