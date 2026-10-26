param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet restore .\FaceEyeLab.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось восстановить NuGet-пакеты.' }
    dotnet build .\FaceEyeLab.sln --no-restore -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Ошибка сборки.' }
} finally { Pop-Location }
