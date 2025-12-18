<#
.SYNOPSIS
    Скрипт сборки установщика AGK ProjectGen

.DESCRIPTION
    Этот скрипт:
    1. Обновляет версию в .csproj (если указана)
    2. Публикует приложение в режиме self-contained
    3. Конвертирует изображения
    4. Создаёт установщик Inno Setup

.PARAMETER Version
    Версия для сборки (например, "1.0.1"). Если не указана, используется текущая.

.PARAMETER Configuration
    Конфигурация сборки (Release/Debug)

.PARAMETER SkipPublish
    Пропустить этап публикации приложения

.EXAMPLE
    .\Build-Installer.ps1 -Version "1.1.0"
#>

param(
    [string]$Version,
    
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

# Пути
$RootDir = Split-Path $PSScriptRoot -Parent
$InstallerDir = $PSScriptRoot
$PublishDir = Join-Path $RootDir "publish"
$AssetsDir = Join-Path $InstallerDir "Assets"
$BinDir = Join-Path $RootDir "bin\Installer"
$UIProjectDir = Join-Path $RootDir "AGK.ProjectGen.UI"
$CsprojFile = Join-Path $UIProjectDir "AGK.ProjectGen.UI.csproj"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  AGK ProjectGen - Installer Builder" -ForegroundColor Cyan
if ($Version) {
    Write-Host "  Target Version: $Version" -ForegroundColor Magenta
}
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# Шаг 0: Обновление версии
# ============================================
if ($Version) {
    Write-Host "[0/4] Обновление версии до $Version..." -ForegroundColor Yellow
    
    # Регулярка для обновления тегов Version, AssemblyVersion, FileVersion
    $content = Get-Content $CsprojFile -Raw
    
    $patterns = @(
        "<Version>.*?</Version>",
        "<AssemblyVersion>.*?</AssemblyVersion>",
        "<FileVersion>.*?</FileVersion>"
    )
    
    foreach ($pattern in $patterns) {
        $tag = $pattern.Split(">")[0] + ">" # <Version>
        $endTag = "</" + $pattern.Split("<")[2] # </Version>
        $replacement = "$tag$Version$endTag"
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, $replacement
        }
        else {
            # Если тега нет, можно добавить (упрощенно - просто предупреждаем)
            Write-Warning "Тег $tag не найден в csproj, пропускаем."
        }
    }
    
    Set-Content -Path $CsprojFile -Value $content
    Write-Host "   ✓ Версия в .csproj обновлена" -ForegroundColor Green
}
else {
    # Пытаемся прочитать версию из csproj, если не задана
    $xml = [xml](Get-Content $CsprojFile)
    $Version = $xml.Project.PropertyGroup.Version
    if (-not $Version) { $Version = "1.0.0" }
    Write-Host "[0/4] Используется текущая версия: $Version" -ForegroundColor DarkGray
}

# ============================================
# Шаг 1: Публикация приложения
# ============================================
if (-not $SkipPublish) {
    Write-Host "[1/4] Публикация приложения..." -ForegroundColor Yellow
    
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    
    $publishArgs = @(
        "publish"
        $CsprojFile
        "-c", $Configuration
        "-r", "win-x64"
        "-o", $PublishDir
        "--self-contained", "true"
        "-p:PublishSingleFile=false"
        "-p:PublishReadyToRun=true"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ОШИБКА: Не удалось опубликовать приложение!" -ForegroundColor Red
        exit 1
    }
    Write-Host "   Приложение опубликовано" -ForegroundColor Green
}
else {
    Write-Host "[1/4] Пропуск публикации (--SkipPublish)" -ForegroundColor DarkGray
}

# ============================================
# Шаг 2: Подготовка изображений
# ============================================
Write-Host "[2/4] Подготовка изображений..." -ForegroundColor Yellow

$magickAvailable = $null -ne (Get-Command "magick" -ErrorAction SilentlyContinue)
$AppIconPng = Join-Path $AssetsDir "AppIcon.png"
$AppIconIco = Join-Path $AssetsDir "AppIcon.ico"
$WizardImageBmp = Join-Path $AssetsDir "WizardImage.bmp"
$WizardSmallBmp = Join-Path $AssetsDir "WizardSmallImage.bmp"
$WizardPng = Join-Path $AssetsDir "InstallerWizard.png"

# Вспомогательная функция конвертации
function Convert-PngToBmp {
    param([string]$SourcePath, [string]$DestPath, [int]$Width, [int]$Height)
    if (-not (Test-Path $SourcePath)) { return }
    
    Add-Type -AssemblyName System.Drawing
    $sourceImage = [System.Drawing.Image]::FromFile($SourcePath)
    $destImage = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($destImage)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($sourceImage, 0, 0, $Width, $Height)
    $destImage.Save($DestPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $graphics.Dispose()
    $destImage.Dispose()
    $sourceImage.Dispose()
}

# Проверяем и создаем, если нет
if (-not (Test-Path $WizardImageBmp)) {
    if ($magickAvailable) {
        & magick $WizardPng -resize 164x314! -type TrueColor BMP3:$WizardImageBmp
    }
    else {
        Convert-PngToBmp -SourcePath $WizardPng -DestPath $WizardImageBmp -Width 164 -Height 314
    }
    Write-Host "   ✓ WizardImage.bmp создан" -ForegroundColor Green
}

if (-not (Test-Path $WizardSmallBmp)) {
    if ($magickAvailable) {
        & magick $AppIconPng -resize 55x55! -type TrueColor BMP3:$WizardSmallBmp
    }
    else {
        Convert-PngToBmp -SourcePath $AppIconPng -DestPath $WizardSmallBmp -Width 55 -Height 55
    }
    Write-Host "   ✓ WizardSmallImage.bmp создан" -ForegroundColor Green
}

if (-not (Test-Path $AppIconIco)) {
    if ($magickAvailable) {
        & magick $AppIconPng -define icon:auto-resize=256, 64, 48, 32, 16 $AppIconIco
        Write-Host "   ✓ AppIcon.ico создан (ImageMagick)" -ForegroundColor Green
    }
    else {
        # Простой fallback
        $icon = [System.Drawing.Image]::FromFile($AppIconPng)
        $iconResized = New-Object System.Drawing.Bitmap($icon, 256, 256)
        $iconHandle = $iconResized.GetHicon()
        $iconObj = [System.Drawing.Icon]::FromHandle($iconHandle)
        $fs = [System.IO.File]::Create($AppIconIco)
        $iconObj.Save($fs)
        $fs.Close()
        Write-Host "   ✓ AppIcon.ico создан (.NET)" -ForegroundColor Green
    }
    # Копируем в UI
    Copy-Item $AppIconIco (Join-Path $UIProjectDir "app.ico") -Force
}

# ============================================
# Шаг 3: Сборка установщика
# ============================================
Write-Host "[3/4] Сборка установщика Inno Setup..." -ForegroundColor Yellow

$InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoSetupPath)) {
    $InnoSetupPath = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $InnoSetupPath)) { 
    Write-Host "ОШИБКА: Inno Setup не найден!" -ForegroundColor Red 
    exit 1 
}

if (-not (Test-Path $BinDir)) { New-Item -ItemType Directory -Path $BinDir -Force | Out-Null }

$IssFile = Join-Path $InstallerDir "AGKProjectGen.iss"

# Передаем версию через /D
& $InnoSetupPath "/DMyAppVersion=$Version" $IssFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось создать установщик!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  ✓ Готово! Версия: $Version" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
$Exe = Get-ChildItem $BinDir -Filter "*$Version.exe" | Select-Object -First 1
if ($Exe) { Write-Host "Installer: $($Exe.FullName)" -ForegroundColor Cyan }
