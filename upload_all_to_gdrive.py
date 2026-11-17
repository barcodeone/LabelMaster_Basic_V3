# -*- coding: utf-8 -*-
"""
Google Drive 대상 폴더(18Ee0vrvlibAwPf9lP0HNeXpE2DENZv-h) 배포 스크립트
- 옵션:
  python upload_all_to_gdrive.py --windows-only  (Windows 배포본만 단독 업로드)
  python upload_all_to_gdrive.py --all           (Windows, Android, Mac, iOS 전체 플랫폼 통합 업로드)
"""

import os
import sys
import mimetypes
from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

# Windows 콘솔 UTF-8 설정
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

SCOPES = ['https://www.googleapis.com/auth/drive']
TARGET_FOLDER_ID = '18Ee0vrvlibAwPf9lP0HNeXpE2DENZv-h'


def authenticate_gdrive(base_dir):
    token_path = os.path.join(base_dir, 'token.json')
    if not os.path.exists(token_path):
        print(f"[ERROR] token.json 파일을 찾을 수 없습니다: {token_path}")
        sys.exit(1)

    creds = Credentials.from_authorized_user_file(token_path, SCOPES)
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            print("[*] 구글 드라이브 토큰 갱신 중...", flush=True)
            creds.refresh(Request())
            with open(token_path, 'w', encoding='utf-8') as f:
                f.write(creds.to_json())
        else:
            print("[ERROR] 유효한 구글 드라이브 인증 정보가 없습니다.")
            sys.exit(1)

    return build('drive', 'v3', credentials=creds)


def get_mimetype(filepath):
    mime, _ = mimetypes.guess_type(filepath)
    if mime:
        return mime
    if filepath.endswith('.dmg'):
        return 'application/x-apple-diskimage'
    if filepath.endswith('.apk'):
        return 'application/vnd.android.package-archive'
    if filepath.endswith('.aab'):
        return 'application/octet-stream'
    if filepath.endswith('.zip'):
        return 'application/zip'
    return 'application/octet-stream'


def upload_file_to_folder(service, local_filepath, parent_folder_id, display_name=None):
    if not os.path.exists(local_filepath):
        print(f"[SKIP] 로컬 파일이 존재하지 않습니다: {local_filepath}", flush=True)
        return None

    filename = display_name or os.path.basename(local_filepath)
    filesize = os.path.getsize(local_filepath)
    mimetype = get_mimetype(local_filepath)

    print(f"\n[*] 업로드 시작: {filename} ({filesize:,} bytes)", flush=True)
    print(f"    - 로컬 경로: {local_filepath}", flush=True)
    print(f"    - MIME 타입: {mimetype}", flush=True)

    # 폴더 내 동일 파일명 검색
    safe_name = filename.replace("'", "\\'")
    query = f"'{parent_folder_id}' in parents and name = '{safe_name}' and trashed = false"
    results = service.files().list(q=query, fields="files(id, name, webViewLink)").execute()
    items = results.get('files', [])

    media = MediaFileUpload(local_filepath, mimetype=mimetype, resumable=True)

    try:
        if items:
            file_id = items[0]['id']
            print(f"    [*] 기존 파일 업데이트 중... (File ID: {file_id})", flush=True)
            request = service.files().update(fileId=file_id, media_body=media, fields='id, name, webViewLink, size')
        else:
            print(f"    [+] 신규 파일 생성 중...", flush=True)
            body = {
                'name': filename,
                'parents': [parent_folder_id]
            }
            request = service.files().create(body=body, media_body=media, fields='id, name, webViewLink, size')

        response = None
        while response is None:
            status, response = request.next_chunk()
            if status:
                progress = int(status.progress() * 100)
                print(f"    -> 진행률: {progress}%", flush=True)

        view_link = response.get('webViewLink', f"https://drive.google.com/file/d/{response.get('id')}/view")
        print(f"    [SUCCESS] 완료! 링크: {view_link}", flush=True)
        return response
    except Exception as e:
        print(f"    [ERROR] 업로드 실패 ({filename}): {e}", flush=True)
        return None


def list_folder_contents(service, folder_id):
    print("\n==================================================")
    print("[Google Drive] 대상 폴더 최종 내용 목록:")
    print("==================================================")
    query = f"'{folder_id}' in parents and trashed = false"
    res = service.files().list(q=query, fields="files(id, name, size, modifiedTime, webViewLink)").execute()
    items = res.get('files', [])
    for f in items:
        size_str = f"{int(f.get('size', 0)):,} bytes" if 'size' in f else 'Folder'
        print(f"  - {f['name']:<35} | {size_str:>12} | {f.get('modifiedTime', '')}")


def get_current_version(base_dir):
    ver_path = os.path.join(base_dir, "version.txt")
    if os.path.exists(ver_path):
        try:
            with open(ver_path, "r", encoding="utf-8") as f:
                v = f.read().strip()
                if v:
                    return v
        except Exception:
            pass
    return "1.0.2"


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    is_windows_only = "--windows-only" in sys.argv or "--win" in sys.argv
    version = get_current_version(base_dir)

    service = authenticate_gdrive(base_dir)

    print("==================================================")
    if is_windows_only:
        print(f"Google Drive Windows 전용 배포 업로드 (v{version})")
    else:
        print(f"Google Drive 모든 플랫폼 통합 업로드 (Folder ID: {TARGET_FOLDER_ID})")
    print("==================================================")

    # 1. Windows 배포 대상 (항상 포함)
    targets = [
        {
            "path": os.path.join(base_dir, "bin", f"LabelMaster_v{version}.zip"),
            "name": f"LabelMaster_Windows_v{version}.zip"
        },
        {
            "path": os.path.join(base_dir, "bin", "LabelMaster.zip"),
            "name": "LabelMaster_Windows_Latest.zip"
        }
    ]

    repo_root = os.path.dirname(base_dir)

    # 2. 전체 플랫폼 통합 배포일 때만 타 플랫폼 추가
    if not is_windows_only:
        targets.extend([
            # Android
            {
                "path": os.path.join(repo_root, "Android", "app", "build", "outputs", "apk", "release", "LabelMaster.apk"),
                "name": "LabelMaster_Android.apk"
            },
            {
                "path": os.path.join(repo_root, "Android", "app", "build", "outputs", "bundle", "release", "LabelMaster.aab"),
                "name": "LabelMaster_Android_Store.aab"
            },
            # Mac
            {
                "path": os.path.join(repo_root, "Mac", "dist", "LabelMaster_Mac.dmg"),
                "name": "LabelMaster_Mac.dmg"
            },
            # iOS
            {
                "path": os.path.join(repo_root, "iOS", "AppBinary", "LabelMaster_iOS.app.zip"),
                "name": "LabelMaster_iOS.app.zip"
            }
        ])

    for item in targets:
        upload_file_to_folder(service, item["path"], TARGET_FOLDER_ID, item["name"])

    list_folder_contents(service, TARGET_FOLDER_ID)
    if is_windows_only:
        print(f"\n[SUCCESS] Windows 버전 (v{version})이 Google Drive에 성공적으로 업로드되었습니다!")
    else:
        print("\n[SUCCESS] 모든 플랫폼 파일이 Google Drive 폴더에 성공적으로 업로드되었습니다!")


if __name__ == "__main__":
    main()
