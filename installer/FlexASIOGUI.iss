#define MyAppName "FlexASIO GUI"
#define MyAppVersion "0.36.0"
#define MyAppPublisher "https://github.com/ruticejp/FlexASIO_GUI"
#define MyAppURL ""
#define MyAppExeName "FlexASIOGUI.exe"

; This installer is built from the fork maintained by Rutice (https://github.com/ruticejp/FlexASIO_GUI).
; The original GUI project is by flipswitchingmonkey (https://github.com/flipswitchingmonkey/FlexASIO_GUI),
; based on FlexASIO by dechamps (https://github.com/dechamps/FlexASIO), and all work is respected and credited.

; Target framework to package
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
; Use a filename format close to the original installer naming, and indicate this is the Rutice fork with UTF-8 fix.
OutputBaseFilename=FlexASIO.GUIInstaller_{#MyAppVersion}_Rutice_UTF8fix
SetupIconFile=flexasiogui.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
;Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Only package the framework-dependent output (without RID subfolders like win-x64/win-x86).
; This keeps the installer small and avoids bundling redundant runtime-specific folders.
Source: "..\bin\Release\{#TargetFramework}\*"; DestDir: "{app}"; Flags: ignoreversion
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