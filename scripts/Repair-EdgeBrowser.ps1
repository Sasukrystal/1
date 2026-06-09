# 修复 Edge 浏览器无法启动（WebView2 / 用户配置损坏时常见）
param(
    [switch]$ResetProfile
)

$ErrorActionPreference = "Continue"

Write-Host "==> 结束 Edge / WebView2 残留进程 ..."
Get-Process msedge, msedgewebview2 -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "==> 尝试修复 Edge 应用包 ..."
$edge = Get-AppxPackage -Name "Microsoft.MicrosoftEdge.Stable" -ErrorAction SilentlyContinue
if ($edge) {
    Write-Host "  找到 Edge: $($edge.InstallLocation)"
} else {
    Write-Host "  未找到 Microsoft Edge Stable 包，可能需从官网重装 Edge" -ForegroundColor Yellow
}

Write-Host "==> 检查 WebView2 运行时 ..."
$wv2 = Get-AppxPackage -Name "Microsoft.WebView2" -ErrorAction SilentlyContinue
if (-not $wv2) {
    Write-Host "  WebView2 未安装。请下载: https://go.microsoft.com/fwlink/p/?LinkId=2124703" -ForegroundColor Yellow
}

if ($ResetProfile) {
    $edgeData = Join-Path $env:LOCALAPPDATA "Microsoft\Edge\User Data"
    if (Test-Path $edgeData) {
        $backup = Join-Path $env:LOCALAPPDATA "Microsoft\Edge_UserData_backup_$(Get-Date -Format yyyyMMdd_HHmmss)"
        Write-Host "==> 备份 Edge 配置到: $backup"
        Copy-Item $edgeData $backup -Recurse -ErrorAction SilentlyContinue
        Remove-Item $edgeData -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  已清除 Edge 用户数据，下次启动会重新初始化" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "请依次尝试:" -ForegroundColor Cyan
Write-Host "  1. 开始菜单搜索 Edge 并打开"
Write-Host "  2. 若仍失败，Win+R 输入: msedge --disable-extensions"
Write-Host "  3. 仍失败，以管理员运行本脚本并加 -ResetProfile"
Write-Host "  4. 临时用 Chrome 打开 WebGL: https://sasukrystal.github.io/1/"
Write-Host ""
Write-Host "WebGL 测试前请清除站点数据: F12 -> Application -> Clear site data"
