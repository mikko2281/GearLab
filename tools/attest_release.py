"""Rebuild provenance, verified before append-only publication; no Authenticode claim."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

REPO = 'mikko2281/GearLab'
TAG = 'v6.7.2'
RELEASE = 439255249
ORIGINAL = 'dbdaae4c2c8315b1d0729748608e73805bcd74ea'
ALLOWED = {'.github/workflows/attest-release.yml', 'tools/attest_release.py', 'docs/BUILD_ATTESTATION.md'}
ROOT = Path(__file__).resolve().parents[1]
os.chdir(ROOT)
OUT = ROOT / 'attested'

def run(*args):
    return subprocess.check_output(args, text=True, encoding='utf-8').strip()

def release():
    return json.loads(run('gh', 'api', f'repos/{REPO}/releases/tags/{TAG}'))

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def dump(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

def require(condition, message):
    if not condition:
        raise RuntimeError(message)

def metadata(r):
    return {k: r[k] for k in ('id', 'tag_name', 'target_commitish', 'draft', 'prerelease', 'body', 'name')}

mode = sys.argv[1]
if mode == 'guard':
    require(os.environ['GITHUB_REPOSITORY'] == REPO, 'Unexpected repository')
    require(os.environ['GITHUB_REF'] == 'refs/heads/main', 'Only main can attest')
    require(run('git', 'rev-parse', 'HEAD') == os.environ['GITHUB_SHA'], 'Checkout mismatch')
    require(run('git', 'rev-parse', TAG + '^{commit}') == ORIGINAL, 'Release tag moved')
    changed = set(run('git', 'diff', '--name-only', ORIGINAL, 'HEAD').splitlines())
    require(changed <= ALLOWED, f'Production source changed: {changed - ALLOWED}')
    r = release()
    require(r['id'] == RELEASE and not r['draft'], 'Wrong or unpublished release')
    require(r['target_commitish'] == ORIGINAL, 'Unexpected release source')
    require(len(r['assets']) >= 6, 'Missing original assets')
    dump(ROOT / 'attestation-release-before.json', r)
    print('Release source unchanged; only attestation workflow, helper and documentation added.')
elif mode == 'prepare':
    OUT.mkdir(exist_ok=False)
    prefix = f"GearLab_6.7.2_attested_{os.environ['GITHUB_RUN_ID']}_{os.environ['GITHUB_RUN_ATTEMPT']}"
    for kind in ('Windows', 'source'):
        shutil.copy2(ROOT / 'dist' / f'GearLab_6.7.2_{kind}.zip', OUT / f'{prefix}_{kind}.zip')
    info = json.loads((ROOT / 'dist/GearLab_6.7.2/build-info.json').read_text(encoding='utf-8'))
    require(info['SourceCommit'] == os.environ['GITHUB_SHA'], 'Package commit mismatch')
    require(info['Sha256'].lower() == sha(ROOT / 'Windows/GearLab.exe'), 'Packaged EXE mismatch')
    info['BuildAttestation'] = 'GitHub Actions / Sigstore SLSA provenance; detached, not Authenticode'
    info['OriginalReleaseCommit'] = ORIGINAL
    info['WorkflowRun'] = f"https://github.com/{REPO}/actions/runs/{os.environ['GITHUB_RUN_ID']}"
    dump(OUT / f'{prefix}_build-info.json', info)
    subjects = sorted(OUT.iterdir())
    sums = ''.join(f'{sha(p)}  {p.name}\n' for p in subjects)
    sums += f'{sha(ROOT / "Windows/GearLab.exe")}  GearLab_6.7.2/GearLab.exe\n'
    (OUT / f'{prefix}_SHA256SUMS.txt').write_text(sums, encoding='ascii')
elif mode == 'publish':
    prefix = f"GearLab_6.7.2_attested_{os.environ['GITHUB_RUN_ID']}_{os.environ['GITHUB_RUN_ATTEMPT']}"
    bundle = OUT / f'{prefix}_provenance.sigstore.json'
    shutil.copy2(os.environ['ATTESTATION_BUNDLE'], bundle)
    results = {}
    subjects = [ROOT / 'Windows/GearLab.exe'] + sorted(p for p in OUT.iterdir() if p != bundle)
    for p in subjects:
        args = ['gh', 'attestation', 'verify', str(p), '--repo', REPO,
                '--signer-workflow', f'{REPO}/.github/workflows/attest-release.yml',
                '--source-digest', os.environ['GITHUB_SHA'], '--deny-self-hosted-runners', '--format', 'json']
        results[p.name] = json.loads(run(*args))
        require(bool(results[p.name]), f'No verification result for {p.name}')
    dump(OUT / f'{prefix}_verification.json', results)
    readme = (f'GearLab 6.7.2: GitHub build provenance\n\n'
              f'Attestation: {os.environ["ATTESTATION_URL"]}\n'
              f'Source commit: {os.environ["GITHUB_SHA"]}\n'
              f'Production code matches release commit: {ORIGINAL}\n'
              f'Windows EXE SHA256: {sha(ROOT / "Windows/GearLab.exe")}\n\n'
              'Verify the downloaded Windows ZIP (GitHub CLI):\n'
              f'gh attestation verify {prefix}_Windows.zip -R {REPO} '
              f'--signer-workflow {REPO}/.github/workflows/attest-release.yml '
              f'--source-digest {os.environ["GITHUB_SHA"]}\n\n'
              'After extraction, the same command also verifies GearLab_6.7.2/GearLab.exe.\n'
              'This detached signature confirms build provenance and integrity. It is not Windows Authenticode.\n'
              'This is a separate rebuild; original release downloads remain unchanged and are not retroactively attested.\n')
    (OUT / f'{prefix}_VERIFY.txt').write_text(readme, encoding='utf-8')
    before = json.loads((ROOT / 'attestation-release-before.json').read_text(encoding='utf-8'))
    current = release()
    require(metadata(before) == metadata(current), 'Release metadata changed during build')
    original_assets = {a['id']: a for a in before['assets']}
    current_assets = {a['id']: a for a in current['assets']}
    for aid, a in original_assets.items():
        require(aid in current_assets and all(a[k] == current_assets[aid][k] for k in ('name', 'digest', 'size')), 'Existing asset changed')
    files = sorted(OUT.iterdir())
    require(not ({p.name for p in files} & {a['name'] for a in current['assets']}), 'Refusing to replace an asset')
    subprocess.run(['gh', 'release', 'upload', TAG, '--repo', REPO] + [str(p) for p in files], check=True)
    after = release()
    require(metadata(before) == metadata(after), 'Release metadata changed')
    for aid, a in original_assets.items():
        match = next((x for x in after['assets'] if x['id'] == aid), None)
        require(match is not None and all(a[k] == match[k] for k in ('name', 'digest', 'size')), 'Original asset changed after upload')
    for p in files:
        a = next(x for x in after['assets'] if x['name'] == p.name)
        require(a['state'] == 'uploaded' and a['size'] == p.stat().st_size and a['digest'] == 'sha256:' + sha(p), 'Uploaded asset digest mismatch')
    with open(os.environ['GITHUB_STEP_SUMMARY'], 'a', encoding='utf-8') as f:
        f.write(f'## Verified GearLab 6.7.2 build provenance\n\n{os.environ["ATTESTATION_URL"]}\n\n')
        for p in files:
            f.write(f'- [{p.name}](https://github.com/{REPO}/releases/download/{TAG}/{p.name})\n')
    print('Signatures verified for EXE, both ZIPs, build information and checksums; new assets published; original release preserved.')
else:
    raise SystemExit('Expected guard, prepare or publish')
