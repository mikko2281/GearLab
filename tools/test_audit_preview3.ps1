param([string]$BinaryFolder='Windows-fix',[string]$ResultsFolder='test-results-fixes/regression')
$ErrorActionPreference='Stop'
$fixRoot=Split-Path -Parent $PSScriptRoot
$fixResults=Join-Path $fixRoot $ResultsFolder
New-Item -ItemType Directory -Force $fixResults|Out-Null
$fixRows=@()
foreach($suite in @('transmission60','train60','robot-transmission60','ui60','workspace','self','mechanism','robot','step','placement','preset','micro','print','controller','reducer56','viewer','visibility','procedural','author-view','reference','ui')) {
 $folder=Join-Path $fixResults $suite
 New-Item -ItemType Directory -Force $folder|Out-Null
 $clock=[Diagnostics.Stopwatch]::StartNew()
 $process=Start-Process -FilePath (Join-Path $fixRoot "$BinaryFolder/GearLab.exe") -ArgumentList @("--$suite-test",('"'+$folder+'"')) -WindowStyle Hidden -PassThru
 $done=$process.WaitForExit(900000)
 if($done){$process.Refresh();$code=$process.ExitCode}else{$process.Kill($true);$code=-999}
 $fixRows+=@{Suite=$suite;ExitCode=$code;Seconds=$clock.Elapsed.TotalSeconds;Timeout=(-not $done)}
 $fixRows|ConvertTo-Json -Depth 5|Set-Content -Encoding utf8 (Join-Path $fixResults 'status.json')
 Write-Output "$suite : $code"
}
Push-Location $fixRoot
try {
 foreach($name in @('test_cad_kernel','test_loads_controls','test_body_motion','test_robot_step_edit','test_wave_flat','test_transmission_audit60')) {
  $clock=[Diagnostics.Stopwatch]::StartNew()
  & ./runtime/python.exe -B -X utf8 -I "tools/$name.py" (Join-Path $fixResults $name) *> (Join-Path $fixResults "$name.log")
  $fixRows+=@{Suite=$name;ExitCode=$LASTEXITCODE;Seconds=$clock.Elapsed.TotalSeconds}
  $fixRows|ConvertTo-Json -Depth 5|Set-Content -Encoding utf8 (Join-Path $fixResults 'status.json')
 }
 & node --experimental-vm-modules tools/test_joint_controller.mjs (Join-Path $fixResults 'gizmo') *> (Join-Path $fixResults 'gizmo.log')
 $fixRows+=@{Suite='gizmo';ExitCode=$LASTEXITCODE}
 $fixRows|ConvertTo-Json -Depth 5|Set-Content -Encoding utf8 (Join-Path $fixResults 'status.json')
} finally {Pop-Location}
Set-Content (Join-Path $fixResults 'complete.txt') 'Complete'
