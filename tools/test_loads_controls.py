"""Regression for torsion/axial sizing and degenerate sketch directions."""
import sys,math,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'kernel'))
from body_section import equivalent_stress,size_section
from sketch_solver import solve
out=Path(sys.argv[1]);out.mkdir(parents=True,exist_ok=True);passed=[]
def check(value,name):
    if not value:raise AssertionError(name)
    passed.append(name)
r,w=20,3
area=math.pi*(r*r-(r-w)**2);inertia=math.pi*(r**4-(r-w)**4)/4
check(math.isclose(equivalent_stress(r,w,10000,0,0),10000*r/inertia),'analytic pure bending')
check(math.isclose(equivalent_stress(r,w,0,10000,0),math.sqrt(3)*10000*r/(2*inertia)),'analytic pure torsion with exact polar inertia')
check(math.isclose(equivalent_stress(r,w,0,0,1000),1000/area),'analytic pure axial load')
normal=10000*r/inertia+1000/area;shear=20000*r/(2*inertia)
check(math.isclose(equivalent_stress(r,w,10000,20000,1000),math.hypot(normal,math.sqrt(3)*shear)),'combined von Mises stress')
baseline=size_section(18,3.2,100,0,0,12)
torque=size_section(18,3.2,100,150000,0,12)
axial=size_section(18,3.2,100,0,12000,12)
check(torque[0]>baseline[0] and torque[1]<=12,'torsion expands automatic section')
check(axial[0]>baseline[0] and axial[1]<=12,'axial force expands automatic section')
try:size_section(18,3.2,1e12,1e12,1e9,12)
except ValueError:passed.append('impossible 70mm section rejected')
else:raise AssertionError('unsafe oversized section accepted')
for kind in ['parallel','perpendicular','angle','tangent','horizontal','vertical']:
    for zero_both in [False,True]:
        line=dict(Id='a',Kind='line',Points=[[0,0],[0,0]])
        second=dict(Id='b',Kind='circle',Points=[[3,0]],Radius=1) if kind=='tangent' else dict(Id='b',Kind='line',Points=[[0,0],[0,0] if zero_both else [10,0]])
        c=dict(Kind=kind,A='a',B='b',Value=90)
        try:solve(dict(Entities=[line,second],Constraints=[c]),float)
        except ValueError as ex:check('Направление' in str(ex),kind+': explicit degeneracy error '+str(zero_both))
        else:raise AssertionError(kind+': zero-length direction accepted')
valid=dict(Entities=[dict(Id='a',Kind='line',Points=[[0,0],[10,0]]),dict(Id='b',Kind='line',Points=[[0,0],[0,10]])],Constraints=[dict(Kind='perpendicular',A='a',B='b')])
_,status=solve(valid,float);check(status['Error']<1e-6,'valid perpendicular sketch still solves')
(out/'loads-kernel-results.json').write_text(json.dumps(dict(passed=passed,section_examples=dict(baseline=baseline,torsion=torque,axial=axial)),indent=2),encoding='utf-8')
print('PASS',len(passed),'section and sketch regression checks')
