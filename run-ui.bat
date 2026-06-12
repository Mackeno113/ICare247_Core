@echo off
:: ============================================================
:: run-ui.bat — Chạy web end-user ICare247_UI
:: URL: https://localhost:7027 (tự mở trình duyệt)
:: Ghi chú: Shell + menu chạy độc lập, KHÔNG cần Backend API.
::          Chỉ trang "Công cụ (Dev)" mới cần API (https://localhost:7130).
:: ============================================================
title ICare247 - Web UI (ICare247_UI)

cd /d "%~dp0src\frontend"

echo.
echo  ========================================
echo   ICare247 — Web UI (ICare247_UI)
echo   https://localhost:7027
echo.
echo   [i] Shell + menu KHONG can Backend API.
echo  ========================================
echo.

dotnet run --project ICare247_UI\ICare247_UI.csproj --launch-profile https

pause
