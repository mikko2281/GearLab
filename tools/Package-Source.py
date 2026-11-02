"""Create a reviewable source archive without local runtime/caches/private assets."""
import sys,zipfile,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1];dest=Path(sys.argv[1]);dest.mkdir(parents=True,exist_ok=True);archive=dest/'GearLab_5.5_source.zip'
if archive.exists():raise SystemExit('Archive exists; preserve it and choose another destination.')
paths=[root/name for name in ['.github','src','web','kernel','tools','docs','examples','licenses','Для_Fusion','README.md','FREEWARE.txt','НАЧНИТЕ_ЗДЕСЬ.txt','test.ps1','.gitignore','.gitattributes']]
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for item in paths:
  for p in sorted(item.rglob('*')) if item.is_dir() else [item]:
   if p.is_file() and '__pycache__' not in p.parts and p.suffix!='.pyc':z.write(p,Path('GearLab_5.5_source')/p.relative_to(root))
with zipfile.ZipFile(archive,'r') as z:
 check=z.testzip()
 if check:raise RuntimeError(check)
archive.with_suffix(archive.suffix+'.sha256').write_text(hashlib.sha256(archive.read_bytes()).hexdigest().upper()+'  '+archive.name+'\n',encoding='ascii');print(archive,archive.stat().st_size)
