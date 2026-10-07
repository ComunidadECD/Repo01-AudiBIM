$ErrorActionPreference = 'Stop'

$revitApi = 'C:\Program Files\Autodesk\Revit 2027\RevitAPI.dll'
$revitApiUi = 'C:\Program Files\Autodesk\Revit 2027\RevitAPIUI.dll'

if (-not (Test-Path -LiteralPath $revitApi) -or -not (Test-Path -LiteralPath $revitApiUi)) {
    throw 'No se encontró la API de Revit 2027. Instale Autodesk Revit 2027 antes de compilar.'
}

$hasNet10 = dotnet --list-sdks | Select-String -Pattern '^10\.'
if (-not $hasNet10) {
    throw 'No se encontró el SDK de .NET 10. Instálelo antes de compilar para Revit 2027.'
}

dotnet build "$PSScriptRoot\BIMQualityAuditor.csproj" -c Release -p:RevitVersion=2027
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Compilación Revit 2027 completada.' -ForegroundColor Green
Write-Host "$PSScriptRoot\bin\Revit2027\Release\net10.0-windows\BIMQualityAuditor.dll"
