param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '0.1.0',
    [string]$ProjectPath,
    [string]$OutputRoot,
    [bool]$SelfContained = $true,
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot 'src\DepSphere.App\DepSphere.App.csproj'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot 'artifacts\publish'
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    throw "Project not found: $ProjectPath"
}

$publishDir = Join-Path $OutputRoot "DepSphere.App\$Configuration\$Runtime\$Version"
if ($Clean -and (Test-Path -LiteralPath $publishDir)) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

$publishArgs = @(
    'publish',
    $ProjectPath,
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', $SelfContained.ToString().ToLowerInvariant(),
    '-p:PublishSingleFile=false',
    '-p:EnableWindowsTargeting=true',
    "-p:Version=$Version",
    '-o', $publishDir
)

Write-Host "[build-app] dotnet $($publishArgs -join ' ')"
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "[build-app] Publish completed: $publishDir"
Write-Output $publishDir
