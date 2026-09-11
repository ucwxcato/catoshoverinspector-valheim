[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'tests\EtaFormatterTests\EtaFormatterTests.csproj'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "ETA test project is missing: $project"
}

dotnet run --project $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "ETA formatter tests failed with exit code $LASTEXITCODE."
}
