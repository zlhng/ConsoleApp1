@echo off
rem Lights only: prints the COM port / IP of every OPT and LC18 light. Usage: LightScan.bat [subnet ...]
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe" set "PS=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0LightScan.ps1" %*
set RC=%ERRORLEVEL%
pause
exit /b %RC%
