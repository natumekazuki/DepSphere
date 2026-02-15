param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '0.1.0',
    [string]$Publisher = 'DepSphere Team',
    [string]$PublishDir,
    [string]$OutputRoot,
    [string]$InnoScriptPath,
    [string]$IsccPath,
    [bool]$SelfContained = $true,
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if ([string]::IsNullOrWhiteSpace($InnoScriptPath)) {
    $InnoScriptPath = Join-Path $repoRoot 'installer\DepSphere.iss'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot 'artifacts\installer'
}

if (-not (Test-Path -LiteralPath $InnoScriptPath)) {
    throw "Inno Setup script not found: $InnoScriptPath"
}

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $buildAppScriptPath = Join-Path $PSScriptRoot 'build-app.ps1'
    if (-not (Test-Path -LiteralPath $buildAppScriptPath)) {
        throw "Build script not found: $buildAppScriptPath"
    }

    $PublishDir = & $buildAppScriptPath `
        -Configuration $Configuration `
        -Runtime $Runtime `
        -Version $Version `
        -SelfContained $SelfContained `
        -Clean:$Clean
}

$PublishDir = (Resolve-Path -LiteralPath $PublishDir).Path
if (-not (Test-Path -LiteralPath $PublishDir)) {
    throw "Publish directory not found: $PublishDir"
}

$installerOutputDir = Join-Path $OutputRoot "$Version"
if ($Clean -and (Test-Path -LiteralPath $installerOutputDir)) {
    Remove-Item -LiteralPath $installerOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $installerOutputDir -Force | Out-Null

function Resolve-IsccPath {
    param([string]$PreferredPath)

    if (-not [string]::IsNullOrWhiteSpace($PreferredPath)) {
        if (-not (Test-Path -LiteralPath $PreferredPath)) {
            throw "ISCC not found: $PreferredPath"
        }
        return (Resolve-Path -LiteralPath $PreferredPath).Path
    }

    $isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($isccCommand) {
        return $isccCommand.Source
    }

    $candidatePaths = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidatePaths) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'ISCC.exe not found. Install Inno Setup 6 or pass -IsccPath explicitly.'
}

$resolvedIsccPath = Resolve-IsccPath -PreferredPath $IsccPath

$isccArgs = @(
    "/DMyAppVersion=$Version",
    "/DMyAppPublisher=$Publisher",
    "/DPublishDir=$PublishDir",
    "/DOutputDir=$installerOutputDir",
    $InnoScriptPath
)

Write-Host "[build-installer] Using ISCC: $resolvedIsccPath"
Write-Host "[build-installer] iscc $($isccArgs -join ' ')"

& $resolvedIsccPath @isccArgs
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

$installerFile = Join-Path $installerOutputDir "DepSphere-setup-$Version.exe"
if (-not (Test-Path -LiteralPath $installerFile)) {
    throw "Installer not found after ISCC run: $installerFile"
}

Write-Host "[build-installer] Installer created: $installerFile"
Write-Output $installerFile
