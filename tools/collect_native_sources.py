"""Bundle upstream native-library sources alongside binary distribution."""
import urllib.request,hashlib,json,concurrent.futures
from pathlib import Path
out=Path(__file__).resolve().parents[1]/'licenses/sources';out.mkdir(parents=True,exist_ok=True)
urls={
 'OCCT-8.0.1.zip':'https://codeload.github.com/Open-Cascade-SAS/OCCT/zip/refs/tags/V8_0_1',
 'LibRaw-0.22.2.zip':'https://codeload.github.com/LibRaw/LibRaw/zip/refs/tags/0.22.2',
 'OCP-build-8.0.1.1.0.zip':'https://codeload.github.com/CadQuery/ocp-build-system/zip/refs/tags/v8.0.1.1.0',
}
def fetch(item):
 name,url=item;p=out/name
 if not p.exists():
  with urllib.request.urlopen(url,timeout=120) as source,open(p,'wb') as dest:
   while data:=source.read(1024*1024):dest.write(data)
 if p.stat().st_size<1000:raise ValueError(name)
 return dict(file=name,url=url,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:records=list(pool.map(fetch,urls.items()))
(out/'ORIGINS.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print(records)
