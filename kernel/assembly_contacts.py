"""Exact positive-volume check of the assembled, imported CAD parts."""
from OCP.gp import gp_Trsf
from OCP.BRepBuilderAPI import BRepBuilderAPI_Transform
from OCP.BRepAlgoAPI import BRepAlgoAPI_Common
from procedural_body import disjoint
def check(spec,k):
 parts=[];collisions=[]
 for p in spec['Parts']:
  s=k.import_step(p['Step']);tr=gp_Trsf();tr.SetValues(*p['Transform']);s=BRepBuilderAPI_Transform(s,tr,True).Shape();parts.append(dict(p,Shape=s,Bounds=k.bounds(s)))
 for i,a in enumerate(parts):
  for b in parts[i+1:]:
   if a.get('Integral') and a.get('Integral')==b.get('Integral'):continue
   if disjoint(a['Bounds'],b['Bounds']):continue
   common=BRepAlgoAPI_Common(a['Shape'],b['Shape']).Shape()
   if not k.shapes(common,k.TopAbs_SOLID):continue
   v=abs(k.props(common)[0])
   if v>.05:collisions.append(dict(A=a['Id'],B=b['Id'],Volume=v,Position=k.props(common)[1]))
 return dict(Ok=True,Motion=dict(Status='collision' if collisions else 'clear',Visited=1,CertifiedRegions=0,UnresolvedRegions=0,Seconds=0,Scope='Точные CAD-пересечения текущей позы; крепёжные резьбовые пары исключены.',Collisions=collisions))
