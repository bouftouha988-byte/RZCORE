@echo off
set RES=
if exist logo.png set RES=/resource:logo.png,logo.png /resource:lt.png,lt.png /resource:lp.png,lp.png
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:RzCore.exe /win32manifest:rzcore.manifest %RES% /r:System.Windows.Forms.dll /r:System.Drawing.dll RzCore.cs
if exist RzCore.exe copy /y RzCore.exe ..\server\public\RzCore.exe >nul
pause
