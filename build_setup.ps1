# Build the application and package that exact publish into the installer.
$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'build_install.ps1')
if ($LASTEXITCODE -ne 0) { throw "Application publish failed (exit code $LASTEXITCODE)." }

$compiler = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $compiler) {
    foreach ($candidate in @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $compiler = $candidate
            break
        }
    }
}
if (-not $compiler) { throw 'Inno Setup 6 (ISCC.exe) is required to build the installer.' }

$script = Join-Path $PSScriptRoot 'installer.iss'
$project = Join-Path $PSScriptRoot 'DrawingSearchTools\DrawingSearchTools.csproj'
[xml]$projectXml = Get-Content -LiteralPath $project
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Expected a three-part application version in $project"
}

& $compiler "/DAppVersion=$version" $script
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed (exit code $LASTEXITCODE)." }

$installer = Join-Path $PSScriptRoot "installer_output\SmartDrawingFinder_Setup_v$version.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    throw "Installer not found: $installer"
}
Write-Host "Setup ready: $installer"

