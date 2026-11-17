import os
import re

def update_version():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    version_file = os.path.join(base_dir, "version.txt")
    assembly_info_path = os.path.join(base_dir, "Properties", "AssemblyInfo.cs")
    
    # 1. version.txt에서 현재 버전 읽기
    old_ver = "1.0.1"
    if os.path.exists(version_file):
        try:
            with open(version_file, "r", encoding="utf-8") as f:
                content = f.read().strip()
                if content:
                    old_ver = content
        except Exception as ex:
            print(f"[!] version.txt 읽기 실패: {ex}", flush=True)

    # 2. 버전 번호 자동 증가 (예: 1.0.1 -> 1.0.2)
    parts = old_ver.split('.')
    if len(parts) >= 3 and parts[-1].isdigit():
        parts[-1] = str(int(parts[-1]) + 1)
        new_ver = '.'.join(parts[:3])
    elif len(parts) == 2 and parts[-1].isdigit():
        new_ver = f"{old_ver}.1"
    else:
        new_ver = f"{old_ver}.1"

    # version.txt에 증가된 최신 버전 저장
    try:
        with open(version_file, "w", encoding="utf-8") as f:
            f.write(new_ver + "\n")
        print(f"[VERSION INCREMENT] version.txt: {old_ver} -> {new_ver}", flush=True)
    except Exception as ex:
        print(f"[ERROR] version.txt 쓰기 실패: {ex}", flush=True)
        return False

    # 3. 어셈블리 버전 생성: 네 자리 (예: 1.0.2.0), 짧은 버전: 세 자리 (예: 1.0.2)
    sub_parts = new_ver.split('.')
    while len(sub_parts) < 4:
        sub_parts.append('0')
    four_part_ver = '.'.join(sub_parts[:4])
    short_ver = new_ver

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
            
        print(f"[+] AssemblyInfo.cs 동기화 완료: v{short_ver} (어셈블리: {four_part_ver})", flush=True)

        # 4. XAML 및 UI 타이틀 정적 문자열 동기화
        try:
            start_window_path = os.path.join(base_dir, "WpfView", "StartWindow.xaml")
            if os.path.exists(start_window_path):
                with open(start_window_path, 'r', encoding='utf-8') as f:
                    sw_code = f.read()
                sw_code = re.sub(r'Title="라벨마스터 - LabelMaster(\s*\([^)]*\))?"', f'Title="라벨마스터 - LabelMaster (v{short_ver})"', sw_code)
                sw_code = re.sub(r'Text="라벨마스터\s+v[^"]+"', f'Text="라벨마스터 v{short_ver}"', sw_code)
                with open(start_window_path, 'w', encoding='utf-8') as f:
                    f.write(sw_code)
                print(f"[+] StartWindow.xaml 타이틀 동기화 완료: (v{short_ver})", flush=True)
        except Exception as e:
            print(f"[-] StartWindow.xaml 갱신 중 에러: {e}", flush=True)

        try:
            splash_window_path = os.path.join(base_dir, "WpfView", "SplashScreenWindow.xaml")
            if os.path.exists(splash_window_path):
                with open(splash_window_path, 'r', encoding='utf-8') as f:
                    sp_code = f.read()
                sp_code = re.sub(r'Title="라벨마스터\s+v[^"]*\s+시작 중\.\.\."', f'Title="라벨마스터 v{short_ver} 시작 중..."', sp_code)
                sp_code = re.sub(r'(x:Name="TxtSplashVersion"\s+Text=")v[^"]+(")', rf'\g<1>v{short_ver}\2', sp_code)
                with open(splash_window_path, 'w', encoding='utf-8') as f:
                    f.write(sp_code)
                print(f"[+] SplashScreenWindow.xaml 타이틀 동기화 완료: v{short_ver}", flush=True)
        except Exception as e:
            print(f"[-] SplashScreenWindow.xaml 갱신 중 에러: {e}", flush=True)

        try:
            about_window_path = os.path.join(base_dir, "WpfView", "AboutWindow.xaml")
            if os.path.exists(about_window_path):
                with open(about_window_path, 'r', encoding='utf-8') as f:
                    ab_code = f.read()
                ab_code = re.sub(r'(x:Name="TxtVersionBadge"\s+Text=")v[^"]+(")', rf'\g<1>v{short_ver}\2', ab_code)
                with open(about_window_path, 'w', encoding='utf-8') as f:
                    f.write(ab_code)
        except Exception as e:
            pass

        return True
    except Exception as e:
        print(f"[-] AssemblyInfo.cs 갱신 중 에러: {e}", flush=True)
        return False

if __name__ == '__main__':
    update_version()
