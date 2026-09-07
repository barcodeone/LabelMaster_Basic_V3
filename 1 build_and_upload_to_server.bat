@echo off
chcp 65001 >nul
setlocal

echo ==================================================
echo [0/5] Terminating running LabelMaster and compiler processes...
taskkill /f /im LabelMaster_Basic_v2.exe >nul 2>nul
taskkill /f /im LabelMaster_Basic.exe >nul 2>nul
taskkill /f /im LabelMaster.exe >nul 2>nul
taskkill /f /im VBCSCompiler.exe >nul 2>nul
taskkill /f /im msbuild.exe >nul 2>nul
taskkill /f /im obfuscar.console.exe >nul 2>nul

:: Clear obj temp files to prevent defender lock conflicts
del /f /q "%~dp0obj\Release\*.exe" >nul 2>nul
del /f /q "%~dp0obj\Release\*.pdb" >nul 2>nul

echo ==================================================
echo [1/5] Auto-incrementing project version...
python "%~dp0update_version.py"
if %errorlevel% neq 0 (
    echo [WARNING] Auto-versioning skipped.
)

echo ==================================================
echo [2/5] Searching for MSBuild.exe...
set MSBUILD_PATH=
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        set "MSBUILD_PATH=%%i"
    )
)

if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
)
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
)

if "%MSBUILD_PATH%"=="" (
    echo [ERROR] MSBuild.exe not found!
    pause
    exit /b 1
)

echo [INFO] Found MSBuild at: "%MSBUILD_PATH%"
echo [2/5] Building project (Release)...
"%MSBUILD_PATH%" "%~dp0BarcodePrint.csproj" /p:Configuration=Release /p:Platform=AnyCPU /p:UseSharedCompilation=false /nr:false /t:Build

if %errorlevel% neq 0 (
    echo [WARNING] Initial build attempt had lock issue. Retrying in 2 seconds...
    timeout /t 2 >nul
    "%MSBUILD_PATH%" "%~dp0BarcodePrint.csproj" /p:Configuration=Release /p:Platform=AnyCPU /p:UseSharedCompilation=false /nr:false /t:Build
)

if %errorlevel% neq 0 (
    echo [ERROR] Build failed! Please check if Microsoft Defender has locked the obj folder.
    echo [TIP] Add 'E:\4.sourcecode' to Windows Defender Exclusion list.
    pause
    exit /b %errorlevel%
)

echo [3/5] Build Completed!
echo ==================================================
echo [3.5/5] Running Obfuscar Code Obfuscation (Anti-Decompile)...
obfuscar.console "%~dp0obfuscar.xml"
if %errorlevel% neq 0 (
    echo [WARNING] Obfuscation failed or obfuscar.console not found in PATH. Using raw build output.
) else (
    echo [SUCCESS] Code Obfuscation and String Encryption Completed!
)

echo ==================================================
echo [4/5] Overwriting build outputs in LabelMaster_Basic_v2 (preserving user files)...
:: Create folder if not exist (never rmdir this directory to protect manual, samples and license text files)
if not exist "%~dp0bin\LabelMaster_Basic_v2" mkdir "%~dp0bin\LabelMaster_Basic_v2"
if not exist "%~dp0bin\LabelMaster_Basic_v2\Info" mkdir "%~dp0bin\LabelMaster_Basic_v2\Info"

:: Copy Obfuscated executable if available, otherwise raw executable
if exist "%~dp0bin\Release\Obfuscated\LabelMaster_Basic_v2.exe" (
    echo [INFO] Deploying Obfuscated protected executable...
    copy /y "%~dp0bin\Release\Obfuscated\LabelMaster_Basic_v2.exe" "%~dp0bin\LabelMaster_Basic_v2\" >nul
) else (
    copy /y "%~dp0bin\Release\LabelMaster_Basic_v2.exe" "%~dp0bin\LabelMaster_Basic_v2\" >nul
)

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

:: Remove unnecessary log, temp, and local serial dat files (prevent uploading local auth files, preserve server registered serials)
del "%~dp0bin\LabelMaster_Basic_v2\Info\PrinterSerialMatch.log" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\*.dat" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\serial_helper.html" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\app_settings.json" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\Info\print_history.json" 2>nul
del "%~dp0bin\LabelMaster_Basic_v2\print_history.json" 2>nul
rd /s /q "%~dp0bin\LabelMaster_Basic_v2\Info\server" 2>nul

if %errorlevel% neq 0 (
    echo [ERROR] Overwriting specified build files failed!
    pause
    exit /b 1
)
echo [INFO] Build artifacts copied successfully.

echo ==================================================
echo [5/5] Packaging and Uploading to xpix.co.kr...
:: Tar the release folder
tar -cf release_basic_v2.tar -C "%~dp0bin" LabelMaster_Basic_v2

echo [INFO] Transferring release_basic_v2.tar to remote server...
scp -O release_basic_v2.tar root@xpix.co.kr:/root/
if %errorlevel% neq 0 (
    echo [ERROR] SCP Upload failed!
    del release_basic_v2.tar 2>nul
    pause
    exit /b 1
)

echo [INFO] Extracting release files to both /root/labelmaster/basic and /root/labelmaster/basic_v2...
ssh root@xpix.co.kr "mkdir -p /root/labelmaster/basic && rm -rf /root/labelmaster/basic/* && tar -xf /root/release_basic_v2.tar -C /root/labelmaster/basic --strip-components=1 && mkdir -p /root/labelmaster/basic_v2 && rm -rf /root/labelmaster/basic_v2/* && tar -xf /root/release_basic_v2.tar -C /root/labelmaster/basic_v2 --strip-components=1 && rm /root/release_basic_v2.tar"
del release_basic_v2.tar 2>nul

echo ==================================================
echo [INFO] Synchronizing and uploading to Google Drive via server API...
curl.exe -s -X POST -H "Cookie: LabelMasterAuthSession=LabelMaster_Authorized_Success_2026" "https://xpix.co.kr/labelmaster/Serial/SyncAndZipToGDrive?app=basic" >nul

echo ==================================================
echo [SUCCESS] Build, upload, and Google Drive sync completed successfully!
echo.
echo [IMPORTANT NOTICE / 주의 사항]
echo - xpix.co.kr 서버의 다운로드 폴더(/root/labelmaster/basic)가 v2 신버전으로 완벽 교체되었습니다.
echo - 이제 xpix 웹사이트 및 구글 드라이브에서 다운로드 시 LabelMaster_Basic_v2가 다운로드됩니다.
echo.
pause
