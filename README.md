# X-QuickLabel

<div align="center">

![X-QuickLabel Logo](XQuick.png)

**Excel 기반 바코드 라벨 자동 출력 솔루션**

[![.NET](https://img.shields.io/badge/.NET-4.8-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-green.svg)](https://github.com/yourusername/x-quicklabel)

[English](README_EN.md) | 한국어

</div>

---

## 📋 목차

- [프로젝트 소개](#-프로젝트-소개)
- [주요 기능](#-주요-기능)
- [시스템 요구사항](#-시스템-요구사항)
- [설치 방법](#-설치-방법)
- [사용 방법](#-사용-방법)
- [기술 스택](#-기술-스택)
- [프로젝트 구조](#-프로젝트-구조)
- [개발 가이드](#-개발-가이드)
- [문서](#-문서)
- [라이센스](#-라이센스)
- [기여](#-기여)
- [문의](#-문의)

---

## 🎯 프로젝트 소개

**X-QuickLabel**은 Excel 데이터를 기반으로 바코드 라벨을 자동 생성하고 Xprinter 열전사 프린터로 출력하는 Windows 데스크톱 애플리케이션입니다.

### 왜 X-QuickLabel인가?

- ✅ **Excel 연동**: 기존 Excel 데이터를 그대로 활용
- ✅ **다양한 바코드**: QR, CODE128, EAN13 등 7종 지원
- ✅ **실시간 미리보기**: 출력 전 실제 크기 미리보기
- ✅ **터치 최적화**: 터치스크린 환경에서도 편리한 사용
- ✅ **무료 라이브러리**: 상업적 사용 가능한 오픈소스만 사용

### 주요 사용처

- 📦 물류 창고 재고 관리
- 🏭 제조업 제품 라벨링
- 🏪 소매업 가격표 출력
- 📚 도서관 장서 관리

---

## 🚀 주요 기능

### 1. 바코드 생성 및 출력

<div align="center">

| QR Code | CODE 128 | EAN-13 |
|---------|----------|--------|
| ![QR](docs/images/qr.png) | ![CODE128](docs/images/code128.png) | ![EAN13](docs/images/ean13.png) |

</div>

**지원 바코드 타입**:
- `QR_CODE` (2D 바코드)
- `CODE_128` (1D 바코드)
- `EAN_13` (유통 표준)
- `CODE_39` (산업 표준)
- `ITF` (물류용)
- `DATA_MATRIX` (2D 바코드)
- `PDF_417` (2D 바코드)

**바코드 커스터마이징**:
- 크기 조절 (10 ~ 200)
- 위치 조절 (X, Y 오프셋)
- 텍스트 표시 여부
- 회전 (0°, 90°, 180°, 270°)

### 2. Excel 데이터 연동

```
Excel 파일 (.xlsx, .xls)
    ↓
컬럼 선택 (바코드 / 텍스트)
    ↓
자동 배치 출력
    ↓
완료! 🎉
```

**특징**:
- 대용량 파일 지원 (최대 10,000행)
- 자동 컬럼 감지
- 실시간 진행 상태 표시
- 오류 발생 시 자동 재시도

### 3. 실시간 미리보기

- 실제 라벨 크기로 미리보기
- 바코드 위치 실시간 반영
- 출력 전 오류 확인
- 용지 낭비 방지

### 4. 프린터 관리

- USB 프린터 자동 감지
- TSPL 프린터 전용 최적화
- 프린터 상태 모니터링
- TSPL 명령어 지원

### 5. 설정 관리

- 바코드 설정 저장/로드
- 여러 프로필 관리
- 자동 백업 기능
- Excel 연동 설정 영속화

---

## 💻 시스템 요구사항

### 최소 요구사항

| 항목 | 사양 |
|-----|------|
| **운영체제** | Windows 10 (64-bit) |
| **.NET Framework** | 4.8 이상 |
| **CPU** | Intel Core i3 이상 |
| **메모리** | 4GB RAM |
| **저장공간** | 500MB |
| **디스플레이** | 1280x720 이상 |

### 권장 요구사항

| 항목 | 사양 |
|-----|------|
| **운영체제** | Windows 11 (64-bit) |
| **CPU** | Intel Core i5 이상 |
| **메모리** | 8GB RAM |
| **저장공간** | 1GB SSD |
| **디스플레이** | 1920x1080 터치스크린 |

### 지원 프린터

- **XP-T265B**
- **XP-DT326B**
- **XP-D4601B**
- **XP-D4602B**
- **XP-D4604B**
- **XP-D461B**
- **XP-423B**
- **XP-P423B**
- **XP-T4501B**
- **XP-T428B**
- 기타 Xprinter TSPL 호환 감열/열전사 라벨 프린터 전 기종

---

## 📥 설치 방법

### 방법 1: 설치 프로그램 (권장)

1. [최신 릴리스](https://github.com/yourusername/x-quicklabel/releases) 페이지 방문
2. `X-QuickLabel-Setup-v1.0.0.exe` 다운로드
3. 다운로드한 파일 실행
4. 설치 마법사 지침 따라하기
5. 완료!

### 방법 2: 포터블 버전

1. [최신 릴리스](https://github.com/yourusername/x-quicklabel/releases) 페이지 방문
2. `X-QuickLabel-Portable-v1.0.0.zip` 다운로드
3. 압축 해제
4. `BarcodePrint.exe` 실행

### 방법 3: 소스 코드 빌드

```powershell
# 저장소 클론
git clone https://github.com/yourusername/x-quicklabel.git
cd x-quicklabel

# Visual Studio에서 열기
start BarcodePrint.sln

# 또는 명령줄에서 빌드
msbuild BarcodePrint.sln /p:Configuration=Release
```

---

## 📖 사용 방법

### 빠른 시작

#### 1. 단일 바코드 출력

```
1. X-QuickLabel 실행
2. 바코드 값 입력 (예: "ABC123")
3. [미리보기] 확인
4. [출력] 버튼 클릭
```

#### 2. Excel 배치 출력

```
1. [Excel 모드] 체크박스 활성화
2. [파일 찾기] 버튼 클릭
3. Excel 파일 선택
4. 바코드 컬럼 및 텍스트 컬럼 선택
5. [출력] 버튼 클릭
```

### 설정 방법

#### 프린터 설정

```
1. [설정] 버튼 클릭
2. "프린터 선택" 드롭다운에서 프린터 선택
3. 라벨 크기 입력 (가로 x 세로, mm 단위)
4. 라벨 간격 타입 선택 (간격지 / 흑선 / 연속지)
5. [저장] 버튼 클릭
```

#### 바코드 설정

```
1. [설정] 버튼 클릭
2. "바코드 타입" 드롭다운에서 타입 선택
3. 슬라이더로 크기 조절 (10 ~ 200)
4. 슬라이더로 위치 조절 (X, Y)
5. "텍스트 표시" 체크박스 설정
6. [저장] 버튼 클릭
```

### 단축키

| 단축키 | 기능 |
|-------|------|
| `Enter` | 바코드 입력 완료 |
| `Ctrl + P` | 출력 |
| `Ctrl + O` | Excel 파일 열기 |
| `Ctrl + S` | 설정 저장 |
| `F5` | 미리보기 새로고침 |
| `ESC` | 설정 창 닫기 |

---

## 🛠 기술 스택

### 프레임워크 및 언어

- **C# 7.3** - 프로그래밍 언어
- **.NET Framework 4.8** - 애플리케이션 프레임워크
- **Windows Forms** - UI 프레임워크

### 주요 라이브러리

| 라이브러리 | 버전 | 용도 | 라이센스 |
|-----------|------|------|---------|
| **ZXing.Net** | 0.16.9 | 바코드 생성 | Apache 2.0 |
| **ClosedXML** | 0.104.2 | Excel 파일 처리 | MIT |
| **PdfPig** | 0.1.9 | PDF 바코드 추출 | Apache 2.0 |
| **MaterialSkin.2** | 2.3.1 | UI 테마 | MIT |
| **MetroModernUI** | 1.4.0.0 | UI 프레임워크 | MIT |
| **DocumentFormat.OpenXml** | 3.1.1 | Office 문서 처리 | MIT |
| **Costura.Fody** | 6.0.0 | DLL 임베딩 | MIT |

### 개발 도구

- **Visual Studio 2022** - IDE
- **Git** - 버전 관리
- **NuGet** - 패키지 관리

---

## 📁 프로젝트 구조

```
X-QuickLabel/
├── View/                   # UI 계층 (Forms)
│   ├── MainForm.cs         # 메인 화면
│   ├── AdjustForm.cs       # 설정 화면
│   └── PrintStatusForm.cs  # 인쇄 상태
├── Model/                  # 데이터 모델
│   ├── BarcodeSettings.cs  # 바코드 설정
│   ├── ExcelSettings.cs    # Excel 설정
│   └── TSPLPrint/          # TSPL 프린터
├── Controll/              # 컨트롤러
│   ├── BarcodePrintManager.cs
│   └── MachineList.cs
├── UserControls/          # 커스텀 UI 컨트롤
│   ├── TouchCheckBox.cs
│   └── TouchSlider.cs
├── docs/                  # 문서
│   ├── 프로그램_설계_문서.md
│   └── 개발_로그.md
├── backup/                # 소스 백업
├── resource/              # 이미지 리소스
└── README.md             # 본 문서
```

### MVC 패턴

```
┌─────────────┐
│    View     │ ← 사용자 인터랙션
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Controller  │ ← 비즈니스 로직
└──────┬──────┘
       │
       ▼
┌─────────────┐
│    Model    │ ← 데이터 처리
└─────────────┘
```

---

## 👨‍💻 개발 가이드

### 개발 환경 설정

1. **Visual Studio 2022 설치**
   - 워크로드: `.NET 데스크톱 개발`
   - 개별 구성 요소: `.NET Framework 4.8 SDK`

2. **저장소 클론**
   ```powershell
   git clone https://github.com/yourusername/x-quicklabel.git
   cd x-quicklabel
   ```

3. **NuGet 패키지 복원**
   ```powershell
   nuget restore BarcodePrint.sln
   ```

4. **빌드**
   ```powershell
   msbuild BarcodePrint.sln /p:Configuration=Debug
   ```

5. **실행**
   ```powershell
   .\bin\Debug\BarcodePrint.exe
   ```

### 코딩 규칙

#### 네이밍 규칙

- **클래스**: PascalCase (예: `BarcodePrintManager`)
- **메서드**: PascalCase (예: `GenerateBarcode()`)
- **변수**: camelCase (예: `barcodeSize`)
- **상수**: UPPER_CASE (예: `INCH`)
- **private 필드**: camelCase (예: `previewManager`)

#### 파일 구조

```csharp
using System;
// ... 기타 using

namespace BarcodePrint.View
{
    /// <summary>
    /// 클래스 설명
    /// </summary>
    public class ClassName
    {
        // 1. 상수
        private const int CONSTANT_VALUE = 100;
        
        // 2. 필드
        private int fieldValue;
        
        // 3. 프로퍼티
        public int PropertyValue { get; set; }
        
        // 4. 생성자
        public ClassName()
        {
            // 초기화
        }
        
        // 5. public 메서드
        public void PublicMethod()
        {
            // 구현
        }
        
        // 6. private 메서드
        private void PrivateMethod()
        {
            // 구현
        }
    }
}
```

### 브랜치 전략

```
main
  ├── develop
  │   ├── feature/barcode-enhancement
  │   ├── feature/excel-integration
  │   └── bugfix/printer-connection
  └── hotfix/critical-bug
```

### 커밋 메시지 규칙

```
<type>: <subject>

<body>

<footer>
```

**타입**:
- `feat`: 새로운 기능
- `fix`: 버그 수정
- `docs`: 문서 수정
- `style`: 코드 포맷팅
- `refactor`: 코드 리팩토링
- `test`: 테스트 추가
- `chore`: 빌드 설정 등

**예시**:
```
feat: Add barcode text size control

- Add text size slider in AdjustForm
- Update barcode rendering logic
- Sync preview and print output

Closes #123
```

---

## 📚 문서

### 프로젝트 문서

- [프로그램 설계 문서](docs/프로그램_설계_문서.md) - 상세한 시스템 설계
- [개발 로그](docs/개발_로그.md) - 개발 히스토리 및 변경 사항
- [API 문서](docs/API_문서.md) (작성 예정)

### 외부 문서

- [ZXing.Net 문서](https://github.com/micjahn/ZXing.Net)
- [ClosedXML 문서](https://github.com/ClosedXML/ClosedXML/wiki)
- [TSPL 프로그래밍 가이드](https://www.tscprinters.com/EN/Download/Programming_manual/66)

---

## 📜 라이센스

이 프로젝트는 **BarcodeOne** 소유의 독점 소프트웨어입니다.

### 사용 제한

- ❌ 상업적 재배포 금지
- ❌ 소스 코드 무단 복제 금지
- ✅ 개인적 사용 허용
- ✅ 교육 목적 사용 허용 (출처 명시)

### 라이센스 문의

상업적 사용을 원하시면 [license@barcode.one](mailto:license@barcode.one)로 문의해주세요.

---

## 🤝 기여

### 기여 방법

1. 이 저장소를 포크합니다.
2. 새로운 브랜치를 생성합니다. (`git checkout -b feature/AmazingFeature`)
3. 변경사항을 커밋합니다. (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치에 푸시합니다. (`git push origin feature/AmazingFeature`)
5. Pull Request를 생성합니다.

### 기여 가이드라인

- 코드 스타일 준수
- 테스트 작성
- 문서 업데이트
- 커밋 메시지 규칙 준수

### 기여자

<a href="https://github.com/yourusername/x-quicklabel/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=yourusername/x-quicklabel" />
</a>

---

## 📞 문의

### 지원

- **이메일**: support@barcode.one
- **전화**: 02-1234-5678
- **웹사이트**: [https://printerlabel.co.kr](https://printerlabel.co.kr)

### 버그 리포트

버그를 발견하셨나요? [이슈 트래커](https://github.com/yourusername/x-quicklabel/issues)에 리포트해주세요.

### 기능 요청

새로운 기능을 제안하고 싶으신가요? [토론 게시판](https://github.com/yourusername/x-quicklabel/discussions)에 남겨주세요.

---

## 🌟 후원

이 프로젝트가 도움이 되셨나요? ⭐️을 눌러주세요!

---

<div align="center">

**Made with ❤️ by BarcodeOne**

© 2025 BarcodeOne. All rights reserved.

</div>


