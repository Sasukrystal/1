# 一键发布到 GitHub 新账号 Sasukrystal/1（仅此 remote）
# 用法: 在 PowerShell 中执行
#   cd D:\unity\test\bagsys
#   .\scripts\Publish-To-Sasukrystal.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$allowedRemotes = @(
    "https://github.com/Sasukrystal/1.git",
    "git@github.com:Sasukrystal/1.git"
)
$remoteUrl = (git remote get-url origin 2>$null)

if ($allowedRemotes -notcontains $remoteUrl) {
    Write-Error "origin 必须是 Sasukrystal/1 ，当前是: $remoteUrl"
}

Write-Host "==> 1/4 推送到 Sasukrystal/1 ..."
# 大仓库易断线：优先走精简单次提交推送
& (Join-Path $PSScriptRoot "Push-Lightweight-To-Sasukrystal.ps1")
if ($LASTEXITCODE -ne 0) {
    exit 1
}

Write-Host "==> 2/4 检查 Windows 构建 ..."
$exe = Join-Path $root "Builds\Windows\黑暗地牢.exe"
if (-not (Test-Path $exe)) {
    Write-Host "未找到 Windows 包，请先在 Unity: Modern Rogue -> Build Windows Release" -ForegroundColor Yellow
    Write-Host "源码已推送；构建完成后再次运行本脚本可上传 Release。"
    Write-Host "仓库: https://github.com/Sasukrystal/1"
    exit 0
}

Write-Host "==> 3/4 打包 zip ..."
& (Join-Path $PSScriptRoot "Package-WindowsRelease.ps1")

$zipPath = Join-Path $root "Builds\DarkDungeon-Windows.zip"
if (-not (Test-Path $zipPath)) {
    Write-Error "zip 未生成"
}

Write-Host "==> 4/4 发布 GitHub Release ..."
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    Write-Host "未安装 GitHub CLI (gh)，请手动上传 Release:" -ForegroundColor Yellow
    Write-Host "  https://github.com/Sasukrystal/1/releases/new"
    Write-Host "  Tag: v1.0  文件: $zipPath"
    Write-Host ""
    Write-Host "同学游玩链接（发布后）:" -ForegroundColor Green
    Write-Host "  https://github.com/Sasukrystal/1/releases/latest"
    Start-Process "https://github.com/Sasukrystal/1/releases/new"
    exit 0
}

$authStatus = & gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "请先登录新账号 Sasukrystal:" -ForegroundColor Yellow
    & gh auth login -h github.com -p https -w
}

& gh release view v1.0 --repo Sasukrystal/1 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Release v1.0 已存在，上传/更新 zip ..."
    & gh release upload v1.0 $zipPath --repo Sasukrystal/1 --clobber
} else {
    & gh release create v1.0 $zipPath `
        --repo Sasukrystal/1 `
        --title "黑暗地牢 Windows 版" `
        --notes "解压后运行 黑暗地牢.exe。需要 Windows 64 位。"
}

Write-Host ""
Write-Host "完成！发给同学的链接:" -ForegroundColor Green
Write-Host "  https://github.com/Sasukrystal/1/releases/latest"
Write-Host "源码仓库:" -ForegroundColor Green
Write-Host "  https://github.com/Sasukrystal/1"
