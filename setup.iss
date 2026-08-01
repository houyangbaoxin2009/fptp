; FPTP 证件照处理工具 — Inno Setup 安装脚本
; 编译：ISCC.exe setup.iss

#define MyAppName "FPTP"
#define MyAppVersion "1.4.1.3"
#define MyAppPublisher "FranJ2"
#define MyAppURL "https://github.com/houyangbaoxin2009/fptp"
#define MyAppExeName "fptp.exe"
#define MyAppIcon "FPTP.ico"

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
SetupIconFile={#MyAppIcon}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ChangesEnvironment=yes

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinesesimp.DocsPageTitle=文档安装
chinesesimp.DocsPageDescription=选择要安装的文档格式
chinesesimp.DocsPageSubCaption=安装的文档将保存到应用目录，可在应用设置中切换。
chinesesimp.DocsOptionMd=Markdown 文档（.md）
chinesesimp.DocsOptionPdf=PDF 文档（.pdf）
chinesesimp.DocsOptionNone=不安装文档
english.DocsPageTitle=Documentation
english.DocsPageDescription=Select the documentation format to install
english.DocsPageSubCaption=Installed documents are saved to the app folder.
english.DocsOptionMd=Markdown documents (.md)
english.DocsOptionPdf=PDF documents (.pdf)
english.DocsOptionNone=Do not install documents

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："
Name: "addtopath"; Description: "添加到 PATH（可在 cmd 中直接使用 fptp 命令）"; GroupDescription: "路径配置："

[Files]
Source: "bin\Release\net48\publish\fptp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\fptp.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\fptp.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\register-path.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net48\publish\*.md"; DestDir: "{app}"; Flags: ignoreversion; Check: ShouldInstallDocs
Source: "bin\Release\net48\publish\*.pdf"; DestDir: "{app}"; Flags: ignoreversion; Check: ShouldInstallDocs
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
var
  DocsPage: TInputOptionWizardPage;
  SelectedDocsFormat: String;

procedure InitializeWizard;
begin
  // 静默模式（静默更新）不创建页面，SelectedDocsFormat 保持默认 'md'
  if not WizardSilent then
  begin
    DocsPage := CreateInputOptionPage(wpSelectTasks,
      ExpandConstant('{cm:DocsPageTitle}'),
      ExpandConstant('{cm:DocsPageDescription}'),
      ExpandConstant('{cm:DocsPageSubCaption}'),
      True, False);
    // 注意：6.7.3 中访问 TNewRadioButton.Checked 会报 "Could not call proc"，
    // 必须用 DocsPage.Values[] 索引读取
    DocsPage.Add(ExpandConstant('{cm:DocsOptionMd}'));
    DocsPage.Add(ExpandConstant('{cm:DocsOptionPdf}'));
    DocsPage.Add(ExpandConstant('{cm:DocsOptionNone}'));
    DocsPage.Values[0] := True;
  end;
  // 默认值；用户离开文档页时由 NextButtonClick 覆盖
  SelectedDocsFormat := 'md';
end;

function GetDocsFormat: String;
begin
  if DocsPage = nil then
    Result := 'md'
  else if DocsPage.Values[0] then
    Result := 'md'
  else if DocsPage.Values[1] then
    Result := 'pdf'
  else
    Result := 'none';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  // 用户离开文档选择页时缓存选择（此时控件存活；安装阶段控件已释放不可访问）
  if (not WizardSilent) and (DocsPage <> nil) and (CurPageID = DocsPage.ID) then
    SelectedDocsFormat := GetDocsFormat;
end;

function ShouldInstallDocs: Boolean;
begin
  Result := SelectedDocsFormat <> 'none';
end;

function GetInstallLang: String;
begin
  if ExpandConstant('{language}') = 'english' then
    Result := 'en-US'
  else
    Result := 'zh-CN';
end;

procedure WriteInstallOptions;
var
  JsonString: string;
begin
  JsonString := '{"docsFormat":"' + SelectedDocsFormat + '","installLang":"' + GetInstallLang + '"}';
  if SaveStringToFile(ExpandConstant('{app}\install-options.json'), JsonString, False) then
    Log('install-options.json 写入成功: ' + JsonString)
  else
    Log('install-options.json 写入失败');
end;

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
    // 静默模式（静默更新）不写 install-options.json，避免默认值覆盖用户偏好
    if not WizardSilent then
      WriteInstallOptions;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemoveFromPath;
end;
