#requires -Version 7.0
<#
.SYNOPSIS
    一条命令发布 v2rayN 2.0 新版本：改版本号 -> 编译 -> 打包 -> 建 Release -> 传资产。

.DESCRIPTION
    产物结构（AmazTool 会剥掉第一层目录）：
        v2rayN-2.0/v2rayN.exe + AmazTool.exe + 原生 dll + 使用说明

    资产名固定为 v2rayN-windows-64.zip（程序内自更新写死的名字）。

.EXAMPLE
    .\release.ps1 -Version 2.0.1
    .\release.ps1 -Version 2.0.1 -Notes "修复 xxx" -Draft
    .\release.ps1 -Version 2.0.1 -SkipBuild      # 用已有构建打包发布
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = '',
    [switch]$SkipBuild,
    [switch]$Draft,
    [string]$TokenFile = 'S:\v2rayN-2.0-kit\05-凭证\.gh-token',
    [string]$Proxy = 'http://127.0.0.1:10888'
)

$ErrorActionPreference = 'Stop'
$Repo = 'S:\v2rayN-2.0-kit\02-源码'
$Proj = Join-Path $Repo 'v2rayN\v2rayN.Desktop\v2rayN.Desktop.csproj'
$AmazProj = Join-Path $Repo 'v2rayN\AmazTool\AmazTool.csproj'
$AppOut = 'S:\v2rayN-build'
$AmazOut = 'S:\v2rayN-build-amaz'
$RelRoot = 'S:\v2rayN-release'
$PkgDir = Join-Path $RelRoot 'v2rayN-2.0'
$Zip = Join-Path $RelRoot 'v2rayN-windows-64.zip'
$Dotnet = 'F:\dotnet-sdk\dotnet.exe'
$Owner = 'NZNL31'; $RepoName = 'v2rayN-2.0'
$Api = "https://api.github.com/repos/$Owner/$RepoName"

function Info($m) { Write-Host "[release] $m" -ForegroundColor Cyan }
function Warn($m) { Write-Host "[release] $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "[release] $m" -ForegroundColor Red; exit 1 }

if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "版本号必须是 x.y.z（SemanticVersion 只认三段），收到: $Version" }

# ---------- 1. 版本号 ----------
$props = Join-Path $Repo 'v2rayN\Directory.Build.props'
$txt = [IO.File]::ReadAllText($props)
if ($txt -notmatch '<Version>[^<]+</Version>') { Fail "Directory.Build.props 里找不到 <Version>" }
$old = [regex]::Match($txt, '<Version>([^<]+)</Version>').Groups[1].Value
$txt = [regex]::Replace($txt, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
[IO.File]::WriteAllText($props, $txt, (New-Object System.Text.UTF8Encoding($true)))
Info "版本号 $old -> $Version"

# ---------- 2. 编译 ----------
if (-not $SkipBuild) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
    Info "编译主程序…"
    $log = & $Dotnet publish $Proj -c Release -r win-x64 --self-contained true -o $AppOut --nologo -v q 2>&1
    if ($LASTEXITCODE -ne 0) { ($log | Select-String ': error' | Select-Object -Last 6); Fail "主程序编译失败" }

    Info "编译 AmazTool…"
    $log = & $Dotnet publish $AmazProj -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $AmazOut --nologo -v q 2>&1
    if ($LASTEXITCODE -ne 0) { ($log | Select-String ': error' | Select-Object -Last 6); Fail "AmazTool 编译失败" }
}

# ---------- 3. 打包 ----------
if (Test-Path $RelRoot) { Remove-Item $RelRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $PkgDir | Out-Null
foreach ($f in 'v2rayN.exe', 'libSkiaSharp.dll', 'libHarfBuzzSharp.dll', 'e_sqlite3.dll', 'av_libglesv2.dll') {
    $src = Join-Path $AppOut $f
    if (-not (Test-Path $src)) { Fail "缺少构建产物: $src" }
    Copy-Item $src $PkgDir -Force
}
Copy-Item (Join-Path $AmazOut 'AmazTool.exe') $PkgDir -Force

$ver = (Get-Item (Join-Path $AppOut 'v2rayN.exe')).VersionInfo.ProductVersion
@"
v2rayN $Version（界面重制）
================================
基于上游 v2rayN 7.25.6，界面重制分支。
项目主页：https://github.com/$Owner/$RepoName

安装：解压后整个文件夹覆盖到现有 v2rayN 目录（guiConfigs / bin 保留不动）。
自更新：程序内「检查更新」里的 v2rayN 一项会检查本仓库的 Release。
"@ | Set-Content -Path (Join-Path $PkgDir '使用说明.txt') -Encoding utf8

Info "打包…"
$tmp = Join-Path $RelRoot ("tmp-" + [guid]::NewGuid().ToString('N').Substring(0, 6) + ".zip")
Compress-Archive -Path $PkgDir -DestinationPath $tmp -CompressionLevel Optimal -Force
Move-Item $tmp $Zip -Force
$sha = (Get-FileHash $Zip -Algorithm SHA256).Hash
"$sha  v2rayN-windows-64.zip" | Set-Content "$Zip.sha256" -Encoding ascii
Info ("包: {0:N1} MB  SHA256 {1}…" -f ((Get-Item $Zip).Length / 1MB), $sha.Substring(0, 16))

# ---------- 4. 提交版本号 ----------
Push-Location $Repo
$dirty = git status --porcelain
if ($dirty) {
    git -c user.name='custom' -c user.email='custom@local' add -A 2>&1 | Out-Null
    git -c user.name='custom' -c user.email='custom@local' commit -q -m "chore(release): v$Version" 2>&1 | Out-Null
    Info "已提交版本号变更"
}

# ---------- 5. 发布 ----------
$token = ''
if (Test-Path $TokenFile) { $token = (Get-Content $TokenFile -Raw).Trim() }
if (-not $token) {
    Warn "没有找到 token（$TokenFile），只打包不发布。手动发布步骤："
    Write-Host "  1) https://github.com/$Owner/$RepoName/releases/new"
    Write-Host "  2) Tag: $Version   Title: v2rayN $Version"
    Write-Host "  3) 上传 $Zip（资产名不要改）和 $Zip.sha256"
    Pop-Location
    return
}

$h = @{ Authorization = "Bearer $token"; 'User-Agent' = 'dsh'; Accept = 'application/vnd.github+json' }
$iwr = @{ Headers = $h; Proxy = $Proxy; TimeoutSec = 120; UseBasicParsing = $true }

$body = if ($Notes) { $Notes } else { "v2rayN $Version（界面重制）`n`n- 基于上游 v2rayN 7.25.6`n- 完整说明见 README.md / 示例.md" }
$payload = @{ tag_name = $Version; target_commitish = 'main'; name = "v2rayN $Version"; body = $body; draft = [bool]$Draft; prerelease = $false } | ConvertTo-Json -Depth 5
Info "创建 Release $Version …"
$rel = (Invoke-WebRequest -Uri "$Api/releases" -Method Post @iwr -Body ([Text.Encoding]::UTF8.GetBytes($payload)) -ContentType 'application/json; charset=utf-8').Content | ConvertFrom-Json

foreach ($file in @($Zip, "$Zip.sha256")) {
    $name = Split-Path $file -Leaf
    Info "上传 $name …"
    $up = @{ Authorization = "Bearer $token"; 'User-Agent' = 'dsh'; Accept = 'application/vnd.github+json' }
    $r = Invoke-WebRequest -Uri "https://uploads.github.com/repos/$Owner/$RepoName/releases/$($rel.id)/assets?name=$name" `
        -Method Post -Headers $up -Proxy $Proxy -TimeoutSec 1800 -UseBasicParsing -InFile $file -ContentType 'application/octet-stream'
    $a = $r.Content | ConvertFrom-Json
    Info "  -> $($a.name) $([math]::Round($a.size / 1MB, 1)) MB  state=$($a.state)"
}

Info "完成 ✓  $($rel.html_url)"
Info "程序内自更新地址: https://github.com/$Owner/$RepoName/releases/latest"
Pop-Location
