﻿@echo off
setlocal
cd /d "%~dp0"

echo =========================
echo Pulling & Starting Rental API...
echo =========================

docker compose down
docker compose pull
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