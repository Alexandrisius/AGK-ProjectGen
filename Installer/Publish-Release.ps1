<#
.SYNOPSIS
    Скрипт публикации AGK ProjectGen в GitHub Releases

.DESCRIPTION
    Этот скрипт:
    1. Обновляет версию в .csproj
    2. Публикует приложение
    3. Создает Velopack пакет
    4. Загружает релиз в GitHub Releases

.PARAMETER Version
    Версия для публикации (например, "1.5.0")

.PARAMETER GitHubToken
    GitHub Personal Access Token с правами 'repo'

.EXAMPLE
    .\Publish-Release.ps1 -Version "1.5.0" -GitHubToken "ghp_xxx..."
    
    # Или через переменную окружения:
    $env:GITHUB_TOKEN = "ghp_xxx..."
    .\Publish-Release.ps1 -Version "1.5.0"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [string]$GitHubToken,
    
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [switch]$Prerelease,
    
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

# ============================================
# Конфигурация
# ============================================
$GitHubRepo = "Alexandrisius/AGK-ProjectGen"  # TODO: Замените на ваш репозиторий
$AppId = "AGKProjectGen"
$AppName = "AGK ProjectGen"

# Пути
$RootDir = Split-Path $PSScriptRoot -Parent
$InstallerDir = $PSScriptRoot
$PublishDir = Join-Path $RootDir "publish"
$ReleasesDir = Join-Path $RootDir "releases"
$UIProjectDir = Join-Path $RootDir "AGK.ProjectGen.UI"
$CsprojFile = Join-Path $UIProjectDir "AGK.ProjectGen.UI.csproj"
$IconPath = Join-Path $InstallerDir "Assets\AppIcon.ico"

# Получаем токен
if (-not $GitHubToken) {
    $GitHubToken = $env:GITHUB_TOKEN
}

if (-not $GitHubToken) {
    Write-Host "ОШИБКА: GitHub Token не указан!" -ForegroundColor Red
    Write-Host "Используйте параметр -GitHubToken или установите переменную `$env:GITHUB_TOKEN" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     AGK ProjectGen - GitHub Release          ║" -ForegroundColor Cyan
Write-Host "║     Version: $Version                          ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ============================================
# Шаг 1: Обновление версии
# ============================================
Write-Host "[1/4] 📝 Обновление версии до $Version..." -ForegroundColor Yellow

$content = Get-Content $CsprojFile -Raw

$patterns = @(
    "<Version>.*?</Version>",
    "<AssemblyVersion>.*?</AssemblyVersion>",
    "<FileVersion>.*?</FileVersion>"
)

foreach ($pattern in $patterns) {
    $tag = $pattern.Split(">")[0] + ">"
    $endTag = "<" + $pattern.Split("<")[2]
    $replacement = "$tag$Version$endTag"
    
    if ($content -match $pattern) {
        $content = $content -replace $pattern, $replacement
    }
}

Set-Content -Path $CsprojFile -Value $content -NoNewline
Write-Host "   ✓ Версия обновлена" -ForegroundColor Green

# ============================================
# Шаг 2: Публикация приложения
# ============================================
if (-not $SkipPublish) {
    Write-Host "[2/4] 🔨 Сборка приложения..." -ForegroundColor Yellow
    
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
    Write-Host "   ✓ Приложение собрано" -ForegroundColor Green
}
else {
    Write-Host "[2/4] ⏭ Пропуск сборки (--SkipPublish)" -ForegroundColor DarkGray
}

# ============================================
# Шаг 3: Создание Velopack пакета
# ============================================
Write-Host "[3/4] 📦 Создание Velopack пакета..." -ForegroundColor Yellow

if (-not (Test-Path $ReleasesDir)) {
    New-Item -ItemType Directory -Path $ReleasesDir -Force | Out-Null
}

$packArgs = @(
    "pack"
    "--packId", $AppId
    "--packVersion", $Version
    "--packDir", $PublishDir
    "--mainExe", "AGK.ProjectGen.UI.exe"
    "--outputDir", $ReleasesDir
    "--packTitle", $AppName
)

# Добавляем иконку, если есть
if (Test-Path $IconPath) {
    $packArgs += "--icon", $IconPath
}

& vpk @packArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось создать Velopack пакет!" -ForegroundColor Red
    exit 1
}
Write-Host "   ✓ Пакет создан" -ForegroundColor Green

# ============================================
# Шаг 4: Загрузка в GitHub Releases
# ============================================
Write-Host "[4/4] 🚀 Загрузка в GitHub Releases..." -ForegroundColor Yellow

$uploadArgs = @(
    "upload", "github"
    "--repoUrl", "https://github.com/$GitHubRepo"
    "--token", $GitHubToken
    "--outputDir", $ReleasesDir
    "--tag", "v$Version"
    "--releaseName", "$AppName $Version"
    "--publish"
)

if ($Prerelease) {
    $uploadArgs += "--pre"
}

& vpk @uploadArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ОШИБКА: Не удалось загрузить в GitHub!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║     ✓ Релиз v$Version опубликован!             ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Ссылка: https://github.com/$GitHubRepo/releases/tag/v$Version" -ForegroundColor Cyan
