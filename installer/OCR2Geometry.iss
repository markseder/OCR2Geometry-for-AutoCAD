#ifndef StageDir
  #error StageDir must be supplied by Build-Installer.ps1
#endif
#ifndef OutputDir
  #error OutputDir must be supplied by Build-Installer.ps1
#endif
#ifndef RedistPath
  #error RedistPath must be supplied by Build-Installer.ps1
#endif
[Setup]
AppId={{1BAFDE30-A4BA-4E44-A77D-F73DF8E26F6A}
AppName=OCR2Geometry for AutoCAD
AppVersion=0.9.0
AppPublisher=Pavel Matveev
AppPublisherURL=https://github.com/markseder/OCR2Geometry-for-AutoCAD
DefaultDirName={autopf}\Autodesk\ApplicationPlugins\OCR2Geometry.bundle
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=OCR2Geometry-0.9.0-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
UninstallDisplayName=OCR2Geometry for AutoCAD
SetupLogging=yes
[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#RedistPath}"; DestName: "vc_redist.x64.exe"; Flags: dontcopy
[Code]
function AutoCADRunning: Boolean;
var Locator, Service, Processes: Variant;
begin
  Result := True;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Processes := Service.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name="acad.exe"');
    Result := Processes.Count > 0;
  except
    MsgBox('Cannot check whether AutoCAD is running. Please resolve the Windows WMI error and try again.', mbError, MB_OK);
  end;
end;
function InitializeSetup: Boolean;
var Release: Cardinal;
begin
  Result := False;
  if AutoCADRunning then begin
    MsgBox('Save your drawings and close AutoCAD before installing OCR2Geometry.', mbInformation, MB_OK);
    exit;
  end;
  if not RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then Release := 0;
  if Release < 461808 then begin
    MsgBox('Install .NET Framework 4.7.2 or later before installing OCR2Geometry.', mbError, MB_OK);
    exit;
  end;
  Result := True;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
  Result := '';
  if AutoCADRunning then begin
    Result := 'Save your drawings and close AutoCAD, then try again.';
    exit;
  end;
  ExtractTemporaryFile('vc_redist.x64.exe');
  if not Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'), '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, Code) then begin
    Result := 'Could not start the Microsoft Visual C++ runtime installer.';
    exit;
  end;
  { 1638 means a newer runtime is already installed. }
  if (Code <> 0) and (Code <> 3010) and (Code <> 1638) then
    Result := 'Microsoft Visual C++ runtime installation failed. Exit code: ' + IntToStr(Code);
  NeedsRestart := Code = 3010;
end;
function InitializeUninstall: Boolean;
begin
  Result := not AutoCADRunning;
  if not Result then MsgBox('Save your drawings and close AutoCAD before uninstalling OCR2Geometry.', mbInformation, MB_OK);
end;
