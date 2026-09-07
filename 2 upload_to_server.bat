@echo off
chcp 65001 >nul
setlocal

echo ==================================================
echo [1/3] Packaging LabelMaster_Basic_v2 release folder...
if not exist "%~dp0bin\LabelMaster_Basic_v2" (
    echo [ERROR] bin\LabelMaster_Basic_v2 folder not found!
    echo Please build the project or prepare the folder first.
    pause
    exit /b 1
)

:: Copy root Info folder to release directory if needed
if not exist "%~dp0bin\LabelMaster_Basic_v2\Info" mkdir "%~dp0bin\LabelMaster_Basic_v2\Info"

if exist "%~dp0LICENSES.txt" copy /y "%~dp0LICENSES.txt" "%~dp0bin\LabelMaster_Basic_v2\" >nul
if exist "%~dp0README_사용자안내.txt" copy /y "%~dp0README_사용자안내.txt" "%~dp0bin\LabelMaster_Basic_v2\" >nul
if exist "%~dp0엑셀파일_사용_가이드.txt" copy /y "%~dp0엑셀파일_사용_가이드.txt" "%~dp0bin\LabelMaster_Basic_v2\" >nul
if exist "%~dp0사용자_매뉴얼.html" copy /y "%~dp0사용자_매뉴얼.html" "%~dp0bin\LabelMaster_Basic_v2\" >nul
if exist "%~dp0*.pdf" copy /y "%~dp0*.pdf" "%~dp0bin\LabelMaster_Basic_v2\" >nul

:: 불필요한 dll, config, resource 폴더, 백업 폴더 완전 제거 (단일 exe에 모두 내장됨)
del "%~dp0bin\LabelMaster_Basic_v2\LabelMaster_Basic_v2.exe.config" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\LabelMaster.exe" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\LabelMaster.exe.config" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\JsPrinterDll.dll" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\printer.sdk.dll" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\XPrinterAuth.dll" 2>nul
rd /s /q "%~dp0bin\LabelMaster_Basic_v2\resource" 2>nul
rd /s /q "%~dp0bin\LabelMaster_Basic_v2\backup" 2>nul

:: Remove unnecessary log, temp, and local serial dat files (preserve server registered serials)
del "%~dp0bin\LabelMaster_Basic_v2\Info\PrinterSerialMatch.log" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\*.dat" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\serial_helper.html" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\app_settings.json" 2>nul
rd /s /q "%~dp0bin\LabelMaster_Basic_v2\Info\server" 2>nul

:: Tar the release folder
tar -cf release_basic_v2.tar -C "%~dp0bin" LabelMaster_Basic_v2
if %errorlevel% neq 0 (
    echo [ERROR] Packaging failed!
    pause
    exit /b 1
)

echo ==================================================
echo [2/3] Transferring release_basic_v2.tar to remote server...
scp -O release_basic_v2.tar root@xpix.co.kr:/root/
if %errorlevel% neq 0 (
    echo [ERROR] SCP Upload failed!
    del release_basic_v2.tar 2>nul
    pause
    exit /b 1
)

echo ==================================================
echo [3/3] Extracting release files to both /root/labelmaster/basic and /root/labelmaster/basic_v2...
ssh root@xpix.co.kr "mkdir -p /root/labelmaster/basic && rm -rf /root/labelmaster/basic/* && tar -xf /root/release_basic_v2.tar -C /root/labelmaster/basic --strip-components=1 && mkdir -p /root/labelmaster/basic_v2 && rm -rf /root/labelmaster/basic_v2/* && tar -xf /root/release_basic_v2.tar -C /root/labelmaster/basic_v2 --strip-components=1 && rm /root/release_basic_v2.tar"
del release_basic_v2.tar 2>nul

if %errorlevel% neq 0 (
    echo [ERROR] Extraction on remote server failed!
    pause
    exit /b 1
)

echo ==================================================
echo [INFO] Synchronizing and uploading to Google Drive via server API...
curl.exe -s -X POST -H "Cookie: LabelMasterAuthSession=LabelMaster_Authorized_Success_2026" "https://xpix.co.kr/labelmaster/Serial/SyncAndZipToGDrive?app=basic" >nul

echo ==================================================
echo [SUCCESS] Release folder uploaded and Google Drive sync completed successfully!
echo.
pause
