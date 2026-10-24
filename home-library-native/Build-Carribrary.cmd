@echo off
cd /d "%~dp0"
where py >nul 2>nul
if errorlevel 1 (
    python Build-Carribrary.py %*
) else (
    py -3 Build-Carribrary.py %*
)
pause
