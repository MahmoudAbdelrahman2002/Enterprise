# =============================================================================
# Automated Azure Deployment Script for Subito API
# =============================================================================
$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Deploying Subito API to Azure...      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Locate PublishSettings file
$publishSettingsPath = Get-ChildItem -Path $PSScriptRoot -Filter "*.PublishSettings" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

if (-not $publishSettingsPath) {
    $downloadPath = Join-Path $HOME "Downloads\subito-api.PublishSettings"
    if (Test-Path $downloadPath) {
        $publishSettingsPath = $downloadPath
    } else {
        $downloadAny = Get-ChildItem -Path (Join-Path $HOME "Downloads") -Filter "*.PublishSettings" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
        if ($downloadAny) {
            $publishSettingsPath = $downloadAny
        }
    }
}

if (-not $publishSettingsPath -or -not (Test-Path $publishSettingsPath)) {
    Write-Error "Could not find a .PublishSettings file in the project folder or Downloads folder. Please place your *.PublishSettings file in the project root."
    exit 1
}

Write-Host "Using PublishSettings: $publishSettingsPath" -ForegroundColor Gray

# 2. Parse PublishSettings XML
[xml]$profileXml = Get-Content $publishSettingsPath -Raw
$zipProfile = $profileXml.publishData.publishProfile | Where-Object { $_.publishMethod -eq "ZipDeploy" }
if (-not $zipProfile) {
    $zipProfile = $profileXml.publishData.publishProfile | Select-Object -First 1
}

$user = $zipProfile.userName
$pass = $zipProfile.userPWD
$destinationUrl = $zipProfile.destinationAppUrl
$scmHost = $zipProfile.publishUrl.Split(':')[0]
$kuduZipDeployUrl = "https://${scmHost}/api/zipdeploy"

# 3. Build & Publish .NET Project
$projectPath = Join-Path $PSScriptRoot "src\Enterprise.Api\Enterprise.Api.csproj"
$outputFolder = Join-Path $PSScriptRoot "publish-temp"
$zipPath = Join-Path $PSScriptRoot "publish.zip"

if (Test-Path $outputFolder) {
    Remove-Item -Path $outputFolder -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
}

Write-Host "`n[1/3] Building & Publishing project in Release mode..." -ForegroundColor Yellow
dotnet publish $projectPath -c Release -o $outputFolder --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed! Please fix build errors before deploying."
    exit 1
}

# 4. Compress to ZIP
Write-Host "`n[2/3] Compressing package for deployment..." -ForegroundColor Yellow
Compress-Archive -Path "$outputFolder\*" -DestinationPath $zipPath -Force

# 5. Upload to Azure via Kudu ZipDeploy
Write-Host "`n[3/3] Uploading package to Azure App Service ($destinationUrl)..." -ForegroundColor Yellow
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${user}:${pass}"))
$headers = @{
    Authorization = "Basic $base64Auth"
}

try {
    $response = Invoke-RestMethod -Uri $kuduZipDeployUrl -Headers $headers -Method Post -InFile $zipPath -ContentType "application/zip" -TimeoutSec 600
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "   Deployment Succeeded!                                " -ForegroundColor Green
    Write-Host "   URL: $destinationUrl                                 " -ForegroundColor Green
    Write-Host "   Swagger: $destinationUrl/swagger                     " -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
}
catch {
    Write-Error "Deployment failed: $_"
    exit 1
}
finally {
    # Clean up temporary artifacts
    if (Test-Path $outputFolder) {
        Remove-Item -Path $outputFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
    }
}
