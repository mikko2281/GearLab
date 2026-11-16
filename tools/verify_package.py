"""Verify the complete portable archive and its isolated CAD runtime."""

import sys,json,hashlib,zipfile,subprocess,tempfile

from pathlib import Path

stage=Path(sys.argv[1]).resolve();archive=Path(sys.argv[2]).resolve()

with zipfile.ZipFile(archive) as z:

 assert z.testzip() is None,'ZIP CRC'

 assert all(not Path(n).is_absolute() and '..' not in Path(n).parts for n in z.namelist()),'ZIP paths'

 count=len(z.namelist())
 expected_files={p.relative_to(stage).as_posix():p for p in stage.rglob('*') if p.is_file()}
 archive_files={n[len(stage.name)+1:]:n for n in z.namelist() if not n.endswith('/')}
 assert all(n.startswith(stage.name+'/') for n in z.namelist()),'Archive root'
 assert len(archive_files)==len(set(z.namelist())),'Duplicate ZIP paths'
 assert archive_files.keys()==expected_files.keys(),'Archive/stage file list differs'
 for name,p in expected_files.items():
  assert hashlib.sha256(z.read(archive_files[name])).digest()==hashlib.sha256(p.read_bytes()).digest(),name


for required in ['GearLab.exe','GearLab.exe.config','web/scene.js','web/index.html','runtime/python.exe','kernel/cad_kernel.py','FREEWARE.txt']:
 assert (stage/required).is_file(),required
for required in ['examples','licenses']:
 assert any((stage/required).rglob('*')),required

checked=0

for line in (stage/'SHA256SUMS.txt').read_text(encoding='utf-8-sig').splitlines():

 expected,name=line.split('  ',1);p=(stage/name).resolve();assert p.is_relative_to(stage),name

 assert hashlib.sha256(p.read_bytes()).hexdigest().upper()==expected.upper(),name

 checked+=1

info=json.loads((stage/'build-info.json').read_text(encoding='utf-8-sig'))

assert hashlib.sha256((stage/'GearLab.exe').read_bytes()).hexdigest().upper()==info['Sha256'], 'EXE identity'
assert info.get('ReadyForManufacture') is False,'Preliminary engineering status missing'

with tempfile.TemporaryDirectory(prefix='gearlab-package-check-') as out:

 code="import sys;sys.path.insert(0,sys.argv[1]);import cad_kernel as k;from pathlib import Path;b,_=k.build_document({'Features':[{'Id':'b','Kind':'box','Values':{'Width':20,'Depth':30,'Height':40}}]},Path(sys.argv[2]),display=False);assert abs(sum(k.props(s)[0] for s in b.values())-24000)<1e-5;print('isolated CAD: 24000 mm3')"

 subprocess.run([str(stage/'runtime/python.exe'),'-I','-c',code,str(stage/'kernel'),out],check=True)

digest=hashlib.sha256(archive.read_bytes()).hexdigest().upper()

assert archive.with_suffix(archive.suffix+'.sha256').read_text().split()[0].upper()==digest,'archive SHA256'

print(json.dumps(dict(archive=archive.name,bytes=archive.stat().st_size,entries=count,verified_files=checked,sha256=digest,version=info['Version']),indent=2))
