# -*- coding: utf-8 -*-
import os
import shutil
import zipfile

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    deploy_dir = os.path.join(base_dir, "bin", "LabelMaster")
    
    # 1. 기존 배포 폴더 정리 및 신규 생성
    if os.path.exists(deploy_dir):
        shutil.rmtree(deploy_dir)
    os.makedirs(deploy_dir, exist_ok=True)
    os.makedirs(os.path.join(deploy_dir, "Info"), exist_ok=True)

    # 2. 실행 파일 복사 (난독화된 파일 우선)
    obf_exe = os.path.join(base_dir, "bin", "Release", "Obfuscated", "LabelMaster.exe")
    rel_exe = os.path.join(base_dir, "bin", "Release", "LabelMaster.exe")
    target_exe = obf_exe if os.path.exists(obf_exe) else rel_exe
    if os.path.exists(target_exe):
        shutil.copy2(target_exe, os.path.join(deploy_dir, "LabelMaster.exe"))
        print(f"[OK] Copied executable: {target_exe} -> LabelMaster.exe")
    else:
        print("[ERROR] LabelMaster.exe not found in Release or Obfuscated!")

    # 3. 필수 문서 및 샘플 엑셀 파일 복사
    files_to_copy = [
        "샘플_상품라벨_출력목록.xlsx",
        "사용자_매뉴얼.html",
        "README_사용자안내.txt",
        "엑셀파일_사용_가이드.txt",
        "LICENSES.txt"
    ]
    for filename in files_to_copy:
        src = os.path.join(base_dir, filename)
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(deploy_dir, filename))
            print(f"[OK] Copied file: {filename}")
        else:
            print(f"[WARN] File not found: {filename}")

    # 4. Info/app_settings.json 및 Info/quick_tools.html 복사
    settings_src = os.path.join(base_dir, "Info", "app_settings.json")
    if os.path.exists(settings_src):
        shutil.copy2(settings_src, os.path.join(deploy_dir, "Info", "app_settings.json"))
        print("[OK] Copied Info/app_settings.json")

    quick_tools_src = os.path.join(base_dir, "Info", "quick_tools.html")
    if os.path.exists(quick_tools_src):
        shutil.copy2(quick_tools_src, os.path.join(deploy_dir, "Info", "quick_tools.html"))
        print("[OK] Copied Info/quick_tools.html")

    banner_url_src = os.path.join(base_dir, "Info", "banner_url.txt")
    if os.path.exists(banner_url_src):
        shutil.copy2(banner_url_src, os.path.join(deploy_dir, "Info", "banner_url.txt"))
        print("[OK] Copied Info/banner_url.txt")

    # 5. 버전 읽기
    version_file = os.path.join(base_dir, "version.txt")
    version = "1.0.1"
    if os.path.exists(version_file):
        with open(version_file, "r", encoding="utf-8") as f:
            version = f.read().strip()
    print(f"[INFO] Deploy Version: v{version}")

    # 6. ZIP 압축 생성 (LabelMaster_v{version}.zip & LabelMaster.zip)
    bin_dir = os.path.join(base_dir, "bin")
    zip_ver_path = os.path.join(bin_dir, f"LabelMaster_v{version}.zip")
    zip_latest_path = os.path.join(bin_dir, "LabelMaster.zip")

    for zip_path in [zip_ver_path, zip_latest_path]:
        if os.path.exists(zip_path):
            os.remove(zip_path)
        with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
            for root, dirs, files in os.walk(deploy_dir):
                for file in files:
                    file_path = os.path.join(root, file)
                    rel_path = os.path.relpath(file_path, deploy_dir)
                    zipf.write(file_path, arcname=os.path.join("LabelMaster", rel_path))
        print(f"[OK] Created zip archive: {os.path.basename(zip_path)} ({os.path.getsize(zip_path):,} bytes)")

    print("\n[SUCCESS] 배포 패키지 구성 완료!")
    print(f"배포 폴더: {deploy_dir}")
    print(f"파일 목록:")
    for root, dirs, files in os.walk(deploy_dir):
        for f in files:
            p = os.path.join(root, f)
            print(f" - {os.path.relpath(p, deploy_dir)} ({os.path.getsize(p):,} bytes)")

if __name__ == "__main__":
    main()
