import os
import re

def update_version():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    version_file = os.path.join(base_dir, "version.txt")
    assembly_info_path = os.path.join(base_dir, "Properties", "AssemblyInfo.cs")
    
    # 1. version.txt에서 버전 읽기
    ver_str = "1.0.1"
    if os.path.exists(version_file):
        try:
            with open(version_file, "r", encoding="utf-8") as f:
                content = f.read().strip()
                if content:
                    ver_str = content
        except Exception as ex:
            print(f"[!] version.txt 읽기 실패: {ex}", flush=True)
            
    # 세 자리 (예: 1.0.1) -> 네 자리 (예: 1.0.1.0)
    parts = ver_str.split('.')
    while len(parts) < 4:
        parts.append('0')
    four_part_ver = '.'.join(parts[:4])
    short_ver = '.'.join(parts[:3])

    if not os.path.exists(assembly_info_path):
        print(f"[-] AssemblyInfo.cs 파일을 찾을 수 없습니다: {assembly_info_path}", flush=True)
        return False
        
    try:
        with open(assembly_info_path, 'r', encoding='utf-8') as f:
            code = f.read()
            
        code = re.sub(r'\[assembly:\s*AssemblyVersion\("[^"]+"\)', f'[assembly: AssemblyVersion("{four_part_ver}")', code)
        code = re.sub(r'\[assembly:\s*AssemblyFileVersion\("[^"]+"\)', f'[assembly: AssemblyFileVersion("{four_part_ver}")', code)
        code = re.sub(r'\[assembly:\s*AssemblyInformationalVersion\("[^"]+"\)', f'[assembly: AssemblyInformationalVersion("{short_ver}")', code)
        
        with open(assembly_info_path, 'w', encoding='utf-8') as f:
            f.write(code)
            
        print(f"[+] 버전 동기화 완료: v{short_ver} (어셈블리: {four_part_ver})", flush=True)
        return True
    except Exception as e:
        print(f"[-] 버전 검사 중 에러 발생: {e}", flush=True)
        return False

if __name__ == '__main__':
    update_version()
