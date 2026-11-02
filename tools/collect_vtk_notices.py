"""Preserve vendored VTK license notices in addition to its top-level license."""
import urllib.request,json,concurrent.futures,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'licenses/VTK-third-party';root.mkdir(parents=True,exist_ok=True)
def read(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'GearLab-license-bundler'}),timeout=30).read()
tree=json.loads(read('https://api.github.com/repos/Kitware/VTK/git/trees/v9.6.2?recursive=1'))
paths=[x['path'] for x in tree['tree'] if x['type']=='blob' and x['path'].startswith(('ThirdParty/','Utilities/')) and Path(x['path']).name.lower().split('.')[0] in {'license','copyright','copying','notice','copying-lib','copying3','copying-lib3'}]
def fetch(path):
 url='https://raw.githubusercontent.com/Kitware/VTK/v9.6.2/'+path;data=read(url);p=root/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data);return dict(path=path,url=url,sha256=hashlib.sha256(data).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:records=list(pool.map(fetch,paths))
for name,url in [('pybind11','https://raw.githubusercontent.com/pybind/pybind11/master/LICENSE'),('nanobind','https://raw.githubusercontent.com/wjakob/nanobind/master/LICENSE')]:
 data=read(url);(root.parent/(name+'.txt')).write_bytes(data)
(root/'ORIGINS.json').write_text(json.dumps(records,indent=2),encoding='utf-8');print('Preserved',len(records),'VTK dependency notices')
