"""Build an isolated redistributable Python/OCCT runtime from explicit inputs."""
import argparse, shutil, sys, sysconfig, json
import pefile
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('destination');args=parser.parse_args()
source=Path(sys.executable).parent;target=Path(args.destination).resolve()
if target.exists():raise SystemExit('Destination already exists; preserve existing runtime.')
target.mkdir(parents=True)
for filename in ['python.exe','pythonw.exe','python3.dll','python312.dll','vcruntime140.dll','vcruntime140_1.dll','LICENSE.txt']:
 shutil.copy2(source/filename,target/filename)
def ignore(directory,names):return [n for n in names if n in {'site-packages','__pycache__','test','tests','idlelib','turtledemo','ensurepip','sitecustomize.py','usercustomize.py'} or n.endswith('.pyc')]
shutil.copytree(source/'Lib',target/'Lib',ignore=ignore)
if (source/'DLLs').exists():shutil.copytree(source/'DLLs',target/'DLLs',ignore=ignore)
packages=source/'Lib/site-packages';out=target/'Lib/site-packages';out.mkdir()
shutil.copytree(packages/'OCP',out/'OCP',ignore=ignore)
# The CAD worker uses OCCT only. Resolve native PE imports, keeping the VTK
# libraries actually linked by the OCP extension without unrelated render/filter
# modules. Missing system imports remain Windows/.NET installation prerequisites.
search={p.name.lower():p for name in ['cadquery_ocp.libs','vtk.libs','vtkmodules'] for p in (packages/name).glob('*.dll')}
pending=list((packages/'OCP').glob('*.pyd'));selected={};imports={}
while pending:
 binary=pending.pop();pe=pefile.PE(str(binary),fast_load=True);pe.parse_data_directories(directories=[1,13],import_dllnames_only=True)
 names=[e.dll.decode('ascii').lower() for attr in ['DIRECTORY_ENTRY_IMPORT','DIRECTORY_ENTRY_DELAY_IMPORT'] for e in getattr(pe,attr,[])]
 imports[binary.name]=names;pe.close()
 for name in names:
  if name in search and name not in selected:selected[name]=search[name];pending.append(search[name])
for path in selected.values():
 dest=out/path.parent.name;dest.mkdir(exist_ok=True);shutil.copy2(path,dest/path.name)
for name in ['cadquery_ocp.libs','vtk.libs']:(out/name).mkdir(exist_ok=True)
for pattern in ['cadquery_ocp-*.dist-info','cadquery_ocp_proxy-*.dist-info','vtk-*.dist-info']:
 for p in packages.glob(pattern):shutil.copytree(p,out/p.name,ignore=ignore)
(target/'python312._pth').write_text('.\nLib\nDLLs\nLib/site-packages\n',encoding='ascii')
(target/'RUNTIME.json').write_text(json.dumps({'python':sys.version,'packages':['cadquery-ocp 8.0.1.1.0','vtk 9.6.2'],'isolated':True},indent=2),encoding='utf-8')
(target/'NATIVE_IMPORTS.json').write_text(json.dumps(imports,indent=2),encoding='utf-8')
print(str(target),flush=True)
