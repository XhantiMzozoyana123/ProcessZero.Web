# Run this AFTER the Windows restart to run the container WITHOUT Docker Desktop.
# It uses a WSL2 Ubuntu distro with the native Docker Engine.
# Open PowerShell in this folder and run:  .\finish-docker-setup.ps1

$ErrorActionPreference = 'Stop'
$distro  = "UbuntuDocker"
$dir     = "C:\WSL\UbuntuDocker"
$rootfs  = "$env:TEMP\ubuntu-wsl.tar.gz"
$rootUrl = "https://cloud-images.ubuntu.com/wsl/jammy/current/ubuntu-jammy-wsl-amd64-ubuntu22.04lts.rootfs.tar.gz"
$scriptWin = Join-Path $PSScriptRoot "wsl-docker-run.sh"

Write-Host "==> Checking that the hypervisor is running..." -ForegroundColor Cyan
if ((Get-ComputerInfo -Property CsHypervisorPresent).CsHypervisorPresent -ne $true) {
    Write-Host "Hypervisor is still not running. Make sure you rebooted after enabling it. Aborting." -ForegroundColor Red
    exit 1
}

Write-Host "==> Importing the '$distro' WSL2 distro if needed..." -ForegroundColor Cyan
$existing = (wsl --list --quiet) -join "`n"
if ($existing -notmatch [regex]::Escape($distro)) {
    if (-not (Test-Path $rootfs)) {
        Write-Host "    downloading Ubuntu rootfs..."
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $rootUrl -OutFile $rootfs
    }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    wsl --import $distro $dir $rootfs --version 2
} else {
    Write-Host "    '$distro' already exists."
}

# Convert the Windows path of the shell script to a /mnt/c path
$scriptWsl = (& wsl -d $distro -- wslpath -a "$scriptWin").Trim()

Write-Host "==> Installing Docker Engine + building/running the container inside WSL..." -ForegroundColor Cyan
# sed strips Windows CR characters so the bash script runs cleanly.
wsl -d $distro -u root -- bash -c "sed 's/\r$//' '$scriptWsl' | bash"

Write-Host ""
Write-Host "If everything succeeded, the app is available at http://localhost:8080" -ForegroundColor Green
