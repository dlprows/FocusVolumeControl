@echo off
REM USAGE: Install.bat <DEBUG/RELEASE>
setlocal

REM cd to directory of install.bat
cd /d %~dp0

REM cd to bin/<Debug|Release>
cd bin/%1


SET DISTRIBUTION_TOOL="%~dp0%DistributionTool.exe"
SET STREAM_DECK_FILE="c:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
SET STREAM_DECK_LOAD_TIMEOUT=7

REM close processes
taskkill /f /im streamdeck.exe
taskkill /f /im FocusVolumeControl.exe
timeout /t 2

del com.dlprows.focusvolumecontrol.streamDeckPlugin
%DISTRIBUTION_TOOL% -b -i com.dlprows.focusvolumecontrol.sdPlugin -o ./
rmdir %APPDATA%\Elgato\StreamDeck\Plugins\com.dlprows.focusvolumecontrol.sdPlugin /s /q
START "" %STREAM_DECK_FILE%

timeout /t %STREAM_DECK_LOAD_TIMEOUT%
com.dlprows.focusvolumecontrol.streamDeckPlugin
