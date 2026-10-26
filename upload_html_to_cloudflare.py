# -*- coding: utf-8 -*-
"""
Cloudflare R2에 HTML 파일 업로드 스크립트
"""
import os
import sys
import boto3
from botocore.config import Config

# Windows 콘솔 UTF-8 설정
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

ENDPOINT_URL = "https://653bb8b500f44aca9e1fa38aa0ef3ce0.r2.cloudflarestorage.com"
ACCESS_KEY_ID = "0adbbdc7bb8f6c3fea9170de38221cf4"
SECRET_ACCESS_KEY = "e29c5e2d6283f1558c391fc12a103560ba33d65bb2fda3e10a91dffe08d0b084"
BUCKET_NAME = "xprinter-drivers"
PUBLIC_DOMAIN = "https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev"

def upload_html(local_path, target_key):
    s3 = boto3.client(
        service_name='s3',
        endpoint_url=ENDPOINT_URL,
        aws_access_key_id=ACCESS_KEY_ID,
        aws_secret_access_key=SECRET_ACCESS_KEY,
        config=Config(signature_version='s3v4')
    )
    
    with open(local_path, "rb") as f:
        s3.put_object(
            Bucket=BUCKET_NAME,
            Key=target_key,
            Body=f,
            ContentType="text/html; charset=utf-8",
            CacheControl="no-cache, no-store, must-revalidate"
        )
    public_url = f"{PUBLIC_DOMAIN}/{target_key}"
    print(f"[OK] Uploaded {local_path} -> {public_url}")
    return public_url

if __name__ == "__main__":
    if len(sys.argv) > 2:
        local_file = sys.argv[1]
        target_name = sys.argv[2]
    else:
        local_file = os.path.join(os.path.dirname(__file__), "Info", "quick_tools.html")
        target_name = "quick_tools.html"
    
    upload_html(local_file, target_name)
