[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'release-floating-auto'))
$artifactsDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$repositoryPrefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

foreach ($targetDirectory in @($releaseDirectory, $artifactsDirectory)) {
    if (-not $targetDirectory.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "拒绝清理仓库以外的目录：$targetDirectory"
    }

    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory | Out-Null
    }

    Get-ChildItem -LiteralPath $targetDirectory -Force | Remove-Item -Recurse -Force
}

Push-Location $repositoryRoot
try {
    dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Token 用量测试失败。' }

    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw '展示层测试失败。' }

    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw '设置与迁移测试失败。' }

    dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw '刷新协调器测试失败。' }

    dotnet publish .\UsageLens.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $releaseDirectory

    if ($LASTEXITCODE -ne 0) { throw 'Release 发布失败。' }
}
finally {
    Pop-Location
}

$executablePath = Join-Path $releaseDirectory 'UsageLens.exe'
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "未生成预期的可执行文件：$executablePath"
}

Get-ChildItem -LiteralPath $releaseDirectory -Force |
    Where-Object { $_.Name -ne 'UsageLens.exe' } |
    Remove-Item -Recurse -Force

Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination (Join-Path $releaseDirectory 'LICENSE')

$quickStart = @"
UsageLens v$Version
========================

运行前提
--------
1. Windows 10 或 Windows 11 x64。
2. 本机已经安装并登录 Codex。
3. 便携包自带 .NET 运行组件，无需另装 .NET Runtime。

开始使用
--------
1. 解压 ZIP 内的全部文件。
2. 双击 UsageLens.exe；右下角会出现 UsageLens 图标。
3. 鼠标离开 1 秒后窗口会折叠到屏幕顶部；移入顶部把手即可展开。
4. 左右箭头切换界面，浮窗右键可打开控制中心、刷新或退出。
5. 托盘左键打开控制中心，双击显示/隐藏悬浮窗，右键可进入设置、切换样式、刷新或退出。

控制中心与隐私
--------------
控制中心展示与悬浮窗相同的一份额度和 Token 数据。程序复用本机 Codex 的已有登录状态，不读取或保存账号凭据；Token 统计仅扫描本机会话的用量元数据。

数据说明
--------
额度通过本机 codex app-server 读取。Token 只统计当前电脑 .codex\sessions 中保留的本地会话元数据，价格为估算值，不等同实际账单。程序不读取或上传对话正文，不保存 API Key 或密码。

项目主页
--------
https://github.com/miaotingxu/UsageLens
"@

[System.IO.File]::WriteAllText(
    (Join-Path $releaseDirectory 'README.txt'),
    $quickStart,
    [System.Text.UTF8Encoding]::new($true))

$archiveName = "UsageLens-v$Version-win-x64-portable.zip"
$archivePath = Join-Path $artifactsDirectory $archiveName
Compress-Archive -Path (Join-Path $releaseDirectory '*') -DestinationPath $archivePath -CompressionLevel Optimal

$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumLine = "$hash  $archiveName`n"
[System.IO.File]::WriteAllText(
    (Join-Path $artifactsDirectory 'SHA256SUMS.txt'),
    $checksumLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "便携目录：$releaseDirectory"
Write-Host "发布压缩包：$archivePath"
Write-Host "SHA256：$hash"
