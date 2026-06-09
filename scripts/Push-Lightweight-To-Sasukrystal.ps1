# 精简仓库后推送到 Sasukrystal/1
# 用法:
#   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
#   .\scripts\Push-Lightweight-To-Sasukrystal.ps1
#
# HTTPS 连不上时先运行:
#   .\scripts\Setup-GitHub-Ssh443.ps1

param(
    [string]$ProxyUrl = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$allowedRemotes = @(
    "https://github.com/Sasukrystal/1.git",
    "git@github.com:Sasukrystal/1.git"
)
$remoteUrl = (git remote get-url origin 2>$null)
if ($allowedRemotes -notcontains $remoteUrl) {
    Write-Error "origin 必须是 Sasukrystal/1 ，当前: $remoteUrl"
}

function Get-RemoteMainHash {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    try {
        $line = git ls-remote origin refs/heads/main 2>$null
        if (-not $line) { return $null }
        return ($line.Split("`t")[0]).Trim()
    } finally {
        $ErrorActionPreference = $prev
    }
}

function Invoke-RefreshIndexFromGitignore {
    git rm -r --cached . 2>$null | Out-Null
    git add -A
}

function Test-CommitHasIgnoredPacks {
    $n = (git ls-tree -r --name-only HEAD | Select-String "^(RogueArt_|Temp_RogueArt_|Unity_2D_Roguelike_CoreCombat_FullPack)" | Measure-Object).Count
    return $n -gt 0
}

function Test-GithubHttpsReachable {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    try {
        $r = Test-NetConnection github.com -Port 443 -WarningAction SilentlyContinue
        return [bool]$r.TcpTestSucceeded
    } finally {
        $ErrorActionPreference = $prev
    }
}

$localHash = (git rev-parse HEAD).Trim()
$remoteHash = Get-RemoteMainHash
if ($remoteHash -and $remoteHash -eq $localHash) {
    Write-Host "远程 main 已与本地一致，跳过推送。"
    exit 0
}

$commitCount = [int](git rev-list --count HEAD)
if ($commitCount -gt 1) {
    Write-Host "==> 合并为单次精简提交（去掉旧历史、截图、原始资源包、重复音频）..."
    $backupBranch = "backup-before-lightweight-push"
    git branch -f $backupBranch HEAD 2>$null
    git checkout --orphan github-main
    Invoke-RefreshIndexFromGitignore
    if (-not (git status --short)) {
        Write-Error "没有可提交文件"
    }
    git commit -m "Initial publish: Dark Dungeon Unity project (lightweight for GitHub)."
    git branch -M main
    Write-Host "    旧历史备份分支: $backupBranch"
} elseif (Test-CommitHasIgnoredPacks) {
    Write-Host "==> 当前提交仍含 RogueArt 原始包，正在重新应用 .gitignore ..."
    Invoke-RefreshIndexFromGitignore
    git commit --amend -m "Initial publish: Dark Dungeon Unity project (lightweight for GitHub)."
}

$blobMb = [math]::Round((
    git rev-list --objects HEAD |
    git cat-file --batch-check='%(objecttype) %(objectsize)' |
    Where-Object { $_ -match '^blob' } |
    ForEach-Object { [int]($_ -split ' ')[1] } |
    Measure-Object -Sum
).Sum / 1MB, 1)
Write-Host "    待推送体积约: ${blobMb} MB"

$useHttps = $remoteUrl -like "https://*"
if ($useHttps -and -not (Test-GithubHttpsReachable)) {
    Write-Host ""
    Write-Host "无法连接 github.com:443（HTTPS 被阻断）。" -ForegroundColor Yellow
    Write-Host "本机 ssh.github.com:443 通常可用，请改用 SSH：" -ForegroundColor Yellow
    Write-Host "  .\scripts\Setup-GitHub-Ssh443.ps1"
    Write-Host "  git remote set-url origin git@github.com:Sasukrystal/1.git"
    Write-Host "  .\scripts\Push-Lightweight-To-Sasukrystal.ps1"
    exit 1
}

Write-Host "==> 推送到 Sasukrystal/1 ..."
$attempts = 3
for ($i = 1; $i -le $attempts; $i++) {
    Write-Host "    尝试 $i / $attempts ..."
    if ($useHttps) {
        $gitArgs = @("-c", "http.postBuffer=1572864000", "-c", "http.version=HTTP/1.1", "-c", "core.compression=0")
        if ($ProxyUrl) { $gitArgs += @("-c", "http.proxy=$ProxyUrl", "-c", "https.proxy=$ProxyUrl") }
        & git @gitArgs push -u origin main --force
    } else {
        & git push -u origin main --force
    }
    if ($LASTEXITCODE -eq 0) {
        Write-Host "推送成功: https://github.com/Sasukrystal/1" -ForegroundColor Green
        exit 0
    }
    if ($i -lt $attempts) { Start-Sleep -Seconds 8 }
}

Write-Host ""
Write-Host "推送仍失败，可尝试:" -ForegroundColor Yellow
Write-Host "  1. SSH 443: .\scripts\Setup-GitHub-Ssh443.ps1"
Write-Host "  2. 代理: .\scripts\Push-Lightweight-To-Sasukrystal.ps1 -ProxyUrl http://127.0.0.1:7890"
Write-Host "  3. 换手机热点 / 开关 VPN 后重试"
exit 1
