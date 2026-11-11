"""Publish the current version from verified CI assets; published releases are immutable."""
import hashlib
import json
import os
import re
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
repo = 'mikko2281/GearLab'
version = '.'.join(re.search(r'AssemblyFileVersion\("([0-9.]+)"\)', (root/'src/AssemblyInfo.cs').read_text(encoding='utf-8-sig')).group(1).split('.')[:3])
tag, prefix = 'v'+version, 'GearLab_'+version
assert os.environ.get('GITHUB_REPOSITORY') == repo
assert os.environ.get('GITHUB_REF') == 'refs/heads/main'
commit = os.environ['GITHUB_SHA']
assert re.fullmatch(r'[0-9a-f]{40}', commit)

def api(path, method='GET', data=None):
    args = ['gh', 'api', 'repos/'+repo+'/'+path, '--method', method]
    if data is not None:
        args += ['--input', '-']
    result = subprocess.run(args, input=json.dumps(data) if data is not None else None, capture_output=True, text=True, encoding='utf-8', check=True)
    return json.loads(result.stdout)

assert api('git/ref/heads/main')['object']['sha'] == commit, 'Superseded source commit'
releases = []
for page in range(1, 101):
    batch = api('releases?per_page=100&page='+str(page))
    releases.extend(batch)
    if len(batch) < 100:
        break
else:
    raise RuntimeError('Release inventory incomplete')
existing = next((r for r in releases if r['tag_name'] == tag), None)
if existing and not existing['draft']:
    raise RuntimeError('Published release exists; refusing replacement: '+tag)
if existing and existing['target_commitish'] != commit:
    raise RuntimeError('Draft belongs to another source commit')

assets = [root/'dist'/(prefix+suffix) for suffix in ('_Windows.zip', '_source.zip')]
assets += [p.with_suffix(p.suffix+'.sha256') for p in assets]
assets += [root/'dist'/(prefix+'_SHA256SUMS.txt'), root/'dist'/prefix/'build-info.json']
for path in assets:
    assert path.is_file() and path.stat().st_size > 0, path
info = json.loads(assets[-1].read_text(encoding='utf-8'))
assert info['Version'] == version and info['SourceCommit'] == commit and not info['LocalReviewOnly']
for path in assets[:2]:
    assert path.with_suffix(path.suffix+'.sha256').read_text().split()[0].lower() == hashlib.sha256(path.read_bytes()).hexdigest()
notes = (root/'docs'/('RELEASE_'+version+'.md')).read_text(encoding='utf-8')
base = 'https://github.com/'+repo+'/blob/'+commit+'/docs/'
notes = notes.replace('(verification/', '('+base+'verification/').replace('(PROJECT_MAP.md)', '('+base+'PROJECT_MAP.md)')
data = dict(tag_name=tag, target_commitish=commit, name='GearLab '+version+' — интерфейс и исправления · предварительная конструкция', body=notes, draft=True, prerelease=True, make_latest='false')
release = api('releases/'+str(existing['id']), 'PATCH', data) if existing else api('releases', 'POST', data)
subprocess.run(['gh', 'release', 'upload', tag, '--repo', repo, '--clobber']+[str(p) for p in assets], check=True)
check = api('releases/'+str(release['id']))
server = {a['name']: a for a in check['assets']}
assert set(server) == {p.name for p in assets}, 'Unexpected release assets'
for path in assets:
    remote = server[path.name]
    assert remote['state'] == 'uploaded' and remote['size'] == path.stat().st_size
    assert remote['digest'] == 'sha256:'+hashlib.sha256(path.read_bytes()).hexdigest()
assert api('git/ref/heads/main')['object']['sha'] == commit, 'Source changed during packaging'
published = api('releases/'+str(release['id']), 'PATCH', dict(draft=False, prerelease=True, make_latest='false'))
assert not published['draft'] and published['prerelease']
assert api('git/ref/tags/'+tag)['object']['sha'] == commit
print('PUBLISHED', published['html_url'])
