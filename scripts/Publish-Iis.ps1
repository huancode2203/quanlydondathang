[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)][int]$ApiPort = 5081,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $projectRoot ('artifacts\iis\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "Thu muc publish da ton tai. Chon thu muc moi de giu nguyen ban cu: $outputPath"
}
$null = New-Item -ItemType Directory -Path $outputPath
$frontendPath = Join-Path $projectRoot 'Frontend\OrderManagement.Web'

Push-Location $frontendPath
try {
    if (-not (Test-Path -LiteralPath 'node_modules')) {
        & npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed.' }
    }
    & npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'Angular build failed.' }
} finally { Pop-Location }

$projects = @(
    @{ Project = 'Backend\Services\Order\Order.Api\Order.Api.csproj'; Folder = 'api' },
    @{ Project = 'Backend\APIGateway\APIGateway.csproj'; Folder = 'web' }
)
foreach ($item in $projects) {
    & dotnet publish (Join-Path $projectRoot $item.Project) -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=false -o (Join-Path $outputPath $item.Folder)
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $($item.Project)" }
}

$angularOutput = Join-Path $frontendPath 'dist\order-management-ui\browser'
if (-not (Test-Path -LiteralPath (Join-Path $angularOutput 'index.html'))) {
    throw "Khong tim thay Angular build: $angularOutput"
}
$webRoot = Join-Path $outputPath 'web\wwwroot'
$null = New-Item -ItemType Directory -Path $webRoot -Force
Get-ChildItem -LiteralPath $angularOutput -Force | Copy-Item -Destination $webRoot -Recurse

# Build output only: do not commit machine-specific configuration or signing keys.
$localSettingsDirectory = Join-Path $projectRoot 'Deployment\local'
$null = New-Item -ItemType Directory -Path $localSettingsDirectory -Force
$keyPath = Join-Path $localSettingsDirectory 'jwt-key.txt'
if (Test-Path -LiteralPath $keyPath) {
    $signingKey = (Get-Content -LiteralPath $keyPath -Raw).Trim()
} else {
    $bytes = New-Object byte[] 64
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    $signingKey = [Convert]::ToBase64String($bytes)
    [IO.File]::WriteAllText($keyPath, $signingKey)
}
@{ Jwt = @{ Key = $signingKey } } | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $outputPath 'api\appsettings.Production.json') -Encoding UTF8
@{ ReverseProxy = @{ Clusters = @{ 'order-cluster' = @{ Destinations = @{ 'order-api' = @{ Address = "http://127.0.0.1:$ApiPort/" } } } } } } |
    ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputPath 'web\appsettings.Production.json') -Encoding UTF8

$manifest = @{ PublishedAt = (Get-Date -Format o); Path = $outputPath; ApiPort = $ApiPort; SelfContained = $true }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'deployment.json') -Encoding UTF8
$outputPath | Set-Content -LiteralPath (Join-Path $localSettingsDirectory 'latest-publish.txt') -Encoding UTF8
Write-Host "Da publish: $outputPath"
Write-Host 'Tiep theo: chay scripts\Install-Iis.ps1 trong Windows PowerShell Run as administrator.'
