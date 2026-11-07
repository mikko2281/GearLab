param([string]$OutputFolder='Windows',[string]$ResultsFolder='docs/verification/6.6.0')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
 $out=Join-Path $root $ResultsFolder
 New-Item -ItemType Directory -Force $out | Out-Null
 foreach($suite in @('industrial66','optimization641','workspace64','workspace','settings63','upgrade62','audit-fix','transmission60','train60','robot-transmission60','ui60','ui')) {
  $env:GEARLAB_PROFILE_DIR=Join-Path $out ('profile-'+$suite)
  $p=Start-Process -FilePath (Join-Path $root ($OutputFolder+'/GearLab.exe')) -ArgumentList @("--$suite-test", ('"'+$out+'"')) -PassThru -WindowStyle Hidden
  if(-not $p.WaitForExit(300000)){$p.Kill();throw "Test timeout: $suite"}
  $p.Refresh()
  if($p.ExitCode -ne 0){Get-ChildItem $out -Filter '*error*.txt'|ForEach-Object {Get-Content $_.FullName};throw "Test failed: $suite"}
  Write-Output "PASS $suite"
 }
 node tools/test_render_scheduler.mjs
 if($LASTEXITCODE -ne 0){throw 'Render scheduler checks failed'}
 foreach($js in @('render-scheduler','scene','joint-controller')) {node --check ("web/$js.js");if($LASTEXITCODE -ne 0){throw "JS syntax failed: $js"}}
} finally {Pop-Location}
