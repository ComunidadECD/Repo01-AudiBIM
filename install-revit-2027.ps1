$ErrorActionPreference = 'Stop'

$sourceManifest = Join-Path $PSScriptRoot 'BIMQualityAuditor.Revit2027.addin'
$sourceDirectory = Join-Path $PSScriptRoot 'bin\Revit2027\Release\net10.0-windows'
$sourceAssembly = Join-Path $sourceDirectory 'BIMQualityAuditor.dll'
$targetDirectory = Join-Path $env:APPDATA 'Autodesk\Revit\Addins\2027'
$targetAssemblyDirectory = Join-Path $targetDirectory 'AudiBIM'
$targetAssembly = Join-Path $targetAssemblyDirectory 'BIMQualityAuditor.dll'
$targetManifest = Join-Path $targetDirectory 'BIMQualityAuditor.addin'

if (-not (Test-Path -LiteralPath $sourceAssembly)) {
    throw 'No existe la DLL para Revit 2027. Ejecute primero build-revit-2027.ps1.'
}

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $targetAssemblyDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $sourceDirectory '*') -Destination $targetAssemblyDirectory -Recurse -Force

[xml]$manifest = Get-Content -LiteralPath $sourceManifest
$manifest.RevitAddIns.AddIn.Assembly = $targetAssembly
$manifest.Save($targetManifest)

Write-Host "AudiBIM instalado para Revit 2027: $targetManifest" -ForegroundColor Green
