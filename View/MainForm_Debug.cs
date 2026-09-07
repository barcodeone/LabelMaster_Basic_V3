using BarcodePrint.Controll;
using BarcodePrint.Model;
using BarcodePrint.Properties;
using BarcodePrint.UserControls;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Management;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using Color = System.Drawing.Color;
using DataFormats = System.Windows.DataFormats;
using DataTable = System.Data.DataTable;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using Font = System.Drawing.Font;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

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

        // 미리보기 이벤트 관련 (참조 프로그램 방식)
        public event EventHandler previewContentEvent;

        Queue<Row_Set> PrintDataQueue;

        Thread printStatusThread;

        public int PreviewNum = 0;

        // 빌드시 변경해야 할 정보들 1.시리얼넘버, 2.회사링크, 3.전화번호
        string[] UserSerials = {
                                    "4501ELW24A240037",
                                    "325BBE23C120001",
                                    "326BBE243220126",
                                    "4501ELW24A240037"
                                };
        //string CompanyLink = "https://printerlabel.co.kr"; // 창성제일
        string CompanyLink = "https://xprinter.co.kr"; // 

        bool IsRelease = false; // 배포 모드 여부
        bool shouldCloseOnLoad = false; // 로드 시 종료 여부 플래그

        // 바코드 미리보기 관련 변수들 (참조 프로그램 방식)
        private string currentBarcodeValue = "";
        public string currentBarcodeText = ""; // 바코드 아래 표시할 텍스트 (엑셀 연동용)
        private int barcodeSize = 50;
        private int barcodeHeight = 15;
        private int barcodeX = 0;
        private int barcodeY = 0;
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
        private string excelFilePath = "";
        private DataTable excelData = null;
        private bool isExcelMode = false;
        private int selectedBarcodeColumn = 0; // 바코드로 사용할 컬럼 인덱스 (기존 호환성)
        private int selectedTextColumn = 1; // 출력할 텍스트 컬럼 인덱스 (기존 호환성)

        // 다중 컬럼 설정 관련 변수들
        private List<ExcelMultiColumnSelectionForm.ColumnSetting> multiColumnSettings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();
        private bool useMultiColumnMode = false; // 다중 컬럼 모드 사용 여부

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
            if (useMultiColumnMode && multiColumnSettings.Count > 0)
            {
                // 다중 컬럼 모드: 첫 번째 바코드 컬럼의 타입 사용
                var firstBarcodeSetting = multiColumnSettings.FirstOrDefault(s => s.IsEnabled && s.UsageType == "바코드");
                if (firstBarcodeSetting != null)
                {
                    return firstBarcodeSetting.BarcodeType;
                }
            }

            // 단일 컬럼 모드 또는 바코드 컬럼이 없는 경우 기본값 사용
            return PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
        }

        private string currentBarcodeType = "QR_CODE"; // 현재 바코드 타입

        // 미리보기 크기는 디자이너에서 고정 설정됨

        public MainForm()
        {
            InitializeComponent();
            InitPictureButton();
            LoadLogo();

            // 포커스와 무관하게 바코드 스캐너 입력을 받기 위한 설정
            this.KeyPreview = true;

            this.CenterToScreen();

            string programName = "Barcode Print";
            string version = " Ver 1.2 (20260130)";
            this.Text = programName + "(" + version + ")";
            label_companyLink.Text = CompanyLink;
            //PrinterSerialLogger.LogProgramStart();

            barcodePrintManager = new BarcodePrintManager(this);
            previewManager = new PreviewManager(this);
            printerList = new MachineList();
            Fontlist = new FontList();

            FileLoad_Excel_data();

            // ★ 중요: setControl()을 가장 먼저 호출하여 PRINT_INFO_CLASS 설정 로드
            // LoadAppSettings()가 PRINT_INFO_CLASS를 덮어쓰지 않도록 순서 변경
            setControl(); // PRINT_INFO_CLASS 로드 (바코드 타입, 용지 사이즈 등)

            // 엑셀 설정 로드 및 적용 (PRINT_INFO_CLASS를 덮어쓰지 않도록 주의)
            LoadAppSettings();

            // 컬럼 위치 설정 로드 (setControl() 이후에 호출하여 정확한 용지 타입 정보 사용)
            LoadColumnPositionSettings();

            // ★ 중요: 바코드 타입 적용 (PRINT_INFO_CLASS → columnPositionSettings)
            ApplyBarcodeTypeFromSettings();

            // useMultiColumnMode 초기화 (엑셀 데이터가 로드되지 않으면 false)
            if (excelData == null || excelData.Rows.Count == 0)
            {
                useMultiColumnMode = false;
                System.Diagnostics.Debug.WriteLine("useMultiColumnMode를 false로 초기화 (엑셀 데이터 없음)");
            }

            // 프로그램 시작 시 파일명 라벨 초기화 및 패널 상태 업데이트 (설정 로드 후 최종 확인)
            UpdateExcelPanelState();

            // 참조 프로젝트 방식: 바코드 설정은 기본값 사용

            SetReceiveFunction();// 컨트롤 앞에서 실행해야 할 이벤트

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

            // 트랙바 초기값 설정 (마지막에 호출하여 UI 업데이트 보장)
            InitializeTrackBars();

            // 프린터 시리얼넘버 추출 및 저장 (생성자에서 동기 처리)
            string printerName = PRINT_INFO_CLASS.PrinterInfo.printerName;
            string printerPort = PRINT_INFO_CLASS.PrinterInfo.printerPortName;
            System.Diagnostics.Debug.WriteLine($"[MainForm 생성자] 프린터 시리얼 번호 확인 시작: 프린터명 = '{printerName}', 포트 = '{printerPort}'");

            // 실제 프린터 연결 상태 확인
            bool isPrinterActuallyConnected = IsPrinterActuallyConnected(printerName, printerPort);
            System.Diagnostics.Debug.WriteLine($"[MainForm 생성자] 실제 프린터 연결 상태: {isPrinterActuallyConnected}");
            //PrinterSerialLogger.WriteLog($"실제 프린터 연결 상태: {(isPrinterActuallyConnected ? "연결됨" : "연결되지 않음")}");

            string normalizedSerial = string.Empty;
            bool serialMatched = false;

            try
            {
                //PrinterSerialLogger.LogPrinterListQueryStart(printerName);

                var usbPrinterInfoList = BarcodePrint.Model.UsbPrinterHelper.GetConnectedUsbPrinters();
                //PrinterSerialLogger.LogPrinterList(usbPrinterInfoList);

                foreach (var usbInfo in usbPrinterInfoList)
                {
                    if (string.IsNullOrEmpty(usbInfo.Serial))
                        continue;

                    string currentSerial = usbInfo.Serial.ToUpperInvariant().Trim().Replace(" ", "");
                    if (UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(currentSerial)))
                    {
                        normalizedSerial = currentSerial;
                        serialMatched = true;
                        PRINT_INFO_CLASS.PrinterInfo.printerName = usbInfo.Name;
                        PRINT_INFO_CLASS.PrinterInfo.printerPortName = usbInfo.PortName;
                        //PrinterSerialLogger.WriteLog($"1단계 시리얼 매칭 성공: {usbInfo.Name} ({currentSerial})");
                        break;
                    }
                }

                if (!serialMatched)
                {
                    //PrinterSerialLogger.WriteLog("1단계 시리얼 매칭 실패. 2단계 하드웨어 정밀 스캔 시작...");
                    //var testResults = UsbPrinterInfoTest.GetUsbPrinterInfo();
                    //foreach (var matchedPrinter in testResults)
                    //{
                    //    if (string.IsNullOrEmpty(matchedPrinter.Serial))
                    //        continue;

                    //    string currentSerial = matchedPrinter.Serial.ToUpperInvariant().Trim().Replace(" ", "");
                    //    if (UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(currentSerial)))
                    //    {
                    //        normalizedSerial = currentSerial;
                    //        serialMatched = true;
                    //        PRINT_INFO_CLASS.PrinterInfo.printerName = matchedPrinter.Name;
                    //        PRINT_INFO_CLASS.PrinterInfo.printerPortName = matchedPrinter.Port;
                    //        //PrinterSerialLogger.WriteLog($"2단계 정밀 스캔 매칭 성공: {matchedPrinter.Name} ({currentSerial})");
                    //        break;
                    //    }
                    //}
                }

                if (!serialMatched)
                {
                    //PrinterSerialLogger.WriteLog("허용된 시리얼 번호를 추출하지 못했습니다.");
                }
            }
            catch (Exception ex)
            {
                //PrinterSerialLogger.LogError("프린터 시리얼번호 추출 중 오류 발생", ex);
            }

            PRINT_INFO_CLASS.PrinterInfo.PrinterSerial = normalizedSerial;

            if (IsRelease)
            {
                //PrinterSerialLogger.LogSerialValidationStart(normalizedSerial, UserSerials);
                bool isSerialAllowed = !string.IsNullOrEmpty(normalizedSerial) &&
                                       UserSerials.Any(s => s.ToUpperInvariant().Trim().Replace(" ", "").Equals(normalizedSerial));

                //PrinterSerialLogger.LogSerialValidationResult(normalizedSerial, isSerialAllowed);

                if (!isSerialAllowed)
                {
                    //PrinterSerialLogger.LogError($"시리얼번호 검증 실패: {normalizedSerial}");
                    shouldCloseOnLoad = true;
                }
            }
            else
            {
                //PrinterSerialLogger.WriteLog($"디버그 모드 - 시리얼번호 검증 건너뜀 (시리얼: {normalizedSerial})");
            }

            //Printer status
            //printStatusThread = new Thread(previewManager.OnStateCheck);
            //printStatusThread.Start();

            // 라벨 크기 정보 초기화
            UpdateLabelSizeInfo();
        }

        /// <summary>
        /// 로고 이미지 로드
        /// </summary>
        private void LoadLogo()
        {
            try
            {
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LabelMaster_Logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    pictureBox_Logo.Image = Image.FromFile(logoPath);
                    System.Diagnostics.Debug.WriteLine($"로고 로드 성공: {logoPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"로고 파일을 찾을 수 없음: {logoPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로고 로드 실패: {ex.Message}");
            }
        }

        private void SetReceiveFunction()
        {
            this.printerList.machineListChanged += ReceiveEventPrinterListUpdate;
            this.previewManager.PrintStateUpdateEvent += ReceiveEventPrinterStateUpdate;
            this.Fontlist.FontListUpdated += ReceiveEventFontListUpdate;

            // TextBox 실시간 입력 이벤트 추가 (가격표용: 품명, 가격, 바코드만)
            this.TextBox_ProductName.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_Price.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_Barcode.TextChanged += TextBox_InputText_TextChanged;
            this.TextBox_Option.TextChanged += TextBox_InputText_TextChanged;

            // 가격 TextBox에 숫자만 입력받도록 KeyPress 이벤트 추가
            this.TextBox_Price.KeyPress += TextBox_Price_KeyPress;

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

        /// <summary>
        /// TextBox에 포커스 이동 (비활성화됨)
        /// </summary>
        private void FocusToTextBox(object state)
        {
            // 자동 포커스 이동 기능 비활성화
            isFocusTimerActive = false;
            return;

            // 아래 코드는 비활성화됨 (필요시 주석 해제)
            /*
            try {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<object>(FocusToTextBox), state);
                    return;
                }

                TextBox_ProductName.Focus();

                // 커서 활성화 및 텍스트 선택
                TextBox_ProductName.SelectAll();
                TextBox_ProductName.SelectionStart = TextBox_ProductName.Text.Length;
                TextBox_ProductName.SelectionLength = 0;

                isFocusTimerActive = false;
            }
            catch (Exception e) {

            }
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
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"[setControl 불러오기] 파일에서 로드 완료");
                    System.Diagnostics.Debug.WriteLine($"  - PaperType: {PRINT_INFO_CLASS.PrinterInfo.PaperType}");
                    System.Diagnostics.Debug.WriteLine($"  - Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width} mm");
                    System.Diagnostics.Debug.WriteLine($"  - Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height} mm");
                    System.Diagnostics.Debug.WriteLine($"  - BarcodeType: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                    System.Diagnostics.Debug.WriteLine($"  - nDarkness: {PRINT_INFO_CLASS.PrinterInfo.nDarkness}");
                    System.Diagnostics.Debug.WriteLine($"  - nPrintSpeed: {PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed}");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[setControl] 설정 파일이 없습니다. 기본값 사용: {settingsPath}");
                    System.Diagnostics.Debug.WriteLine($"  - BarcodeType 기본값: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE"}");
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

                // 첫 번째로 연결된 Xprinter 프린터 찾기
                for (int i = 0; i < minCount; i++)
                {
                    try
                    {
                        if (USB_status.Connect[i] && USB_status.Name[i].Contains("Xprinter"))
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

            // 미리보기 이벤트 해제 (참조 프로그램 방식)
            previewManager.PaintPreviewEvent -= ReceivePaint;
            previewManager.PreviewEventClose();
            //this.recv.RecvThreadEvent -= RecvDataUpdate;
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainForm_Load] 시작");

            ConfigureMaterialSkin();

            System.Diagnostics.Debug.WriteLine("[MainForm_Load] MaterialSkin 설정 완료");

            // 시리얼 번호 검증 실패 시 메시지 표시 후 종료
            if (shouldCloseOnLoad)
            {
                System.Diagnostics.Debug.WriteLine("[MainForm_Load] 시리얼 매칭 실패로 인한 종료 처리");

                //PrinterSerialLogger.LogError("시리얼 매칭 실패로 프로그램 종료");
                //PrinterSerialLogger.LogProgramEnd();

                string serial = PRINT_INFO_CLASS.PrinterInfo.PrinterSerial;

                // 주소라벨 프로젝트 방식의 메시지 표시
                System.Windows.Forms.MessageBox.Show(
                    "프린터 시리얼 번호가 일치하지 않습니다.\n\n" +
                    "프린터 시리얼 넘버 : " + serial + "\n" +
                    "구매처에 문의하시기 바랍니다.\n-한국Xprinter-",
                    "라벨마스터 for 바코드",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                System.Diagnostics.Debug.WriteLine("[MainForm_Load] 프로그램 종료");
                this.Close(); // 폼이 완전히 로드된 후 종료
                return;
            }

            // 화면 비율 깨짐 방지: 폼 크기와 위치를 명시적으로 설정
            FixFormSizeAndPosition();

            // Anchor/Dock으로 인한 위치 오차 방지: 모든 컨트롤 위치 명시적으로 설정
            FixControlsSizeAndPosition();

            // 초기 모드 설정 (엑셀 출력)
            SwitchToExcelMode();

            // 인쇄매수 컨트롤을 최상단에 표시
            if (numericUpDown_PrintCopies != null)
            {
                numericUpDown_PrintCopies.BringToFront();
                numericUpDown_PrintCopies.Invalidate();
            }

            // 초기 미리보기 트리거 (Title 설정 반영 포함)
            // LoadAppSettings()에서 설정된 Title 값이 미리보기에 반영되도록 보장
            System.Diagnostics.Debug.WriteLine($"[MainForm_Load 미리보기] touchSwitch_Title.Checked={touchSwitch_Title?.Checked}, textBox_Title.Text='{textBox_Title?.Text}', GetSenderReceiverLabel()='{GetSenderReceiverLabel()}'");
            OnPreviewEvent();

            // 이벤트 수동 연결 (디자이너 오류 회피)
            if (label_license != null)
            {
                label_license.Click += label_license_Click;
                label_license.MouseEnter += label_license_MouseEnter;
                label_license.MouseLeave += label_license_MouseLeave;
            }

            if (label_licenseInfo != null)
            {
                label_licenseInfo.Click += label_licenseInfo_Click;
                label_licenseInfo.MouseEnter += label_licenseInfo_MouseEnter;
                label_licenseInfo.MouseLeave += label_licenseInfo_MouseLeave;
            }

            if (touchSwitch_Title != null)
            {
                touchSwitch_Title.CheckedChanged += touchSwitch_Title_CheckedChanged;
            }

            if (textBox_Title != null)
            {
                textBox_Title.TextChanged += textBox_Title_TextChanged;
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

                System.Diagnostics.Debug.WriteLine("[컨트롤 위치 수정] 모든 Anchor/Dock 제거 완료");
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

            // 모드 변경 저장
            appSettings.CurrentMode = "Manual";
            appSettings.Save();
            System.Diagnostics.Debug.WriteLine($"[모드 전환] 직접 출력 모드로 변경 및 저장");
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

            // 모드 변경 저장
            appSettings.CurrentMode = "Excel";
            appSettings.Save();
            System.Diagnostics.Debug.WriteLine($"[모드 전환] 엑셀 출력 모드로 변경 및 저장");
        }

        /// <summary>
        /// 직접 출력 모드로 전환
        /// </summary>
        private void SwitchToManualMode()
        {
            try
            {
                // 왼쪽: 수동 입력 컨트롤 표시 (가격표용: 품명, 가격, 바코드만)
                TextBox_ProductName.Visible = true; // 품명 표시
                TextBox_Price.Visible = true; // 가격 표시
                TextBox_Barcode.Visible = true; // 바코드 표시
                TextBox_Option.Visible = true; // 비고 표시
                label_ProductName.Visible = true; // 품명 라벨 표시
                label_Price.Visible = true; // 가격 라벨 표시
                label_Barcode.Visible = true; // 바코드 라벨 표시
                label_Option.Visible = true; // 비고 라벨 표시

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
                panel_PrintButton.Enabled = true;

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
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 이름, 전화번호
        /// </summary>
        private void RestoreManualModeLabels()
        {
            try
            {
                // 가격표용 라벨 텍스트
                label_ProductName.Text = "품명";     // 첫 번째 필드
                label_Price.Text = "가격";    // 두 번째 필드
                label_Barcode.Text = "바코드";     // 세 번째 필드
                label_Option.Text = "비고";       // 네 번째 필드
                label_ProductName.Visible = true;      // 품명 라벨 표시
                label_Price.Visible = true;      // 가격 라벨 표시
                label_Barcode.Visible = true;      // 바코드 라벨 표시
                label_Option.Visible = true;       // 비고 라벨 표시

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

                // 타이틀만 입력되어 있어도 미리보기를 업데이트해야 함
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
                // 왼쪽: 수동 입력 컨트롤 숨김 (가격표용)
                TextBox_ProductName.Visible = false;
                TextBox_Price.Visible = false;
                TextBox_Barcode.Visible = false;
                label_ProductName.Visible = false;
                label_Price.Visible = false;
                label_Barcode.Visible = false;

                TextBox_Option.Visible = false;
                label_Option.Visible = false;

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
                // 엑셀 데이터가 있으면 버튼 활성화, 없으면 비활성화
                panel_PrintButton.Enabled = (excelData != null && excelData.Rows.Count > 0);

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

        /// <summary>
        /// 애플리케이션 종료 전 정리 작업
        /// </summary>
        private void CleanupBeforeExit()
        {
            try
            {
                // 스레드 종료 (timeout 추가)
                if (printStatusThread != null && printStatusThread.IsAlive)
                {
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

                if (barcodePrintManager != null)
                {
                    barcodePrintManager.ClearPrintQueue(PRINT_INFO_CLASS.PrinterInfo.printerName);
                }

                // 포커스 타이머 정리
                if (focusTimer != null)
                {
                    focusTimer.Dispose();
                    focusTimer = null;
                }

                // SaveData() 제거 (중복 저장 방지, FormClosing에서 저장)
                Unsubscribe_Event();
                printStatusThread = null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"CleanupBeforeExit 오류: {e.Message}");
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
        public void PrintStart()
        {
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

            foreach (var proc in Process.GetProcessesByName("chrome")) // 브라우저 이름은 환경에 맞게 변경
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



        private void MainForm_Shown(object sender, EventArgs e)
        {
            /*
            Thread.Sleep(500);
            OpenExcelFileDlg();
            */

            // Shown 이벤트에서도 한 번 더 화면 비율 확인 (Load 이후 자동 스케일링 방지)
            FixFormSizeAndPosition();

            // Anchor/Dock으로 인한 위치 오차 재확인
            FixControlsSizeAndPosition();
        }
        private void new_PrintButton(float shadowRate = 1f)
        {
            // 모드에 따라 버튼 텍스트 변경
            string ButtonText = isExcelMode ? "엑셀 출력" : "인쇄";
            System.Drawing.Font TextFont = new System.Drawing.Font("맑은 고딕", isExcelMode ? 18 : 20, System.Drawing.FontStyle.Bold);

            Color borderColor = Color.DodgerBlue; // 테두리 색

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
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
                    // 중앙에 "Print" 텍스트 그리기
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
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

        // new_ExcelPrintButton 메서드는 제거됨 (new_PrintButton에서 모드에 따라 텍스트 변경)

        private void new_OpenAdjustFormButton(float shadowRate = 1f)
        {
            string ButtonText = "고급 설정";
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
                    // 중앙에 "고급 설정" 텍스트 그리기
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
            if (!isExcelMode)
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

        /// <summary>
        /// 바코드 입력 처리 공통 메서드 (가격표용에는 사용 안 함)
        /// </summary>
        /*
        private void ProcessBarcodeInput(bool isFromScanner = false)
        {
            // 가격표용 프로그램에서는 바코드 스캐너 입력 기능을 사용하지 않음
            // 필요 시 주석 해제하여 사용
        }
        */



        /// <summary>
        /// 전역 키 입력 처리 (비활성화 - TextBox별 KeyDown 이벤트 사용)
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // ProcessCmdKey를 통한 전역 엔터키 처리 비활성화
            // (각 TextBox의 KeyDown 이벤트에서 개별 처리)

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // TextBox_InputText는 가격표용에서 제거됨

        private void TextBox_Price_KeyDown(object sender, KeyEventArgs e)
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

        private void TextBox_Price_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// 가격 TextBox에 숫자만 입력받도록 제한
        /// </summary>
        private void TextBox_Price_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 숫자(0-9), 백스페이스, Delete, Tab, Enter 키만 허용
            if (char.IsDigit(e.KeyChar) ||
                e.KeyChar == (char)Keys.Back ||
                e.KeyChar == (char)Keys.Delete ||
                e.KeyChar == (char)Keys.Tab ||
                e.KeyChar == (char)Keys.Enter)
            {
                // 허용된 키는 그대로 처리
                return;
            }

            // 그 외의 키는 차단
            e.Handled = true;
        }

        private void TextBox_Option_KeyDown(object sender, KeyEventArgs e)
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

        private void TextBox_Option_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_ProductName_KeyDown(object sender, KeyEventArgs e)
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

        private void TextBox_ProductName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_Barcode_KeyDown(object sender, KeyEventArgs e)
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

        private void TextBox_Barcode_KeyUp(object sender, KeyEventArgs e)
        {
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }



        // 가격표용으로 TextBox_ProductName, TextBox_Price, TextBox_Barcode만 사용

        // TextBox_InputText4는 가격표용에서 제거됨

        // 슬라이더바 이벤트 핸들러는 PositionAdjustmentForm으로 이동됨
        // 슬라이더바 관련 이벤트 핸들러 제거됨

        private void panel_PrintButton_Click(object sender, EventArgs e)
        {
            // 모드에 따라 다른 기능 실행
            if (isExcelMode)
            {
                // 엑셀 출력 모드
                panel_ExcelPrintButton_Click(sender, e);
            }
            else
            {
                // 직접 출력 모드 (가격표용: 품명, 가격, 바코드)

                // 가격표용 입력 데이터 가져오기 (품명, 가격, 바코드만 사용)
                // columnPositionSettings 인덱스와 매칭 (가격표용):
                // 인덱스 0: 품명 (TextBox_ProductName)
                // 인덱스 1: 가격 (TextBox_Price)
                // 인덱스 2: 비고 (TextBox_Option)
                // 인덱스 3: 바코드 (TextBox_Barcode)
                string productName = GetTextBoxValue(TextBox_ProductName);
                string price = GetTextBoxValue(TextBox_Price);
                string barcodeValue = GetTextBoxValue(TextBox_Barcode);
                string optionValue = GetTextBoxValue(TextBox_Option);

                // 가격 데이터에 자동 포맷 적용 (인쇄 시)
                if (!string.IsNullOrWhiteSpace(price))
                {
                    price = BarcodePrint.Model.PriceFormatHelper.FormatPrice(price);
                }

                var textBoxInputs = new List<string>
                {
                    productName,   // 인덱스 0: 품명
                    price,         // 인덱스 1: 가격 (포맷 적용됨)
                    optionValue,   // 인덱스 2: 비고
                    barcodeValue   // 인덱스 3: 바코드
                };

                System.Diagnostics.Debug.WriteLine($"[직접 인쇄] 가격표용 입력 데이터:");
                System.Diagnostics.Debug.WriteLine($"  품명 (인덱스 0): '{textBoxInputs[0]}'");
                System.Diagnostics.Debug.WriteLine($"  가격 (인덱스 1): '{textBoxInputs[1]}'");
                System.Diagnostics.Debug.WriteLine($"  비고 (인덱스 2): '{textBoxInputs[2]}'");
                System.Diagnostics.Debug.WriteLine($"  바코드 (인덱스 3): '{textBoxInputs[3]}'");

                // 직접 출력 버튼은 TextBox 입력값을 직접 출력 (가격표용: 다중 입력 방식 사용)
                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    ProcessMultipleInputsWithSettings(textBoxInputs);
                }
                else
                {
                    // 설정이 없으면 기본 설정으로 초기화 후 처리
                    LoadColumnPositionSettings();
                    ProcessMultipleInputsWithSettings(textBoxInputs);
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

                // ★ 가격표용: inputs 인덱스와 columnPositionSettings 인덱스가 동일
                // inputs 인덱스: 0(품명), 1(가격), 2(비고), 3(바코드)
                // columnPositionSettings 인덱스: 0(품명), 1(가격), 2(비고), 3(바코드)
                int[] usedIndices = { 0, 1, 2, 3 }; // ★ 수정: 가격표용 인덱스

                foreach (int colIndex in usedIndices)
                {
                    if (colIndex >= inputs.Count || colIndex >= columnPositionSettings.Count)
                    {
                        System.Diagnostics.Debug.WriteLine($"[다중 입력] 컬럼 {colIndex} 건너김: 인덱스 범위 초과 (inputs={inputs.Count}, settings={columnPositionSettings.Count})");
                        continue;
                    }

                    var setting = columnPositionSettings[colIndex];
                    var input = inputs[colIndex];

                    System.Diagnostics.Debug.WriteLine($"[다중 입력 체크] 컬럼 {colIndex}: ColumnIndex={setting.ColumnIndex}, 값='{input}', UsageType={setting.UsageType}, IsEnabled={setting.IsEnabled}");

                    bool shouldInclude = !string.IsNullOrEmpty(input);

                    if (shouldInclude)
                    {
                        // 바코드 여부 확인
                        bool isBarcode = setting.UsageType == "바코드";

                        // 가격표용 - ColumnName을 명확하게 설정
                        string columnName = setting.ColumnIndex == 0 ? "품명" :
                                          setting.ColumnIndex == 1 ? "가격" :
                                          setting.ColumnIndex == 2 ? "비고" : "바코드";

                        // 바코드 타입 결정
                        string barcodeType = isBarcode ? (setting.BarcodeType ?? BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE") : setting.BarcodeType;

                        // 바코드는 텍스트를 출력하지 않도록 설정
                        bool showBarcodeText = isBarcode ? false : true;

                        multiColumnData.Add(new MultiColumnData
                        {
                            Value = input,
                            IsBarcode = isBarcode,
                            BarcodeType = barcodeType,
                            ColumnName = columnName,
                            Size = setting.Size,
                            BarcodeHeight = setting.BarcodeHeight,
                            OffsetX = setting.OffsetX,
                            OffsetY = setting.OffsetY,
                            ShowBarcodeText = showBarcodeText
                        });

                        System.Diagnostics.Debug.WriteLine($"[다중 입력 추가 ✓] 컬럼 {colIndex}: ColumnName='{columnName}', 값='{input}', IsBarcode={isBarcode}, BarcodeType={barcodeType}, 크기={setting.Size}, X={setting.OffsetX}, Y={setting.OffsetY}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[다중 입력 제외 ✗] 컬럼 {colIndex}: 값이 비어있음");
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
        /// 프린터 이름에서 괄호와 그 안의 텍스트를 제거하여 정규화
        /// 예: "Xprinter XP-T265B (1 복사)" -> "Xprinter XP-T265B"
        /// </summary>
        private string RemoveParenthesesFromPrinterName(string printerName)
        {
            if (string.IsNullOrEmpty(printerName))
                return printerName;

            // 괄호로 시작하는 부분 제거 (예: " (1 복사)", " (복사 1)" 등)
            int bracketIndex = printerName.IndexOf('(');
            if (bracketIndex >= 0)
            {
                return printerName.Substring(0, bracketIndex).Trim();
            }

            return printerName.Trim();
        }

        #region 바코드 미리보기 이벤트 핸들러

        /// <summary>
        /// TextBox 실시간 입력 변경 이벤트 (미리보기 업데이트)
        /// </summary>
        private void TextBox_InputText_TextChanged(object sender, EventArgs e)
        {
            // 자동 지우기 중이면 미리보기를 변경하지 않음
            if (isAutoClearingTextBox)
            {
                return;
            }

            // 가격 TextBox인 경우 자동 포맷 적용
            if (sender == TextBox_Price)
            {
                ApplyPriceFormat();
            }

            // 모든 텍스트 변경은 미리보기 업데이트를 트리거함
            // GetMultiInputData()에서 각 TextBox의 값을 읽어 처리하므로 개별 할당 불필요
            OnPreviewEvent();
        }

        /// <summary>
        /// 가격 TextBox에 자동으로 원화 기호와 천단위 구분 기호 적용
        /// </summary>
        private void ApplyPriceFormat()
        {
            try
            {
                if (TextBox_Price == null)
                    return;

                string currentText = TextBox_Price.Text;

                System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] 시작 - 현재 텍스트: '{currentText}'");

                // 빈 문자열이거나 placeholder인 경우 처리 안 함
                if (string.IsNullOrWhiteSpace(currentText) ||
                    currentText.StartsWith("예:", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] 건너김 (빈 값 또는 placeholder)");
                    return;
                }

                // 현재 커서 위치 저장
                int selectionStart = TextBox_Price.SelectionStart;
                int selectionLength = TextBox_Price.SelectionLength;

                // 숫자만 추출 (원화 기호, 쉼표 제거)
                string numericValue = BarcodePrint.Model.PriceFormatHelper.ExtractNumericValue(currentText);

                // 숫자가 없으면 빈 문자열로 설정
                if (string.IsNullOrWhiteSpace(numericValue))
                {
                    if (!string.IsNullOrWhiteSpace(currentText))
                    {
                        isAutoClearingTextBox = true;
                        TextBox_Price.Text = "";
                        isAutoClearingTextBox = false;
                    }
                    return;
                }

                // 가격 포맷 적용 (숫자만 전달)
                string formattedPrice = BarcodePrint.Model.PriceFormatHelper.FormatPrice(numericValue);

                System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] 포맷 결과: '{numericValue}' → '{formattedPrice}'");

                // 포맷이 변경된 경우에만 업데이트
                if (formattedPrice != currentText)
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] TextBox 업데이트: '{currentText}' → '{formattedPrice}'");

                    // TextChanged 이벤트가 재귀적으로 발생하지 않도록 플래그 설정
                    isAutoClearingTextBox = true;

                    TextBox_Price.Text = formattedPrice;

                    // 커서 위치 복원 (포맷된 텍스트 길이에 맞게 조정)
                    // 원화 기호와 쉼표를 고려하여 커서 위치 계산
                    int newCursorPosition = formattedPrice.Length;
                    if (selectionStart > 0 && selectionStart <= currentText.Length)
                    {
                        // 원본 텍스트에서 숫자만 추출한 위치 계산
                        int numericPosition = 0;
                        for (int i = 0; i < Math.Min(selectionStart, currentText.Length); i++)
                        {
                            if (char.IsDigit(currentText[i]))
                            {
                                numericPosition++;
                            }
                        }

                        // 포맷된 텍스트에서 해당 위치 찾기
                        int formattedPosition = 1; // 원화 기호 다음부터
                        int digitCount = 0;
                        for (int i = 1; i < formattedPrice.Length && digitCount < numericPosition; i++)
                        {
                            if (char.IsDigit(formattedPrice[i]))
                            {
                                digitCount++;
                                formattedPosition = i + 1;
                            }
                        }
                        newCursorPosition = formattedPosition;
                    }

                    TextBox_Price.SelectionStart = Math.Min(newCursorPosition, formattedPrice.Length);
                    TextBox_Price.SelectionLength = 0;

                    isAutoClearingTextBox = false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] 변경 없음 - TextBox 업데이트 안 함");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyPriceFormat] 오류: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("[MainForm] AdjustForm 생성 시작");
                System.Diagnostics.Debug.WriteLine($"  - 현재 BarcodeType: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                System.Diagnostics.Debug.WriteLine($"  - 현재 Paper_Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width} mm");
                System.Diagnostics.Debug.WriteLine($"  - 현재 Paper_Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height} mm");
                System.Diagnostics.Debug.WriteLine("========================================");

                AdjustForm adjustForm = new AdjustForm(this);

                // 바코드 타입 변경 이벤트는 ExcelMultiColumnSelectionForm에서 처리하므로 제거됨
                // adjustForm.BarcodeTypeChanged += AdjustForm_BarcodeTypeChanged;

                // AdjustForm이 닫힐 때 포커스 이동 이벤트 구독
                adjustForm.FormClosed += AdjustForm_FormClosed;

                adjustForm.StartPosition = FormStartPosition.CenterParent;
                System.Diagnostics.Debug.WriteLine("[MainForm] AdjustForm 표시 시작 (ShowDialog)");
                adjustForm.ShowDialog(this);
                System.Diagnostics.Debug.WriteLine("[MainForm] AdjustForm 표시 완료");

                // 포커스 자동 이동 처리 제거 (AdjustForm이 닫힐 때 처리)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] AdjustForm 생성/표시 오류: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                MessageBox.Show($"AdjustForm을 열 수 없습니다.\n{ex.Message}", "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 위치조절 폼 열기 버튼 클릭 이벤트
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
                        "알림", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
            }
            else
            {
                label_ExcelFileName.Text = "";
                label_ExcelFileName.Visible = false;
            }
        }

        /// <summary>
        /// 컬럼명 라벨 업데이트
        /// </summary>
        private void UpdateColumnLabels(List<ExcelMultiColumnSelectionForm.ColumnSetting> settings)
        {
            try
            {
                var labels = new[] { label_ProductName, label_Price, label_Option, label_Barcode }; // 가격표용: 품명, 가격, 비고, 바코드

                // 먼저 모든 라벨 초기화
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].Text = $"컬럼 {i + 1}:";
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
                            labels[setting.ColumnIndex].Text = $"컬럼 {setting.ColumnIndex + 1}:";
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
                label_ProductName.Text = "컬럼 1(품명):";  // 인덱스 0
                label_Price.Text = "컬럼 2(가격):";  // 인덱스 1
                label_Option.Text = "컬럼 3(비고):"; // 인덱스 2
                label_Barcode.Text = "컬럼 4(바코드):"; // 인덱스 3
                label_ProductName.Visible = true;          // 품명 라벨 표시
                label_Price.Visible = true;          // 가격 라벨 표시
                label_Option.Visible = true;           // 비고 라벨 표시
                label_Barcode.Visible = true;          // 바코드 라벨 표시
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
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 모든 TextBox 내용 삭제
        /// </summary>
        private void ClearAllTextBoxes()
        {
            try
            {
                TextBox_ProductName.Text = "";      // 품명
                TextBox_Price.Text = "";            // 가격
                TextBox_Option.Text = "";           // 비고
                TextBox_Barcode.Text = "";          // 바코드
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TextBox 내용 삭제 오류: {ex.Message}");
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

                if (string.IsNullOrWhiteSpace(appSettings.MultiColumnSettingsData))
                {
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 컬럼 설정 없음");
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"  - 저장된 데이터: {appSettings.MultiColumnSettingsData}");

                // 형식: "Index|ExcelCol|Name|Enabled|UsageType|BarcodeType|Size|X|Y|ShowText;..."
                // 또는 구형 형식: "Name:UsageType|..."
                string data = appSettings.MultiColumnSettingsData;
                multiColumnSettings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();

                // 1. 먼저 신규 형식 (;) 시도
                if (data.Contains(";"))
                {
                    var settingsParts = data.Split(';');
                    foreach (var part in settingsParts)
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;
                        RestoreNewFormatColumn(part);
                    }
                }
                // 2. 단일 컬럼인 경우 (|가 많으면 신규 형식)
                else if (data.Count(c => c == '|') >= 4)
                {
                    RestoreNewFormatColumn(data);
                }
                // 3. 구형 형식 (| 또는 없음)
                else
                {
                    var settingsParts = data.Split('|');
                    foreach (var part in settingsParts)
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;
                        var columnParts = part.Split(':');
                        if (columnParts.Length >= 2)
                        {
                            multiColumnSettings.Add(new ExcelMultiColumnSelectionForm.ColumnSetting
                            {
                                ColumnName = columnParts[0],
                                UsageType = columnParts[1],
                                IsEnabled = true // 구형은 기본 활성화
                            });
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 복원] 완료 - {multiColumnSettings.Count}개 컬럼");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[다중 컬럼 설정 복원 오류] {ex.Message}");
                multiColumnSettings = new List<ExcelMultiColumnSelectionForm.ColumnSetting>();
            }
        }

        /// <summary>
        /// 신규 형식의 컬럼 데이터 한 줄을 파싱하여 multiColumnSettings에 추가
        /// </summary>
        private void RestoreNewFormatColumn(string part)
        {
            var columnParts = part.Split('|');
            if (columnParts.Length >= 5)
            {
                var setting = new ExcelMultiColumnSelectionForm.ColumnSetting();
                setting.ColumnIndex = int.TryParse(columnParts[0], out int idx) ? idx : 0;
                setting.ExcelColumnNumber = int.TryParse(columnParts[1], out int exCol) ? exCol : 1;
                setting.ColumnName = columnParts[2];
                setting.IsEnabled = bool.TryParse(columnParts[3], out bool enabled) ? enabled : false;
                setting.UsageType = columnParts[4];

                if (columnParts.Length >= 10)
                {
                    setting.BarcodeType = columnParts[5];
                    setting.Size = int.TryParse(columnParts[6], out int size) ? size : 50;
                    setting.OffsetX = int.TryParse(columnParts[7], out int ox) ? ox : 0;
                    setting.OffsetY = int.TryParse(columnParts[8], out int oy) ? oy : 0;
                    setting.ShowBarcodeText = bool.TryParse(columnParts[9], out bool show) ? show : true;
                }
                multiColumnSettings.Add(setting);
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
                if (string.IsNullOrEmpty(savedBarcodeType))
                {
                    savedBarcodeType = "QR_CODE"; // 기본값
                }

                System.Diagnostics.Debug.WriteLine($"  - PRINT_INFO_CLASS 바코드 타입: {savedBarcodeType}");

                // 모든 바코드 컬럼의 BarcodeType 업데이트
                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    int updatedCount = 0;
                    foreach (var setting in columnPositionSettings)
                    {
                        // 가격표용: ColumnIndex == 3 (바코드 컬럼) 또는 UsageType == "바코드"
                        if (setting.ColumnIndex == 3 || setting.UsageType == "바코드")
                        {
                            setting.BarcodeType = savedBarcodeType;
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"  - 컬럼 {setting.ColumnIndex} 업데이트: {setting.BarcodeType}");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"  - 총 {updatedCount}개 컬럼 업데이트됨");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  - ⚠ columnPositionSettings가 비어있음");
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
        /// 통합 설정 로드 (엑셀 + 주소 통합)
        /// </summary>
        private void LoadAppSettings()
        {
            try
            {
                appSettings.Load();

                // 회전 상태 복원
                BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = appSettings.IsRotated;

                // 다중 컬럼 설정 복원
                RestoreMultiColumnSettings();

                // 엑셀 파일 경로 복원 및 자동 로드
                if (appSettings.IsExcelFileValid())
                {
                    excelFilePath = appSettings.ExcelFilePath;
                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"[엑셀 파일 자동 연동] 시작");
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 경로: {excelFilePath}");
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 컬럼 설정: {(multiColumnSettings != null && multiColumnSettings.Count > 0 ? $"{multiColumnSettings.Count}개" : "없음")}");

                    // 파일이 실제로 존재하는지 확인
                    if (File.Exists(excelFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 파일 존재 확인됨, 자동 로드 시작");

                        try
                        {
                            // 엑셀 데이터 자동 로드 (multiColumnSettings가 있으면 다이얼로그 건너뛰기)
                            LoadExcelData(excelFilePath, false);
                            System.Diagnostics.Debug.WriteLine($"  - ✓ 엑셀 파일 자동 로드 완료");
                        }
                        catch (Exception loadEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - ✗ 엑셀 파일 자동 로드 실패: {loadEx.Message}");
                            // 실패해도 계속 진행
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

                // 저장된 모드로 전환
                string savedMode = appSettings.CurrentMode;
                if (savedMode == "Excel" && excelData != null && excelData.Rows.Count > 0)
                {
                    isExcelMode = true;
                    SwitchToExcelMode();
                    System.Diagnostics.Debug.WriteLine($"[모드 복원] 엑셀 모드로 전환");

                    // 엑셀 파일 재로드 후 미리보기 업데이트 보장
                    if (excelData != null && excelData.Rows.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[모드 복원] 엑셀 데이터로 미리보기 업데이트");
                        UpdatePreviewWithFirstRow();
                    }
                }
                else if (savedMode == "Manual")
                {
                    isExcelMode = false;
                    SwitchToManualMode();
                    System.Diagnostics.Debug.WriteLine($"[모드 복원] 직접 출력 모드로 전환");
                }

                // 엑셀 모드 상태 설정
                panel_OpenExcelForm.Enabled = isExcelMode;
                panel_PrintButton.Enabled = isExcelMode || !isExcelMode; // 항상 활성화
                panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트

                // 파일명 라벨 업데이트 (엑셀 모드이고 파일이 있으면 항상 표시)
                if (isExcelMode && !string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
                {
                    UpdateExcelFileNameLabel();
                }
                else
                {
                    label_ExcelFileName.Text = "";
                    label_ExcelFileName.Visible = false;
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
                        textBox_Title.Text = "공용문구";
                    }

                    textBox_Title.Enabled = touchSwitch_Title.Checked;
                }

                // 용지 및 바코드 기본 설정 로드 (appSettings -> PRINT_INFO_CLASS)
                if (appSettings.PaperWidth > 0)
                    PRINT_INFO_CLASS.PrinterInfo.Paper_Width = appSettings.PaperWidth;
                if (appSettings.PaperHeight > 0)
                    PRINT_INFO_CLASS.PrinterInfo.Paper_Height = appSettings.PaperHeight;
                if (!string.IsNullOrEmpty(appSettings.BarcodeType))
                    PRINT_INFO_CLASS.PrinterInfo.BarcodeType = appSettings.BarcodeType;

                System.Diagnostics.Debug.WriteLine($"[설정 로드] 용지 크기: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width}x{PRINT_INFO_CLASS.PrinterInfo.Paper_Height}mm, 바코드 타입: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");

                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 완료: ExcelMode={isExcelMode}, SenderReceiverLabel={appSettings.SenderReceiverLabel}");

                // Title 설정 후 미리보기 즉시 업데이트는 MainForm_Load에서 처리
                // (SetReceiveFunction이 호출된 후에 previewContentEvent가 연결되므로)
                System.Diagnostics.Debug.WriteLine($"[Title 초기화] touchSwitch_Title.Checked={touchSwitch_Title?.Checked}, textBox_Title.Text='{textBox_Title?.Text}', GetSenderReceiverLabel()='{GetSenderReceiverLabel()}'");

                // 마지막 입력값 복원 (가격표용: 품명, 가격, 바코드)
                if (!string.IsNullOrWhiteSpace(appSettings.LastProductName))
                {
                    TextBox_ProductName.Text = appSettings.LastProductName;
                    System.Diagnostics.Debug.WriteLine($"[입력값 복원] 품명: '{appSettings.LastProductName}'");
                }

                if (!string.IsNullOrWhiteSpace(appSettings.LastPrice))
                {
                    TextBox_Price.Text = appSettings.LastPrice;
                    System.Diagnostics.Debug.WriteLine($"[입력값 복원] 가격: '{appSettings.LastPrice}'");
                }

                if (!string.IsNullOrWhiteSpace(appSettings.LastBarcode))
                {
                    TextBox_Barcode.Text = appSettings.LastBarcode;
                    System.Diagnostics.Debug.WriteLine($"[입력값 복원] 바코드: '{appSettings.LastBarcode}'");
                }

                // 인쇄 매수 복원
                if (numericUpDown_PrintCopies != null && appSettings.LastPrintCopies > 0)
                {
                    numericUpDown_PrintCopies.Value = appSettings.LastPrintCopies;
                    System.Diagnostics.Debug.WriteLine($"[입력값 복원] 인쇄 매수: {appSettings.LastPrintCopies}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 로드 오류: {ex.Message}");

                // 예외 발생 시 기본 상태 설정
                isExcelMode = false;
                label_ExcelFileName.Text = "";
                label_ExcelFileName.Visible = false;
                panel_OpenExcelForm.Enabled = false;
                panel_PrintButton.Enabled = true; // 직접 출력 모드에서는 항상 활성화
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
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[SaveAppSettings] 시작");

                // 엑셀 설정
                appSettings.ExcelFilePath = excelFilePath;
                appSettings.IsExcelModeEnabled = isExcelMode;

                // 현재 모드 저장
                appSettings.CurrentMode = isExcelMode ? "Excel" : "Manual";

                // 엑셀 다중 컬럼 설정 저장
                if (multiColumnSettings != null && multiColumnSettings.Count > 0)
                {
                    var settingsLines = new List<string>();
                    foreach (var setting in multiColumnSettings)
                    {
                        // ★ 중요: 현재 레이아웃 설정(Size, X, Y, BarcodeType 등)과 동기화
                        var pos = columnPositionSettings.FirstOrDefault(p => p.ColumnIndex == setting.ColumnIndex);
                        if (pos != null)
                        {
                            setting.IsEnabled = pos.IsEnabled;
                            setting.UsageType = pos.UsageType;
                            setting.BarcodeType = pos.BarcodeType;
                            setting.Size = pos.Size;
                            setting.OffsetX = pos.OffsetX;
                            setting.OffsetY = pos.OffsetY;
                            setting.ShowBarcodeText = pos.ShowBarcodeText;
                        }

                        // 형식: "index|enabled|colName|usageType|barcodeType|size|offsetX|offsetY|showBarcodeText|excelColumnNumber|prefix|suffix|usePrefixNewLine|useSuffixNewLine"
                        settingsLines.Add($"{setting.ColumnIndex}|{setting.IsEnabled}|{setting.ColumnName}|{setting.UsageType}|{setting.BarcodeType}|{setting.Size}|{setting.OffsetX}|{setting.OffsetY}|{setting.ShowBarcodeText}|{setting.ExcelColumnNumber}|{setting.Prefix ?? ""}|{setting.Suffix ?? ""}|{setting.UsePrefixNewLine}|{setting.UseSuffixNewLine}");
                    }
                    appSettings.MultiColumnSettingsData = string.Join(";", settingsLines);
                    System.Diagnostics.Debug.WriteLine($"  - 다중 컬럼 설정 저장: {multiColumnSettings.Count}개 (레이아웃 동기화 완료)");
                }
                else
                {
                    appSettings.MultiColumnSettingsData = "";
                }

                System.Diagnostics.Debug.WriteLine($"  - 엑셀 파일 경로: {excelFilePath ?? "없음"}");
                System.Diagnostics.Debug.WriteLine($"  - 엑셀 모드 활성화: {isExcelMode}");
                System.Diagnostics.Debug.WriteLine($"  - 현재 모드: {appSettings.CurrentMode}");

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
                    appSettings.SenderReceiverLabel = "공용문구";
                }

                // 현재 입력값 저장 (가격표용: 품명, 가격, 바코드, 인쇄 매수)
                appSettings.LastProductName = GetTextBoxValue(TextBox_ProductName);
                appSettings.LastPrice = TextBox_Price?.Text?.Trim() ?? ""; // 포맷된 상태 그대로 저장
                appSettings.LastBarcode = GetTextBoxValue(TextBox_Barcode);
                appSettings.LastPrintCopies = numericUpDown_PrintCopies != null ? (int)numericUpDown_PrintCopies.Value : 1;

                // 용지 및 바코드 기본 설정 저장 (PRINT_INFO_CLASS -> appSettings)
                appSettings.PaperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                appSettings.PaperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                appSettings.BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType;

                System.Diagnostics.Debug.WriteLine($"  - 품명: '{appSettings.LastProductName}'");
                System.Diagnostics.Debug.WriteLine($"  - 가격: '{appSettings.LastPrice}'");
                System.Diagnostics.Debug.WriteLine($"  - 바코드: '{appSettings.LastBarcode}'");
                System.Diagnostics.Debug.WriteLine($"  - 인쇄 매수: {appSettings.LastPrintCopies}");

                appSettings.Save();

                // ★ 주석 처리: PRINT_INFO_CLASS는 AdjustForm 닫힐 때와 MainForm_FormClosing에서만 저장
                // SaveAppSettings()는 appSettings만 저장하고, PRINT_INFO_CLASS는 건드리지 않음
                // string printerSettingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                // PRINT_INFO_CLASS.SaveToFile(printerSettingsPath);
                // System.Diagnostics.Debug.WriteLine($"프린터 설정 저장 완료: BarcodeType={PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");

                System.Diagnostics.Debug.WriteLine($"[SaveAppSettings] 완료");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"통합 설정 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// TextBox에서 주소 데이터 가져오기
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 전화번호, 이름
        /// UI 매핑: 주소1(TextBox4), 우편번호(TextBox2), 주소2(TextBox), 전화번호(TextBox5), 이름(TextBox3)
        /// </summary>
        private AddressData GetAddressDataFromTextBoxes()
        {
            return new AddressData
            {
                // 가격표용에는 주소 기능 사용 안 함
                Address = "",
                Address2 = "",
                Name = GetTextBoxValue(TextBox_ProductName),         // 품명
                PhoneNumber = "",
                PostalCode = "",
                CopyCount = 1
            };
        }

        /// <summary>
        /// AddressData를 TextBox에 설정
        /// UI 순서 (위→아래): 주소1, 우편번호, 주소2, 전화번호, 이름
        /// UI 매핑: 주소1(TextBox4), 우편번호(TextBox2), 주소2(TextBox), 전화번호(TextBox5), 이름(TextBox3)
        /// </summary>
        private void SetAddressDataToTextBoxes(AddressData addressData)
        {
            if (addressData == null) return;

            try
            {
                // 가격표용에는 주소 데이터 사용 안 함
                TextBox_ProductName.Text = addressData.Name ?? "";           // 품명
                TextBox_Price.Text = "";                               // 가격
                TextBox_Barcode.Text = "";                               // 바코드
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

                        if (extension == ".xlsx" || extension == ".xls" || extension == ".csv")
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
                        if (extension == ".xlsx" || extension == ".xls" || extension == ".csv")
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
                            LoadExcelData(excelFilePath, true); // 드래그앤드롭 시 다이얼로그 강제 표시

                            // 엑셀 모드로 전환
                            if (excelData != null && excelData.Rows.Count > 0)
                            {
                                SwitchToExcelMode();
                            }
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("엑셀 파일(.xlsx, .xls, .csv)만 지원합니다.", "알림",
                                System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[드래그 앤 드롭 Drop 오류] {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                System.Windows.Forms.MessageBox.Show($"엑셀 파일 열기 오류: {ex.Message}", "오류",
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// MainForm 종료 시 설정 저장
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[프로그램 종료] 설정 저장 시작");

                // 정리 작업 수행
                CleanupBeforeExit();

                // 통합 설정 저장 (입력값 포함)
                SaveAppSettings();
                SaveColumnPositionSettings();

                // PRINT_INFO_CLASS 설정 저장 (바코드 타입, 용지 사이즈 등)
                string printerSettingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                PRINT_INFO_CLASS.SaveToFile(printerSettingsPath);
                System.Diagnostics.Debug.WriteLine($"[프로그램 종료] PRINT_INFO_CLASS 저장 완료: {printerSettingsPath}");
                System.Diagnostics.Debug.WriteLine($"  - BarcodeType: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                System.Diagnostics.Debug.WriteLine($"  - Paper_Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width} mm");
                System.Diagnostics.Debug.WriteLine($"  - Paper_Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height} mm");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프로그램 종료 시 설정 저장 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 컬럼 위치 설정 저장 (현재 용지 타입에 맞게 저장)
        /// </summary>
        public void SaveColumnPositionSettings()
        {
            try
            {
                if (columnPositionSettings != null && columnPositionSettings.Count > 0)
                {
                    // ★ 가격표용: 3개 또는 4개 컬럼 사용 (품명, 가격, 바코드, [비고])
                    // 무결성 검사: 3개 미만 또는 4개 초과인 경우 저장하지 않음
                    if (columnPositionSettings.Count < 3 || columnPositionSettings.Count > 4)
                    {
                        System.Diagnostics.Debug.WriteLine($"[경고] 컬럼 설정이 {columnPositionSettings.Count}개입니다. 가격표용은 3개 또는 4개여야 합니다. 저장하지 않습니다.");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"========================================");
                    System.Diagnostics.Debug.WriteLine($"[컬럼 위치 저장] 시작 - {columnPositionSettings.Count}개 컬럼");
                    for (int i = 0; i < columnPositionSettings.Count; i++)
                    {
                        var s = columnPositionSettings[i];
                        System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: Index={s.ColumnIndex}, Size={s.Size}, X={s.OffsetX}, Y={s.OffsetY}, Type={s.UsageType}, BarcodeType={s.BarcodeType}");
                    }
                    System.Diagnostics.Debug.WriteLine($"========================================");

                    var lines = new List<string>();
                    foreach (var setting in columnPositionSettings)
                    {
                        string line = $"{setting.ColumnIndex}|{setting.IsEnabled}|{setting.Size}|{setting.OffsetX}|{setting.OffsetY}|{setting.UsageType}|{setting.BarcodeType}|{setting.ShowBarcodeText}|{setting.BarcodeHeight}";
                        lines.Add(line);
                        System.Diagnostics.Debug.WriteLine($"  저장할 데이터: {line}");
                    }

                    // 현재 용지 타입 가져오기 (SelectedPaperType 대신 실제 사이즈 사용)
                    int width = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                    int height = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                    string sensorType = "";
                    if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
                        sensorType = "블랙마크";
                    else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
                        sensorType = "갭";
                    else
                        sensorType = "연속";
                    string currentPaperType = $"{width}x{height}({sensorType})";
                    string columnData = string.Join(";", lines);

                    // 용지 타입별 컬럼 위치 데이터 저장
                    SavePaperTypeColumnPositionData(currentPaperType, columnData);

                    // 기존 호환성 유지 (기본 컬럼 위치 데이터도 저장)
                    appSettings.ColumnPositionData = columnData;

                    // 용지 및 바코드 기본 설정 저장 (SelectedPaperType 대신 별도 저장)
                    appSettings.PaperWidth = width;
                    appSettings.PaperHeight = height;
                    appSettings.BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType;

                    // 파일에 저장 (PaperTypeColumnPositionData 포함)
                    appSettings.Save();

                    System.Diagnostics.Debug.WriteLine($"[저장 완료] 용지 타입 '{currentPaperType}' 컬럼 설정 {columnPositionSettings.Count}개 저장됨");
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
                                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 원본 주변: ...{columnData.Substring(Math.Max(0, i - 10), Math.Min(20, columnData.Length - Math.Max(0, i - 10)))}...");
                                    System.Diagnostics.Debug.WriteLine($"[저장 검증] 불러온 주변: ...{testLoadedData.Substring(Math.Max(0, i - 10), Math.Min(20, testLoadedData.Length - Math.Max(0, i - 10)))}...");
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
        /// 용지 타입별 컬럼 위치 데이터 저장
        /// </summary>
        private void SavePaperTypeColumnPositionData(string paperType, string columnData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 시작] PaperType: '{paperType}', DataLength: {columnData?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 저장할 데이터: {columnData.Substring(0, Math.Min(100, columnData.Length))}...");

                // 기존 데이터 파싱
                // 문제: columnData에 세미콜론(;)이 포함되어 있어서 Split(';')로 나누면 잘못 파싱됨
                // 해결: 정규표현식을 사용하여 "용지타입:데이터" 형식을 정확히 파싱
                // 패턴: 용지타입(콜론 없음):데이터(세미콜론 포함 가능) - 다음 용지타입 또는 끝까지
                Dictionary<string, string> paperTypeData = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(appSettings.PaperTypeColumnPositionData))
                {
                    System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 기존 데이터: {appSettings.PaperTypeColumnPositionData.Substring(0, Math.Min(100, appSettings.PaperTypeColumnPositionData.Length))}...");

                    // 정규표현식: (용지타입):(데이터) - 다음 "용지타입:" 또는 끝까지
                    // 예: "100x145(블랙마크):0|True|12;1|True|12;87x58(블랙마크):0|True|12"
                    // 매칭: "100x145(블랙마크)" : "0|True|12;1|True|12"
                    // 용지타입 패턴: \d+x\d+\([^)]+\) (예: 100x145(블랙마크))
                    // lookahead: 다음 용지타입 패턴(\d+x\d+\([^)]+\):)이 나오기 전까지 또는 문자열 끝까지
                    var pattern = @"(\d+x\d+\([^)]+\)):(.+?)(?=\d+x\d+\([^)]+\):|$)";
                    var matches = System.Text.RegularExpressions.Regex.Matches(appSettings.PaperTypeColumnPositionData, pattern);

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count == 3)
                        {
                            string storedPaperType = match.Groups[1].Value.Trim();
                            string storedData = match.Groups[2].Value;

                            if (!string.IsNullOrWhiteSpace(storedPaperType) && !string.IsNullOrEmpty(storedData))
                            {
                                paperTypeData[storedPaperType] = storedData;
                                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 기존 항목 로드: '{storedPaperType}' -> 데이터 길이={storedData.Length}");
                                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 기존 항목 전체 데이터: {storedData}");
                            }
                        }
                    }
                }

                // 현재 용지 타입 데이터 업데이트
                paperTypeData[paperType] = columnData;
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 업데이트: '{paperType}' -> 데이터 길이={columnData.Length}");

                // 다시 문자열로 변환 (형식: PaperType1:ColumnData1;PaperType2:ColumnData2;...)
                // 주의: ColumnData에 세미콜론이 포함될 수 있으므로 정확히 저장해야 함
                string savedData = string.Join(";", paperTypeData.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                appSettings.PaperTypeColumnPositionData = savedData;
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 완료] 총 {paperTypeData.Count}개 용지 타입 저장됨");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 최종 데이터 길이: {savedData.Length}");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장] 최종 데이터 일부: {savedData.Substring(0, Math.Min(200, savedData.Length))}...");

                // 저장 직후 바로 확인 (메모리에서)
                string verificationData = LoadPaperTypeColumnPositionData(paperType);
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] 저장한 데이터 길이: {columnData.Length}");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] 불러온 데이터 길이: {verificationData?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] 저장한 데이터: {columnData}");
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] 불러온 데이터: {verificationData ?? "null"}");

                if (verificationData == columnData)
                {
                    System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] ✓ 메모리 검증 성공 - '{paperType}' 데이터 일치");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 검증] ✗ 메모리 검증 실패 - '{paperType}' 데이터 불일치");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 저장 오류] {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 용지 타입별 컬럼 위치 데이터 로드 (문자열에서 직접 파싱)
        /// </summary>
        private string LoadPaperTypeColumnPositionDataFromString(string dataString, string paperType)
        {
            try
            {
                if (string.IsNullOrEmpty(dataString))
                    return null;

                foreach (var item in dataString.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    var parts = item.Split(new[] { ':' }, 2);
                    if (parts.Length == 2 && parts[0] == paperType)
                    {
                        return parts[1];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드 오류] {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 용지 타입별 컬럼 위치 데이터 로드
        /// </summary>
        private string LoadPaperTypeColumnPositionData(string paperType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드 시작] PaperType: '{paperType}'");

                if (string.IsNullOrEmpty(appSettings.PaperTypeColumnPositionData))
                {
                    System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드] 저장된 데이터 없음");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드] 저장된 데이터: {appSettings.PaperTypeColumnPositionData.Substring(0, Math.Min(100, appSettings.PaperTypeColumnPositionData.Length))}...");

                // 문제: columnData에 세미콜론(;)이 포함되어 있어서 Split(';')로 나누면 잘못 파싱됨
                // 해결: 정규표현식을 사용하여 "용지타입:데이터" 형식을 정확히 파싱
                // 용지타입 패턴: \d+x\d+\([^)]+\) (예: 100x145(블랙마크))
                // lookahead: 다음 용지타입 패턴(\d+x\d+\([^)]+\):)이 나오기 전까지 또는 문자열 끝까지
                var pattern = @"(\d+x\d+\([^)]+\)):(.+?)(?=\d+x\d+\([^)]+\):|$)";
                var matches = System.Text.RegularExpressions.Regex.Matches(appSettings.PaperTypeColumnPositionData, pattern);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string storedPaperType = match.Groups[1].Value.Trim();
                        string storedData = match.Groups[2].Value;

                        System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드] 비교: 저장된 타입 '{storedPaperType}' == 요청 타입 '{paperType}'? {storedPaperType == paperType}");
                        System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드] 저장된 데이터 길이: {storedData.Length}");

                        if (storedPaperType == paperType)
                        {
                            System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드 성공] '{paperType}': 전체 데이터 길이={storedData.Length}");
                            System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드] 전체 데이터: {storedData}");
                            return storedData;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드 실패] '{paperType}'에 해당하는 데이터 없음");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 로드 오류] {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 용지 타입별 Title 데이터 저장
        /// </summary>
        public void SavePaperTypeTitleData(string paperType, int titleSize, int titleOffsetY)
        {
            try
            {
                // 기존 데이터 파싱
                Dictionary<string, string> paperTypeData = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(appSettings.PaperTypeTitleData))
                {
                    foreach (var item in appSettings.PaperTypeTitleData.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(item)) continue;
                        var parts = item.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            paperTypeData[parts[0]] = parts[1];
                        }
                    }
                }

                // 현재 용지 타입 데이터 업데이트
                paperTypeData[paperType] = $"{titleSize},{titleOffsetY}";

                // 다시 문자열로 변환
                appSettings.PaperTypeTitleData = string.Join(";", paperTypeData.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 Title 저장] '{paperType}': Size={titleSize}, OffsetY={titleOffsetY}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 Title 저장 오류] {ex.Message}");
            }
        }

        /// <summary>
        /// 용지 타입별 Title 데이터 로드
        /// </summary>
        public (int size, int offsetY) LoadPaperTypeTitleData(string paperType)
        {
            try
            {
                if (string.IsNullOrEmpty(appSettings.PaperTypeTitleData))
                    return (appSettings.TitleSize, appSettings.TitleOffsetY);

                foreach (var item in appSettings.PaperTypeTitleData.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    var parts = item.Split(new[] { ':' }, 2);
                    if (parts.Length == 2 && parts[0] == paperType)
                    {
                        var values = parts[1].Split(',');
                        if (values.Length == 2)
                        {
                            if (int.TryParse(values[0], out int size) && int.TryParse(values[1], out int offsetY))
                            {
                                System.Diagnostics.Debug.WriteLine($"[용지 타입별 Title 로드] '{paperType}': Size={size}, OffsetY={offsetY}");
                                return (size, offsetY);
                            }
                        }
                    }
                }

                return (appSettings.TitleSize, appSettings.TitleOffsetY);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[용지 타입별 Title 로드 오류] {ex.Message}");
                return (appSettings.TitleSize, appSettings.TitleOffsetY);
            }
        }

        /// <summary>
        /// 컬럼 위치 설정 불러오기 (현재 용지 타입에 맞는 데이터 로드)
        /// </summary>
        private void LoadColumnPositionSettings()
        {
            try
            {
                // 파일에서 최신 설정 다시 로드 (다른 곳에서 저장했을 수 있음)
                appSettings.Load();
                System.Diagnostics.Debug.WriteLine($"[LoadColumnPositionSettings] AppSettings 다시 로드 완료");

                // 현재 용지 타입 가져오기 (SelectedPaperType 대신 실제 사이즈 사용)
                int width = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                int height = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                string sensorType = "";
                if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
                    sensorType = "블랙마크";
                else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
                    sensorType = "갭";
                else
                    sensorType = "연속";
                string currentPaperType = $"{width}x{height}({sensorType})";
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
                                UsageType = int.Parse(parts[0]) == 3 ? "바코드" : (int.Parse(parts[0]) == 2 ? "텍스트" : "텍스트"), // 인덱스 3은 바코드, 나머지는 텍스트
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

                            // BarcodeHeight 추가 (9개 이상일 때)
                            if (parts.Length >= 9)
                            {
                                setting.BarcodeHeight = int.TryParse(parts[8], out int h) ? h : 50;
                            }

                            columnPositionSettings.Add(setting);
                        }
                    }

                    // ★ 가격표용: 3개(구버전)일 경우 4번째 추가, 그 외 이상한 개수면 초기화
                    if (columnPositionSettings.Count < 4)
                    {
                        if (columnPositionSettings.Count == 3)
                        {
                            // 구버전(3개) -> 신버전(4개) 업그레이드: 비고 컬럼 추가
                            System.Diagnostics.Debug.WriteLine($"[업그레이드] 구버전 설정 감지 (3개). 4번째 컬럼(비고)을 자동 추가합니다.");
                            columnPositionSettings.Add(new ColumnPositionSetting
                            {
                                ColumnIndex = 3,
                                IsEnabled = true,
                                Size = 8, // 기본 텍스트 크기
                                OffsetX = 0,
                                OffsetY = 18, // 바코드 위쪽 적절한 위치
                                UsageType = "텍스트",
                                BarcodeType = BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE",  // 설정된 바코드 타입 사용
                                ShowBarcodeText = false
                            });
                            // 변경사항 즉시 저장
                            SaveColumnPositionSettings();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[경고] 컬럼 설정이 {columnPositionSettings.Count}개입니다. 가격표용은 4개여야 합니다. 초기화합니다.");
                            needsReset = true;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"========================================");
                        System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] 용지 타입 '{currentPaperType}'에서 {columnPositionSettings.Count}개 컬럼 불러옴");
                        for (int i = 0; i < columnPositionSettings.Count; i++)
                        {
                            var s = columnPositionSettings[i];
                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: Index={s.ColumnIndex}, Size={s.Size}, X={s.OffsetX}, Y={s.OffsetY}, Type={s.UsageType}, BarcodeType={s.BarcodeType}");
                        }
                        System.Diagnostics.Debug.WriteLine($"========================================");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드] 용지 타입 '{currentPaperType}'에 데이터 없음 - 기본값으로 초기화");
                    needsReset = true;
                }

                // Title 설정도 용지 타입별로 로드
                var (titleSize, titleOffsetY, titleOffsetX) = LoadPaperTypeTitleData(currentPaperType);
                appSettings.TitleSize = titleSize;
                appSettings.TitleOffsetY = titleOffsetY;
                appSettings.TitleOffsetX = titleOffsetX;

                if (needsReset)
                {
                    // 파일이 없을 때 기본값으로 초기화
                    columnPositionSettings.Clear();

                    // PRINT_INFO_CLASS에서 로드된 바코드 타입 사용 (없으면 기본값)
                    string loadedBarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
                    System.Diagnostics.Debug.WriteLine($"[설정 초기화] 사용할 바코드 타입: {loadedBarcodeType} (PRINT_INFO_CLASS에서 로드됨)");

                    // 가격표용 기본 컬럼 설정 (70x28mm 기준)
                    // 레이아웃 (위→아래): Title, 품명, 가격, 비고, 바코드
                    // 글자크기: 품명/가격/비고 8, 바코드 15

                    // 컬럼 0: 품명 (절대좌표: 용지 최상단 기준 OffsetY mm)
                    columnPositionSettings.Add(new ColumnPositionSetting
                    {
                        ColumnIndex = 0,
                        IsEnabled = true,
                        Size = 8, // 작은 용지에 맞게 크기 축소
                        OffsetX = 0,  // 센터 정렬이므로 X는 0
                        OffsetY = 5, // 용지 최상단 기준 5mm (절대좌표)
                        UsageType = "텍스트",
                        BarcodeType = loadedBarcodeType,  // 로드된 바코드 타입 사용
                        ShowBarcodeText = false
                    });

                    // 컬럼 1: 가격 (절대좌표: 용지 최상단 기준 OffsetY mm)
                    columnPositionSettings.Add(new ColumnPositionSetting
                    {
                        ColumnIndex = 1,
                        IsEnabled = true,
                        Size = 8, // 작은 용지에 맞게 크기 축소
                        OffsetX = 0,  // 센터 정렬이므로 X는 0
                        OffsetY = 10, // 용지 최상단 기준 10mm (절대좌표)
                        UsageType = "텍스트",
                        BarcodeType = loadedBarcodeType,  // 로드된 바코드 타입 사용
                        ShowBarcodeText = false
                    });

                    // 컬럼 2: 비고 (절대좌표: 용지 최상단 기준 OffsetY mm)
                    columnPositionSettings.Add(new ColumnPositionSetting
                    {
                        ColumnIndex = 2,
                        IsEnabled = true,
                        Size = 8,
                        OffsetX = 0,
                        OffsetY = 18,
                        UsageType = "텍스트",
                        BarcodeType = BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE",
                        ShowBarcodeText = false
                    });

                    // 컬럼 3: 바코드 (절대좌표: 용지 최상단 기준 OffsetY mm)
                    columnPositionSettings.Add(new ColumnPositionSetting
                    {
                        ColumnIndex = 3,
                        IsEnabled = true,
                        Size = 30,
                        OffsetX = 0,
                        OffsetY = 35,
                        UsageType = "바코드",
                        BarcodeType = BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE",
                        ShowBarcodeText = false
                    });

                    // *참고: 기본값 3개일때 바코드 Y가 15였는데 비고가 추가되면 겹칠 수 있음.
                    // 위 기본값 설정시 바코드 Y를 25정도로 내리는게 좋겠지만,
                    // 기존 사용자의 혼동을 줄이기 위해 일단 안전한 값으로 설정.
                    // (사용자가 직접 조절하도록 유도)

                    System.Diagnostics.Debug.WriteLine($"[설정 초기화 완료] columnPositionSettings={columnPositionSettings.Count}개, 바코드 타입={loadedBarcodeType}");

                    // 초기화 후 즉시 저장
                    SaveColumnPositionSettings();
                }

                // 바코드 타입 동기화: PRINT_INFO_CLASS 우선 (파일에서 로드된 값)
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[바코드 타입 동기화 및 강제 교정] 시작");

                // 가격표 전용 강제 교정 (Index 2: 비고/텍스트, Index 3: 바코드)

                if (columnPositionSettings.Count > 3)
                {
                    var optCol = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == 2);
                    if (optCol != null && optCol.UsageType != "텍스트")
                    {
                        optCol.UsageType = "텍스트";
                        System.Diagnostics.Debug.WriteLine("  - Index 2(비고) UsageType 강제 교정: 텍스트");
                    }


                    var barCol = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == 3);
                    if (barCol != null && barCol.UsageType != "바코드")
                    {
                        barCol.UsageType = "바코드";
                        System.Diagnostics.Debug.WriteLine("  - Index 3(바코드) UsageType 강제 교정: 바코드");
                    }
                }

                string savedBarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";
                var barcodeColumn = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == 3);
                if (barcodeColumn != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - 저장된 바코드 타입: {savedBarcodeType}");
                    barcodeColumn.BarcodeType = savedBarcodeType;

                    // 바코드 타입 동기화
                    if (!string.IsNullOrEmpty(PRINT_INFO_CLASS.PrinterInfo.BarcodeType))
                    {
                        barcodeColumn.BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType;
                        System.Diagnostics.Debug.WriteLine($"[바코드 타입 동기화] ✓ 전역 설정 → 컬럼 설정: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                    }
                    else
                    {
                        PRINT_INFO_CLASS.PrinterInfo.BarcodeType = barcodeColumn.BarcodeType;
                        System.Diagnostics.Debug.WriteLine($"[바코드 타입 동기화] ✓ 컬럼 설정 → 전역 설정: {barcodeColumn.BarcodeType}");
                    }
                }
                else
                {
                    // 바코드 컬럼이 없는 경우 기본값
                    if (string.IsNullOrEmpty(PRINT_INFO_CLASS.PrinterInfo.BarcodeType))
                    {
                        PRINT_INFO_CLASS.PrinterInfo.BarcodeType = "QR_CODE";
                        System.Diagnostics.Debug.WriteLine($"[바코드 타입 동기화] ⚠ 바코드 컬럼 없음, 기본값 사용: QR_CODE");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"  - 최종 BarcodeType: {PRINT_INFO_CLASS.PrinterInfo.BarcodeType}");
                System.Diagnostics.Debug.WriteLine($"========================================");

                // 로드된 설정 정보 출력
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[컬럼 설정 로드 완료] 총 {columnPositionSettings.Count}개 컬럼");
                for (int i = 0; i < columnPositionSettings.Count; i++)
                {
                    var s = columnPositionSettings[i];
                    System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: Index={s.ColumnIndex}, Enabled={s.IsEnabled}, Size={s.Size}, X={s.OffsetX}, Y={s.OffsetY}, Type={s.UsageType}");
                }
                System.Diagnostics.Debug.WriteLine($"========================================");

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
                // 현재 용지 타입 가져오기 (SelectedPaperType 대신 실제 사이즈 사용)
                int width = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                int height = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                string sensorType = "";
                if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
                    sensorType = "블랙마크";
                else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
                    sensorType = "갭";
                else
                    sensorType = "연속";
                string currentPaperType = $"{width}x{height}({sensorType})";
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



        /// <summary>
        /// 위치 조절 버튼 그리기
        /// </summary>
        private void new_PositionAdjustButton(float shadowRate = 1f)
        {
            string ButtonText = "위치 조절";
            System.Drawing.Font TextFont = new System.Drawing.Font("페이퍼로지 5 Medium", 12, System.Drawing.FontStyle.Bold);

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
            Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

            using (Graphics g = panel_PositionAdjust.CreateGraphics())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; // 부드럽게 그리기
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 10;
                    int w = panel_PositionAdjust.Width - 1, h = panel_PositionAdjust.Height - 1;
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
                    // 중앙에 텍스트 그리기
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(ButtonText, TextFont);
                        PointF textPosition = new PointF(
                            (panel_PositionAdjust.Width - textSize.Width) / 2,
                            (panel_PositionAdjust.Height - textSize.Height) / 2
                        );
                        g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                    }
                }
            }
        }

        private void new_HomeButton(float shadowRate = 1f)
        {
            string ButtonText = "HOME";
            System.Drawing.Font TextFont = new System.Drawing.Font("페이퍼로지 5 Medium", 13, System.Drawing.FontStyle.Bold);

            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.DodgerBlue;
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

        /// <summary>
        /// 위치 조절 버튼 이벤트 핸들러
        /// </summary>
        private void panel_PositionAdjust_Paint(object sender, PaintEventArgs e)
        {
            new_PositionAdjustButton();
        }

        private void panel_PositionAdjust_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] PositionAdjustmentForm 열기 시작");

                // 이미 열려있는 위치조절 폼이 있는지 확인
                foreach (Form form in System.Windows.Forms.Application.OpenForms)
                {
                    if (form.Name == "PositionAdjustmentForm")
                    {
                        form.Activate();
                        return;
                    }
                }

                // 위치조절 폼 생성
                PositionAdjustmentForm positionForm = new PositionAdjustmentForm(this);
                positionForm.FormClosed += PositionAdjustmentForm_FormClosed;

                // MainForm의 미리보기 패널 위치 기준으로 위치 결정
                if (panel_BarcodePreview != null)
                {
                    // 미리보기 패널 오른쪽에 위치조절 폼 배치
                    Point previewLocation = panel_BarcodePreview.PointToScreen(Point.Empty);
                    positionForm.StartPosition = FormStartPosition.Manual;
                    positionForm.Location = new Point(
                        previewLocation.X + panel_BarcodePreview.Width + 20,
                        previewLocation.Y
                    );
                }
                else
                {
                    positionForm.StartPosition = FormStartPosition.CenterScreen;
                }

                positionForm.Show();
                System.Diagnostics.Debug.WriteLine("[MainForm] PositionAdjustmentForm 열기 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] PositionAdjustmentForm 열기 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }

        private void PositionAdjustmentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // 위치조절 폼이 닫힐 때 포커스 자동 이동 처리
            OnControlStateChanged(sender, e);
        }

        private void panel_PositionAdjust_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                new_PositionAdjustButton(0.8f); // 눌린 상태 효과
            }
        }

        private void panel_PositionAdjust_MouseUp(object sender, MouseEventArgs e)
        {
            new_PositionAdjustButton(1f);
        }

        private void panel_PositionAdjust_MouseEnter(object sender, EventArgs e)
        {
            new_PositionAdjustButton(1.1f); // 호버 효과
        }

        private void panel_PositionAdjust_MouseLeave(object sender, EventArgs e)
        {
            new_PositionAdjustButton(1f);
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

        private void ReceivePaint(object sender, EventArgs e)
        {
            Bitmap bitmap = ((PreviewManager.PaintEventArgs)e).bit;
            try
            {
                panel_BarcodePreview.Invoke(new Action(() =>
                {
                    // 새 이미지를 받으면 원본으로 저장하고 표시
                    UpdatePreviewImage(bitmap);
                }));
            }
            catch { }
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

            // 가격표용: 바코드 TextBox 사용
            var inputText = TextBox_Barcode.Text;

            // placeholder 텍스트는 빈 문자열 반환
            if (string.IsNullOrWhiteSpace(inputText) ||
                inputText == "값을 입력하세요." ||
                inputText.Contains("값을 입력하세요"))
            {
                System.Diagnostics.Debug.WriteLine($"GetPrintData - placeholder 텍스트 무시: '{inputText}'");
                return "";
            }

            System.Diagnostics.Debug.WriteLine($"GetPrintData - 바코드 TextBox 입력 사용: '{inputText}'");
            return inputText;
        }

        public int GetBarcodeSize()
        {
            if (columnPositionSettings != null && columnPositionSettings.Count > 2)
            {
                return columnPositionSettings[2].Size;
            }
            return barcodeSize;
        }

        public int GetBarcodeHeight()
        {
            // 가격표 모드(다중 입력/엑셀)인 경우 컬럼 2의 설정을 우선 사용
            if (columnPositionSettings != null && columnPositionSettings.Count > 2)
            {
                return columnPositionSettings[2].BarcodeHeight;
            }
            return barcodeHeight;
        }

        public int GetBarcodeX()
        {
            if (columnPositionSettings != null && columnPositionSettings.Count > 2)
            {
                return columnPositionSettings[2].OffsetX;
            }
            return barcodeX;
        }

        public int GetBarcodeY()
        {
            if (columnPositionSettings != null && columnPositionSettings.Count > 2)
            {
                return columnPositionSettings[2].OffsetY;
            }
            return barcodeY;
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

            // 가격표용 입력 데이터 가져오기 (품명, 가격, 바코드, 비고 사용)
            string productName = GetTextBoxValue(TextBox_ProductName);   // 품명
            string price = GetTextBoxValue(TextBox_Price);         // 가격
            string barcodeValue = GetTextBoxValue(TextBox_Barcode); // 바코드
            string optionValue = GetTextBoxValue(TextBox_Option);   // 비고

            System.Diagnostics.Debug.WriteLine($"[GetMultiInputData] TextBox에서 가져온 가격 원본: '{price}'");

            // 가격 데이터에 자동 포맷 적용 (미리보기/인쇄 시)
            if (!string.IsNullOrWhiteSpace(price))
            {
                string originalPrice = price;
                price = BarcodePrint.Model.PriceFormatHelper.FormatPrice(price);
                System.Diagnostics.Debug.WriteLine($"[GetMultiInputData] 가격 포맷 적용: '{originalPrice}' → '{price}'");
            }

            // columnPositionSettings 인덱스와 매칭 (가격표용):
            // 인덱스 0: 품명 (TextBox_ProductName)
            // 인덱스 1: 가격 (TextBox_Price)
            // 인덱스 2: 비고 (TextBox_Option)
            // 인덱스 3: 바코드 (TextBox_Barcode)
            var textBoxInputs = new List<string>
            {
                productName,   // 인덱스 0: 품명
                price,         // 인덱스 1: 가격 (포맷 적용됨)
                optionValue,   // 인덱스 2: 비고
                barcodeValue   // 인덱스 3: 바코드
            };

            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[다중 입력 미리보기 데이터] 생성 시작 (가격표용)");
            System.Diagnostics.Debug.WriteLine($"  품명 (인덱스 0): '{productName}'");
            System.Diagnostics.Debug.WriteLine($"  가격 (인덱스 1): '{price}'");
            System.Diagnostics.Debug.WriteLine($"  비고 (인덱스 2): '{optionValue}'");
            System.Diagnostics.Debug.WriteLine($"  바코드 (인덱스 3): '{barcodeValue}'");
            System.Diagnostics.Debug.WriteLine($"  컬럼 설정 개수: {columnPositionSettings.Count}");

            // 가격표용 컬럼 인덱스: 0(품명), 1(가격), 2(비고), 3(바코드)
            int[] usedIndices = { 0, 1, 2, 3 };

            foreach (int i in usedIndices)
            {
                if (i >= textBoxInputs.Count || i >= columnPositionSettings.Count)
                    continue;

                var setting = columnPositionSettings[i];
                var input = textBoxInputs[i];

                System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 체크] 인덱스 {i}: ColumnIndex={setting.ColumnIndex}, 입력값='{input}', IsEnabled={setting.IsEnabled}, UsageType={setting.UsageType}");

                // 가격표용: 품명(0), 가격(1), 비고(2), 바코드(3) 모두 값이 있으면 표시 (IsEnabled 무시)
                bool shouldInclude = !string.IsNullOrEmpty(input);

                if (shouldInclude)
                {
                    // 가격표용 - 컬럼명 결정
                    string columnName = setting.ColumnIndex == 0 ? "품명" :
                                      setting.ColumnIndex == 1 ? "가격" :
                                      setting.ColumnIndex == 2 ? "비고" : "바코드";

                    // 바코드 여부 확인 (UsageType 따름)
                    bool isBarcode = setting.UsageType == "바코드";

                    // 바코드는 텍스트를 출력하지 않도록 설정
                    bool showBarcodeText = isBarcode ? false : true;

                    // 바코드 타입 결정 (디버깅 강화)
                    string barcodeType = isBarcode ? (setting.BarcodeType ?? PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE") : setting.BarcodeType;

                    result.Add(new MultiColumnData
                    {
                        Value = input,
                        IsBarcode = isBarcode,
                        BarcodeType = barcodeType,
                        ColumnName = columnName, // 명확한 컬럼명 사용
                        Size = setting.Size,
                        BarcodeHeight = setting.BarcodeHeight,
                        OffsetX = setting.OffsetX,
                        OffsetY = setting.OffsetY, // 절대좌표 (용지 최상단 기준)
                        ShowBarcodeText = showBarcodeText
                    });

                    System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 추가 ✓] 컬럼 {setting.ColumnIndex}({columnName}): 값='{input}', 타입={setting.UsageType}, IsBarcode={isBarcode}, BarcodeType={barcodeType}, 크기={setting.Size}, X={setting.OffsetX}, Y={setting.OffsetY}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[미리보기 데이터 제외 ✗] 인덱스 {i}: 값이 비어있음");
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
                            // 변환된 컬럼 이름 사용
                            string columnName = setting.UsageType == "바코드" ?
                                $"Barcode_{setting.ColumnIndex}_{setting.BarcodeType}" :
                                $"Text_{setting.ColumnIndex}";

                            System.Diagnostics.Debug.WriteLine($"  컬럼 {i} ({setting.ColumnName}): 변환된 이름='{columnName}'");

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

            // 첫 번째 행 데이터 사용
            DataRow firstRow = excelData.Rows[0];

            // 가격표용 컬럼 매핑 (품명, 가격, 바코드, 비고)
            // multiColumnSettings의 ColumnIndex는 0, 1, 2, 3
            // columnPositionSettings도 동일한 인덱스 사용

            string[] columnNames = { "품명", "가격", "바코드", "비고" };

            System.Diagnostics.Debug.WriteLine($"엑셀 데이터 컬럼 목록:");
            for (int i = 0; i < excelData.Columns.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  컬럼 {i}: '{excelData.Columns[i].ColumnName}'");
            }

            System.Diagnostics.Debug.WriteLine($"활성화된 excelSetting 수: {multiColumnSettings.Count(s => s.IsEnabled)}");

            // multiColumnSettings를 순회하면서 활성화된 컬럼만 처리
            foreach (var excelSetting in multiColumnSettings.Where(s => s.IsEnabled).OrderBy(s => s.ColumnIndex))
            {
                // 가격표용 컬럼 인덱스 (0, 1, 2)
                int actualColumnIndex = excelSetting.ColumnIndex;

                System.Diagnostics.Debug.WriteLine($"처리 중: excelSetting.ColumnIndex={actualColumnIndex}, ColumnName='{excelSetting.ColumnName}', ExcelColumnNumber={excelSetting.ExcelColumnNumber}");

                // columnPositionSettings에서 위치 정보 가져오기
                var positionSetting = columnPositionSettings.FirstOrDefault(p => p.ColumnIndex == actualColumnIndex);
                if (positionSetting == null)
                {
                    System.Diagnostics.Debug.WriteLine($"    → 컬럼 {actualColumnIndex}에 대한 positionSetting 없음");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"    → positionSetting 찾음: Size={positionSetting.Size}, OffsetY={positionSetting.OffsetY}, UsageType={positionSetting.UsageType}");

                // 바코드 여부 확인 (UsageType 따름)
                bool isBarcode = positionSetting.UsageType == "바코드";

                // 엑셀 데이터에서 값 가져오기
                // 바코드 컬럼인 경우 Barcode_ 형식으로 먼저 찾아보고, 없으면 Text_ 형식으로 찾기
                // excelSetting.ColumnIndex는 실제 컬럼 인덱스 (2, 3, 4)
                string excelDataColumnName = null;
                string value = "";

                System.Diagnostics.Debug.WriteLine($"    → isBarcode={isBarcode}");

                if (isBarcode)
                {
                    // 바코드 컬럼: 먼저 Barcode_ 형식으로 찾기
                    string barcodeColumnName = $"Barcode_{excelSetting.ColumnIndex}_{positionSetting.BarcodeType ?? "QR_CODE"}";
                    System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 찾기 시도: '{barcodeColumnName}'");

                    if (excelData.Columns.Contains(barcodeColumnName))
                    {
                        excelDataColumnName = barcodeColumnName;
                        value = firstRow[excelDataColumnName]?.ToString() ?? "";
                        System.Diagnostics.Debug.WriteLine($"    ✓ 바코드 컬럼 '{barcodeColumnName}' 찾음, 값='{value}'");
                    }
                    else
                    {
                        // Barcode_ 형식이 없으면 Text_ 형식으로 찾기
                        string textColumnName = $"Text_{excelSetting.ColumnIndex}";
                        System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 없음, Text_ 형식 시도: '{textColumnName}'");

                        if (excelData.Columns.Contains(textColumnName))
                        {
                            excelDataColumnName = textColumnName;
                            value = firstRow[excelDataColumnName]?.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"    ✓ Text_ 형식 '{textColumnName}' 사용, 값='{value}'");
                        }
                    }
                }
                else
                {
                    // 텍스트 컬럼: Text_ 형식으로 찾기
                    excelDataColumnName = $"Text_{excelSetting.ColumnIndex}";
                    System.Diagnostics.Debug.WriteLine($"    → 텍스트 컬럼 찾기 시도: '{excelDataColumnName}'");

                    if (excelData.Columns.Contains(excelDataColumnName))
                    {
                        value = firstRow[excelDataColumnName]?.ToString() ?? "";
                        System.Diagnostics.Debug.WriteLine($"    ✓ 텍스트 컬럼 '{excelDataColumnName}' 찾음, 값='{value}'");
                    }
                }

                if (string.IsNullOrEmpty(excelDataColumnName) || !excelData.Columns.Contains(excelDataColumnName))
                {
                    System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 '{excelDataColumnName ?? "null"}' 없음");
                    continue;
                }

                if (string.IsNullOrEmpty(value))
                {
                    System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 {actualColumnIndex} 값이 비어있음");
                    continue;
                }

                // 가격 컬럼인 경우 포맷 적용 (미리보기/인쇄 시 확실하게)
                if (actualColumnIndex == 1 && !string.IsNullOrWhiteSpace(value))
                {
                    string originalValue = value;
                    value = BarcodePrint.Model.PriceFormatHelper.FormatPrice(value);
                    System.Diagnostics.Debug.WriteLine($"    → [엑셀] 가격 포맷 적용: '{originalValue}' → '{value}'");
                }

                // 바코드는 텍스트를 출력하지 않도록 설정
                bool showBarcodeText = isBarcode ? false : positionSetting.ShowBarcodeText;

                // 컬럼명 결정 (가격표용: 0=품명, 1=가격, 2=비고, 3=바코드)
                string columnName = actualColumnIndex == 0 ? "품명" :
                                  actualColumnIndex == 1 ? "가격" :
                                  actualColumnIndex == 2 ? "비고" : "바코드";

                // 바코드 타입 결정 (디버깅 강화)
                string barcodeType = isBarcode ? (positionSetting.BarcodeType ?? PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE") : positionSetting.BarcodeType;

                result.Add(new MultiColumnData
                {
                    Value = value,
                    IsBarcode = isBarcode,
                    BarcodeType = barcodeType,
                    ColumnName = columnName,
                    Size = positionSetting.Size,
                    BarcodeHeight = positionSetting.BarcodeHeight,
                    OffsetX = positionSetting.OffsetX,
                    OffsetY = positionSetting.OffsetY,
                    ShowBarcodeText = showBarcodeText // 바코드는 항상 false
                });

                System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기] 컬럼 {actualColumnIndex} ({columnName}): 값='{value}', IsBarcode={isBarcode}, BarcodeType={barcodeType} (설정:{positionSetting.BarcodeType}, 전역:{PRINT_INFO_CLASS.PrinterInfo.BarcodeType}), 크기={positionSetting.Size}, X={positionSetting.OffsetX}, Y={positionSetting.OffsetY}");
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

        /// <summary>
        /// 미리보기 크기 업데이트
        /// </summary>
        private void UpdatePreviewPanelSize()
        {
            try
            {
                // 실제 라벨 크기 (mm 단위)
                int labelWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                int labelHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;

                // 라벨이 회전되어 있는지 확인
                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
                {
                    // 회전된 경우 너비와 높이 교체
                    int temp = labelWidth;
                    labelWidth = labelHeight;
                    labelHeight = temp;
                }

                // 비율 계산
                double aspectRatio = (double)labelWidth / labelHeight;

                // 최대 미리보기 크기 설정 (크게 수정)
                const int MAX_PREVIEW_WIDTH = 500;
                const int MAX_PREVIEW_HEIGHT = 400;

                int previewWidth, previewHeight;

                // 최대 크기 내에서 비율 유지하며 크기 계산
                if (aspectRatio > 1) // 가로가 더 긴 경우
                {
                    previewWidth = Math.Min(labelWidth * 4, MAX_PREVIEW_WIDTH); // 4배 확대 또는 최대 크기
                    previewHeight = (int)(previewWidth / aspectRatio);

                    if (previewHeight > MAX_PREVIEW_HEIGHT)
                    {
                        previewHeight = MAX_PREVIEW_HEIGHT;
                        previewWidth = (int)(previewHeight * aspectRatio);
                    }
                }
                else // 세로가 더 긴 경우
                {
                    previewHeight = Math.Min(labelHeight * 4, MAX_PREVIEW_HEIGHT); // 4배 확대 또는 최대 크기
                    previewWidth = (int)(previewHeight * aspectRatio);

                    if (previewWidth > MAX_PREVIEW_WIDTH)
                    {
                        previewWidth = MAX_PREVIEW_WIDTH;
                        previewHeight = (int)(previewWidth / aspectRatio);
                    }
                }

                // 최소 크기 보장
                previewWidth = Math.Max(previewWidth, 100);
                previewHeight = Math.Max(previewHeight, 80);

                // Panel 크기 적용
                if (panel_BarcodePreview != null)
                {
                    panel_BarcodePreview.Size = new Size(previewWidth, previewHeight);
                }

                // 라벨 크기 정보 업데이트
                UpdateLabelSizeInfo();

                // 디버그 정보 출력
                System.Diagnostics.Debug.WriteLine($"라벨 크기: {labelWidth}mm x {labelHeight}mm");
                System.Diagnostics.Debug.WriteLine($"미리보기 크기: {previewWidth}px x {previewHeight}px");
                System.Diagnostics.Debug.WriteLine($"비율: {aspectRatio:F2}");

                // 미리보기 다시 그리기
                if (panel_BarcodePreview != null)
                {
                    panel_BarcodePreview.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"미리보기 크기 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 엑셀 파일 열기 대화상자
        /// </summary>
        private void OpenExcelFileDlg()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Excel Files (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Excel 2007+ (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
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

                    LoadExcelData(excelFilePath, true); // 파일 직접 열기 시 다이얼로그 강제 표시
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
        private void LoadExcelData(string filePath, bool forceShowDialog = false)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".xlsx" || extension == ".xls")
                {
                    // Excel 파일의 경우 컬럼 선택 기능 사용
                    LoadExcelWithColumnSelection(filePath, forceShowDialog);
                }
                else
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

                        MessageBox.Show($"CSV 파일이 성공적으로 로드되었습니다.\n총 {excelData.Rows.Count}개의 행이 있습니다.\n첫 번째 컬럼이 바코드로 사용됩니다.",
                            "성공", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 디버그 정보 출력
                        System.Diagnostics.Debug.WriteLine($"CSV 파일 로드 완료: {filePath}");
                        System.Diagnostics.Debug.WriteLine($"총 행 수: {excelData.Rows.Count}");
                        System.Diagnostics.Debug.WriteLine($"총 열 수: {excelData.Columns.Count}");
                    }
                    else
                    {
                        MessageBox.Show("CSV 파일에서 데이터를 읽을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 데이터 로드 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"엑셀 데이터 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 컬럼 선택을 통한 Excel 데이터 로드
        /// </summary>
        private void LoadExcelWithColumnSelection(string filePath, bool forceShowDialog = false)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // 첫 번째 워크시트 사용

                    if (worksheet == null)
                    {
                        MessageBox.Show("워크시트를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var range = worksheet.RangeUsed();
                    if (range == null)
                    {
                        MessageBox.Show("엑셀 파일에 데이터가 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ★ 저장된 컬럼 설정이 있고 forceShowDialog가 false이면 다이얼로그 건너뛰기
                    bool hasStoredSettings = multiColumnSettings != null && multiColumnSettings.Count > 0;
                    bool showDialog = !hasStoredSettings || forceShowDialog;

                    System.Diagnostics.Debug.WriteLine($"[LoadExcelWithColumnSelection] 저장된 컬럼 설정: {(hasStoredSettings ? $"{multiColumnSettings.Count}개 있음" : "없음")}");
                    System.Diagnostics.Debug.WriteLine($"[LoadExcelWithColumnSelection] 다이얼로그 표시: {showDialog} (forceShowDialog={forceShowDialog})");

                    List<ExcelMultiColumnSelectionForm.ColumnSetting> selectedSettings = null;

                    if (showDialog)
                    {
                        // 다중 컬럼 선택 다이얼로그 표시
                        using (var multiColumnSelectionForm = new ExcelMultiColumnSelectionForm(worksheet, this, columnPositionSettings))
                        {
                            if (multiColumnSelectionForm.ShowDialog() == DialogResult.OK)
                            {
                                selectedSettings = multiColumnSelectionForm.SelectedColumnSettings;
                            }
                            else
                            {
                                return; // 사용자가 취소한 경우
                            }
                        }
                    }
                    else
                    {
                        // 저장된 설정 사용
                        selectedSettings = multiColumnSettings;
                        System.Diagnostics.Debug.WriteLine($"[LoadExcelWithColumnSelection] 저장된 설정 사용: {selectedSettings.Count}개 컬럼");
                    }

                    if (selectedSettings != null && selectedSettings.Count > 0)
                    {
                        // selectedSettings를 multiColumnSettings에 할당
                        multiColumnSettings = selectedSettings;

                        if (multiColumnSettings.Count > 0)
                        {
                            // 1단계: 모든 컬럼을 비활성화 (선택되지 않은 컬럼은 False로 설정)
                            System.Diagnostics.Debug.WriteLine("=== ExcelMultiColumn 동기화 시작 ===");
                            foreach (var posSetting in columnPositionSettings)
                            {
                                posSetting.IsEnabled = false;
                                System.Diagnostics.Debug.WriteLine($"컬럼 {posSetting.ColumnIndex} 초기화: IsEnabled=False");
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
                                    posSetting.Size = excelSetting.Size;
                                    posSetting.OffsetX = excelSetting.OffsetX;
                                    posSetting.OffsetY = excelSetting.OffsetY;
                                    posSetting.ShowBarcodeText = excelSetting.ShowBarcodeText;

                                    System.Diagnostics.Debug.WriteLine($"컬럼 {excelSetting.ColumnIndex} 설정 동기화: IsEnabled={excelSetting.IsEnabled}, Type={excelSetting.UsageType}, BarcodeType={excelSetting.BarcodeType}, Size={excelSetting.Size}");
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

                                MessageBox.Show($"엑셀 파일이 성공적으로 로드되었습니다.\n총 {excelData.Rows.Count}개의 행이 있습니다.\n\n선택된 컬럼들:\n{columnInfo}",
                                    "성공", MessageBoxButton.OK, MessageBoxImage.Information);

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

                                // 버튼 상태 업데이트 (엑셀 출력 버튼 활성화)
                                UpdateExcelPanelState();
                            }
                            else
                            {
                                MessageBox.Show("선택된 컬럼에 유효한 데이터가 없습니다.", "오류",
                                    MessageBoxButton.OK, MessageBoxImage.Error);

                                // 데이터가 없으면 버튼 비활성화
                                UpdateExcelPanelState();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"엑셀 파일 처리 중 오류가 발생했습니다: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"엑셀 파일 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 바코드 컬럼과 출력 텍스트 컬럼을 기준으로 DataTable 생성
        /// </summary>
        private DataTable CreateDataTableFromExcel(IXLWorksheet worksheet, int barcodeColumnIndex, int textColumnIndex)
        {
            try
            {
                DataTable dataTable = new DataTable();
                var range = worksheet.RangeUsed();
                int rowCount = range.RowCount();
                int columnCount = range.ColumnCount();

                // 바코드 컬럼과 출력 텍스트 컬럼만 추가
                var barcodeHeaderCell = worksheet.Cell(1, barcodeColumnIndex + 1);
                var textHeaderCell = worksheet.Cell(1, textColumnIndex + 1);

                string barcodeColumnName = barcodeHeaderCell.IsEmpty() ? $"바코드컬럼{barcodeColumnIndex + 1}" : barcodeHeaderCell.GetString().Trim();
                string textColumnName = textHeaderCell.IsEmpty() ? $"텍스트컬럼{textColumnIndex + 1}" : textHeaderCell.GetString().Trim();

                dataTable.Columns.Add(barcodeColumnName);
                dataTable.Columns.Add(textColumnName);

                // 데이터 행 추가 (헤더 제외하고 2행부터)
                for (int row = 2; row <= rowCount; row++)
                {
                    DataRow dataRow = dataTable.NewRow();

                    // 바코드 컬럼 데이터
                    var barcodeCell = worksheet.Cell(row, barcodeColumnIndex + 1);
                    dataRow[0] = barcodeCell.IsEmpty() ? string.Empty : barcodeCell.GetString().Trim();

                    // 출력 텍스트 컬럼 데이터
                    var textCell = worksheet.Cell(row, textColumnIndex + 1);
                    dataRow[1] = textCell.IsEmpty() ? string.Empty : textCell.GetString().Trim();

                    dataTable.Rows.Add(dataRow);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataTable 생성 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 다중 컬럼 설정을 사용하여 Excel에서 DataTable 생성
        /// </summary>
        private DataTable CreateDataTableFromMultiColumns(IXLWorksheet worksheet, List<ExcelMultiColumnSelectionForm.ColumnSetting> settings)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CreateDataTableFromMultiColumns 시작 ===");
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
                        System.Diagnostics.Debug.WriteLine($"DataTable 컬럼 추가: '{columnName}' (ColumnIndex={setting.ColumnIndex}, ExcelCol={setting.ExcelColumnNumber}, UsageType={setting.UsageType})");
                    }
                }

                // 데이터 행 추가
                var range = worksheet.RangeUsed();
                if (range != null)
                {
                    int rowCount = range.RowCount();
                    System.Diagnostics.Debug.WriteLine($"엑셀 데이터 행 수: {rowCount} (헤더 포함)");

                    for (int row = 2; row <= rowCount; row++) // 헤더 제외하고 2행부터
                    {
                        var dataRow = dataTable.NewRow();
                        int columnIndex = 0;

                        foreach (var setting in settings)
                        {
                            if (setting.IsEnabled)
                            {
                                var cell = worksheet.Cell(row, setting.ExcelColumnNumber); // ExcelColumnNumber 사용 (1부터 시작)
                                string cellValue = cell.IsEmpty() ? "" : cell.GetString();
                                dataRow[columnIndex] = cellValue;

                                if (row == 2) // 첫 번째 데이터 행만 로그
                                {
                                    System.Diagnostics.Debug.WriteLine($"  행 2, 엑셀 컬럼 {setting.ExcelColumnNumber} → DataTable[{columnIndex}]: '{cellValue}'");
                                }
                                columnIndex++;
                            }
                        }

                        dataTable.Rows.Add(dataRow);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== CreateDataTableFromMultiColumns 완료: {dataTable.Rows.Count}행, {dataTable.Columns.Count}열 ===");
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

                                    // 가격 컬럼인 경우 자동 포맷 적용
                                    if (BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(dataTable.Columns[i].ColumnName))
                                    {
                                        cellValue = BarcodePrint.Model.PriceFormatHelper.FormatPrice(cellValue);
                                    }

                                    row[i] = cellValue;
                                }
                                dataTable.Rows.Add(row);
                            }
                        }
                    }
                }
                else if (extension == ".xlsx" || extension == ".xls")
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
                                string cellValue = string.Empty;

                                if (!cell.IsEmpty())
                                {
                                    cellValue = cell.GetString().Trim();

                                    // 가격 컬럼인 경우 자동 포맷 적용
                                    if (BarcodePrint.Model.PriceFormatHelper.IsPriceColumn(dataTable.Columns[col - 1].ColumnName))
                                    {
                                        cellValue = BarcodePrint.Model.PriceFormatHelper.FormatPrice(cellValue);
                                    }
                                }

                                dataRow[col - 1] = cellValue;
                            }
                            dataTable.Rows.Add(dataRow);
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
        /// 첫 번째 행 데이터를 미리보기에 표시
        /// </summary>
        private void UpdatePreviewWithFirstRow()
        {
            try
            {
                if (excelData != null && excelData.Rows.Count > 0)
                {
                    DataRow firstRow = excelData.Rows[0];

                    if (useMultiColumnMode && multiColumnSettings.Count > 0)
                    {
                        // 가격표용 다중 컬럼 모드: 전체 데이터 미리보기 트리거
                        System.Diagnostics.Debug.WriteLine("=== 엑셀 다중 컬럼 모드 미리보기 시작 ===");
                        System.Diagnostics.Debug.WriteLine($"excelData.Rows.Count: {excelData.Rows.Count}");
                        System.Diagnostics.Debug.WriteLine($"excelData.Columns.Count: {excelData.Columns.Count}");
                        System.Diagnostics.Debug.WriteLine($"multiColumnSettings.Count: {multiColumnSettings.Count}");

                        // 첫 번째 행의 모든 컬럼 데이터 출력 (디버깅)
                        for (int i = 0; i < excelData.Columns.Count; i++)
                        {
                            string colName = excelData.Columns[i].ColumnName;
                            string colValue = firstRow[i]?.ToString() ?? "(null)";
                            System.Diagnostics.Debug.WriteLine($"  excelData 컬럼 {i}: '{colName}' = '{colValue}'");
                        }

                        // 다중 컬럼 모드에서는 OnPreviewEvent()를 호출하여 전체 미리보기 업데이트
                        // currentBarcodeValue와 currentBarcodeText는 사용하지 않음
                        currentBarcodeValue = "";
                        currentBarcodeText = "";

                        System.Diagnostics.Debug.WriteLine($"다중 컬럼 모드 - 첫 번째 행 데이터로 미리보기 생성");

                        // 첫 번째 행의 데이터를 가격표용 MultiColumnData로 변환
                        var previewData = new List<MultiColumnData>();

                        foreach (var setting in multiColumnSettings.Where(s => s.IsEnabled).OrderBy(s => s.ColumnIndex))
                        {
                            // 해당 컬럼의 DataTable 컬럼명 찾기
                            string columnName = setting.UsageType == "바코드" ?
                                $"Barcode_{setting.ColumnIndex}_{setting.BarcodeType}" :
                                $"Text_{setting.ColumnIndex}";

                            if (excelData.Columns.Contains(columnName))
                            {
                                string cellValue = firstRow[columnName]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(cellValue))
                                {
                                    var posSetting = columnPositionSettings?.FirstOrDefault(p => p.ColumnIndex == setting.ColumnIndex);
                                    if (posSetting != null)
                                    {
                                        bool isBarcode = setting.UsageType == "바코드" || setting.ColumnIndex == 3;

                                        // 바코드 타입 결정 (posSetting 우선, 없으면 setting, 없으면 전역 설정)
                                        string barcodeType = isBarcode ?
                                            (posSetting.BarcodeType ?? setting.BarcodeType ?? PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE") :
                                            posSetting.BarcodeType;

                                        previewData.Add(new MultiColumnData
                                        {
                                            Value = cellValue,
                                            IsBarcode = isBarcode,
                                            BarcodeType = barcodeType,
                                            ColumnName = setting.ColumnIndex == 0 ? "품명" : setting.ColumnIndex == 1 ? "가격" : "바코드",
                                            Size = posSetting.Size,
                                            BarcodeHeight = posSetting.BarcodeHeight,
                                            OffsetX = posSetting.OffsetX,
                                            OffsetY = posSetting.OffsetY,
                                            ShowBarcodeText = isBarcode ? false : true,
                                            Alignment = posSetting.Alignment
                                        });

                                        System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기 데이터] 컬럼 {setting.ColumnIndex}: '{cellValue}', BarcodeType={barcodeType}");
                                    }
                                }
                            }
                        }

                        // 미리보기 데이터가 있으면 미리보기 생성
                        if (previewData.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[엑셀 미리보기] {previewData.Count}개 컬럼 데이터로 미리보기 생성");
                            // GetPrintState의 PreviewWork에서 처리할 수 있도록 데이터 설정은 했으므로
                            // OnPreviewEvent 호출
                            OnPreviewEvent();
                        }

                        System.Diagnostics.Debug.WriteLine($"=== 엑셀 로드 후 미리보기 완료 ===");
                    }
                    else
                    {
                        // 기존 단일 컬럼 모드
                        string barcodeValue, textValue;

                        if (excelData.Columns.Count == 2)
                        {
                            // CreateDataTableFromExcel로 생성된 경우 (바코드, 텍스트만)
                            barcodeValue = firstRow[0]?.ToString() ?? "";
                            textValue = firstRow[1]?.ToString() ?? "";
                        }
                        else
                        {
                            // LoadExcelDataFromFile로 생성된 경우 (원본 컬럼 구조)
                            int barcodeColIndex = Math.Max(0, Math.Min(selectedBarcodeColumn, excelData.Columns.Count - 1));
                            int textColIndex = Math.Max(0, Math.Min(selectedTextColumn, excelData.Columns.Count - 1));

                            barcodeValue = firstRow[barcodeColIndex]?.ToString() ?? "";
                            textValue = firstRow[textColIndex]?.ToString() ?? "";
                        }

                        currentBarcodeValue = barcodeValue;
                        currentBarcodeText = textValue;

                        System.Diagnostics.Debug.WriteLine($"단일 컬럼 모드 - currentBarcodeValue 설정: '{currentBarcodeValue}'");
                        System.Diagnostics.Debug.WriteLine($"단일 컬럼 모드 - currentBarcodeText 설정: '{currentBarcodeText}'");
                    }

                    // 미리보기 이벤트 트리거
                    OnPreviewEvent();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"미리보기 업데이트 오류: {ex.Message}");
            }
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
                    int barcodeHeight = GetBarcodeHeight();
                    barcodePrintManager.AddToPrintQueue(barcode, barcodeSize, barcodeX, barcodeY, barcodeHeight);
                    PrintStart();
                }
                else
                {
                    // 매칭되지 않은 경우 기본 바코드만 출력
                    currentBarcodeValue = barcode;
                    currentBarcodeText = ""; // 텍스트 초기화
                    int barcodeHeight = GetBarcodeHeight();
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

                // 확인 대화상자 표시
                var result = MessageBox.Show($"엑셀 파일의 모든 데이터를 출력하시겠습니까?\n\n총 {excelData.Rows.Count}개의 행이 있습니다.",
                    "전체 출력 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                int validCount = 0;
                int errorCount = 0;

                // 모든 행을 큐에 추가
                foreach (DataRow row in excelData.Rows)
                {
                    try
                    {
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
                            int barcodeHeight = GetBarcodeHeight();
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
                MessageBox.Show($"엑셀 전체 출력 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"엑셀 전체 출력 오류: {ex.Message}");
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

                // 확인 대화상자 표시
                var result = MessageBox.Show($"엑셀 파일의 모든 데이터를 출력하시겠습니까?\n\n총 {excelData.Rows.Count}개의 행이 있습니다.\n선택된 컬럼 수: {multiColumnSettings.Count}개",
                    "전체 출력 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    System.Diagnostics.Debug.WriteLine($"[사용자 취소]");
                    return;
                }

                int validCount = 0;
                int errorCount = 0;

                System.Diagnostics.Debug.WriteLine($"[큐 추가 시작] 총 {excelData.Rows.Count}개 행");

                // 모든 행을 큐에 추가
                for (int i = 0; i < excelData.Rows.Count; i++)
                {
                    try
                    {
                        // 다중 컬럼 데이터를 인쇄 큐에 추가 (실제로 추가된 경우에만 카운트)
                        bool added = barcodePrintManager.AddMultiColumnToPrintQueue((DataTable)excelData, multiColumnSettings, i);
                        if (added)
                        {
                            validCount++;

                            if (i < 3 || i == excelData.Rows.Count - 1)
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
                MessageBox.Show($"다중 컬럼 엑셀 전체 출력 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"다중 컬럼 엑셀 전체 출력 오류: {ex.Message}");
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
                    // 엑셀 데이터가 있으면 버튼 활성화, 없으면 비활성화
                    panel_PrintButton.Enabled = (excelData != null && excelData.Rows.Count > 0);
                    panel_PrintButton.Invalidate(); // 버튼 텍스트 업데이트

                    // 실제 데이터가 있을 때만 파일명 라벨 표시
                    if (excelData != null && !string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
                    {
                        UpdateExcelFileNameLabel();
                    }
                    else
                    {
                        label_ExcelFileName.Text = "";
                        label_ExcelFileName.Visible = false;
                    }
                }
                else
                {
                    DrawDisabledExcelButton();
                    panel_PrintButton.Enabled = true; // 직접 출력 모드에서는 항상 활성화
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
                TouchSwitch switchControl = sender as TouchSwitch;
                if (switchControl != null)
                {
                    // 스위치 상태에 따라 텍스트박스 활성화/비활성화
                    textBox_Title.Enabled = switchControl.Checked;

                    // 설정 저장
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

        // 슬라이더는 PositionAdjustmentForm으로 이동됨

        private void touchSlider_OffsetY0_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void touchSlider_OffsetY2_ValueChanged(object sender, EventArgs e)
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
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public string UsageType { get; set; } = "텍스트"; // 기본값
        public string BarcodeType { get; set; } = "QR_CODE"; // 기본값
        public bool ShowBarcodeText { get; set; } = true; // 바코드 값 표시 (기본값 true)
        public string Alignment { get; set; } = "Left";   // 정렬 상태: Left, Center, Right
        public object Tag { get; set; } // UI 컨트롤 참조
    }
}

