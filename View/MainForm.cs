using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Resources;
using BarcodePrint.Properties;
using System.Drawing.Drawing2D;
using MaterialSkin.Controls;
using System.Drawing.Printing;
using System.Threading;
using System.Runtime.InteropServices;
using BarcodePrint.Controll;
using BarcodePrint.Model;
using BarcodePrint.UserControls;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;

using ZXing.QrCode.Internal;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using System.Diagnostics;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using System.Management;
using DataFormats = System.Windows.Forms.DataFormats;
using DragDropEffects = System.Windows.Forms.DragDropEffects;

namespace BarcodePrint.View
{
    public partial class MainForm : Form
    {
        public const double INCH = 0.03937;
        public int MAIN_MODE = Item_Mode.MODE_EXCEL;
        private PreviewManager previewManager; // 프린터 상태 및 미리보기 
        private BarcodePrintManager barcodePrintManager;
        public MachineList printerList;
        public FontList Fontlist;
        public List<string> fontlist = new List<string>();

                    // 미리보기 이벤트 관련 (참조 프로그램 방식)
        public event EventHandler previewContentEvent;

        Queue<Row_Set> PrintDataQueue;

        Thread printStatusThread;  

        public int PreviewNum = 0;

        // 빌드시 변경해야 할 정보들 1.시리얼넘버(외부 암호화 파일로 분리됨), 2.회사링크, 3.전화번호
        string[] UserSerials = new string[0];
        //string CompanyLink = "https://printerlabel.co.kr"; // 창성제일
        string CompanyLink = "https://xprinter.co.kr"; // 
        //string CompanyLink = "https://kklabel.co.kr"; 
        bool IsRelease = true; // 배포 모드 여부
        bool shouldCloseOnLoad = false; // 로드 시 종료 여부 플래그
        public bool isDemoVersion = false; // 데모 버전 플래그 (체험판)
        private bool isSerialMatched = false; // 시리얼 매칭 성공 여부

        // 바코드 미리보기 관련 변수들 (참조 프로그램 방식)
        private string currentBarcodeValue = "";
        public string currentBarcodeText = ""; // 바코드 아래 표시할 텍스트 (엑셀 연동용)
        private int barcodeSize = 50;
        private int barcodeX = 0;
        private int barcodeY = 0;
        private int barcodeHeight = 15;
        public bool showBarcodeText = true; // 바코드 값 표시 여부
        private bool isAutoClearingTextBox = false; // TextBox 자동 지우기 플래그
        private bool isUpdatingExcelMode = false; // 엑셀 모드 업데이트 중 플래그
        private AppSettings appSettings = new AppSettings(); // 통합 설정 관리
        private bool isPreviewEventProcessing = false; // 미리보기 이벤트 처리 중 플래그 (무한 루프 방지)
        
        // 미리보기 확대/축소 관련 변수들
        private float previewZoomFactor = 1.0f; // 확대 비율 (1.0 = 100%)
        private Bitmap originalPreviewBitmap = null; // 원본 미리보기 이미지
        
        // 엑셀 검색 중복 실행 방지 플래그
        private bool isSearchingExcel = false;
        
        // 포커스 자동 이동 관련 변수들
        private System.Threading.Timer focusTimer;
        private bool isFocusTimerActive = false;
        
        // 엑셀 연동 관련 변수들
        private AdjustForm adjustForm = null;
        private LabelLayoutSettingsForm layoutSettingsForm = null;
        private string excelFilePath = "";
        private DataTable excelData = null;
        private bool isExcelMode = false;
        private int selectedBarcodeColumn = 0; // 바코드로 사용할 컬럼 인덱스 (기존 호환성)
        private int selectedTextColumn = 1; // 출력할 텍스트 컬럼 인덱스 (기존 호환성)
        
        // 다중 컬럼 설정 관련 변수들
        private List<ExcelMultiColumnSelectionForm.ColumnSetting> multiColumnSettings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();
        private bool useMultiColumnMode = false; // 다중 컬럼 모드 사용 여부
        private bool isInitialized = false; // 초기화 완료 여부 플래그
        
        // 컬럼 위치 설정 관련 변수들
        private List<ColumnPositionSetting> columnPositionSettings = new List<ColumnPositionSetting>();
        // 컬럼 위치 설정은 AppSettings에 통합됨 (별도 파일 경로 제거)
        
        // 컬럼 위치 설정 가져오기 (외부에서 접근용)
        public List<ColumnPositionSetting> GetColumnPositionSettings()
        {
            // 저장된 값을 그대로 반환 (mm 단위)
            return columnPositionSettings;
        }
        
        // Title 크기 가져오기 (외부에서 접근용)
        public int GetTitleSize()
        {
            return appSettings.TitleSize;
        }
        
        // Title Y축 오프셋 가져오기 (외부에서 접근용)
        public int GetTitleOffsetY()
        {
            return appSettings.TitleOffsetY;
        }
        
        // AppSettings 가져오기 (외부에서 접근용)
        public AppSettings GetAppSettings()
        {
            return appSettings;
        }
        
        /// <summary>
        /// Title 크기 설정
        /// </summary>
        public void SetTitleSize(int size)
        {
            appSettings.TitleSize = size;
            appSettings.Save();
            OnPreviewEvent();
        }
        
        /// <summary>
        /// Title Y축 오프셋 설정
        /// </summary>
        public void SetTitleOffsetY(int offsetY)
        {
            appSettings.TitleOffsetY = offsetY;
            appSettings.Save();
            OnPreviewEvent();
        }

        /// <summary>
        /// 현재 입력된 텍스트박스 값들을 미리보기용으로 가져옴
        /// </summary>
        public List<string> GetPreviewTexts()
        {
            return new List<string>
            {
                GetTextBoxValue(TextBox_InputText4),   // 인덱스 0: 컬럼1
                GetTextBoxValue(TextBox_InputText),    // 인덱스 1: 컬럼2
                GetTextBoxValue(TextBox_InputText3),   // 인덱스 2: 컬럼3
                GetTextBoxValue(TextBox_InputText2),   // 인덱스 3: 바코드
                GetSenderReceiverLabel()               // 인덱스 4: 공통문구 (추가)
            };
        }

        /// <summary>
        /// 컬럼 위치 설정 업데이트 및 저장 (에디터 실시간 미리보기용 플래그 추가)
        /// </summary>
        public void UpdateColumnPositionSettings(List<ColumnPositionSetting> settings, bool save = true)
        {
            if (settings == null) return;
            this.columnPositionSettings = settings;
            
            SyncBarcodeFieldsFromSettings();
            
            if (columnPositionSettings != null && columnPositionSettings.Count > 4)
            {
                if (touchSwitch_Title != null)
                {
                    touchSwitch_Title.Checked = columnPositionSettings[4].IsEnabled;
                    textBox_Title.Enabled = touchSwitch_Title.Checked;
                }
                appSettings.TitleEnabled = columnPositionSettings[4].IsEnabled;
            }
            
            if (save)
            {
                SaveColumnPositionSettings();
                SaveAppSettings();
            }
            
            OnPreviewEvent();
        }

        private void SyncBarcodeFieldsFromSettings()
        {
            if (columnPositionSettings != null)
            {
                var barcodeSetting = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == 3);
                if (barcodeSetting != null)
                {
                    this.barcodeSize = barcodeSetting.Size;
                    this.barcodeX = barcodeSetting.OffsetX;
                    this.barcodeY = barcodeSetting.OffsetY;
                    this.barcodeHeight = barcodeSetting.BarcodeHeight;
                    this.showBarcodeText = barcodeSetting.ShowBarcodeText;
                    System.Diagnostics.Debug.WriteLine($"[SyncBarcodeFields] 동기화 완료: Size={barcodeSize}, X={barcodeX}, Y={barcodeY}, Height={barcodeHeight}, ShowText={showBarcodeText}");
                }
            }
        }

        /// <summary>
        /// 기본 컬럼 위치 설정 가져오기 (리셋용 - 가격표용 70x28mm 기준)
        /// </summary>
        public List<ColumnPositionSetting> GetDefaultColumnPositionSettings()
        {
            var defaults = new List<ColumnPositionSetting>();
            
            // 사용자 입력값 기반 용지 세로 크기(mm) 및 가로 크기(mm) 동적 확인
            float pHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height > 0 ? (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height : 60f; // 기본값 60mm
            float pWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width > 0 ? (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width : 80f;  // 기본값 80mm
            
            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
            {
                float temp = pHeight;
                pHeight = pWidth;
                pWidth = temp;
            }

            // 사용자 입력 용지 높이에 비례한 글자 및 바코드 크기 동적 계산
            int fontSize = Math.Max(6, (int)(pHeight * 0.3f));       // 예: 28mm -> 8pt, 50mm -> 15pt
            int barcodeSize = Math.Max(12, (int)(pHeight * 0.9f));   // 예: 28mm -> 25, 50mm -> 45
            int barcodeHeight = Math.Max(5, (int)(pHeight * 0.5f));  // 바코드 높이 비례
            
            // 사용자 입력 용지 높이에 비례한 각 항목 Y축 오프셋(세로 좌표 mm) 계산
            int commonTextY = Math.Max(1, (int)(pHeight * 0.08f));  // 공통문구 (최상단)
            int titleY = Math.Max(3, (int)(pHeight * 0.20f));       // 품명
            int priceY = Math.Max(5, (int)(pHeight * 0.42f));       // 가격
            int barcodeY = Math.Max(5, (int)(pHeight * 0.42f));     // 바코드
            int optionY = Math.Max(7, (int)(pHeight * 0.82f));      // 비고 (최하단)

            // 가격표용 동적 컬럼 추가 (용지 크기 자동 조절 적용)
            // 컬럼 0: 품명
            defaults.Add(new ColumnPositionSetting { ColumnIndex = 0, IsEnabled = true, Size = fontSize, OffsetX = 0, OffsetY = titleY, UsageType = "텍스트", FontFamily = "맑은 고딕", IsBold = true });
            // 컬럼 1: 가격
            defaults.Add(new ColumnPositionSetting { ColumnIndex = 1, IsEnabled = true, Size = fontSize, OffsetX = 0, OffsetY = priceY, UsageType = "텍스트", FontFamily = "맑은 고딕", IsBold = true });
            // 컬럼 2: 비고 (옵션)
            defaults.Add(new ColumnPositionSetting { ColumnIndex = 2, IsEnabled = true, Size = fontSize, OffsetX = 0, OffsetY = optionY, UsageType = "텍스트", FontFamily = "맑은 고딕", IsBold = true });
            // 컬럼 3: 바코드
            defaults.Add(new ColumnPositionSetting { ColumnIndex = 3, IsEnabled = true, Size = barcodeSize, BarcodeHeight = barcodeHeight, OffsetX = 0, OffsetY = barcodeY, UsageType = "바코드", BarcodeType = "QR_CODE", ShowBarcodeText = false, FontFamily = "맑은 고딕", IsBold = false });
            // 컬럼 4: 공통문구 (타이틀)
            defaults.Add(new ColumnPositionSetting { ColumnIndex = 4, IsEnabled = true, Size = (int)(fontSize * 1.1), OffsetX = 0, OffsetY = commonTextY, UsageType = "텍스트", FontFamily = "맑은 고딕", IsBold = true });

            return defaults;
        }

        /// <summary>
        /// 엑셀 배치 출력 모드인지 확인 (BarcodePrintManager에서 호출)
        /// </summary>
        public bool IsExcelBatchMode()
        {
            return isExcelMode && excelData != null;
        }

        /// <summary>
        /// 현재 사용 중인 바코드 타입을 가져옴 (다중 컬럼 모드에서는 첫 번째 바코드 컬럼의 타입 사용)
        /// </summary>
        public string GetCurrentBarcodeType()
        {
            string result;
            
            if (useMultiColumnMode && multiColumnSettings != null && multiColumnSettings.Count > 0)
            {
                // 다중 컬럼 모드: 첫 번째 바코드 컬럼의 타입 사용
                var firstBarcodeSetting = multiColumnSettings.FirstOrDefault(s => s.IsEnabled && s.UsageType == "바코드");
                if (firstBarcodeSetting != null && !string.IsNullOrEmpty(firstBarcodeSetting.BarcodeType))
                {
                    result = firstBarcodeSetting.BarcodeType;
                    System.Diagnostics.Debug.WriteLine($"[GetCurrentBarcodeType] 다중 컬럼 모드: {result} (컬럼 {firstBarcodeSetting.ColumnIndex})");
                    return result;
                }
            }
            
            // 단일 컬럼 모드 또는 바코드 컬럼이 없는 경우 columnPositionSettings 또는 PRINT_INFO_CLASS 사용
            // columnPositionSettings에서 바코드 컬럼 찾기
            if (columnPositionSettings != null && columnPositionSettings.Count > 0)
            {
                var barcodeColumn = columnPositionSettings.FirstOrDefault(s => s.UsageType == "바코드");
                if (barcodeColumn != null && !string.IsNullOrEmpty(barcodeColumn.BarcodeType))
                {
                    result = barcodeColumn.BarcodeType;
                    System.Diagnostics.Debug.WriteLine($"[GetCurrentBarcodeType] columnPositionSettings: {result} (컬럼 {barcodeColumn.ColumnIndex})");
                    return result;
                }
            }
            
            // 마지막으로 PRINT_INFO_CLASS 사용
            result = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
            System.Diagnostics.Debug.WriteLine($"[GetCurrentBarcodeType] PRINT_INFO_CLASS: {result}");
            return result;
        }
        
        private string currentBarcodeType = "QR_CODE"; // 현재 바코드 타입
        private TextBox lastFocusedPageTextBox = null; // 마지막으로 포커스를 가졌던 페이지 텍스트박스
        
        // 미리보기 크기는 디자이너에서 고정 설정됨

        public MainForm()
        {
            InitializeComponent();
            InitPictureButton();

            // 포커스와 무관하게 바코드 스캐너 입력을 받기 위한 설정
            this.KeyPreview = true;

            this.CenterToScreen();

            string programName = "LabelMaster";
            string version = " Ver 1.2 (20260406)";
            this.Text = programName + "(" + version + ")";
            label_companyLink.Text = CompanyLink;

            barcodePrintManager = new BarcodePrintManager(this);
            previewManager = new PreviewManager(this);
            printerList = new MachineList();
            Fontlist = new FontList();

            FileLoad_Excel_data();

            // ★ 중요: setControl()을 가장 먼저 호출하여 PRINT_INFO_CLASS 설정 로드
            setControl(); // PRINT_INFO_CLASS 로드 (바코드 타입, 용지 사이즈 등)

            // 엑셀 설정 로드 및 적용
            LoadAppSettings();

            // 컬럼 위치 설정 로드
            LoadColumnPositionSettings();

            // ★ 중요: 바코드 타입 적용
            ApplyBarcodeTypeFromSettings();

            // useMultiColumnMode 초기화
            if (excelData == null || excelData.Rows.Count == 0)
            {
                useMultiColumnMode = false;
            }

            // 프로그램 시작 시 파일명 라벨 초기화 및 패널 상태 업데이트
            UpdateExcelPanelState();

            SetReceiveFunction();// 컨트롤 앞에서 실행해야 할 이벤트

            // 텍스트박스 포커스 추적을 위한 이벤트 추가
            txtCurrentPage.Enter += (s, ev) => lastFocusedPageTextBox = txtCurrentPage;
            txtEndPage.Enter += (s, ev) => lastFocusedPageTextBox = txtEndPage;
            lastFocusedPageTextBox = txtCurrentPage; // 기본값

            // 미리보기 패널에 마우스 휠 이벤트 추가
            panel_BarcodePreview.MouseWheel += Panel_BarcodePreview_MouseWheel;
            panel_BarcodePreview.MouseEnter += Panel_BarcodePreview_MouseEnter;

            // 드래그 앤 드롭으로 엑셀 파일 열기 기능 추가
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            // 폼 종료 시 설정 저장
            this.FormClosing += MainForm_FormClosing;

            // setControl 이후 패널 상태 재설정
            UpdateExcelPanelState();

            // 트랙바 초기값 설정
            InitializeTrackBars();

            // 프린터 초기화는 InitializeSelectedPrinterAsync에서 비동기로 처리됨
            UpdateLabelSizeInfo();

            // 설정 초기화 완료 설정
            isInitialized = true;
        }


        /// <summary>
        /// 애플리케이션 종료 전 정리 작업
        /// </summary>
        private void CleanupBeforeExit()
        {
            try
            {
                // 1. 상태 체크 스레드 종료 대기
                if (printStatusThread != null && printStatusThread.IsAlive)
                {
                    previewManager.RequestStop();
                    int timeout = 0;
                    while (printStatusThread.IsAlive && timeout < 50) // 최대 5초 대기
                    {
                        printStatusThread.Interrupt();
                        Thread.Sleep(100);
                        timeout++;
                    }

                    // 강제 종료
                    if (printStatusThread.IsAlive)
                    {
                        try
                        {
                            printStatusThread.Abort();
                        }
                        catch { }
                    }
                }

                // 2. 인쇄 큐 정리
                if (barcodePrintManager != null)
                {
                    barcodePrintManager.ClearPrintQueue(PRINT_INFO_CLASS.PrinterInfo.printerName);
                }

                // 3. 포커스 타이머 정리
                if (focusTimer != null)
                {
                    focusTimer.Dispose();
                    focusTimer = null;
                }

                Unsubscribe_Event();
                printStatusThread = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CleanupBeforeExit 오류: {ex.Message}");
            }
        }
                     
       

        private void SetReceiveFunction()
        {
            this.printerList.machineListChanged += ReceiveEventPrinterListUpdate;
            this.previewManager.PrintStateUpdateEvent += ReceiveEventPrinterStateUpdate;
            this.Fontlist.FontListUpdated += ReceiveEventFontListUpdate;
            
            // TextBox 실시간 입력 이벤트 추가
            this.TextBox_InputText.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_InputText2.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_InputText3.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_InputText4.TextChanged += TextBox_InputText_TextChanged;
            // TextBox_InputText5는 명찰용으로 제거됨 (4개 컬럼만 사용)
            // this.TextBox_InputText5.TextChanged += TextBox_InputText_TextChanged; // 전화번호
            
            // 포커스 자동 이동 이벤트 추가
            SetupFocusAutoMoveEvents();
            
            // 미리보기 이벤트 설정 (참조 프로그램 방식)
            previewManager.PaintPreviewEvent += ReceivePaint;
            previewManager.PreviewEventSet();
        }

        /// <summary>
        /// 트랙바 초기값 설정 (트랙바는 ColumnSettingsForm으로 이동됨)
        /// </summary>
        private void InitializeTrackBars()
        {
            try
            {
                // 라벨들은 제거됨 (ColumnSettingsForm으로 이동)
                // label_BarcodeSize.Text = $"크기 : {barcodeSize}";
                // label_BarcodeX.Text = $"X 위치 : {barcodeX}";
                // label_BarcodeY.Text = $"Y 위치 : {-barcodeY}";
                
                System.Diagnostics.Debug.WriteLine($"라벨 초기화 완료: Size={barcodeSize}, X={barcodeX}, Y={barcodeY}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"라벨 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 포커스 자동 이동 이벤트 설정
        /// </summary>
        private void SetupFocusAutoMoveEvents()
        {
            // 이벤트 등록은 각 이벤트 핸들러에서 OnControlStateChanged 호출로 처리
            // 중복 등록을 방지하기 위해 여기서는 별도 등록하지 않음
        }

        /// <summary>
        /// 컨트롤 상태 변경 시 포커스 자동 이동 처리 (비활성화)
        /// </summary>
        private void OnControlStateChanged(object sender, EventArgs e)
        {
            // 자동 포커스 기능 비활성화
            return;
            
            // 아래 코드는 비활성화됨 (필요시 주석 해제)
            /*
            // 종료 버튼은 제외
            if (sender == customCloseButton)
                return;
                
            // 이미 타이머가 활성화되어 있으면 리셋
            if (isFocusTimerActive)
            {
                focusTimer?.Dispose();
            }
            
            // 1초 후 포커스 이동 타이머 시작
            focusTimer = new System.Threading.Timer(FocusToTextBox, null, 1000, Timeout.Infinite);
            isFocusTimerActive = true;
            */
        }

        
                
        private void setControl()
        {
            try
            {
                // Paper Type 설정 불러오기
                string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                if (System.IO.File.Exists(settingsPath))
                {
                    PRINT_INFO_CLASS.LoadFromFile(settingsPath);
                    System.Diagnostics.Debug.WriteLine($"[setControl 불러오기] PaperType={PRINT_INFO_CLASS.PrinterInfo.PaperType}, SelectedPaperType={PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType}, Width={PRINT_INFO_CLASS.PrinterInfo.Paper_Width}, Height={PRINT_INFO_CLASS.PrinterInfo.Paper_Height}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[setControl] 설정 파일이 없습니다. 기본값 사용: {settingsPath}");
                }
                
                Fontlist.getFontList();
                printerList.getMachineList();
                autoSetPrinter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"setControl 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        private void autoSetPrinter()
        {

            // 안전한 배열 접근을 위한 크기 확인
            if (USB_status.Connect != null && USB_status.Name != null &&
                USB_status.Port != null && USB_status.DPI != null)
            {
                int minCount = Math.Min(USB_status.Connect.Count,
                                       Math.Min(USB_status.Name.Count,
                                               Math.Min(USB_status.Port.Count, USB_status.DPI.Count)));

                System.Diagnostics.Debug.WriteLine($"setControl - 프린터 검색 시작, 총 {minCount}개 프린터 확인");

                // [추가] 마지막으로 성공했던 프린터 정보가 저장되어 있다면, 해당 프린터를 우선 매칭
                if (appSettings != null && !string.IsNullOrEmpty(appSettings.LastPrinterName))
                {
                    for (int i = 0; i < minCount; i++)
                    {
                        try
                        {
                            if (USB_status.Connect[i] && USB_status.Name[i] == appSettings.LastPrinterName)
                            {
                                PRINT_INFO_CLASS.PrinterInfo.printerName = USB_status.Name[i];
                                PRINT_INFO_CLASS.PrinterInfo.printerPortName = USB_status.Port[i];
                                PRINT_INFO_CLASS.PrinterInfo.printer_DPI = USB_status.DPI[i];

                                System.Diagnostics.Debug.WriteLine($"setControl - [복구 연결] 저장된 마지막 프린터 선택: {USB_status.Name[i]} (포트: {USB_status.Port[i]}, DPI: {USB_status.DPI[i]})");

                                MachineList_selectChanged(USB_status.Name[i]);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"setControl - 복구 프린터 매칭 중 오류 (인덱스 {i}): {ex.Message}");
                        }
                    }
                }

                // 첫 번째로 연결된 Xprinter 프린터 찾기
                for (int i = 0; i < minCount; i++)
                {
                    try
                    {
                        if (USB_status.Connect[i] && (USB_status.Name[i].IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      USB_status.Name[i].IndexOf("XP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      USB_status.Name[i].IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      BarcodePrint.Model.UsbPrinterHelper.supportedPrinterModels.Any(model => USB_status.Name[i].IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0)))
                        {
                            PRINT_INFO_CLASS.PrinterInfo.printerName = USB_status.Name[i];
                            PRINT_INFO_CLASS.PrinterInfo.printerPortName = USB_status.Port[i];
                            PRINT_INFO_CLASS.PrinterInfo.printer_DPI = USB_status.DPI[i];

                            System.Diagnostics.Debug.WriteLine($"setControl - 프린터 선택: {USB_status.Name[i]} (포트: {USB_status.Port[i]}, DPI: {USB_status.DPI[i]})");

                            System.Diagnostics.Debug.WriteLine("MachineList_selectChanged : 1");
                            // 프린터 연결 정보 업데이트
                            MachineList_selectChanged(USB_status.Name[i]);
                            
                            // DPI 값 확인
                            System.Diagnostics.Debug.WriteLine($"[setControl] 최종 설정된 프린터 DPI: {PRINT_INFO_CLASS.PrinterInfo.printer_DPI}");
                            
                            return; // 첫 번째 연결된 프린터를 찾으면 종료
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"setControl - 프린터 정보 처리 중 오류 (인덱스 {i}): {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("setControl - 연결된 Xprinter 프린터를 찾을 수 없습니다.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("setControl - USB_status 정보가 초기화되지 않았습니다.");
            }

        }

        private void Unsubscribe_Event()
        {
            this.printerList.machineListChanged -= ReceiveEventPrinterListUpdate;
            this.previewManager.PrintStateUpdateEvent -= ReceiveEventPrinterStateUpdate;
            this.Fontlist.FontListUpdated -= ReceiveEventFontListUpdate;
            this.TextBox_InputText.TextChanged -= TextBox_InputText_TextChanged;
            
            // 미리보기 이벤트 해제 (참조 프로그램 방식)
            previewManager.PaintPreviewEvent -= ReceivePaint;
            previewManager.PreviewEventClose();
            //this.recv.RecvThreadEvent -= RecvDataUpdate;
        }

         
        private void MainForm_Load(object sender, EventArgs e)
        {
            // 단건 출력 텍스트박스 줄바꿈 속성 설정
            TextBox_InputText.Multiline = true;
            TextBox_InputText.AcceptsReturn = true;
            TextBox_InputText3.Multiline = true;
            TextBox_InputText3.AcceptsReturn = true;
            TextBox_InputText4.Multiline = true;
            TextBox_InputText4.AcceptsReturn = true;

            // 체험판 안내 메시지 출력
            if (isDemoVersion)
            {
                MessageBox.Show("본 프로그램은 체험판(Demo) 버전입니다.\n엑셀 데이터 로드 시 최대 5행으로 기능이 제한됩니다.", 
                    "체험판 안내", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ConfigureMaterialSkin();

            // 화면 비율 깨짐 방지: 폼 크기와 위치를 명시적으로 설정
            FixFormSizeAndPosition();

            // Anchor/Dock으로 인한 위치 오차 방지: 모든 컨트롤 위치 명시적으로 설정
            FixControlsSizeAndPosition();

            // [중요] 비동기 작업(프린터 초기화) 전에 화면 모드부터 먼저 설정 (사용자 체감 지연 개선)
            if (isExcelMode)
            {
                menuItem_Manual.IsSelected = false;
                menuItem_Excel.IsSelected = true;
                SwitchToExcelMode();
            }
            else
            {
                menuItem_Manual.IsSelected = true;
                menuItem_Excel.IsSelected = false;
                SwitchToManualMode();
            }

            // [알림] 비동기 프린터 초기화와 인증 체크는 MainForm_Shown에서 수행됨 (부팅 속도 최적화)
        }

        /// <summary>
        /// 폼이 완전히 표시된 후(Visible) 실행되는 이벤트 (부팅 체감 속도 향상)
        /// </summary>
        private async void MainForm_Shown(object sender, EventArgs e)
        {
            // [1] 레이아웃 초기화
            FixFormSizeAndPosition();
            FixControlsSizeAndPosition();

            // [2] 백그라운드 환경에서 프린터 식별 및 시리얼 번호 인증 수행
            await InitializeSelectedPrinterAsync();

            // [2] 시리얼 번호 및 DLL 인증 모두 실패 시 메시지 표시 후 종료
            if (shouldCloseOnLoad)
            {
                string serial = PRINT_INFO_CLASS.PrinterInfo.PrinterSerial;

                System.Windows.Forms.MessageBox.Show(
                    "프린터 인증에 실패했거나 시리얼 번호가 일치하지 않습니다.\n\n" +
                    "프린터 시리얼 넘버 : " + (string.IsNullOrEmpty(serial) ? "미확인" : serial) + "\n" +
                    "구매처에 문의하시기 바랍니다.\n-한국Xprinter-",
                    "라벨마스터",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                this.Close(); // 인증 실패 시 폼 종료
            }

            // 인쇄매수 컨트롤을 최상단에 표시
            if (numericUpDown_PrintCopies != null)
            {
                numericUpDown_PrintCopies.BringToFront();
                numericUpDown_PrintCopies.Invalidate();
            }

            // 초기 미리보기 트리거
            OnPreviewEvent();
        }
        
        /// <summary>
        /// 로고 이미지 로드
        /// </summary>
        private void LoadLogoImage()
        {
            try
            {
                string logoPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "resource", "LabelMaster_Logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    pictureBox_Logo.Image = System.Drawing.Image.FromFile(logoPath);
                    System.Diagnostics.Debug.WriteLine($"[로고 이미지 로드 성공] {logoPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[로고 이미지 없음] {logoPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[로고 이미지 로드 오류] {ex.Message}");
            }
        }
        
        /// <summary>
        /// 화면 비율 깨짐 방지: 폼 크기와 위치를 명시적으로 고정
        /// </summary>
        private void FixFormSizeAndPosition()
        {
            try
            {
                // Windows가 저장한 폼 크기/위치 무시하고 명시적으로 설정
                this.Size = new Size(950, 600);
                this.ClientSize = new Size(950, 600);
                this.MaximumSize = new Size(950, 600);
                this.MinimumSize = new Size(950, 600);
                
                // 화면 중앙에 위치 (StartPosition이 CenterScreen이지만 명시적으로 설정)
                this.StartPosition = FormStartPosition.Manual;
                Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
                int x = (screenBounds.Width - this.Width) / 2;
                int y = (screenBounds.Height - this.Height) / 2;
                this.Location = new Point(x, y);
                
                // DPI 스케일링 문제 방지: AutoScaleMode.None으로 설정하여
                // 모든 환경에서 동일한 크기로 고정
                
                System.Diagnostics.Debug.WriteLine($"[화면비율 수정] 폼 크기: {this.Size}, 위치: {this.Location}, DPI: {this.DeviceDpi}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[화면비율 수정 오류] {ex.Message}");
            }
        }
        
        /// <summary>
        /// 모든 컨트롤의 Anchor/Dock 속성 무시하고 원래 위치로 복원
        /// </summary>
        private void FixControlsSizeAndPosition()
        {
            try
            {
                // panel1 (하단 패널): Dock을 제거하고 명시적 위치 설정
                if (panel1 != null)
                {
                    panel1.Dock = DockStyle.None;
                    panel1.Location = new Point(0, 570);
                    panel1.Size = new Size(950, 30);
                }

                // 오른쪽 버튼 영역 간격 및 배치 조절 (균형감 있는 배치)
                int rightX = 736;
                int startY = 85;
                int gap = 15;
                int blueGap = 8; // 파란색 버튼끼리의 더 좁은 간격

                // 1. 인쇄매수 (NumericUpDown)
                if (numericUpDown_PrintCopies != null)
                {
                    numericUpDown_PrintCopies.Location = new Point(rightX - 20, startY);
                    numericUpDown_PrintCopies.Size = new Size(205, 50);
                }

                // 2. 저장 / 열기 버튼
                int projectY = startY + 50 + gap;
                if (panel_SaveProject != null)
                {
                    panel_SaveProject.Location = new Point(rightX, projectY);
                    panel_SaveProject.Size = new Size(80, 48);
                }
                if (panel_OpenProject != null)
                {
                    panel_OpenProject.Location = new Point(rightX + 85, projectY);
                    panel_OpenProject.Size = new Size(80, 48);
                }

                // 3. HOME 버튼 (파란색 버튼 그룹의 시작으로 취급)
                int groupStartGap = 25; // 상단 버튼들과의 분리 간격
                int homeY = projectY + 48 + groupStartGap;
                if (panel_Home != null)
                {
                    panel_Home.Location = new Point(rightX, homeY);
                    panel_Home.Size = new Size(167, 48);
                }

                // 4. 레이아웃 버튼 (HOME과 파란색 버튼 사이를 좁게 조정)
                int layoutY = homeY + 48 + blueGap;
                if (panel_OpenLayoutForm != null)
                {
                    panel_OpenLayoutForm.Location = new Point(rightX, layoutY);
                    panel_OpenLayoutForm.Size = new Size(165, 48);
                }

                // 5. 상세설정 버튼 (파란색 버튼끼리 가깝게)
                int adjustY = layoutY + 48 + blueGap;
                if (panel_OpenAdjustForm != null)
                {
                    panel_OpenAdjustForm.Location = new Point(rightX, adjustY);
                    panel_OpenAdjustForm.Size = new Size(165, 48);
                }

                // 6. 인쇄 / 엑셀 출력 버튼 (파란색 버튼끼리 가깝게)
                int printY = adjustY + 48 + blueGap;
                if (panel_PrintButton != null)
                {
                    panel_PrintButton.Location = new Point(rightX, printY);
                    panel_PrintButton.Size = new Size(165, 140);
                }
                
                System.Diagnostics.Debug.WriteLine("[컨트롤 위치 수정] 우측 버튼 영역 배치 최적화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[컨트롤 위치 수정 오류] {ex.Message}");
            }
        }
         
        /// <summary>
        /// 직접 출력 메뉴 클릭
        /// </summary>
        private void menuItem_Manual_Click(object sender, EventArgs e)
        {
            menuItem_Manual.IsSelected = true;
            menuItem_Excel.IsSelected = false;
            menuItem_Manual.BringToFront(); // 직접출력 버튼을 위로
            SwitchToManualMode();
        }

        /// <summary>
        /// 엑셀 출력 메뉴 클릭
        /// </summary>
        private void menuItem_Excel_Click(object sender, EventArgs e)
        {
            menuItem_Manual.IsSelected = false;
            menuItem_Excel.IsSelected = true;
            menuItem_Excel.BringToFront(); // 엑셀출력 버튼을 위로
            SwitchToExcelMode();
        }

        /// <summary>
        /// 직접 출력 모드로 전환
        /// </summary>
        private void SwitchToManualMode()
        {
            try
            {
                // 왼쪽: 수동 입력 컨트롤 표시
                TextBox_InputText.Visible = true;
                TextBox_InputText2.Visible = true;
                TextBox_InputText3.Visible = true;
                TextBox_InputText4.Visible = true;
                // TextBox_InputText5는 명찰용으로 제거됨 (4개 컬럼만 사용)
                // TextBox_InputText5.Visible = false; // 5번째 컬럼 숨김 (명찰용 4개만 사용)
                label_Column1.Visible = true;
                // label_Column2는 제거됨, label_Column5가 바코드 라벨로 사용됨
                label_Column3.Visible = true;
                label_Column4.Visible = true;
                label_Column5.Visible = true; // 바코드 라벨 표시
                
                // 라벨 텍스트 복원 (엑셀 모드에서 변경되었을 수 있음)
                RestoreManualModeLabels();

                // 왼쪽: 엑셀 관련 컨트롤 숨김
                panel_OpenExcelForm.Visible = false;
                label_ExcelFileName.Visible = false;
                
                // 엑셀 모드 비활성화 (미리보기 분기에 영향)
                isExcelMode = false;
                useMultiColumnMode = false; // 직접 출력 모드에서는 엑셀 다중 컬럼 모드 비활성화
                panel_OpenExcelForm.Enabled = false;
                
                // 직접 출력 버튼을 위로
                menuItem_Manual.BringToFront();

                // 오른쪽: 미리보기와 인쇄 버튼 표시 (하나의 버튼만 사용)
                panel_BarcodePreview.Visible = true;
                panel_PrintButton.Visible = true;
                panel_PrintButton.Enabled = isSerialMatched;
                
                // 버튼 텍스트 업데이트
                panel_PrintButton.Invalidate();

                System.Diagnostics.Debug.WriteLine("직접 출력 모드로 전환 - isExcelMode=false, useMultiColumnMode=false, 버튼 비활성화");
                
                // TextBox 값 기반으로 미리보기 갱신
                RefreshPreviewFromTextboxes();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"직접 출력 모드 전환 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 직접 출력 모드의 라벨 텍스트 복원
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 컬럼3, 전화번호
        /// </summary>
        private void RestoreManualModeLabels()
        {
            try
            {
                // 가격표용 라벨 텍스트 (바코드와 비고 순서 변경)
                label_Column4.Text = "컬럼1";     // Y=190 (가장 위) - 컬럼 0
                label_Column1.Text = "컬럼2";     // Y=260 - 컬럼 1
                label_Column3.Text = "컬럼3";       // Y=320 - 컬럼 2 (위치 변경)
                label_Column5.Text = "컬럼4";     // Y=485 - 컬럼 3 (위치 변경)
                label_Column5.Visible = true;      // 바코드 라벨 표시
                
                System.Diagnostics.Debug.WriteLine("[라벨 복원] 직접 출력 모드 라벨 텍스트 복원 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"라벨 텍스트 복원 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// TextBox 값들을 기반으로 미리보기를 갱신
        /// </summary>
        private void RefreshPreviewFromTextboxes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RefreshPreviewFromTextboxes 시작");
                
                // 공통문구만 입력되어 있어도 미리보기를 업데이트해야 함
                // 항상 미리보기 이벤트 트리거
                OnPreviewEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshPreviewFromTextboxes 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 엑셀 출력 모드로 전환
        /// </summary>
        private void SwitchToExcelMode()
        {
            try
            {
                // 왼쪽: 수동 입력 컨트롤 숨김 (전화번호 포함)
                TextBox_InputText.Visible = false;
                TextBox_InputText2.Visible = false;
                TextBox_InputText3.Visible = false;
                TextBox_InputText4.Visible = false;
                // TextBox_InputText5는 명찰용으로 제거됨
                // TextBox_InputText5.Visible = false; // 전화번호 숨김
                label_Column1.Visible = false;
                label_Column5.Visible = false;
                label_Column3.Visible = false;
                label_Column4.Visible = false;
                label_Column5.Visible = false; // 전화번호 라벨 숨김

                // 왼쪽: 엑셀 관련 컨트롤 표시
                panel_OpenExcelForm.Visible = true;

                // 엑셀 모드 자동 활성화
                isExcelMode = true;
                panel_OpenExcelForm.Enabled = true;
                
                // 엑셀 출력 버튼을 위로
                menuItem_Excel.BringToFront();
                
                // 엑셀 버튼 다시 그리기
                new_OpenExcelFormButton(1f);
                // 버튼 텍스트 업데이트 (하나의 버튼만 사용)
                panel_PrintButton.Invalidate();

                // 엑셀 데이터가 있고 다중 컬럼 설정이 있으면 다중 컬럼 모드 복원
                if (excelData != null && excelData.Rows.Count > 0 && multiColumnSettings != null && multiColumnSettings.Count > 0)
                {
                    useMultiColumnMode = true;
                    System.Diagnostics.Debug.WriteLine("엑셀 출력 모드로 전환 - isExcelMode=true, useMultiColumnMode=true 복원, 버튼 활성화");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("엑셀 출력 모드로 전환 - isExcelMode=true, 버튼 활성화");
                }
                
                // 오른쪽: 미리보기와 인쇄 버튼 표시 (하나의 버튼만 사용)
                panel_BarcodePreview.Visible = true;
                panel_PrintButton.Visible = true;
                // 엑셀 데이터가 있고 시리얼 매칭이 완료되었으면 버튼 활성화, 없으면 비활성화
                panel_PrintButton.Enabled = isSerialMatched && (excelData != null && excelData.Rows.Count > 0);
                
                // 버튼 텍스트 업데이트
                panel_PrintButton.Invalidate();
                
                // 파일명 라벨 업데이트 (엑셀 모드로 전환 시 항상 파일명 표시)
                if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
                {
                    UpdateExcelFileNameLabel();
                    System.Diagnostics.Debug.WriteLine($"엑셀 모드 전환 - 파일명 라벨 업데이트: {Path.GetFileName(excelFilePath)}");
                }
                else
                {
                    label_ExcelFileName.Text = "";
                    label_ExcelFileName.Visible = false;
                    System.Diagnostics.Debug.WriteLine("엑셀 모드 전환 - 파일명이 없어 라벨 숨김");
                }
                
                // 엑셀 데이터가 있으면 첫 번째 행으로 미리보기 갱신
                if (excelData != null && excelData.Rows.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("엑셀 모드 전환 - 엑셀 데이터로 미리보기 갱신");
                    UpdatePreviewWithFirstRow();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("엑셀 모드 전환 - 엑셀 데이터가 없음");
                    // 엑셀 데이터가 없으면 미리보기 초기화
                    OnPreviewEvent();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 출력 모드 전환 오류: {ex.Message}");
            }
        }
         
        private void TextBox_CopyCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 숫자(0-9)나 백스페이스만 입력받도록 설정
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != (char)8)
            {
                e.Handled = true;  // 숫자가 아닌 문자는 입력되지 않도록 처리
            }
        }
        public void PrintStart() {
            System.Diagnostics.Debug.WriteLine("=== PrintStart 시작 ===");
            System.Diagnostics.Debug.WriteLine($"스레드 ID: {Thread.CurrentThread.ManagedThreadId}");
            
            try
            {
                // 프린터 상태 확인 (먼저 확인)
                if (string.IsNullOrEmpty(PRINT_INFO_CLASS.PrinterInfo.printerName))
                {
                    System.Diagnostics.Debug.WriteLine("프린터가 선택되지 않음");
                    MessageBox.Show("프린터가 선택되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 다중 컬럼/입력 모드 확인: 큐에 데이터가 있는지 먼저 확인
                bool hasQueueData = barcodePrintManager != null && barcodePrintManager.HasPrintQueueData();
                System.Diagnostics.Debug.WriteLine($"[PrintStart] 큐에 데이터 존재 여부: {hasQueueData}");
                
                if (!hasQueueData)
                {
                    System.Diagnostics.Debug.WriteLine("인쇄 데이터 확인 시작");
                    // 큐에 데이터가 없으면 단일 바코드 데이터 확인
                    string printData = GetPrintData();
                    System.Diagnostics.Debug.WriteLine($"인쇄 데이터: '{printData}'");
                    
                    if (string.IsNullOrEmpty(printData))
                    {
                        System.Diagnostics.Debug.WriteLine("인쇄할 데이터가 없음");
                        MessageBox.Show("인쇄할 데이터가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("프린터 상태 확인 시작");
                System.Diagnostics.Debug.WriteLine($"인쇄 시작 - 프린터: {PRINT_INFO_CLASS.PrinterInfo.printerName}, 프린터 포트: {PRINT_INFO_CLASS.PrinterInfo.printerPortName}, DPI: {PRINT_INFO_CLASS.PrinterInfo.printer_DPI}");

                System.Diagnostics.Debug.WriteLine("인쇄 스레드 생성 시작");
                // 인쇄 실행
                Thread thread = new Thread(barcodePrintManager.Barcode_PrintGo)
                {
                    IsBackground = true,
                    Name = "PrintThread"
                };
                
                System.Diagnostics.Debug.WriteLine("인쇄 스레드 시작");
                thread.Start();
                
                System.Diagnostics.Debug.WriteLine($"인쇄 스레드 시작 완료 - 스레드 ID: {thread.ManagedThreadId}");
            }
            catch (AccessViolationException avEx)
            {
                System.Diagnostics.Debug.WriteLine($"PrintStart에서 AccessViolationException 발생: {avEx.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {avEx.StackTrace}");
                MessageBox.Show($"인쇄 중 메모리 접근 오류가 발생했습니다: {avEx.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PrintStart에서 예외 발생: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"인쇄 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== PrintStart 종료 ===");
            }
        }


        public void Init_ColumData()
        {
            COLUM_DATA.Column_Sets.Clear();

            for (int i = 0; i < 5; i++)
            {
                using (Column_Set ColumData = new Column_Set())
                {
                    ColumData.font = new System.Drawing.Font("굴림", 12);
                    ColumData.Location = new System.Drawing.Point(0, 0);
                    // EnabeBarcode 제거됨
                    ColumData.Fixed_Text_Check = false;
                    ColumData.Fixed_Text_After = true;
                    ColumData.Fixed_Text = "";
                    ColumData.BarcodeType = "QR_CODE";
                    ColumData.Barcode_Location = new System.Drawing.Point(0, 0);
                    ColumData.Barcode_Size = 15;                        

                    COLUM_DATA.Column_Sets.Add(ColumData);
                }
            }
        }

                 // 프린터 목록 업데이트 메서드 (AdjustForm에서 호출)
         public void UpdatePrinterList()
         {
             printerList.getMachineList();
         }

      internal static class User32
      {
          [System.Runtime.InteropServices.DllImport("user32.dll")]
          public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
      }
            

                
        private void Label_AdjustFormOpen_DoubleClick_1(object sender, EventArgs e)
        {
            // 컬럼 위치 설정 기능 제거됨 (고정값 사용)
            // 모든 컬럼은 기본 활성화 상태이며, 크기와 위치는 초기값으로 고정
            
            // 포커스 자동 이동 처리
            OnControlStateChanged(sender, e);
        }
        

        private void Label_buy_link_Paint(object sender, PaintEventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl == null) return;

            using (Pen pen = new Pen(Color.White, 2))
            {
                int y = lbl.Height - 2; // 밑줄 위치
                e.Graphics.DrawLine(pen, 0, y, lbl.Width, y);
            }
        }

        private void Label_buy_link_Click_1(object sender, EventArgs e)
        {
            string url = "https://kklabel.co.kr";

            // Chrome 설치 여부 확인
            string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\Chrome\\Application\\chrome.exe");
            if (!File.Exists(chromePath))
            {
                chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google\\Chrome\\Application\\chrome.exe");
            }

            ProcessStartInfo psi;
            if (File.Exists(chromePath))
            {
                psi = new ProcessStartInfo(chromePath, url) { UseShellExecute = true };
            }
            else
            {
                psi = new ProcessStartInfo(url) { UseShellExecute = true };
            }

            Process.Start(psi);

            // 잠시 대기 후 창을 중앙에 배치
            System.Threading.Thread.Sleep(1000);

            foreach (var proc in Process.GetProcessesByName("chrome")) // 브라우저 컬럼3은 환경에 맞게 변경
            {
                IntPtr hWnd = proc.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    Screen screen = Screen.PrimaryScreen;
                    int x = (screen.Bounds.Width - 800) / 2; // 너비 800px 가정
                    int y = (screen.Bounds.Height - 600) / 2; // 높이 600px 가정
                    User32.SetWindowPos(hWnd, 0, x, y, 800, 600, 0);
                    break;
                }
            }
            
            // 포커스 자동 이동 처리
            OnControlStateChanged(sender, e);
        }

        private void new_PrintButton(float shadowRate = 1f)
        {
            // 모드에 따라 버튼 텍스트 변경
            string ButtonText = isExcelMode ? "엑셀 출력" : "인쇄";
            System.Drawing.Font TextFont = new System.Drawing.Font("맑은 고딕", isExcelMode ? 18 : 20, System.Drawing.FontStyle.Bold);

            Color borderColor = Color.DodgerBlue; // 테두리 색

            // 음영 효과를 적용한 색상 결정 (비활성화 상태 시 회색배경)
            Color originalColor = panel_PrintButton.Enabled ? Color.DodgerBlue : Color.Gray;
            Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

            using (Graphics g = panel_PrintButton.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_PrintButton.Width - 1, h = panel_PrintButton.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    // 부모 배경을 지우고 내부 채우기
                    using (SolidBrush brush = new SolidBrush(panelColor))
                    {
                        g.FillPath(brush, path);
                    }
                    // 중앙에 "Print" 텍스트 그리기 (비활성화 시 밝은 회색 글자)
                    using (SolidBrush textBrush = new SolidBrush(panel_PrintButton.Enabled ? Color.White : Color.LightGray))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_PrintButton.Width - textSize.Width) / 2,
                            (panel_PrintButton.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }

        private void new_OpenLayoutFormButton(float shadowRate = 1f)
        {
            string ButtonText = "레이아웃";
            System.Drawing.Font TextFont = new System.Drawing.Font("페이퍼로지 5 Medium", 13, System.Drawing.FontStyle.Bold);

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
            Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

            if (panel_OpenLayoutForm == null) return;

            using (Graphics g = panel_OpenLayoutForm.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_OpenLayoutForm.Width - 1, h = panel_OpenLayoutForm.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    // 부모 배경을 지우고 내부 채우기
                    using (SolidBrush brush = new SolidBrush(panelColor))
                    {
                        g.FillPath(brush, path);
                    }
                    // 중앙에 "레이아웃" 텍스트 그리기
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_OpenLayoutForm.Width - textSize.Width) / 2,
                            (panel_OpenLayoutForm.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }

        // new_ExcelPrintButton 메서드는 제거됨 (new_PrintButton에서 모드에 따라 텍스트 변경)

        private void new_OpenAdjustFormButton(float shadowRate = 1f)
        {
            string ButtonText = "프린터설정";
            System.Drawing.Font TextFont = new System.Drawing.Font("페이퍼로지 5 Medium", 13, System.Drawing.FontStyle.Bold);

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
            Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

            using (Graphics g = panel_OpenAdjustForm.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_OpenAdjustForm.Width - 1, h = panel_OpenAdjustForm.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    // 부모 배경을 지우고 내부 채우기
                    using (SolidBrush brush = new SolidBrush(panelColor))
                    {
                        g.FillPath(brush, path);
                    }
                    // 중앙에 "상세설정" 텍스트 그리기
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_OpenAdjustForm.Width - textSize.Width) / 2,
                            (panel_OpenAdjustForm.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }

        private void new_OpenExcelFormButton(float shadowRate = 1f)
        {
            // 엑셀 모드에서는 항상 활성화
            if(!isExcelMode)
            {
                DrawDisabledExcelButton();
                return;
            }

            string ButtonText = "파일 불러오기";
            System.Drawing.Font TextFont = new System.Drawing.Font("맑은 고딕", 11, System.Drawing.FontStyle.Bold);

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
            Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

            using (Graphics g = panel_OpenExcelForm.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_OpenExcelForm.Width - 1, h = panel_OpenExcelForm.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    // 부모 배경을 지우고 내부 채우기
                    using (SolidBrush brush = new SolidBrush(panelColor))
                    {
                        g.FillPath(brush, path);
                    }
                    // 중앙에 "엑셀 연동" 텍스트 그리기
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_OpenExcelForm.Width - textSize.Width) / 2,
                            (panel_OpenExcelForm.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }

        private void DrawDisabledExcelButton()
        {
            string ButtonText = "파일 불러오기";
            System.Drawing.Font TextFont = new System.Drawing.Font("맑은 고딕", 11, System.Drawing.FontStyle.Bold);

            // 비활성화 상태의 회색 색상
            Color disabledColor = Color.Gray;

            using (Graphics g = panel_OpenExcelForm.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_OpenExcelForm.Width - 1, h = panel_OpenExcelForm.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    // 회색 배경 채우기
                    using (SolidBrush brush = new SolidBrush(disabledColor))
                    {
                        g.FillPath(brush, path);
                    }
                    // 중앙에 "엑셀 연동" 텍스트 그리기 (회색 텍스트)
                    using (SolidBrush textBrush = new SolidBrush(Color.DarkGray))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_OpenExcelForm.Width - textSize.Width) / 2,
                            (panel_OpenExcelForm.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }
        
        private void panel_PrintButton_Paint(object sender, PaintEventArgs e)
        {
            new_PrintButton();
            return;
        }

        private void panel_OpenAdjustForm_Paint(object sender, PaintEventArgs e)
        {
            new_OpenAdjustFormButton();
            return;
        }


        private void panel_OpenExcelForm_Paint(object sender, PaintEventArgs e)
        {
            new_OpenExcelFormButton();
            return;
        }

        private void panel_SaveProject_Paint(object sender, PaintEventArgs e)
        {
            DrawProjectButton(e.Graphics, panel_SaveProject, "저장", Color.MediumSeaGreen);
        }

        private void panel_OpenProject_Paint(object sender, PaintEventArgs e)
        {
            DrawProjectButton(e.Graphics, panel_OpenProject, "열기", Color.RoyalBlue);
        }

        private void DrawProjectButton(Graphics g, Panel panel, string text, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = new GraphicsPath())
            {
                int r = 10;
                int w = panel.Width - 1, h = panel.Height - 1;
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(w - r, 0, r, r, 270, 90);
                path.AddArc(w - r, h - r, r, r, 0, 90);
                path.AddArc(0, h - r, r, r, 90, 90);
                path.CloseFigure();

                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }

                using (System.Drawing.Font font = new System.Drawing.Font("맑은 고딕", 10, System.Drawing.FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, textBrush, (panel.Width - textSize.Width) / 2, (panel.Height - textSize.Height) / 2);
                }
            }
        }

        /// <summary>
        /// 바코드 입력 처리 공통 메서드
        /// </summary>
        private void ProcessBarcodeInput(bool isFromScanner = false)
        {
            var inputText = TextBox_InputText.Text;

            // placeholder 텍스트나 빈 문자열은 처리하지 않음
            if (string.IsNullOrWhiteSpace(inputText) || 
                inputText == "값을 입력하세요." || 
                inputText.Contains("값을 입력하세요"))
            {
                System.Diagnostics.Debug.WriteLine($"바코드 입력 무시: '{inputText}' (placeholder 텍스트)");
                return;
            }

            if (!string.IsNullOrWhiteSpace(inputText))
            {
                // 엑셀 모드인 경우 엑셀 연동 처리
                if (isExcelMode && excelData != null)
                {
                    ProcessBarcodeWithExcel(inputText);
                }
                else
                {
                    // 일반 모드: 바코드 미리보기 업데이트 (참조 프로그램 방식)
                    currentBarcodeValue = inputText;
                    currentBarcodeText = ""; // 일반 모드에서는 텍스트 초기화
                    
                    barcodePrintManager.AddToPrintQueue(inputText, barcodeSize, barcodeX, barcodeY, barcodeHeight);
                    PrintStart();
                }
                
                // 스캐너 입력인 경우에만 TextBox 지우기
                if (isFromScanner)
                {
                    isAutoClearingTextBox = true;
                    TextBox_InputText.Text = "";
                    isAutoClearingTextBox = false;
                }
                // 수동 입력의 경우 텍스트박스는 그대로 유지
            }
        }



        /// <summary>
        /// 전역 키 입력 처리 (비활성화 - TextBox별 KeyDown 이벤트 사용)
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ProcessCmdKey를 통한 전역 엔터키 처리 비활성화
            // (각 TextBox의 KeyDown 이벤트에서 개별 처리)
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 모든 TextBox의 엔터키 이벤트 핸들러 (출력 버튼과 동일한 동작)
        /// </summary>
        private void TextBox_InputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                if (isSearchingExcel)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                
                e.Handled = true;
                e.SuppressKeyPress = true;
                panel_PrintButton_Click(sender, e);
            }
        }
        
        private void TextBox_InputText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_InputText2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (isSearchingExcel)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                
                e.Handled = true;
                e.SuppressKeyPress = true;
                panel_PrintButton_Click(sender, e);
            }
        }
        
        private void TextBox_InputText2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_InputText3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                if (isSearchingExcel)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                
                e.Handled = true;
                e.SuppressKeyPress = true;
                panel_PrintButton_Click(sender, e);
            }
        }
        
        private void TextBox_InputText3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // TextBox_InputText5는 명찰용으로 제거됨 (4개 컬럼만 사용)
        // private void TextBox_InputText5_KeyDown(object sender, KeyEventArgs e)
        // {
        //     if (e.KeyCode == Keys.Enter)
        //     {
        //         if (isSearchingExcel)
        //         {
        //             e.Handled = true;
        //             e.SuppressKeyPress = true;
        //             return;
        //         }
        //         
        //         e.Handled = true;
        //         e.SuppressKeyPress = true;
        //         panel_PrintButton_Click(sender, e);
        //     }
        // }
        // 
        // private void TextBox_InputText5_KeyUp(object sender, KeyEventArgs e)
        // {
        //     if (e.KeyCode == Keys.Enter)
        //     {
        //         e.Handled = true;
        //         e.SuppressKeyPress = true;
        //     }
        // }

        private void TextBox_InputText4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                if (isSearchingExcel)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                
                e.Handled = true;
                e.SuppressKeyPress = true;
                panel_PrintButton_Click(sender, e);
            }
        }
        
        private void TextBox_InputText4_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (sender is RoundedTextBox rtb && rtb.Multiline)
                {
                    return;
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // 슬라이더바 이벤트 핸들러는 PositionAdjustmentForm으로 이동됨
        // 슬라이더바 관련 이벤트 핸들러 제거됨

        private void panel_PrintButton_Click(object sender, EventArgs e)
        {
            // 버튼이 비활성화되어 있으면 인쇄 명령을 실행하지 않고 리턴
            if (!panel_PrintButton.Enabled)
            {
                System.Diagnostics.Debug.WriteLine("[인쇄 실패] 인쇄 버튼이 비활성화 상태입니다. (시리얼 인증 실패 또는 조건 미충족)");
                return;
            }

            // 모드에 따라 다른 기능 실행
            if (isExcelMode)
            {
                // 엑셀 출력 모드
                panel_ExcelPrintButton_Click(sender, e);
            }
            else
            {
                // 직접 출력 모드
                // 주소1과 주소2를 하나로 합치기
                string address1 = GetTextBoxValue(TextBox_InputText4); // 주소1
                string address2 = GetTextBoxValue(TextBox_InputText);  // 주소2
                string combinedAddress = "";
                
                if (!string.IsNullOrEmpty(address1) && !string.IsNullOrEmpty(address2))
                {
                    combinedAddress = address1 + "\n" + address2; // 주소1 + 줄바꿈 + 주소2
                }
                else if (!string.IsNullOrEmpty(address1))
                {
                    combinedAddress = address1;
                }
                else if (!string.IsNullOrEmpty(address2))
                {
                    combinedAddress = address2;
                }
                
                // 컬럼3만 사용 (명찰용으로 전화번호 제거)
                string name = GetTextBoxValue(TextBox_InputText3);       // 컬럼3
                // string phoneNumber = GetTextBoxValue(TextBox_InputText5); // 전화번호 (명찰용으로 제거됨)
                string combinedNamePhone = "";
                
                // 명찰용으로 전화번호 제거, 컬럼3만 사용
                if (!string.IsNullOrEmpty(name))
                {
                    combinedNamePhone = name;
                }
                // else if (!string.IsNullOrEmpty(phoneNumber))
                // {
                //     combinedNamePhone = phoneNumber;
                // }
                
                // 가격표용 입력 데이터 가져오기 (GetPreviewTexts와 동일한 순서)
                // columnPositionSettings 인덱스와 매칭 (가격표용):
                // 인덱스 0: 품명 (TextBox_InputText4)
                // 인덱스 1: 가격 (TextBox_InputText)
                // 인덱스 2: 비고 (TextBox_InputText3)
                // 인덱스 3: 바코드 (TextBox_InputText2)
                // 인덱스 4: 공통문구 (GetSenderReceiverLabel)
                var textBoxInputs = new List<string>
                {
                    GetTextBoxValue(TextBox_InputText4),   // 인덱스 0: 품명
                    GetTextBoxValue(TextBox_InputText),    // 인덱스 1: 가격
                    GetTextBoxValue(TextBox_InputText3),   // 인덱스 2: 비고
                    GetTextBoxValue(TextBox_InputText2),   // 인덱스 3: 바코드
                    GetSenderReceiverLabel()               // 인덱스 4: 공통문구
                };
                
                System.Diagnostics.Debug.WriteLine($"[직접 인쇄] 가격표용 입력 데이터:");
                System.Diagnostics.Debug.WriteLine($"  품명 (인덱스 0): '{textBoxInputs[0]}'");
                System.Diagnostics.Debug.WriteLine($"  가격 (인덱스 1): '{textBoxInputs[1]}'");
                System.Diagnostics.Debug.WriteLine($"  비고 (인덱스 2): '{textBoxInputs[2]}'");
                System.Diagnostics.Debug.WriteLine($"  바코드 (인덱스 3): '{textBoxInputs[3]}'");
                if (textBoxInputs.Count > 4)
                    System.Diagnostics.Debug.WriteLine($"  공통문구 (인덱스 4): '{textBoxInputs[4]}'");
                
                // 직접 출력 버튼은 TextBox 입력값을 직접 출력
                // 컬럼 위치 설정이 있으면 다중 입력 출력
                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    ProcessMultipleInputsWithSettings(textBoxInputs);
                }
                else
                {
                    // 기존 방식: 첫 번째 TextBox만 사용
                    ProcessBarcodeInput(isFromScanner: false);
                }
                
                // 포커스 자동 이동 처리
                OnControlStateChanged(sender, e);
            }
        }
        
        /// <summary>
        /// TextBox에서 유효한 값 가져오기 (placeholder 텍스트 제외)
        /// </summary>
        private string GetTextBoxValue(BarcodePrint.UserControls.RoundedTextBox textBox)
        {
            if (textBox == null) return "";
            
            string text = textBox.Text?.Trim() ?? "";
            
            // placeholder 텍스트는 빈 문자열로 처리 (예: 18462, 예: 김상윤 등)
            if (string.IsNullOrWhiteSpace(text) || 
                text.StartsWith("예:", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("값을 입력하세요.", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("값을 입력하세요", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("값을입력하세요", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            
            return text;
        }
        
        /// <summary>
        /// 여러 입력값을 컬럼 위치 설정에 따라 처리 (한 용지에 모든 컬럼 출력)
        /// </summary>
        private void ProcessMultipleInputsWithSettings(List<string> inputs)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[다중 입력 인쇄] 처리 시작: {inputs.Count}개 입력");
                
                // 다중 입력 데이터를 한 용지에 출력하기 위한 MultiColumnData 리스트 생성
                var multiColumnData = new List<MultiColumnData>();
                
                for (int i = 0; i < Math.Min(inputs.Count, columnPositionSettings.Count); i++)
                {
                    var setting = columnPositionSettings[i];
                    var input = inputs[i];
                    
                    // 바코드 컬럼(ColumnIndex==3)인 경우 값이 입력되어 있으면 IsEnabled와 관계없이 포함
                    bool isBarcodeColumn = setting.ColumnIndex == 3;
                    // 공통문구(ColumnIndex==4): "표시없음"이면 제외
                    bool isCommonTextColumn = setting.ColumnIndex == 4;
                    bool shouldInclude = (setting.IsEnabled || (isBarcodeColumn && !string.IsNullOrEmpty(input))) 
                        && !string.IsNullOrEmpty(input) 
                        && (!isCommonTextColumn || input != "표시없음");
                    
                    if (shouldInclude)
                    {
                        // 가격표용 - ColumnName을 명확하게 설정 (인쇄 시 매칭을 위해)
                        string[] columnNames = { "컬럼1", "컬럼2", "컬럼3", "컬럼4", "공통문구" };
                        string columnName = setting.ColumnIndex < columnNames.Length ? columnNames[setting.ColumnIndex] : $"컬럼 {setting.ColumnIndex}";
                        
                        // 바코드 컬럼(ColumnIndex==3)인 경우 UsageType과 관계없이 IsBarcode=True로 설정 -> 제거하여 UsageType을 따름
                        bool isBarcode = setting.UsageType == "바코드";
                        
                        // 바코드 컬럼도 ShowBarcodeText 설정 허용 (LabelLayoutSettingsForm에서 설정)
                        bool showBarcodeText = setting.ShowBarcodeText;
                        
                        multiColumnData.Add(new MultiColumnData
                        {
                            Value = input,
                            IsBarcode = isBarcode,
                            BarcodeType = isBarcode ? (setting.BarcodeType ?? "QR_CODE") : setting.BarcodeType,
                            ColumnName = columnName, // "품명", "가격", "비고", "바코드"로 설정
                            Size = setting.Size,
                            BarcodeHeight = setting.BarcodeHeight,
                            OffsetX = setting.OffsetX,
                            OffsetY = setting.OffsetY,
                            ShowBarcodeText = showBarcodeText,
                            BarcodeTextOffsetX = setting.BarcodeTextOffsetX,
                            BarcodeTextOffsetY = setting.BarcodeTextOffsetY,
                            BarcodeTextSize = setting.BarcodeTextSize,
                            FontFamily = setting.FontFamily,
                            IsBold = setting.IsBold,
                            CustomWidth = setting.CustomWidth,
                            RotateAngle = setting.RotateAngle,
                            Alignment = setting.Alignment
                        });
                        
                        System.Diagnostics.Debug.WriteLine($"[다중 입력] 컬럼 {i} 추가: ColumnName='{columnName}', 값='{input}', 타입={setting.UsageType}, IsBarcode={isBarcode}, 크기={setting.Size}, X={setting.OffsetX}, Y={setting.OffsetY}, ShowText={setting.ShowBarcodeText}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[다중 입력] 컬럼 {i} 제외: IsEnabled={setting.IsEnabled}, 값='{input}', IsBarcodeColumn={isBarcodeColumn}");
                    }
                }
                
                if (multiColumnData.Count > 0)
                {
                    // BarcodePrintManager에 다중 컬럼 데이터를 전달 (한 용지에 모든 컬럼 포함)
                    barcodePrintManager.AddMultiInputToPrintQueue(multiColumnData);
                    
                    PrintStart();
                    
                    System.Diagnostics.Debug.WriteLine($"[다중 입력 인쇄] 한 용지에 {multiColumnData.Count}개 컬럼 출력");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[경고] 출력할 데이터가 없습니다.");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Windows.MessageBox.Show("출력할 데이터가 없습니다.", "알림", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다중 입력 처리 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Windows.MessageBox.Show($"출력 처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        

        // calibration 프로젝트 방식의 시리얼 추출 함수 추가
        /// <summary>
        /// 프린터가 실제로 연결되어 있고 통신 가능한지 확인
        /// (장치 관리자의 범용 직렬 버스 컨트롤러에서 현재 연결된 USB 장치 확인)
        /// </summary>
        private bool IsPrinterActuallyConnected(string printerName, string printerPort)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[IsPrinterActuallyConnected] 장치 관리자에서 현재 연결된 USB 장치 확인 시작: {printerName}");
                
                if (string.IsNullOrEmpty(printerName))
                {
                    System.Diagnostics.Debug.WriteLine("[IsPrinterActuallyConnected] 프린터 이름이 비어있음");
                    return false;
                }

                string normalizedPrinterName = RemoveParenthesesFromPrinterName(printerName);

                try
                {
                    bool foundMatchingDevice = false;
                    
                    // USBPRINT 장치 쿼리 (실물 연결된 USB 프린터만 탐색)
                    using (var searcher = new ManagementObjectSearcher("SELECT Name, DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE 'USBPRINT%'"))
                    {
                        foreach (ManagementObject device in searcher.Get())
                        {
                            string deviceName = device["Name"]?.ToString() ?? "";
                            string deviceId = device["DeviceID"]?.ToString() ?? "";
                            
                            System.Diagnostics.Debug.WriteLine($"[IsPrinterActuallyConnected] 발견된 USBPRINT 장치: {deviceName}, ID: {deviceId}");
                            
                            // 프린터 이름(전체 또는 정규화된 이름)이 장치명에 포함되어 있는지 검사
                            if (deviceName.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                printerName.IndexOf(deviceName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                deviceName.IndexOf(normalizedPrinterName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[IsPrinterActuallyConnected] ✅ 프린터 '{printerName}' 실제 물리적 연결 확인됨");
                                foundMatchingDevice = true;
                                break;
                            }
                        }
                    }
                    
                    return foundMatchingDevice;
                }
                catch (Exception wmiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[IsPrinterActuallyConnected] WMI 쿼리 오류: {wmiEx.Message}");
                    // WMI 실패 시 보수적으로 true를 반환하여 복구 연결이 구동되도록 시도
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IsPrinterActuallyConnected] 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 프린터 컬럼3에서 괄호와 그 안의 텍스트를 제거하여 정규화
        /// 예: "Xprinter XP-T265B (1 복사)" -> "Xprinter XP-T265B"
        /// </summary>
        /// <summary>
        /// 프린터 컬럼3에서 괄호 내용 제거 (정규화)
        /// </summary>
        private string RemoveParenthesesFromPrinterName(string printerName)
        {
            if (string.IsNullOrEmpty(printerName))
                return printerName;

            // 괄호와 그 내용 제거
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\s*\([^)]*\)");
            string normalized = regex.Replace(printerName, "").Trim();

            System.Diagnostics.Debug.WriteLine($"RemoveParenthesesFromPrinterName: '{printerName}' -> '{normalized}'");
            return normalized;
        }

        /// <summary>
        /// 비동기적으로 프린터를 식별하고 시리얼 번호를 검증함 (3단계 정밀 스캔 + DLL 인증)
        /// </summary>
        private async Task InitializeSelectedPrinterAsync()
        {
            // 하드웨어 스캔 시작 전 상태 메시지 표시
            if (label_PrinterStatus != null)
            {
                label_PrinterStatus.Text = "프린터 준비중..";
            }

            // 스캔 및 검증 완료 전까지는 인쇄 버튼 비활성화
            this.isSerialMatched = false;
            panel_PrintButton.Enabled = false;
            panel_PrintButton.Invalidate();

            await Task.Run(() =>
            {
                // UI가 뜬 직후 WMI 검색을 위해 약간의 여유를 둠 (시스템 부하 고려)
                Thread.Sleep(300);

                try
                {
                    UserSerials = BarcodePrint.Model.SerialCryptoManager.LoadSerials().ToArray();
                    if (UserSerials.Length == 0)
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[경고] 로드된 프린터 시리얼 번호가 없습니다. Info/58c5dec13bce64ea.dat 암호화 시리얼 라이선스 파일을 확인해주세요.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[MainForm] 시리얼 로드 중 오류: " + ex.Message);
                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[에러] 시리얼 로드 중 예외 발생: {ex.Message}");
                }

                BarcodePrint.Model.PrinterSerialLogger.LogProgramStart();
                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[비동기 초기화] 프린터 식별 및 시리얼 번호 확인 시작. 등록된 시리얼 개수: {UserSerials.Length}");

                // 1단계 시작 전 최신 프린터 목록 재확인 (WMI 지연 대비)
                printerList.getMachineList();

                string normalizedSerial = string.Empty;
                bool serialMatched = false;
                string matchedNameFromHardware = "";
                string matchedPortFromHardware = "";

                try
                {
                    // 0단계: 이전 저장된 프린터 정보가 존재하고 실제 연결되어 있다면 즉시 사용
                    if (!string.IsNullOrEmpty(appSettings.LastPrinterName) && !string.IsNullOrEmpty(appSettings.LastPrinterPort))
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[0단계-복구] 이전 저장된 프린터 정보 확인 시작: {appSettings.LastPrinterName} ({appSettings.LastPrinterPort})");
                        
                        if (IsPrinterActuallyConnected(appSettings.LastPrinterName, appSettings.LastPrinterPort))
                        {
                            string lastSerial = appSettings.LastPrinterSerial;
                            
                            // 저장된 시리얼이 등록 리스트에 존재하는지 검증
                            if (!string.IsNullOrEmpty(lastSerial) && 
                                UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(lastSerial.ToUpperInvariant().Trim().Replace(" ", ""))))
                            {
                                normalizedSerial = lastSerial.ToUpperInvariant().Trim().Replace(" ", "");
                                serialMatched = true;
                                matchedNameFromHardware = appSettings.LastPrinterName;
                                matchedPortFromHardware = appSettings.LastPrinterPort;
                                
                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [0단계-복구 매칭 성공] 저장된 프린터가 활성 상태이며 인증 확인됨. 시리얼: {normalizedSerial}");
                            }
                            else
                            {
                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [0단계-복구 실패] 저장된 시리얼 '{lastSerial}'이(가) 등록 목록에 없거나 유효하지 않음");
                            }
                        }
                        else
                        {
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog("  => [0단계-복구 실패] 저장된 프린터가 현재 감지되지 않거나 전원이 꺼져 있음");
                        }
                    }

                    // 1단계: USB 장치 목록에서 매칭 시도 (UsbPrinterHelper)
                    if (!serialMatched)
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[1단계] USB 장치 목록(Registry USB/USBPRINT) 탐색 시작...");
                        var usbPrinterInfoList = BarcodePrint.Model.UsbPrinterHelper.GetConnectedUsbPrinters(null, UserSerials);
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[1단계] USB 장치 목록 검색 완료. 검색된 장치 수: {usbPrinterInfoList.Count}");

                        foreach (var usbInfo in usbPrinterInfoList)
                        {
                            if (string.IsNullOrEmpty(usbInfo.Serial)) continue;
                            string currentSerial = usbInfo.Serial.ToUpperInvariant().Trim().Replace(" ", "");
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  - 발견 기기: {usbInfo.Name} (포트: {usbInfo.PortName}, Serial: {currentSerial})");

                            if (UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(currentSerial)))
                            {
                                // [보완] 인증된 기기 중 현재 온라인 상태(Connect=true)인 것을 선택
                                bool isOnline = false;
                                if (USB_status.Port != null && USB_status.Connect != null)
                                {
                                    int idx = USB_status.Port.IndexOf(usbInfo.PortName);
                                    if (idx >= 0 && idx < USB_status.Connect.Count && USB_status.Connect[idx])
                                    {
                                        isOnline = true;
                                    }
                                }

                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  - [검사] 시리얼: {currentSerial}, 온라인 상태: {isOnline}");

                                if (isOnline)
                                {
                                    normalizedSerial = currentSerial;
                                    serialMatched = true;
                                    matchedNameFromHardware = usbInfo.Name;
                                    matchedPortFromHardware = usbInfo.PortName;

                                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [1단계 매칭 성공] 시리얼: {currentSerial}, 온라인 상태: {isOnline}");
                                    System.Diagnostics.Debug.WriteLine($"[비동기 초기화] 온라인 인증 장치 발견: {usbInfo.Name} ({usbInfo.PortName})");
                                    break; // 온라인 기기를 찾으면 즉시 종료
                                }
                            }
                        }
                    }

                    // 2단계: 하드웨어 정밀 스캔 (UsbPrinterInfoTest)
                    if (!serialMatched)
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[2단계] 하드웨어 정밀 스캔(Win32_PnPEntity VID_2D37) 시작...");
                        var testResults = UsbPrinterInfoTest.GetUsbPrinterInfo();
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[2단계] 정밀 스캔 완료. 후보 장치 수: {testResults.Count}");

                        foreach (var matched in testResults)
                        {
                            if (string.IsNullOrEmpty(matched.Serial)) continue;
                            string currentSerial = matched.Serial.ToUpperInvariant().Trim().Replace(" ", "");
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  - 발견 기기: {matched.Name} (포트: {matched.Port}, Serial: {currentSerial})");

                            if (UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(currentSerial)))
                            {
                                normalizedSerial = currentSerial;
                                serialMatched = true;
                                matchedNameFromHardware = matched.Name;
                                matchedPortFromHardware = matched.Port;
                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [2단계 매칭 성공] 시리얼: {currentSerial}");
                                break;
                            }
                        }
                    }

                    // 3단계: WMI 폴백 (GetSerialFromWmi)
                    if (!serialMatched)
                    {
                        string curName = PRINT_INFO_CLASS.PrinterInfo.printerName;
                        string curPort = PRINT_INFO_CLASS.PrinterInfo.printerPortName;
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[3단계] WMI 폴백 탐색 시작 (현재 선택된 프린터: {curName}, 포트: {curPort})...");

                        if (!string.IsNullOrEmpty(curName) && !string.IsNullOrEmpty(curPort))
                        {
                            string fallbackSerial = GetSerialFromWmi(curName, curPort);
                            if (!string.IsNullOrEmpty(fallbackSerial))
                            {
                                normalizedSerial = fallbackSerial.ToUpperInvariant().Trim().Replace(" ", "");
                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  - WMI 시리얼 발견: {normalizedSerial}");

                                if (UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(normalizedSerial)))
                                {
                                    serialMatched = true;
                                    matchedNameFromHardware = curName;
                                    matchedPortFromHardware = curPort;
                                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [3단계 매칭 성공] 시리얼: {normalizedSerial}");
                                }
                                else
                                {
                                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  => [3단계 실패] 시리얼 {normalizedSerial}이 등록 목록에 없음");
                                }
                            }
                            else
                            {
                                BarcodePrint.Model.PrinterSerialLogger.WriteLog("  => [3단계 실패] WMI에서 시리얼 추출 실패");
                            }
                        }
                        else
                        {
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog("  => [3단계 건너뜀] 현재 선택된 프린터 이름 또는 포트가 비어 있음");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"프린터 시리얼번호 추출 비동기 처리 중 오류: {ex.Message}");
                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[에러] 시리얼 추출 실패: {ex.Message}\n{ex.StackTrace}");
                }

                PRINT_INFO_CLASS.PrinterInfo.PrinterSerial = normalizedSerial;
                
                // 만약 하드웨어 식별에 성공했다면 해당 정보를 임시 저장
                if (serialMatched)
                {
                    PRINT_INFO_CLASS.PrinterInfo.printerName = matchedNameFromHardware;
                    PRINT_INFO_CLASS.PrinterInfo.printerPortName = matchedPortFromHardware;
                }

                // 최종 검증 로직 (IsRelease 모드이며 데모 버전이 아닐 때만 검증 수행)
                if (IsRelease && !isDemoVersion)
                {
                    System.Diagnostics.Debug.WriteLine($"[비동기 초기화 최종결과] Authorized: {serialMatched} (SerialMatch: {serialMatched})");
                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[최종 결과] Authorized: {serialMatched} (SerialMatch: {serialMatched})");

                    // 시리얼 매칭이 실패한 경우 즉시 종료 플래그 설정
                    shouldCloseOnLoad = !serialMatched;
                }
                else
                {
                    shouldCloseOnLoad = false;
                    string modeName = isDemoVersion ? "체험판 모드" : "디버그 모드";
                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[최종 결과] {modeName}로 동작 (인증 검사 패스)");
                }

                // 성공적으로 매칭된 경우 프린터 정보 영구 저장
                if (serialMatched && !shouldCloseOnLoad)
                {
                    try
                    {
                        appSettings.LastPrinterName = matchedNameFromHardware;
                        appSettings.LastPrinterPort = matchedPortFromHardware;
                        appSettings.LastPrinterSerial = normalizedSerial;
                        appSettings.Save();
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[설정 저장] 성공적으로 검증된 프린터 정보 저장: {matchedNameFromHardware} ({matchedPortFromHardware})");
                    }
                    catch (Exception saveEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainForm] 프린터 설정 저장 중 에러: {saveEx.Message}");
                    }
                }

                // 프린터 연결 및 상태 스레드 업데이트 (UI 스레드에서 실행)
                this.Invoke(new Action(() =>
                {
                    this.isSerialMatched = serialMatched;

                    if (!serialMatched)
                    {
                        if (label_PrinterStatus != null)
                        {
                            if (USB_status.Name == null || USB_status.Name.Count == 0)
                            {
                                label_PrinterStatus.Text = "연결된 프린터 없음";
                            }
                            else
                            {
                                label_PrinterStatus.Text = "시리얼번호 매칭 실패";
                            }
                            label_PrinterStatus.ForeColor = Color.Red;
                        }

                        // 데모버전이 아닐 때 매칭 실패시 팝업창 표시 (프로그램이 자동 종료되지 않는 경우에만 알림)
                        if (!isDemoVersion && !shouldCloseOnLoad)
                        {
                            System.Windows.Forms.MessageBox.Show(
                                "프린터 시리얼 번호 매칭에 실패하였습니다.\n인쇄 기능이 비활성화됩니다.\n\n구매처에 문의하시기 바랍니다.\n-한국Xprinter-",
                                "라벨마스터",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    }

                    // 프린터 검증 상태 반영을 위해 패널 상태 재설정
                    UpdateExcelPanelState();

                    string hwName = PRINT_INFO_CLASS.PrinterInfo.printerName;
                    string hwPort = PRINT_INFO_CLASS.PrinterInfo.printerPortName;
                    string bestMatchName = "";

                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[UI 연동] 식별된 하드웨어 이름: '{hwName}', 포트: '{hwPort}'");

                    // 1단계: USB_status 리스트에서 하드웨어 식별 정보와 매칭되는 실제 Windows 프린터명 찾기
                    if (USB_status.Name != null && USB_status.Name.Count > 0)
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[UI 연동] USB_status에 등록된 프린터 수: {USB_status.Name.Count}");
                        for (int k = 0; k < USB_status.Name.Count; k++)
                        {
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog($"  - {k}: {USB_status.Name[k]} | 포트: {USB_status.Port[k]} | 연결상태: {USB_status.Connect[k]}");
                        }

                        // A. 포트가 일치하는 것이 있는지 먼저 탐색 (가장 정확)
                        if (!string.IsNullOrEmpty(hwPort))
                        {
                            for (int i = 0; i < USB_status.Name.Count; i++)
                            {
                                if (USB_status.Port[i].Equals(hwPort, StringComparison.OrdinalIgnoreCase))
                                {
                                    bestMatchName = USB_status.Name[i];
                                    System.Diagnostics.Debug.WriteLine($"[비동기 초기화] 포트 기반 매칭 성공: {bestMatchName} ({hwPort})");
                                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[UI 연동] 포트 기반 매칭 성공: {bestMatchName} (포트: {hwPort})");
                                    break;
                                }
                            }
                        }

                        // B. 포트 매칭 실패 시, 하드웨어 식별 이름이 Windows 프린터명에 포함되는지 확인
                        if (string.IsNullOrEmpty(bestMatchName) && !string.IsNullOrEmpty(hwName))
                        {
                            for (int i = 0; i < USB_status.Name.Count; i++)
                            {
                                if (USB_status.Name[i].IndexOf(hwName, StringComparison.OrdinalIgnoreCase) >= 0 || 
                                    hwName.IndexOf(USB_status.Name[i], StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    bestMatchName = USB_status.Name[i];
                                    System.Diagnostics.Debug.WriteLine($"[비동기 초기화] 이름 부분 매칭 성공: {bestMatchName} (HW: {hwName})");
                                    BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[UI 연동] 이름 부분 매칭 성공: {bestMatchName} (HW: {hwName})");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[UI 연동 경고] USB_status.Name이 비어 있거나 null입니다.");
                    }

                    // 2단계: 매칭 성공 여부에 따른 최종 연동
                    if (!string.IsNullOrEmpty(bestMatchName))
                    {
                        // 찾은 정확한 Windows 이름으로 상태 활성화
                        MachineList_selectChanged(bestMatchName);
                        System.Diagnostics.Debug.WriteLine($"[비동기 초기화 성공] 프린터 활성화 완료: {bestMatchName}");
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog($"[UI 연동] 프린터 활성화 완료: {bestMatchName}");
                    }
                    else
                    {
                        if (serialMatched)
                        {
                            
                        }

                        // 매칭 실패 시 기본 Xprinter 자동 선택 시도 (폴백)
                        System.Diagnostics.Debug.WriteLine("[비동기 초기화] 정밀 매칭 실패, autoSetPrinter 폴백 수행");
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[UI 연동 경고] 정밀 매칭 실패로 autoSetPrinter 폴백 수행");
                        autoSetPrinter();
                    }

                    if (shouldCloseOnLoad)
                    {
                        System.Diagnostics.Debug.WriteLine($"[비동기 초기화 경고] 인증 또는 매칭 실패로 프로그램 종료가 예정됨 (SerialMatched: {serialMatched})");
                        BarcodePrint.Model.PrinterSerialLogger.WriteLog("[UI 연동 경고] 인증 또는 매칭 실패로 프로그램 종료 예정");
                    }

                    BarcodePrint.Model.PrinterSerialLogger.LogProgramEnd();

                    UpdateLabelSizeInfo();
                    OnPreviewEvent();
                }));
            });
        }

        /// <summary>
        /// WMI를 사용하여 특정 프린터의 시리얼 번호 추출
        /// </summary>
        private string GetSerialFromWmi(string printerName, string printerPort)
        {
            try
            {
                string normalizedPrinterName = RemoveParenthesesFromPrinterName(printerName);
                System.Diagnostics.Debug.WriteLine($"[GetSerialFromWmi] 프린터: '{printerName}' (정규화: '{normalizedPrinterName}'), 포트: '{printerPort}'");
                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"    [GetSerialFromWmi] 프린터: '{printerName}' (정규화: '{normalizedPrinterName}'), 포트: '{printerPort}'");

                using (ManagementObjectSearcher usbControllerSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBControllerDevice"))
                {
                    foreach (ManagementObject usbController in usbControllerSearcher.Get())
                    {
                        try
                        {
                            string dependentPath = usbController["Dependent"]?.ToString();
                            if (string.IsNullOrEmpty(dependentPath))
                                continue;

                            using (ManagementObject usbDevice = new ManagementObject(dependentPath))
                            {
                                usbDevice.Get();
                                string deviceName = usbDevice["Name"]?.ToString() ?? "";
                                string deviceID = usbDevice["DeviceID"]?.ToString() ?? "";

                                if (deviceName.IndexOf("printer", StringComparison.OrdinalIgnoreCase) < 0 && 
                                    deviceName.IndexOf("printing", StringComparison.OrdinalIgnoreCase) < 0)
                                    continue;

                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"      - USB 프린팅 장치 후보 발견: '{deviceName}' (DeviceID: '{deviceID}')");

                                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^USB\\VID_.*\\(.+)$");
                                System.Text.RegularExpressions.Match match = regex.Match(deviceID);

                                if (!match.Success)
                                    continue;

                                string serial = match.Groups[1].Value;
                                string baseName = deviceName.Split('(')[0].Trim();

                                using (ManagementObjectSearcher printerSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer"))
                                {
                                    foreach (ManagementObject printer in printerSearcher.Get())
                                    {
                                        string matchedPrinterName = printer["Name"]?.ToString() ?? "";
                                        string matchedPort = printer["PortName"]?.ToString() ?? "";

                                        if (matchedPrinterName.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            string normalizedMatchedName = RemoveParenthesesFromPrinterName(matchedPrinterName);

                                            if (normalizedMatchedName.Equals(normalizedPrinterName, StringComparison.OrdinalIgnoreCase) &&
                                                matchedPort.Equals(printerPort, StringComparison.OrdinalIgnoreCase))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[GetSerialFromWmi] 매칭 성공: 프린터='{matchedPrinterName}', 포트='{matchedPort}', 시리얼='{serial}'");
                                                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"      => [WMI 매칭 성공] 프린터='{matchedPrinterName}', 포트='{matchedPort}', 시리얼='{serial}'");
                                                return serial;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetSerialFromWmi] USB 디바이스 처리 중 오류: {ex.Message}");
                            BarcodePrint.Model.PrinterSerialLogger.WriteLog($"      [GetSerialFromWmi 에러] USB 장치 처리 오류: {ex.Message}");
                            continue;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[GetSerialFromWmi] 시리얼 번호를 찾을 수 없음: 프린터='{printerName}', 포트='{printerPort}'");
                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"    [GetSerialFromWmi] 매칭되는 시리얼 번호를 찾지 못함");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetSerialFromWmi] 오류: {ex.Message}");
                BarcodePrint.Model.PrinterSerialLogger.WriteLog($"    [GetSerialFromWmi 치명적 에러] {ex.Message}");
                return string.Empty;
            }
        }

        #region 바코드 미리보기 이벤트 핸들러

        /// <summary>
        /// TextBox 실시간 입력 변경 이벤트 (미리보기 업데이트)
        /// </summary>
        /// <summary>
        /// 수동 직접 입력 시 텍스트박스 입력값 변경 이벤트 핸들러
        /// </summary>
        private void TextBox_InputText_TextChanged(object sender, EventArgs e)
        {
            // 자동 지우기 또는 포맷팅 중이면 미리보기를 변경하지 않음
            if (isAutoClearingTextBox)
            {
                return;
            }

            // 가격 입력란(TextBox_InputText)인 경우 실시간 가격 포맷팅(원화/쉼표) 적용 (텍스트 입력방식으로 변경되면서 제거)
            /*
            if (sender == TextBox_InputText)
            {
                ApplyPriceFormat();
                return;
            }
            */

            // 일반적인 변경 시 미리보기 갱신
            OnPreviewEvent();
        }

        /// <summary>
        /// 가격 입력 컨트롤에 실시간 가격 포맷(₩ 기호 및 천단위 자릿수 구분) 적용
        /// </summary>
        private void ApplyPriceFormat()
        {
            try
            {
                string currentText = TextBox_InputText.Text;
                if (string.IsNullOrWhiteSpace(currentText) || currentText.StartsWith("예:", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // 숫자만 추출 (원화 기호, 쉼표 제거)
                string numericValue = BarcodePrint.Model.PriceFormatHelper.ExtractNumericValue(currentText);

                if (string.IsNullOrWhiteSpace(numericValue))
                {
                    isAutoClearingTextBox = true;
                    TextBox_InputText.Text = "";
                    isAutoClearingTextBox = false;
                    OnPreviewEvent();
                    return;
                }

                // 가격 포맷 적용 (₩ 기호 + 천단위 구분)
                string formattedPrice = BarcodePrint.Model.PriceFormatHelper.FormatPrice(numericValue);

                if (formattedPrice != currentText)
                {
                    isAutoClearingTextBox = true;
                    TextBox_InputText.Text = formattedPrice;
                    isAutoClearingTextBox = false;

                    // 커서 위치를 맨 우측 끝으로 이동
                    TextBox_InputText.SelectionStart = formattedPrice.Length;
                }

                OnPreviewEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyPriceFormat 오류: {ex.Message}");
            }
        }



        // 트랙바 이벤트 핸들러는 ColumnSettingsForm으로 이동됨
        /*
        private void trackBar_BarcodeSize_Scroll(object sender, EventArgs e) { }
        private void trackBar_BarcodeX_Scroll(object sender, EventArgs e) { }
        private void trackBar_BarcodeY_Scroll(object sender, EventArgs e) { }
        */



        /// <summary>
        /// AdjustForm 열기 버튼 클릭 이벤트 (기존 고급 설정)
        /// </summary>
        private void panel_OpenAdjustForm_Click(object sender, EventArgs e)
        {
            try
            {
                if (adjustForm == null || adjustForm.IsDisposed)
                {
                    adjustForm = new AdjustForm(this);
                }
                adjustForm.StartPosition = FormStartPosition.CenterParent;
                adjustForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdjustForm 열기 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 레이아웃 설정 열기 버튼 클릭 이벤트
        /// </summary>
        private void panel_OpenLayoutForm_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] LabelLayoutSettingsForm 열기");
                
                // 메인윈도우의 최신 용지 사이즈를 항상 반영하여 레이아웃 설정 폼을 새로 생성
                if (layoutSettingsForm != null && !layoutSettingsForm.IsDisposed)
                {
                    layoutSettingsForm.Close();
                    layoutSettingsForm.Dispose();
                    layoutSettingsForm = null;
                }
                
                layoutSettingsForm = new LabelLayoutSettingsForm(this);
                layoutSettingsForm.StartPosition = FormStartPosition.CenterParent;
                layoutSettingsForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] LabelLayoutSettingsForm 생성/표시 오류: {ex.Message}");
                MessageBox.Show($"레이아웃 설정 창을 열 수 없습니다.\n{ex.Message}", "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 레이아웃 설정 열기 버튼 클릭 이벤트
        /// </summary>

         private void panel_OpenAdjustForm_MouseDown(object sender, MouseEventArgs e)
         {
             if (e.Button == MouseButtons.Left)
             {
                 new_OpenAdjustFormButton(0.8f); // 눌린 상태 효과
             }
         }

         private void panel_OpenAdjustForm_MouseUp(object sender, MouseEventArgs e)
         {
             new_OpenAdjustFormButton(1f);
         }

         private void panel_OpenAdjustForm_MouseEnter(object sender, EventArgs e)
         {
             new_OpenAdjustFormButton(1.1f); // 호버 효과
         }

         private void panel_OpenAdjustForm_MouseLeave(object sender, EventArgs e)
         {
             new_OpenAdjustFormButton(1f);
         }

        // 레이아웃 설정 버튼 이벤트들
        private void panel_OpenLayoutForm_Paint(object sender, PaintEventArgs e)
        {
            new_OpenLayoutFormButton();
        }

        private void panel_OpenLayoutForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                new_OpenLayoutFormButton(0.8f);
            }
        }

        private void panel_OpenLayoutForm_MouseUp(object sender, MouseEventArgs e)
        {
            new_OpenLayoutFormButton(1f);
        }

        private void panel_OpenLayoutForm_MouseEnter(object sender, EventArgs e)
        {
            new_OpenLayoutFormButton(1.1f);
        }

        private void panel_OpenLayoutForm_MouseLeave(object sender, EventArgs e)
        {
            new_OpenLayoutFormButton(1f);
        }

         /// <summary>
         /// 엑셀 연동 버튼 클릭 이벤트
         /// </summary>
         private void panel_OpenExcelForm_Click(object sender, EventArgs e)
         {
             // 엑셀 파일 열기 대화상자
             OpenExcelFileDlg();
             
             // 포커스 자동 이동 처리
             OnControlStateChanged(sender, e);
         }

         private void panel_OpenExcelForm_MouseDown(object sender, MouseEventArgs e)
         {
             if (e.Button == MouseButtons.Left)
             {
                 new_OpenExcelFormButton(0.8f); // 눌린 상태 효과
             }
         }

         private void panel_OpenExcelForm_MouseUp(object sender, MouseEventArgs e)
         {
             new_OpenExcelFormButton(1f);
         }

         private void panel_OpenExcelForm_MouseEnter(object sender, EventArgs e)
         {
             new_OpenExcelFormButton(1.1f); // 호버 효과
         }

         private void panel_OpenExcelForm_MouseLeave(object sender, EventArgs e)
         {
             new_OpenExcelFormButton(1f);
         }

        // 새로운 엑셀 출력 버튼 이벤트 핸들러들
        private void panel_ExcelPrintButton_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[엑셀 출력 버튼 클릭] 시작");
                System.Diagnostics.Debug.WriteLine($"  - 버튼 활성화 상태: {panel_PrintButton.Enabled}");
                System.Diagnostics.Debug.WriteLine($"  - isExcelMode: {isExcelMode}");
                System.Diagnostics.Debug.WriteLine($"  - excelData null 여부: {excelData == null}");
                System.Diagnostics.Debug.WriteLine($"  - excelData 행 수: {excelData?.Rows.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"  - useMultiColumnMode: {useMultiColumnMode}");
                System.Diagnostics.Debug.WriteLine($"  - multiColumnSettings 수: {multiColumnSettings?.Count ?? 0}");
                
                // 버튼이 비활성화되어 있으면 종료
                if (!panel_PrintButton.Enabled)
                {
                    System.Diagnostics.Debug.WriteLine($"[엑셀 출력 실패] 버튼이 비활성화 상태입니다.");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    return;
                }
                
                // 엑셀 모드가 활성화된 경우에만 출력 실행
                if (isExcelMode && excelData != null)
                {
                    if (useMultiColumnMode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[다중 컬럼 모드] PrintAllExcelDataMultiColumn 호출");
                        PrintAllExcelDataMultiColumn();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[단일 모드] PrintAllExcelData 호출");
                        PrintAllExcelData();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[엑셀 출력 실패] 조건 미충족");
                    MessageBox.Show("엑셀 데이터가 로드되지 않았습니다.\n먼저 엑셀 연동을 활성화하고 파일을 선택해주세요.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[엑셀 출력 버튼 클릭 오류] {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
            finally
            {
                // 포커스 자동 이동 처리
                OnControlStateChanged(sender, e);
            }
        }

        // panel_ExcelPrintButton 관련 이벤트 핸들러는 제거됨 (하나의 버튼만 사용)
                  
        // 바코드 타입 변경은 ExcelMultiColumnSelectionForm에서 처리하므로 제거됨
          
        /// <summary>
        /// 엑셀 모드 활성화/비활성화 (체크박스 제거로 인한 변경)
        /// </summary>
        private void ActivateExcelMode(bool enable)
        {
            if (enable)
            {
                // 엑셀 모드 활성화
                panel_OpenExcelForm.Enabled = true;
                new_OpenExcelFormButton(1f);
                isExcelMode = true;
                // 버튼 텍스트 업데이트
                panel_PrintButton.Invalidate();
            }
            else
            {
                // 엑셀 모드 비활성화
                panel_OpenExcelForm.Enabled = false;
                DrawDisabledExcelButton();
                excelFilePath = "";
                excelData = null;
                isExcelMode = false;
                currentBarcodeValue = "";
                currentBarcodeText = "";

                // 미리보기 초기화
                OnPreviewEvent();
                
                // 엑셀 파일명 라벨 숨기기
                UpdateExcelFileNameLabel();
                
                // 설정 저장
                SaveAppSettings();

                System.Diagnostics.Debug.WriteLine("엑셀 연동 비활성화됨");
            }

            // 포커스 자동 이동 처리
            OnControlStateChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// 프로그램 시작 시 엑셀 파일명 라벨 초기화
        /// </summary>
        private void InitializeExcelFileNameLabel()
        {
            // 컬럼명 라벨 초기화
            ClearColumnLabels();
            
            // 프로그램 시작 시 엑셀 연동 체크박스 상태에 관계없이 파일명 라벨 숨기기
            // 실제 연결된 데이터가 있을 때만 표시하도록 함
            bool hasValidExcelData = isExcelMode && !string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath) && excelData != null;
            
            if (hasValidExcelData)
            {
                string fileName = Path.GetFileName(excelFilePath);
                label_ExcelFileName.Text = $"연동 파일: {fileName}";
                label_ExcelFileName.Visible = true;
            }
            else
            {
                label_ExcelFileName.Text = "";
                label_ExcelFileName.Visible = false;
            }
            
            System.Diagnostics.Debug.WriteLine($"파일명 라벨 초기화: isExcelMode={isExcelMode}, hasData={excelData != null}, visible={label_ExcelFileName.Visible}");
        }

        /// <summary>
        /// 엑셀 파일명 라벨 업데이트
        /// </summary>
        private void UpdateExcelFileNameLabel()
        {
            if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
            {
                string fileName = Path.GetFileName(excelFilePath);
                label_ExcelFileName.Text = $"연동 파일: {fileName}";
                label_ExcelFileName.Visible = true;
                System.Diagnostics.Debug.WriteLine($"[UpdateExcelFileNameLabel] 라벨 표시: Text='{label_ExcelFileName.Text}', Visible={label_ExcelFileName.Visible}");
            }
            else
            {
                label_ExcelFileName.Text = "";
                label_ExcelFileName.Visible = false;
                System.Diagnostics.Debug.WriteLine($"[UpdateExcelFileNameLabel] 라벨 숨김: excelFilePath='{excelFilePath}', FileExists={File.Exists(excelFilePath ?? "")}");
            }
        }
        
        /// <summary>
        /// 컬럼명 라벨 업데이트
        /// </summary>
        private void UpdateColumnLabels(List<ExcelMultiColumnSelectionForm.ColumnSetting> settings)
        {
            try
            {
                var labels = new[] { label_Column4, label_Column1, label_Column3, label_Column5 }; // 가격표용: 컬럼1(0), 컬럼2(1), 컬럼3(2), 컬럼4(3) 순서 맞춤
                string[] defaultLabels = { "컬럼1", "컬럼2", "컬럼3", "컬럼4" };
                
                // 먼저 모든 라벨 초기화
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].Text = $"{defaultLabels[i]}:";
                }
                
                // 활성화된 컬럼명 표시
                foreach (var setting in settings)
                {
                    if (setting.ColumnIndex >= 0 && setting.ColumnIndex < labels.Length)
                    {
                        if (setting.IsEnabled && !string.IsNullOrWhiteSpace(setting.ColumnName))
                        {
                            labels[setting.ColumnIndex].Text = $"{setting.ColumnName}:";
                        }
                        else
                        {
                            labels[setting.ColumnIndex].Text = $"{defaultLabels[setting.ColumnIndex]}:";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"컬럼 라벨 업데이트 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 컬럼명 라벨 초기화
        /// </summary>
        private void ClearColumnLabels()
        {
            try
            {
                // 가격표용 라벨 텍스트 (엑셀 모드)
                label_Column4.Text = "컬럼1:";  // 컬럼 0
                label_Column1.Text = "컬럼2:";  // 컬럼 1
                label_Column3.Text = "컬럼3:";  // 컬럼 2
                label_Column5.Text = "컬럼4:";  // 컬럼 3 (label_Column2 대신 label_Column5 사용)
                label_Column5.Visible = true;          // 바코드 라벨 표시
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"컬럼 라벨 초기화 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 회사 링크 라벨 클릭 이벤트 - 웹 브라우저로 링크 열기
        /// </summary>
        private void label_companyLink_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[회사 링크 클릭] URL: {CompanyLink}");
                
                // 기본 웹 브라우저로 링크 열기
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = CompanyLink,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"링크 열기 오류: {ex.Message}");
                MessageBox.Show($"링크를 열 수 없습니다.\n{CompanyLink}", "알림", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// 모든 TextBox 내용 삭제
        /// </summary>
        private void ClearAllTextBoxes()
        {
            try
            {
                TextBox_InputText4.Text = "";      // 컬럼1
                TextBox_InputText.Text = "";       // 컬럼2
                TextBox_InputText3.Text = "";      // 컬럼3
                TextBox_InputText2.Text = "";      // 바코드
                // TextBox_InputText5는 명찰용으로 제거됨
                // TextBox_InputText5.Text = "";      // 전화번호 (Y=420)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TextBox 내용 삭제 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// PRINT_INFO_CLASS에서 바코드 타입을 읽어와 컬럼 설정에 적용
        /// </summary>
        private void ApplyBarcodeTypeFromSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[바코드 타입 적용] 시작");
                
                // PRINT_INFO_CLASS에서 바코드 타입 불러오기
                string savedBarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType;
                
                // 전역 설정이 없으면 현재 컬럼 설정(인덱스 3)에서 가져오기 시도
                if (string.IsNullOrEmpty(savedBarcodeType) && columnPositionSettings != null && columnPositionSettings.Count > 3)
                {
                    savedBarcodeType = columnPositionSettings[3].BarcodeType;
                    System.Diagnostics.Debug.WriteLine($"  - 전역 설정 없음, 컬럼 3에서 가져옴: {savedBarcodeType}");
                }

                if (string.IsNullOrEmpty(savedBarcodeType))
                {
                    savedBarcodeType = "QR_CODE"; // 여전히 없으면 기본값
                }
                
                PRINT_INFO_CLASS.PrinterInfo.BarcodeType = savedBarcodeType; // 전역 상태 동기화
                System.Diagnostics.Debug.WriteLine($"  - 최종 적용 바코드 타입: {savedBarcodeType}");
                
                // 모든 바코드 컬럼의 BarcodeType 업데이트
                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    int updatedCount = 0;
                    foreach (var setting in columnPositionSettings)
                    {
                        // 명찰용: UsageType이 "바코드"인 컬럼만 업데이트 (사용자가 텍스트로 변경한 경우 유지)
                        if (setting.UsageType == "바코드")
                        {
                            setting.BarcodeType = savedBarcodeType;
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"  - 컬럼 {setting.ColumnIndex} 업데이트: UsageType={setting.UsageType}, BarcodeType={setting.BarcodeType}");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"  - 총 {updatedCount}개 컬럼 업데이트됨");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  - ⚠ columnPositionSettings가 비어있음");
                }
                
                // multiColumnSettings도 있으면 업데이트
                if (multiColumnSettings != null && multiColumnSettings.Count > 0)
                {
                    int updatedCount = 0;
                    foreach (var setting in multiColumnSettings)
                    {
                        // 명찰용: UsageType이 "바코드"인 컬럼만 업데이트
                        if (setting.UsageType == "바코드")
                        {
                            setting.BarcodeType = savedBarcodeType;
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"  - multiColumnSettings 컬럼 {setting.ColumnIndex} 업데이트: UsageType={setting.UsageType}, BarcodeType={setting.BarcodeType}");
                        }
                    }
                    if (updatedCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - multiColumnSettings {updatedCount}개 컬럼 업데이트됨");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[바코드 타입 적용 완료]");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[바코드 타입 적용 오류] {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 저장된 다중 컬럼 설정 복원
        /// </summary>
        private void RestoreMultiColumnSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 복원] 시작");
                
                // LoadMultiColumnSettings를 사용하여 상세 설정 복원
                var loadedSettings = LoadMultiColumnSettings();
                
                if (loadedSettings != null && loadedSettings.Count > 0)
                {
                    multiColumnSettings = loadedSettings;
                    System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 복원] 완료 - {multiColumnSettings.Count}개 컬럼");
                    
                    // 복원된 설정 로그 출력
                    foreach (var setting in multiColumnSettings)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 컬럼 {setting.ColumnIndex}: {setting.ColumnName}, UsageType={setting.UsageType}, BarcodeType={setting.BarcodeType}, ExcelColumnNumber={setting.ExcelColumnNumber}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 컬럼 설정 없음");
                }
                
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 복원 오류] {ex.Message}");
                multiColumnSettings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();
            }
        }
        
        /// <summary>
        /// 통합 설정 로드 (엑셀 + 주소 통합)
        /// </summary>
        private void LoadAppSettings()
        {
            try
            {
                appSettings.Load();

                // 다중 컬럼 설정 복원 (엑셀 파일 로드 전에 먼저 복원)
                RestoreMultiColumnSettings();

                // 엑셀 파일 경로 복원 및 자동 로드
                if (appSettings.IsExcelFileValid())
                {
                    excelFilePath = appSettings.ExcelFilePath;
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"[엑셀 파일 자동 연동] 시작");
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 경로: {excelFilePath}");
                    
                    // 파일이 실제로 존재하는지 확인
                    if (File.Exists(excelFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 파일 존재 확인됨, 자동 로드 시작");
                        
                        try
                        {
                            // 엑셀 데이터 자동 로드 (isAutoLoad = true로 메시지박스 표시 안 함)
                            LoadExcelData(excelFilePath, isAutoLoad: true);
                            System.Diagnostics.Debug.WriteLine($"  - ✓ 엑셀 파일 자동 로드 완료");
                            
                            // 자동 로드 완료 후 즉시 파일명 라벨 업데이트
                            if (!string.IsNullOrEmpty(excelFilePath))
                            {
                                UpdateExcelFileNameLabel();
                                System.Diagnostics.Debug.WriteLine($"  - ✓ 파일명 라벨 업데이트 완료");
                            }
                            
                            // 엑셀 파일 재로드 후 미리보기 업데이트 보장
                            if (excelData != null && excelData.Rows.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"  - ✓ 엑셀 데이터로 미리보기 업데이트 시작");
                                UpdatePreviewWithFirstRow();
                                System.Diagnostics.Debug.WriteLine($"  - ✓ 엑셀 데이터로 미리보기 업데이트 완료");
                            }
                        }
                        catch (Exception loadEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - ✗ 엑셀 파일 자동 로드 실패: {loadEx.Message}");
                            // 실패해도 파일명은 표시 (사용자가 확인 가능)
                            if (!string.IsNullOrEmpty(excelFilePath))
                            {
                                UpdateExcelFileNameLabel();
                                System.Diagnostics.Debug.WriteLine($"  - ⚠ 로드 실패했지만 파일명은 표시");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  - ⚠ 파일이 존재하지 않음, 경로 초기화");
                        excelFilePath = "";
                        appSettings.ExcelFilePath = "";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[엑셀 파일 자동 연동] 완료");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                }
                
                // 엑셀 모드 상태 설정
                isExcelMode = appSettings.IsExcelModeEnabled;
                panel_OpenExcelForm.Enabled = appSettings.IsExcelModeEnabled;
                panel_PrintButton.Enabled = isSerialMatched && (appSettings.IsExcelModeEnabled || !isExcelMode); // 직접 출력 모드에서는 항상 활성화
                panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트
                
                // 파일명 라벨 업데이트 (파일 경로가 있으면 표시, 데이터 로드 여부와 무관)
                if (appSettings.IsExcelModeEnabled && !string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
                {
                    UpdateExcelFileNameLabel();
                    System.Diagnostics.Debug.WriteLine($"[LoadAppSettings] 파일명 라벨 업데이트 호출: {Path.GetFileName(excelFilePath)} (HasData={excelData != null})");
                }
                else
                {
                    label_ExcelFileName.Text = "";
                    label_ExcelFileName.Visible = false;
                    System.Diagnostics.Debug.WriteLine($"[LoadAppSettings] 파일명 라벨 숨김: IsExcelMode={appSettings.IsExcelModeEnabled}, HasPath={!string.IsNullOrEmpty(excelFilePath)}, FileExists={File.Exists(excelFilePath ?? "")}");
                }
                
                // Title 스위치 및 텍스트박스 초기화
                if (touchSwitch_Title != null && textBox_Title != null)
                {
                    // 저장된 TitleEnabled 상태 불러오기
                    touchSwitch_Title.Checked = appSettings.TitleEnabled;
                    
                    // SenderReceiverLabel에 저장된 공통문구 텍스트 복원 (비어있거나 "표시없음"이면 기본값 적용)
                    if (!string.IsNullOrWhiteSpace(appSettings.SenderReceiverLabel) && appSettings.SenderReceiverLabel != "표시없음")
                    {
                        textBox_Title.Text = appSettings.SenderReceiverLabel;
                    }
                    else
                    {
                        textBox_Title.Text = "공통문구";
                    }
                    
                    textBox_Title.Enabled = touchSwitch_Title.Checked;
                }

                // 마지막 작업 텍스트 입력값 복원
                if (!string.IsNullOrEmpty(appSettings.LastProductName))
                    TextBox_InputText4.Text = appSettings.LastProductName;
                if (!string.IsNullOrEmpty(appSettings.LastPrice))
                    TextBox_InputText.Text = appSettings.LastPrice;
                if (!string.IsNullOrEmpty(appSettings.LastBarcode))
                    TextBox_InputText2.Text = appSettings.LastBarcode;
                if (!string.IsNullOrEmpty(appSettings.LastOption))
                    TextBox_InputText3.Text = appSettings.LastOption;
                
                numericUpDown_PrintCopies.Value = Math.Max(1, Math.Min(9999, appSettings.LastPrintCopies));
                
                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 완료: ExcelMode={isExcelMode}, SenderReceiverLabel={appSettings.SenderReceiverLabel}");
                
                // Title 설정 후 미리보기 즉시 업데이트는 MainForm_Load에서 처리
                // (SetReceiveFunction이 호출된 후에 previewContentEvent가 연결되므로)
                System.Diagnostics.Debug.WriteLine($"[Title 초기화] touchSwitch_Title.Checked={touchSwitch_Title?.Checked}, textBox_Title.Text='{textBox_Title?.Text}', GetSenderReceiverLabel()='{GetSenderReceiverLabel()}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 오류: {ex.Message}");
                
                // 예외 발생 시 기본 상태 설정
                isExcelMode = false;
                label_ExcelFileName.Text = "";
                label_ExcelFileName.Visible = false;
                panel_OpenExcelForm.Enabled = false;
                panel_PrintButton.Enabled = isSerialMatched; // 직접 출력 모드에서는 시리얼 매칭 상태에 따름
                panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트
                if (touchSwitch_Title != null && textBox_Title != null)
                {
                    touchSwitch_Title.Checked = false;
                    textBox_Title.Text = "";
                    textBox_Title.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 통합 설정 저장 (엑셀 + 주소 통합)
        /// </summary>
        public void SaveAppSettings()
        {
            try
            {
                if (!isInitialized) return;

                // 엑셀 설정
                appSettings.ExcelFilePath = excelFilePath;
                appSettings.IsExcelModeEnabled = isExcelMode;
                
                // Title 설정
                // Title On/Off 상태 및 텍스트 박스 내용 항상 저장 (스위치 상태와 무관하게 텍스트 내용 보존)
                if (touchSwitch_Title != null)
                {
                    appSettings.TitleEnabled = touchSwitch_Title.Checked;
                }
                
                if (textBox_Title != null && !string.IsNullOrWhiteSpace(textBox_Title.Text))
                {
                    appSettings.SenderReceiverLabel = textBox_Title.Text;
                }
                else
                {
                    appSettings.SenderReceiverLabel = "공통문구";
                }
                
                // 종료 및 저장 시점의 용지 상태 동기화 (구버전 덮어쓰기 방지)
                appSettings.PaperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                appSettings.PaperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                appSettings.BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
                appSettings.IsRotated = PRINT_INFO_CLASS.PrinterInfo.bPaperRotated;

                // 마지막 입력값 임시 보존 (수동 모드용)
                appSettings.LastProductName = TextBox_InputText4.Text ?? "";
                appSettings.LastPrice = TextBox_InputText.Text ?? "";
                appSettings.LastBarcode = TextBox_InputText2.Text ?? "";
                appSettings.LastOption = TextBox_InputText3.Text ?? "";
                appSettings.LastPrintCopies = (int)numericUpDown_PrintCopies.Value;
                
                appSettings.Save();
                
                System.Diagnostics.Debug.WriteLine($"통합 설정 저장 완료: ExcelMode={isExcelMode}, SenderReceiverLabel={appSettings.SenderReceiverLabel}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// TextBox에서 주소 데이터 가져오기
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 전화번호, 컬럼3
        /// UI 매핑: 주소1(TextBox4), 우편번호(TextBox2), 주소2(TextBox), 전화번호(TextBox5), 컬럼3(TextBox3)
        /// </summary>
        private AddressData GetAddressDataFromTextBoxes()
        {
            return new AddressData
            {
                Address = GetTextBoxValue(TextBox_InputText4),      // 주소1 (Y=210)
                Address2 = GetTextBoxValue(TextBox_InputText),      // 주소2 (Y=350)
                Name = GetTextBoxValue(TextBox_InputText3),         // 컬럼3 (Y=490)
                PhoneNumber = "",  // 전화번호는 명찰용으로 제거됨
                PostalCode = GetTextBoxValue(TextBox_InputText2),   // 우편번호 (Y=280)
                            CopyCount = 1
            };
        }

        /// <summary>
        /// 용지 타입별 컬럼 위치 데이터 저장 (단일 공통 설정 직접 기록)
        /// </summary>
        private void SavePaperTypeColumnPositionData(string paperType, string columnData)
        {
            try
            {
                appSettings.ColumnPositionData = columnData;
                appSettings.PaperTypeColumnPositionData = ""; // 기존 규격별 데이터 비우기
                appSettings.Save();
                System.Diagnostics.Debug.WriteLine($"[컬럼 위치 저장] 단일 공통 설정 기록 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[컬럼 위치 저장 오류] {ex.Message}");
            }
        }
        
        /// <summary>
        /// 용지 타입별 컬럼 위치 데이터 로드 (단일 공통 설정 사용)
        /// </summary>
        private string LoadPaperTypeColumnPositionDataFromString(string dataString, string paperType)
        {
            return appSettings.ColumnPositionData;
        }
        
        /// <summary>
        /// 용지 타입별 컬럼 위치 데이터 로드 (단일 공통 설정 사용)
        /// </summary>
        private string LoadPaperTypeColumnPositionData(string paperType)
        {
            return appSettings.ColumnPositionData;
        }
        
        /// <summary>
        /// 용지 타입별 Title 데이터 저장 (단일 공통 설정 직접 기록)
        /// </summary>
        public void SavePaperTypeTitleData(string paperType, int titleSize, int titleOffsetY)
        {
            try
            {
                appSettings.TitleSize = titleSize;
                appSettings.TitleOffsetY = titleOffsetY;
                appSettings.PaperTypeTitleData = ""; // 기존 규격별 데이터 비우기
                appSettings.Save();
                System.Diagnostics.Debug.WriteLine($"[Title 저장] 공통 설정 기록 완료: Size={titleSize}, OffsetY={titleOffsetY}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Title 저장 오류] {ex.Message}");
            }
        }
        
        /// <summary>
        /// 용지 타입별 Title 데이터 로드 (단일 공통 설정 사용)
        /// </summary>
        public (int size, int offsetY) LoadPaperTypeTitleData(string paperType)
        {
            return (appSettings.TitleSize, appSettings.TitleOffsetY);
        }

        /// <summary>
        /// AddressData를 TextBox에 설정
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 전화번호, 컬럼3
        /// UI 매핑: 주소1(TextBox4), 우편번호(TextBox2), 주소2(TextBox), 전화번호(TextBox5), 컬럼3(TextBox3)
        /// </summary>
        private void SetAddressDataToTextBoxes(AddressData addressData)
        {
            if (addressData == null) return;

            try
            {
                TextBox_InputText4.Text = addressData.Address ?? "";        // 주소1 (Y=210)
                TextBox_InputText2.Text = addressData.PostalCode ?? "";     // 우편번호 (Y=280)
                TextBox_InputText.Text = addressData.Address2 ?? "";        // 주소2 (Y=350)
                // TextBox_InputText5는 명찰용으로 제거됨
                // TextBox_InputText5.Text = addressData.PhoneNumber ?? "";    // 전화번호 (Y=420)
                TextBox_InputText3.Text = addressData.Name ?? "";           // 컬럼3 (Y=490)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주소 데이터 설정 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일에서 엑셀 데이터 로드 (설정 복원용)
        /// </summary>
        private void LoadExcelDataFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("엑셀 파일이 존재하지 않습니다.");
                    return;
                }

                // ClosedXML을 사용하여 엑셀 파일 로드
                using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트
                    
                    // DataTable로 변환
                    excelData = new DataTable();
                    
                    // 첫 번째 행을 헤더로 사용
                    var firstRow = worksheet.FirstRowUsed();
                    var lastRow = worksheet.LastRowUsed();
                    var lastColumn = worksheet.LastColumnUsed();
                    
                    if (firstRow != null && lastRow != null && lastColumn != null)
                    {
                        // 컬럼 추가
                        for (int col = 1; col <= lastColumn.ColumnNumber(); col++)
                        {
                            var cellValue = firstRow.Cell(col).GetString();
                            excelData.Columns.Add(string.IsNullOrEmpty(cellValue) ? $"Column{col}" : cellValue);
                        }
                        
                        // 데이터 행 추가
                        for (int row = 2; row <= lastRow.RowNumber(); row++)
                        {
                            DataRow dataRow = excelData.NewRow();
                            for (int col = 1; col <= lastColumn.ColumnNumber(); col++)
                            {
                                var cellValue = worksheet.Cell(row, col).GetString();
                                dataRow[col - 1] = cellValue ?? "";
                            }
                            excelData.Rows.Add(dataRow);
                        }
                        
                        isExcelMode = true;
                        
                        // 첫 번째 행 데이터를 미리보기에 표시 (저장된 컬럼 정보 사용)
                        UpdatePreviewWithFirstRow();
                        
                        System.Diagnostics.Debug.WriteLine($"엑셀 데이터 로드 완료: {excelData.Rows.Count}행, {excelData.Columns.Count}열, 바코드컬럼: {selectedBarcodeColumn}, 텍스트컬럼: {selectedTextColumn}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 데이터 로드 오류: {ex.Message}");
                excelData = null;
                isExcelMode = false;
            }
        }

        /// <summary>
        /// 드래그 앤 드롭 - 파일을 폼 위로 드래그할 때
        /// </summary>
        private void MainForm_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            try
            {
                // 파일이 드래그되고 있는지 확인
                if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                    
                    // 엑셀 파일인지 확인
                    if (files.Length > 0)
                    {
                        string filePath = files[0];
                        string extension = Path.GetExtension(filePath).ToLower();
                        
                        if (extension == ".xlsx" || extension == ".xlsm" || extension == ".xls" || extension == ".csv")
                        {
                            // 엑셀 파일이면 복사 아이콘 표시
                            e.Effect = System.Windows.Forms.DragDropEffects.Copy;
                            System.Diagnostics.Debug.WriteLine($"[드래그 앤 드롭] 엑셀 파일 감지: {filePath}");
                        }
                        else
                        {
                            // 엑셀 파일이 아니면 거부
                            e.Effect = System.Windows.Forms.DragDropEffects.None;
                        }
                    }
                }
                else
                {
                    e.Effect = System.Windows.Forms.DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[드래그 앤 드롭 Enter 오류] {ex.Message}");
                e.Effect = System.Windows.Forms.DragDropEffects.None;
            }
        }

        /// <summary>
        /// 드래그 앤 드롭 - 파일을 폼에 드롭했을 때
        /// </summary>
        private void MainForm_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                    
                    if (files.Length > 0)
                    {
                        string filePath = files[0];
                        string extension = Path.GetExtension(filePath).ToLower();
                        
                        // 엑셀 파일인지 다시 확인
                        if (extension == ".xlsx" || extension == ".xlsm" || extension == ".xls" || extension == ".csv")
                        {
                            System.Diagnostics.Debug.WriteLine($"[드래그 앤 드롭] 엑셀 파일 열기: {filePath}");
                            
                            excelFilePath = filePath;
                            
                            // 엑셀 파일 경로 저장
                            appSettings.ExcelFilePath = excelFilePath;
                            appSettings.IsExcelModeEnabled = true;
                            
                            // 마지막 사용한 폴더 경로 저장
                            appSettings.LastUsedFolder = Path.GetDirectoryName(excelFilePath);
                            
                            // 즉시 저장
                            appSettings.Save();
                            
                            System.Diagnostics.Debug.WriteLine($"========================================");
                            System.Diagnostics.Debug.WriteLine($"[드래그앤드롭 - 엑셀 파일 경로 저장]");
                            System.Diagnostics.Debug.WriteLine($"  - 파일 경로: {excelFilePath}");
                            System.Diagnostics.Debug.WriteLine($"  - 폴더 경로: {appSettings.LastUsedFolder}");
                            System.Diagnostics.Debug.WriteLine($"========================================");
                            
                            // 엑셀 데이터 로드
                            LoadExcelData(excelFilePath);
                            
                            // 엑셀 모드로 전환
                            if (excelData != null && excelData.Rows.Count > 0)
                            {
                                SwitchToExcelMode();
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("엑셀 파일(.xlsx, .xlsm, .csv)만 지원합니다.", "알림", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[드래그 앤 드롭 Drop 오류] {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                System.Windows.Forms.MessageBox.Show($"엑셀 파일 열기 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// MainForm 종료 시 설정 저장
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 정리 작업 수행
                CleanupBeforeExit();
                
                // 통합 설정 저장
                SaveAppSettings();
                SaveColumnPositionSettings();
                
                // 참조 프로젝트 방식: 바코드 설정은 별도 저장하지 않음
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프로그램 종료 시 설정 저장 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 컬럼 위치 설정 저장 (현재 용지 타입에 맞게 저장)
        /// </summary>
        public void SaveColumnPositionSettings()
        {
            try
            {
                if (!isInitialized) return;

                SyncBarcodeFieldsFromSettings();

                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    // 5개 컬럼이 아니면 저장하지 않음 (무결성 검사) - 명찰용
                    if (columnPositionSettings.Count != 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"[경고] 컬럼 설정이 {columnPositionSettings.Count}개입니다. 저장하지 않습니다.");
                        return;
                    }
                    
                    var lines = new List<string>();
                    foreach (var setting in columnPositionSettings)
                    {
                        lines.Add($"{setting.ColumnIndex}|{setting.IsEnabled}|{setting.Size}|{setting.OffsetX}|{setting.OffsetY}|{setting.UsageType}|{setting.BarcodeType}|{setting.ShowBarcodeText}|{setting.FontFamily}|{setting.IsBold}|{setting.BarcodeTextOffsetX}|{setting.BarcodeTextOffsetY}|{setting.BarcodeTextSize}|{setting.CustomWidth}|{setting.BarcodeHeight}|{setting.RotateAngle}|{setting.Alignment}");
                    }
                    
                    // 현재 용지 타입 가져오기
                    string currentPaperType = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "105x145(블랙마크)";
                    string columnData = string.Join(";", lines);
                    
                    // 용지 타입별 컬럼 위치 데이터 저장
                    SavePaperTypeColumnPositionData(currentPaperType, columnData);
                    
                    // 기존 호환성 유지 (기본 컬럼 위치 데이터도 저장)
                    appSettings.ColumnPositionData = columnData;
                    
                    // 종료 및 저장 시점의 용지 상태 동기화 (구버전 덮어쓰기 방지)
                    appSettings.PaperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                    appSettings.PaperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                    appSettings.BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
                    appSettings.IsRotated = PRINT_INFO_CLASS.PrinterInfo.bPaperRotated;
                    
                    // 파일에 저장 (PaperTypeColumnPositionData 포함)
                    appSettings.Save();
                    
                    System.Diagnostics.Debug.WriteLine($"[저장 완료] 용지 타입 '{currentPaperType}' 컬럼 설정 4개 저장됨");
                    System.Diagnostics.Debug.WriteLine($"[저장 완료] PaperTypeColumnPositionData 저장 여부 확인: {!string.IsNullOrEmpty(appSettings.PaperTypeColumnPositionData)}");
                    System.Diagnostics.Debug.WriteLine($"[저장 완료] PaperTypeColumnPositionData 길이: {appSettings.PaperTypeColumnPositionData?.Length ?? 0}");
                    
                    // 파일에 실제로 저장되었는지 확인 (다시 로드해서 검증)
                    AppSettings testSettings = new AppSettings();
                    testSettings.Load();
                    string savedInFile = testSettings.PaperTypeColumnPositionData;
                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 파일에서 불러온 PaperTypeColumnPositionData 길이: {savedInFile?.Length ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 메모리와 파일 데이터 일치: {appSettings.PaperTypeColumnPositionData == savedInFile}");
                    
                    // 특정 용지 타입 데이터 확인
                    string testLoadedData = LoadPaperTypeColumnPositionDataFromString(savedInFile, currentPaperType);
                    System.Diagnostics.Debug.WriteLine($"[저장 검증] '{currentPaperType}' 데이터 불러오기: {(string.IsNullOrEmpty(testLoadedData) ? "실패" : "성공")}");
                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 원본 데이터 길이: {columnData.Length}");
                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 불러온 데이터 길이: {testLoadedData?.Length ?? 0}");
                    if (!string.IsNullOrEmpty(testLoadedData))
                    {
                        System.Diagnostics.Debug.WriteLine($"[저장 검증] 원본 데이터 전체: {columnData}");
                        System.Diagnostics.Debug.WriteLine($"[저장 검증] 불러온 데이터 전체: {testLoadedData}");
                        bool isMatch = testLoadedData == columnData;
                        System.Diagnostics.Debug.WriteLine($"[저장 검증] 원본 데이터와 일치: {isMatch}");
                        
                        if (!isMatch)
                        {
                            // 차이점 찾기
                            int minLength = Math.Min(columnData.Length, testLoadedData.Length);
                            for (int i = 0; i < minLength; i++)
                            {
                                if (columnData[i] != testLoadedData[i])
                                {
                                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 차이점 발견 - 위치 {i}: 원본='{columnData[i]}', 불러온='{testLoadedData[i]}'");
                                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 원본 주변: ...{columnData.Substring(Math.Max(0, i-10), Math.Min(20, columnData.Length - Math.Max(0, i-10)))}...");
                                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 불러온 주변: ...{testLoadedData.Substring(Math.Max(0, i-10), Math.Min(20, testLoadedData.Length - Math.Max(0, i-10)))}...");
                                    break;
                                }
                            }
                            if (columnData.Length != testLoadedData.Length)
                            {
                                System.Diagnostics.Debug.WriteLine($"[저장 검증] 길이 차이: 원본={columnData.Length}, 불러온={testLoadedData.Length}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[저장 검증] ✗ 데이터를 불러올 수 없음");
                        System.Diagnostics.Debug.WriteLine($"[저장 검증] 파일 내용 일부: {savedInFile.Substring(0, Math.Min(200, savedInFile.Length))}...");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainForm 컬럼 위치 설정 저장 오류: {ex.Message}");
            }
        }
        

        
        /// <summary>
        /// 컬럼 위치 설정 불러오기 (현재 용지 타입에 맞는 데이터 로드)
        /// </summary>
        public void LoadColumnPositionSettings()
        {
            try
            {
                // 파일에서 최신 설정 다시 로드 (다른 곳에서 저장했을 수 있음)
                appSettings.Load();
                System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] AppSettings 다시 로드 완료");
                
                // 현재 용지 타입 가져오기
                string currentPaperType = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "105x145(블랙마크)";
                System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 현재 용지 타입: {currentPaperType}");
                
                // 용지 타입별 데이터에서 먼저 로드 시도
                System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 저장된 PaperTypeColumnPositionData 길이: {appSettings.PaperTypeColumnPositionData?.Length ?? 0}");
                if (!string.IsNullOrEmpty(appSettings.PaperTypeColumnPositionData))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 저장된 데이터 일부: {appSettings.PaperTypeColumnPositionData.Substring(0, Math.Min(100, appSettings.PaperTypeColumnPositionData.Length))}...");
                }
                
                string columnData = LoadPaperTypeColumnPositionData(currentPaperType);
                System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 용지 타입별 데이터 로드: {(string.IsNullOrEmpty(columnData) ? "없음" : "있음")}");
                if (!string.IsNullOrEmpty(columnData))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 불러온 데이터 일부: {columnData.Substring(0, Math.Min(100, columnData.Length))}...");
                }
                
                // 용지 타입별 데이터가 없으면 기존 데이터 사용
                if (string.IsNullOrEmpty(columnData))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 용지 타입별 데이터 없음, 기본 ColumnPositionData 사용");
                    columnData = appSettings.ColumnPositionData;
                    System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] 기본 데이터 일부: {columnData?.Substring(0, Math.Min(100, columnData?.Length ?? 0)) ?? "null"}...");
                }
                
                bool needsReset = false;
                
                // 컬럼 위치 데이터 로드
                if (!string.IsNullOrEmpty(columnData))
                {
                    var lines = columnData.Split(';');
                    columnPositionSettings.Clear();
                    
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var setting = new ColumnPositionSetting
                            {
                                ColumnIndex = int.Parse(parts[0]),
                                IsEnabled = bool.Parse(parts[1]),
                                Size = int.Parse(parts[2]),
                                OffsetX = int.Parse(parts[3]),
                                OffsetY = int.Parse(parts[4]),
                                UsageType = (int.Parse(parts[0]) == 3) ? "바코드" : "텍스트", // 인덱스 3은 바코드 기본
                                BarcodeType = "QR_CODE", // 기본값
                                ShowBarcodeText = false // 기본값
                            };
                            
                            // UsageType, BarcodeType, ShowBarcodeText 추가 (8개 이상일 때)
                            if (parts.Length >= 8)
                            {
                                setting.UsageType = parts[5];
                                setting.BarcodeType = parts[6];
                                setting.ShowBarcodeText = bool.Parse(parts[7]);
                            }

                            // FontFamily, IsBold 추가 (10개 이상일 때)
                            if (parts.Length >= 10)
                            {
                                setting.FontFamily = parts[8];
                                setting.IsBold = bool.Parse(parts[9]);
                            }
                            // BarcodeTextOffsetX, BarcodeTextOffsetY, BarcodeTextSize 추가 (13개 이상일 때)
                            if (parts.Length >= 13)
                            {
                                setting.BarcodeTextOffsetX = int.Parse(parts[10]);
                                setting.BarcodeTextOffsetY = int.Parse(parts[11]);
                                setting.BarcodeTextSize = int.Parse(parts[12]);
                            }

                            // CustomWidth 추가 (14개 이상일 때)
                            if (parts.Length >= 14)
                            {
                                setting.CustomWidth = int.Parse(parts[13]);
                            }
                            if (parts.Length >= 15)
                            {
                                setting.BarcodeHeight = int.Parse(parts[14]);
                            }
                            if (parts.Length >= 16)
                            {
                                setting.RotateAngle = int.Parse(parts[15]);
                            }
                            if (parts.Length >= 17)
                            {
                                setting.Alignment = parts[16];
                            }
                            else
                            {
                                setting.Alignment = "Left";
                            }
                            
                            columnPositionSettings.Add(setting);
                        }
                    }
                    
                    // 5개 컬럼이 아니면 기본값으로 재설정 - 명찰용
                    if (columnPositionSettings.Count != 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"[경고] 컬럼 설정이 {columnPositionSettings.Count}개입니다. 5개로 초기화합니다.");
                        needsReset = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] 용지 타입 '{currentPaperType}'에서 {columnPositionSettings.Count}개 컬럼 불러옴");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] 용지 타입 '{currentPaperType}'에 데이터 없음 - 기본값으로 초기화");
                    needsReset = true;
                }
                
                // Title 설정도 용지 타입별로 로드
                var (titleSize, titleOffsetY) = LoadPaperTypeTitleData(currentPaperType);
                appSettings.TitleSize = titleSize;
                appSettings.TitleOffsetY = titleOffsetY;
                
                // Title 스위치 상태를 컬럼 4 IsEnabled에서 복원
                if (columnPositionSettings != null && columnPositionSettings.Count > 4)
                {
                    if (touchSwitch_Title != null)
                    {
                        touchSwitch_Title.Checked = columnPositionSettings[4].IsEnabled;
                        textBox_Title.Enabled = touchSwitch_Title.Checked;
                        System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] Title 스위치 복원: {touchSwitch_Title.Checked}");
                    }
                }

                if (needsReset)
                {
                    // 파일이 없을 때 기본값으로 초기화
                    columnPositionSettings.Clear();
                    
                    // 명찰용 기본 컬럼 설정 (현재 용지 크기에 비례하여 동적 생성)
                    var defaultSettings = GetDefaultColumnPositionSettings();
                    columnPositionSettings.AddRange(defaultSettings);
                    
                    System.Diagnostics.Debug.WriteLine($"[설정 초기화 완료] columnPositionSettings={columnPositionSettings.Count}개");
                    
                    // 초기화 후 즉시 저장
                    SaveColumnPositionSettings();
                }
                
                // 로드된 설정 정보 출력
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드 완료] 총 {columnPositionSettings.Count}개 컬럼");
                for (int i = 0; i < columnPositionSettings.Count; i++)
                {
                    var s = columnPositionSettings[i];
                    System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: Index={s.ColumnIndex}, Enabled={s.IsEnabled}, Size={s.Size}, X={s.OffsetX}, Y={s.OffsetY}, Type={s.UsageType}");
                }
                System.Diagnostics.Debug.WriteLine($"========================================");
                
                if (columnPositionSettings != null && columnPositionSettings.Count > 3)
                {
                    // 로드된 바코드 타입이 있으면 전역 상태에 반영
                    if (!string.IsNullOrEmpty(columnPositionSettings[3].BarcodeType))
                    {
                        PRINT_INFO_CLASS.PrinterInfo.BarcodeType = columnPositionSettings[3].BarcodeType;
                        System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] 전역 바코드 타입 동기화: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                    }
                }

                // 바코드 관련 변수 동기화 추가
                SyncBarcodeFieldsFromSettings();

                // 슬라이더 값 업데이트
                // 슬라이더바는 PositionAdjustmentForm으로 이동됨
                // 위치조절 폼이 열려있으면 값을 업데이트
                foreach (Form form in System.Windows.Forms.Application.OpenForms)
                {
                    if (form.Name == "PositionAdjustmentForm" && form is PositionAdjustmentForm positionForm)
                    {
                        // PositionAdjustmentForm의 LoadCurrentValues를 호출할 수 있도록
                        // 폼을 새로고침할 수 있는 메서드를 추가해야 할 수도 있음
                        // 현재는 폼이 열릴 때 자동으로 값을 로드하므로 별도 처리 불필요
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainForm 컬럼 위치 설정 불러오기 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 용지 타입 변경 시 컬럼 위치 정보 다시 로드
        /// </summary>
        public void ReloadColumnPositionSettingsForPaperType()
        {
            try
            {
                string currentPaperType = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "105x145(블랙마크)";
                System.Diagnostics.Debug.WriteLine($"[용지 타입 변경] 컬럼 위치 정보 다시 로드 시작 - 용지 타입: {currentPaperType}");
                
                // 파일에서 최신 설정 다시 로드 (다른 곳에서 저장했을 수 있음)
                appSettings.Load();
                System.Diagnostics.Debug.WriteLine($"[용지 타입 변경] AppSettings 다시 로드 완료");
                System.Diagnostics.Debug.WriteLine($"[용지 타입 변경] PaperTypeColumnPositionData 길이: {appSettings.PaperTypeColumnPositionData?.Length ?? 0}");
                
                LoadColumnPositionSettings();
                OnPreviewEvent();
                
                // PositionAdjustmentForm이 열려있으면 값을 업데이트
                foreach (Form form in System.Windows.Forms.Application.OpenForms)
                {
                    if (form.Name == "PositionAdjustmentForm" && form is PositionAdjustmentForm positionForm)
                    {
                        System.Diagnostics.Debug.WriteLine("[용지 타입 변경] 위치조절 폼 값 업데이트 시작");
                        positionForm.LoadCurrentValues();
                        System.Diagnostics.Debug.WriteLine("[용지 타입 변경] 위치조절 폼 값 업데이트 완료");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[용지 타입 변경] 컬럼 위치 정보 다시 로드 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입 변경 오류] 컬럼 위치 정보 로드 실패: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }
        
        // UpdateSlidersFromSettings 메서드는 PositionAdjustmentForm으로 이동됨

        /// <summary>
        /// AdjustForm이 닫힐 때 포커스 이동 처리
        /// </summary>
        private void AdjustForm_FormClosed(object sender, FormClosedEventArgs e)        
        {
              System.Diagnostics.Debug.WriteLine("[AdjustForm_FormClosed] AdjustForm 닫힘 - 바코드 타입 재적용");
              
              // AdjustForm에서 바코드 타입이 변경되었을 수 있으므로 다시 적용
              // PRINT_INFO_CLASS의 최신 바코드 타입을 모든 컬럼 설정에 적용
              ApplyBarcodeTypeFromSettings();
              
              // 컬럼 설정 저장 (multiColumnSettings도 함께 저장됨)
              SaveColumnPositionSettings();
              
              // 미리보기 업데이트
              OnPreviewEvent();
              
              // AdjustForm이 닫힌 후 포커스 자동 이동 처리
              OnControlStateChanged(sender, e);          
        }

         private ZXing.BarcodeFormat GetBarcodeFormat(string barcodeType)
         {
             switch (barcodeType)
             {
                 // 1D 바코드 (선형 바코드)
                 case "CODE_128":
                 case "CODE_128_A":
                 case "CODE_128_B":
                 case "CODE_128_C":
                     return ZXing.BarcodeFormat.CODE_128;
                 case "CODE_39":
                     return ZXing.BarcodeFormat.CODE_39;
                 case "CODE_93":
                     return ZXing.BarcodeFormat.CODE_93;
                 case "EAN_13":
                     return ZXing.BarcodeFormat.EAN_13;
                 case "EAN_8":
                     return ZXing.BarcodeFormat.EAN_8;
                 case "UPC_A":
                     return ZXing.BarcodeFormat.UPC_A;
                 case "UPC_E":
                     return ZXing.BarcodeFormat.UPC_E;
                 case "CODABAR":
                     return ZXing.BarcodeFormat.CODABAR;
                 case "ITF":
                     return ZXing.BarcodeFormat.ITF;
                 case "RSS_14":
                     return ZXing.BarcodeFormat.RSS_14;
                 case "RSS_EXPANDED":
                     return ZXing.BarcodeFormat.RSS_EXPANDED;
                 
                 // 2D 바코드 (매트릭스 바코드)
                 case "QR_CODE":
                     return ZXing.BarcodeFormat.QR_CODE;
                 case "DATA_MATRIX":
                     return ZXing.BarcodeFormat.DATA_MATRIX;
                 case "PDF_417":
                     return ZXing.BarcodeFormat.PDF_417;
                 case "AZTEC":
                     return ZXing.BarcodeFormat.AZTEC;
                 case "MAXICODE":
                     return ZXing.BarcodeFormat.MAXICODE;
                 
                 default:
                     return ZXing.BarcodeFormat.CODE_128; // 기본값을 CODE_128로 변경
             }
         }

         

         private void new_HomeButton(float shadowRate = 1f)
         {
             string ButtonText = "HOME";
             System.Drawing.Font TextFont = new System.Drawing.Font("페이퍼로지 5 Medium", 13, System.Drawing.FontStyle.Bold);

             // 음영 효과를 적용한 색상 결정
             Color originalColor = Color.Gray;
             Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

             using (Graphics g = panel_Home.CreateGraphics())
             {
                 g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                 using (GraphicsPath path = new GraphicsPath())
                 {
                     int r = 10;
                     int w = panel_Home.Width - 1, h = panel_Home.Height - 1;
                     path.AddArc(0, 0, r, r, 180, 90);
                     path.AddArc(w - r, 0, r, r, 270, 90);
                     path.AddArc(w - r, h - r, r, r, 0, 90);
                     path.AddArc(0, h - r, r, r, 90, 90);
                     path.CloseFigure();
                     // 부모 배경을 지우고 내부 채우기
                     using (SolidBrush brush = new SolidBrush(panelColor))
                     {
                         g.FillPath(brush, path);
                     }
                     // 중앙에 "HOME" 텍스트 그리기
                     using (SolidBrush textBrush = new SolidBrush(Color.White))
                     {
                         SizeF textSize = g.MeasureString(ButtonText, TextFont);
                         PointF textPosition = new PointF(
                             (panel_Home.Width - textSize.Width) / 2,
                             (panel_Home.Height - textSize.Height) / 2
                         );
                         g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                     }
                 }
             }
         }

         private void panel_Home_Paint(object sender, PaintEventArgs e)
         {
             new_HomeButton();
             return;
         }

         private void panel_Home_Click(object sender, EventArgs e)
         {
             try
             {
                 string printerName = PRINT_INFO_CLASS.PrinterInfo.printerName;
                 new BarcodePrintManager().SendPrintCommand(printerName, "HOME");
                 //MessageBox.Show("프린터에 HOME 명령어를 전송했습니다.", "알림");
             }
             catch (Exception ex)
             {
                 MessageBox.Show($"HOME 명령어 전송 실패: {ex.Message}", "오류");
             }
             
             // 포커스 자동 이동 처리
             OnControlStateChanged(sender, e);
         }
         
         /// <summary>
         /// HOME 버튼 더블클릭 시 컬럼 설정 초기화
         /// </summary>
         private void panel_Home_DoubleClick(object sender, EventArgs e)
         {
             // 더블클릭 초기화 기능 제거됨
             // 필요시 고급 설정에서 초기화 가능
         }

         private void panel_Home_MouseDown(object sender, MouseEventArgs e)
         {
             if (e.Button == MouseButtons.Left)
             {
                 new_HomeButton(0.8f); // 눌린 상태 효과
             }
         }

         private void panel_Home_MouseUp(object sender, MouseEventArgs e)
         {
             new_HomeButton(1f);
         }

         private void panel_Home_MouseEnter(object sender, EventArgs e)
         {
             new_HomeButton(1.1f); // 호버 효과
         }

         private void panel_Home_MouseLeave(object sender, EventArgs e)
         {
             new_HomeButton(1f);
         }





        #endregion

        // 미리보기 이벤트 관련 메서드들 (변경시에만 즉시 업데이트)
        public void OnPreviewEvent()
        {
            try
            {
                // 무한 루프 방지
                if (isPreviewEventProcessing)
                {
                    return;
                }

                // 타이머 없이 즉시 미리보기 업데이트 (변경시에만 호출)
                isPreviewEventProcessing = true;
                previewContentEvent?.Invoke(this, EventArgs.Empty);
                isPreviewEventProcessing = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnPreviewEvent 오류: {ex.Message}");
                isPreviewEventProcessing = false;
            }
        }


        private void ReceiveEventFontListUpdate(object sender, List<string> e)
        {
            this.fontlist = e;
        }

        private void ReceivePaint(object sender, EventArgs e) {
            Bitmap bitmap = ((PreviewManager.PaintEventArgs)e).bit;
            try {
                panel_BarcodePreview.Invoke(new Action(() =>
                {
                    // 새 이미지를 받으면 원본으로 저장하고 표시
                    UpdatePreviewImage(bitmap);
                }));
            } catch { }                           
        }      

        /// <summary>
        /// 새로운 미리보기 이미지 업데이트 (원본 이미지 저장 및 표시)
        /// </summary>
        private void UpdatePreviewImage(Bitmap newBitmap)
        {
            if (newBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePreviewImage] 이미지가 null");
                return;
            }
            
            try
            {
                // 원본 이미지 저장
                originalPreviewBitmap?.Dispose();
                originalPreviewBitmap = new Bitmap(newBitmap);
                
                // 확대 비율 초기화 (새 이미지가 오면 100%로 리셋)
                previewZoomFactor = 1.0f;
                
                System.Diagnostics.Debug.WriteLine($"[UpdatePreviewImage] 새 이미지 저장: {newBitmap.Width}x{newBitmap.Height}");
                
                // 현재 확대 비율로 표시
                DisplayPreviewWithZoom();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreviewImage 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 확대 비율로 미리보기 표시
        /// </summary>
        private void DisplayPreviewWithZoom()
        {
            if (panel_BarcodePreview == null || originalPreviewBitmap == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DisplayPreviewWithZoom] panel 또는 originalPreviewBitmap이 null");
                return;
            }

            try
            {
                // panel_BarcodePreview 크기에서 여백을 제외한 최대 사용 가능 크기
                int maxWidth = panel_BarcodePreview.Width - 20;
                int maxHeight = panel_BarcodePreview.Height - 20;
                
                // 유효성 검증
                if (maxWidth <= 0 || maxHeight <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DisplayPreviewWithZoom 중단] 패널 크기 부족: {maxWidth}x{maxHeight}");
                    return;
                }

                // 원본 이미지 비율 계산
                float aspectRatio = (float)originalPreviewBitmap.Width / originalPreviewBitmap.Height;
                float panelRatio = (float)maxWidth / maxHeight;

                // 패널에 맞는 기본 크기 계산
                int baseWidth, baseHeight;

                if (aspectRatio > panelRatio)
                {
                    baseWidth = maxWidth;
                    baseHeight = (int)(maxWidth / aspectRatio);
                }
                else
                {
                    baseHeight = maxHeight;
                    baseWidth = (int)(maxHeight * aspectRatio);
                }
                
                // 기본 크기 검증
                if (baseWidth <= 0 || baseHeight <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DisplayPreviewWithZoom 중단] 기본 크기 오류: {baseWidth}x{baseHeight}");
                    return;
                }

                // 확대 비율 적용
                int newWidth = (int)(baseWidth * previewZoomFactor);
                int newHeight = (int)(baseHeight * previewZoomFactor);
                
                // 크기 검증 (최소 10픽셀, 최대 5000픽셀)
                newWidth = Math.Max(5, Math.Min(5000, newWidth));
                newHeight = Math.Max(5, Math.Min(5000, newHeight));
                
                System.Diagnostics.Debug.WriteLine($"[DisplayPreviewWithZoom] 원본={originalPreviewBitmap.Width}x{originalPreviewBitmap.Height}, 기본={baseWidth}x{baseHeight}, 확대후={newWidth}x{newHeight}, 배율={previewZoomFactor:F2}");

                // 리사이즈된 비트맵 생성
                Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(resizedBitmap))
                {
                    g.Clear(System.Drawing.Color.LightCyan);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(originalPreviewBitmap, new Rectangle(0, 0, newWidth, newHeight));
                }

                // 패널에 이미지 설정
                panel_BarcodePreview.BackgroundImage?.Dispose();
                panel_BarcodePreview.BackgroundImage = resizedBitmap;
                panel_BarcodePreview.BackgroundImageLayout = ImageLayout.Center;

                // 이미지 저장
                SavePreviewImage(resizedBitmap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DisplayPreviewWithZoom 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  - 원본크기: {originalPreviewBitmap?.Width}x{originalPreviewBitmap?.Height}");
                System.Diagnostics.Debug.WriteLine($"  - 패널크기: {panel_BarcodePreview?.Width}x{panel_BarcodePreview?.Height}");
                System.Diagnostics.Debug.WriteLine($"  - 확대비율: {previewZoomFactor:F2}");
            }
        }

        // 이미지 저장 함수 (참조 프로그램 방식)
        private void SavePreviewImage(Bitmap bitmap)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LastPrintImage");
                string filePath = Path.Combine(folderPath, "LastPrint_Image.bmp");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 저장 중 오류 발생: {ex.Message}");
            }
        }

        // 바코드 설정 가져오기 메서드들
public string GetPrintData()
{
    // currentBarcodeValue가 있으면 우선 사용 (엑셀 연동 등)
    if (!string.IsNullOrWhiteSpace(currentBarcodeValue))
    {
        System.Diagnostics.Debug.WriteLine($"GetPrintData - currentBarcodeValue 사용: '{currentBarcodeValue}'");
        return currentBarcodeValue;
    }
    
    var inputText = TextBox_InputText.Text;
    
    // placeholder 텍스트는 빈 문자열 반환
    if (string.IsNullOrWhiteSpace(inputText) || 
        inputText == "값을 입력하세요." || 
        inputText.Contains("값을 입력하세요"))
    {
        System.Diagnostics.Debug.WriteLine($"GetPrintData - placeholder 텍스트 무시: '{inputText}'");
        return "";
    }
    
    System.Diagnostics.Debug.WriteLine($"GetPrintData - TextBox 입력 사용: '{inputText}'");
    return inputText;
}

        public int GetBarcodeSize()
        {
            return barcodeSize;
        }
        

        public int GetBarcodeX()
        {
            return barcodeX;
        }

        public int GetBarcodeY()
        {
            return barcodeY;
        }

        public int GetBarcodeHeight()
        {
            return barcodeHeight;
        }

        /// <summary>
        /// 다중 컬럼 모드인지 확인 (엑셀)
        /// </summary>
        public bool IsMultiColumnMode()
        {
            // 직접 출력 모드(isExcelMode=false)에서는 무조건 false 반환
            if (!isExcelMode)
                return false;
            
            return useMultiColumnMode && multiColumnSettings != null && multiColumnSettings.Count > 0;
        }
        
        /// <summary>
        /// 다중 입력 모드인지 확인 (수동 입력)
        /// </summary>
        public bool IsMultiInputMode()
        {
            return columnPositionSettings != null && columnPositionSettings.Count > 0;
        }
        
        /// <summary>
        /// 다중 입력 데이터 가져오기 (미리보기용)
        /// </summary>
        public List<MultiColumnData> GetMultiInputData()
        {
            var result = new List<MultiColumnData>();
            
            if (!IsMultiInputMode())
                return result;
            
            // 가격표용 입력 데이터 가져오기
            string productName = GetTextBoxValue(TextBox_InputText4); // 품명
            string price = GetTextBoxValue(TextBox_InputText);        // 가격
            string option = GetTextBoxValue(TextBox_InputText3);       // 옵션
            string barcodeValue = GetTextBoxValue(TextBox_InputText2); // 바코드
            
            // columnPositionSettings 인덱스와 매칭 (가격표용):
            // 인덱스 0: 품명 (TextBox_InputText4)
            // 인덱스 1: 가격 (TextBox_InputText)
            // 인덱스 2: 옵션 (TextBox_InputText3)
            // 인덱스 3: 바코드 (TextBox_InputText2)
            string commonText = "표시없음";
            var commonSetting = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == 4);
            if (commonSetting != null)
            {
                if (commonSetting.IsEnabled)
                {
                    commonText = !string.IsNullOrWhiteSpace(textBox_Title?.Text) ? textBox_Title.Text : (appSettings.SenderReceiverLabel ?? "공통문구");
                    if (commonText == "표시없음")
                    {
                        commonText = "공통문구";
                    }
                }
            }

            var textBoxInputs = new List<string>
            {
                productName,    // 인덱스 0: 품명
                price,          // 인덱스 1: 가격
                option,         // 인덱스 2: 옵션
                barcodeValue,   // 인덱스 3: 바코드
                commonText      // 인덱스 4: 공통문구 (추가)
            };
            
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[다중 입력 미리보기 데이터] 생성 시작 (가격표용)");
            System.Diagnostics.Debug.WriteLine($"  품명: '{productName}'");
            System.Diagnostics.Debug.WriteLine($"  가격: '{price}'");
            System.Diagnostics.Debug.WriteLine($"  비고: '{option}'");
            System.Diagnostics.Debug.WriteLine($"  바코드: '{barcodeValue}'");
            System.Diagnostics.Debug.WriteLine($"  컬럼 설정 개수: {columnPositionSettings.Count}");
            
            for (int i = 0; i < textBoxInputs.Count; i++)
            {
                var setting = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == i);
                if (setting == null) continue;
                var input = textBoxInputs[i];
                
                System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 체크] 인덱스 {i}: ColumnIndex={setting.ColumnIndex}, 입력값='{input}', IsEnabled={setting.IsEnabled}, UsageType={setting.UsageType}");
                
                // 바코드 컬럼(ColumnIndex==3)인 경우 값이 입력되어 있으면 IsEnabled와 관계없이 표시
                bool isBarcodeColumn = setting.ColumnIndex == 3;
                bool shouldInclude = setting.IsEnabled || (isBarcodeColumn && !string.IsNullOrEmpty(input));
                
                if (shouldInclude)
                {
                    // 바코드인 경우 값이 비어있으면 표시하지 않음
                    string valueToUse = input;
                    bool isBarcode = setting.UsageType == "바코드";
                    
                    if (isBarcode && string.IsNullOrEmpty(input))
                    {
                        System.Diagnostics.Debug.WriteLine($"[바코드 제외] 인덱스 {i}: 값이 비어있어 표시하지 않음");
                        continue; // 바코드 값이 없으면 이 컬럼 건너뛰기
                    }
                    
                    // 공통문구(Index 4): Title 비활성화("표시없음") 시 미리보기에 표시하지 않음
                    if (setting.ColumnIndex == 4 && valueToUse == "표시없음")
                    {
                        System.Diagnostics.Debug.WriteLine($"[공통문구 제외] 인덱스 4: 표시없음이므로 미리보기에서 제외");
                        continue;
                    }
                    
                    // 값이 있을 때만 추가 (바코드 컬럼은 값이 있으면 IsEnabled와 관계없이 포함)
                    if (!string.IsNullOrEmpty(valueToUse))
                    {
                        // 명찰용 - 상대좌표 사용
                        string[] columnNames = { "컬럼1", "컬럼2", "컬럼3", "컬럼4", "공통문구" };
                        string columnName = setting.ColumnIndex < columnNames.Length ? columnNames[setting.ColumnIndex] : $"컬럼 {setting.ColumnIndex}";
                        
                        // 바코드 컬럼도 ShowBarcodeText 설정 허용 (LabelLayoutSettingsForm에서 설정)
                        bool showBarcodeText = setting.ShowBarcodeText;
                        
                        result.Add(new MultiColumnData
                        {
                            Value = valueToUse,
                            IsBarcode = isBarcode, // 실제 판정된 타입을 사용
                            BarcodeType = setting.BarcodeType ?? "QR_CODE",
                            ColumnName = columnName,
                            Size = setting.Size,
                            BarcodeHeight = setting.BarcodeHeight, // 바코드 세로 크기 추가
                            OffsetX = setting.OffsetX,
                            OffsetY = setting.OffsetY,
                            ShowBarcodeText = setting.ShowBarcodeText,
                            BarcodeTextOffsetX = setting.BarcodeTextOffsetX,
                            BarcodeTextOffsetY = setting.BarcodeTextOffsetY,
                            BarcodeTextSize = setting.BarcodeTextSize,
                            FontFamily = setting.FontFamily,
                            IsBold = setting.IsBold,
                            CustomWidth = setting.CustomWidth,
                            RotateAngle = setting.RotateAngle,
                            Alignment = setting.Alignment
                        });
                        
                        System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 추가 ✓] 컬럼 {setting.ColumnIndex}: 값='{valueToUse}', 타입={setting.UsageType}, IsBarcode={isBarcode}, 크기={setting.Size}, X={setting.OffsetX}, Y={setting.OffsetY}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 제외 ✗] 인덱스 {i}: 값이 비어있음");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 제외 ✗] 인덱스 {i}: IsEnabled=false");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[다중 입력 미리보기 데이터] 생성 완료: {result.Count}개 컬럼");
            System.Diagnostics.Debug.WriteLine($"========================================");
            
            return result;
        }

        /// <summary>
        /// 엑셀에서 입력값으로 행 검색하여 출력
        /// </summary>
        private void SearchAndPrintExcelRow(List<string> searchValues)
        {
            // 이미 검색 중이면 무시 (중복 실행 방지)
            if (isSearchingExcel)
            {
                System.Diagnostics.Debug.WriteLine($"[엑셀 검색] 이미 검색 중이므로 무시");
                return;
            }
            
            try
            {
                // 검색 시작 플래그 설정
                isSearchingExcel = true;
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[엑셀 검색 및 출력] 시작 (플래그 설정)");
                
                if (excelData == null || excelData.Rows.Count == 0 || multiColumnSettings == null || multiColumnSettings.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[검증 실패] excelData 또는 설정이 없음");
                    
                    // 검색 종료 플래그 해제 (및 오류 시 메시지 표시 방지)
                    isSearchingExcel = false;
                    
                    // MessageBox 표시 전 포커스 제거
                    this.ActiveControl = null;
                    MessageBox.Show("엑셀 데이터가 로드되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // MessageBox 닫힌 후 이벤트 큐 처리 및 대기
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                    return;
                }
                
                // 각 TextBox 입력값과 매칭할 엑셀 컬럼 정보
                System.Diagnostics.Debug.WriteLine($"검색 조건 (총 {searchValues.Count}개 값):");
                for (int i = 0; i < searchValues.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  TextBox{i + 1}: '{searchValues[i]}' (비어있음: {string.IsNullOrWhiteSpace(searchValues[i])})");
                }
                
                // 유효한 검색 조건만 출력
                System.Diagnostics.Debug.WriteLine($"유효한 검색 조건:");
                int validConditionCount = 0;
                for (int i = 0; i < Math.Min(searchValues.Count, multiColumnSettings.Count); i++)
                {
                    if (!string.IsNullOrWhiteSpace(searchValues[i]))
                    {
                        var setting = multiColumnSettings.FirstOrDefault(s => s.ColumnIndex == i);
                        if (setting != null && setting.IsEnabled)
                        {
                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i} ({setting.ColumnName}): 검색값='{searchValues[i]}'");
                            validConditionCount++;
                        }
                    }
                }
                
                if (validConditionCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[경고] 유효한 검색 조건이 없습니다!");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    
                    // MessageBox 표시 전 포커스 제거 (엔터키 중복 방지)
                    this.ActiveControl = null;
                    
                    MessageBox.Show("검색할 값을 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // MessageBox 닫힌 후 이벤트 큐 처리 및 대기
                    System.Windows.Forms.Application.DoEvents();  // 대기 중인 이벤트 처리
                    System.Threading.Thread.Sleep(200);  // 충분한 대기 시간
                    return;
                }
                
                // 엑셀 데이터에서 일치하는 행 검색
                DataRow matchedRow = null;
                int matchedRowIndex = -1;
                
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[검색 시작] 총 {excelData.Rows.Count}개 행 검색");
                
                for (int rowIdx = 0; rowIdx < excelData.Rows.Count; rowIdx++)
                {
                    DataRow row = excelData.Rows[rowIdx];
                    bool isMatch = true;
                    int checkedConditions = 0;
                    
                    System.Diagnostics.Debug.WriteLine($"--- 행 {rowIdx} 검사 시작 ---");
                    
                    // 입력된 값이 있는 컬럼만 검색 조건으로 사용
                    for (int i = 0; i < Math.Min(searchValues.Count, multiColumnSettings.Count); i++)
                    {
                        string searchValue = searchValues[i];
                        if (string.IsNullOrWhiteSpace(searchValue))
                        {
                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: 검색값 없음 (건너뜀)");
                            continue; // 입력값이 없으면 해당 컬럼은 검색 조건에서 제외
                        }
                        
                        var setting = multiColumnSettings.FirstOrDefault(s => s.ColumnIndex == i);
                        if (setting != null && setting.IsEnabled)
                        {
                            // 변환된 컬럼 컬럼3 사용
                            string columnName = setting.UsageType == "바코드" ?
                                $"Barcode_{setting.ColumnIndex}_{setting.BarcodeType}" :
                                $"Text_{setting.ColumnIndex}";
                            
                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i} ({setting.ColumnName}): 변환된 컬럼3='{columnName}'");
                            
                            if (excelData.Columns.Contains(columnName))
                            {
                                string cellValue = row[columnName]?.ToString()?.Trim() ?? "";
                                string searchValueTrimmed = searchValue.Trim();
                                
                                checkedConditions++;
                                
                                bool matches = cellValue.Equals(searchValueTrimmed, StringComparison.OrdinalIgnoreCase);
                                System.Diagnostics.Debug.WriteLine($"    → 셀값='{cellValue}', 검색값='{searchValueTrimmed}', 일치={matches}");
                                
                                if (!matches)
                                {
                                    isMatch = false;
                                    System.Diagnostics.Debug.WriteLine($"    → 불일치! 다음 행으로");
                                    break;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"    → 경고: excelData에 '{columnName}' 컬럼이 없음");
                                isMatch = false;
                                break;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: setting 없음 또는 비활성화");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"--- 행 {rowIdx} 검사 완료: isMatch={isMatch}, 검사한 조건={checkedConditions}개 ---");
                    
                    if (isMatch && checkedConditions > 0)
                    {
                        matchedRow = row;
                        matchedRowIndex = rowIdx;
                        System.Diagnostics.Debug.WriteLine($"========================================");
                        System.Diagnostics.Debug.WriteLine($"✅ 일치하는 행 발견: 행 {rowIdx}");
                        System.Diagnostics.Debug.WriteLine($"========================================");
                        break; // 첫 번째 일치하는 행만 출력
                    }
                }
                
                if (matchedRow == null)
                {
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"❌ 일치하는 행을 찾지 못함 (총 {excelData.Rows.Count}개 행 검사 완료)");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                }
                
                if (matchedRow != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[검색 성공] 행 {matchedRowIndex + 1} 출력 시작");
                    
                    // 일치하는 행을 인쇄 큐에 추가
                    barcodePrintManager.AddMultiColumnToPrintQueue((DataTable)excelData, multiColumnSettings, matchedRowIndex);
                    
                    // 모든 TextBox 내용 삭제
                    ClearAllTextBoxes();
                    
                    // 검색 종료 플래그 해제
                    isSearchingExcel = false;
                    
                    // 다중 컬럼 인쇄 실행 (PrintStart 대신 Barcode_PrintGo 직접 호출)
                    barcodePrintManager.Barcode_PrintGo();
                    
                    System.Diagnostics.Debug.WriteLine($"[검색 성공] 인쇄 큐 처리 완료");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[검색 실패] 일치하는 행을 찾을 수 없음");
                    
                    // 검색 종료 플래그 해제
                    isSearchingExcel = false;
                    
                    // MessageBox 표시
                    this.ActiveControl = null;
                    MessageBox.Show("입력한 값과 일치하는 행을 찾을 수 없습니다.", "검색 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // MessageBox 닫힌 후 폼 자체로 포커스 이동 (TextBox 자동 포커스 방지)
                    this.Focus();
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[엑셀 검색 오류] {ex.Message}");
                isSearchingExcel = false;
                this.ActiveControl = null;
                MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // MessageBox 닫힌 후 폼 자체로 포커스 이동
                this.Focus();
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }
            finally
            {
                // 검색 종료 플래그 해제 (안전장치)
                if (isSearchingExcel)
                {
                    isSearchingExcel = false;
                }
            }
        }
        
        /// <summary>
        /// 다중 컬럼 데이터 가져오기 (미리보기용)
        /// </summary>
        public List<MultiColumnData> GetMultiColumnData()
        {
            var result = new List<MultiColumnData>();
            
            if (!IsMultiColumnMode() || excelData == null || excelData.Rows.Count == 0)
                return result;
            
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기 데이터 생성] 시작");
            
            // 현재 선택된 행(PreviewNum) 데이터 사용
            DataRow firstRow = excelData.Rows[PreviewNum];
            
            // 명찰용 컬럼 매핑 (단건 출력과 동일한 순서)
            // 엑셀 컬럼 인덱스와 columnPositionSettings 인덱스 매칭:
            // 엑셀 0 → columnPositionSettings[0] (컬럼1)
            // 엑셀 1 → columnPositionSettings[1] (컬럼2)
            // 엑셀 2 → columnPositionSettings[2] (컬럼3)
            // 엑셀 3 → columnPositionSettings[3] (바코드)
            
            string[] columnNames = { "컬럼1", "컬럼2", "컬럼3", "바코드", "공통문구" };
            
            // 명찰용 5개 컬럼 처리 (Index 0~4)
            for (int excelColIndex = 0; excelColIndex < 5; excelColIndex++)
            {
                // columnPositionSettings에서 위치 정보 가져오기 (물리 인덱스 대신 ColumnIndex 매칭으로 조회)
                var positionSetting = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == excelColIndex);
                if (positionSetting == null) continue;
                
                // 인덱스 4(공통문구)는 엑셀 설정이 없을 수 있으므로 직접 조회하거나 처리
                var excelSetting = multiColumnSettings.FirstOrDefault(s => s.ColumnIndex == excelColIndex && s.IsEnabled);
                
                // 공통문구(Index 4)는 엑셀 설정이 없어도 positionSetting이 있으면 처리 (수동 입력값 사용)
                if (excelSetting == null && excelColIndex != 4)
                    continue;
                
                // 바코드 컬럼인지 판단 (UsageType 설정을 그대로 따름)
                bool isBarcode = positionSetting.UsageType == "바코드";
                
                // 엑셀 데이터에서 값 가져오기
                // 바코드 컬럼인 경우 Barcode_ 형식으로 먼저 찾아보고, 없으면 Text_ 형식으로 찾기
                string columnName = null;
                string value = "";
                
                if (isBarcode)
                {
                    // 바코드 컬럼: 먼저 Barcode_ 형식으로 찾기
                    string barcodeColumnName = excelSetting != null ? $"Barcode_{excelSetting.ColumnIndex}_{positionSetting.BarcodeType ?? "QR_CODE"}" : "";
                    if (!string.IsNullOrEmpty(barcodeColumnName) && excelData.Columns.Contains(barcodeColumnName))
                    {
                        columnName = barcodeColumnName;
                        value = firstRow[columnName]?.ToString() ?? "";
                        System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 '{barcodeColumnName}' 찾음, 값='{value}'");
                    }
                    else if (excelSetting != null)
                    {
                        // Barcode_ 형식이 없으면 Text_ 형식으로 찾기
                        string textColumnName = $"Text_{excelSetting.ColumnIndex}";
                        if (excelData.Columns.Contains(textColumnName))
                        {
                            columnName = textColumnName;
                            value = firstRow[columnName]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 '{barcodeColumnName}' 없음, Text_ 형식 '{textColumnName}' 사용, 값='{value}'");
                        }
                    }
                }
                else if (excelColIndex == 4)
                {
                    // 공통문구: 에디터에서 활성화(IsEnabled)되어 있거나 스위치가 On인 경우 텍스트 적용
                    if (positionSetting.IsEnabled)
                    {
                        columnName = "공통문구";
                        value = !string.IsNullOrWhiteSpace(textBox_Title?.Text) ? textBox_Title.Text : (appSettings.SenderReceiverLabel ?? "공통문구");
                        if (value == "표시없음")
                        {
                            value = "공통문구";
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[공통문구 제외] 인덱스 4 IsEnabled=false 이므로 미리보기에서 제외");
                        continue;
                    }
                }
                else if (excelSetting != null)
                {
                    // 텍스트 컬럼: Text_ 형식으로 찾기
                    columnName = $"Text_{excelSetting.ColumnIndex}";
                    if (excelData.Columns.Contains(columnName))
                    {
                        string rawValue = firstRow[columnName]?.ToString() ?? "";
                        string combined = "";
                        string prefixSeparator = excelSetting.UsePrefixNewLine ? "\n" : " ";
                        string suffixSeparator = excelSetting.UseSuffixNewLine ? "\n" : " ";
                        if (!string.IsNullOrWhiteSpace(excelSetting.Prefix))
                        {
                            combined += excelSetting.Prefix.Trim() + prefixSeparator;
                        }
                        combined += rawValue;
                        if (!string.IsNullOrWhiteSpace(excelSetting.Suffix))
                        {
                            combined += suffixSeparator + excelSetting.Suffix.Trim();
                        }
                        value = combined;
                    }
                }

                // 가격 컬럼인 경우 가격 포맷 적용 (단위 구분 및 원화 기호) - 제거됨 (텍스트 표시로 수정)
                /*
                if ((excelColIndex == 1 || (columnName != null && BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(columnName))) && !string.IsNullOrWhiteSpace(value))
                {
                    value = BarcodePrint.Model.PriceFormatHelper.FormatPrice(value);
                }
                */
                
                if (excelColIndex != 4 && (string.IsNullOrEmpty(columnName) || !excelData.Columns.Contains(columnName)))
                {
                    System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 '{columnName ?? "null"}' 없음");
                    continue;
                }
                
                if (string.IsNullOrEmpty(value))
                {
                    System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 {excelColIndex} 값이 비어있음");
                    continue;
                }
                
                // 바코드 텍스트 표시 여부 설정 (설정값 그대로 사용)
                bool showBarcodeText = positionSetting.ShowBarcodeText;
                
                result.Add(new MultiColumnData
                {
                    Value = value,
                    IsBarcode = isBarcode,
                    BarcodeType = isBarcode ? (positionSetting.BarcodeType ?? "QR_CODE") : positionSetting.BarcodeType,
                    ColumnName = columnNames[excelColIndex],
                    Size = positionSetting.Size,
                    OffsetX = positionSetting.OffsetX,
                    OffsetY = positionSetting.OffsetY, 
                    ShowBarcodeText = showBarcodeText,
                    BarcodeTextOffsetX = positionSetting.BarcodeTextOffsetX,
                    BarcodeTextOffsetY = positionSetting.BarcodeTextOffsetY,
                    BarcodeTextSize = positionSetting.BarcodeTextSize,
                    FontFamily = positionSetting.FontFamily,
                    IsBold = positionSetting.IsBold,
                    CustomWidth = positionSetting.CustomWidth,
                    BarcodeHeight = positionSetting.BarcodeHeight,
                    RotateAngle = positionSetting.RotateAngle,
                    Alignment = positionSetting.Alignment
                });
                System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기] 컬럼 {excelColIndex} ({columnNames[excelColIndex]}): 값='{value}', IsBarcode={isBarcode}, 크기={positionSetting.Size}, X={positionSetting.OffsetX}, Y={positionSetting.OffsetY}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기 데이터 생성] 완료: {result.Count}개 컬럼");
            System.Diagnostics.Debug.WriteLine($"========================================");
            
            return result;
        }

        private void customCloseButton_Click(object sender, EventArgs e)
        {
            // 바로 종료
            this.Close();
        }

        /// <summary>
        /// 라벨 크기 정보 업데이트
        /// </summary>
        public void UpdateLabelSizeInfo()
        {
            try
            {
                // 실제 라벨 크기 (mm 단위)
                int originalWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                int originalHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                
                // label_LabelSize 업데이트
                if (label_LabelSize != null)
                {
                    label_LabelSize.Text = $"라벨 크기: {originalWidth}x{originalHeight}mm";
                }
                            
                
                // 디버그 정보 출력
                System.Diagnostics.Debug.WriteLine($"라벨 크기 업데이트: {originalWidth}mm x {originalHeight}mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"라벨 크기 업데이트 오류: {ex.Message}");
            }
        }

   
        private void OpenExcelFileDlg()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Excel Files (*.xlsx;*.xlsm;*.xls;*.csv)|*.xlsx;*.xlsm;*.xls;*.csv|Excel 2007+ (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|Excel 97-2003 (*.xls)|*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                openFileDialog.Title = "엑셀 파일 선택";
                
                // 마지막 사용한 폴더 경로 설정 (없으면 프로그램 실행 경로 사용)
                if (!string.IsNullOrEmpty(appSettings.LastUsedFolder) && Directory.Exists(appSettings.LastUsedFolder))
                {
                    openFileDialog.InitialDirectory = appSettings.LastUsedFolder;
                }
                else
                {
                    openFileDialog.InitialDirectory = System.Windows.Forms.Application.StartupPath;
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelFilePath = openFileDialog.FileName;
                    
                    // 엑셀 파일 경로 저장
                    appSettings.ExcelFilePath = excelFilePath;
                    appSettings.IsExcelModeEnabled = true;
                    
                    // 마지막 사용한 폴더 경로 저장
                    appSettings.LastUsedFolder = Path.GetDirectoryName(excelFilePath);
                    
                    // 즉시 저장
                    appSettings.Save();
                    
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"[엑셀 파일 경로 저장]");
                    System.Diagnostics.Debug.WriteLine($"  - 파일 경로: {excelFilePath}");
                    System.Diagnostics.Debug.WriteLine($"  - 폴더 경로: {appSettings.LastUsedFolder}");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    
                    LoadExcelData(excelFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 파일 열기 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 엑셀 데이터 로드
        /// </summary>
        private void LoadExcelData(string filePath, bool isAutoLoad = false)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".xlsx" || extension == ".xlsm")
                {
                    // Excel 파일의 경우 컬럼 선택 기능 사용
                    LoadExcelWithColumnSelection(filePath, isAutoLoad);
                }
                else if (extension == ".csv")
                {
                    // CSV 파일의 경우 기존 방식 사용
                    excelData = ReadExcelAsDataTable(filePath);
                    
                    if (excelData != null && excelData.Rows.Count > 0)
                    {
                        isExcelMode = true;
                        
                        // 마지막 페이지번호 입력값을 새로운 CSV 파일의 마지막 페이지로 변경
                        if (this.txtEndPage != null)
                        {
                            if (this.txtEndPage.InvokeRequired)
                            {
                                this.txtEndPage.Invoke(new Action(() => this.txtEndPage.Text = excelData.Rows.Count.ToString()));
                            }
                            else
                            {
                                this.txtEndPage.Text = excelData.Rows.Count.ToString();
                            }
                        }
                        
                        // 수동 로드 시에만 메시지박스 표시
                        if (!isAutoLoad)
                        {
                            MessageBox.Show($"CSV 파일이 성공적으로 로드되었습니다.\n총 {excelData.Rows.Count}개의 행이 있습니다.\n첫 번째 컬럼이 바코드로 사용됩니다.", 
                                "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        
                        // 디버그 정보 출력
                        System.Diagnostics.Debug.WriteLine($"CSV 파일 로드 완료: {filePath}");
                        System.Diagnostics.Debug.WriteLine($"총 행 수: {excelData.Rows.Count}");
                        System.Diagnostics.Debug.WriteLine($"총 열 수: {excelData.Columns.Count}");
                    }
                    else
                    {
                        if (!isAutoLoad)
                        {
                            MessageBox.Show("CSV 파일에서 데이터를 읽을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (extension == ".xls")
                {
                    if (!isAutoLoad)
                    {
                        MessageBox.Show("구버전 엑셀 파일(.xls)은 지원되지 않습니다.\nExcel에서 '다른 이름으로 저장'을 통해 'Excel 통합 문서(*.xlsx)' 형식으로 저장한 후 다시 선택해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("자동 로드 실패: 구버전 엑셀 파일(.xls)은 지원되지 않습니다.");
                    }
                }
                else
                {
                    if (!isAutoLoad)
                    {
                        MessageBox.Show("지원되지 않는 파일 형식입니다.\n'.xlsx', '.xlsm' 또는 '.csv' 파일만 지원합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isAutoLoad)
                {
                    MessageBox.Show($"엑셀 데이터 로드 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                System.Diagnostics.Debug.WriteLine($"엑셀 데이터 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 다중 컬럼 설정 저장
        /// </summary>
        private void SaveMultiColumnSettings()
        {
            try
            {
                if (multiColumnSettings == null || multiColumnSettings.Count == 0)
                {
                    return;
                }
                
                // 형식: "index|enabled|colName|usageType|barcodeType|size|offsetX|offsetY|showBarcodeText|excelColumnNumber|prefix|suffix|usePrefixNewLine|useSuffixNewLine;..."
                var settingsData = string.Join(";", multiColumnSettings.Select(s =>
                    $"{s.ColumnIndex}|{s.IsEnabled}|{s.ColumnName}|{s.UsageType}|{s.BarcodeType}|{s.Size}|{s.OffsetX}|{s.OffsetY}|{s.ShowBarcodeText}|{s.ExcelColumnNumber}|{s.Prefix ?? ""}|{s.Suffix ?? ""}|{s.UsePrefixNewLine}|{s.UseSuffixNewLine}"));
                
                appSettings.MultiColumnSettingsData = settingsData;
                appSettings.Save();
                
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 저장] {multiColumnSettings.Count}개 컬럼 (Prefix/Suffix 포함)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 저장 오류] {ex.Message}");
            }
        }
        
        /// <summary>
        /// 다중 컬럼 설정 불러오기
        /// </summary>
        private List<ExcelMultiColumnSelectionForm.ColumnSetting> LoadMultiColumnSettings()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appSettings.MultiColumnSettingsData))
                {
                    return null;
                }
                
                var settings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();
                var items = appSettings.MultiColumnSettingsData.Split(';');
                bool hasInvalidSettings = false;
                
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                        
                    var parts = item.Split('|');
                    if (parts.Length >= 9)
                    {
                        var setting = new ExcelMultiColumnSelectionForm.ColumnSetting
                        {
                            ColumnIndex = int.Parse(parts[0]),
                            IsEnabled = bool.Parse(parts[1]),
                            ColumnName = parts[2],
                            UsageType = parts[3],
                            BarcodeType = parts[4],
                            Size = int.Parse(parts[5]),
                            OffsetX = int.Parse(parts[6]),
                            OffsetY = int.Parse(parts[7]),
                            ShowBarcodeText = bool.Parse(parts[8])
                        };
                        
                        // ExcelColumnNumber 로드 (10번째 필드, 필수)
                        if (parts.Length >= 10 && int.TryParse(parts[9], out int excelColumnNumber) && excelColumnNumber > 0)
                        {
                            setting.ExcelColumnNumber = excelColumnNumber;
                        }
                        else
                        {
                            // ExcelColumnNumber가 없거나 유효하지 않으면 저장된 설정을 무시하고 다시 선택하도록 함
                            System.Diagnostics.Debug.WriteLine($"[경고] ExcelColumnNumber가 없거나 유효하지 않습니다. 컬럼 선택 폼을 다시 표시합니다.");
                            hasInvalidSettings = true;
                            break;
                        }

                        // Prefix / Suffix 로드 (11, 12번째 필드)
                        if (parts.Length >= 12)
                        {
                            setting.Prefix = parts[10];
                            setting.Suffix = parts[11];
                        }
                        else
                        {
                            setting.Prefix = "";
                            setting.Suffix = "";
                        }

                        // UsePrefixNewLine / UseSuffixNewLine 로드 (13, 14번째 필드)
                        if (parts.Length >= 14)
                        {
                            setting.UsePrefixNewLine = bool.Parse(parts[12]);
                            setting.UseSuffixNewLine = bool.Parse(parts[13]);
                        }
                        else
                        {
                            setting.UsePrefixNewLine = false;
                            setting.UseSuffixNewLine = false;
                        }
                        
                        settings.Add(setting);
                    }
                }
                
                // 유효하지 않은 설정이 있으면 null 반환 (컬럼 선택 폼 다시 표시)
                if (hasInvalidSettings)
                {
                    System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정] 유효하지 않은 설정 발견. 컬럼 선택 폼을 표시합니다.");
                    return null;
                }
                
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 불러오기] {settings.Count}개 컬럼");
                return settings.Count > 0 ? settings : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 불러오기 오류] {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 컬럼 선택을 통한 Excel 데이터 로드
        /// </summary>
        private void LoadExcelWithColumnSelection(string filePath, bool isAutoLoad = false)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트 사용
                    
                    if (worksheet == null)
                    {
                        if (!isAutoLoad)
                        {
                            MessageBox.Show("워크시트를 찾을 수 없습니다.", "오류", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    
                    var range = worksheet.RangeUsed();
                    if (range == null)
                    {
                        if (!isAutoLoad)
                        {
                            MessageBox.Show("엑셀 파일에 데이터가 없습니다.", "오류", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    
                    // 저장된 컬럼 설정 불러오기
                    var savedSettings = LoadMultiColumnSettings();
                    bool useSavedSettings = savedSettings != null && savedSettings.Count > 0;
                    // 수동 로드 시에는 항상 다이얼로그를 열어 사용자가 직접 매핑을 확인하도록 강제
                    bool shouldShowSelectionDialog = !isAutoLoad || !useSavedSettings;
                    
                    // 다중 컬럼 선택 다이얼로그 표시 (저장된 설정이 없거나 수동 로드인 경우)
                    if (shouldShowSelectionDialog)
                    {
                        using (var multiColumnSelectionForm = new ExcelMultiColumnSelectionForm(worksheet, this, columnPositionSettings, multiColumnSettings))
                        {
                            if (multiColumnSelectionForm.ShowDialog() == DialogResult.OK)
                            {
                                multiColumnSettings = multiColumnSelectionForm.SelectedColumnSettings;
                                
                                // 선택한 설정을 저장
                                SaveMultiColumnSettings();
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        // 저장된 설정 사용
                        multiColumnSettings = savedSettings;
                        System.Diagnostics.Debug.WriteLine("[저장된 컬럼 설정 사용]");
                    }
                    
                    if (multiColumnSettings != null && multiColumnSettings.Count > 0)
                    {
                        // 1단계: 엑셀 매핑에 속하는 컬럼만 비활성화 (선택되지 않은 컬럼은 False로 설정)
                        System.Diagnostics.Debug.WriteLine("=== ExcelMultiColumn 동기화 시작 ===");
                        foreach (var posSetting in columnPositionSettings)
                        {
                            // 인덱스 4 (공통문구)는 엑셀 매핑에 참여하지 않는 독립 항목이므로 비활성화 대상에서 제외
                            if (posSetting.ColumnIndex < 4)
                            {
                                posSetting.IsEnabled = false;
                                System.Diagnostics.Debug.WriteLine($"컬럼 {posSetting.ColumnIndex} 초기화: IsEnabled=False");
                            }
                        }
                        
                        // 2단계: 선택된 컬럼만 활성화 및 동기화
                        foreach (var excelSetting in multiColumnSettings)
                        {
                            var posSetting = columnPositionSettings.FirstOrDefault(p => p.ColumnIndex == excelSetting.ColumnIndex);
                            if (posSetting != null)
                            {
                                posSetting.IsEnabled = excelSetting.IsEnabled;
                                posSetting.UsageType = excelSetting.UsageType;
                                posSetting.BarcodeType = excelSetting.BarcodeType;
                                // 엑셀 설정 로드 시 최신 레이아웃 위치 및 크기가 구형 엑셀 매핑 데이터로 덮어쓰여 초기화되지 않도록 주석 처리
                                // posSetting.Size = excelSetting.Size;
                                // posSetting.OffsetX = excelSetting.OffsetX;
                                // posSetting.OffsetY = excelSetting.OffsetY;
                                posSetting.ShowBarcodeText = excelSetting.ShowBarcodeText;
                                
                                System.Diagnostics.Debug.WriteLine($"컬럼 {excelSetting.ColumnIndex} 설정 동기화: IsEnabled={excelSetting.IsEnabled}, Type={excelSetting.UsageType}, BarcodeType={excelSetting.BarcodeType}, Size={posSetting.Size}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ 컬럼 {excelSetting.ColumnIndex} ColumnPositionSetting을 찾을 수 없음");
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("=== ExcelMultiColumn 동기화 완료 ===");
                        
                        // 변경된 ColumnPositionSettings를 파일에 저장
                        SaveColumnPositionSettings();
                        
                        // 다중 컬럼 모드 활성화
                        useMultiColumnMode = true;
                        
                        // 선택된 컬럼들을 사용하여 DataTable 생성
                        excelData = CreateDataTableFromMultiColumns(worksheet, multiColumnSettings);
                        
                        // 컬럼명 라벨 업데이트
                        UpdateColumnLabels(multiColumnSettings);
                        
                        if (excelData != null && excelData.Rows.Count > 0)
                        {
                            isExcelMode = true;
                            
                            // 선택된 컬럼 정보 생성
                            string columnInfo = "";
                            foreach (var setting in multiColumnSettings)
                            {
                                if (setting.IsEnabled)
                                {
                                    string usageInfo = setting.UsageType == "바코드" ? 
                                        $"바코드({setting.BarcodeType})" : "텍스트";
                                    columnInfo += $"{usageInfo}: {setting.ColumnName}\n";
                                }
                            }
                            
                            // 저장된 설정을 사용한 경우 메시지 표시 (자동 로드 시에는 표시 안 함)
                            if (useSavedSettings && !isAutoLoad)
                            {
                                MessageBox.Show($"엑셀 파일이 성공적으로 로드되었습니다.\n총 {excelData.Rows.Count}개의 행이 있습니다.\n\n선택된 컬럼들:\n{columnInfo}", 
                                    "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            
                            // 디버그 정보 출력
                            System.Diagnostics.Debug.WriteLine($"========================================");
                            System.Diagnostics.Debug.WriteLine($"[엑셀 데이터 로드 성공]");
                            System.Diagnostics.Debug.WriteLine($"  파일: {filePath}");
                            System.Diagnostics.Debug.WriteLine($"  다중 컬럼 모드 활성화: {useMultiColumnMode}");
                            System.Diagnostics.Debug.WriteLine($"  선택된 컬럼 수: {multiColumnSettings.Count}");
                            System.Diagnostics.Debug.WriteLine($"  총 행 수: {excelData.Rows.Count}");
                            System.Diagnostics.Debug.WriteLine($"  총 열 수: {excelData.Columns.Count}");
                            System.Diagnostics.Debug.WriteLine($"  isExcelMode: {isExcelMode}");
                            System.Diagnostics.Debug.WriteLine($"========================================");
                            
                            // 마지막 페이지번호 입력값을 새로운 엑셀 파일의 마지막 페이지로 변경
                            if (this.txtEndPage != null)
                            {
                                if (this.txtEndPage.InvokeRequired)
                                {
                                    this.txtEndPage.Invoke(new Action(() => this.txtEndPage.Text = excelData.Rows.Count.ToString()));
                                }
                                else
                                {
                                    this.txtEndPage.Text = excelData.Rows.Count.ToString();
                                }
                            }

                            // 첫 번째 행 데이터를 미리보기에 표시
                            UpdatePreviewWithFirstRow();
                            
                            // 엑셀 모드 자동 활성화
                            isExcelMode = true;
                            
                            // 엑셀 파일명 표시
                            UpdateExcelFileNameLabel();
                            
                            // 설정 저장
                            SaveAppSettings();
                            
                            // UI 모드 전환 및 버튼 활성화
                            SwitchToExcelMode();
                        }
                        else
                        {
                            if (!isAutoLoad)
                            {
                                MessageBox.Show("선택된 컬럼에 유효한 데이터가 없습니다.", "오류", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isAutoLoad)
                {
                    MessageBox.Show($"엑셀 파일 처리 중 오류가 발생했습니다: {ex.Message}", "오류", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                System.Diagnostics.Debug.WriteLine($"엑셀 파일 처리 오류: {ex.Message}");
            }
        }

       

        /// <summary>
        /// 다중 컬럼 설정을 사용하여 Excel에서 DataTable 생성
        /// </summary>
        private DataTable CreateDataTableFromMultiColumns(IXLWorksheet worksheet, List<ExcelMultiColumnSelectionForm.ColumnSetting> settings)
        {
            try
            {
                var dataTable = new DataTable();
                
            // 컬럼 추가 (활성화된 컬럼만 추가)
            foreach (var setting in settings)
            {
                if (setting.IsEnabled)
                {
                    string columnName = setting.UsageType == "바코드" ? 
                        $"Barcode_{setting.ColumnIndex}_{setting.BarcodeType}" : 
                        $"Text_{setting.ColumnIndex}";
                    dataTable.Columns.Add(columnName, typeof(string));
                }
            }
                
                // 데이터 행 추가
                var range = worksheet.RangeUsed();
                if (range != null)
                {
                    int rowCount = range.RowCount();
                    
                    for (int row = 2; row <= rowCount; row++) // 헤더 제외하고 2행부터
                    {
                        var dataRow = dataTable.NewRow();
                        int columnIndex = 0;
                        bool hasData = false;
                        
                        foreach (var setting in settings)
                        {
                            if (setting.IsEnabled)
                            {
                                var cell = worksheet.Cell(row, setting.ExcelColumnNumber); // ExcelColumnNumber 사용 (1부터 시작)
                                string originalValue = cell.IsEmpty() ? "" : cell.GetString();
                                
                                if (!string.IsNullOrWhiteSpace(originalValue))
                                {
                                    hasData = true;
                                }

                                string cellValue = originalValue;

                                // 가격 컬럼인 경우 가격 포맷 적용 (단위 구분 및 원화 기호) - 제거됨 (텍스트 표시로 수정)
                                /*
                                string headerName = worksheet.Cell(1, setting.ExcelColumnNumber).GetString() ?? "";
                                if (setting.ColumnIndex == 1 || BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(headerName))
                                {
                                    cellValue = BarcodePrint.Model.PriceFormatHelper.FormatPrice(cellValue);
                                }
                                */

                                dataRow[columnIndex] = cellValue;
                                columnIndex++;
                            }
                        }
                        
                        if (hasData)
                        {
                            dataTable.Rows.Add(dataRow);
                        }

                        // 고객 체험용: 데이터 행 최대 5개로 제한
                        if (isDemoVersion && dataTable.Rows.Count >= 5) break;
                    }
                }
                
                return dataTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다중 컬럼 DataTable 생성 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 엑셀 파일을 DataTable로 읽기 (ClosedXML 사용)
        /// </summary>
        private DataTable ReadExcelAsDataTable(string filePath)
        {
            try
            {
                DataTable dataTable = new DataTable();
                
                // 파일 확장자에 따라 처리
                string extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".csv")
                {
                    // CSV 파일 처리 (기존 방식 유지)
                    using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
                    {
                        string line;
                        bool isFirstLine = true;
                        
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] values = line.Split(',');
                            
                            if (isFirstLine)
                            {
                                // 첫 번째 줄을 컬럼명으로 사용
                                foreach (string columnName in values)
                                {
                                    dataTable.Columns.Add(columnName.Trim());
                                }
                                isFirstLine = false;
                            }
                            else
                            {
                                // 데이터 행 추가
                                DataRow row = dataTable.NewRow();
                                for (int i = 0; i < values.Length && i < dataTable.Columns.Count; i++)
                                {
                                    string cellValue = values[i].Trim();
                                    // 가격 컬럼인 경우 가격 포맷 적용 (단위 구분 및 원화 기호)
                                    if (BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(dataTable.Columns[i].ColumnName))
                                    {
                                        cellValue = BarcodePrint.Model.PriceFormatHelper.FormatPrice(cellValue);
                                    }
                                    row[i] = cellValue;
                                }
                                dataTable.Rows.Add(row);

                                // 고객 체험용: 데이터 행 최대 5개로 제한
                                if (isDemoVersion && dataTable.Rows.Count >= 5) break;
                            }
                        }
                    }
                }
                else if (extension == ".xlsx" || extension == ".xlsm" || extension == ".xls")
                {
                    // Excel 파일 처리 (ClosedXML 사용)
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트 사용
                        
                        if (worksheet == null)
                        {
                            MessageBox.Show("워크시트를 찾을 수 없습니다.", "오류", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }
                        
                        var range = worksheet.RangeUsed();
                        if (range == null)
                        {
                            MessageBox.Show("엑셀 파일에 데이터가 없습니다.", "오류", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }
                        
                        int rowCount = range.RowCount();
                        int columnCount = range.ColumnCount();
                        
                        // 컬럼명 설정 (첫 번째 행을 헤더로 사용)
                        for (int col = 1; col <= columnCount; col++)
                        {
                            var headerCell = worksheet.Cell(1, col);
                            string columnName = headerCell.IsEmpty() ? $"컬럼{col}" : headerCell.GetString().Trim();
                            dataTable.Columns.Add(columnName);
                        }
                        
                        // 데이터 행 추가 (헤더 제외하고 2행부터)
                        for (int row = 2; row <= rowCount; row++)
                        {
                            DataRow dataRow = dataTable.NewRow();
                            for (int col = 1; col <= columnCount; col++)
                            {
                                var cell = worksheet.Cell(row, col);
                                string cellValue = cell.IsEmpty() ? string.Empty : cell.GetString().Trim();

                                // 가격 컬럼인 경우 가격 포맷 적용 (단위 구분 및 원화 기호)
                                if (BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(dataTable.Columns[col - 1].ColumnName))
                                {
                                    cellValue = BarcodePrint.Model.PriceFormatHelper.FormatPrice(cellValue);
                                }
                                dataRow[col - 1] = cellValue;
                            }
                            dataTable.Rows.Add(dataRow);

                            // 데모 버전: 데이터 행 최대 5개로 제한
                            if (isDemoVersion && dataTable.Rows.Count >= 5) break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("지원하지 않는 파일 형식입니다.\nCSV, XLSX, XLS 파일만 지원됩니다.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }
                
                return dataTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 파일 읽기 오류: {ex.Message}");
                MessageBox.Show($"엑셀 파일 읽기 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// (Pagination) 특정 행 데이터를 미리보기에 표시
        /// </summary>
        private void UpdatePreviewWithRow(int rowIndex)
        {
            try
            {
                if (excelData == null || excelData.Rows.Count == 0) return;
                
                // 범위 제한
                if (rowIndex < 0) rowIndex = 0;
                if (rowIndex >= excelData.Rows.Count) rowIndex = excelData.Rows.Count - 1;
                
                PreviewNum = rowIndex;
                DataRow targetRow = excelData.Rows[PreviewNum];
                
                if (useMultiColumnMode && multiColumnSettings.Count > 0)
                {
                    // 다중 컬럼 모드: 바코드와 텍스트 컬럼 찾기
                    string barcodeValue = "";
                    string textValue = "";
                    
                    foreach (var setting in multiColumnSettings)
                    {
                        if (setting.IsEnabled)
                        {
                            if (setting.UsageType == "바코드" && string.IsNullOrEmpty(barcodeValue))
                            {
                                var barcodeColumnName = $"Barcode_{setting.ColumnIndex}_{setting.BarcodeType}";
                                if (excelData.Columns.Contains(barcodeColumnName))
                                    barcodeValue = targetRow[barcodeColumnName]?.ToString() ?? "";
                            }
                            else if (setting.UsageType == "텍스트" && string.IsNullOrEmpty(textValue))
                            {
                                var textColumnName = $"Text_{setting.ColumnIndex}";
                                if (excelData.Columns.Contains(textColumnName))
                                    textValue = targetRow[textColumnName]?.ToString() ?? "";
                            }
                        }
                    }
                    currentBarcodeValue = barcodeValue;
                    currentBarcodeText = textValue;
                    
                    System.Diagnostics.Debug.WriteLine($"다중 컬럼 모드 - currentBarcodeValue 설정: '{currentBarcodeValue}'");
                    System.Diagnostics.Debug.WriteLine($"다중 컬럼 모드 - currentBarcodeText 설정: '{currentBarcodeText}'");
                }
                else
                {
                    // 기존 단일 컬럼 모드
                    string barcodeValue, textValue;
                    if (excelData.Columns.Count == 2)
                    {
                        // CreateDataTableFromExcel로 생성된 경우 (바코드, 텍스트만)
                        barcodeValue = targetRow[0]?.ToString() ?? "";
                        textValue = targetRow[1]?.ToString() ?? "";
                    }
                    else
                    {
                        // LoadExcelDataFromFile로 생성된 경우 (원본 컬럼 구조)
                        int barcodeColIndex = Math.Max(0, Math.Min(selectedBarcodeColumn, excelData.Columns.Count - 1));
                        int textColIndex = Math.Max(0, Math.Min(selectedTextColumn, excelData.Columns.Count - 1));
                        barcodeValue = targetRow[barcodeColIndex]?.ToString() ?? "";
                        textValue = targetRow[textColIndex]?.ToString() ?? "";
                    }
                    currentBarcodeValue = barcodeValue;
                    currentBarcodeText = textValue;
                    
                    System.Diagnostics.Debug.WriteLine($"단일 컬럼 모드 - currentBarcodeValue 설정: '{currentBarcodeValue}'");
                    System.Diagnostics.Debug.WriteLine($"단일 컬럼 모드 - currentBarcodeText 설정: '{currentBarcodeText}'");
                }
                
                // 네비게이션 UI 업데이트
                UpdatePreviewNavUI();

                // 미리보기 이벤트 트리거
                OnPreviewEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"미리보기 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 첫 번째 행 데이터를 미리보기에 표시 (호환성 유지)
        /// </summary>
        private void UpdatePreviewWithFirstRow()
        {
            UpdatePreviewWithRow(0);
        }

        /// <summary>
        /// 미리보기 하단의 페이지 네비게이션 UI 업데이트
        /// </summary>
        private void UpdatePreviewNavUI()
        {
            try
            {
                var txtPage = this.Controls.Find("txtCurrentPage", true).FirstOrDefault() as TextBox;
                var txtEnd = this.Controls.Find("txtEndPage", true).FirstOrDefault() as TextBox;
                var lblTotal = this.Controls.Find("lblTotalPages", true).FirstOrDefault() as Label;
                
                if (txtPage != null && lblTotal != null && excelData != null)
                {
                    // 메인 UI 스레드에서 접근 보장
                    if (txtPage.InvokeRequired)
                    {
                        txtPage.Invoke(new Action(() => txtPage.Text = (PreviewNum + 1).ToString()));
                        if (txtEnd != null && string.IsNullOrEmpty(txtEnd.Text))
                        {
                            txtEnd.Invoke(new Action(() => txtEnd.Text = excelData.Rows.Count.ToString()));
                        }
                        lblTotal.Invoke(new Action(() => lblTotal.Text = $" / {excelData.Rows.Count}"));
                    }
                    else
                    {
                        txtPage.Text = (PreviewNum + 1).ToString();
                        if (txtEnd != null && string.IsNullOrEmpty(txtEnd.Text))
                        {
                            txtEnd.Text = excelData.Rows.Count.ToString();
                        }
                        lblTotal.Text = $" / {excelData.Rows.Count}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreviewNavUI 오류: {ex.Message}");
            }
        }

        // 이전 페이지 버튼 클릭 이벤트 핸들러
        public void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (excelData == null) return;

            if (lastFocusedPageTextBox == txtEndPage)
            {
                if (int.TryParse(txtEndPage.Text, out int endPage))
                {
                    int startPage = GetStartPage();
                    int nextEndPage = Math.Max(startPage, endPage - 1);
                    txtEndPage.Text = nextEndPage.ToString();
                    ValidateEndPageInput();
                    txtEndPage.Focus(); // 포커스 유지
                }
            }
            else
            {
                UpdatePreviewWithRow(PreviewNum - 1);
                txtCurrentPage.Focus(); // 포커스 유지
            }
        }

        // 다음 페이지 버튼 클릭 이벤트 핸들러
        public void btnNextPage_Click(object sender, EventArgs e)
        {
            if (excelData == null) return;

            if (lastFocusedPageTextBox == txtEndPage)
            {
                if (int.TryParse(txtEndPage.Text, out int endPage))
                {
                    int totalRows = excelData.Rows.Count;
                    int nextEndPage = Math.Min(totalRows, endPage + 1);
                    txtEndPage.Text = nextEndPage.ToString();
                    ValidateEndPageInput();
                    txtEndPage.Focus(); // 포커스 유지
                }
            }
            else
            {
                UpdatePreviewWithRow(PreviewNum + 1);
                txtCurrentPage.Focus(); // 포커스 유지
            }
        }

        // 특정 페이지 번호 텍스트박스 입력 엔터 이벤트 핸들러
        public void txtCurrentPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                var txtPage = sender as TextBox;
                if (txtPage != null && int.TryParse(txtPage.Text, out int pageNumber))
                {
                    // 1-based 인덱스를 0-based로 변환
                    UpdatePreviewWithRow(pageNumber - 1);
                }
                else
                {
                    // 숫자가 아니거나 범위를 벗어난 경우 원래 페이지 번호로 되돌림
                    UpdatePreviewNavUI();
                }
            }
        }

        // 끝 페이지 번호 텍스트박스 입력 엔터 이벤트 핸들러
        public void txtEndPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ValidateEndPageInput();
            }
        }

        // 끝 페이지 번호 텍스트박스 포커스 이탈 이벤트 핸들러
        public void txtEndPage_Leave(object sender, EventArgs e)
        {
            ValidateEndPageInput();
        }

        private void ValidateEndPageInput()
        {
            if (excelData == null || excelData.Rows.Count == 0) return;
            
            if (int.TryParse(txtEndPage.Text, out int endPage))
            {
                int startPage = GetStartPage();
                int totalRows = excelData.Rows.Count;
                if (endPage < startPage) endPage = startPage;
                if (endPage > totalRows) endPage = totalRows;
                txtEndPage.Text = endPage.ToString();
            }
            else
            {
                txtEndPage.Text = excelData.Rows.Count.ToString();
            }
        }

        private int GetStartPage()
        {
            if (txtCurrentPage != null && int.TryParse(txtCurrentPage.Text, out int startPage))
            {
                return Math.Max(1, Math.Min(startPage, excelData?.Rows.Count ?? 1));
            }
            return 1;
        }

        private int GetEndPage()
        {
            if (txtEndPage != null && int.TryParse(txtEndPage.Text, out int endPage))
            {
                return Math.Max(GetStartPage(), Math.Min(endPage, excelData?.Rows.Count ?? 1));
            }
            return excelData?.Rows.Count ?? 1;
        }

        /// <summary>
        /// 바코드로 엑셀 데이터 검색
        /// </summary>
        private DataRow FindExcelRowByBarcode(string barcode)
        {
            if (excelData == null || string.IsNullOrEmpty(barcode))
                return null;

            try
            {
                // 첫 번째 컬럼(바코드 컬럼)에서 바코드와 일치하는 행 찾기
                foreach (DataRow row in excelData.Rows)
                {
                    if (row[0] != null && row[0].ToString().Trim() == barcode.Trim())
                    {
                        return row;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 데이터 검색 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 엑셀 모드에서 바코드 처리
        /// </summary>
        private void ProcessBarcodeWithExcel(string barcode)
        {
            try
            {
                // placeholder 텍스트는 처리하지 않음
                if (string.IsNullOrWhiteSpace(barcode) || 
                    barcode == "값을 입력하세요." || 
                    barcode.Contains("값을 입력하세요"))
                {
                    System.Diagnostics.Debug.WriteLine($"엑셀 모드 바코드 입력 무시: '{barcode}' (placeholder 텍스트)");
                    return;
                }

                DataRow matchedRow = FindExcelRowByBarcode(barcode);
                
                if (matchedRow != null)
                {
                    // 매칭된 행의 출력 텍스트 가져오기 (두 번째 컬럼)
                    string outputText = matchedRow[1]?.ToString() ?? "";
                    
                    // 바코드 값과 텍스트를 분리하여 저장
                    currentBarcodeValue = barcode;
                    currentBarcodeText = outputText;
                    
                    // 프린트 큐에 추가 (바코드 값만)
                    barcodePrintManager.AddToPrintQueue(barcode, barcodeSize, barcodeX, barcodeY, barcodeHeight);
                    PrintStart();
                }
                else
                {
                    // 매칭되지 않은 경우 기본 바코드만 출력
                    currentBarcodeValue = barcode;
                    currentBarcodeText = ""; // 텍스트 초기화
                    barcodePrintManager.AddToPrintQueue(barcode, barcodeSize, barcodeX, barcodeY, barcodeHeight);
                    PrintStart();
                    
                    MessageBox.Show($"엑셀에서 '{barcode}'를 찾을 수 없습니다.\n기본 바코드만 출력됩니다.", 
                        "매칭 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 연동 처리 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"엑셀 연동 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 엑셀 데이터 전체 출력 기능
        /// </summary>
        private void PrintAllExcelData()
        {
            try
            {
                // 엑셀 모드가 아니거나 데이터가 없는 경우 확인
                if (!isExcelMode || excelData == null || excelData.Rows.Count == 0)
                {
                    MessageBox.Show("엑셀 데이터가 로드되지 않았습니다.\n먼저 엑셀 파일을 선택해주세요.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int startPage = GetStartPage();
                int endPage = GetEndPage();
                int totalToPrint = endPage - startPage + 1;

                // 확인 대화상자 표시
                string confirmMsg = isDemoVersion ? 
                    $"체험판 버전은 최대 5개까지만 출력 가능합니다.\n\n출력하시겠습니까?" : 
                    $"엑셀 파일의 선택한 구간({startPage} ~ {endPage} 페이지) 데이터를 출력하시겠습니까?\n\n총 {totalToPrint}개의 행이 인쇄 대상입니다.";
                
                var result = MessageBox.Show(confirmMsg, "출력 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                int validCount = 0;
                int errorCount = 0;

                int startIdx = startPage - 1;
                int endIdx = endPage - 1;

                // 선택된 구간의 행들을 큐에 추가
                for (int i = startIdx; i <= endIdx; i++)
                {
                    if (isDemoVersion && validCount >= 5) break;

                    try
                    {
                        DataRow row = excelData.Rows[i];
                        // 바코드 값 (첫 번째 컬럼)
                        string barcode = row[0]?.ToString()?.Trim();
                        
                        // 출력 텍스트 (두 번째 컬럼)
                        string outputText = row[1]?.ToString()?.Trim();

                        // 바코드가 비어있지 않은 경우만 처리
                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            // 바코드 값과 텍스트 업데이트
                            currentBarcodeValue = barcode;
                            currentBarcodeText = outputText;

                            // 디버그 정보 출력
                            System.Diagnostics.Debug.WriteLine($"큐에 추가 - 바코드: '{barcode}', 텍스트: '{outputText}'");

                            // 프린트 큐에 추가 (텍스트 정보도 함께 전달)
                            barcodePrintManager.AddToPrintQueue(barcode, barcodeSize, barcodeX, barcodeY, barcodeHeight);
                            validCount++;
                        }
                    }
                    catch (Exception rowEx)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"행 처리 오류: {rowEx.Message}");
                    }
                }

                // 모든 데이터를 큐에 추가한 후 한 번에 출력 실행
                if (validCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"총 {validCount}개 항목을 큐에 추가 완료. 출력 시작...");
                    PrintStart(); // 한 번만 호출
                    
                    System.Diagnostics.Debug.WriteLine($"[PrintStart 호출 완료] 메시지 표시 전 대기...");
                    System.Threading.Thread.Sleep(100); // 인쇄 시작 시간 확보
                    
                    string message = $"출력이 완료되었습니다.\n성공: {validCount}개";
                    if (errorCount > 0)
                    {
                        message += $"\n오류: {errorCount}개";
                    }
                    MessageBox.Show(message, "출력 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[인쇄 취소] validCount=0 - 유효한 데이터가 없음");
                    MessageBox.Show("출력할 유효한 데이터가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 구간 출력 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"엑셀 구간 출력 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 다중 컬럼 모드에서 엑셀 데이터 전체 출력 기능
        /// </summary>
        private void PrintAllExcelDataMultiColumn()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[PrintAllExcelDataMultiColumn] 시작");
                System.Diagnostics.Debug.WriteLine($"[초기 검증] isExcelMode={isExcelMode}, excelData null={excelData == null}, Rows={(excelData?.Rows.Count ?? 0)}, multiColumnSettings Count={multiColumnSettings?.Count ?? 0}");
                
                // 엑셀 모드가 아니거나 데이터가 없는 경우 확인
                if (!isExcelMode || excelData == null || excelData.Rows.Count == 0 || multiColumnSettings.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[검증 실패] isExcelMode={isExcelMode}, excelData={excelData != null}, Rows={excelData?.Rows.Count ?? 0}, multiColumnSettings={multiColumnSettings?.Count ?? 0}");
                    MessageBox.Show("엑셀 데이터가 로드되지 않았습니다.\n먼저 엑셀 파일을 선택해주세요.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // ColumnPositionSettings 상태 확인
                System.Diagnostics.Debug.WriteLine($"[ColumnPositionSettings 확인] columnPositionSettings null={columnPositionSettings == null}, Count={columnPositionSettings?.Count ?? 0}");
                if (columnPositionSettings != null)
                {
                    foreach (var posSetting in columnPositionSettings)
                    {
                        System.Diagnostics.Debug.WriteLine($"  컬럼 {posSetting.ColumnIndex}: IsEnabled={posSetting.IsEnabled}, Type={posSetting.UsageType}, Size={posSetting.Size}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("  ⚠️ columnPositionSettings가 null입니다!");
                }
                
                // 활성화된 컬럼 수 확인
                int enabledColumns = columnPositionSettings?.Count(c => c.IsEnabled) ?? 0;
                System.Diagnostics.Debug.WriteLine($"[활성화된 컬럼 수]: {enabledColumns}개");
                
                if (enabledColumns == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[인쇄 취소] 활성화된 컬럼이 없음 - enabledColumns={enabledColumns}");
                    MessageBox.Show("인쇄할 컬럼이 선택되지 않았습니다.\n컬럼 위치 설정에서 최소 1개 이상의 컬럼을 활성화해주세요.", 
                        "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int startPage = GetStartPage();
                int endPage = GetEndPage();
                int totalToPrint = endPage - startPage + 1;

                // 확인 대화상자 표시
                string confirmMsg = isDemoVersion ? 
                    $"체험판 버전은 최대 5개까지만 출력 가능합니다.\n\n출력하시겠습니까?" : 
                    $"엑셀 파일의 선택한 구간({startPage} ~ {endPage} 페이지) 데이터를 출력하시겠습니까?\n\n총 {totalToPrint}개의 행이 인쇄 대상입니다.\n선택된 컬럼 수: {multiColumnSettings.Count}개";
                
                var result = MessageBox.Show(confirmMsg, "전체 출력 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    System.Diagnostics.Debug.WriteLine($"[사용자 취소]");
                    return;
                }

                int validCount = 0;
                int errorCount = 0;

                int startIdx = startPage - 1;
                int endIdx = endPage - 1;

                System.Diagnostics.Debug.WriteLine($"[큐 추가 시작] 구간 {startIdx} ~ {endIdx}");
                
                // 선택된 구간의 행들을 큐에 추가
                for (int i = startIdx; i <= endIdx; i++)
                {
                    if (isDemoVersion && validCount >= 5) break;

                    try
                    {
                        // 다중 컬럼 데이터를 인쇄 큐에 추가 (실제로 추가된 경우에만 카운트)
                        bool added = barcodePrintManager.AddMultiColumnToPrintQueue((DataTable)excelData, multiColumnSettings, i);
                        if (added)
                        {
                            validCount++;
                            
                            if (i < startIdx + 3 || i == endIdx)
                            {
                                System.Diagnostics.Debug.WriteLine($"  행 {i} 큐 추가 완료");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  행 {i}: 데이터가 없어 큐에 추가되지 않음");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"  행 {i} 처리 오류: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[큐 추가 완료] 성공: {validCount}개, 오류: {errorCount}개");
                System.Diagnostics.Debug.WriteLine($"[프린터 상태 확인] printerName={PRINT_INFO_CLASS.PrinterInfo.printerName}, printerPort={PRINT_INFO_CLASS.PrinterInfo.printerPortName}, DPI={PRINT_INFO_CLASS.PrinterInfo.printer_DPI}");
                
                // 인쇄 실행
                if (validCount > 0)
                {
                    // 프린터 상태 확인
                    if (string.IsNullOrEmpty(PRINT_INFO_CLASS.PrinterInfo.printerName))
                    {
                        System.Diagnostics.Debug.WriteLine("프린터가 선택되지 않음");
                        MessageBox.Show("프린터가 선택되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[Barcode_PrintGo 직접 호출] - 다중 컬럼 모드");
                    
                    // 다중 컬럼 모드에서는 큐에 데이터가 있으므로 Barcode_PrintGo를 직접 호출
                    Thread thread = new Thread(barcodePrintManager.Barcode_PrintGo)
                    {
                        IsBackground = true,
                        Name = "PrintThread"
                    };
                    
                    System.Diagnostics.Debug.WriteLine("인쇄 스레드 시작");
                    thread.Start();
                    System.Diagnostics.Debug.WriteLine($"인쇄 스레드 시작 완료 - 스레드 ID: {thread.ManagedThreadId}");
                    
                    System.Diagnostics.Debug.WriteLine($"[인쇄 스레드 시작 완료] 메시지 표시 전 대기...");
                    System.Threading.Thread.Sleep(200); // 인쇄 시작 시간 확보
                    
                    string message = $"출력이 완료되었습니다.\n성공: {validCount}개";
                    if (errorCount > 0)
                    {
                        message += $"\n오류: {errorCount}개";
                    }
                    
                    MessageBox.Show(message, "출력 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[인쇄 취소] validCount=0 - 활성화된 컬럼이나 데이터가 없음");
                    MessageBox.Show("출력할 유효한 데이터가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다중 컬럼 엑셀 구간 출력 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"다중 컬럼 엑셀 구간 출력 오류: {ex.Message}");
            }
        }


        #region 라이센스 관련 이벤트 핸들러

        /// <summary>
        /// 프로그램 정보 라벨 클릭 (label_license)
        /// </summary>
        private void label_license_Click(object sender, EventArgs e)
        {
            try
            {
                var infoForm = new ProgramInfoForm();
                infoForm.StartPosition = FormStartPosition.CenterParent;
                infoForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 정보를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void label_license_MouseEnter(object sender, EventArgs e)
        {
            // 마우스를 올렸을 때 손가락 모양으로 변경
            label_license.Cursor = Cursors.Hand;
        }

        private void label_license_MouseLeave(object sender, EventArgs e)
        {
            // 마우스를 벗어났을 때 기본 커서로 변경
            label_license.Cursor = Cursors.Default;
        }
        
 
        
        private void label_licenseInfo_MouseEnter(object sender, EventArgs e)
        {
            label_licenseInfo.ForeColor = Color.DarkBlue; // 호버 색상
        }
        
        private void label_licenseInfo_MouseLeave(object sender, EventArgs e)
        {
            label_licenseInfo.ForeColor = Color.Blue; // 기본 색상
        }
        
        /// <summary>
        /// 미리보기 패널에 마우스 포커스 설정 (마우스 휠 이벤트 받기 위해)
        /// </summary>
        private void Panel_BarcodePreview_MouseEnter(object sender, EventArgs e)
        {
            // 패널에 포커스를 주어서 마우스 휠 이벤트를 받을 수 있도록 설정
            if (!panel_BarcodePreview.Focused)
            {
                panel_BarcodePreview.Focus();
            }
        }
        
        /// <summary>
        /// 미리보기 패널 마우스 휠 이벤트 - 확대/축소
        /// Ctrl 키를 누른 상태에서 휠을 돌리면 확대/축소, 그렇지 않으면 AutoScroll 동작
        /// </summary>
        private void Panel_BarcodePreview_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                // Ctrl 키를 누른 상태에서만 확대/축소
                if (ModifierKeys == Keys.Control)
                {
                    if (originalPreviewBitmap == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[확대/축소 중단] 원본 이미지 없음");
                        return;
                    }
                    
                    // 마우스 휠 델타에 따라 확대/축소 (120 단위로 증가/감소)
                    float zoomDelta = e.Delta > 0 ? 0.1f : -0.1f;
                    float oldZoom = previewZoomFactor;
                    float newZoom = Math.Max(0.5f, Math.Min(3.0f, previewZoomFactor + zoomDelta)); // 0.5배 ~ 3배 범위
                    
                    // 변화가 없으면 처리 중단
                    if (Math.Abs(newZoom - oldZoom) < 0.01f)
                    {
                        System.Diagnostics.Debug.WriteLine($"[확대/축소 중단] 이미 한계값: {oldZoom:F2}");
                        ((HandledMouseEventArgs)e).Handled = true;
                        return;
                    }
                    
                    previewZoomFactor = newZoom;
                    System.Diagnostics.Debug.WriteLine($"[미리보기 확대/축소] {oldZoom:F2} → {previewZoomFactor:F2}");
                    
                    // 현재 확대 비율로 이미지 다시 표시
                    DisplayPreviewWithZoom();
                    
                    // 기본 스크롤 동작 방지
                    ((HandledMouseEventArgs)e).Handled = true;
                }
                // Ctrl이 눌리지 않았으면 기본 AutoScroll 동작 (아무것도 하지 않음)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"미리보기 확대/축소 오류: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 엑셀 패널 상태를 체크박스 상태에 맞게 업데이트
        /// </summary>
        private void UpdateExcelPanelState()
        {
            try
            {
                // isExcelMode 변수 사용
                panel_OpenExcelForm.Enabled = isExcelMode;
                
                if (isExcelMode)
                {
                    new_OpenExcelFormButton(1f);
                    // 엑셀 데이터가 있고 시리얼 매칭이 완료되었으면 버튼 활성화, 없으면 비활성화
                    panel_PrintButton.Enabled = isSerialMatched && (excelData != null && excelData.Rows.Count > 0);
                    panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트
                    
                    // 파일 경로가 있으면 파일명 라벨 표시 (데이터 로드 여부와 무관)
                    if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
                    {
                        UpdateExcelFileNameLabel();
                        System.Diagnostics.Debug.WriteLine($"[UpdateExcelPanelState] 파일명 라벨 표시: {Path.GetFileName(excelFilePath)} (HasData={excelData != null})");
                    }
                    else if (label_ExcelFileName.Visible)
                    {
                        // 라벨이 이미 표시되어 있으면 유지 (자동 로드 진행 중일 수 있음)
                        System.Diagnostics.Debug.WriteLine($"[UpdateExcelPanelState] 파일명 라벨 유지: Text='{label_ExcelFileName.Text}'");
                    }
                    else
                    {
                        label_ExcelFileName.Text = "";
                        label_ExcelFileName.Visible = false;
                        System.Diagnostics.Debug.WriteLine($"[UpdateExcelPanelState] 파일명 라벨 숨김: HasPath={!string.IsNullOrEmpty(excelFilePath)}, FileExists={File.Exists(excelFilePath ?? "")}");
                    }
                }
                else
                {
                    DrawDisabledExcelButton();
                    panel_PrintButton.Enabled = isSerialMatched; // 직접 출력 모드에서는 시리얼 매칭 상태에 따름
                    panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트
                    
                    // 비활성화 시 파일명 라벨 숨기기
                    label_ExcelFileName.Text = "";
                    label_ExcelFileName.Visible = false;
                }
                
                System.Diagnostics.Debug.WriteLine($"엑셀 패널 상태 업데이트: OpenExcelEnabled={panel_OpenExcelForm.Enabled}, PrintButtonEnabled={panel_PrintButton.Enabled}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 패널 상태 업데이트 오류: {ex.Message}");
            }
        }

        // 참조 프로젝트 방식: 바코드 설정은 별도 저장/로드하지 않음
        
        /// <summary>
        /// 보내는 분/받는 분/표시없음 라디오버튼 변경 이벤트
        /// </summary>
        private void touchSwitch_Title_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isInitialized) return;
                
                TouchSwitch switchControl = sender as TouchSwitch;
                if (switchControl != null)
                {
                    // 스위치 상태에 따라 텍스트박스 활성화/비활성화
                    textBox_Title.Enabled = switchControl.Checked;
                    
                    // 컬럼 설정에도 반영 (Index 4: 공통문구)
                    if (columnPositionSettings != null && columnPositionSettings.Count > 4)
                    {
                        columnPositionSettings[4].IsEnabled = switchControl.Checked;
                        System.Diagnostics.Debug.WriteLine($"Title 스위치 변경 -> 컬럼 4 IsEnabled={columnPositionSettings[4].IsEnabled}");
                    }

                    // 설정 저장
                    SaveColumnPositionSettings();
                    SaveAppSettings();
                    
                    System.Diagnostics.Debug.WriteLine($"Title 스위치 변경: Checked={switchControl.Checked}, TextBox Enabled={textBox_Title.Enabled}");
                    
                    // 미리보기 업데이트
                    OnPreviewEvent();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Title 스위치 이벤트 오류: {ex.Message}");
            }
        }
        
        private void textBox_Title_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isInitialized) return;

                // 설정 저장
                SaveAppSettings();
                
                System.Diagnostics.Debug.WriteLine($"[Title 텍스트 변경] Text='{textBox_Title?.Text}', GetSenderReceiverLabel()='{GetSenderReceiverLabel()}'");
                
                // 텍스트 변경 시 미리보기 즉시 업데이트 (항상 OnPreviewEvent 사용)
                OnPreviewEvent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Title 텍스트박스 변경 이벤트 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 현재 선택된 Title 라벨 가져오기 (스위치가 On이고 텍스트가 있으면 텍스트 반환, Off이면 "표시없음")
        /// </summary>
        public string GetSenderReceiverLabel()
        {
            if (touchSwitch_Title != null && touchSwitch_Title.Checked)
            {
                // 스위치가 On인 경우 텍스트박스의 값을 반환 (입력값과 일치)
                string titleText = textBox_Title?.Text ?? "";
                if (!string.IsNullOrWhiteSpace(titleText))
                {
                    return titleText;
                }
                else
                {
                    // 텍스트가 비어있으면 "표시없음" 반환 (입력값과 일치)
                    return "표시없음";
                }
            }
            else
            {
                // 스위치가 Off인 경우 "표시없음" 반환
                return "표시없음";
            }
        }

        /// <summary>
        /// 인쇄매수 가져오기
        /// </summary>
        public int GetPrintCopies()
        {
            if (numericUpDown_PrintCopies != null)
            {
                return (int)numericUpDown_PrintCopies.Value;
            }
            return 1; // 기본값
        }

        private void btn_SaveProject_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Label Project (*.lmb)|*.lmb|JSON Files (*.json)|*.json";
                    sfd.Title = "프로젝트 저장";
                    sfd.FileName = $"Project_{DateTime.Now:yyyyMMdd_HHmm}.lmb";
                    sfd.InitialDirectory = System.Windows.Forms.Application.StartupPath;

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        BarcodePrint.Model.ProjectData project = new BarcodePrint.Model.ProjectData
                        {
                            Version = "1.0",
                            SavedDate = DateTime.Now,
                            TitleText = textBox_Title?.Text ?? "",
                            IsTitleEnabled = touchSwitch_Title?.Checked ?? false,
                            Column1Text = GetTextBoxValue(TextBox_InputText4),
                            Column2Text = GetTextBoxValue(TextBox_InputText),
                            Column3Text = GetTextBoxValue(TextBox_InputText3),
                            Column4Text = GetTextBoxValue(TextBox_InputText2),
                            PrintCopies = GetPrintCopies(),
                            ColumnPositionData = appSettings.ColumnPositionData,
                            TitleSize = appSettings.TitleSize,
                            TitleOffsetY = appSettings.TitleOffsetY,
                            TitleOffsetX = appSettings.TitleOffsetX,
                            BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType,
                            PaperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width,
                            PaperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height,
                            PaperTypeName = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "",
                            IsRotated = PRINT_INFO_CLASS.PrinterInfo.bPaperRotated,
                            GlobalOffsetX = PRINT_INFO_CLASS.PrinterInfo.offset_X,
                            GlobalOffsetY = PRINT_INFO_CLASS.PrinterInfo.offset_Y,
                            PaperGap = PRINT_INFO_CLASS.PrinterInfo.nGAP,
                            // 누락되었던 세부 프린터 옵션 및 다중 컬럼 설정 추가
                            PrinterSettings = BarcodePrint.Model.PrinterSettingsSnapshot.FromPaperClass(PRINT_INFO_CLASS.PrinterInfo),
                            MultiColumnSettingsData = appSettings.MultiColumnSettingsData
                        };

                        if (BarcodePrint.Model.ProjectData.SaveToFile(project, sfd.FileName))
                        {
                            System.Windows.Forms.MessageBox.Show("프로젝트가 저장되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"저장 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btn_OpenProject_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Label Project (*.lmb)|*.lmb|JSON Files (*.json)|*.json";
                    ofd.Title = "프로젝트 불러오기";
                    ofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        BarcodePrint.Model.ProjectData project = BarcodePrint.Model.ProjectData.LoadFromFile(ofd.FileName);
                        if (project != null)
                        {
                            // 텍스트 값 복원
                            TextBox_InputText4.Text = project.Column1Text;
                            TextBox_InputText.Text = project.Column2Text;
                            TextBox_InputText3.Text = project.Column3Text;
                            TextBox_InputText2.Text = project.Column4Text;
                            numericUpDown_PrintCopies.Value = Math.Max(1, Math.Min(9999, project.PrintCopies));

                            // 설정값 및 누락되었던 다중 컬럼 매핑 정보 복원
                            appSettings.ColumnPositionData = project.ColumnPositionData;
                            appSettings.TitleSize = project.TitleSize;
                            appSettings.TitleOffsetY = project.TitleOffsetY;
                            appSettings.TitleOffsetX = project.TitleOffsetX;
                            appSettings.MultiColumnSettingsData = project.MultiColumnSettingsData ?? "";
                            
                            // [중요] 프로젝트의 용지 규격을 appSettings에도 확실히 동기화해 줌 (미리보기 캔버스 크기 연동 및 복원용)
                            appSettings.PaperWidth = project.PaperWidth;
                            appSettings.PaperHeight = project.PaperHeight;
                            appSettings.BarcodeType = project.BarcodeType ?? "QR_CODE";
                            appSettings.IsRotated = project.IsRotated;
                            
                            appSettings.Save();

                            // 용지 및 바코드 타입 복원
                            PRINT_INFO_CLASS.PrinterInfo.BarcodeType = project.BarcodeType;
                            PRINT_INFO_CLASS.PrinterInfo.Paper_Width = project.PaperWidth;
                            PRINT_INFO_CLASS.PrinterInfo.Paper_Height = project.PaperHeight;
                            PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType = project.PaperTypeName;
                            PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = project.IsRotated;
                            PRINT_INFO_CLASS.PrinterInfo.offset_X = project.GlobalOffsetX;
                            PRINT_INFO_CLASS.PrinterInfo.offset_Y = project.GlobalOffsetY;
                            PRINT_INFO_CLASS.PrinterInfo.nGAP = project.PaperGap;

                            // 누락되었던 세부 프린터 옵션 복원
                            if (project.PrinterSettings != null)
                            {
                                project.PrinterSettings.ApplyTo(PRINT_INFO_CLASS.PrinterInfo);
                            }



                            // 로컬 Xprinter 설정 파일에 복원된 용지 설정 즉시 보존
                            try
                            {
                                string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                                PRINT_INFO_CLASS.SaveToFile(settingsPath);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[OpenProject] 로컬 프린터 설정 저장 실패: {ex.Message}");
                            }

                            // [중요] 프로젝트의 레이아웃 및 타이틀 설정을 현재 용지 타입의 설정으로 강제 적용하여 
                            // LoadColumnPositionSettings()에서 올바른 데이터를 불러오게 함
                            string currentPaperType = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "105x145(블랙마크)";
                            SavePaperTypeColumnPositionData(currentPaperType, project.ColumnPositionData);
                            SavePaperTypeTitleData(currentPaperType, project.TitleSize, project.TitleOffsetY);

                            // 공통문구 상태 복원
                            if (touchSwitch_Title != null)
                            {
                                touchSwitch_Title.Checked = project.IsTitleEnabled;
                                if (textBox_Title != null)
                                {
                                    textBox_Title.Text = project.TitleText;
                                    textBox_Title.Enabled = project.IsTitleEnabled;
                                }
                            }

                            // UI 업데이트 및 미리보기 갱신
                            UpdateLabelSizeInfo();
                            LoadColumnPositionSettings();
                            OnPreviewEvent();

                            System.Windows.Forms.MessageBox.Show("프로젝트를 불러왔습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"불러오기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region 프로젝트 버튼 호버 효과

        private void panel_SaveProject_MouseEnter(object sender, EventArgs e)
        {
            panel_SaveProject.BackColor = Color.FromArgb(230, 240, 255); // 연한 파란색
        }

        private void panel_SaveProject_MouseLeave(object sender, EventArgs e)
        {
            panel_SaveProject.BackColor = Color.Transparent;
        }

        private void panel_OpenProject_MouseEnter(object sender, EventArgs e)
        {
            panel_OpenProject.BackColor = Color.FromArgb(230, 240, 255);
        }

        private void panel_OpenProject_MouseLeave(object sender, EventArgs e)
        {
            panel_OpenProject.BackColor = Color.Transparent;
        }

        #endregion

        
        private void label_licenseInfo_Click(object sender, EventArgs e)
        {
            try
            {
                var licenseForm = new LicenseForm();
                licenseForm.StartPosition = FormStartPosition.CenterParent;
                licenseForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"라이센스 정보를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void touchSlider_OffsetX3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_TitleOffsetY_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_Size0_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_Size1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_Size2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_Size3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY0_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY3_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_TitleSize_ValueChanged(object sender, EventArgs e)
        {

        }

    }

    /// <summary>
    /// 다중 컬럼 데이터 구조
    /// </summary>
    public class MultiColumnData
    {
        public string Value { get; set; }
        public bool IsBarcode { get; set; }
        public string BarcodeType { get; set; }
        public string ColumnName { get; set; }
        public int Size { get; set; } = 50; // 기본 크기
        public int BarcodeHeight { get; set; } = 15; // 바코드 세로 크기 (기본값 15)
        public int OffsetX { get; set; } = 0; // 기본 X 위치
        public int OffsetY { get; set; } = 0; // 기본 Y 위치
        public bool ShowBarcodeText { get; set; } = true; // 바코드 값 표시 여부
        public int BarcodeTextOffsetX { get; set; } = 0; // 바코드 값 텍스트 X 오프셋
        public int BarcodeTextOffsetY { get; set; } = 0; // 바코드 값 텍스트 Y 오프셋
        public int BarcodeTextSize { get; set; } = 0; // 바코드 값 텍스트 크기 (0이면 Size의 60% 또는 8)
        public string FontFamily { get; set; } = "맑은 고딕"; // 폰트 종류
        public bool IsBold { get; set; } = true;            // 굵게 여부
        public int CustomWidth { get; set; } = 0;           // 가로폭 (0=자동)
        public int RotateAngle { get; set; } = 0;          // 회전 각도 (0, 90, 180, 270)
        public string Alignment { get; set; } = "Left";   // 정렬 상태: Left, Center, Right
    }
    
    /// <summary>
    /// 컬럼 위치 설정 데이터 클래스
    /// </summary>
    public class ColumnPositionSetting
    {
        public int ColumnIndex { get; set; }
        public bool IsEnabled { get; set; }
        public int Size { get; set; }
        public int BarcodeHeight { get; set; } = 15; // 바코드 세로 크기 (기본값 15)
        public int OffsetX { get; set; } = 0; // 기본 X 위치
        public int OffsetY { get; set; } = 0; // 기본 Y 위치
        public string UsageType { get; set; } = "텍스트"; // 기본값
        public string BarcodeType { get; set; } = "QR_CODE"; // 기본값
        public bool ShowBarcodeText { get; set; } = true; // 바코드 값 표시 (기본값 true)
        public int BarcodeTextOffsetX { get; set; } = 0; // 바코드 값 텍스트 X 오프셋 (mm)
        public int BarcodeTextOffsetY { get; set; } = 0; // 바코드 값 텍스트 Y 오프셋 (mm)
        public int BarcodeTextSize { get; set; } = 0; // 바코드 값 텍스트 크기 (0이면 Size의 60% 또는 8 사용)
        public string FontFamily { get; set; } = "맑은 고딕"; // 폰트 종류
        public bool IsBold { get; set; } = true;            // 굵게 여부
        public int CustomWidth { get; set; } = 0;           // 가로폭 (0=자동)
        public int RotateAngle { get; set; } = 0;          // 회전 각도 (0, 90, 180, 270)
        public string Alignment { get; set; } = "Left";   // 정렬 상태: Left, Center, Right
        public object Tag { get; set; } // UI 컨트롤 참조
    }
}
