@echo off
:: ============================================================
:: run-api.bat — Chạy ICare247 Backend API
:: URL: https://localhost:7130  (HTTPS)
::      http://localhost:5215   (HTTP)
:: Scalar UI: https://localhost:7130/scalar
:: ============================================================
title ICare247 - Backend API

cd /d "%~dp0src\backend"

echo.
echo  ========================================
echo   ICare247 Backend API
echo   https://localhost:7130
echo   Scalar UI: https://localhost:7130/scalar
echo  ========================================
echo.

dotnet run --project src\ICare247.Api\ICare247.Api.csproj --launch-profile https

pause
