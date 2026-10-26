@echo off
chcp 65001 >nul
echo ========================================================
echo   LabelMaster 전체 플랫폼 Google Drive 배포 동기화
echo ========================================================
echo.
echo 대상 폴더: https://drive.google.com/drive/u/0/folders/18Ee0vrvlibAwPf9lP0HNeXpE2DENZv-h
echo.

python "%~dp0upload_all_to_gdrive.py"

echo.
pause
