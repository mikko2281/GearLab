param([Parameter(Mandatory=$true)][string]$ReleaseId)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
$OutputEncoding=[Console]::OutputEncoding
$gearRoot=Split-Path -Parent $PSScriptRoot
$gearRepo=$env:GITHUB_REPOSITORY
if($gearRepo -ne 'mikko2281/GearLab' -or $ReleaseId -notmatch '^\d+$'){throw 'Unexpected repository or draft ID.'}
Push-Location $gearRoot
try {
    $gearRaw=gh api ('repos/'+$gearRepo+'/releases/'+$ReleaseId)
    if($LASTEXITCODE -ne 0){throw 'Cannot read the existing draft.'}
    $gearRelease=($gearRaw -join "`n") | ConvertFrom-Json
    if(-not $gearRelease.draft -or $gearRelease.tag_name -ne 'v5.4.0'){throw 'Refusing to alter a published or unexpected release.'}
    $gearRequest=Join-Path $env:RUNNER_TEMP 'gearlab-draft-update.json'
    [IO.File]::WriteAllText($gearRequest,($gearRaw -join "`n"),(New-Object Text.UTF8Encoding($false)))
    python -X utf8 tools/write_formula_reference.py
    if($LASTEXITCODE -ne 0){throw 'Formula snapshot failed.'}
    python -X utf8 tools/prepare_ci_release.py $gearRequest $env:GITHUB_SHA
    if($LASTEXITCODE -ne 0){throw 'CI verification metadata failed.'}
    & ./tools/Package-Windows.ps1 -BinaryFolder Windows -OutputDirectory dist
    python -X utf8 tools/Package-Source.py dist
    if($LASTEXITCODE -ne 0){throw 'Source package failed.'}
    python -X utf8 tools/verify_package.py dist/GearLab_5.4 dist/GearLab_5.4_Windows.zip
    if($LASTEXITCODE -ne 0){throw 'Portable verification failed.'}
    $gearLines=@()
    foreach($gearName in @('GearLab_5.4_Windows.zip','GearLab_5.4_source.zip')){$gearLines+=(Get-FileHash -LiteralPath ('dist/'+$gearName)).Hash+'  '+$gearName}
    $gearLines+=(Get-FileHash -LiteralPath 'dist/GearLab_5.4/GearLab.exe').Hash+'  GearLab_5.4/GearLab.exe'
    [IO.File]::WriteAllLines((Join-Path $gearRoot 'dist/GearLab_5.4_SHA256SUMS.txt'),$gearLines,[Text.Encoding]::ASCII)
    gh api --method PATCH ('repos/'+$gearRepo+'/releases/'+$ReleaseId) --input $gearRequest | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Draft metadata update failed.'}
    gh release upload v5.4.0 --repo $gearRepo --clobber dist/GearLab_5.4_Windows.zip dist/GearLab_5.4_source.zip dist/GearLab_5.4_Windows.zip.sha256 dist/GearLab_5.4_source.zip.sha256 dist/GearLab_5.4_SHA256SUMS.txt dist/GearLab_5.4/build-info.json
    if($LASTEXITCODE -ne 0){throw 'Release asset upload failed.'}
    $gearCheck=(gh api ('repos/'+$gearRepo+'/releases/'+$ReleaseId) | ConvertFrom-Json)
    foreach($gearName in @('GearLab_5.4_Windows.zip','GearLab_5.4_source.zip')) {
        $gearAsset=$gearCheck.assets | Where-Object name -eq $gearName
        $gearExpected='sha256:'+(Get-FileHash -LiteralPath ('dist/'+$gearName)).Hash.ToLowerInvariant()
        if($gearAsset.state -ne 'uploaded' -or $gearAsset.digest -ne $gearExpected){throw ('Server checksum mismatch: '+$gearName)}
    }
    Write-Output 'PASS: verified packages attached; release remains a draft for final publication.'
} finally { Pop-Location }
