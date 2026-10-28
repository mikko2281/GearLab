$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & .\src\build.ps1
    $gearExe = Join-Path $PSScriptRoot 'Windows/GearLab.exe'
    foreach ($gearSuite in @('self', 'placement', 'viewer', 'visibility', 'preset', 'micro', 'print')) {
        $gearOutput = Join-Path $PSScriptRoot ('test-results/' + $gearSuite)
        New-Item -ItemType Directory -Force -Path $gearOutput | Out-Null
        $gearProcess = Start-Process -FilePath $gearExe -ArgumentList @('--' + $gearSuite + '-test', ('"' + $gearOutput + '"')) -WindowStyle Hidden -PassThru -Wait
        if ($gearProcess.ExitCode -ne 0) {
            Get-ChildItem -LiteralPath $gearOutput -Filter '*error*' | ForEach-Object { Get-Content -LiteralPath $_.FullName }
            throw ('Failed test suite: ' + $gearSuite)
        }
        Write-Output ('PASS: ' + $gearSuite)
    }
} finally { Pop-Location }
