# 内置模块信任名单生成脚本（防篡改）：
# 扫描模块根目录（默认输出目录 plugins/）下所有清单（module.data.tie 首选，兼容 module.json），
# 计算主 DLL（entryPoint）的 SHA-256，生成 tie:data 顶层表到 trusted-modules.data.tie：
#   type tie<data>
#   [ "modules": [ "模块Id": ["sha256", ...], ... ] ]
# 由 Osiris.App.csproj 的 GenerateTrustedModules Target 在构建后自动调用。
#
# 用法: pwsh -File generate-trusted-modules.ps1 -ModuleRoot <plugins目录> -Output <输出tie:data路径>

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
$visited = @{}  # 已处理的模块目录（防同名清单覆盖）

function Get-ManifestPath([string]$moduleDir) {
    $tieData = Join-Path $moduleDir "module.data.tie"
    if (Test-Path -LiteralPath $tieData) { return $tieData }
    $json = Join-Path $moduleDir "module.json"
    if (Test-Path -LiteralPath $json) { return $json }
    return $null
}

function Add-Module([string]$moduleDir) {
    $manifest = Get-ManifestPath $moduleDir
    if (-not $manifest) { return }

    $id = $null
    $entryPoint = $null
    if ($manifest.EndsWith(".data.tie")) {
        # tie:data 清单：提取 id / entryPoint（简易解析：行 "key": value）
        $lines = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8
        foreach ($line in $lines -split "`n") {
            $line = $line.Trim()
            if ($line -match '^"id"\s*:\s*"([^"]*)"') { $id = $Matches[1] }
            elseif ($line -match '^"entryPoint"\s*:\s*"([^"]*)"') { $entryPoint = $Matches[1] }
        }
    }
    else {
        # JSON 清单
        $json = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
        $id = $json.id
        $entryPoint = $json.entryPoint
    }
    if (-not $id -or -not $entryPoint) {
        Write-Host "警告: 清单缺少 id/entryPoint，跳过: $manifest"
        return
    }

    $dll = Join-Path $moduleDir $entryPoint
    if (-not (Test-Path -LiteralPath $dll)) {
        Write-Host "警告: 模块 $id 主 DLL 不存在: $dll"
        return
    }

    $hash = $null
    try {
        $hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    catch {
        Write-Host "警告: 模块 $id 哈希计算失败: $($_.Exception.Message)"
    }
    if ([string]::IsNullOrWhiteSpace($hash)) {
        Write-Host "错误: 模块 $id 未获得有效哈希，拒绝生成名单（防空哈希信任条目）"
        exit 1
    }
    if (-not $modules.ContainsKey($id)) {
        $modules[$id] = @()
    }
    $modules[$id] += $hash
    Write-Host "已登记模块 $id ($entryPoint): $hash"
}

# 扫描：目录根 + 一级子目录（与 ModuleLoader.FindManifests 同源）
Add-Module $ModuleRoot
Get-ChildItem -LiteralPath $ModuleRoot -Directory | ForEach-Object { Add-Module $_.FullName }

if ($modules.Count -eq 0) {
    Write-Host "未发现任何模块，生成空信任名单。"
}

# 输出 tie:data：type tie<data> 头部 + 顶层表（4 空格缩进 + 尾逗号）
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("type tie<data>")
[void]$sb.AppendLine()
[void]$sb.AppendLine("// 内置模块信任名单（构建后自动生成，防篡改；由 ModuleTrustStore 读取）")
[void]$sb.AppendLine("[")
[void]$sb.AppendLine('    "modules": [')
$first = $true
foreach ($id in $modules.Keys) {
    if (-not $first) { [void]$sb.AppendLine(",") }
    $first = $false
    [void]$sb.AppendLine("        `"$id`": [")
    for ($i = 0; $i -lt $modules[$id].Count; $i++) {
        $comma = if ($i -lt $modules[$id].Count - 1) { "," } else { "" }
        [void]$sb.AppendLine("            `"$($modules[$id][$i])`"$comma")
    }
    [void]$sb.Append("        ]")
}
[void]$sb.AppendLine("")
[void]$sb.AppendLine("    ],")
[void]$sb.AppendLine("]")

$dir = Split-Path -Parent $Output
if ($dir -and -not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
# UTF8 无 BOM（tiec 不接受带 BOM 的文件头）
[System.IO.File]::WriteAllText($Output, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "信任名单已生成: $Output（$($modules.Count) 个模块）"