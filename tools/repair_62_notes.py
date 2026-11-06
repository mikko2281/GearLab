"""Repair only the known 6.2 release's documentation links; never change tag/assets."""
import json,os,subprocess
from pathlib import Path
repo='mikko2281/GearLab';tag='v6.2.0';commit='f353e9c758425c9e02690f00a13140d6d4d7f3b0'
assert os.environ.get('GITHUB_REPOSITORY')==repo
assert os.environ.get('GITHUB_REF')=='refs/heads/main'
def api(path,method='GET',body=None):
 args=['gh','api','repos/'+repo+'/'+path]
 if method!='GET':args+=['--method',method]
 if body is not None:
  target=Path('release-62-notes-input.json');target.write_text(json.dumps(body,ensure_ascii=True),encoding='ascii');args+=['--input',str(target)]
 result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8',check=True)
 return json.loads(result.stdout)
release=api('releases/tags/'+tag)
assert release['id']==436307369 and not release['draft'] and not release['prerelease']
assert api('git/ref/tags/'+tag)['object']['sha']==commit
before=[(a['id'],a['name'],a['digest'],a['size']) for a in release['assets']]
body=release['body']
blob='https://github.com/'+repo+'/blob/'+commit+'/docs/'
body=body.replace('(PTK_6.2.png)','(https://raw.githubusercontent.com/'+repo+'/'+commit+'/docs/PTK_6.2.png)')
body=body.replace('(verification/6.2/','('+blob+'verification/6.2/')
body=body.replace('(ENGINEERING_6.2.json)','('+blob+'ENGINEERING_6.2.json)')
updated=api('releases/'+str(release['id']),'PATCH',{'body':body})
assert updated['body']==body
assert [(a['id'],a['name'],a['digest'],a['size']) for a in updated['assets']]==before
assert api('git/ref/tags/'+tag)['object']['sha']==commit
print('Verified 6.2 documentation links; tag and all asset digests preserved.')
