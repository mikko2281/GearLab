"""Fetch upstream license/notice texts; retain origins and hashes for audit."""
import urllib.request,hashlib,json,concurrent.futures,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root/'licenses';out.mkdir(exist_ok=True)
items={
 'OCP-Apache-2.0':['https://raw.githubusercontent.com/CadQuery/OCP/master/LICENSE'],
 'OCCT-LGPL-2.1':['https://raw.githubusercontent.com/Open-Cascade-SAS/OCCT/V8_0_1/LICENSE_LGPL_21.txt','https://raw.githubusercontent.com/Open-Cascade-SAS/OCCT/master/LICENSE_LGPL_21.txt'],
 'OCCT-exception-notice':['https://raw.githubusercontent.com/Open-Cascade-SAS/OCCT/V8_0_1/dox/license.md','https://raw.githubusercontent.com/Open-Cascade-SAS/OCCT/master/dox/license.md'],
 'FreeImage-FIPL':['https://raw.githubusercontent.com/WinMerge/freeimage/master/license-fi.txt','https://raw.githubusercontent.com/danoli3/FreeImage/master/license-fi.txt'],
 'FreeType':['https://raw.githubusercontent.com/freetype/freetype/master/docs/FTL.TXT'],
 'OpenEXR':['https://raw.githubusercontent.com/AcademySoftwareFoundation/openexr/main/LICENSE.md'],
 'Imath':['https://raw.githubusercontent.com/AcademySoftwareFoundation/Imath/main/LICENSE.md'],
 'libjpeg-turbo':['https://raw.githubusercontent.com/libjpeg-turbo/libjpeg-turbo/main/LICENSE.md'],
 'lcms':['https://raw.githubusercontent.com/mm2/Little-CMS/master/LICENSE'],
 'Lerc':['https://raw.githubusercontent.com/Esri/lerc/master/LICENSE'],
 'XZ':['https://raw.githubusercontent.com/tukaani-project/xz/master/COPYING'],
 'libpng':['https://raw.githubusercontent.com/pnggroup/libpng/libpng16/LICENSE'],
 'libwebp':['https://raw.githubusercontent.com/webmproject/libwebp/main/COPYING'],
 'OpenJPEG':['https://raw.githubusercontent.com/uclouvain/openjpeg/master/LICENSE'],
 'OpenJPH':['https://raw.githubusercontent.com/aous72/OpenJPH/master/LICENSE'],
 'LibRaw-LGPL':['https://raw.githubusercontent.com/LibRaw/LibRaw/master/LICENSE.LGPL'],
 'LibRaw-copyright':['https://raw.githubusercontent.com/LibRaw/LibRaw/master/COPYRIGHT'],
 'TIFF':['https://raw.githubusercontent.com/libsdl-org/libtiff/master/LICENSE.md','https://raw.githubusercontent.com/vadz/libtiff/master/COPYRIGHT'],
 'zlib':['https://raw.githubusercontent.com/madler/zlib/develop/LICENSE'],
 'zstd':['https://raw.githubusercontent.com/facebook/zstd/dev/LICENSE'],
 'libdeflate':['https://raw.githubusercontent.com/ebiggers/libdeflate/master/COPYING'],
 'VTK':['https://raw.githubusercontent.com/Kitware/VTK/v9.6.2/Copyright.txt','https://raw.githubusercontent.com/Kitware/VTK/master/Copyright.txt'],
 'Viskores':['https://raw.githubusercontent.com/Viskores/viskores/main/LICENSE.txt'],
 'OpenXR':['https://raw.githubusercontent.com/KhronosGroup/OpenXR-SDK/main/LICENSE'],
 'DoubleConversion':['https://raw.githubusercontent.com/google/double-conversion/master/LICENSE'],
 'expat':['https://raw.githubusercontent.com/libexpat/libexpat/master/expat/COPYING'],
 'fmt':['https://raw.githubusercontent.com/fmtlib/fmt/master/LICENSE'],
 'pugixml':['https://raw.githubusercontent.com/zeux/pugixml/master/LICENSE.md'],
 'LZ4':['https://raw.githubusercontent.com/lz4/lz4/dev/LICENSE'],
 'utf8cpp':['https://raw.githubusercontent.com/nemtrif/utfcpp/master/LICENSE'],
 'Python':['https://raw.githubusercontent.com/python/cpython/v3.12.14/LICENSE'],
}
def fetch(item):
 name,urls=item
 for url in urls:
  try:
   data=urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'GearLab-license-bundler'}),timeout=25).read()
   if len(data)<80:raise ValueError('Short notice')
   (out/(name+'.txt')).write_bytes(data);return dict(component=name,url=url,sha256=hashlib.sha256(data).hexdigest())
  except Exception as e:error=str(e)
 return dict(component=name,error=error)
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:records=list(pool.map(fetch,items.items()))
(out/'ORIGINS.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
shutil.copy2(root/'src/vendor/LibTessDotNet/LICENSE.txt',out/'LibTessDotNet.txt')
for record in records:print(record['component'],record.get('error','OK'))
if any('error' in r for r in records):raise SystemExit(1)
