# 一次性配置：经 443 端口 SSH 推送 GitHub（HTTPS 被墙时常用）
# 用法:
#   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
#   .\scripts\Setup-GitHub-Ssh443.ps1

$ErrorActionPreference = "Stop"
$sshDir = Join-Path $env:USERPROFILE ".ssh"
$keyPath = Join-Path $sshDir "id_ed25519_github"
$configPath = Join-Path $sshDir "config"

New-Item -ItemType Directory -Force -Path $sshDir | Out-Null

if (-not (Test-Path $keyPath)) {
    Write-Host "==> 生成 SSH 密钥 ..."
    ssh-keygen -t ed25519 -C "sasukrystal-github" -f $keyPath -N '""'
} else {
    Write-Host "==> 已存在密钥: $keyPath"
}

$block = @"

Host github.com
    HostName ssh.github.com
    Port 443
    User git
    IdentityFile $keyPath
    IdentitiesOnly yes
"@

if (Test-Path $configPath) {
    $existing = Get-Content $configPath -Raw
    if ($existing -notmatch "Host github\.com") {
        Add-Content -Path $configPath -Value $block
        Write-Host "==> 已追加 github.com 到 $configPath"
    } else {
        Write-Host "==> $configPath 已有 github.com 配置，请手动确认 HostName=ssh.github.com Port=443"
    }
} else {
    Set-Content -Path $configPath -Value $block.TrimStart()
    Write-Host "==> 已创建 $configPath"
}

Write-Host ""
Write-Host "========== 下一步（在浏览器） ==========" -ForegroundColor Cyan
Write-Host "1. 登录 GitHub 账号 Sasukrystal"
Write-Host "2. 打开 https://github.com/settings/ssh/new"
Write-Host "3. Title 随意，Key 粘贴下面整段公钥："
Write-Host ""
Get-Content "$keyPath.pub"
Write-Host ""
Write-Host "4. 添加完成后在本机测试："
Write-Host "   ssh -T git@github.com"
Write-Host "5. 切换 remote 并推送："
Write-Host "   git remote set-url origin git@github.com:Sasukrystal/1.git"
Write-Host "   .\scripts\Push-Lightweight-To-Sasukrystal.ps1"
Write-Host "=======================================" -ForegroundColor Cyan
