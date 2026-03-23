@echo off
:: ============================================================
:: run-all.bat — Khởi động tất cả project ICare247
:: Mỗi project chạy trong cửa sổ CMD riêng biệt.
::
::   Cua so 1: Backend API      https://localhost:7130
::   Cua so 2: Blazor WASM      https://localhost:7017
::   Cua so 3: ConfigStudio WPF (desktop)
::
:: Thứ tự khởi động:
::   1. API khởi động trước (cần vài giây)
::   2. Blazor + WPF khởi động sau
:: ============================================================
title ICare247 - Launcher

echo.
echo  ============================================
echo   ICare247 — Khoi dong toan bo project
echo  ============================================
echo.
echo   [1] Backend API      https://localhost:7130
echo   [2] Blazor WASM      https://localhost:7017
echo   [3] ConfigStudio WPF (desktop)
echo.
echo   Moi project se mo trong cua so rieng.
echo.

:: ── Bước 1: Backend API ────────────────────────────────────
echo  >> Khoi dong Backend API...
start "ICare247 - Backend API" cmd /k "cd /d %~dp0src\backend && echo. && echo  https://localhost:7130 && echo. && dotnet run --project src\ICare247.Api\ICare247.Api.csproj --launch-profile https"

:: Đợi 5 giây để API khởi động trước
echo  >> Doi 5 giay de API san sang...
timeout /t 5 /nobreak >nul

:: ── Bước 2: Blazor RuntimeCheck ────────────────────────────
echo  >> Khoi dong Blazor RuntimeCheck...
start "ICare247 - Blazor" cmd /k "cd /d %~dp0src\backend && echo. && echo  https://localhost:7017 && echo. && dotnet run --project src\ICare247.Blazor.RuntimeCheck\ICare247.Blazor.RuntimeCheck.csproj --launch-profile https"

:: ── Bước 3: ConfigStudio WPF ───────────────────────────────
echo  >> Khoi dong ConfigStudio WPF...
start "ICare247 - ConfigStudio WPF" cmd /k "cd /d %~dp0src\frontend\ConfigStudio.WPF.UI && echo. && echo  ConfigStudio WPF && echo. && dotnet run --project src\ConfigStudio.WPF.UI\ConfigStudio.WPF.UI.csproj"

echo.
echo  >> Tat ca project da duoc khoi dong!
echo  >> Dong cua so nay hoac nhan phim bat ky.
echo.
pause
