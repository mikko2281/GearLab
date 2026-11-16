param([Parameter(Mandatory=$true)][string]$ReleaseId)
$ErrorActionPreference='Stop'
$gearRoot=Split-Path -Parent $PSScriptRoot
$gearRepo=$env:GITHUB_REPOSITORY
if($gearRepo -ne 'mikko2281/GearLab' -or $ReleaseId -notmatch '^\d+$'){throw 'Unexpected repository or draft ID.'}
$gearAssembly=Get-Content -LiteralPath (Join-Path $gearRoot 'src/AssemblyInfo.cs') -Raw
if($gearAssembly -notmatch 'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.0"\)'){throw 'Invalid source version.'}
$gearVersion=$Matches[1]
$gearTag='v'+$gearVersion
$gearPrefix='GearLab-'+$gearVersion
Push-Location $gearRoot
try {
    $gearRelease=(gh api ('repos/'+$gearRepo+'/releases/'+$ReleaseId) | ConvertFrom-Json)
    if($LASTEXITCODE -ne 0 -or -not $gearRelease.draft -or $gearRelease.tag_name -ne $gearTag){throw 'Refusing to alter a published or unexpected release.'}
    & ./tools/Package-Windows.ps1 -BinaryFolder Windows -OutputDirectory dist
    & ./runtime/python.exe -B -X utf8 tools/verify_package.py ('dist/'+$gearPrefix) ('dist/'+$gearPrefix+'-windows.zip')
    if($LASTEXITCODE -ne 0){throw 'Portable verification failed.'}
    # Recheck immediately before uploading; never replace an existing asset.
    $gearRelease=(gh api ('repos/'+$gearRepo+'/releases/'+$ReleaseId) | ConvertFrom-Json)
    if($LASTEXITCODE -ne 0 -or -not $gearRelease.draft -or $gearRelease.tag_name -ne $gearTag){throw 'Draft changed during packaging.'}
    $gearAssets=@('-windows.zip','-source.zip','-windows.zip.sha256','-source.zip.sha256','-SHA256SUMS.txt') | ForEach-Object {$gearPrefix+$_}
    foreach($gearName in $gearAssets){if($gearRelease.assets | Where-Object name -eq $gearName){throw ('Asset already exists: '+$gearName)}}
    $gearPaths=@($gearAssets | ForEach-Object {'dist/'+$_})
    gh release upload $gearTag --repo $gearRepo @gearPaths
    if($LASTEXITCODE -ne 0){throw 'Release asset upload failed.'}
    $gearCheck=(gh api ('repos/'+$gearRepo+'/releases/'+$ReleaseId) | ConvertFrom-Json)
    if($LASTEXITCODE -ne 0 -or -not $gearCheck.draft){throw 'Draft verification failed.'}
    foreach($gearName in @(($gearPrefix+'-windows.zip'),($gearPrefix+'-source.zip'))){
        $gearAsset=$gearCheck.assets | Where-Object name -eq $gearName
        $gearExpected='sha256:'+(Get-FileHash -LiteralPath ('dist/'+$gearName)).Hash.ToLowerInvariant()
        if($gearAsset.state -ne 'uploaded' -or $gearAsset.digest -ne $gearExpected){throw ('Server checksum mismatch: '+$gearName)}
    }
    Write-Output 'PASS: verified packages attached; release remains a draft.'
} finally {Pop-Location}
