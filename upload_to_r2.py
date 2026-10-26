# -*- coding: utf-8 -*-
"""
Cloudflare R2 버킷 단일 압축 파일(LabelMaster_v{VERSION}.zip) 자동 업로드 및 검증 스크립트
- 규정: 실행파일, 매뉴얼 등 개별 파일 업로드 금지 / 오직 버전 압축파일 1개만 업로드
"""

import os
import sys

# Windows 콘솔 UTF-8 설정
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

try:
    import boto3
    from botocore.config import Config
    from botocore.exceptions import ClientError
except ImportError:
    print("[ERROR] boto3 라이브러리가 필요합니다. 'pip install boto3'를 실행하세요.")
    sys.exit(1)

# Cloudflare R2 접속 및 인증 정보
ENDPOINT_URL = "https://653bb8b500f44aca9e1fa38aa0ef3ce0.r2.cloudflarestorage.com"
ACCESS_KEY_ID = "0adbbdc7bb8f6c3fea9170de38221cf4"
SECRET_ACCESS_KEY = "e29c5e2d6283f1558c391fc12a103560ba33d65bb2fda3e10a91dffe08d0b084"
BUCKET_NAME = "xprinter-drivers"
PUBLIC_DOMAIN = "https://pub-50e160c2ce5d41f6a4e2edcf7267f555.r2.dev"


def get_s3_client():
    return boto3.client(
        service_name='s3',
        endpoint_url=ENDPOINT_URL,
        aws_access_key_id=ACCESS_KEY_ID,
        aws_secret_access_key=SECRET_ACCESS_KEY,
        config=Config(signature_version='s3v4')
    )


def upload_zip_to_r2(s3, local_path, target_key):
    """단일 압축 파일을 Cloudflare R2에 업로드합니다."""
    if not os.path.exists(local_path):
        print(f"[ERROR] 로컬 압축 파일을 찾을 수 없습니다: {local_path}")
        return False

    file_size = os.path.getsize(local_path)
    content_type = "application/x-zip-compressed"

    print(f"\n[R2 업로드 시작] {local_path} ({file_size:,} bytes)")
    print(f" -> Bucket: [{BUCKET_NAME}] Key: '{target_key}'")

    try:
        with open(local_path, "rb") as f:
            s3.put_object(
                Bucket=BUCKET_NAME,
                Key=target_key,
                Body=f,
                ContentType=content_type
            )
        public_url = f"{PUBLIC_DOMAIN}/{target_key}"
        print(f"[업로드 성공] 다운로드 URL: {public_url}")
        return True
    except ClientError as e:
        print(f"[ERROR] 업로드 실패: {e}")
        return False
    except Exception as ex:
        print(f"[ERROR] 오류 발생: {ex}")
        return False


def verify_bucket_contents(s3, verified_key):
    """버킷 내 객체 목록을 조회하여 업로드 상태를 검증합니다."""
    print("\n==================================================")
    print(f"[{BUCKET_NAME}] 버킷 내용 조회 및 배포 검증:")
    print("==================================================")
    try:
        res = s3.list_objects_v2(Bucket=BUCKET_NAME)
        contents = res.get("Contents", [])
        if not contents:
            print("  (버킷에 파일이 없습니다)")
            return

        for item in contents:
            key = item['Key']
            size = item['Size']
            modified = item.get('LastModified', '')
            status = " [NEW UPLOAD VERIFIED]" if key == verified_key else ""
            print(f"  - Key: {key:<35} | Size: {size:>12,} bytes | {modified}{status}")
    except ClientError as e:
        print(f"[WARNING] 버킷 목록 조회 실패: {e}")


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))

    # 1. 빌드 버전 읽기
    version_file = os.path.join(base_dir, "version.txt")
    version = "1.0.1"
    if os.path.exists(version_file):
        with open(version_file, "r", encoding="utf-8") as f:
            version = f.read().strip()

    # 업로드 대상: 오직 버전 압축 파일 하나만 지정
    target_zip_name = f"LabelMaster_v{version}.zip"
    target_zip_path = os.path.join(base_dir, "bin", target_zip_name)

    # 명령행 인자로 특정 ZIP 경로가 지정된 경우
    if len(sys.argv) >= 2:
        custom_path = sys.argv[1]
        if not os.path.isabs(custom_path):
            custom_path = os.path.join(base_dir, custom_path)
        if not custom_path.lower().endswith(".zip"):
            print(f"[ERROR] 정책 위반: ZIP 압축 파일만 업로드할 수 있습니다! (입력: {custom_path})")
            sys.exit(1)
        target_zip_path = custom_path
        target_zip_name = os.path.basename(custom_path)

    print("==================================================")
    print(f"Cloudflare R2 단일 압축 파일 배포 (v{version})")
    print(f"- Endpoint:      {ENDPOINT_URL}")
    print(f"- Bucket:        {BUCKET_NAME}")
    print(f"- 대상 압축파일: {target_zip_name}")
    print("※ 정책: 실행파일, 매뉴얼 등 개별 파일 업로드 금지 (단일 압축파일만 업로드)")
    print("==================================================")

    s3 = get_s3_client()

    # 단일 압축 파일 업로드 실행
    success = upload_zip_to_r2(s3, target_zip_path, target_zip_name)
    if not success:
        sys.exit(1)

    # 버킷 목록 검증
    verify_bucket_contents(s3, verified_key=target_zip_name)

    download_url = f"{PUBLIC_DOMAIN}/{target_zip_name}"
    print("\n==================================================")
    print("[SUCCESS] Cloudflare R2 단일 압축 파일 업로드 완료!")
    print(f"다운로드 주소: {download_url}")
    print("==================================================")


if __name__ == "__main__":
    main()
