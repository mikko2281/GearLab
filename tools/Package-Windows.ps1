param(
    [string]$BinaryFolder='Windows',
    [string]$OutputDirectory,
    [switch]$RequireTrustedSignature,
    [switch]$ResumeIncomplete
)
$ErrorActionPreference='Stop'
$gearRoot=Split-Path -Parent $PSScriptRoot
if($ResumeIncomplete){throw 'Select a fresh output directory; completed and partial packages are preserved.'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $gearRoot 'dist'}
$gearExe=Join-Path $gearRoot ($BinaryFolder+'/GearLab.exe')
$gearSignature=Get-AuthenticodeSignature -LiteralPath $gearExe
if($RequireTrustedSignature -and $gearSignature.Status -ne 'Valid'){throw ('Trusted signature required; found '+$gearSignature.Status)}
$gearPython=Join-Path $gearRoot 'runtime/python.exe'
if(-not (Test-Path -LiteralPath $gearPython)){throw 'Bundled Python runtime missing.'}
Push-Location $gearRoot
try {
    & $gearPython -B -X utf8 (Join-Path $PSScriptRoot 'package_local.py') $OutputDirectory $BinaryFolder
    if($LASTEXITCODE -ne 0){throw 'Portable/source packaging failed.'}
} finally {Pop-Location}
