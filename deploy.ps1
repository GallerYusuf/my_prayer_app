# Ezan Vakti — Android telefona yükle ve çalıştır
# Kullanım: Sağ tık → "PowerShell ile Çalıştır" veya terminalde: .\deploy.ps1

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot

# adb.exe'yi yaygin konumlarda otomatik ara
$candidatePaths = @(
    "C:\Android\platform-tools\adb.exe",
    "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "$env:USERPROFILE\AppData\Local\Android\Sdk\platform-tools\adb.exe"
)

$AdbPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $AdbPath) {
    Write-Host "adb bulunamadi. Android SDK kurulu degil veya farkli bir konumda." -ForegroundColor Red
    Write-Host "Bulunan konumlar denendi:" -ForegroundColor DarkGray
    $candidatePaths | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    Read-Host "Kapatmak icin Enter'a basin"
    exit 1
}

$AndroidSdk = Split-Path (Split-Path $AdbPath)
$env:ANDROID_HOME = $AndroidSdk
$env:PATH = "$(Split-Path $AdbPath);$env:PATH"

$JavaCandidates = @(
    "C:\Program Files\Eclipse Adoptium\jdk-21.0.12.101-hotspot",
    "C:\Program Files\Eclipse Adoptium\jdk-17*",
    "C:\Program Files\Microsoft\jdk-*"
)
$JavaSdk = Get-Item $JavaCandidates -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName

# aapt2 Türkçe karakterli kullanıcı yollarında (APT2265) hata vermemesi için ASCII junction kullan
$AsciiRoot = "C:\Android"
if ($ProjectDir -match "[^\x00-\x7F]" -and (Test-Path $AsciiRoot)) {
    $JunctionProject = Join-Path $AsciiRoot "my_prayer_app"
    $JunctionNuget = Join-Path $AsciiRoot "nuget_packages"
    $JunctionLocalAppData = Join-Path $AsciiRoot "localappdata"

    if (-not (Test-Path $JunctionProject)) {
        cmd /c mklink /J "$JunctionProject" "$ProjectDir" | Out-Null
    }
    if (-not (Test-Path $JunctionNuget) -and (Test-Path "$env:USERPROFILE\.nuget\packages")) {
        cmd /c mklink /J "$JunctionNuget" "$env:USERPROFILE\.nuget\packages" | Out-Null
    }
    if (-not (Test-Path $JunctionLocalAppData)) {
        cmd /c mklink /J "$JunctionLocalAppData" "$env:LOCALAPPDATA" | Out-Null
    }

    $ProjectDir = $JunctionProject
    $env:NUGET_PACKAGES = $JunctionNuget
    $env:LOCALAPPDATA = $JunctionLocalAppData
}

Write-Host ""
Write-Host "=== Ezan Vakti — Android Deploy ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Bagli cihazlar kontrol ediliyor..." -ForegroundColor Yellow
$devices = & $AdbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "device$" }

if (-not $devices) {
    Write-Host "Telefon bulunamadi." -ForegroundColor Red
    Write-Host "  - USB kablosunu kontrol edin"
    Write-Host "  - USB hata ayiklama acik olsun"
    Write-Host "  - Telefonda 'Bu bilgisayara guven' onayini verin"
    Read-Host "Kapatmak icin Enter'a basin"
    exit 1
}

foreach ($line in $devices) {
    $id = ($line -split "\s+")[0]
    Write-Host "  Cihaz: $id" -ForegroundColor Green
}

Write-Host ""
Write-Host "Derleniyor ve telefona yukleniyor..." -ForegroundColor Yellow
Write-Host "(Ilk seferde 1-2 dakika surebilir)" -ForegroundColor DarkGray
Write-Host ""

Set-Location $ProjectDir
if ($JavaSdk) {
    dotnet build -t:Run -f net10.0-android -p:AndroidSdkDirectory="$AndroidSdk" -p:JavaSdkDirectory="$JavaSdk"
} else {
    dotnet build -t:Run -f net10.0-android -p:AndroidSdkDirectory="$AndroidSdk"
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Basarili! Uygulama telefonda acildi." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Deploy basarisiz. Yukaridaki hata mesajlarina bakin." -ForegroundColor Red
}

Write-Host ""
Read-Host "Kapatmak icin Enter'a basin"
