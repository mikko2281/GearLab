"""Create/finalize only the GearLab 5.6 release using the Actions repository token.
Never modifies an already published release. Run after all build/native checks.
"""
import json,os,subprocess,sys,hashlib
from pathlib import Path
REPO='mikko2281/GearLab';TAG='v5.6.0'
root=Path(__file__).resolve().parents[1]
assert os.environ.get('GITHUB_REPOSITORY')==REPO,'Unexpected repository'
commit=os.environ['GITHUB_SHA']
def api(path,method='GET',data=None):
 args=['gh','api',f'repos/{REPO}/{path}']
 if method!='GET':args+=['--method',method]
 if data is not None:
  payload=root/'release-api-input.json';payload.write_text(json.dumps(data,ensure_ascii=True),encoding='ascii');args+=['--input',str(payload)]
 p=subprocess.run(args,capture_output=True,text=True,encoding='utf-8')
 if p.returncode:raise RuntimeError(p.stderr)
 return json.loads(p.stdout)
if sys.argv[1]=='draft':
 releases=api('releases?per_page=100');existing=next((r for r in releases if r['tag_name']==TAG),None)
 if existing and not existing['draft']:raise RuntimeError('5.6 is already published; refusing to replace it')
 notes=(root/'docs/RELEASE_5.6.md').read_text(encoding='utf-8')
 data=dict(tag_name=TAG,target_commitish=commit,name='GearLab 5.6 — общий интерфейс и быстрый 3D волнового редуктора',body=notes,draft=True,prerelease=False)
 release=api('releases/'+str(existing['id']),'PATCH',data) if existing else api('releases','POST',data)
 with open(os.environ['GITHUB_OUTPUT'],'a',encoding='utf-8') as out:out.write('release_id='+str(release['id'])+'\n')
 print('Draft prepared',release['id'])
else:
 release_id=int(sys.argv[2]);release=api('releases/'+str(release_id))
 assert release['draft'] and release['tag_name']==TAG and release['target_commitish']==commit
 assets={a['name']:a for a in release['assets']}
 for name in ['GearLab_5.6_Windows.zip','GearLab_5.6_source.zip']:
  p=root/'dist'/name;digest=hashlib.sha256(p.read_bytes()).hexdigest();a=assets[name]
  assert a['state']=='uploaded' and a['size']==p.stat().st_size and a['digest']=='sha256:'+digest,'Unverified release asset'
 for name in ['build-info.json','GearLab_5.6_SHA256SUMS.txt','GearLab_5.6_Windows.zip.sha256','GearLab_5.6_source.zip.sha256']:assert assets[name]['state']=='uploaded'
 result=api('releases/'+str(release_id),'PATCH',dict(draft=False,prerelease=False,make_latest='true'))
 assert not result['draft']
 tag=api('git/ref/tags/'+TAG);assert tag['object']['sha']==commit
 print('PUBLISHED',result['html_url'])
