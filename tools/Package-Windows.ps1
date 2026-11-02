param(
    [string]$BinaryFolder='Windows',
    [string]$OutputDirectory,
    [switch]$RequireTrustedSignature,
    [switch]$ResumeIncomplete
)
$ErrorActionPreference = 'Stop'
$gearRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $gearRoot 'dist' }
$gearExe = Join-Path $gearRoot ($BinaryFolder + '/GearLab.exe')
$gearSignature = Get-AuthenticodeSignature -LiteralPath $gearExe
if ($RequireTrustedSignature -and $gearSignature.Status -ne 'Valid') { throw ('Trusted release requires Valid Authenticode signature; found ' + $gearSignature.Status) }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$gearOutput = (Resolve-Path -LiteralPath $OutputDirectory).Path
$gearStage = Join-Path $gearOutput 'GearLab_5.4'
$gearZip = Join-Path $gearOutput 'GearLab_5.4_Windows.zip'
if (Test-Path -LiteralPath $gearZip) { throw 'Archive already exists. Select a new output directory.' }
if (Test-Path -LiteralPath $gearStage) {
    if (-not $ResumeIncomplete -or (Test-Path -LiteralPath (Join-Path $gearStage 'build-info.json'))) { throw 'Output exists; only an incomplete build can be resumed explicitly.' }
    $gearPartialExe = Join-Path $gearStage 'GearLab.exe'
    if (-not (Test-Path -LiteralPath $gearPartialExe) -or (Get-FileHash -LiteralPath $gearPartialExe).Hash -ne (Get-FileHash -LiteralPath $gearExe).Hash) { throw 'Incomplete stage does not match this exact executable.' }
}
New-Item -ItemType Directory -Force -Path $gearStage | Out-Null
Copy-Item -LiteralPath $gearExe,($gearExe + '.config') -Destination $gearStage
foreach ($gearDll in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll')) { Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $gearExe) $gearDll) -Destination $gearStage }
Copy-Item -LiteralPath (Join-Path $gearRoot 'README.md'),(Join-Path $gearRoot 'FREEWARE.txt'),(Join-Path $gearRoot 'НАЧНИТЕ_ЗДЕСЬ.txt') -Destination $gearStage
foreach ($gearName in @('docs','examples','Для_Fusion','runtime','licenses','web')) { Copy-Item -LiteralPath (Join-Path $gearRoot $gearName) -Destination $gearStage -Recurse -Force }
$gearWebNotices = New-Item -ItemType Directory -Force -Path (Join-Path $gearStage 'licenses/WebView2')
Copy-Item -LiteralPath (Join-Path $gearRoot 'src/vendor/WebView2/LICENSE.txt'),(Join-Path $gearRoot 'src/vendor/WebView2/NOTICE.txt') -Destination $gearWebNotices.FullName
$gearKernelStage = New-Item -ItemType Directory -Force -Path (Join-Path $gearStage 'kernel')
Get-ChildItem -LiteralPath (Join-Path $gearRoot 'kernel') -Filter '*.py' -File | Copy-Item -Destination $gearKernelStage.FullName
$gearTools = New-Item -ItemType Directory -Force -Path (Join-Path $gearStage 'tools')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Check-Windows.ps1') -Destination $gearTools.FullName
$gearLicenses = Get-Item -LiteralPath (Join-Path $gearStage 'licenses')
Copy-Item -LiteralPath (Join-Path $gearRoot 'src/vendor/LibTessDotNet/LICENSE.txt'),(Join-Path $gearRoot 'src/vendor/LibTessDotNet/ORIGIN.txt') -Destination $gearLicenses.FullName
$gearModes = @{ 'Reducers'='--reducer'; 'Robotics'='--robot'; 'Mechanisms'='--mechanism'; 'Polygon'='--polygon'; 'Settings'='--settings' }
foreach ($gearName in $gearModes.Keys) {
    $gearCommand = '@echo off' + "`r`n" + 'start "" "%~dp0GearLab.exe" ' + $gearModes[$gearName] + "`r`n"
    [IO.File]::WriteAllText((Join-Path $gearStage ($gearName + '.cmd')), $gearCommand, [Text.Encoding]::ASCII)
}
$gearStatus = [ordered]@{Version='5.4.0';BuiltUtc=[DateTime]::UtcNow.ToString('o');Signature=[string]$gearSignature.Status;Sha256=(Get-FileHash -LiteralPath $gearExe -Algorithm SHA256).Hash;Requirement='Windows 10/11 x64, .NET Framework 4.8; bundled isolated Python/OCCT; Three.js uses WebView2 Runtime with WPF fallback';TrustedLaunchGuaranteed=$false}
$gearStatus | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $gearStage 'build-info.json') -Encoding UTF8
$gearHashes = Get-ChildItem -LiteralPath $gearStage -Recurse -File | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash + '  ' + $_.FullName.Substring($gearStage.Length+1).Replace('\','/') }
$gearHashes | Set-Content -LiteralPath (Join-Path $gearStage 'SHA256SUMS.txt') -Encoding UTF8
Compress-Archive -LiteralPath $gearStage -DestinationPath $gearZip -CompressionLevel Optimal
((Get-FileHash -LiteralPath $gearZip -Algorithm SHA256).Hash + '  ' + [IO.Path]::GetFileName($gearZip)) | Set-Content -LiteralPath ($gearZip + '.sha256') -Encoding ASCII
Write-Output $gearStage
Write-Output $gearZip
