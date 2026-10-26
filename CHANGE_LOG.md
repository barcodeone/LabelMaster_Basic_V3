# 📋 LabelMaster_Basic_v2 변경 및 업데이트 로그 (Change Log)

본 문서는 `LabelMaster_Basic_v2` 프로젝트의 기능 추가, UI 개선, 버그 수정 및 리팩토링 내역을 상세히 기록합니다.

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

