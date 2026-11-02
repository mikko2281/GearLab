"""Fetch pinned upstream Three.js and Microsoft WebView2 SDK assets."""
import urllib.request,zipfile,tarfile,io,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];vendor=root/'web/vendor';vendor.mkdir(parents=True,exist_ok=True);sdk=root/'src/vendor/WebView2';sdk.mkdir(parents=True,exist_ok=True)
urls={'three':'https://registry.npmjs.org/three/-/three-0.180.0.tgz','webview2':'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4314.35/microsoft.web.webview2.1.0.4314.35.nupkg'};origins=[]
def get(key):
 data=urllib.request.urlopen(urls[key],timeout=90).read();origins.append(dict(component=key,url=urls[key],sha256=hashlib.sha256(data).hexdigest()));return data
with tarfile.open(fileobj=io.BytesIO(get('three')),mode='r:gz') as archive:
 for source,dest in [('build/three.module.js','three.module.js'),('build/three.core.js','three.core.js'),('examples/jsm/controls/OrbitControls.js','addons/OrbitControls.js'),('examples/jsm/controls/TransformControls.js','addons/TransformControls.js'),('LICENSE','LICENSE-three.txt')]:
  p=vendor/dest;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(archive.extractfile('package/'+source).read())
with zipfile.ZipFile(io.BytesIO(get('webview2'))) as archive:
 for source,dest in [('lib/net462/Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.Core.dll'),('lib/net462/Microsoft.Web.WebView2.WinForms.dll','Microsoft.Web.WebView2.WinForms.dll'),('build/native/x64/WebView2Loader.dll','WebView2Loader.dll')]:
  (sdk/dest).write_bytes(archive.read(source))
 for name in archive.namelist():
  if name.lower().endswith(('.txt','.md')) and any(key in name.lower() for key in ['license','notice']):(sdk/Path(name).name).write_bytes(archive.read(name))
(vendor/'ORIGINS.json').write_text(json.dumps(origins,indent=2),encoding='utf-8');print('Pinned Three.js 0.180.0 and WebView2 1.0.4314.35 assets ready')
