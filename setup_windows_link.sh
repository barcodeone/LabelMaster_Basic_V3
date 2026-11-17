#!/bin/bash
set -e

NGINX_CONF="/etc/nginx/sites-available/logimaster"

if [ ! -f "$NGINX_CONF" ]; then
    echo "[ERROR] Nginx config not found at $NGINX_CONF"
    exit 1
fi

# Python을 이용한 안전하고 정확한 Nginx 블록 삽입 (sed 줄바꿈 이스케이프 오류 방지)
python3 - << 'EOF'
import sys

conf_path = "/etc/nginx/sites-available/logimaster"
with open(conf_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. 기존 labelmaster/windows 관련 설정 정리
import re
content = re.sub(r'location\s*\^?~?\s*/labelmaster/windows[^{]*\{[^}]*\}', '', content)

# 2. location /labelmaster 블록 바로 앞에 우선순위 매치(^~) 삽입
# Cloudflare R2 초고속 CDN 직링크로 302 리다이렉트
replacement = """    location ^~ /labelmaster/windows/download {
        return 302 https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev/LabelMaster.zip;
    }
    location ^~ /labelmaster/windows {
        return 302 https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev/LabelMaster.zip;
    }
    location /labelmaster {"""

if "location ^~ /labelmaster/windows" not in content:
    content = content.replace("location /labelmaster {", replacement, 1)

with open(conf_path, "w", encoding="utf-8") as f:
    f.write(content)

print("[INFO] Nginx configuration successfully updated in logimaster.")
EOF

# 3. Nginx 문법 검사 및 리로드
nginx -t
systemctl reload nginx
echo "[SUCCESS] Nginx updated! /labelmaster/windows/download now serves v3 directly!"
