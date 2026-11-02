param([string]$OutputFolder='Windows',[string]$ResultsFolder='test-results',[switch]$NoBuild,[switch]$SkipProceduralKernel,[string[]]$Suites=@('reducer55','author-view','controller','self','mechanism','step','placement','preset','micro','print'))
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not $NoBuild) { & .\src\build.ps1 -OutputFolder $OutputFolder }
    $gearExe = Join-Path $PSScriptRoot ($OutputFolder + '/GearLab.exe')
    foreach ($gearSuite in $Suites) {
        $gearOutput = Join-Path $PSScriptRoot ($ResultsFolder + '/' + $gearSuite)
        New-Item -ItemType Directory -Force -Path $gearOutput | Out-Null
        $gearProcess = Start-Process -FilePath $gearExe -ArgumentList @('--' + $gearSuite + '-test', ('"' + $gearOutput + '"')) -WindowStyle Hidden -PassThru -Wait
        if ($gearProcess.ExitCode -ne 0) {
            Get-ChildItem -LiteralPath $gearOutput -Filter '*error*' | ForEach-Object { Get-Content -LiteralPath $_.FullName }
            throw ('Failed test suite: ' + $gearSuite)
        }
        Write-Output ('PASS: ' + $gearSuite)
    }
    & ./runtime/python.exe -I ./tools/test_cad_kernel.py (Join-Path $ResultsFolder 'kernel')
    if ($LASTEXITCODE -ne 0) { throw 'Native CAD kernel checks failed.' }
    & ./runtime/python.exe -B -X utf8 -I ./tools/test_robot_step_edit.py (Join-Path $ResultsFolder 'step-edit')
    if ($LASTEXITCODE -ne 0) { throw 'Exported robot STEP edit checks failed.' }
    & ./runtime/python.exe -B -X utf8 -I ./tools/test_loads_controls.py (Join-Path $ResultsFolder 'loads')
    if ($LASTEXITCODE -ne 0) { throw 'Combined section and sketch direction checks failed.' }
    if (Get-Command node -ErrorAction SilentlyContinue) {
        & node --experimental-vm-modules ./tools/test_joint_controller.mjs (Join-Path $ResultsFolder 'gizmo')
        if ($LASTEXITCODE -ne 0) { throw 'Joint gizmo checks failed.' }
    } else { Write-Warning 'Node.js unavailable; the separate gizmo-math suite was not run.' }
    & ./runtime/python.exe -X utf8 -I ./tools/test_body_motion.py (Join-Path $ResultsFolder 'motion')
    if ($LASTEXITCODE -ne 0) { throw 'CAD motion checks failed.' }
    if (-not $SkipProceduralKernel) {
        & ./runtime/python.exe -X utf8 -I ./tools/test_procedural_body.py (Join-Path $ResultsFolder 'procedural-kernel')
        if ($LASTEXITCODE -ne 0) { throw 'Procedural body CAD checks failed.' }
    }
} finally { Pop-Location }
