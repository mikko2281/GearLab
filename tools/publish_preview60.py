"""Publish only this verified prerelease. Never mutate an already published tag."""
import hashlib,json,os,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];repo='mikko2281/GearLab';tag='v6.0.0-preview.1';prefix='GearLab_6.0_preview_1'
assert os.environ.get('GITHUB_REPOSITORY')==repo
assert os.environ.get('GITHUB_REF')=='refs/heads/codex/gearlab-6.0'
commit=os.environ['GITHUB_SHA']
def api(path,method='GET',data=None):
    args=['gh','api','repos/'+repo+'/'+path]
    if method!='GET':args+=['--method',method]
    if data is not None:
        p=root/'release-preview-input.json';p.write_text(json.dumps(data,ensure_ascii=True),encoding='ascii');args+=['--input',str(p)]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8')
    if result.returncode:raise RuntimeError(result.stderr)
    return json.loads(result.stdout)
existing=next((r for r in api('releases?per_page=100') if r['tag_name']==tag),None)
if existing and not existing['draft']:raise RuntimeError('Preview already published; refusing replacement')
body=(root/'docs/PREVIEW_6.0.md').read_text(encoding='utf-8')+'\n\nПубличный пакет собирается GitHub Actions из этого коммита. В CI повторяются расчётные и нативные проверки экспорта; локальные отчёты GUI относятся к отдельной настольной сборке того же кода. Последний стабильный выпуск остаётся 5.6.\n'
data=dict(tag_name=tag,target_commitish=commit,name='GearLab 6.0 Preview 1 — библиотека передач и конструктор ступеней',body=body,draft=True,prerelease=True,make_latest='false')
release=api('releases/'+str(existing['id']),'PATCH',data) if existing else api('releases','POST',data)
assets=[root/'dist'/(prefix+'_Windows.zip'),root/'dist'/(prefix+'_source.zip')]
assets += [p.with_suffix(p.suffix+'.sha256') for p in assets]
assets += [root/'dist'/(prefix+'_SHA256SUMS.txt'),root/'dist'/prefix/'build-info.json']
subprocess.run(['gh','release','upload',tag,'--repo',repo,'--clobber']+[str(p) for p in assets],check=True)
check=api('releases/'+str(release['id']));server={a['name']:a for a in check['assets']}
for path in assets:
    a=server[path.name];assert a['state']=='uploaded' and a['size']==path.stat().st_size
    assert a['digest']=='sha256:'+hashlib.sha256(path.read_bytes()).hexdigest()
published=api('releases/'+str(release['id']),'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
assert published['prerelease'] and not published['draft']
assert api('git/ref/tags/'+tag)['object']['sha']==commit
print('PUBLISHED',published['html_url'])
