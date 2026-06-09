# 将 Builds/Windows 打成 zip，用于上传到 GitHub Releases
# 用法: .\scripts\Package-WindowsRelease.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root "Builds\Windows"
$outDir = Join-Path $root "Builds"
$zipName = "DarkDungeon-Windows.zip"
$zipPath = Join-Path $outDir $zipName

if (-not (Test-Path $source)) {
    Write-Error "未找到 $source ，请先在 Unity 执行 Modern Rogue -> Build Windows Release"
}

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "正在压缩 $source ..."
Compress-Archive -Path (Join-Path $source "*") -DestinationPath $zipPath -CompressionLevel Optimal
$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "已生成: $zipPath ($sizeMb MB)"
Write-Host ""
Write-Host "下一步（在 GitHub 新账号 Sasukrystal/1）："
Write-Host "1. 打开 https://github.com/Sasukrystal/1/releases/new"
Write-Host "2. Tag 填 v1.0，Title 填 黑暗地牢 Windows 版"
Write-Host "3. 上传此 zip 文件"
Write-Host "4. 发布后分享链接: https://github.com/Sasukrystal/1/releases/latest"
