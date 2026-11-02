"""Expand the compressed native examples tracked in Git without overwriting edits."""
import gzip,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'examples'
for source in root.rglob('*.robot.json.gz'):
 target=source.with_suffix('');data=gzip.decompress(source.read_bytes())
 if target.exists() and target.read_bytes()!=data:
  raise SystemExit('Existing example differs; preserve it: '+str(target))
 target.write_bytes(data)
 print(target.relative_to(root),len(data),hashlib.sha256(data).hexdigest())
