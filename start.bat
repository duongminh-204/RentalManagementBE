@echo off
setlocal

echo =========================
echo Building & Starting Rental API...
echo =========================

REM Step 1: Stop old containers (optional but safe)
docker compose down

REM Step 2: Build everything (force rebuild)
docker compose build --no-cache

REM Step 3: Start containers
docker compose up -d

echo.
echo =========================
echo Waiting for API startup...
timeout /t 10 >nul

echo =========================
echo API Running:
echo http://localhost:8090/swagger
echo =========================

pause