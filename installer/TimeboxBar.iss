#define MyAppName "TimeboxBar"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Jan Kerth"
#define MyAppURL "https://github.com/jankerth/TimeboxBar"
#define MyAppExeName "TimeboxBar.exe"

[Setup]
AppId={{8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=TimeboxBar-{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Kein Admin nötig — Tray-App ohne Systemdienst
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Keine eigene Taskleisten-Einträge
DisableStartupPrompt=yes
; Kleines Installer-Fenster
WizardSizePercent=100

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "TimeboxBar beim Windows-Start automatisch starten"; GroupDescription: "Extras:"; Flags: unchecked

[Files]
Source: "..\TimeboxBar\bin\Release\TimeboxBar.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; Flags: unchecked

[Registry]
; Autostart (nur wenn Task ausgewählt)
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} jetzt starten"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; App beenden vor Deinstallation
Filename: "taskkill.exe"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"
