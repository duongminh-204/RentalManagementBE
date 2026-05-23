@echo off
setlocal
cd /d "%~dp0"

echo =========================
echo Building & Starting Rental API...
echo =========================

docker compose down
docker compose up -d --build

echo.
echo =========================
echo Waiting for API startup...
timeout /t 10 >nul

echo =========================
echo API Running:
echo http://localhost:8090/swagger
echo =========================

pause