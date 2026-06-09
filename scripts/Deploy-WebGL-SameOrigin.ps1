# 触发 GitHub Actions 同域 WebGL 部署（Build/ 与入口页同在 github.io/1/）
param(
    [string]$ProxyUrl = "http://127.0.0.1:7890",
    [string]$Repo = "Sasukrystal/1"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

if ($ProxyUrl) {
    $env:HTTPS_PROXY = $ProxyUrl
    $env:HTTP_PROXY = $ProxyUrl
}

function Get-GithubOAuthToken {
    $gcm = "E:\Git\mingw64\bin\git-credential-manager.exe"
    if (-not (Test-Path $gcm)) { $gcm = "git-credential-manager" }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $gcm
    $psi.Arguments = "get"
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.UseShellExecute = $false
    $p = [Diagnostics.Process]::Start($psi)
    $p.StandardInput.WriteLine("protocol=https")
    $p.StandardInput.WriteLine("host=github.com")
    $p.StandardInput.WriteLine("")
    $p.StandardInput.Close()
    $out = $p.StandardOutput.ReadToEnd()
    $p.WaitForExit()
    return (($out -split "`n" | Where-Object { $_ -like 'password=*' }) -replace 'password=','').Trim()
}

$token = Get-GithubOAuthToken
$headers = @{
    Authorization = "Bearer $token"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

Write-Host "==> 将 Pages 源切换为 GitHub Actions ..."
$pagesBody = '{"build_type":"workflow"}'
try {
    & curl.exe -x $ProxyUrl -sS -X PUT `
        -H "Authorization: Bearer $token" `
        -H "Accept: application/vnd.github+json" `
        -H "Content-Type: application/json" `
        -d $pagesBody `
        "https://api.github.com/repos/$Repo/pages" | Out-Null
} catch {
    Write-Warning "Pages API 可能需要先在网页端启用: Settings -> Pages -> Source: GitHub Actions"
}

Write-Host "==> 推送 main 并触发 workflow ..."
git add WebGLDeploy/index.html .github/workflows/deploy-webgl-pages.yml scripts/Deploy-WebGL-SameOrigin.ps1
$status = git status --porcelain
if ($status) {
    git commit -m "WebGL: same-origin Build/ deploy via GitHub Actions (fix Edge tracking prevention)"
}
git push origin main

Write-Host "==> 手动触发 Deploy WebGL workflow ..."
& curl.exe -x $ProxyUrl -sS -X POST `
    -H "Authorization: Bearer $token" `
    -H "Accept: application/vnd.github+json" `
    "https://api.github.com/repos/$Repo/actions/workflows/deploy-webgl-pages.yml/dispatches" `
    -d '{"ref":"main"}'

Write-Host ""
Write-Host "部署已触发。约 3–8 分钟后打开:" -ForegroundColor Green
Write-Host "  https://sasukrystal.github.io/1/"
Write-Host ""
Write-Host "若 Actions 失败，请到仓库 Settings -> Pages 选择 Source = GitHub Actions" -ForegroundColor Yellow
Write-Host "部署完成前请清除浏览器站点数据 (F12 -> Application -> Clear site data)" -ForegroundColor Yellow
