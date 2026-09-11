[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'src\CatosHoverInspector\CatosHoverInspector.csproj'
$dllPath = Join-Path $repoRoot 'src\CatosHoverInspector\bin\Release\net48\net48\CatosHoverInspector.dll'
$manifestPath = Join-Path $repoRoot 'thunderstore\manifest.json'
$readmePath = Join-Path $repoRoot 'thunderstore\README.md'
$iconPath = Join-Path $repoRoot 'thunderstore\icon.png'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))

foreach ($requiredPath in @($project, $manifestPath, $readmePath, $iconPath, $changelogPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required package file is missing: $requiredPath"
    }
}

$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.name -ne 'CatosHoverInspector') {
    throw "Unexpected package name '$($manifest.name)'."
}
if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Manifest version_number must use MAJOR.MINOR.PATCH format.'
}

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "CatosHoverInspector build failed with exit code $LASTEXITCODE."
}
if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
    throw "Expected release DLL is missing: $dllPath"
}

$packageRoot = Join-Path $artifactsRoot "package-$($manifest.version_number)"
$outputPath = Join-Path $artifactsRoot "CatosHoverInspector-$($manifest.version_number).zip"
New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }
if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Force }
New-Item -ItemType Directory -Path $packageRoot | Out-Null

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $packageRoot 'CatosHoverInspector.dll')
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $packageRoot 'manifest.json')
Copy-Item -LiteralPath $readmePath -Destination (Join-Path $packageRoot 'README.md')
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $packageRoot 'icon.png')
Copy-Item -LiteralPath $changelogPath -Destination (Join-Path $packageRoot 'CHANGELOG.md')
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $outputPath -CompressionLevel Optimal -Force

$hash = Get-FileHash -LiteralPath $outputPath -Algorithm SHA256
Write-Host "Created $outputPath"
Write-Host "SHA256: $($hash.Hash)"
