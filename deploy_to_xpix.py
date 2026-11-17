# -*- coding: utf-8 -*-
"""
LabelMaster Windows (v3) xpix.co.kr 전용 배포 및 Nginx 링크 등록기
- UTF-8 및 한글 경로 인코딩 문제를 완벽히 방지하는 Python 전용 배포 스크립트
"""

import os
import sys
import tarfile
import subprocess

# Windows 콘솔 UTF-8 설정
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except Exception:
        pass

def main():
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    SERVER_USER = "root"
    SERVER_IP = "xpix.co.kr"

    print("========================================================")
    print("  LabelMaster Windows (v3) xpix.co.kr 서버 배포기")
    print("  - 기존 basic과 완전 분리된 Windows 전용 독립 경로 배포")
    print("  - 공식 링크: https://xpix.co.kr/labelmaster/windows/download")
    print("========================================================\n")

    # 1. 버전 확인
    version_file = os.path.join(BASE_DIR, "version.txt")
    app_version = "1.0.1"
    if os.path.exists(version_file):
        with open(version_file, "r", encoding="utf-8") as f:
            app_version = f.read().strip() or "1.0.1"

    # 2. 빌드 결과물 확인
    bin_dir = os.path.join(BASE_DIR, "bin")
    pkg_dir = os.path.join(bin_dir, "LabelMaster")
    if not os.path.exists(pkg_dir):
        print(f"[ERROR] 배포 폴더가 없습니다: {pkg_dir}")
        input("\n계속하려면 엔터 키를 누르십시오...")
        sys.exit(1)

    # 3. 배포 아카이브 (release_windows_v3.tar) 생성
    tar_path = os.path.join(BASE_DIR, "release_windows_v3.tar")
    print(f"[1/3] 배포 패키지 아카이브 생성 중... -> {os.path.basename(tar_path)}")
    try:
        with tarfile.open(tar_path, "w") as tar:
            tar.add(pkg_dir, arcname="LabelMaster")
        print(f"  -> 아카이브 생성 완료 ({os.path.getsize(tar_path):,} bytes)")
    except Exception as e:
        print(f"[ERROR] tar 아카이브 생성 실패: {e}")
        input("\n계속하려면 엔터 키를 누르십시오...")
        sys.exit(1)

    # 전송할 파일 목록
    files_to_send = [
        tar_path,
        os.path.join(bin_dir, "LabelMaster.zip"),
        os.path.join(bin_dir, f"LabelMaster_v{app_version}.zip"),
        os.path.join(BASE_DIR, "setup_windows_link.sh")
    ]

    for f in files_to_send:
        if not os.path.exists(f):
            print(f"[ERROR] 전송 대상 파일이 없습니다: {f}")
            input("\n계속하려면 엔터 키를 누르십시오...")
            sys.exit(1)

    # 4. scp 파일 전송
    print("\n========================================================")
    print(f"[2/3] xpix.co.kr 서버로 파일 전송 중...")
    print(f"※ 서버 접속을 위해 {SERVER_USER}@{SERVER_IP} 비밀번호를 입력해주세요.")
    print("========================================================")

    scp_cmd = ["scp", "-O"] + files_to_send + [f"{SERVER_USER}@{SERVER_IP}:/root/"]
    res = subprocess.run(scp_cmd)

    # 로컬 임시 tar 정리
    if os.path.exists(tar_path):
        try:
            os.remove(tar_path)
        except Exception:
            pass

    if res.returncode != 0:
        print("\n[ERROR] SCP 파일 전송 실패! (비밀번호 확인 필요)")
        input("\n계속하려면 엔터 키를 누르십시오...")
        sys.exit(1)

    # 5. 원격 서버 압축 해제 및 Nginx 설정 적용
    print("\n========================================================")
    print("[3/3] 서버 원격 배포 및 Nginx v3 전용 다운로드 링크 설정...")
    print(f"※ 비밀번호를 한 번 더 입력해주세요.")
    print("========================================================")

    remote_sh = (
        "mkdir -p /root/labelmaster/windows && "
        "rm -rf /root/labelmaster/windows/* && "
        "tar -xf /root/release_windows_v3.tar -C /root/labelmaster/windows --strip-components=1 && "
        "cp -f /root/LabelMaster.zip /root/labelmaster/windows/ && "
        f"cp -f /root/LabelMaster_v{app_version}.zip /root/labelmaster/windows/ && "
        "chmod -R 755 /root/labelmaster/windows && "
        f"rm -f /root/release_windows_v3.tar /root/LabelMaster.zip /root/LabelMaster_v{app_version}.zip && "
        "bash /root/setup_windows_link.sh && "
        "rm -f /root/setup_windows_link.sh"
    )

    ssh_cmd = ["ssh", f"{SERVER_USER}@{SERVER_IP}", remote_sh]
    res2 = subprocess.run(ssh_cmd)

    if res2.returncode == 0:
        print("\n========================================================")
        print(" [SUCCESS] xpix 서버에 v3 전용 배포 및 전용 링크 설정 완료!")
        print("\n [v3 전용 다운로드 링크 주소]")
        print(" 1. https://xpix.co.kr/labelmaster/windows/download")
        print(" 2. https://xpix.co.kr/labelmaster/windows")
        print("\n (기존 basic 프로젝트: https://xpix.co.kr/labelmaster/basic/download 와 완전 분리 유지됨)")
        print("========================================================")
    else:
        print("\n[ERROR] 원격 배포 명령 실행 실패!")

    input("\n완료되었습니다. 창을 닫으려면 엔터 키를 누르십시오...")

if __name__ == "__main__":
    main()
