# Publish the current application to the folder consumed by installer.iss.
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DrawingSearchTools\DrawingSearchTools.csproj'
$publish = Join-Path $PSScriptRoot 'publish'

if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Application project not found: $project"
}

# Verify the target before clearing an older publish. Never remove a path
# supplied by the caller or a directory outside this project.
$rootPath = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$publishPath = [IO.Path]::GetFullPath($publish).TrimEnd('\')
if (-not $publishPath.Equals((Join-Path $rootPath 'publish'), [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected publish directory: $publishPath"
}
if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

& dotnet publish $project --configuration Release --runtime win-x64 --self-contained true --output $publishPath -p:PublishSingleFile=false -p:PublishReadyToRun=true -p:NuGetAudit=false --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw "Application publish failed (exit code $LASTEXITCODE)." }

$exe = Join-Path $publishPath 'DrawingSearchTools.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "Published application not found: $exe"
}
Write-Host "Published application: $exe"

