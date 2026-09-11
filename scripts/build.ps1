[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'src\CatosHoverInspector\CatosHoverInspector.csproj'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "CatosHoverInspector project is not scaffolded yet: $project"
}

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "CatosHoverInspector build failed with exit code $LASTEXITCODE."
}

$dllPath = Join-Path $repoRoot 'src\CatosHoverInspector\bin\Release\net48\net48\CatosHoverInspector.dll'
if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Build completed but the expected newest DLL is missing: $dllPath"
}

$dll = Get-Item -LiteralPath $dllPath
Write-Host "Newest CatosHoverInspector DLL: $($dll.FullName)"
Write-Host "Built: $($dll.LastWriteTime.ToString('u'))"

