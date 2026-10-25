@echo off
setlocal EnableDelayedExpansion
set "hishiFramework=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319"
set "hishiRefs="
for %%F in ("%SystemRoot%\System32\WinMetadata\*.winmd") do set "hishiRefs=!hishiRefs! /r:"%%~fF""
"%hishiFramework%\csc.exe" /nologo /target:winexe /resource:"%~dp0cover.jpg",hishi.cover /platform:x64 /win32icon:"%~dp0hishi.ico" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Net.Http.dll /r:"%hishiFramework%\System.Runtime.dll" /out:"%~dp0HishiAudio-0.6.3.exe" !hishiRefs! "%~dp0App.cs" "%~dp0Core.cs" "%~dp0TrackLink.cs" "%~dp0AssemblyInfo.cs" "%~dp0PlayerStartup.cs" "%~dp0PlayerForm.cs" "%~dp0DiscMotion.cs"
if errorlevel 1 exit /b 1
echo Native build OK






