# 📋 LabelMaster_Basic_v3 변경 및 업데이트 로그 (Change Log)

본 문서는 `LabelMaster_Basic_v3` 프로젝트의 기능 추가, UI 개선, 버그 수정 및 리팩토링 내역을 상세히 기록합니다. 배포 및 업데이트 릴리스 노트 작성 시 참고 자료로 활용됩니다.

---

## 📌 [2026-10-30] 상단 공식 홈페이지 분리 및 왼쪽 사이드바 HTML 배너 슬롯 구현

### 1. 주요 기능 추가 및 변경 사항
- **상단 공식 홈페이지 연결 배너 분리**:
  - 프로그램 상단 배너를 한국 Xprinter 공식 홈페이지(`https://xprinter.co.kr`) 연결로 명확히 분리 및 툴팁/이벤트(`OnOfficialSiteClick`) 적용.
  - `AppSettings.OfficialWebsiteUrl` 프로퍼티 연동으로 유지보수성 확보.
- **왼쪽 사이드바 기존 '정품 소모품 몰' 버튼 제거 및 230px 규격 맞춤 정렬**:
  - 왼쪽 사이드바 메뉴 7번 텍스트 버튼(`정품 소모품 몰`)을 제거.
  - 해당 위치(프로그램 정보 아래)에 HTML 웹뷰를 통해 자유롭게 배너를 띄울 수 있는 `SidebarWebBrowser` 배너 컨테이너(`SidebarBannerBorder`) 배치.
  - 가로 폭과 세로 높이를 **`230px × 230px`**로 중앙 정렬 고정.
  - **빠른 메뉴 영역 사이즈를 230px 배너 폭에 정확히 일치**: 사이드바 컬럼 너비를 `254px`, 내부 여백을 `12px`로 조정하여 빠른 메뉴 버튼들과 배너의 가로 폭이 정확히 `230px`로 끝선이 100% 일치하도록 정돈. 버튼 높이(40px) 및 마진을 슬림화하여 스크롤바 없는 깔끔한 뷰 구현.
- **광고/소모품몰 링크 완전 제거 및 빈 HTML 파일 연결**:
  - 사용자 요청에 따라 모든 광고 및 구매 링크를 제거하고 깨끗한 빈 HTML(`sidebar_banner.html`, `banner.html`)을 기본 연결.
  - Cloudflare R2 원격 스토리지에도 빈 HTML 파일 동기화 완료.
- **메인 워크스페이스 하단 웹 배너 4분할 광고 레이아웃 (WPF WebBrowser 호환성 적용)**:
  - WPF `WebBrowser`(IE Trident 렌더링 엔진)에서 CSS Grid 미지원으로 세로로 쌓이던 문제를 해결하기 위해, 100% 호환되는 테이블 레이아웃(`table-layout: fixed; width: 25%`)으로 전면 개편.
  - 가로 한 줄에 25%씩 정확히 4개 슬롯이 나란히 배치되도록 수정.
  - 첫 번째 칸(슬롯 1)에 테리스(`https://terrys.kr/`) 배너 이미지 및 링크를 세로 130px 높이에 맞춰 적용.
  - 2, 3, 4번 슬롯은 신규 업체 광고를 손쉽게 추가할 수 있는 대기 슬롯으로 세팅.
  - Cloudflare R2 원격 스토리지(`banner.html`)에 실시간 업로드 배포 완료.
- **메인 워크스페이스 하단 웹 배너 세로 130px 고정**:
  - 메인 대시보드 하단 `QuickToolsHtmlBorder` 및 `QuickToolsWebBrowser`의 높이를 **`130px`** 고정으로 최적화.
- **메인 워크스페이스 하단 '공식 인증 스토어' 네이티브 배너 제거**:
  - 메인 페이지 최근 작업 내역 하단은 기존 레이아웃으로 복원.

### 2. 수정된 파일 목록
| 파일 경로 | 수정 내용 |
|---|---|
| `Model/AppSettings.cs` | `OfficialWebsiteUrl`, `SuppliesStoreUrl`, `SidebarBannerUrl` 전역 설정 속성 추가 |
| `WpfView/StartWindow.xaml` | 상단 공식 홈페이지 분리, 메인 하단 스토어 배너 제거, 왼쪽 사이드바에 `SidebarWebBrowser` 배너 공간 신설 |
| `WpfView/StartWindow.xaml.cs` | `LoadSidebarHtml`, `OnSidebarWebBrowserNavigating`, `OnSidebarWebBrowserLoadCompleted`, `CreateDefaultSidebarBannerHtml` 구현 |
| `Info/sidebar_banner.html` | 사이드바 규격 맞춤 반응형 정품 소모품 몰 HTML 배너 템플릿 신설 |
| `WpfView/StartWindow.xaml.cs` | `OnOfficialSiteClick`, `OnSuppliesStoreClick`, `OpenOfficialSiteUrl`, `OpenSuppliesStoreUrl` 구현 |
| `Info/banner.html` | 요청 디자인 시안 100% 일치 반응형 정품 소모품 몰 웹 배너 템플릿 신설 |

---

## 📌 [2026-10-30] 최대 인쇄 길이 1778.0 mm (70인치 TSPL 표준 규격) 지원 확장

### 1. 주요 기능 추가 및 변경 사항
- **최대 라벨 높이 한도 1778.0mm로 확장**:
  - TSPL 펌웨어 표준 최대 지원 길이(70인치 = 1778.0mm)에 맞춰 시스템 최대 용지 높이 상수를 `1778mm`로 설정.
  - `AppSettings.MAX_PAPER_HEIGHT = 1778` 상수 정의.
  - 상단 메인 툴바의 용지 높이 입력창(`PaperHeightStr`) 및 증감 버튼(`IncreasePaperHeightCommand`) 한도를 기존 300mm에서 1778mm로 확장.
  - 신규 라벨 마법사(`LabelSetupWizardDialog`) 및 고급 용지 설정 폼(`LabelLayoutSettingsForm`)의 용지 세로 최대 입력 범위를 1778mm로 상향.
  - 상단 툴바의 규격 입력 텍스트박스 폭을 확장하여 4자리 숫자(1778)가 잘림 없이 표시되도록 UI 조정.

### 2. 수정된 파일 목록
| 파일 경로 | 수정 내용 |
|---|---|
| `Model/AppSettings.cs` | `MAX_PAPER_HEIGHT = 1778` 상수 추가 |
| `ViewModel/MainViewModel.cs` | `PaperHeight` 및 `PaperHeightStr` 세터에 최대 1778mm 클램핑 반영 |
| `ViewModel/MainViewModel.Commands.cs` | `IncreasePaperHeightCommand` 증가 상한을 1778mm로 확장 |
| `WpfView/WpfMainWindow.xaml` | 툴바 `PaperHeightStr` 입력 박스 Width 확장(44->54) |
| `WpfView/LabelSetupWizardDialog.xaml.cs` | 높이 증가 버튼 및 커스텀 규격 저장/완료 유효성 검사 상한을 1778mm로 확장 |
| `View/LabelLayoutSettingsForm.cs` | `numPaperHeight.Maximum`을 1778로 변경 및 입력창 크기 확장 |

---

## 📌 [2026-10-29] 30cm 긴 라벨 뷰포트 개선, 요소별 세로쓰기/미러/음영반전 효과 추가, 전역 설정 일원화 및 인쇄값 보존 패치

### 1. 주요 기능 추가 및 변경 사항

#### ① 30cm(300mm) 이상 긴 용지 축소 배율 및 스크롤 뷰포트 정상화
- **줌 최소 배율 완화**: 기존 최소 배율 `0.5(50%)` 제한을 `0.15(15%)`로 대폭 완화하여 300mm가 넘는 긴 라벨도 편집 화면 내에 한눈에 보이도록 축소 가능.
- **상하단 스크롤 및 포커스 이동 보장**: ScrollViewer 내부 메인 캔버스 Grid의 정렬을 `VerticalAlignment="Top"`으로 최적화하여 긴 라벨 상단 0점부터 최하단 끝까지 마우스 휠 및 스크롤바로 완벽히 탐색 가능.

#### ② 텍스트 세로쓰기 (한 글자씩 세로 작성) 기능 추가
- `TemplateElement.IsVertical` 속성 추가 및 상단 툴바/우측 인스펙터 패널에 `[세]` / `[세로쓰기]` 토글 버튼 제공.
- `MainViewModel.ConvertToVerticalText()` 헬퍼를 통해 텍스트 각 글자를 줄바꿈(`\n`) 분리하여 세로 방향으로 자연스럽게 자동 정렬.
- 화면 캔버스 미리보기 및 실제 TSPL/윈도우 그래픽 인쇄(`DrawTextOnDc`)에 100% 동일하게 렌더링.

#### ③ 좌우 미러 (Mirror / 반전) 기능 추가
- `TemplateElement.IsMirrored` 속성 추가 및 상단 툴바/우측 인스펙터 패널에 `[미]` / `[미러 반전]` 토글 버튼 제공.
- 캔버스 미리보기 및 인쇄 렌더러에 중심점 기준 `ScaleTransform(-1, 1)` 수평 반전 변환 적용.

#### ④ 요소별 개별 음영 반전 (Inverse / 흑백 반전) 기능 추가
- `TemplateElement.IsInverse` 속성 추가 및 상단 툴바/우측 인스펙터 패널에 `[반]` / `[음영 반전]` 토글 버튼 제공.
- 라벨 전체가 아닌 **사용자가 선택한 특정 텍스트 요소별로 독립 적용** 가능.
- 캔버스 미리보기 시 검정 배경 박스 + 흰색 글씨로 렌더링되며, 실제 인쇄 출력 시에도 텍스트 영역에 검정 사각형 배경 채우기 + 흰색 글자(`Brushes.White`)로 인쇄.

#### ⑤ 용지 및 프린터 설정 전역 관리 일원화
- `MainViewModel._settings`가 로컬 복사본이 아닌 `AppSettings.Default` 전역 싱글톤을 직접 참조하도록 수정하여, 모든 대화상자와 인쇄 모듈 간 설정 불일치 방지.

#### ⑥ 인쇄 후 기존 입력값 및 서식 보존 (데이터 증발 버그 해결)
- `AppSettings.cs`의 `IsCol1Keep` ~ `IsCol8Keep` 기본값이 `false`여서 인쇄 직후 입력창 데이터가 빈 값으로 초기화되던 치명적인 문제를 해결 (`true`로 기본값 전환하여 인쇄 후에도 입력 내용 유지).

---

### 2. 수정된 파일 목록
| 파일 경로 | 수정 내용 |
|---|---|
| `Model/TemplateElement.cs` | `IsVertical`, `IsMirrored`, `IsInverse` 속성 추가 |
| `Model/AppSettings.cs` | `IsCol1Keep` ~ `IsCol8Keep` 기본값을 `true`로 변경 (인쇄 후 입력값 보존) |
| `ViewModel/MainViewModel.cs` | 줌 최소 배율 클램핑 0.15 완화, `_settings = AppSettings.Default;` 전역 싱글톤 바인딩, `ConvertToVerticalText()` static 헬퍼 추가 |
| `ViewModel/MainViewModel.Template.cs` | `SelectedElementIsVertical`, `SelectedElementIsMirrored`, `SelectedElementIsInverse` 프로퍼티 및 변경 알림 추가 |
| `ViewModel/MainViewModel.Printing.cs` | `DrawTextOnDc()`에 `isVertical`, `isMirrored`, `isInverse` 매개변수 및 미러/음영반전/세로쓰기 인쇄 렌더링 반영 |
| `WpfView/WpfMainWindow.xaml` | 줌 슬라이더 Minimum 0.15 변경, CanvasScrollViewer Grid 수직 Top 정렬, 상단 툴바 및 우측 인스펙터에 세로쓰기/미러/음영반전 버튼 배치 |
| `WpfView/WpfMainWindow.xaml.cs` | `MeasureTextDimensions()` 및 `AddWysiwygItem()`에 세로쓰기, 미러(ScaleTransform), 음영반전(검정배경+흰글자) 적용 |

---

### 3. 앱스토어 / 업데이트 배포용 요약 문구 (Release Notes)
> **[업데이트 안내 v3.x.x]**
> 1. **긴 라벨(30cm 이상) 편집 환경 개선**: 줌 축소 비율(최대 15%) 및 스크롤 영역을 개선하여 긴 용지도 화면 한눈에 보고 편집할 수 있습니다.
> 2. **세로쓰기 기능 추가**: 클릭 한 번으로 글자를 한 글자씩 세로 방향으로 정렬할 수 있습니다.
> 3. **미러(좌우 반전) 기능 추가**: 투명 라벨이나 전사 인쇄를 위한 좌우 대칭 반전 기능을 지원합니다.
> 4. **요소별 음영 반전(흑백 반전) 기능 추가**: 강조하고 싶은 특정 텍스트를 검정 배경에 흰색 글씨로 개별 지정할 수 있습니다.
> 5. **인쇄 후 입력값 보존**: 단건 인쇄 후에도 입력했던 데이터와 서식이 지워지지 않고 안전하게 보존됩니다.

---

## 📌 [2026-09-01] 날짜/시간 및 순번(Serial) 서식, 폰트/사이즈/박스 너비 및 드래그 조절 기능 강화


### 1. 주요 기능 추가 및 변경 사항

#### ① 날짜/시간(DateTime) 서식 제어 기능
- **글꼴(FontFamily), 글자 크기(FontSize), 굵게(Bold), 기울임(Italic) 제어**:
  - `AppSettings`: `DateTimeFont`, `DateTimeSize`, `DateTimeBold`, `DateTimeItalic`, `DateTimeWidth` 프로퍼티 추가 및 `Load()` / `Save()` 영구 저장 동기화.
  - `MainViewModel`: `DateTimeFont`, `DateTimeSize`, `DateTimeBold`, `DateTimeItalic`, `DateTimeWidth` 프로퍼티 및 `IncreaseDateTimeSizeCommand`, `DecreaseDateTimeSizeCommand` 지원.
  - 서식 편집창(`DesignEditorWindow.xaml`): 시스템 폰트 드롭다운 콤보박스(`AvailableFontFamilies`), 크기 증감(`-`/`+`), 굵게(`B`), 기울임(`I`), 실시간 포맷 미리보기 배지 제공.

#### ② 순번(Serial / 일련번호) 서식 제어 기능
- **글꼴(FontFamily), 글자 크기(FontSize), 굵게(Bold), 기울임(Italic) 제어**:
  - `AppSettings`: `SerialFont`, `SerialSize`, `SerialBold`, `SerialItalic`, `SerialWidth` 프로퍼티 추가 및 `Load()` / `Save()` 영구 저장 동기화.
  - `MainViewModel`: `SerialFont`, `SerialSize`, `SerialBold`, `SerialItalic`, `SerialWidth` 프로퍼티 및 `IncreaseSerialSizeCommand`, `DecreaseSerialSizeCommand` 지원.
  - 서식 편집창(`DesignEditorWindow.xaml`): 시스템 폰트 드롭다운 콤보박스(`AvailableFontFamilies`), 크기 증감(`-`/`+`), 굵게(`B`), 기울임(`I`), 실시간 순번 미리보기 배지 제공.

#### ③ 캔버스 마우스 드래그 박스 크기(너비) 조절 및 자동 줄바꿈 최적화
- **우측 크기 조절 핸들(`RESIZE_ITEM_WIDTH_`) 확장**:
  - **표 모드(`IsTableBorderEnabled == true`) 활성화 여부와 관계없이**, 캔버스에서 순번(101) 또는 날짜/시간(99) 아이템 선택 시 우측 파란색 크기 조절 핸들이 항상 나타나도록 조건식 개선.
  - 핸들을 마우스로 잡고 좌우로 드래그하여 박스 너비(줄바꿈 폭)를 실시간 조절 가능.
- **이모지 너비 누락 및 좁아서 내려앉는 줄바꿈 현상 원천 차단**:
  - 기존 텍스트 렌더링 시 `🕒 ` 및 `🔢 ` 이모지 너비가 계산에서 누락되어 박스가 좁게 잡히며 두 줄로 내려앉던 문제를 해결.
  - 폭 미지정(0) 기본 상태에서는 한 줄(`TextWrapping.NoWrap`)로 자동 정렬되어 절대 잘리거나 깨지지 않도록 처리.
  - 사용자가 드래그하여 임의의 폭을 지정했을 때만 해당 너비에 맞춰 자동 줄바꿈(`TextWrapping.Wrap`) 처리.

#### ④ 뷰모델 인덱스 헬퍼 및 인쇄 엔진 완벽 연동
- `MainViewModel.cs` / `MainViewModel.Canvas.cs`:
  - `GetFontByIndex`, `SetFontByIndex`, `GetSizeByIndex`, `SetSizeByIndex`, `GetBoldByIndex`, `SetBoldByIndex`, `GetItalicByIndex`, `SetItalicByIndex`, `GetWidthByIndex`, `SetWidthByIndex`, `ChangeItemFontSize`, `GetPreviewDisplayTextForColumn`에 인덱스 `99`(날짜/시간) 및 `101`(순번), `100`(표 제목) 분기 완전 지원.
- `MainViewModel.Printing.cs`:
  - 실제 프린터 인쇄(`DrawTextOnDc`) 시에도 설정된 `DateTimeWidth`, `SerialWidth` 및 정렬(`SerialAlign`)이 100% 동일하게 반영되도록 처리.

---

### 2. 수정된 파일 목록 및 상세 내용

| 파일 경로 | 수정 내용 |
|---|---|
| `Model/AppSettings.cs` | `DateTimeWidth`, `SerialWidth` 속성 추가 및 `Load()` 메서드 내 `JsonConvert.PopulateObject` 적용 및 누락 필드(`DateTime...`, `Serial...`, `Col1...`~`Col8...`, `Table...`) 복원 로직 보완 |
| `ViewModel/MainViewModel.cs` | `DateTimeFont`, `DateTimeSize`, `DateTimeBold`, `DateTimeItalic`, `DateTimeWidth`, `SerialFont`, `SerialSize`, `SerialBold`, `SerialItalic`, `SerialWidth` 프로퍼티 선언 및 `_settings.Save()` 연동, 인덱스 헬퍼 메서드(99, 101) 분기 추가 |
| `ViewModel/MainViewModel.Commands.cs` | 중복 커맨드 선언 정리 및 크기 증감 커맨드 초기화 확인 |
| `ViewModel/MainViewModel.Canvas.cs` | `ChangeItemFontSize`에 `case 101` 추가, `GetPreviewDisplayTextForColumn`에 `99`, `101` 분기 추가 |
| `ViewModel/MainViewModel.Printing.cs` | 인쇄 렌더링(`DrawTextOnDc`) 호출 시 `DateTimeWidth`, `SerialWidth`, `SerialAlign` 전달 |
| `WpfView/WpfMainWindow.xaml.cs` | `AddWysiwygItem`에서 99/101 너비 전달, 이모지 포함 실제 렌더링 너비 계산 + 3mm 마진, NoWrap 기본값 적용, 표 모드 여부와 관계없이 99/101 우측 크기조절핸들 활성화, 드래그 상태 메시지 개선 |
| `WpfView/DesignEditorWindow.xaml` | '순번 및 날짜/시간 인쇄 설정 카드'에 폰트 콤보박스, 크기 조절(-/+), 굵게(B), 기울임(I), 실시간 미리보기 배지 추가 (폭 설정은 캔버스 마우스 드래그 방식으로 일원화하여 제거) |

---

### 3. 검증 결과
- **MSBuild 컴파일 검증**:
  - `MSBuild.exe BarcodePrint.csproj /p:Configuration=Debug /p:Platform=AnyCPU` -> **성공 (경고 0, 오류 0)**
  - `MSBuild.exe BarcodePrint.csproj /p:Configuration=Release /p:Platform=AnyCPU` -> **성공 (Obfuscar 난독화 및 패키징 완료)**

---

## 📌 [2026-09-08] TSPL 센서 보정(Calibration/Detect), 원점(HOME), 피드(FEED) 열전사 리본(Ribbon) 제어 동기화

### 1. 주요 기능 추가 및 변경 사항
- **열전사(Ribbon / IsRibbon) 모드 연동 안정화**:
  - 감열(Direct Thermal, 리본 미사용)일 경우: `SET RIBBON OFF\r\n` 주입
  - 열전사(Thermal Transfer, 리본/먹지 사용)일 경우: `SET RIBBON ON\r\nSET ENCODER OFF\r\nSET RIBBONEND OFF\r\n` 주입
  - 라벨 이송 시 리본 구동 모터가 동기화되어 공회전 또는 헛돌지 않고, 엔코더 센서 오류에 따른 멈춤/오작동을 원천 차단.
- **WPF ViewModel 제어(`MainViewModel.Printing.cs`)**:
  - `ExecutePrinterAction`의 `CALIBRATE`, `GAPDETECT`, `AUTODETECT`, `HOME`, `FEED` 명령어 빌드 시 `IsRibbon` 설정에 따른 리본 제어 명령 헤더 자동 주입.
  - `HOME` 실행 시 용지 규격(`SIZE`), 갭/블랙마크(`GAP`/`BLINE`) 및 리본 설정을 먼저 세팅 후 원점 정렬(`HOME\r\nCLS\r\n`) 수행.
- **Model 레거시/엔진 제어(`PrintManager.cs`, `BarcodePrintManager.cs`)**:
  - `PrintManager.Command_CALIBRATION()`: `PRINT_INFO_CLASS.PrinterInfo.bRibbon`에 맞춰 `SET RIBBON` 블록 주입.
  - `PrintManager.Command_HOME()`: 기존 `GAPDETECT`로 잘못 덮어쓰던 복사 오류를 수정하고 규격, 갭, 리본 설정 및 `HOME\r\nCLS\r\n` 정상 주입.
  - `PrintManager.Command_FEED()`: 신규 메서드 추가 및 `BarcodePrintManager.SendPrintCommand` 연동 지원.

### 2. 검증 결과
- **MSBuild 컴파일 검증**: `MSBuild.exe BarcodePrint.csproj -t:Build -p:Configuration=Debug` -> **빌드 성공 (오류 0개)**

---

## 📌 [2026-10-27] TSPL 공식 매뉴얼(20p) 표준 순서에 맞춘 CLS 호출 시점 정상화 (쏠림 현상 방지)

### 1. 주요 변경 사항
- **TSPL 매뉴얼 20p 준수 (`CLS must be placed after SIZE command`)**:
  - `Model/PrintManager.cs`의 `ConvertBitmapToTSPL_` 및 `BarcodeToTSPL_`에서 `CLS`가 `SIZE`보다 먼저 호출되던 순서를 수정.
  - `SIZE`, `SPEED`, `DENSITY`, `DIRECTION`, `REFERENCE`, `SHIFT` 및 후처리 명령 이후, `BITMAP`/`BARCODE` 생성 직전에 `CLS`가 호출되도록 변경.
  - 라벨 크기(폭/높이) 변경 시 프린터 내부 그래픽 버퍼가 새로 할당된 후 깨끗이 초기화되어, 40mm 등 좁은 용지에서 원점 계산이 꼬이거나 인쇄물이 좌측으로 쏠리는 현상을 원천 방지.
  - Android, Mac(`TsplImageBuilder.swift`), iOS(`TsplImageBuilder.swift`)와 TSPL 패킷 생성 순서를 100% 동일하게 표준화.

### 2. 검증 결과
- **MSBuild 컴파일 검증**: `MSBuild.exe BarcodePrint.sln -t:Build -p:Configuration=Debug` -> **빌드 성공 (오류 0개, 경고 4개)**

---

## 📌 [2026-11-03] 일부 PC 환경에서 인쇄 시 백지(빈 종이)만 배출되는 문제 해결

### 1. 근본 원인 분석
1. **WPF `RenderTargetBitmap` + GDI+ `LockBits(Format24bppRgb)` 투명도 처리 결함**:
   - `GenerateLabelBitmapForPrint`에서 비주얼 트리가 없는 오프스크린 `DrawingVisual`을 렌더링할 때, 특정 PC(내장 그래픽, 가상 환경, RDP, 고DPI 배율)의 하드웨어 가속 이슈로 비트맵 배경/요소가 투명 픽셀(Alpha=0)로 캡처되는 현상 발생.
   - `PngBitmapEncoder`를 거쳐 GDI+ `Bitmap`을 `LockBits(Format24bppRgb)`로 변환할 때, 특정 Windows OS 환경에서 Alpha=0인 투명 픽셀이 흰색(255, 255, 255)으로 일괄 합성됨.
   - 모든 픽셀이 grayscale >= 128로 판정되어 결과 비트맵이 0x00(흰색)이 되고, 비트 반전 후 TSPL 프린터에는 0xFF(미인쇄 공백)만 전송되어 프린터가 백지만 출력함.
2. **TSPL `PRINT` 명령 매수(Copies) 인자 생략 문제**:
   - `PRINT {copy}` 단일 인자 전송 시 일부 프린터 모델/펌웨어에서 파라미터 인식 결함으로 인쇄 없이 빈 페이지만 배출(Feed)하는 현상 발생 (공식 매뉴얼 규격: `PRINT m,n`).
3. **Windows Spooler ANSI API (`OpenPrinterA`) 로캘 호환성**:
   - `PrinterHelper.cs`에서 ANSI Win32 API를 사용하여 비한국어 로캘 또는 영문 윈도우 환경에서 한글 프린터명 전송 시 스풀러 연결이 실패하거나 오작동함.

### 2. 주요 개선 사항
- **무-코덱(No-Codec) 메모리 직접 추출 및 순수 흰색 알파 플래트닝 (`MainViewModel.Printing.cs`)**:
  - `PngBitmapEncoder`/`MemoryStream` 코덱 과정을 완전히 제거하고 `rtb.CopyPixels`로 메모리에서 직접 버퍼 추출.
  - 모든 픽셀의 Alpha 채널을 검사하여 투명 픽셀(`a == 0`)을 순수 불투명 흰색(255, 255, 255)으로 강제 변환하고, 반투명 안티에일리어싱 픽셀을 흰색 배경과 완벽하게 블렌딩 합성(Flatten).
  - 알파 채널이 배제된 `Format32bppRgb` GDI+ 비트맵으로 1:1 메모리 복사하여 투명도 오염 원천 차단.
- **TSPL 1-Bit 라스터 직접 변환 엔진 구현 (`Model/PrintManager.cs`)**:
  - `ConvertBitmapToTsplRaster` 신규 구현: `Format1bppIndexed` 비트맵 생성 및 이중 LockBits/비트반전을 걷어내고, BGRA 32bpp 픽셀에서 직접 1-Bit TSPL 라스터 바이트(`byte[]`)를 1회 순회로 생성 (안드로이드/Mac 엔진과 100% 동일).
  - Alpha < 128인 픽셀은 확실한 공백(도트 오프, 비트 1)으로 유지하고, 유효한 검은색 도트만 0으로 비트 클리어.
  - `SIZE` 명령어의 0 이하 기본값(80x60mm) 방어 및 `CultureInfo.InvariantCulture` 적용.
  - `CODEPAGE 949\r\n` 추가 및 `PRINT {safeCopy},1\r\n` 2개 인자 명시로 프린터 펌웨어 호환성 극대화.
- **Windows Spooler 유니코드 API 전면 전환 (`Model/TSPLPrint/PrinterHelper.cs`)**:
  - `OpenPrinterW`, `StartDocPrinterW`, `DOCINFOW` 유니코드 Win32 API로 전환하여 다국어/영문 Windows PC 환경 호환성 완벽 확보.

### 3. 검증 결과
- **MSBuild 컴파일 검증**: `MSBuild.exe BarcodePrint.sln /p:Configuration=Debug` -> **빌드 성공 (오류 0개, 경고 4개)**


