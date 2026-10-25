@echo off
setlocal
set "hishiTestDir=%TEMP%\HishiAudio-tests-%RANDOM%-%RANDOM%"
mkdir "%hishiTestDir%"
echo Test artifacts: %hishiTestDir%
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /r:System.Web.Extensions.dll /out:"%hishiTestDir%\Tests.exe" "%~dp0Core.cs" "%~dp0tests\Tests.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\Tests.exe"
if errorlevel 1 exit /b 1
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /r:System.Web.Extensions.dll /out:"%hishiTestDir%\Integration.exe" "%~dp0Core.cs" "%~dp0tests\Integration.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\Integration.exe"
if errorlevel 1 exit /b 1
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /r:System.Web.Extensions.dll /r:System.Net.Http.dll /out:"%hishiTestDir%\LinkTests.exe" "%~dp0TrackLink.cs" "%~dp0tests\LinkTests.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\LinkTests.exe"
if errorlevel 1 exit /b 1
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /main:StartTests /r:System.Windows.Forms.dll /out:"%hishiTestDir%\StartTests.exe" "%~dp0PlayerStartup.cs" "%~dp0tests\StartTests.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\StartTests.exe"
if errorlevel 1 exit /b 1
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /out:"%hishiTestDir%\MotionTests.exe" "%~dp0DiscMotion.cs" "%~dp0tests\MotionTests.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\MotionTests.exe"
if errorlevel 1 exit /b 1
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /main:VisualTests /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:"%hishiTestDir%\VisualTests.exe" "%~dp0PlayerForm.cs" "%~dp0DiscMotion.cs" "%~dp0tests\VisualTests.cs"
if errorlevel 1 exit /b 1
"%hishiTestDir%\VisualTests.exe"
exit /b %errorlevel%




