@echo off
setlocal
chcp 65001 >nul
echo ========================================================
echo   Cloudflare 사이드바 배너 HTML 업로더 (sidebar_banner.html)
echo ========================================================
echo.

if exist "Info\sidebar_banner.html" (
    python upload_html_to_cloudflare.py "Info\sidebar_banner.html" "sidebar_banner.html"
) else (
    echo [ERROR] Info\sidebar_banner.html 파일을 찾을 수 없습니다.
)

echo.
echo [안내] 업로드 주소: https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev/sidebar_banner.html
pause
