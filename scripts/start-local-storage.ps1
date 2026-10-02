$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$storageRoot = Join-Path $projectRoot '.build-check/azurite'
$entryPoint = Join-Path $storageRoot 'node_modules/azurite/dist/src/blob/main.js'

if (Get-NetTCPConnection -State Listen -LocalPort 10000 -ErrorAction SilentlyContinue) {
    Write-Host 'Storage port 10000 is already listening.'
    return
}

if (-not (Test-Path -LiteralPath $entryPoint)) {
    $previousSystemCa = $env:NODE_USE_SYSTEM_CA
    try {
        $env:NODE_USE_SYSTEM_CA = '1'
        & npm.cmd install --prefix $storageRoot azurite@3.35.0 --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'Azurite installation failed.' }
    }
    finally { $env:NODE_USE_SYSTEM_CA = $previousSystemCa }
}

$dataRoot = Join-Path $storageRoot 'data'
New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
$nodePath = (Get-Command node.exe -ErrorAction Stop).Source
$arguments = '"{0}" --blobHost 127.0.0.1 --blobPort 10000 --location "{1}" --skipApiVersionCheck' -f $entryPoint, $dataRoot
$process = Start-Process -FilePath $nodePath -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $storageRoot 'stdout.log') -RedirectStandardError (Join-Path $storageRoot 'stderr.log')
for ($attempt = 0; $attempt -lt 40; $attempt++) {
    if (Get-NetTCPConnection -State Listen -LocalPort 10000 -ErrorAction SilentlyContinue) {
        Write-Host "Azure blob emulator running on 127.0.0.1:10000 (PID $($process.Id))."
        return
    }
    if ($process.HasExited) { throw "Azurite exited. See $storageRoot/stderr.log." }
    Start-Sleep -Milliseconds 250
}
throw "Azurite did not become ready. See $storageRoot/stderr.log."