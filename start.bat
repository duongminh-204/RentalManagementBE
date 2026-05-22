@echo off

echo =========================
echo Starting Rental API...
echo =========================

docker compose up -d --build

echo.
echo =========================
echo API Running:
echo http://localhost:8080/swagger
echo =========================

pause