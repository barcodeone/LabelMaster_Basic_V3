import os
import re
from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

# drive scope allows full control over files in user's Drive.
SCOPES = ['https://www.googleapis.com/auth/drive']

# 대상 구글 드라이브 배포 폴더 ID
TARGET_FOLDER_ID = '1THDfsTCunW5gQIcwSuv0n31-PXk7L6xl'

def authenticate_gdrive(base_dir):
    creds = None
    token_path = os.path.join(base_dir, 'token.json')
    credentials_path = os.path.join(base_dir, 'credentials.json')

    if os.path.exists(token_path):
        creds = Credentials.from_authorized_user_file(token_path, SCOPES)
    
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            try:
                print("[*] 기존 인증 토큰 갱신 시도 중...", flush=True)
                creds.refresh(Request())
            except Exception as ex:
                print(f"[!] 토큰 갱신 실패: {ex}", flush=True)
                if os.path.exists(token_path):
                    try:
                        os.remove(token_path)
                    except:
                        pass
                flow = InstalledAppFlow.from_client_secrets_file(credentials_path, SCOPES)
                creds = flow.run_local_server(port=0)
        else:
            flow = InstalledAppFlow.from_client_secrets_file(credentials_path, SCOPES)
            creds = flow.run_local_server(port=0)
        with open(token_path, 'w') as token:
            token.write(creds.to_json())
    
    return build('drive', 'v3', credentials=creds)

def get_or_create_remote_folder(service, name, parent_id):
    # 지정한 부모 폴더 아래에 특정 이름의 서브 폴더가 있는지 검색
    query = f"mimeType = 'application/vnd.google-apps.folder' and '{parent_id}' in parents and name = '{name}' and trashed = false"
    results = service.files().list(q=query, fields="files(id, name)").execute()
    items = results.get('files', [])
    if items:
        return items[0]['id']
    else:
        # 폴더 새로 생성
        file_metadata = {
            'name': name,
            'mimeType': 'application/vnd.google-apps.folder',
            'parents': [parent_id]
        }
        folder = service.files().create(body=file_metadata, fields='id').execute()
        print(f"  [+] 구글 드라이브 폴더 생성: {name} (ID: {folder.get('id')})", flush=True)
        return folder.get('id')

def get_mimetype(filepath):
    import mimetypes
    mime, _ = mimetypes.guess_type(filepath)
    if mime is None:
        if filepath.endswith('.dat'):
            return 'application/octet-stream'
        return 'application/octet-stream'
    return mime

def upload_file_to_folder(service, local_filepath, parent_folder_id):
    filename = os.path.basename(local_filepath)
    mimetype = get_mimetype(local_filepath)
    
    # 해당 구글 드라이브 폴더 내에 동일 이름의 파일이 이미 있는지 조회
    query = f"mimeType != 'application/vnd.google-apps.folder' and '{parent_folder_id}' in parents and name = '{filename}' and trashed = false"
    results = service.files().list(q=query, fields="files(id, name)").execute()
    items = results.get('files', [])
    
    media = MediaFileUpload(local_filepath, mimetype=mimetype, resumable=True)
    
    try:
        if items:
            # 기존 파일의 ID(링크 주소)를 유지하며 파일 콘텐츠만 덮어쓰기
            file_id = items[0]['id']
            print(f"  [*] 파일 업데이트: {filename} (ID: {file_id})", flush=True)
            request = service.files().update(fileId=file_id, media_body=media)
        else:
            # 파일 신규 업로드
            print(f"  [+] 파일 신규 업로드: {filename}", flush=True)
            file_metadata = {
                'name': filename,
                'parents': [parent_folder_id]
            }
            request = service.files().create(body=file_metadata, media_body=media)
            
        response = None
        while response is None:
            status, response = request.next_chunk()
            if status:
                pass
        return True
    except Exception as e:
        print(f"  [-] 파일 업로드 실패 ({filename}): {e}", flush=True)
        return False

def find_remote_id(parent_map, local_path):
    # parent_map에서 local_path와 물리적으로 동일한 경로를 찾아 Value(remote_id) 리턴
    for k, v in parent_map.items():
        try:
            if os.path.samefile(k, local_path):
                return v
        except Exception:
            pass
    # 문자열 폴백 비교
    for k, v in parent_map.items():
        if k.replace('\\', '/').lower() == local_path.replace('\\', '/').lower():
            return v
    return None

def sync_local_to_remote(service, local_dir, remote_folder_id):
    # 폴더 구조 재귀 동기화
    parent_map = {local_dir: remote_folder_id}
    
    for root, dirs, files in os.walk(local_dir):
        current_remote_id = find_remote_id(parent_map, root)
        if not current_remote_id:
            parent_dir = os.path.dirname(root)
            parent_remote_id = find_remote_id(parent_map, parent_dir) or remote_folder_id
            dir_name = os.path.basename(root)
            current_remote_id = get_or_create_remote_folder(service, dir_name, parent_remote_id)
            parent_map[root] = current_remote_id
            
        # 1. 하위 디렉토리에 대해 미리 구글 드라이브 내에 폴더를 만들어 ID 매핑
        for d in dirs:
            local_subdir_path = os.path.join(root, d)
            subdir_remote_id = get_or_create_remote_folder(service, d, current_remote_id)
            parent_map[local_subdir_path] = subdir_remote_id
            
        # 2. 현재 폴더에 들어있는 파일들을 업로드
        for f in files:
            local_file_path = os.path.join(root, f)
            try:
                rel_path = os.path.relpath(local_file_path, local_dir)
            except ValueError:
                rel_path = os.path.basename(local_file_path)
            print(f"[*] 업로드 처리 중: {rel_path}", flush=True)
            upload_file_to_folder(service, local_file_path, current_remote_id)

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    target_dir = os.path.join(base_dir, "bin", "LabelMaster_Basic_v2")
    target_dir = os.path.normpath(os.path.abspath(target_dir))
    
    print(f"[*] ==================================================", flush=True)
    print(f"[*] 로컬 업로드 대상 폴더: {target_dir}", flush=True)
    print(f"[*] ==================================================", flush=True)
    
    if not os.path.exists(target_dir):
        print(f"[-] 대상 폴더가 존재하지 않습니다: {target_dir}", flush=True)
        return
        
    try:
        service = authenticate_gdrive(base_dir)
        print(f"[*] 구글 드라이브 배포 폴더 동기화 시작 (Folder ID: {TARGET_FOLDER_ID})", flush=True)
        sync_local_to_remote(service, target_dir, TARGET_FOLDER_ID)
        
        # 추가: 빌드된 단일 ZIP 파일도 배포 폴더 최상위 하위에 직접 업로드
        zip_file_path = os.path.join(base_dir, "bin", "LabelMaster_Basic_v2.zip")
        if os.path.exists(zip_file_path):
            print(f"[*] 최신 ZIP 배포 파일 업로드 시작: LabelMaster_Basic_v2.zip", flush=True)
            upload_file_to_folder(service, zip_file_path, TARGET_FOLDER_ID)
            
        print("[SUCCESS] 구글 드라이브 폴더 및 ZIP 동기화가 성공적으로 완료되었습니다!", flush=True)
    except Exception as e:
        print(f"[-] 구글 드라이브 처리 중 치명적 오류: {e}", flush=True)

if __name__ == '__main__':
    main()
