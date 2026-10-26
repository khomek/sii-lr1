param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'TestResults'))
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1') -Configuration Release
$exe = Join-Path $PSScriptRoot 'src\FaceEyeLab\bin\x64\Release\net9.0-windows\win-x64\FaceEyeLab.exe'
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$process = Start-Process -FilePath $exe -ArgumentList @('--self-test', ('"' + $outputPath + '"')) -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath (Join-Path $outputPath 'verification.json')
if ($process.ExitCode -ne 0) { throw 'Проверки не пройдены. См. verification.json.' }
Write-Host 'Все проверки пройдены. Проверка физической камеры выполняется вручную.'
