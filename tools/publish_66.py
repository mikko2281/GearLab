"""Publish only verified v6.6.0; never replace an existing published release."""
import hashlib,json,os,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
repo='mikko2281/GearLab';tag='v6.6.0';prefix='GearLab_6.6.0'
assert os.environ.get('GITHUB_REPOSITORY')==repo
assert os.environ.get('GITHUB_REF')=='refs/heads/main'
commit=os.environ['GITHUB_SHA']
def api(path,method='GET',data=None):
 args=['gh','api','repos/'+repo+'/'+path]
 if method!='GET':args+=['--method',method]
 if data is not None:
  p=root/'release-66-input.json';p.write_text(json.dumps(data,ensure_ascii=True),encoding='ascii');args+=['--input',str(p)]
 r=subprocess.run(args,capture_output=True,text=True,encoding='utf-8')
 if r.returncode:raise RuntimeError(r.stderr)
 return json.loads(r.stdout)
assert api('git/ref/heads/main')['object']['sha']==commit, 'Superseded source commit'
existing=next((r for r in api('releases?per_page=100') if r['tag_name']==tag),None)
if existing and not existing['draft']:raise RuntimeError('6.6.0 already published; refusing replacement')
if existing and existing['target_commitish']!=commit:raise RuntimeError('Existing 6.6.0 draft targets another commit')
assets=[root/'dist'/(prefix+'_Windows.zip'),root/'dist'/(prefix+'_source.zip')]
assets += [p.with_suffix(p.suffix+'.sha256') for p in assets]
assets += [root/'dist'/(prefix+'_SHA256SUMS.txt'),root/'dist'/prefix/'build-info.json']
for p in assets:assert p.is_file() and p.stat().st_size>0,p
info=json.loads(assets[-1].read_text(encoding='utf-8'))
assert info['Version']=='6.6.0' and info['SourceCommit']==commit
for p in assets[:2]:assert p.with_suffix(p.suffix+'.sha256').read_text().split()[0].lower()==hashlib.sha256(p.read_bytes()).hexdigest()
notes=(root/'docs/RELEASE_6.6.md').read_text(encoding='utf-8')
blob='https://github.com/'+repo+'/blob/'+commit+'/docs/'
notes=notes.replace('(verification/6.6.0/','('+blob+'verification/6.6.0/')
notes=notes.replace('(industrial-design-66/','('+blob+'industrial-design-66/')

data=dict(tag_name=tag,target_commitish=commit,name='GearLab 6.6.0 — параметрический промышленный манипулятор',body=notes,draft=True,prerelease=False,make_latest='false')
release=api('releases/'+str(existing['id']),'PATCH',data) if existing else api('releases','POST',data)
subprocess.run(['gh','release','upload',tag,'--repo',repo,'--clobber']+[str(p) for p in assets],check=True)
check=api('releases/'+str(release['id']));server={a['name']:a for a in check['assets']}
for p in assets:
 a=server[p.name]
 assert a['state']=='uploaded' and a['size']==p.stat().st_size
 assert a['digest']=='sha256:'+hashlib.sha256(p.read_bytes()).hexdigest()
published=api('releases/'+str(release['id']),'PATCH',dict(draft=False,prerelease=False,make_latest='true'))
assert not published['draft'] and not published['prerelease']
assert api('git/ref/tags/'+tag)['object']['sha']==commit
assert api('releases/latest')['id']==published['id']
print('PUBLISHED',published['html_url'])
