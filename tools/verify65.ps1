param([string]$OutputFolder='Windows-final',[string]$ResultsFolder='test-results-65-final')
$ErrorActionPreference='Stop'
$gearRoot=Split-Path -Parent $PSScriptRoot
Push-Location $gearRoot
try {
    $gearResults=Join-Path $gearRoot $ResultsFolder
    New-Item -ItemType Directory -Force $gearResults | Out-Null
    foreach($gearSuite in @('author65','optimization641','workspace64','workspace','settings63','upgrade62','audit-fix','transmission60','train60','robot-transmission60','ui60','ui')) {
        $env:GEARLAB_PROFILE_DIR=Join-Path $gearResults ('profile-'+$gearSuite)
        $gearProcess=Start-Process -FilePath (Join-Path $gearRoot ($OutputFolder+'/GearLab.exe')) -ArgumentList @("--$gearSuite-test",('"'+$gearResults+'"')) -PassThru -WindowStyle Hidden
        if(-not $gearProcess.WaitForExit(300000)){$gearProcess.Kill();throw "Timeout: $gearSuite"}
        $gearProcess.Refresh()
        if($gearProcess.ExitCode -ne 0){Get-ChildItem $gearResults -Filter '*error*.txt'|ForEach-Object {Get-Content $_.FullName};throw "Failed: $gearSuite"}
        Write-Output "PASS suite: $gearSuite"
    }
    node tools/test_render_scheduler.mjs
    if($LASTEXITCODE -ne 0){throw 'Render scheduler regression'}
} finally {Pop-Location}
