; QuickScope Inno Setup Script (User-Level Install)

[Setup]
; Basic App Info
AppName=QuickScope
AppVersion=1.0.0
AppPublisher=Carter Heinrichs
AppPublisherURL=https://github.com/carterheinrichs/QuickScope

; Install as the current user (No Admin prompt!)
PrivilegesRequired=lowest

; Routes to C:\Users\YourName\AppData\Local\Programs\QuickScope
DefaultDirName={autopf}\QuickScope

; Skips the "Create a Start Menu folder" screen
DisableProgramGroupPage=yes

; Output settings
OutputDir=.\InstallerOutput
OutputBaseFilename=QuickScopeSetup
SetupIconFile=QuickScope\Resources\QS.ico

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; Make it not cry about the QuickScope being open
CloseApplications=yes

[Tasks]
; Desktop icon is an option, but 'Flags: unchecked' means it is off by default
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The asterisk (*) grabs the .exe, all the .dlls, and any other files in the publish folder
Source: "QuickScope\bin\Release\net10.0-windows10.0.22621.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu Shortcut: Put directly into the Programs list (No subfolder)
Name: "{autoprograms}\QuickScope"; Filename: "{app}\QuickScope.exe"

; Desktop Shortcut: Only created if the user checks the box
Name: "{autodesktop}\QuickScope"; Filename: "{app}\QuickScope.exe"; Tasks: desktopicon

[Run]
; Launch the app immediately after installing
Filename: "{app}\QuickScope.exe"; Description: "{cm:LaunchProgram,QuickScope}"; Flags: nowait postinstall skipifsilent