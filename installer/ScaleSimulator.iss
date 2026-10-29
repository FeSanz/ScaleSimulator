; Simulador de Básculas — Windows installer (Inno Setup 6.1+). Built by build-installer.ps1, which
; passes AppVersion / StagingDir / OutputDir (and optionally the "sscsign" signing tool).
;
; The app ships whole: self-contained .NET single-file exe + its web UI. The only external
; dependency, the Microsoft Edge WebView2 Runtime (already present on every Windows 11), is checked
; and downloaded from Microsoft only when missing.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef StagingDir
  #error StagingDir must be defined (run build-installer.ps1)
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

#define AppName "Simulador de Básculas"
#define AppExe "ScaleSimulator.exe"
#define FirewallRule "Simulador de Basculas (TCP entrante)"
#define WebView2ClientKey "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"

[Setup]
AppId={{B7A41E52-6C3D-4F0A-9E18-5D2C7F3A8B61}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Felipe
VersionInfoVersion={#AppVersion}
VersionInfoDescription=Instalador de {#AppName}
; Per-user install, no admin prompt. Opening the firewall is an optional task that asks for
; elevation by itself.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\ScaleSimulator
DisableDirPage=yes
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=ScaleSimulator-Setup-{#AppVersion}
SetupIconFile=..\ScaleSim.App\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
#ifdef SignToolName
SignTool={#SignToolName}
SignedUninstaller=yes
#endif

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Opciones:"
Name: "startup"; Description: "Abrir el simulador automáticamente al iniciar sesión en Windows"; GroupDescription: "Opciones:"; Flags: unchecked
Name: "firewall"; Description: "Permitir conexiones entrantes en el Firewall de Windows (pide permisos de administrador)"; GroupDescription: "Red:"

[Files]
Source: "{#StagingDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "Abrir {#AppName} ahora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; WebView2 profile of the app. Registered scales (scales.json) are kept on purpose: reinstalling restores them.
Type: filesandordirs; Name: "{localappdata}\ScaleSimulator\WebView2"

[Code]
var
  DownloadPage: TDownloadWizardPage;

function WebView2Version(RootKey: Integer; SubKey: String): String;
begin
  if not RegQueryStringValue(RootKey, SubKey, 'pv', Result) then Result := '';
end;

function IsWebView2Installed: Boolean;
var
  V: String;
begin
  V := WebView2Version(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{#WebView2ClientKey}');
  if (V = '') or (V = '0.0.0.0') then
    V := WebView2Version(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{#WebView2ClientKey}');
  if (V = '') or (V = '0.0.0.0') then
    V := WebView2Version(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{#WebView2ClientKey}');
  Result := (V <> '') and (V <> '0.0.0.0');
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing),
    'Descargando el componente Microsoft Edge WebView2 Runtime (solo se descarga porque falta en esta PC)...', nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if (CurPageID = wpReady) and not IsWebView2Installed then
  begin
    DownloadPage.Clear;
    // Microsoft's official Evergreen bootstrapper (tiny; it fetches the runtime itself).
    DownloadPage.Add('https://go.microsoft.com/fwlink/p/?LinkId=2124703', 'MicrosoftEdgeWebview2Setup.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        DownloadPage.SetText('Instalando Microsoft Edge WebView2 Runtime...', '');
        if not Exec(ExpandConstant('{tmp}\MicrosoftEdgeWebview2Setup.exe'), '/silent /install', '',
                    SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
          MsgBox('No se pudo instalar WebView2 Runtime (código ' + IntToStr(ResultCode) + '). ' +
                 'El simulador se instalará igual, pero necesitará ese componente para abrir. ' +
                 'Puedes instalarlo después desde https://developer.microsoft.com/microsoft-edge/webview2/',
                 mbError, MB_OK);
      except
        MsgBox('No se pudo descargar WebView2 Runtime: ' + GetExceptionMessage + #13#10 +
               'El simulador se instalará igual; instala WebView2 después para poder abrirlo.', mbError, MB_OK);
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;

procedure CloseRunningApp;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM {#AppExe} /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  CloseRunningApp;
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  Args: String;
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('firewall') then
  begin
    // Elevates just this one netsh call (UAC prompt); the rest of the install stays per-user.
    Args := 'advfirewall firewall delete rule name="{#FirewallRule}" & ' +
            'netsh advfirewall firewall add rule name="{#FirewallRule}" dir=in action=allow protocol=TCP ' +
            'program="' + ExpandConstant('{app}\{#AppExe}') + '" enable=yes profile=any';
    if ShellExec('runas', ExpandConstant('{cmd}'), '/C netsh ' + Args, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
       and (ResultCode = 0) then
      RegWriteDWordValue(HKCU, 'Software\ScaleSimulator', 'FirewallRule', 1)
    else
      MsgBox('No se pudo crear la regla del Firewall. Windows la pedirá la primera vez que el simulador abra un puerto.',
             mbInformation, MB_OK);
  end;
end;

function InitializeUninstall: Boolean;
begin
  CloseRunningApp;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  Created: Cardinal;
begin
  if (CurUninstallStep = usUninstall) and
     RegQueryDWordValue(HKCU, 'Software\ScaleSimulator', 'FirewallRule', Created) and (Created = 1) then
  begin
    ShellExec('runas', ExpandConstant('{cmd}'),
              '/C netsh advfirewall firewall delete rule name="{#FirewallRule}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\ScaleSimulator');
  end;
end;
