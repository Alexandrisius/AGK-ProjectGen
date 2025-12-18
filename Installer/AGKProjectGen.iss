; ============================================
; AGK ProjectGen - Inno Setup Installer Script
; Modern, Beautiful Windows Installer
; ============================================

#define MyAppName "AGK ProjectGen"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "AGK Software"
#define MyAppURL "https://github.com/agk"
#define MyAppExeName "AGK.ProjectGen.UI.exe"
#define MyAppId "{{A8F2B3C4-D5E6-F7A8-B9C0-D1E2F3A4B5C6}"

[Setup]
; Основные настройки
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Директории
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes

; Выходной файл
OutputDir=..\bin\Installer
OutputBaseFilename=AGKProjectGen_Settings_v{#MyAppVersion}

; Визуальные настройки
SetupIconFile=Assets\AppIcon.ico
WizardStyle=modern
WizardSizePercent=120
WizardResizable=yes

; Изображения (должны быть в формате BMP)
WizardImageFile=Assets\WizardImage.bmp
WizardSmallImageFile=Assets\WizardSmallImage.bmp

; Сжатие
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Права и совместимость
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Дополнительные опции
DisableProgramGroupPage=yes
DisableWelcomePage=no
ShowLanguageDialog=auto
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; Версия установщика
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
russian.WelcomeLabel1=Добро пожаловать в мастер установки %n{#MyAppName}
russian.WelcomeLabel2=Программа установит {#MyAppName} версии {#MyAppVersion} на ваш компьютер.%n%nПеред установкой рекомендуется закрыть все работающие приложения.
russian.FinishedHeadingLabel=Установка {#MyAppName} завершена!
russian.FinishedLabel=Программа {#MyAppName} была успешно установлена на ваш компьютер.%n%nНажмите «Завершить» для выхода из мастера установки.
russian.LaunchProgram=Запустить {#MyAppName}
russian.CreateDesktopIcon=Создать ярлык на рабочем столе
russian.CreateQuickLaunchIcon=Создать ярлык в панели быстрого запуска

english.WelcomeLabel1=Welcome to {#MyAppName} Setup Wizard
english.WelcomeLabel2=This will install {#MyAppName} version {#MyAppVersion} on your computer.%n%nIt is recommended that you close all other applications before continuing.
english.FinishedHeadingLabel={#MyAppName} Setup Complete!
english.FinishedLabel={#MyAppName} has been successfully installed on your computer.%n%nClick "Finish" to exit the setup wizard.
english.LaunchProgram=Launch {#MyAppName}
english.CreateDesktopIcon=Create a desktop shortcut
english.CreateQuickLaunchIcon=Create a Quick Launch shortcut

[Tasks]
; Флаг checked удален, так как он дефолтный и вызывает ошибку в новых версиях ISCC, если считается невалидным
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Основные файлы приложения (self-contained publish)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Профили по умолчанию (если есть)
Source: "..\AGK.ProjectGen.UI\Profiles\*"; DestDir: "{app}\Profiles"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Dirs]
Name: "{app}\Data"; Permissions: users-modify
Name: "{app}\Logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Data"
Type: filesandordirs; Name: "{app}\Logs"

[Code]
var
  DownloadPage: TDownloadWizardPage;

// Инициализация установщика
function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Проверяем минимальную версию Windows
  if not IsWin64 then
  begin
    MsgBox('Приложение требует 64-битную версию Windows.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
end;

// Обработка завершения установки
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Регистрируем приложение в Windows
    RegWriteStringValue(HKEY_LOCAL_MACHINE, 
      'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}',
      '', ExpandConstant('{app}\{#MyAppExeName}'));
  end;
end;

// Очистка при удалении
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Удаляем запись из реестра
    RegDeleteKeyIncludingSubkeys(HKEY_LOCAL_MACHINE,
      'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}');
  end;
end;
