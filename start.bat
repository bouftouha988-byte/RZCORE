@echo off
cd /d "%~dp0server"
set DISCORD_CLIENT_ID=1486434070723301396
set DISCORD_CLIENT_SECRET=gd2i7iDRk45MJGzWer-Unp3ISJfe4_-u
set SESSION_SECRET=change-this-to-a-long-random-text
set BASE_URL=https://rzcore-ks0n.onrender.com/
node server.js
pause
