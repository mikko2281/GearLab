param([string]$BinaryFolder='Windows-fix')
$ErrorActionPreference='Stop'
$fixRoot=Split-Path -Parent $PSScriptRoot
$fixResults=Join-Path $fixRoot 'test-results-fixes/robot-full'
New-Item -ItemType Directory -Force $fixResults|Out-Null
$fixRows=@()
foreach($suite in @('connected','blender')) {
 $folder=Join-Path $fixResults $suite
 New-Item -ItemType Directory -Force $folder|Out-Null
 # Reuse only this user's retained audit-generated fixtures. The suite itself
 # validates fingerprints and regenerates every stale/missing native component.
 $old=Join-Path (Split-Path $fixRoot) "program-audit-2026-11-05/extended/$suite/SixAxis_Connected.robot.json"
 if(Test-Path -LiteralPath $old){Copy-Item -LiteralPath $old -Destination (Join-Path $folder 'SixAxis_Connected.robot.json')}
 $clock=[Diagnostics.Stopwatch]::StartNew()
 $process=Start-Process -FilePath (Join-Path $fixRoot "$BinaryFolder/GearLab.exe") -ArgumentList @("--$suite-test",('"'+$folder+'"')) -WindowStyle Hidden -PassThru
 $done=$process.WaitForExit(1200000)
 if($done){$process.Refresh();$code=$process.ExitCode}else{$process.Kill($true);$code=-999}
 $fixRows+=@{Suite=$suite;ExitCode=$code;Seconds=$clock.Elapsed.TotalSeconds;Timeout=(-not $done)}
 $fixRows|ConvertTo-Json -Depth 5|Set-Content -Encoding utf8 (Join-Path $fixResults 'status.json')
 Write-Output "$suite : $code"
}
Set-Content (Join-Path $fixResults 'complete.txt') 'Complete'
