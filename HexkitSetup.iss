;
; -- HexkitSetup.iss --
;
; Creates the self-installing binary package
; for the Hexkit Strategy Game System.
;
; Use with Inno Setup version 5.2.3 or later.
;

[Setup]
AppName=Hexkit
AppPublisher=Christoph Nahr
AppPublisherURL=http://www.kynosarges.org/
AppVerName=Hexkit 4.3.3
AppVersion=4.3.3.0
VersionInfoVersion=4.3.3.0

AllowNoIcons=yes
ArchitecturesInstallIn64BitMode=x64
Compression=lzma
SolidCompression=yes
DefaultDirName={pf}\Hexkit
DefaultGroupName=Hexkit
MinVersion=0.0,5.01
OutputBaseFileName=HexkitSetup
OutputDir=..
SourceDir=bin
UninstallDisplayIcon={app}\Hexkit.Game.exe
UninstallDisplayName=Hexkit

[CustomMessages]
CreateDesktopIcon=Create &desktop icons

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Files]
Source: "*"; DestDir: "{app}"; Excludes: "Rules.csproj*"; Flags: createallsubdirs ignoreversion recursesubdirs

[Dirs]
Name: "{app}\Images"; Permissions: users-modify
Name: "{app}\Scenario"; Permissions: users-modify

[Icons]
Name: "{group}\Hexkit Game"; Filename: "{app}\Hexkit.Game.exe"; WorkingDir: "{app}"; Comment: "Play games based on Hexkit scenarios."
Name: "{group}\Hexkit Editor"; Filename: "{app}\Hexkit.Editor.exe"; WorkingDir: "{app}"; Comment: "Create and edit Hexkit scenarios."
Name: "{group}\Hexkit Help"; Filename: "{app}\Hexkit.Help.chm"; WorkingDir: "{app}"; Comment: "Show online help for Hexkit Game and Hexkit Editor."
Name: "{group}\Read Me"; Filename: "{app}\ReadMe.html"; WorkingDir: "{app}"; Comment: "Read about installing and troubleshooting Hexkit."
Name: "{group}\What's New"; Filename: "{app}\WhatsNew.html"; WorkingDir: "{app}"; Comment: "Show changes in this and earlier Hexkit releases."
Name: "{group}\Uninstall Hexkit"; Filename: "{uninstallexe}"; Comment: "Remove Hexkit from your system."

Name: "{commondesktop}\Hexkit Game"; Filename: "{app}\Hexkit.Game.exe"; WorkingDir: "{app}"; Comment: "Play games based on Hexkit scenarios."; Tasks: desktopicon
Name: "{commondesktop}\Hexkit Editor"; Filename: "{app}\Hexkit.Editor.exe"; WorkingDir: "{app}"; Comment: "Create and edit Hexkit scenarios."; Tasks: desktopicon

[Run]
Filename: "{%COMSPEC|{cmd}}"; Parameters: "/c start ReadMe.html"; WorkingDir: "{app}"; Description: "View Read Me file"; Flags: postinstall runhidden skipifsilent
Filename: "{app}\Hexkit.Game.exe"; WorkingDir: "{app}"; Description: "Start Hexkit Game"; Flags: postinstall skipifsilent

[Code]
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // or 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // or 393297 on Windows 8.1 and older
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;

function InitializeSetup(): Boolean;
begin
    if not IsDotNetDetected('v4\Client', 0) then begin
        MsgBox('Hexkit requires Microsoft .NET Framework 4.0 Client Profile.'#13#13
            'Please use Windows Update to install this version,'#13
            'and then re-run the Hexkit setup program.', mbInformation, MB_OK);
        result := false;
    end else
        result := true;
end;

