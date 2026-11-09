#requires -Version 5.1
<#
.SYNOPSIS
    一键安装 / 更新 v2rayN 2.0（界面重制版）

.DESCRIPTION
    从 https://github.com/NZNL31/v2rayN-2.0 的 Release 下载最新包并解压。
    - 目标目录已有 v2rayN.exe 时，会先备份为 v2rayN.exe.bak-<时间戳>
    - guiConfigs / bin 等配置与内核目录不会被覆盖

.EXAMPLE
    # 安装到默认目录 %USERPROFILE%\v2rayN-2.0
    .\install.ps1

.EXAMPLE
    # 覆盖到现有安装目录（会先备份 exe）
    .\install.ps1 -TargetDir "D:\v2rayN"

.EXAMPLE
    # 走本地代理下载
    .\install.ps1 -Proxy "http://127.0.0.1:10808"
#>
[CmdletBinding()]
param(
    [string]$TargetDir = (Join-Path $env:USERPROFILE 'v2rayN-2.0'),
    [string]$Proxy,
    [string]$Repo = 'NZNL31/v2rayN-2.0'
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Info($m) { Write-Host "[install] $m" -ForegroundColor Cyan }
function Fail($m) { Write-Host "[install] $m" -ForegroundColor Red; exit 1 }

$iwr = @{ UseBasicParsing = $true; TimeoutSec = 60; Headers = @{ 'User-Agent' = 'v2rayn-2.0-installer' } }
if ($Proxy) { $iwr.Proxy = $Proxy }

Info "查询最新版本…"
try {
    $rel = (Invoke-WebRequest -Uri "https://api.github.com/repos/$Repo/releases/latest" @iwr).Content | ConvertFrom-Json
} catch {
    Fail "查询 Release 失败（仓库可能还没有发布版本）：$($_.Exception.Message)"
}
$tag = $rel.tag_name
$asset = $rel.assets | Where-Object { $_.name -eq 'v2rayN-windows-64.zip' } | Select-Object -First 1
if (-not $asset) { Fail "该 Release 里没有 v2rayN-windows-64.zip 资产" }
Info "最新版本: $tag  ($([math]::Round($asset.size/1MB,1)) MB)"

$tmp = Join-Path $env:TEMP ("v2rayn2-" + [guid]::NewGuid().ToString('N') + ".zip")
Info "下载中…"
try {
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tmp @iwr
} catch {
    Fail "下载失败：$($_.Exception.Message)"
}

if (-not (Test-Path $TargetDir)) { New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null }
$exe = Join-Path $TargetDir 'v2rayN.exe'
if (Test-Path $exe) {
    $bak = "$exe.bak-$tag-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $exe $bak -Force
    Info "已备份旧 exe: $(Split-Path $bak -Leaf)"
}

Info "解压到 $TargetDir …"
$stage = Join-Path $env:TEMP ("v2rayn2-" + [guid]::NewGuid().ToString('N'))
Expand-Archive -Path $tmp -DestinationPath $stage -Force
# 包里有一层 v2rayN-2.0 目录，取它下面的内容
$inner = Get-ChildItem $stage -Directory | Select-Object -First 1
$src = if ($inner) { $inner.FullName } else { $stage }
Copy-Item (Join-Path $src '*') $TargetDir -Recurse -Force

Remove-Item $tmp -Force -ErrorAction SilentlyContinue
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue

$installed = Join-Path $TargetDir 'v2rayN.exe'
if (-not (Test-Path $installed)) { Fail "解压后未找到 v2rayN.exe" }
$ver = (Get-Item $installed).VersionInfo.ProductVersion
Info "完成 ✓  版本 $ver"
Info "启动: $installed"
Info "提示：内核（Xray / mihomo / sing-box）与 GeoFiles 可在程序内「检查更新」里单独更新"
