@echo off
setlocal

REM CatosHoverInspector local dedicated-server launcher.
REM The plugin is client-only. The dedicated server is only the multiplayer
REM endpoint and must not receive CatosHoverInspector.dll.

set "REPO_DIR=%~dp0.."
for %%I in ("%REPO_DIR%") do set "REPO_DIR=%%~fI"
if not defined CHI_SERVER_INSTALL set "CHI_SERVER_INSTALL=C:\PROGRA~2\Steam\steamapps\common\Valheim dedicated server"
if not defined CHI_CLIENT_PROFILE set "CHI_CLIENT_PROFILE=C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosHoverInspector"
if not defined CHI_WORLD_DIR set "CHI_WORLD_DIR=C:\Users\magni\Downloads\Dedicated"
if not defined CHI_SAVE_DIR set "CHI_SAVE_DIR=C:\Users\magni\Downloads"
set "CHI_WORLD_MOUNT=%CHI_SAVE_DIR%\worlds_local\Dedicated"
set "CHI_DLL=%REPO_DIR%\src\CatosHoverInspector\bin\Release\net48\net48\CatosHoverInspector.dll"
set "CHI_CONFIG=%REPO_DIR%\TEST_SERVER\com.catosaur.catoshoverinspector.cfg"

if not exist "%CHI_SERVER_INSTALL%\valheim_server.exe" goto :no_server
if not exist "%CHI_SERVER_INSTALL%\valheim_server_Data\Managed\assembly_valheim.dll" goto :no_game_refs
if not exist "%CHI_CLIENT_PROFILE%\BepInEx" goto :no_client
if not exist "%REPO_DIR%\TEST_SERVER\adminlist.txt" goto :no_adminlist
if not exist "%CHI_WORLD_DIR%" goto :no_world
if not exist "%CHI_WORLD_DIR%\*.db2" goto :no_world
if not exist "%CHI_WORLD_DIR%\*.fwl2" goto :no_world
if not exist "%CHI_CONFIG%" goto :no_config
tasklist /FI "IMAGENAME eq valheim.exe" 2>nul | find /I "valheim.exe" >nul && goto :client_running

if not exist "%REPO_DIR%\scripts\build.ps1" goto :no_build_script
powershell -NoProfile -ExecutionPolicy Bypass -File "%REPO_DIR%\scripts\build.ps1"
if errorlevel 1 goto :build_failed
if not exist "%CHI_DLL%" goto :no_mod

REM Refuse to run a pre-refresh dedicated server against newer local refs.
if not exist "%REPO_DIR%\lib\assembly_valheim.dll" goto :no_local_ref
for /f %%A in ('powershell -NoProfile -Command "$server=(Get-Item ''%CHI_SERVER_INSTALL%\valheim_server_Data\Managed\assembly_valheim.dll'').LastWriteTimeUtc; $reference=(Get-Item ''%REPO_DIR%\lib\assembly_valheim.dll'').LastWriteTimeUtc; if($server -lt $reference){''STALE''}"') do if "%%A"=="STALE" goto :stale_game

if not exist "%CHI_SAVE_DIR%\worlds_local" mkdir "%CHI_SAVE_DIR%\worlds_local"
if errorlevel 1 goto :world_mount_failed

REM Match world-setup.md: create a junction only when the mount is absent.
REM A conflicting ordinary directory or wrong junction fails closed.
if exist "%CHI_WORLD_MOUNT%" goto :validate_mount
mklink /J "%CHI_WORLD_MOUNT%" "%CHI_WORLD_DIR%" >nul
if errorlevel 1 goto :world_mount_failed
goto :mount_valid

:validate_mount
powershell -NoProfile -Command "$p=Get-Item -LiteralPath '%CHI_WORLD_MOUNT%' -ErrorAction SilentlyContinue; if($null -eq $p -or -not ($p.Attributes -band [IO.FileAttributes]::ReparsePoint)){ exit 1 }; $t=$p.Target; if($null -eq $t){ exit 1 }; $resolved=(Get-Item -LiteralPath $t -ErrorAction SilentlyContinue).FullName; $expected=(Get-Item -LiteralPath '%CHI_WORLD_DIR%').FullName; if($resolved -ne $expected){ exit 1 }"
if errorlevel 1 goto :world_mount_failed
goto :mount_valid

:mount_valid
copy /Y "%REPO_DIR%\TEST_SERVER\adminlist.txt" "%CHI_SAVE_DIR%\adminlist.txt" >nul
if errorlevel 1 goto :admin_copy_failed
if not exist "%CHI_CLIENT_PROFILE%\BepInEx\plugins" mkdir "%CHI_CLIENT_PROFILE%\BepInEx\plugins"
copy /Y "%CHI_DLL%" "%CHI_CLIENT_PROFILE%\BepInEx\plugins\CatosHoverInspector.dll" >nul
if errorlevel 1 goto :client_copy_failed
if not exist "%CHI_CLIENT_PROFILE%\BepInEx\config" mkdir "%CHI_CLIENT_PROFILE%\BepInEx\config"
if not exist "%CHI_CLIENT_PROFILE%\BepInEx\config\com.catosaur.catoshoverinspector.cfg" copy /Y "%CHI_CONFIG%" "%CHI_CLIENT_PROFILE%\BepInEx\config\com.catosaur.catoshoverinspector.cfg" >nul

set "SteamAppId=892970"
echo.
echo ============================================================
echo  CatosHoverInspector local test server
echo ============================================================
echo  World:     Dedicated (%CHI_WORLD_DIR%)
echo  Save root: %CHI_SAVE_DIR%
echo  Mount:     %CHI_WORLD_MOUNT%
echo  Connect:   127.0.0.1:2462
echo  Client:    %CHI_CLIENT_PROFILE%\BepInEx\plugins\CatosHoverInspector.dll
echo  Server:    client-only endpoint; no plugin DLL deployed
echo  Admins:    %CHI_SAVE_DIR%\adminlist.txt
echo  Log:       %CHI_SERVER_INSTALL%\BepInEx\LogOutput.log
echo.
echo  Press CTRL+C to stop the server.
echo ============================================================
echo.

cd /d "%CHI_SERVER_INSTALL%"
"%CHI_SERVER_INSTALL%\valheim_server.exe" ^
    -name "CatosHoverInspector Test" ^
    -port 2462 ^
    -world "Dedicated" ^
    -password "696969" ^
    -savedir "%CHI_SAVE_DIR%" ^
    -public 0
goto :eof

:no_server
echo ERROR: valheim_server.exe was not found at %CHI_SERVER_INSTALL%
pause
exit /b 1
:no_game_refs
echo ERROR: Valheim managed assemblies are missing from the dedicated server.
pause
exit /b 1
:no_local_ref
echo ERROR: Refreshed local lib\assembly_valheim.dll is missing.
echo Run scripts\setup-references.ps1 before launching the test server.
pause
exit /b 1
:stale_game
echo ERROR: The dedicated server is older than the refreshed local references.
echo Update Valheim through Steam, then try again.
pause
exit /b 1
:no_mod
echo ERROR: CatosHoverInspector.dll was not produced by the build.
pause
exit /b 1
:no_client
echo ERROR: r2modman CatosHoverInspector profile was not found:
echo        %CHI_CLIENT_PROFILE%
echo Set CHI_CLIENT_PROFILE if the profile is installed elsewhere.
pause
exit /b 1
:build_failed
echo ERROR: The latest CatosHoverInspector build failed.
pause
exit /b 1
:client_copy_failed
echo ERROR: Could not deploy CatosHoverInspector.dll to the client profile.
pause
exit /b 1
:no_adminlist
echo ERROR: TEST_SERVER\adminlist.txt is missing.
pause
exit /b 1
:no_world
echo ERROR: The existing Dedicated world was not found at:
echo        %CHI_WORLD_DIR%
echo Expected a Valheim 1.0 world directory containing .db2 and .fwl2 files.
echo Set CHI_WORLD_DIR if the world is stored elsewhere.
pause
exit /b 1
:world_mount_failed
echo ERROR: The world mount is absent, invalid, or points at a different path:
echo        %CHI_WORLD_MOUNT%
echo Expected a directory junction to %CHI_WORLD_DIR%.
echo The launcher will not overwrite a conflicting directory or junction.
pause
exit /b 1
:no_config
echo ERROR: TEST_SERVER\com.catosaur.catoshoverinspector.cfg is missing.
pause
exit /b 1
:client_running
echo ERROR: Valheim is running and may have the client DLL locked.
echo Close Valheim completely, then run this launcher again.
pause
exit /b 1
:admin_copy_failed
echo ERROR: Could not install the test server admin list.
pause
exit /b 1
:no_build_script
echo ERROR: scripts\build.ps1 has not been created yet.
echo Complete the implementation scaffold before launching the test server.
pause
exit /b 1
