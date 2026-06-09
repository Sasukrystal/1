# 部署 WebGL 到 gh-pages：小文件同域 + data 走 LFS media CDN
param(
    [string]$ProxyUrl = "http://127.0.0.1:7890",
    [string]$Repo = "Sasukrystal/1"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$jsdelivr = "https://cdn.jsdelivr.net/gh/Sasukrystal/1@webgl-cdn/Build"
$staging = Join-Path $root "Builds\_gh_pages_hybrid"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $staging "Build") | Out-Null
Copy-Item WebGLDeploy\index.html $staging\
Copy-Item WebGLDeploy\.nojekyll $staging\
Copy-Item WebGLDeploy\TemplateData $staging\TemplateData -Recurse

$proxyArg = if ($ProxyUrl) { @("-x", $ProxyUrl) } else { @() }
foreach ($f in @("WebGL.loader.js", "WebGL.framework.js.unityweb", "WebGL.wasm.unityweb")) {
    Write-Host "Downloading $f ..."
    & curl.exe @proxyArg -fsSL --retry 3 -o (Join-Path $staging "Build\$f") "$jsdelivr/$f"
    $size = (Get-Item (Join-Path $staging "Build\$f")).Length
    Write-Host "  -> $size bytes"
    if ($size -lt 1000) { Write-Error "Download failed or LFS pointer: $f" }
}

Push-Location $staging
git init | Out-Null
git checkout -b gh-pages | Out-Null
git remote add origin "git@github.com:$Repo.git"
git add -A
git commit -m "WebGL hybrid deploy: same-origin loader/wasm/framework, LFS media data"
git push -u origin gh-pages --force
Pop-Location

Write-Host ""
Write-Host "已部署 gh-pages。约 1–3 分钟后打开:" -ForegroundColor Green
Write-Host "  https://sasukrystal.github.io/1/"
Write-Host "请先 F12 -> Application -> Clear site data，再 Ctrl+F5 刷新" -ForegroundColor Yellow
