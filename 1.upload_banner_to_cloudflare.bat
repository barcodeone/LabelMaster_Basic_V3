@echo off
chcp 65001 >nul
echo ========================================================
echo   Cloudflare 배너 HTML 업로더 (banner.html)
echo ========================================================
echo.

if exist "Info\banner_empty.html" (
    python upload_html_to_cloudflare.py Info\banner_empty.html banner.html
) else if exist "Info\quick_tools.html" (
    python upload_html_to_cloudflare.py Info\quick_tools.html banner.html
) else (
    echo [ERROR] 업로드할 HTML 파일을 찾을 수 없습니다.
)

echo.
echo [안내] 업로드 주소: https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev/banner.html
pause
