using System;
using System.Collections.Generic;
using System.IO;
using Argon;

namespace BarcodePrint.Model
{
    public class RecentWorkItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "이전 작업";
        public int Width { get; set; } = 70;
        public int Height { get; set; } = 28;
        public string PaperType { get; set; } = "GAP";
        public int Rotation { get; set; } = 0;
        public string LastModified { get; set; } = "";
        public string Description { get; set; } = "";
        public string TemplateFilePath { get; set; } = "";
    }

    /// <summary>
    /// PDF 인쇄 전용 자주 쓰는 용지 규격 프리셋 항목
    /// </summary>
    public class PdfPaperPresetItem
    {
        public string Name { get; set; } = "";
        public double Width { get; set; } = 100.0;
        public double Height { get; set; } = 100.0;

        public PdfPaperPresetItem() { }

        public PdfPaperPresetItem(string name, double width, double height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? $"{Width}x{Height}" : Name;
        }
    }

    /// <summary>
    /// 통합 애플리케이션 설정 (ExcelSettings + AddressSettings + ColumnPositionSettings 통합)
    /// </summary>
    public class AppSettings
    {
        // 설정 파일 경로 (평문 JSON 사용: Info/app_settings.json)
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info", "app_settings.json");

        private static AppSettings _default;
        public static AppSettings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new AppSettings();
                    _default.Load();
                }
                return _default;
            }
        }

        // 엑셀 관련 설정
        public string ExcelFilePath { get; set; } = "";
        public bool IsExcelModeEnabled { get; set; } = false;
        public string LastUsedFolder { get; set; } = "";

        // 마지막 프린터 연결 정보 (프로그램 재실행 시 자동 복원 및 연결 시도)
        public string LastSelectedConnectionType { get; set; } = "ALL"; // "ALL", "USB", "LAN", "BT"
        public string LastSelectedPrinterName { get; set; } = "";
        public string LastTcpPrinterIp { get; set; } = "";
        public int LastTcpPrinterPort { get; set; } = 9100;
        public string LastBluetoothPortName { get; set; } = "";
        public string LastTcpPrinterModel { get; set; } = "";

        // 시작화면 하단 배너 HTML 외부 링크 URL (비어있으면 Info/quick_tools.html 사용)
        public string QuickToolsBannerUrl { get; set; } = "";

        // 주소 라벨 설정

        public string SenderReceiverLabel { get; set; } = "표시없음"; // "보내는 분", "받는 분", "표시없음"

        // 컬럼 위치 설정 (5개 컬럼) - 기존 호환성 유지

        public string ColumnPositionData { get; set; } = ""; // 파이프(|) 구분자로 저장

        // 용지 타입별 컬럼 위치 설정 (각 용지 타입별로 저장)
        // 형식: "PaperType1:ColumnData1;PaperType2:ColumnData2;..."

        public string PaperTypeColumnPositionData { get; set; } = ""; // 용지 타입별 컬럼 위치 데이터

        // Title 설정 (가격표용)

        public int TitleSize { get; set; } = 8; // Title 폰트 크기 (mm)
        public int TitleOffsetY { get; set; } = 2; // Title Y축 오프셋 (mm, 최상단 기준 상대좌표)
        public int TitleOffsetX { get; set; } = 0; // Title X축 오프셋 (mm, 중앙 기준 상대좌표)
        public bool TitleEnabled { get; set; } = false; // Title On/Off 상태 (기본: 꺼짐)

        // 용지 타입별 Title 설정
        // 형식: "PaperType1:Size,OffsetY;PaperType2:Size,OffsetY;..."

        public string PaperTypeTitleData { get; set; } = ""; // 용지 타입별 Title 데이터

        // 마지막 입력값 저장 (가격표용)

        public string LastProductName { get; set; } = ""; // 품명
        public string LastPrice { get; set; } = ""; // 가격 (포맷 적용된 상태로 저장)
        public string LastBarcode { get; set; } = ""; // 바코드
        public string LastOption { get; set; } = ""; // 옵션
        public int LastPrintCopies { get; set; } = 1; // 인쇄 매수

        // 엑셀 다중 컬럼 설정 저장
        // 형식: "컬럼명1:데이터타입1|컬럼명2:데이터타입2|..."

        public string MultiColumnSettingsData { get; set; } = ""; // 선택한 엑셀 컬럼 정보

        // 현재 모드 저장

        public string CurrentMode { get; set; } = "Manual"; // "Manual" 또는 "Excel"

        // 최근 작업 내역 플래그 및 최대 4개 이력 목록
        public bool HasRecentWork { get; set; } = false;
        public string LastWorkedDate { get; set; } = "";
        public string LastWorkedTitle { get; set; } = "";
        public List<RecentWorkItem> RecentWorks { get; set; } = new List<RecentWorkItem>();

        public void AddRecentWork(string title, int width, int height, string paperType = "GAP", int rotation = 0, string templatePath = "")
        {
            if (width <= 0 || height <= 0) return;
            if (RecentWorks == null) RecentWorks = new List<RecentWorkItem>();

            // 동일 템플릿 파일 또는 동일 규격이 이미 있으면 제거 후 최신순으로 맨 앞에 재삽입
            RecentWorks.RemoveAll(x =>
                (!string.IsNullOrEmpty(templatePath) && x.TemplateFilePath == templatePath) ||
                (string.IsNullOrEmpty(templatePath) && x.Width == width && x.Height == height && (string.IsNullOrEmpty(title) || x.Title == title)));

            string displayTitle = !string.IsNullOrEmpty(title) ? title : $"{width}×{height} mm 라벨";
            string dateStr = DateTime.Now.ToString("yyyy.MM.dd HH:mm");

            RecentWorks.Insert(0, new RecentWorkItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = displayTitle,
                Width = width,
                Height = height,
                PaperType = paperType ?? "GAP",
                Rotation = rotation,
                LastModified = dateStr,
                Description = $"{width}×{height} mm · {dateStr}",
                TemplateFilePath = templatePath ?? ""
            });

            // 최대 4개까지만 보관
            if (RecentWorks.Count > 4)
            {
                RecentWorks = new List<RecentWorkItem>(RecentWorks.GetRange(0, 4));
            }

            HasRecentWork = true;
            LastWorkedDate = DateTime.Now.ToString("yyyy.MM.dd");
            LastWorkedTitle = displayTitle;
            PaperWidth = width;
            PaperHeight = height;
            Save();
        }

        // 용지 및 바코드 기본 설정 (SelectedPaperType 대신 사용)
        public int PaperWidth { get; set; } = 80;
        public int PaperHeight { get; set; } = 60;
        public string BarcodeType { get; set; } = "QR_CODE";
        public bool IsRotated { get => LabelRotationAngle == 180; set => LabelRotationAngle = value ? 180 : 0; }
        public int LabelRotationAngle { get; set; } = 0; // 0, 90, 180, 270 도 회전

        // 메인 뷰 자주 사용하는 라벨 규격 프리셋
        public List<PresetLabelItem> FavoritePresets { get; set; } = GetDefaultPresets();

        public static List<PresetLabelItem> GetDefaultPresets()
        {
            return new List<PresetLabelItem>
            {
                new PresetLabelItem("preset_1", "택배 송장", 100, 100, "쇼핑몰 택배 송장 및 대형 물류 라벨", "#2563EB"),
                new PresetLabelItem("preset_2", "상품 라벨", 80, 50, "식품 표기사항 및 박스 부착용 라벨", "#059669"),
                new PresetLabelItem("preset_3", "바코드 라벨", 50, 30, "제품 바코드, 부품 식별 및 재고 관리", "#7C3AED"),
                new PresetLabelItem("preset_4", "소형 부품", 40, 20, "전자부품, 약품 및 소형 가격표 라벨", "#D97706")
            };
        }

        // PDF 인쇄 전용 자주 쓰는 라벨 용지 규격 프리셋 리스트
        public List<PdfPaperPresetItem> PdfPaperPresets { get; set; } = GetDefaultPdfPaperPresets();

        public static List<PdfPaperPresetItem> GetDefaultPdfPaperPresets()
        {
            return new List<PdfPaperPresetItem>
            {
                new PdfPaperPresetItem("100x100", 100, 100),
                new PdfPaperPresetItem("100x150", 100, 150),
                new PdfPaperPresetItem("80x50", 80, 50),
                new PdfPaperPresetItem("70x100", 70, 100),
                new PdfPaperPresetItem("70x30", 70, 30),
                new PdfPaperPresetItem("50x30", 50, 30)
            };
        }

        // 사용자 지정 5개 컬럼 명칭 및 가격 서식 적용 옵션
        // 활성화된 컬럼 개수 (1 ~ 8개 동적 조절)
        public int ActiveColumnCount { get; set; } = 5;

        // 컬럼 1 ~ 8 명칭
        public string Column1Name { get; set; } = "컬럼1";
        public string Column2Name { get; set; } = "컬럼2";
        public string Column3Name { get; set; } = "컬럼3";
        public string Column4Name { get; set; } = "컬럼4";
        public string Column5Name { get; set; } = "컬럼5";
        public string Column6Name { get; set; } = "컬럼6";
        public string Column7Name { get; set; } = "컬럼7";
        public string Column8Name { get; set; } = "컬럼8";
        public Dictionary<string, string> ExtendedColumnHeaders { get; set; } = new Dictionary<string, string>();

        public bool IsColumn1PriceFormat { get; set; } = false;
        public bool IsColumn2PriceFormat { get; set; } = false;
        public bool IsColumn3PriceFormat { get; set; } = false;
        public bool IsColumn4PriceFormat { get; set; } = false;
        public bool IsColumn5PriceFormat { get; set; } = false;
        public bool IsColumn6PriceFormat { get; set; } = false;
        public bool IsColumn7PriceFormat { get; set; } = false;
        public bool IsColumn8PriceFormat { get; set; } = false;

        // 명칭(제목) 포함 출력 옵션 (예: "색상: 빨강")
        public bool IsColumn1IncludeHeader { get; set; } = false;
        public bool IsColumn2IncludeHeader { get; set; } = false;
        public bool IsColumn3IncludeHeader { get; set; } = false;
        public bool IsColumn4IncludeHeader { get; set; } = false;
        public bool IsColumn5IncludeHeader { get; set; } = false;
        public bool IsColumn6IncludeHeader { get; set; } = false;
        public bool IsColumn7IncludeHeader { get; set; } = false;
        public bool IsColumn8IncludeHeader { get; set; } = false;

        // 디자인 편집 - 컬럼별 폰트 글꼴, 크기, 굵기, 기울임 및 위치 (X, Y mm)
        public string Col1Font { get; set; } = "맑은 고딕";
        public string Col2Font { get; set; } = "맑은 고딕";
        public string Col3Font { get; set; } = "맑은 고딕";
        public string Col4Font { get; set; } = "맑은 고딕";
        public string Col5Font { get; set; } = "맑은 고딕";
        public string Col6Font { get; set; } = "맑은 고딕";
        public string Col7Font { get; set; } = "맑은 고딕";
        public string Col8Font { get; set; } = "맑은 고딕";

        public bool Col1Bold { get; set; } = true;
        public bool Col2Bold { get; set; } = true;
        public bool Col3Bold { get; set; } = true;
        public bool Col4Bold { get; set; } = true;
        public bool Col5Bold { get; set; } = true;
        public bool Col6Bold { get; set; } = true;
        public bool Col7Bold { get; set; } = true;
        public bool Col8Bold { get; set; } = true;

        public bool Col1Italic { get; set; } = false;
        public bool Col2Italic { get; set; } = false;
        public bool Col3Italic { get; set; } = false;
        public bool Col4Italic { get; set; } = false;
        public bool Col5Italic { get; set; } = false;
        public bool Col6Italic { get; set; } = false;
        public bool Col7Italic { get; set; } = false;
        public bool Col8Italic { get; set; } = false;

        public int Col1Size { get; set; } = 12;
        public int Col2Size { get; set; } = 12;
        public int Col3Size { get; set; } = 12;
        public int Col4Size { get; set; } = 12;
        public int Col5Size { get; set; } = 12;
        public int Col6Size { get; set; } = 12;
        public int Col7Size { get; set; } = 12;
        public int Col8Size { get; set; } = 12;

        public int Col1OffsetY { get; set; } = 2;
        public int Col2OffsetY { get; set; } = 7;
        public int Col3OffsetY { get; set; } = 12;
        public int Col4OffsetY { get; set; } = 17;
        public int Col5OffsetY { get; set; } = 22;
        public int Col6OffsetY { get; set; } = 27;
        public int Col7OffsetY { get; set; } = 32;
        public int Col8OffsetY { get; set; } = 37;

        public int Col1OffsetX { get; set; } = 2;
        public int Col2OffsetX { get; set; } = 2;
        public int Col3OffsetX { get; set; } = 2;
        public int Col4OffsetX { get; set; } = 2;
        public int Col5OffsetX { get; set; } = 2;
        public int Col6OffsetX { get; set; } = 2;
        public int Col7OffsetX { get; set; } = 2;
        public int Col8OffsetX { get; set; } = 2;

        // 컬럼별 사용자 지정 너비 (0: 자동/최대 너비, >0: 고정 너비 mm)
        public int Col1Width { get; set; } = 0;
        public int Col2Width { get; set; } = 0;
        public int Col3Width { get; set; } = 0;
        public int Col4Width { get; set; } = 0;
        public int Col5Width { get; set; } = 0;
        public int Col6Width { get; set; } = 0;
        public int Col7Width { get; set; } = 0;
        public int Col8Width { get; set; } = 0;

        // 바코드 변환 옵션 (컬럼별 텍스트 -> 바코드/QR 변환)
        public bool IsColumn1Barcode { get; set; } = false;
        public bool IsColumn2Barcode { get; set; } = false;
        public bool IsColumn3Barcode { get; set; } = false;
        public bool IsColumn4Barcode { get; set; } = false;
        public bool IsColumn5Barcode { get; set; } = false;
        public bool IsColumn6Barcode { get; set; } = false;
        public bool IsColumn7Barcode { get; set; } = false;
        public bool IsColumn8Barcode { get; set; } = false;

        public string Col1BarcodeType { get; set; } = "CODE128";
        public string Col2BarcodeType { get; set; } = "CODE128";
        public string Col3BarcodeType { get; set; } = "CODE128";
        public string Col4BarcodeType { get; set; } = "CODE128";
        public string Col5BarcodeType { get; set; } = "CODE128";
        public string Col6BarcodeType { get; set; } = "CODE128";
        public string Col7BarcodeType { get; set; } = "CODE128";
        public string Col8BarcodeType { get; set; } = "CODE128";

        // 바코드 하단 텍스트(Human Readable Text) 보이기/숨김 옵션
        public bool Col1ShowBarcodeText { get; set; } = true;
        public bool Col2ShowBarcodeText { get; set; } = true;
        public bool Col3ShowBarcodeText { get; set; } = true;
        public bool Col4ShowBarcodeText { get; set; } = true;
        public bool Col5ShowBarcodeText { get; set; } = true;
        public bool Col6ShowBarcodeText { get; set; } = true;
        public bool Col7ShowBarcodeText { get; set; } = true;
        public bool Col8ShowBarcodeText { get; set; } = true;

        // 컬럼 내용 텍스트 정렬 ("LEFT", "CENTER", "RIGHT")
        public string Col1Align { get; set; } = "LEFT";
        public string Col2Align { get; set; } = "LEFT";
        public string Col3Align { get; set; } = "LEFT";
        public string Col4Align { get; set; } = "LEFT";
        public string Col5Align { get; set; } = "LEFT";
        public string Col6Align { get; set; } = "LEFT";
        public string Col7Align { get; set; } = "LEFT";
        public string Col8Align { get; set; } = "LEFT";

        // 컬럼 제목(Header) 텍스트 정렬 ("LEFT", "CENTER", "RIGHT")
        public string Col1HeaderAlign { get; set; } = "CENTER";
        public string Col2HeaderAlign { get; set; } = "CENTER";
        public string Col3HeaderAlign { get; set; } = "CENTER";
        public string Col4HeaderAlign { get; set; } = "CENTER";
        public string Col5HeaderAlign { get; set; } = "CENTER";
        public string Col6HeaderAlign { get; set; } = "CENTER";
        public string Col7HeaderAlign { get; set; } = "CENTER";
        public string Col8HeaderAlign { get; set; } = "CENTER";

        // 표 모드 시 각 컬럼(행)별 개별 세로 높이 (mm, 0: 자동 균등 배분)
        public int Col1RowHeight { get; set; } = 0;
        public int Col2RowHeight { get; set; } = 0;
        public int Col3RowHeight { get; set; } = 0;
        public int Col4RowHeight { get; set; } = 0;
        public int Col5RowHeight { get; set; } = 0;
        public int Col6RowHeight { get; set; } = 0;
        public int Col7RowHeight { get; set; } = 0;
        public int Col8RowHeight { get; set; } = 0;

        // 현재 날짜-시간 출력 설정
        public bool IsDateTimeEnabled { get; set; } = false;
        public int DateTimeFormatType { get; set; } = 0; // 0: 날짜+시간(모두 출력), 1: 날짜만 출력, 2: 시간만 출력
        public string DateTimePrefix { get; set; } = ""; // 사용자 지정 접두사 (예: 일시: , 제조일: 등)
        public string DateTimeFont { get; set; } = "맑은 고딕";
        public bool DateTimeBold { get; set; } = false;
        public bool DateTimeItalic { get; set; } = false;
        public int DateTimeOffsetX { get; set; } = 2;
        public int DateTimeOffsetY { get; set; } = 25;
        public int DateTimeSize { get; set; } = 10;
        public int DateTimeWidth { get; set; } = 0; // mm, 0: 자동 폭

        // 미리보기 확대/축소 비율 (ZoomRatio - 기본 140%)
        public double ZoomRatio { get; set; } = 1.4;

        // 프린터 연결 방식 및 대상 설정 (USB, LAN, BLUETOOTH)
        public string PrinterConnectionType { get; set; } = "USB"; // "USB", "LAN", "BLUETOOTH"
        public string TcpPrinterIp { get; set; } = "192.168.0.50"; // TCP/IP 프린터 IP 주소
        public int TcpPrinterPort { get; set; } = 9100; // TCP/IP 프린터 포트
        public string BluetoothPortName { get; set; } = ""; // 블루투스 직렬 COM 포트 (장치 선택 시 자동 매핑)

        // 프린터 하드웨어 설정 (기존 프로젝트 이식)
        public int PrintDarkness { get; set; } = 8; // 인쇄 농도 (1~15)
        public int PrintSpeed { get; set; } = 4; // 인쇄 속도 (2~6 inch/sec)
        public string PaperType { get; set; } = "GAP"; // GAP, BLINE, CONTINUOUS
        public int GapHeight { get; set; } = 2; // 라벨 간격 (mm)
        public double BlineOffset { get; set; } = 0.0; // 블랙마크 이탈량 / 표시 오프셋 (mm)
        public bool IsRibbon { get; set; } = false; // 감열(false) / 열전사(true)
        public int PrintCopies { get; set; } = 1; // 기본 인쇄 매수
        public int SinglePrintCopies { get; set; } = 1; // 단건 인쇄 매수
        public int ExcelPrintCopies { get; set; } = 1; // 엑셀 인쇄 매수
        public int PrinterOffsetX { get; set; } = 0; // 프린터 하드웨어 X 오프셋 (mm)
        public int PrinterOffsetY { get; set; } = 0; // 프린터 하드웨어 Y 오프셋 (mm)
        public int TearOffOffset { get; set; } = 0; // 절취/배출 위치 보정 오프셋 (mm)

        // 인쇄 후 동작 설정 (NONE: 없음, CUTTER: 절단, TEAR: 뜯어내기, PEEL: 잘라내기/박리, DEFAULT: 현재프린터설정사용)
        public string PostPrintAction { get; set; } = "DEFAULT";
        public string PostPrintCutTrigger { get; set; } = "EVERY_PAGE"; // EVERY_PAGE: 모든 페이지 뒤, AFTER_COPIES: 동일한 사본 뒤, AFTER_JOB: 작업 뒤, CUSTOM_INTERVAL: 지정한 간격 뒤
        public int PostPrintCutInterval { get; set; } = 1; // 지정한 간격 매수
        public int PostPrintOffset { get; set; } = 0; // 인쇄 후 동작 오프셋 (mm)
        // 사용자 정의 맞춤 라벨 규격 목록 (예: "70x40;90x60")
        public string CustomPaperSizes { get; set; } = "";

        // 구간 인쇄 설정
        public int PrintRangeStart { get; set; } = 1;
        public int PrintRangeEnd { get; set; } = 1;

        // 각 컬럼별 입력 삭제 방지 (값 고정/유지) 옵션
        public bool IsCol1Keep { get; set; } = false;
        public bool IsCol2Keep { get; set; } = false;
        public bool IsCol3Keep { get; set; } = false;
        public bool IsCol4Keep { get; set; } = false;
        public bool IsCol5Keep { get; set; } = false;
        public bool IsCol6Keep { get; set; } = false;
        public bool IsCol7Keep { get; set; } = false;
        public bool IsCol8Keep { get; set; } = false;

        // 마지막 컬럼 Enter 키 입력 시 동작 모드 ("PRINT_AND_RECORD" 또는 "RECORD_ONLY")
        public string EnterActionType { get; set; } = "PRINT_AND_RECORD";

        // 그리드 행 복사 추가 수량 (기본 1개)
        public int CopyCount { get; set; } = 1;

        // 마지막으로 연결 성공한 프린터 정보
        public string LastPrinterName { get; set; } = "";
        public string LastPrinterPort { get; set; } = "";
        public string LastPrinterSerial { get; set; } = "";

        // 엑셀 매핑 정보 기억 및 자동 적용 설정
        public bool RememberLastExcelMapping { get; set; } = true;
        public List<string> LastExcelColumnMappingNames { get; set; } = new List<string>();
        public List<int> LastExcelColumnIndices { get; set; } = new List<int>();

        // 표 (Table / 격자 테두리) 인쇄 설정
        public bool IsTableBorderEnabled { get; set; } = false;
        public int TableBorderStyle { get; set; } = 0; // 0: 전체 격자형(Grid), 1: 외곽 테두리형(Outer Box), 2: 가로 구분선형(Horizontal Lines)
        public int TableBorderThickness { get; set; } = 1; // 1, 2, 3 pt
        public int TableBorderPadding { get; set; } = 1; // mm

        // 표 크기 및 위치 조절 (mm, 0: 자동)
        public int TableOffsetX { get; set; } = 2;
        public int TableOffsetY { get; set; } = 2;
        public int TableWidth { get; set; } = 0;
        public int TableHeight { get; set; } = 0;

        // 표 위 제목(Title) 설정
        public bool IsTableTitleEnabled { get; set; } = false;
        public string TableTitleText { get; set; } = "라벨 정보표";
        public string TableTitleFont { get; set; } = "맑은 고딕";
        public bool TableTitleBold { get; set; } = true;
        public bool TableTitleItalic { get; set; } = false;
        public int TableTitleSize { get; set; } = 13; // pt
        public int TableTitleHeight { get; set; } = 6; // mm
        public string TableTitleAlign { get; set; } = "CENTER"; // "LEFT", "CENTER", "RIGHT"
        public int TableTitleOffsetX { get; set; } = 0; // mm
        public int TableTitleOffsetY { get; set; } = 2; // mm
        public bool IsTableTitleUnderline { get; set; } = true; // 제목 밑줄 표시 여부
        public int TableTitleSpacing { get; set; } = 2; // 제목과 표 사이의 간격 (mm)

        // 표 방향 (0: 세로형/헤더 좌측, 1: 가로형/헤더 상단)
        public int TableOrientation { get; set; } = 0;

        // 표 내부 제목(Header) 열 너비 (mm, 세로형 표일 때 제목 표시 체크 시 세로 구분선 위치)
        public int TableHeaderWidthMm { get; set; } = 20;

        // 표 내부 제목(Header) 행 높이 (mm, 가로형 표일 때 상단 헤더 행 높이)
        public int TableHeaderHeightMm { get; set; } = 7;

        // 순번 (시리얼/일련번호/카운터) 인쇄 설정
        public bool IsSerialEnabled { get; set; } = false;
        public int SerialFormatIndex { get; set; } = 4; // 0: 1, 1: No. 1, 2: 001, 3: 0001, 4: No. 001, 5: No. 0001
        public int SerialDirectionIndex { get; set; } = 0; // 0: 증가 (+), 1: 감소 (-)
        public string SerialPrefix { get; set; } = "No. ";
        public string SerialSuffix { get; set; } = "";
        public string SerialFont { get; set; } = "맑은 고딕";
        public bool SerialBold { get; set; } = true;
        public bool SerialItalic { get; set; } = false;
        public int SerialDigits { get; set; } = 3; // 0: 원본 그대로, 3: 001, 4: 0001
        public int SerialStartNumber { get; set; } = 1; // 시작 번호 / 현재 번호
        public int SerialStep { get; set; } = 1; // 인쇄 시 증감 단위
        public int SerialSize { get; set; } = 10; // 폰트 크기
        public int SerialOffsetX { get; set; } = 2; // mm
        public int SerialOffsetY { get; set; } = 2; // mm
        public int SerialWidth { get; set; } = 0; // mm, 0: 자동 폭
        public string SerialAlign { get; set; } = "LEFT"; // LEFT, CENTER, RIGHT
        public int DateTimeFormatIndex { get; set; } = 0; // 0: YYYY-MM-DD, 1: YYYY.MM.DD HH:mm, 2: YYYY-MM-DD HH:mm:ss, 3: HH:mm:ss, 4: YYYY년 MM월 DD일, 5: YY/MM/DD

        /// <summary>
        /// 설정을 파일에 저장
        /// </summary>
        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json, System.Text.Encoding.UTF8);

                System.Diagnostics.Debug.WriteLine($"통합 설정 저장 완료 (JSON): {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일에서 설정을 로드
        /// </summary>
        public void Load()
        {
            try
            {
                string targetPath = SettingsFilePath;
                if (!File.Exists(targetPath))
                {
                    // 구버전 파일(b744ae5dc826f239.dat)과의 하위 호환성 체크
                    string legacyPath = SecureFileNameHelper.GetSecureFilePath("app_settings");
                    if (File.Exists(legacyPath))
                    {
                        targetPath = legacyPath;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"통합 설정 파일이 없음: {SettingsFilePath} - 기본값 사용");
                        return;
                    }
                }

                string content = File.ReadAllText(targetPath, System.Text.Encoding.UTF8);

                // JSON 형식인지 체크 (구버전 텍스트 호환성)
                if (content.TrimStart().StartsWith("{"))
                {
                    try
                    {
                        JsonConvert.PopulateObject(content, this);
                    }
                    catch
                    {
                        var loadedSettings = JsonConvert.DeserializeObject<AppSettings>(content);
                        if (loadedSettings != null)
                        {
                            this.ExcelFilePath = loadedSettings.ExcelFilePath ?? "";
                            this.IsExcelModeEnabled = loadedSettings.IsExcelModeEnabled;
                            this.LastUsedFolder = loadedSettings.LastUsedFolder ?? "";
                            this.SenderReceiverLabel = loadedSettings.SenderReceiverLabel ?? "표시없음";
                            this.ColumnPositionData = loadedSettings.ColumnPositionData ?? "";
                            this.PaperTypeColumnPositionData = loadedSettings.PaperTypeColumnPositionData ?? "";
                            this.TitleSize = loadedSettings.TitleSize;
                            this.TitleOffsetY = loadedSettings.TitleOffsetY;
                            this.TitleOffsetX = loadedSettings.TitleOffsetX;
                            this.TitleEnabled = loadedSettings.TitleEnabled;
                            this.PaperTypeTitleData = loadedSettings.PaperTypeTitleData ?? "";
                            this.LastProductName = loadedSettings.LastProductName ?? "";
                            this.LastPrice = loadedSettings.LastPrice ?? "";
                            this.LastBarcode = loadedSettings.LastBarcode ?? "";
                            this.LastOption = loadedSettings.LastOption ?? "";
                            this.LastPrintCopies = loadedSettings.LastPrintCopies;
                            this.MultiColumnSettingsData = loadedSettings.MultiColumnSettingsData ?? "";
                            this.CurrentMode = loadedSettings.CurrentMode ?? "Manual";
                            this.HasRecentWork = loadedSettings.HasRecentWork;
                            this.LastWorkedDate = loadedSettings.LastWorkedDate ?? "";
                            this.LastWorkedTitle = loadedSettings.LastWorkedTitle ?? "";
                            this.PaperWidth = loadedSettings.PaperWidth > 0 ? loadedSettings.PaperWidth : 80;
                            this.PaperHeight = loadedSettings.PaperHeight > 0 ? loadedSettings.PaperHeight : 60;
                            this.BarcodeType = loadedSettings.BarcodeType ?? "QR_CODE";
                            this.IsRotated = loadedSettings.IsRotated;
                            this.LabelRotationAngle = loadedSettings.LabelRotationAngle;
                            this.FavoritePresets = (loadedSettings.FavoritePresets != null && loadedSettings.FavoritePresets.Count > 0)
                                ? loadedSettings.FavoritePresets
                                : GetDefaultPresets();
                            this.PdfPaperPresets = (loadedSettings.PdfPaperPresets != null && loadedSettings.PdfPaperPresets.Count > 0)
                                ? loadedSettings.PdfPaperPresets
                                : GetDefaultPdfPaperPresets();
                            this.RecentWorks = (loadedSettings.RecentWorks != null && loadedSettings.RecentWorks.Count > 0)
                                ? loadedSettings.RecentWorks
                                : new List<RecentWorkItem>();
                            if (this.RecentWorks.Count == 0 && this.PaperWidth > 0 && this.PaperHeight > 0 && this.HasRecentWork)
                            {
                                this.RecentWorks.Add(new RecentWorkItem
                                {
                                    Title = !string.IsNullOrEmpty(this.LastWorkedTitle) ? this.LastWorkedTitle : "이전 작업 이어서 하기",
                                    Width = this.PaperWidth,
                                    Height = this.PaperHeight,
                                    LastModified = !string.IsNullOrEmpty(this.LastWorkedDate) ? this.LastWorkedDate : DateTime.Now.ToString("yyyy.MM.dd"),
                                    Description = $"{this.PaperWidth}×{this.PaperHeight} mm 기본 라벨"
                                });
                            }
                            this.LastPrinterName = loadedSettings.LastPrinterName ?? "";
                            this.LastPrinterPort = loadedSettings.LastPrinterPort ?? "";
                            this.LastPrinterSerial = loadedSettings.LastPrinterSerial ?? "";
                            this.RememberLastExcelMapping = loadedSettings.RememberLastExcelMapping;
                            this.LastExcelColumnMappingNames = loadedSettings.LastExcelColumnMappingNames ?? new List<string>();
                            this.LastExcelColumnIndices = loadedSettings.LastExcelColumnIndices ?? new List<int>();
                            this.ActiveColumnCount = loadedSettings.ActiveColumnCount > 0 ? loadedSettings.ActiveColumnCount : 5;

                            this.Column1Name = string.IsNullOrEmpty(loadedSettings.Column1Name) ? "컬럼1" : loadedSettings.Column1Name;
                            this.Column2Name = string.IsNullOrEmpty(loadedSettings.Column2Name) ? "컬럼2" : loadedSettings.Column2Name;
                            this.Column3Name = string.IsNullOrEmpty(loadedSettings.Column3Name) ? "컬럼3" : loadedSettings.Column3Name;
                            this.Column4Name = string.IsNullOrEmpty(loadedSettings.Column4Name) ? "컬럼4" : loadedSettings.Column4Name;
                            this.Column5Name = string.IsNullOrEmpty(loadedSettings.Column5Name) ? "컬럼5" : loadedSettings.Column5Name;
                            this.Column6Name = string.IsNullOrEmpty(loadedSettings.Column6Name) ? "컬럼6" : loadedSettings.Column6Name;
                            this.Column7Name = string.IsNullOrEmpty(loadedSettings.Column7Name) ? "컬럼7" : loadedSettings.Column7Name;
                            this.Column8Name = string.IsNullOrEmpty(loadedSettings.Column8Name) ? "컬럼8" : loadedSettings.Column8Name;

                            this.IsColumn1PriceFormat = loadedSettings.IsColumn1PriceFormat;
                            this.IsColumn2PriceFormat = loadedSettings.IsColumn2PriceFormat;
                            this.IsColumn3PriceFormat = loadedSettings.IsColumn3PriceFormat;
                            this.IsColumn4PriceFormat = loadedSettings.IsColumn4PriceFormat;
                            this.IsColumn5PriceFormat = loadedSettings.IsColumn5PriceFormat;
                            this.IsColumn6PriceFormat = loadedSettings.IsColumn6PriceFormat;
                            this.IsColumn7PriceFormat = loadedSettings.IsColumn7PriceFormat;
                            this.IsColumn8PriceFormat = loadedSettings.IsColumn8PriceFormat;

                            this.IsColumn1IncludeHeader = loadedSettings.IsColumn1IncludeHeader;
                            this.IsColumn2IncludeHeader = loadedSettings.IsColumn2IncludeHeader;
                            this.IsColumn3IncludeHeader = loadedSettings.IsColumn3IncludeHeader;
                            this.IsColumn4IncludeHeader = loadedSettings.IsColumn4IncludeHeader;
                            this.IsColumn5IncludeHeader = loadedSettings.IsColumn5IncludeHeader;
                            this.IsColumn6IncludeHeader = loadedSettings.IsColumn6IncludeHeader;
                            this.IsColumn7IncludeHeader = loadedSettings.IsColumn7IncludeHeader;
                            this.IsColumn8IncludeHeader = loadedSettings.IsColumn8IncludeHeader;

                            this.Col1Font = loadedSettings.Col1Font ?? "맑은 고딕";
                            this.Col2Font = loadedSettings.Col2Font ?? "맑은 고딕";
                            this.Col3Font = loadedSettings.Col3Font ?? "맑은 고딕";
                            this.Col4Font = loadedSettings.Col4Font ?? "맑은 고딕";
                            this.Col5Font = loadedSettings.Col5Font ?? "맑은 고딕";
                            this.Col6Font = loadedSettings.Col6Font ?? "맑은 고딕";
                            this.Col7Font = loadedSettings.Col7Font ?? "맑은 고딕";
                            this.Col8Font = loadedSettings.Col8Font ?? "맑은 고딕";

                            this.Col1Bold = loadedSettings.Col1Bold;
                            this.Col2Bold = loadedSettings.Col2Bold;
                            this.Col3Bold = loadedSettings.Col3Bold;
                            this.Col4Bold = loadedSettings.Col4Bold;
                            this.Col5Bold = loadedSettings.Col5Bold;
                            this.Col6Bold = loadedSettings.Col6Bold;
                            this.Col7Bold = loadedSettings.Col7Bold;
                            this.Col8Bold = loadedSettings.Col8Bold;

                            this.Col1Italic = loadedSettings.Col1Italic;
                            this.Col2Italic = loadedSettings.Col2Italic;
                            this.Col3Italic = loadedSettings.Col3Italic;
                            this.Col4Italic = loadedSettings.Col4Italic;
                            this.Col5Italic = loadedSettings.Col5Italic;
                            this.Col6Italic = loadedSettings.Col6Italic;
                            this.Col7Italic = loadedSettings.Col7Italic;
                            this.Col8Italic = loadedSettings.Col8Italic;

                            this.Col1Size = loadedSettings.Col1Size > 0 ? loadedSettings.Col1Size : 12;
                            this.Col2Size = loadedSettings.Col2Size > 0 ? loadedSettings.Col2Size : 12;
                            this.Col3Size = loadedSettings.Col3Size > 0 ? loadedSettings.Col3Size : 12;
                            this.Col4Size = loadedSettings.Col4Size > 0 ? loadedSettings.Col4Size : 12;
                            this.Col5Size = loadedSettings.Col5Size > 0 ? loadedSettings.Col5Size : 12;
                            this.Col6Size = loadedSettings.Col6Size > 0 ? loadedSettings.Col6Size : 12;
                            this.Col7Size = loadedSettings.Col7Size > 0 ? loadedSettings.Col7Size : 12;
                            this.Col8Size = loadedSettings.Col8Size > 0 ? loadedSettings.Col8Size : 12;

                            this.Col1OffsetY = loadedSettings.Col1OffsetY;
                            this.Col2OffsetY = loadedSettings.Col2OffsetY;
                            this.Col3OffsetY = loadedSettings.Col3OffsetY;
                            this.Col4OffsetY = loadedSettings.Col4OffsetY;
                            this.Col5OffsetY = loadedSettings.Col5OffsetY;
                            this.Col6OffsetY = loadedSettings.Col6OffsetY;
                            this.Col7OffsetY = loadedSettings.Col7OffsetY;
                            this.Col8OffsetY = loadedSettings.Col8OffsetY;

                            this.Col1OffsetX = loadedSettings.Col1OffsetX;
                            this.Col2OffsetX = loadedSettings.Col2OffsetX;
                            this.Col3OffsetX = loadedSettings.Col3OffsetX;
                            this.Col4OffsetX = loadedSettings.Col4OffsetX;
                            this.Col5OffsetX = loadedSettings.Col5OffsetX;
                            this.Col6OffsetX = loadedSettings.Col6OffsetX;
                            this.Col7OffsetX = loadedSettings.Col7OffsetX;
                            this.Col8OffsetX = loadedSettings.Col8OffsetX;

                            this.Col1Width = loadedSettings.Col1Width;
                            this.Col2Width = loadedSettings.Col2Width;
                            this.Col3Width = loadedSettings.Col3Width;
                            this.Col4Width = loadedSettings.Col4Width;
                            this.Col5Width = loadedSettings.Col5Width;
                            this.Col6Width = loadedSettings.Col6Width;
                            this.Col7Width = loadedSettings.Col7Width;
                            this.Col8Width = loadedSettings.Col8Width;

                            this.Col1RowHeight = loadedSettings.Col1RowHeight;
                            this.Col2RowHeight = loadedSettings.Col2RowHeight;
                            this.Col3RowHeight = loadedSettings.Col3RowHeight;
                            this.Col4RowHeight = loadedSettings.Col4RowHeight;
                            this.Col5RowHeight = loadedSettings.Col5RowHeight;
                            this.Col6RowHeight = loadedSettings.Col6RowHeight;
                            this.Col7RowHeight = loadedSettings.Col7RowHeight;
                            this.Col8RowHeight = loadedSettings.Col8RowHeight;

                            this.Col1Align = loadedSettings.Col1Align ?? "LEFT";
                            this.Col2Align = loadedSettings.Col2Align ?? "LEFT";
                            this.Col3Align = loadedSettings.Col3Align ?? "LEFT";
                            this.Col4Align = loadedSettings.Col4Align ?? "LEFT";
                            this.Col5Align = loadedSettings.Col5Align ?? "LEFT";
                            this.Col6Align = loadedSettings.Col6Align ?? "LEFT";
                            this.Col7Align = loadedSettings.Col7Align ?? "LEFT";
                            this.Col8Align = loadedSettings.Col8Align ?? "LEFT";

                            this.Col1HeaderAlign = loadedSettings.Col1HeaderAlign ?? "CENTER";
                            this.Col2HeaderAlign = loadedSettings.Col2HeaderAlign ?? "CENTER";
                            this.Col3HeaderAlign = loadedSettings.Col3HeaderAlign ?? "CENTER";
                            this.Col4HeaderAlign = loadedSettings.Col4HeaderAlign ?? "CENTER";
                            this.Col5HeaderAlign = loadedSettings.Col5HeaderAlign ?? "CENTER";
                            this.Col6HeaderAlign = loadedSettings.Col6HeaderAlign ?? "CENTER";
                            this.Col7HeaderAlign = loadedSettings.Col7HeaderAlign ?? "CENTER";
                            this.Col8HeaderAlign = loadedSettings.Col8HeaderAlign ?? "CENTER";

                            this.IsColumn1Barcode = loadedSettings.IsColumn1Barcode;
                            this.IsColumn2Barcode = loadedSettings.IsColumn2Barcode;
                            this.IsColumn3Barcode = loadedSettings.IsColumn3Barcode;
                            this.IsColumn4Barcode = loadedSettings.IsColumn4Barcode;
                            this.IsColumn5Barcode = loadedSettings.IsColumn5Barcode;
                            this.IsColumn6Barcode = loadedSettings.IsColumn6Barcode;
                            this.IsColumn7Barcode = loadedSettings.IsColumn7Barcode;
                            this.IsColumn8Barcode = loadedSettings.IsColumn8Barcode;

                            this.Col1BarcodeType = string.IsNullOrEmpty(loadedSettings.Col1BarcodeType) ? "CODE128" : loadedSettings.Col1BarcodeType;
                            this.Col2BarcodeType = string.IsNullOrEmpty(loadedSettings.Col2BarcodeType) ? "CODE128" : loadedSettings.Col2BarcodeType;
                            this.Col3BarcodeType = string.IsNullOrEmpty(loadedSettings.Col3BarcodeType) ? "CODE128" : loadedSettings.Col3BarcodeType;
                            this.Col4BarcodeType = string.IsNullOrEmpty(loadedSettings.Col4BarcodeType) ? "CODE128" : loadedSettings.Col4BarcodeType;
                            this.Col5BarcodeType = string.IsNullOrEmpty(loadedSettings.Col5BarcodeType) ? "CODE128" : loadedSettings.Col5BarcodeType;
                            this.Col6BarcodeType = string.IsNullOrEmpty(loadedSettings.Col6BarcodeType) ? "CODE128" : loadedSettings.Col6BarcodeType;
                            this.Col7BarcodeType = string.IsNullOrEmpty(loadedSettings.Col7BarcodeType) ? "CODE128" : loadedSettings.Col7BarcodeType;
                            this.Col8BarcodeType = string.IsNullOrEmpty(loadedSettings.Col8BarcodeType) ? "CODE128" : loadedSettings.Col8BarcodeType;

                            this.Col1ShowBarcodeText = loadedSettings.Col1ShowBarcodeText;
                            this.Col2ShowBarcodeText = loadedSettings.Col2ShowBarcodeText;
                            this.Col3ShowBarcodeText = loadedSettings.Col3ShowBarcodeText;
                            this.Col4ShowBarcodeText = loadedSettings.Col4ShowBarcodeText;
                            this.Col5ShowBarcodeText = loadedSettings.Col5ShowBarcodeText;
                            this.Col6ShowBarcodeText = loadedSettings.Col6ShowBarcodeText;
                            this.Col7ShowBarcodeText = loadedSettings.Col7ShowBarcodeText;
                            this.Col8ShowBarcodeText = loadedSettings.Col8ShowBarcodeText;

                            // 날짜/시간 설정
                            this.IsDateTimeEnabled = loadedSettings.IsDateTimeEnabled;
                            this.DateTimeFormatIndex = loadedSettings.DateTimeFormatIndex;
                            this.DateTimeFormatType = loadedSettings.DateTimeFormatType;
                            this.DateTimePrefix = loadedSettings.DateTimePrefix ?? "";
                            this.DateTimeFont = loadedSettings.DateTimeFont ?? "맑은 고딕";
                            this.DateTimeBold = loadedSettings.DateTimeBold;
                            this.DateTimeItalic = loadedSettings.DateTimeItalic;
                            this.DateTimeOffsetX = loadedSettings.DateTimeOffsetX;
                            this.DateTimeOffsetY = loadedSettings.DateTimeOffsetY > 0 ? loadedSettings.DateTimeOffsetY : 25;
                            this.DateTimeSize = loadedSettings.DateTimeSize > 0 ? loadedSettings.DateTimeSize : 10;
                            this.DateTimeWidth = loadedSettings.DateTimeWidth;

                            // 순번(Serial) 설정
                            this.IsSerialEnabled = loadedSettings.IsSerialEnabled;
                            this.SerialFormatIndex = loadedSettings.SerialFormatIndex;
                            this.SerialDirectionIndex = loadedSettings.SerialDirectionIndex;
                            this.SerialPrefix = loadedSettings.SerialPrefix ?? "No. ";
                            this.SerialSuffix = loadedSettings.SerialSuffix ?? "";
                            this.SerialFont = loadedSettings.SerialFont ?? "맑은 고딕";
                            this.SerialBold = loadedSettings.SerialBold;
                            this.SerialItalic = loadedSettings.SerialItalic;
                            this.SerialDigits = loadedSettings.SerialDigits;
                            this.SerialStartNumber = loadedSettings.SerialStartNumber;
                            this.SerialStep = loadedSettings.SerialStep > 0 ? loadedSettings.SerialStep : 1;
                            this.SerialSize = loadedSettings.SerialSize > 0 ? loadedSettings.SerialSize : 10;
                            this.SerialOffsetX = loadedSettings.SerialOffsetX;
                            this.SerialOffsetY = loadedSettings.SerialOffsetY;
                            this.SerialWidth = loadedSettings.SerialWidth;
                            this.SerialAlign = loadedSettings.SerialAlign ?? "LEFT";

                            // 표(Table) 및 제목 설정
                            this.IsTableBorderEnabled = loadedSettings.IsTableBorderEnabled;
                            this.TableBorderStyle = loadedSettings.TableBorderStyle;
                            this.TableBorderThickness = loadedSettings.TableBorderThickness;
                            this.TableBorderPadding = loadedSettings.TableBorderPadding;
                            this.TableOffsetX = loadedSettings.TableOffsetX;
                            this.TableOffsetY = loadedSettings.TableOffsetY;
                            this.TableWidth = loadedSettings.TableWidth;
                            this.TableHeight = loadedSettings.TableHeight;
                            this.IsTableTitleEnabled = loadedSettings.IsTableTitleEnabled;
                            this.TableTitleText = loadedSettings.TableTitleText ?? "라벨 정보표";
                            this.TableTitleFont = loadedSettings.TableTitleFont ?? "맑은 고딕";
                            this.TableTitleBold = loadedSettings.TableTitleBold;
                            this.TableTitleItalic = loadedSettings.TableTitleItalic;
                            this.TableTitleSize = loadedSettings.TableTitleSize > 0 ? loadedSettings.TableTitleSize : 13;
                            this.TableTitleHeight = loadedSettings.TableTitleHeight;
                            this.TableTitleAlign = loadedSettings.TableTitleAlign ?? "CENTER";
                            this.TableTitleOffsetX = loadedSettings.TableTitleOffsetX;
                            this.TableTitleOffsetY = loadedSettings.TableTitleOffsetY;
                            this.IsTableTitleUnderline = loadedSettings.IsTableTitleUnderline;
                            this.TableTitleSpacing = loadedSettings.TableTitleSpacing;
                            this.TableOrientation = loadedSettings.TableOrientation;
                            this.TableHeaderWidthMm = loadedSettings.TableHeaderWidthMm;
                            this.TableHeaderHeightMm = loadedSettings.TableHeaderHeightMm;

                            this.ZoomRatio = loadedSettings.ZoomRatio > 0 ? loadedSettings.ZoomRatio : 1.0;
                            this.PrintDarkness = loadedSettings.PrintDarkness > 0 ? loadedSettings.PrintDarkness : 8;
                            this.PrintSpeed = loadedSettings.PrintSpeed > 0 ? loadedSettings.PrintSpeed : 4;
                            this.PaperType = string.IsNullOrEmpty(loadedSettings.PaperType) ? "GAP" : loadedSettings.PaperType;
                            this.GapHeight = loadedSettings.GapHeight > 0 ? loadedSettings.GapHeight : 2;
                            this.BlineOffset = loadedSettings.BlineOffset;
                            this.IsRibbon = loadedSettings.IsRibbon;
                            this.PrintCopies = loadedSettings.PrintCopies > 0 ? loadedSettings.PrintCopies : 1;
                            this.SinglePrintCopies = loadedSettings.SinglePrintCopies > 0 ? loadedSettings.SinglePrintCopies : 1;
                            this.ExcelPrintCopies = loadedSettings.ExcelPrintCopies > 0 ? loadedSettings.ExcelPrintCopies : 1;
                            this.PrinterOffsetX = loadedSettings.PrinterOffsetX;
                            this.PrinterOffsetY = loadedSettings.PrinterOffsetY;
                            this.TearOffOffset = loadedSettings.TearOffOffset;
                            this.PostPrintAction = loadedSettings.PostPrintAction ?? "DEFAULT";
                            this.PostPrintCutTrigger = loadedSettings.PostPrintCutTrigger ?? "EVERY_PAGE";
                            this.PostPrintCutInterval = loadedSettings.PostPrintCutInterval > 0 ? loadedSettings.PostPrintCutInterval : 1;
                            this.PostPrintOffset = loadedSettings.PostPrintOffset;

                            this.IsCol1Keep = loadedSettings.IsCol1Keep;
                            this.IsCol2Keep = loadedSettings.IsCol2Keep;
                            this.IsCol3Keep = loadedSettings.IsCol3Keep;
                            this.IsCol4Keep = loadedSettings.IsCol4Keep;
                            this.IsCol5Keep = loadedSettings.IsCol5Keep;
                            this.IsCol6Keep = loadedSettings.IsCol6Keep;
                            this.IsCol7Keep = loadedSettings.IsCol7Keep;
                            this.IsCol8Keep = loadedSettings.IsCol8Keep;
                            this.PrinterConnectionType = string.IsNullOrEmpty(loadedSettings.PrinterConnectionType) ? "USB" : loadedSettings.PrinterConnectionType;
                            this.TcpPrinterIp = string.IsNullOrEmpty(loadedSettings.TcpPrinterIp) ? "192.168.0.50" : loadedSettings.TcpPrinterIp;
                            this.TcpPrinterPort = loadedSettings.TcpPrinterPort > 0 ? loadedSettings.TcpPrinterPort : 9100;
                            this.BluetoothPortName = loadedSettings.BluetoothPortName ?? "";
                            this.EnterActionType = string.IsNullOrEmpty(loadedSettings.EnterActionType) ? "PRINT_AND_RECORD" : loadedSettings.EnterActionType;
                            this.CopyCount = loadedSettings.CopyCount > 0 ? loadedSettings.CopyCount : 1;
                        }
                    }
                }
                else
                {
                    // 레거시 텍스트 포맷으로 파싱 시도 (Fallback)
                    System.Diagnostics.Debug.WriteLine("[AppSettings] 레거시 텍스트 포맷 감지, Fallback 로드 시도");
                    ParseLegacySettings(content);
                }

                if (this.FavoritePresets == null || this.FavoritePresets.Count == 0)
                {
                    this.FavoritePresets = GetDefaultPresets();
                }

                if (this.PdfPaperPresets == null || this.PdfPaperPresets.Count == 0)
                {
                    this.PdfPaperPresets = GetDefaultPdfPaperPresets();
                }

                // 구버전 기본 규격(70x28) 또는 유효하지 않은 크기는 80x60 표준으로 자동 승격
                if ((this.PaperWidth == 70 && this.PaperHeight == 28) || this.PaperWidth <= 0 || this.PaperHeight <= 0)
                {
                    this.PaperWidth = 80;
                    this.PaperHeight = 60;
                }

                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 완료: ExcelMode={IsExcelModeEnabled}, SenderReceiverLabel={SenderReceiverLabel}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 오류: {ex.Message}");
                
                // 만약 JSON 파싱 중 예상치 못한 에러 발생 시 최후의 수단으로 텍스트 파싱 한 번 더 시도
                try
                {
                    string content = File.ReadAllText(SettingsFilePath, System.Text.Encoding.UTF8);
                    ParseLegacySettings(content);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"통합 설정 Fallback 로드도 실패: {fallbackEx.Message}");
                }
            }
        }

        /// <summary>
        /// 레거시 텍스트 포맷 설정 파싱 헬퍼
        /// </summary>
        private void ParseLegacySettings(string content)
        {
            using (StringReader reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "ExcelFilePath":
                            ExcelFilePath = value;
                            break;
                        case "IsExcelModeEnabled":
                            IsExcelModeEnabled = bool.TryParse(value, out bool excelMode) && excelMode;
                            break;
                        case "LastUsedFolder":
                            LastUsedFolder = value;
                            break;
                        case "SenderReceiverLabel":
                            SenderReceiverLabel = value;
                            break;
                        case "ColumnPositionData":
                            ColumnPositionData = value;
                            break;
                        case "PaperTypeColumnPositionData":
                            PaperTypeColumnPositionData = value;
                            break;
                        case "TitleSize":
                            if (int.TryParse(value, out int titleSize))
                                TitleSize = titleSize;
                            break;
                        case "TitleOffsetY":
                            if (int.TryParse(value, out int titleOffsetY))
                                TitleOffsetY = titleOffsetY;
                            break;
                        case "TitleOffsetX":
                            if (int.TryParse(value, out int titleOffsetX))
                                TitleOffsetX = titleOffsetX;
                            break;
                        case "TitleEnabled":
                            TitleEnabled = bool.TryParse(value, out bool titleEnabled) && titleEnabled;
                            break;
                        case "PaperTypeTitleData":
                            PaperTypeTitleData = value;
                            break;
                        case "LastProductName":
                            LastProductName = value;
                            break;
                        case "LastPrice":
                            LastPrice = value;
                            break;
                        case "LastBarcode":
                            LastBarcode = value;
                            break;
                        case "LastOption":
                            LastOption = value;
                            break;
                        case "LastPrintCopies":
                            if (int.TryParse(value, out int printCopies))
                                LastPrintCopies = printCopies;
                            break;
                        case "MultiColumnSettingsData":
                            MultiColumnSettingsData = value;
                            break;
                        case "CurrentMode":
                            CurrentMode = value;
                            break;
                        case "PaperWidth":
                            if (int.TryParse(value, out int pw)) PaperWidth = pw;
                            break;
                        case "PaperHeight":
                            if (int.TryParse(value, out int ph)) PaperHeight = ph;
                            break;
                        case "BarcodeType":
                            BarcodeType = value;
                            break;
                        case "IsRotated":
                            IsRotated = bool.TryParse(value, out bool rotated) && rotated;
                            break;
                        case "LastPrinterName":
                            LastPrinterName = value;
                            break;
                        case "LastPrinterPort":
                            LastPrinterPort = value;
                            break;
                        case "LastPrinterSerial":
                            LastPrinterSerial = value;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 엑셀 파일이 유효한지 확인
        /// </summary>
        public bool IsExcelFileValid()
        {
            return !string.IsNullOrEmpty(ExcelFilePath) && File.Exists(ExcelFilePath);
        }
    }
}

