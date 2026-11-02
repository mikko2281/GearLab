param([string]$ExePath=(Join-Path (Split-Path -Parent $PSScriptRoot) 'GearLab.exe'))
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $ExePath)) { $ExePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'Windows/GearLab.exe' }
$gearRelease = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
$gearSignature = if (Test-Path -LiteralPath $ExePath) { Get-AuthenticodeSignature -LiteralPath $ExePath } else { $null }
[pscustomobject]@{
    Windows = [Environment]::OSVersion.VersionString
    DotNet48OrLater = ($null -ne $gearRelease -and $gearRelease -ge 528040)
    BinaryFound = (Test-Path -LiteralPath $ExePath)
    ConfigurationFound = (Test-Path -LiteralPath ($ExePath + '.config'))
    Is64BitWindows = [Environment]::Is64BitOperatingSystem
    CadRuntimeFound = (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $ExePath) 'runtime/python.exe'))
    CadKernelFound = (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $ExePath) 'kernel/cad_kernel.py'))
    WebSceneFound = (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $ExePath) 'web/index.html'))
    WebView2LoaderFound = (Test-Path -LiteralPath (Join-Path (Split-Path -Parent $ExePath) 'WebView2Loader.dll'))
    Signature = if ($gearSignature) { [string]$gearSignature.Status } else { 'MissingBinary' }
    Publisher = if ($gearSignature -and $gearSignature.SignerCertificate) { $gearSignature.SignerCertificate.Subject } else { 'Unsigned' }
    SHA256 = if (Test-Path -LiteralPath $ExePath) { (Get-FileHash -LiteralPath $ExePath -Algorithm SHA256).Hash } else { '' }
    Note = 'Read-only diagnosis. Windows security policy and reputation may impose additional requirements.'
} | Format-List
