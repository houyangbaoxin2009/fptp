# 内置模块信任名单生成脚本（防篡改）：
# 扫描模块根目录（默认输出目录 plugins/）下所有 module.json，计算主 DLL（entryPoint）的 SHA-256，
# 生成 {"modules": {"模块Id": ["sha256",...]}} 到 trusted-modules.json。
# 由 Osiris.App.csproj 的 GenerateTrustedModules Target 在构建后自动调用。
#
# 用法: powershell -File generate-trusted-modules.ps1 -ModuleRoot <plugins目录> -Output <输出json路径>

param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleRoot,
    [Parameter(Mandatory = $true)]
    [string]$Output
)

if (-not (Test-Path -LiteralPath $ModuleRoot)) {
    Write-Host "模块根目录不存在，跳过信任名单生成: $ModuleRoot"
    exit 0
}

$modules = @{}
$visited = @{}  # 已处理的模块目录（防 module.json 同名覆盖）

function Add-Module([string]$moduleDir) {
    $manifest = Join-Path $moduleDir "module.json"
    if (-not (Test-Path -LiteralPath $manifest)) { return }

    $json = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $json.id -or -not $json.entryPoint) { return }

    $dll = Join-Path $moduleDir $json.entryPoint
    if (-not (Test-Path -LiteralPath $dll)) {
        Write-Host "警告: 模块 $($json.id) 主 DLL 不存在: $dll"
        return
    }

    $hash = $null
    try {
        $hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    catch {
        Write-Host "警告: 模块 $($json.id) 哈希计算失败: $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($hash)) {
        Write-Host "错误: 模块 $($json.id) 未获得有效哈希，拒绝生成名单（防空哈希信任条目）"
        exit 1
    }
    if (-not $modules.ContainsKey($json.id)) {
        $modules[$json.id] = @()
    }
    $modules[$json.id] += $hash
    Write-Host "已登记模块 $($json.id) ($($json.entryPoint)): $hash"
}

# 扫描：目录根 + 一级子目录（与 ModuleLoader.FindManifests 同源）
$rootManifest = Join-Path $ModuleRoot "module.json"
if (Test-Path -LiteralPath $rootManifest) { Add-Module $ModuleRoot }
Get-ChildItem -LiteralPath $ModuleRoot -Directory | ForEach-Object { Add-Module $_.FullName }

if ($modules.Count -eq 0) {
    Write-Host "未发现任何模块，生成空信任名单。"
}

$result = @{ modules = $modules }
$dir = Split-Path -Parent $Output
if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$result | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $Output -Encoding UTF8
Write-Host "信任名单已生成: $Output（$($modules.Count) 个模块）"
