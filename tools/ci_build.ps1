param([string]$OutputFolder='Windows')
$ErrorActionPreference='Stop'
$gearRoot=Split-Path -Parent $PSScriptRoot
Push-Location $gearRoot
try {
    python -m pip install 'cadquery-ocp==8.0.1.1.0' 'vtk==9.6.2' 'pefile==2024.8.26'
    if ($LASTEXITCODE -ne 0) { throw 'CAD build dependencies failed' }
    python tools/fetch_viewport_dependencies.py
    if ($LASTEXITCODE -ne 0) { throw 'Viewport dependencies failed' }
    python tools/collect_native_sources.py
    if ($LASTEXITCODE -ne 0) { throw 'Native source archive download failed' }
    python tools/unpack_examples.py
    if ($LASTEXITCODE -ne 0) { throw 'Native example unpack failed' }
    python tools/bundle_cad_runtime.py runtime
    if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed' }
    & ./src/build.ps1 -OutputFolder $OutputFolder
} finally { Pop-Location }
