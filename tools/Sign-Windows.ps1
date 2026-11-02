param(
    [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
    [Parameter(Mandatory=$true)][uri]$TimestampServer,
    [string]$BinaryFolder='Windows'
)
$ErrorActionPreference = 'Stop'
$gearRoot = Split-Path -Parent $PSScriptRoot
$gearExe = Join-Path $gearRoot ($BinaryFolder + '/GearLab.exe')
if (-not (Test-Path -LiteralPath $gearExe)) { throw 'Build GearLab first.' }
$gearCert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object Thumbprint -eq $CertificateThumbprint
if (@($gearCert).Count -ne 1 -or -not $gearCert.HasPrivateKey) { throw 'A code-signing certificate with its private key is required.' }
$gearChain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
try {
    if (-not $gearChain.Build($gearCert)) { throw 'Certificate chain is not trusted. Self-signed certificates cannot be used for this release.' }
} finally { $gearChain.Dispose() }
if ($gearCert.NotAfter -le (Get-Date) -or $gearCert.NotBefore -gt (Get-Date)) { throw 'Certificate is outside its validity period.' }
$gearSignature = Set-AuthenticodeSignature -LiteralPath $gearExe -Certificate $gearCert -HashAlgorithm SHA256 -IncludeChain NotRoot -TimestampServer $TimestampServer.AbsoluteUri
if ($gearSignature.Status -ne 'Valid' -or -not $gearSignature.TimeStamperCertificate) { throw ('Signing or timestamp failed: ' + $gearSignature.StatusMessage) }
$gearVerified = Get-AuthenticodeSignature -LiteralPath $gearExe
if ($gearVerified.Status -ne 'Valid') { throw ('Signature validation failed: ' + $gearVerified.Status) }
Write-Output ('Valid signed binary: ' + $gearExe)
