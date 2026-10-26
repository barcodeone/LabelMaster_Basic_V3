import os

def update_docs():
    base_dir = r"e:\4.sourcecode\C#\2.X-quick\0.엑셀인쇄\LabelMaster_Basic_v3"
    
    # 0. version.txt에서 최신 버전 읽기
    version = "1.0.1"
    ver_path = os.path.join(base_dir, "version.txt")
    if os.path.exists(ver_path):
        with open(ver_path, "r", encoding="utf-8") as f:
            version = f.read().strip()

    # 1. 사용자_매뉴얼.html
    html_path = os.path.join(base_dir, "사용자_매뉴얼.html")
    if os.path.exists(html_path):
        with open(html_path, "r", encoding="utf-8") as f:
            c = f.read()
        c = c.replace("LabelMaster (라벨마스터).0", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster Basic v2.0", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster Basic v2", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster_Basic_v2.exe", "LabelMaster.exe")
        c = c.replace("LabelMaster_Basic_v2", "LabelMaster")
        c = c.replace("v2.0.0 (WPF Modern Edition)", f"v{version} (WPF Modern Edition)")
        c = c.replace("Xprinter 열전사 바코드 라벨 프린터 전용", "TSPL 프린터 전용")
        c = c.replace("Xprinter 열전사/감열 바코드 라벨 프린터를 위한", "TSPL 열전사/감열 바코드 라벨 프린터를 위한")
        with open(html_path, "w", encoding="utf-8") as f:
            f.write(c)
        print("[+] 사용자_매뉴얼.html 갱신 완료")

    # 2. docs/user_manual.html
    docs_html_path = os.path.join(base_dir, "docs", "user_manual.html")
    if os.path.exists(docs_html_path):
        with open(docs_html_path, "r", encoding="utf-8") as f:
            c = f.read()
        c = c.replace("LabelMaster (라벨마스터).0", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster Basic v2.0", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster Basic v2", "LabelMaster (라벨마스터)")
        c = c.replace("v2.0.0 (WPF Modern Edition)", f"v{version} (WPF Modern Edition)")
        c = c.replace("Xprinter 열전사 바코드 라벨 프린터 전용", "TSPL 프린터 전용")
        c = c.replace("Xprinter 열전사/감열 바코드 라벨 프린터를 위한", "TSPL 열전사/감열 바코드 라벨 프린터를 위한")
        with open(docs_html_path, "w", encoding="utf-8") as f:
            f.write(c)
        print("[+] docs/user_manual.html 갱신 완료")

    # 3. README_사용자안내.txt
    readme_path = os.path.join(base_dir, "README_사용자안내.txt")
    if os.path.exists(readme_path):
        with open(readme_path, "r", encoding="utf-8") as f:
            c = f.read()
        c = c.replace("LabelMaster Basic v2", "LabelMaster (라벨마스터)")
        c = c.replace("LabelMaster_Basic_v2.exe", "LabelMaster.exe")
        c = c.replace("LabelMaster_Basic_v2", "LabelMaster")
        c = c.replace("버전: v1.0", f"버전: v{version}")
        c = c.replace("버전: v2.0", f"버전: v{version}")
        with open(readme_path, "w", encoding="utf-8") as f:
            f.write(c)
        print("[+] README_사용자안내.txt 갱신 완료")

    # 4. 엑셀파일_사용_가이드.txt
    excel_guide_path = os.path.join(base_dir, "엑셀파일_사용_가이드.txt")
    if os.path.exists(excel_guide_path):
        with open(excel_guide_path, "r", encoding="utf-8") as f:
            c = f.read()
        c = c.replace("LabelMaster_Basic_v2", "LabelMaster")
        c = c.replace("라벨마스터 for 가격표", "라벨마스터 (LabelMaster) - 엑셀 대량 인쇄")
        with open(excel_guide_path, "w", encoding="utf-8") as f:
            f.write(c)
        print("[+] 엑셀파일_사용_가이드.txt 갱신 완료")

if __name__ == "__main__":
    update_docs()
