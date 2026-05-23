@echo off
setlocal
cd /d "%~dp0"

echo =========================
echo Stopping Rental API...
echo =========================

docker compose down

echo Done.
pause