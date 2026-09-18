@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build-Installer.ps1" %*
if errorlevel 1 (
  echo Installer build failed. See the error above.
  pause
  exit /b 1
)
pause
