@echo off
setlocal
chcp 65001 >nul

echo ==================================================
echo Cloudflare R2 Uploader (Bucket: xprinter-drivers)
echo ==================================================

if "%~1"=="" goto AUTO_ALL
if "%~2"=="" goto SINGLE_DEFAULT_KEY
goto SINGLE_CUSTOM_KEY

:AUTO_ALL
echo [INFO] Auto-uploading single version zip archive (LabelMaster_v{VERSION}.zip)...
python "%~dp0upload_to_r2.py"
goto END

:SINGLE_DEFAULT_KEY
echo [INFO] Uploading single file: %~1
python "%~dp0upload_to_r2.py" "%~1"
goto END

:SINGLE_CUSTOM_KEY
echo [INFO] Uploading single file: %~1 as Key: %~2
python "%~dp0upload_to_r2.py" "%~1" "%~2"
goto END

:END
if %errorlevel% neq 0 (
    echo [ERROR] Upload failed with code %errorlevel%!
) else (
    echo [SUCCESS] Operation finished successfully!
)

echo.
pause
