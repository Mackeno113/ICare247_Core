@echo off
:: ============================================================
:: run-wpf.bat — Chạy ICare247 ConfigStudio WPF
:: App desktop để thiết kế form metadata (Ui_Form, Ui_Field,...)
:: ============================================================
title ICare247 - ConfigStudio WPF

cd /d "%~dp0src\frontend\ConfigStudio.WPF.UI"

echo.
echo  ========================================
echo   ICare247 ConfigStudio WPF
echo   App desktop thiet ke form metadata
echo  ========================================
echo.

dotnet run --project src\ConfigStudio.WPF.UI\ConfigStudio.WPF.UI.csproj

pause
