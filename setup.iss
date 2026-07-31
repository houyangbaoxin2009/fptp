; FPTP 证件照处理工具 — Inno Setup 安装脚本
; 编译：ISCC.exe setup.iss

#define MyAppName "FPTP"
#define MyAppVersion "1.2.0.0"
#define MyAppPublisher "FranJ2"
#define MyAppURL "https://github.com/houyangbaoxin2009/fptp"
#define MyAppExeName "fptp.exe"

[Setup]
AppId={{A8B3C4D5-E6F7-8901-2345-6789ABCDEF01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
OutputDir=installer
OutputBaseFilename=FPTP-v{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
ChangesEnvironment=yes

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："
Name: "addtopath"; Description: "添加到 PATH（可在 cmd 中直接使用 fptp 命令）"; GroupDescription: "路径配置："

[Files]
Source: "bin\Release\net48\publish\fptp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\fptp.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\fptp.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\register-path.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\*.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\*.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion recursesubdirs
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 FPTP"; Flags: postinstall nowait skipifsilent

[Registry]
; 添加/移除 PATH（由 Pascal 脚本处理，见下方代码）

[Code]

procedure AddToPath;
var
  InstallPath: string;
  CurrentPath: string;
begin
  InstallPath := ExpandConstant('{app}');
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
    CurrentPath := '';
  if Pos(LowerCase(InstallPath), LowerCase(CurrentPath)) = 0 then
  begin
    if CurrentPath <> '' then
      CurrentPath := CurrentPath + ';';
    CurrentPath := CurrentPath + InstallPath;
    if RegWriteExpandStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
      Log('PATH 添加成功: ' + InstallPath)
    else
      Log('PATH 添加失败');
  end
  else
    Log('PATH 已存在，跳过');
end;

procedure RemoveFromPath;
var
  InstallPath: string;
  CurrentPath: string;
  P: Integer;
begin
  InstallPath := ExpandConstant('{app}');
  if RegQueryStringValue(HKCU, 'Environment', 'Path', CurrentPath) then
  begin
    P := Pos(LowerCase(InstallPath), LowerCase(CurrentPath));
    if P > 0 then
    begin
      if (P > 1) and (CurrentPath[P - 1] = ';') then
        Delete(CurrentPath, P - 1, Length(InstallPath) + 1)
      else if (P + Length(InstallPath) <= Length(CurrentPath)) and (CurrentPath[P + Length(InstallPath)] = ';') then
        Delete(CurrentPath, P, Length(InstallPath) + 1)
      else
        Delete(CurrentPath, P, Length(InstallPath));
      RegWriteExpandStringValue(HKCU, 'Environment', 'Path', CurrentPath);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('addtopath') then
      AddToPath;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemoveFromPath;
end;
