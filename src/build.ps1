param([string]$OutputFolder='Windows')
$ErrorActionPreference = 'Stop'
$gearRoot = Split-Path -Parent $PSScriptRoot
$gearOutput = Join-Path $gearRoot $OutputFolder
New-Item -ItemType Directory -Force -Path $gearOutput | Out-Null
$gearCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $gearCompiler)) {
    $gearCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $gearCompiler)) { throw 'Не найден компилятор .NET Framework 4.x.' }
$gearWpf = Join-Path (Split-Path $gearCompiler) 'WPF'
$gearSources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName) + @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'vendor/LibTessDotNet') -Filter '*.cs' | ForEach-Object FullName)
& $gearCompiler /nologo /target:winexe /platform:anycpu /optimize+ /define:DOUBLE "/out:$(Join-Path $gearOutput 'GearLab.exe')" "/win32manifest:$(Join-Path $PSScriptRoot 'app.manifest')" "/win32icon:$(Join-Path $PSScriptRoot 'GearLab.ico')" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Xaml.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "/reference:$(Join-Path $gearWpf 'PresentationCore.dll')" "/reference:$(Join-Path $gearWpf 'PresentationFramework.dll')" "/reference:$(Join-Path $gearWpf 'WindowsBase.dll')" "/reference:$(Join-Path $gearWpf 'WindowsFormsIntegration.dll')" $gearSources
if ($LASTEXITCODE -ne 0) { throw 'Ошибка сборки.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'GearLab.exe.config') -Destination $gearOutput -Force
Write-Output (Join-Path $gearOutput 'GearLab.exe')
