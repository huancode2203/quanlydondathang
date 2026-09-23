#Requires -RunAsAdministrator
#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$PublishDirectory,
    [ValidateRange(1024, 65535)][int]$WebPort = 8080,
    [ValidateRange(1024, 65535)][int]$ApiPort = 5081,
    [switch]$InstallPrerequisites
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if ($WebPort -eq $ApiPort) { throw 'Cong web va API noi bo phai khac nhau.' }
if (-not $PublishDirectory) {
    $latestPath = Join-Path $projectRoot 'Deployment\local\latest-publish.txt'
    if (-not (Test-Path -LiteralPath $latestPath)) { throw 'Chay scripts\Publish-Iis.ps1 truoc.' }
    $PublishDirectory = (Get-Content -LiteralPath $latestPath -Raw).Trim()
}
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
foreach ($relativePath in @('api\Order.Api.exe', 'api\web.config', 'web\APIGateway.exe', 'web\web.config', 'web\wwwroot\index.html', 'deployment.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishPath $relativePath))) { throw "Thieu file publish: $relativePath" }
}
$manifest = Get-Content -LiteralPath (Join-Path $publishPath 'deployment.json') -Raw | ConvertFrom-Json
if ($manifest.ApiPort -ne $ApiPort) { throw 'ApiPort khong trung ban publish. Publish lai voi ApiPort mong muon.' }

if ($InstallPrerequisites) {
    $features = @('IIS-WebServerRole','IIS-WebServer','IIS-CommonHttpFeatures','IIS-StaticContent',
        'IIS-DefaultDocument','IIS-HttpErrors','IIS-RequestFiltering','IIS-ManagementConsole',
        'IIS-ApplicationInit')
    $featureResult = Enable-WindowsOptionalFeature -Online -FeatureName $features -All -NoRestart
    if ($featureResult.RestartNeeded) { throw 'Windows yeu cau khoi dong lai. Restart roi chay lai script nay.' }

    $modulePath = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
    if (-not (Test-Path -LiteralPath $modulePath)) {
        $runtimeVersion = '11.0.0-preview.7.26381.103'
        $metadata = Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json'
        $runtime = $metadata.releases | Where-Object { $_.'aspnetcore-runtime'.version -eq $runtimeVersion } | Select-Object -First 1
        $installer = $runtime.'aspnetcore-runtime'.files | Where-Object name -eq 'dotnet-hosting-win.exe' | Select-Object -First 1
        if (-not $installer.url -or -not $installer.hash) { throw 'Khong tim thay Hosting Bundle dung version tren Microsoft.' }
        $installerUri = [Uri]$installer.url
        if ($installerUri.Scheme -ne 'https' -or $installerUri.Host -ne 'builds.dotnet.microsoft.com') { throw 'Nguon Hosting Bundle khong hop le.' }
        $installerPath = Join-Path $env:TEMP ('order-hosting-' + [Guid]::NewGuid().ToString('N') + '.exe')
        Invoke-WebRequest -Uri $installer.url -OutFile $installerPath -UseBasicParsing
        if ((Get-FileHash -LiteralPath $installerPath -Algorithm SHA512).Hash -ne $installer.hash) { throw 'Hosting Bundle sai SHA512.' }
        $signature = Get-AuthenticodeSignature -LiteralPath $installerPath
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
            throw 'Chu ky Hosting Bundle khong hop le.'
        }
        $installation = Start-Process -FilePath $installerPath -ArgumentList '/install','/quiet','/norestart' -Wait -PassThru -WindowStyle Hidden
        if ($installation.ExitCode -notin @(0,3010)) { throw "Cai Hosting Bundle that bai: $($installation.ExitCode)" }
        if ($installation.ExitCode -eq 3010) { throw 'Hosting Bundle yeu cau restart Windows, sau do chay lai script.' }
    }
}

$appCmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
$modulePath = Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'
if (-not (Test-Path -LiteralPath $appCmd) -or -not (Test-Path -LiteralPath $modulePath)) {
    throw 'Chua co IIS/Hosting Bundle. Chay lai script kem -InstallPrerequisites.'
}
Import-Module WebAdministration

$sites = @(
    @{ Name = 'OrderManagement.Api'; Folder = 'api'; Port = $ApiPort },
    @{ Name = 'OrderManagement.Web'; Folder = 'web'; Port = $WebPort }
)
foreach ($site in $sites) {
    $existing = Get-Website -Name $site.Name -ErrorAction SilentlyContinue
    if ($existing) {
        # Existing project sites can be moved to a new immutable release; other sites are untouched.
        $currentPath = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($existing.physicalPath))
        $projectReleases = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts\iis')) + '\'
        if (-not $currentPath.StartsWith($projectReleases, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Site $($site.Name) da ton tai ngoai thu muc cua project: $currentPath"
        }
        $binding = Get-WebBinding -Name $site.Name -Protocol http
        if (@($binding).Count -ne 1 -or $binding.bindingInformation -ne "127.0.0.1:$($site.Port):") {
            throw "Binding cua $($site.Name) khac cau hinh. Kiem tra IIS Manager truoc khi cap nhat."
        }
    } else {
        $portInUse = Get-NetTCPConnection -LocalPort $site.Port -State Listen -ErrorAction SilentlyContinue
        if ($portInUse) { throw "Cong $($site.Port) dang duoc su dung." }
    }
}

foreach ($site in $sites) {
    $poolPath = 'IIS:\AppPools\' + $site.Name
    if (-not (Test-Path $poolPath)) { $null = New-WebAppPool -Name $site.Name }
    Set-ItemProperty $poolPath -Name managedRuntimeVersion -Value ''
    Set-ItemProperty $poolPath -Name startMode -Value AlwaysRunning
    Set-ItemProperty $poolPath -Name processModel.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty $poolPath -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
    $physicalPath = Join-Path $publishPath $site.Folder
    & icacls.exe $physicalPath /grant "IIS AppPool\$($site.Name):(OI)(CI)RX" /T /Q
    if ($LASTEXITCODE -ne 0) { throw "Khong cap duoc quyen doc cho $($site.Name)." }
    if (Get-Website -Name $site.Name -ErrorAction SilentlyContinue) {
        Stop-Website -Name $site.Name
        if ((Get-WebAppPoolState -Name $site.Name).Value -eq 'Started') { Stop-WebAppPool -Name $site.Name }
        Set-ItemProperty ('IIS:\Sites\' + $site.Name) -Name physicalPath -Value $physicalPath
        Set-ItemProperty ('IIS:\Sites\' + $site.Name) -Name applicationPool -Value $site.Name
    } else {
        $null = New-Website -Name $site.Name -IPAddress '127.0.0.1' -Port $site.Port -PhysicalPath $physicalPath -ApplicationPool $site.Name
    }
    Set-ItemProperty ('IIS:\Sites\' + $site.Name) -Name serverAutoStart -Value $true
    Set-ItemProperty ('IIS:\Sites\' + $site.Name) -Name applicationDefaults.preloadEnabled -Value $true
}

# The API app pool authenticates to the local SQL instance using Windows identity.
# Grant only data access to this project database, never sysadmin/db_owner.
$sqlGrant = @'
USE master;
IF SUSER_ID(N'IIS APPPOOL\OrderManagement.Api') IS NULL
    CREATE LOGIN [IIS APPPOOL\OrderManagement.Api] FROM WINDOWS;
USE QuanLyDonDatHangDB;
IF USER_ID(N'IIS APPPOOL\OrderManagement.Api') IS NULL
    CREATE USER [IIS APPPOOL\OrderManagement.Api] FOR LOGIN [IIS APPPOOL\OrderManagement.Api];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [IIS APPPOOL\OrderManagement.Api];
GRANT EXECUTE ON SCHEMA::dbo TO [IIS APPPOOL\OrderManagement.Api];
'@
& sqlcmd -S 'HUANPHAM\MSSQLSERVER01' -E -C -b -Q $sqlGrant
if ($LASTEXITCODE -ne 0) { throw 'Khong cap duoc quyen SQL cho API AppPool.' }

Set-Service WAS -StartupType Automatic
Set-Service W3SVC -StartupType Automatic
Start-Service W3SVC
foreach ($site in $sites) {
    if ((Get-WebAppPoolState -Name $site.Name).Value -ne 'Started') { Start-WebAppPool -Name $site.Name }
    Start-Website -Name $site.Name
}
$health = Invoke-RestMethod -Uri "http://127.0.0.1:$WebPort/api/health" -Method Post -ContentType 'application/json' -Body '{}'
if ($health.status -ne 200) { throw 'IIS da cau hinh nhung health check chua thanh cong.' }
Write-Host "San sang: http://127.0.0.1:$WebPort (khong can mo terminal)"
