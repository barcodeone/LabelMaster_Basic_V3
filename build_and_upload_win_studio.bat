@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ==================================================
echo [0/5] 실행 중인 LabelMaster 및 컴파일러 프로세스 정리...
taskkill /f /im LabelMaster.exe >nul 2>nul
taskkill /f /im LabelMaster_Basic_v2.exe >nul 2>nul
taskkill /f /im LabelMaster_Basic.exe >nul 2>nul
taskkill /f /im VBCSCompiler.exe >nul 2>nul
taskkill /f /im msbuild.exe >nul 2>nul
taskkill /f /im obfuscar.console.exe >nul 2>nul

:: Clear obj temp files to prevent defender lock conflicts
del /f /q "%~dp0obj\Release\*.exe" >nul 2>nul
del /f /q "%~dp0obj\Release\*.pdb" >nul 2>nul

echo ==================================================
echo [1/5] 빌드 버전 동기화 및 매뉴얼 문서 최신화...
python "%~dp0update_version.py"
python "%~dp0update_docs.py"

:: Read Version from version.txt
set "APP_VERSION=1.0.1"
if exist "%~dp0version.txt" (
    set /p APP_VERSION=<"%~dp0version.txt"
)
echo [INFO] 현재 빌드 버전: v%APP_VERSION%

:: 사용자 매뉴얼 최신화 (HTML 형식 배포)
echo [INFO] 사용자_매뉴얼.html 형식으로 배포 패키지에 포함됩니다.

echo ==================================================
echo [2/5] MSBuild 경로 검색 및 컴파일...
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
    echo [ERROR] MSBuild.exe를 찾을 수 없습니다!
    pause
    exit /b 1
)

echo [INFO] MSBuild 위치: "%MSBUILD_PATH%"
echo [2/5] 프로젝트 릴리즈 빌드 시작 (Release | AnyCPU)...
"%MSBUILD_PATH%" "%~dp0BarcodePrint.csproj" /p:Configuration=Release /p:Platform=AnyCPU /p:UseSharedCompilation=false /nr:false /t:Build

if %errorlevel% neq 0 (
    echo [WARNING] 빌드 잠금 재시도 중...
    timeout /t 2 >nul
    "%MSBUILD_PATH%" "%~dp0BarcodePrint.csproj" /p:Configuration=Release /p:Platform=AnyCPU /p:UseSharedCompilation=false /nr:false /t:Build
)

if %errorlevel% neq 0 (
    echo [ERROR] 빌드에 실패했습니다!
    pause
    exit /b %errorlevel%
)

echo [3/5] 빌드 성공!
echo ==================================================
echo [3.5/5] Obfuscar 코드 난독화 및 역컴파일 방지 적용...
obfuscar.console "%~dp0obfuscar.xml"
if %errorlevel% neq 0 (
    echo [WARNING] 난독화 실패 또는 obfuscar가 환경변수에 없습니다. 원본 빌드 출력을 사용합니다.
) else (
    echo [SUCCESS] 난독화 및 문자열 암호화 완료!
)

echo ==================================================
echo [4/5] 배포 폴더(bin\LabelMaster) 구성 및 패키징...
python "%~dp0package_deploy.py"
if %errorlevel% neq 0 (
    echo [ERROR] 배포 폴더 패키징 실패!
    pause
    exit /b %errorlevel%
)

echo ==================================================
echo [5/5] Cloudflare R2 버킷 자동 업로드 및 배포 검증...
python "%~dp0upload_to_r2.py"
if %errorlevel% neq 0 (
    echo [ERROR] Cloudflare R2 업로드 실패!
    pause
    exit /b %errorlevel%
)

:: xpix.co.kr 서버 동기화 (기존 basic 프로그램과 중복되지 않도록 windows 전용 경로로 배포)
if exist "%~dp0bin\LabelMaster" (
    echo.
    echo ==================================================
    echo [INFO] xpix.co.kr 원격 서버 동기화 진행...
    echo (기존 basic 프로그램과 분리된 /root/labelmaster/windows 경로로 배포됩니다)
    tar -cf release_labelmaster.tar -C "%~dp0bin" LabelMaster >nul 2>nul
    scp -O release_labelmaster.tar "%~dp0bin\LabelMaster_v%APP_VERSION%.zip" root@xpix.co.kr:/root/ >nul 2>nul
    if !errorlevel! equ 0 (
        ssh root@xpix.co.kr "mkdir -p /root/labelmaster/windows && rm -rf /root/labelmaster/windows/* && tar -xf /root/release_labelmaster.tar -C /root/labelmaster/windows --strip-components=1 && mv -f /root/LabelMaster_v%APP_VERSION%.zip /root/labelmaster/windows/ && chmod -R 755 /root/labelmaster/windows && rm -f /root/release_labelmaster.tar" >nul 2>nul
        del release_labelmaster.tar 2>nul
        echo [INFO] xpix.co.kr (/root/labelmaster/windows) 동기화 완료
    ) else (
        del release_labelmaster.tar 2>nul
        echo [INFO] xpix.co.kr 동기화 생략 또는 비밀번호 입력 필요 (Cloudflare R2 단독 배포 정상 완료)
    )
)

echo ==================================================
echo [SUCCESS] LabelMaster v%APP_VERSION% 빌드, 패키징 및 배포 완료!
echo.
echo [배포 결과 확인]
echo - 로컬 배포 폴더: bin\LabelMaster
echo - 로컬 실행 파일: bin\LabelMaster\LabelMaster.exe
echo - 로컬 압축 파일: bin\LabelMaster_v%APP_VERSION%.zip
echo.
echo [Cloudflare R2 다운로드 주소]
echo - https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev/LabelMaster_v%APP_VERSION%.zip
echo.
echo [xpix.co.kr 다운로드 주소 (기존 basic과 중복되지 않는 신규 주소)]
echo - https://xpix.co.kr/labelmaster/windows/download
echo.
pause
