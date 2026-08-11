# Osiris CLI → Windows Terminal 标签页注册脚本
# 把 "Osiris CLI" profile 追加到 Windows Terminal settings.json 的 profiles.list。
# 用法：在 PowerShell 中执行  .\scripts\register-wt-profile.ps1
# 执行后重启 Windows Terminal，新增标签页按钮旁会出现 Osiris 图标，点击即以新标签打开 osiris CLI。
$ErrorActionPreference = 'Stop'

# 1) 确定 osiris CLI 路径（Debug 输出；如需正式路径改为 dotnet publish 位置）
$repo = Split-Path -Parent $PSScriptRoot
$cliExe = Join-Path $repo 'src\Osiris.Cli\bin\Debug\net10.0\Osiris.Cli.exe'

# 2) Windows Terminal settings.json 位置（Win11 默认）
$wtSettings = Join-Path $env:LOCALAPPDATA 'Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json'
if (-not (Test-Path $wtSettings)) {
    Write-Host "未找到 Windows Terminal settings.json：$wtSettings（请先安装/运行一次 Windows Terminal）" -ForegroundColor Yellow
    exit 1
}

# 3) 构造 profile
$profile = [ordered]@{
    name        = 'Osiris CLI'
    commandline = "`"$cliExe`""
    icon        = 'C:\Users\Jiro\source\repos\fptp\src\Osiris.App\Assets\App.ico'
    tabTitle    = 'osiris'
    startingDirectory = $repo
}

# 4) 备份并合并
$backup = "$wtSettings.bak-osiris"
Copy-Item $wtSettings $backup -Force
$json = Get-Content $wtSettings -Raw | ConvertFrom-Json
if (-not $json.profiles) { $json | Add-Member -NotePropertyName profiles -NotePropertyValue ([ordered]@{}) }
if (-not $json.profiles.list) { $json.profiles | Add-Member -NotePropertyName list -NotePropertyValue @() }

$exists = @($json.profiles.list | Where-Object { $_.name -eq 'Osiris CLI' }).Count -gt 0
if ($exists) {
    Write-Host 'Osiris CLI profile 已存在，跳过。' -ForegroundColor Cyan
} else {
    $json.profiles.list += $profile
    $json | ConvertTo-Json -Depth 12 | Set-Content $wtSettings -Encoding utf8
    Write-Host "已注册 Osiris CLI 标签页（原配置备份：$backup）" -ForegroundColor Green
    Write-Host '重启 Windows Terminal 生效：点击标签页下拉箭头 → Osiris CLI'
}
