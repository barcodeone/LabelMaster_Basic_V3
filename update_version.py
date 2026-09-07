import os
import re

def update_version():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    assembly_info_path = os.path.join(base_dir, "Properties", "AssemblyInfo.cs")
    
    if not os.path.exists(assembly_info_path):
        print(f"[-] AssemblyInfo.cs 파일을 찾을 수 없습니다: {assembly_info_path}", flush=True)
        return False
        
    try:
        # AssemblyInfo.cs 읽기 (UTF-8)
        with open(assembly_info_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
        # AssemblyVersion("1.0.0.0") 매칭 검색
        version_match = re.search(r'\[assembly:\s*AssemblyVersion\(\s*"(\d+)\.(\d+)\.(\d+)\.(\d+)"\s*\)\]', content)
        if version_match:
            major, minor, build, revision = map(int, version_match.groups())
            new_build = build + 1
            new_version_4 = f"{major}.{minor}.{new_build}.{revision}"
            new_version_3 = f"{major}.{minor}.{new_build}"
            
            # AssemblyVersion 및 AssemblyFileVersion 업데이트
            content = re.sub(
                r'\[assembly:\s*AssemblyVersion\(\s*".*?"\s*\)\]', 
                f'[assembly: AssemblyVersion("{new_version_4}")]', 
                content
            )
            content = re.sub(
                r'\[assembly:\s*AssemblyFileVersion\(\s*".*?"\s*\)\]', 
                f'[assembly: AssemblyFileVersion("{new_version_4}")]', 
                content
            )
            
            # AssemblyInformationalVersion("1.0.0") 업데이트 (존재하는 경우)
            if re.search(r'\[assembly:\s*AssemblyInformationalVersion\(\s*".*?"\s*\)\]', content):
                content = re.sub(
                    r'\[assembly:\s*AssemblyInformationalVersion\(\s*".*?"\s*\)\]', 
                    f'[assembly: AssemblyInformationalVersion("{new_version_3}")]', 
                    content
                )
            
            with open(assembly_info_path, 'w', encoding='utf-8') as f:
                f.write(content)
                
            print(f"[+] AssemblyInfo.cs 버전 자동 업데이트 완료: {major}.{minor}.{build}.{revision} -> {new_version_4}", flush=True)
            return True
        else:
            print("[-] AssemblyInfo.cs에서 AssemblyVersion 태그를 찾을 수 없습니다.", flush=True)
    except Exception as e:
        print(f"[-] 버전 업데이트 중 에러 발생: {e}", flush=True)
        
    return False

if __name__ == '__main__':
    update_version()
