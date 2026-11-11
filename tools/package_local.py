"""Non-destructive portable/source packaging with bounded generated evidence."""
import hashlib,json,shutil,subprocess,sys,zipfile,re,ctypes,os
from pathlib import Path
root=Path(__file__).resolve().parents[1];dest=Path(sys.argv[1]).resolve();binary=root/(sys.argv[2] if len(sys.argv)>2 else 'Windows-final')
version=".".join(re.search(r'AssemblyFileVersion\("([0-9.]+)"\)',(root/"src/AssemblyInfo.cs").read_text(encoding="utf-8-sig")).group(1).split(".")[:3])
prefix='GearLab_'+version;stage=dest/prefix
release='--release' in sys.argv[4:]
source_changes=subprocess.check_output(['git','status','--porcelain','--untracked-files=no'],cwd=root,text=True).strip()
source_dirty=bool(source_changes)
if release:
    assert os.environ.get('GITHUB_REPOSITORY')=='mikko2281/GearLab' and os.environ.get('GITHUB_REF')=='refs/heads/main'
    assert len(sys.argv)>3 and sys.argv[3]==os.environ.get('GITHUB_SHA')
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==sys.argv[3] and not source_dirty,'Release checkout does not match committed sources: '+source_changes
def exe_version(path):
    api=ctypes.windll.version
    size=api.GetFileVersionInfoSizeW(ctypes.c_wchar_p(str(path)),None)
    if not size:raise RuntimeError('EXE has no version resource')
    data=ctypes.create_string_buffer(size)
    if not api.GetFileVersionInfoW(ctypes.c_wchar_p(str(path)),0,size,data):raise RuntimeError('Cannot read EXE version')
    pointer=ctypes.c_void_p();length=ctypes.c_uint()
    if not api.VerQueryValueW(data,ctypes.c_wchar_p('\\'),ctypes.byref(pointer),ctypes.byref(length)):raise RuntimeError('Cannot read fixed EXE version')
    fixed=ctypes.cast(pointer,ctypes.POINTER(ctypes.c_uint32))
    return '.'.join(str(x) for x in [fixed[2]>>16,fixed[2]&65535,fixed[3]>>16,fixed[3]&65535])
assert exe_version(binary/'GearLab.exe')==version+'.0','Source and EXE versions disagree'
assert not (root/'src/AuthorDesign65.cs').exists()
assert not (root/'examples/Manipulator_1000mm.robot.json').exists()
assert (root/'examples/Industrial_1000mm.robot.json').is_file()
if stage.exists() or (dest/(prefix+'_Windows.zip')).exists():raise SystemExit('Preserve existing package; select a new output directory')
dest.mkdir(parents=True,exist_ok=True);stage.mkdir()
def include(p):
    parts=p.relative_to(root).parts
    if len(parts)>=3 and parts[:2]==('docs','verification') and parts[2].startswith(('6.3','6.4')) and p.name in {'broken.json','workspace-en.png','preferences-en.png','train-en.png','visible-en.txt'}:return False
    if '__pycache__' in parts or p.suffix=='.pyc' or any(x.startswith('profile') for x in parts):return False
    if parts[:3]==('docs','verification','6.7.2'):
        return p.name.endswith(('-results.json','-results.txt')) or p.name=='README.md'
    if len(parts)>=3 and parts[:2]==('docs','verification') and parts[2].startswith('6.7'):
        return p.name.endswith(('-results.json','-results.txt')) or p.name=='README.md' or (p.suffix=='.png' and p.suffix=='.png')
    if len(parts)>=3 and parts[:2]==('docs','verification') and parts[2].startswith(('6.0','6.1','6.2','6.3','6.4','6.5','6.6')):
        return p.suffix.lower() in {'.json','.png','.md','.txt'} and '.assembly.' not in p.name and not any(x.startswith('native-') or x.startswith('cache-') or x=='audit-toy' for x in parts[3:-1]) and (p.suffix.lower()!='.json' or p.stat().st_size<2000000)
    return True
def copy_dir(name):
    for p in (root/name).rglob('*'):
        if p.is_file() and include(p):
            q=stage/p.relative_to(root);q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
for p in binary.iterdir():
    if p.is_file():shutil.copy2(p,stage/p.name)
for name in ['runtime','kernel','web','licenses','docs','examples','Для_Fusion']:copy_dir(name)
for name in ['AGENTS.md','README.md','FREEWARE.txt','НАЧНИТЕ_ЗДЕСЬ.txt']:shutil.copy2(root/name,stage/name)
for mode,arg in {'Reducers':'--reducer','Robotics':'--robot','Stages':'--train','Settings':'--settings'}.items():(stage/(mode+'.cmd')).write_text('@echo off\r\nstart "" "%~dp0GearLab.exe" '+arg+'\r\n',encoding='ascii',newline='')
(stage/'Reference.cmd').write_text('@echo off\r\nstart "" "%~dp0GearLab.exe" --robot "%~dp0examples\\Reference67.robot.json"\r\n',encoding='ascii',newline='')
(stage/'Industrial.cmd').write_text('@echo off\r\nstart "" "%~dp0GearLab.exe" --robot "%~dp0examples\\Industrial_1000mm.robot.json"\r\n',encoding='ascii',newline='')
tools=stage/'tools';tools.mkdir(exist_ok=True);shutil.copy2(root/'tools/Check-Windows.ps1',tools)
for name in ['LibTessDotNet','WebView2']:
    target=stage/'licenses'/name;target.mkdir(exist_ok=True)
    for p in (root/'src/vendor'/name).glob('*.txt'):shutil.copy2(p,target/p.name)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
info={'Version':version,'Signature':'NotSigned','LocalReviewOnly':not release,'Sha256':sha(stage/'GearLab.exe'),'SourceCommit':sys.argv[3] if len(sys.argv)>3 else subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),'SourceDirty':source_dirty,'TrustedLaunchGuaranteed':False,'ReadyForManufacture':False,'ReleaseType':('GitHub preliminary release' if release else 'Local review build')+'; robot NOT manufacturing ready; see docs/RELEASE_'+version+'.md','Evidence':'Local GUI results describe the tested desktop binary; CI runs headless/native tests separately.'}
(stage/'build-info.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
(stage/'SHA256SUMS.txt').write_text('\n'.join(sha(p)+'  '+p.relative_to(stage).as_posix() for p in sorted(stage.rglob('*')) if p.is_file())+'\n',encoding='utf-8')
def archive(path,items,base,pref):
    with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for p in items:z.write(p,Path(pref)/p.relative_to(base))
    with zipfile.ZipFile(path) as z:assert z.testzip() is None
    path.with_suffix(path.suffix+'.sha256').write_text(sha(path)+'  '+path.name+'\n',encoding='ascii')
win=dest/(prefix+'_Windows.zip');archive(win,[p for p in sorted(stage.rglob('*')) if p.is_file()],stage,prefix)
source=[]
for name in ['.github','src','kernel','web','tools','docs','examples','licenses','Для_Fusion']:
    source.extend(p for p in (root/name).rglob('*') if p.is_file() and include(p))
source.extend(root/name for name in ['AGENTS.md','README.md','FREEWARE.txt','НАЧНИТЕ_ЗДЕСЬ.txt','test.ps1','.gitattributes','.gitignore'])
src=dest/(prefix+'_source.zip');archive(src,sorted(source),root,prefix+'_source')
(dest/(prefix+'_SHA256SUMS.txt')).write_text('\n'.join(sha(p)+'  '+p.name for p in (win,src))+'\n',encoding='ascii')
print(json.dumps({'Stage':str(stage),'WindowsBytes':win.stat().st_size,'SourceBytes':src.stat().st_size,'ExeSHA256':info['Sha256']},indent=2))
