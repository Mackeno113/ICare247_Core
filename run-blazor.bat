@echo off
:: ============================================================
:: run-blazor.bat — Chạy ICare247 Blazor RuntimeCheck
:: URL: https://localhost:7017
:: Yêu cầu: Backend API phải đang chạy trên https://localhost:7130
:: ============================================================
title ICare247 - Blazor RuntimeCheck

cd /d "%~dp0src\backend"

echo.
echo  ========================================
echo   ICare247 Blazor RuntimeCheck
echo   https://localhost:7017
echo.
echo   [!] Yêu cau Backend API chay truoc:
echo       https://localhost:7130
echo  ========================================
echo.

dotnet run --project src\ICare247.Blazor.RuntimeCheck\ICare247.Blazor.RuntimeCheck.csproj --launch-profile https

pause
