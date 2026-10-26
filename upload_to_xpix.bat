@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set "APP_VERSION=1.0.1"
if exist "%~dp0version.txt" (
    set /p APP_VERSION=<"%~dp0version.txt"
)

echo ==================================================
echo [1/3] xpix.co.kr 배포 파일 검사 (Windows 버전)...
echo - 버전: v%APP_VERSION%
echo - 대상 압축파일: bin\LabelMaster_v%APP_VERSION%.zip
echo - 대상 폴더: bin\LabelMaster
echo.

if not exist "%~dp0bin\LabelMaster_v%APP_VERSION%.zip" (
    echo [ERROR] bin\LabelMaster_v%APP_VERSION%.zip 파일이 없습니다! 먼저 빌드를 수행하세요.
    pause
    exit /b 1
)

echo ==================================================
echo [2/3] xpix.co.kr 서버로 파일 전송 (SCP)...
echo [INFO] 비밀번호를 요청하면 root 비밀번호를 입력하세요.
tar -cf release_labelmaster.tar -C "%~dp0bin" LabelMaster
scp -O release_labelmaster.tar "%~dp0bin\LabelMaster_v%APP_VERSION%.zip" root@xpix.co.kr:/root/
if %errorlevel% neq 0 (
    echo [ERROR] SCP 전송 실패!
    del release_labelmaster.tar 2>nul
    pause
    exit /b 1
)

echo ==================================================
echo [3/3] xpix 서버 원격 배치 (/root/labelmaster/windows)...
ssh root@xpix.co.kr "mkdir -p /root/labelmaster/windows && rm -rf /root/labelmaster/windows/* && tar -xf /root/release_labelmaster.tar -C /root/labelmaster/windows --strip-components=1 && mv -f /root/LabelMaster_v%APP_VERSION%.zip /root/labelmaster/windows/ && chmod -R 755 /root/labelmaster/windows && rm -f /root/release_labelmaster.tar"
del release_labelmaster.tar 2>nul

echo.
echo ==================================================
echo [SUCCESS] xpix 서버 업로드 완료! (기존 basic과 중복되지 않는 독립 주소)
echo - 서버 경로: /root/labelmaster/windows
echo - 다운로드 주소: https://xpix.co.kr/labelmaster/windows/download
echo ==================================================
pause
