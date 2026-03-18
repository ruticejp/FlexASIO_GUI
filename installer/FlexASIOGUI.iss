#define MyAppName "FlexASIO GUI"
#define MyAppVersion "0.36"
#define MyAppPublisher "https://github.com/ruticejp/FlexASIO_GUI"
#define MyAppURL ""
#define MyAppExeName "FlexASIOGUI.exe"

; This installer is built from the fork maintained by Rutice (https://github.com/ruticejp/FlexASIO_GUI).
; The original project is by dechamps (https://github.com/dechamps/FlexASIO_GUI), and their work is respected and credited.

; Target framework to package (change to net11.0-windows when shipping preview builds)
#define TargetFramework "net10.0-windows"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{85A2342E-43B3-4527-A533-6F250F1E5765}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
; Show that this installer comes from the Rutice fork while keeping the original app name.
AppVerName={#MyAppName} {#MyAppVersion} (Rutice fork)
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf64}\FlexASIOGUI
DisableProgramGroupPage=yes
;OutputDir=
OutputBaseFilename={#MyAppName}Installer_{#MyAppVersion}_Rutice
SetupIconFile=flexasiogui.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
;Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\bin\Release\{#TargetFramework}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
;Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKLM64; Subkey: "Software\Fabrikat"; Flags: uninsdeletekeyifempty
Root: HKLM64; Subkey: "Software\Fabrikat\FlexASIOGUI"; Flags: uninsdeletekey
Root: HKLM64; Subkey: "Software\Fabrikat\FlexASIOGUI\Install"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKLM64; Subkey: "Software\Fabrikat\FlexASIOGUI_Rutice\Install"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"

[Code]
function InitializeSetup(): Boolean;
var
  OldPath: string;
begin
  // If the old (original author) install path exists, copy it into the fork-specific key.
  if RegQueryStringValue(HKLM, 'Software\\Fabrikat\\FlexASIOGUI\\Install', 'InstallPath', OldPath) then
  begin
    RegWriteStringValue(HKLM, 'Software\\Fabrikat\\FlexASIOGUI_Rutice\\Install', 'InstallPath', OldPath);
  end;
  Result := True;
end;