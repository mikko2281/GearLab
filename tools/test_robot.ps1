param(
 [string]$OutputFolder='Windows',
 [string]$ResultsFolder='test-results/robot',
 [string[]]$Suites=@('add-link673','robot','robot-transmission60','controller','procedural','connected','reference67','reference67-native','industrial66','gripper671-core','gripper671','robot-lifecycle672-core','robot-lifecycle672','print-export672','procedural-cancel672','stability672-core','stability672','web-preferences672')
)
$ErrorActionPreference='Stop'
$gearRoot=Split-Path -Parent $PSScriptRoot
$gearProfileBefore=$env:GEARLAB_PROFILE_DIR
Push-Location $gearRoot
try {
 foreach($gearSuite in $Suites){
  $gearOut=Join-Path $gearRoot ($ResultsFolder+'/'+$gearSuite)
  New-Item -ItemType Directory -Force $gearOut | Out-Null
  $env:GEARLAB_PROFILE_DIR=Join-Path $gearOut 'profile'
  if($gearSuite -eq 'connected'){Copy-Item -LiteralPath examples/SixAxis_Connected.robot.json -Destination $gearOut}
  $gearProcess=Start-Process -FilePath (Join-Path $gearRoot ($OutputFolder+'/GearLab.exe')) -ArgumentList @("--$gearSuite-test",('"'+$gearOut+'"')) -WindowStyle Hidden -PassThru
  if(-not $gearProcess.WaitForExit(1200000)){$gearProcess.Kill();throw "Test timeout: $gearSuite"}
  $gearProcess.Refresh()
  if($gearProcess.ExitCode -ne 0){Get-ChildItem $gearOut -Filter '*error*.txt' | ForEach-Object {Get-Content -LiteralPath $_.FullName};throw "Failed robot suite: $gearSuite"}
  Write-Output "PASS: $gearSuite"
 }
} finally {$env:GEARLAB_PROFILE_DIR=$gearProfileBefore;Pop-Location}
