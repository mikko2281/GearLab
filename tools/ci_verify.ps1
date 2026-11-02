param([string]$OutputFolder='Windows',[string]$ResultsFolder='test-results-ci')

$ErrorActionPreference='Stop'

$gearRoot=Split-Path -Parent $PSScriptRoot

Push-Location $gearRoot

try {

    New-Item -ItemType Directory -Force -Path $ResultsFolder | Out-Null

    foreach($gearName in @('Robot1_800mm','SixAxis_Connected')) {

        $gearFolder=Join-Path $ResultsFolder ('cache-'+$gearName)

        New-Item -ItemType Directory -Force -Path $gearFolder | Out-Null

        $gearProject=Join-Path $gearFolder ($gearName+'.robot.json')

        Copy-Item -LiteralPath ('examples/'+$gearName+'.robot.json') -Destination $gearProject

        $gearProcess=Start-Process -FilePath (Join-Path $gearRoot ($OutputFolder+'/GearLab.exe')) -ArgumentList @('--cache-test',('"'+(Join-Path $gearRoot $gearProject)+'"')) -PassThru

        if(-not $gearProcess.WaitForExit(120000)) { $gearProcess.Kill();throw 'Non-UI executable cache check timed out.' }

        if($gearProcess.ExitCode -ne 0){throw ('Native project cache check failed: '+$gearName)}

        Get-Content -LiteralPath (Join-Path $gearFolder 'cache-results.txt')

    }

    $gearFastFolder=Join-Path $ResultsFolder 'fast-wave'
    New-Item -ItemType Directory -Force -Path $gearFastFolder | Out-Null
    $gearFastProcess=Start-Process -FilePath (Join-Path $gearRoot ($OutputFolder+'/GearLab.exe')) -ArgumentList @('--wave56-fast-test',('"'+(Join-Path $gearRoot $gearFastFolder)+'"')) -PassThru -WindowStyle Hidden
    if(-not $gearFastProcess.WaitForExit(30000)){$gearFastProcess.Kill();throw 'Fast wave preview timed out.'}
    if($gearFastProcess.ExitCode -ne 0){throw 'Fast wave preview failed.'}
    Get-Content -LiteralPath (Join-Path $gearFastFolder 'fast-preview-results.json')
    foreach($gearTest in @('cad_kernel','body_motion','loads_controls','robot_step_edit','wave_flat')) {

        & ./runtime/python.exe -B -X utf8 -I ('./tools/test_'+$gearTest+'.py') (Join-Path $ResultsFolder $gearTest)

        if($LASTEXITCODE -ne 0){throw ('Native check failed: '+$gearTest)}

    }

    node --experimental-vm-modules tools/test_joint_controller.mjs (Join-Path $ResultsFolder 'gizmo')

    if($LASTEXITCODE -ne 0){throw 'Shipped controller math checks failed.'}

    Write-Output 'PASS: executable caches, native CAD, motion bounds, sections, sketches, STEP editing and controller math.'

} finally { Pop-Location }

