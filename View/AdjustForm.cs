using Argon;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BarcodePrint.Controll;
using BarcodePrint.Model;
using ZXing;

namespace BarcodePrint.View
{
    public partial class AdjustForm : MaterialForm
    {
        MainForm mainForm;
        FontList fontList1 = new FontList();
        private MaterialSkinManager materialSkinManager;
        

        public AdjustForm(MainForm main)
        {
            InitializeComponent();
            mainForm = main;

            // MaterialSkinManager 초기화
            materialSkinManager = MaterialSkinManager.Instance;

            // 폼에 MaterialSkin 적용
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            
            materialSkinManager.ColorScheme = new ColorScheme(
            Primary.Blue500,  // 버튼 및 컨트롤 색상
            Primary.Blue500,  // 폼 배경색상
            Primary.Blue500,  // 폼의 다른 강조 색상
            Accent.Blue400,  // 체크박스 등의 강조 색상
            TextShade.WHITE  // 텍스트 색상
             );

            // 폼 크기 변경 불가능하게 설정
            this.FormBorderStyle = FormBorderStyle.FixedSingle;  // 크기 변경 불가능
            this.MaximizeBox = false;  // 최대화 버튼 비활성화
            this.MinimizeBox = false;  // 최소화 버튼 비활성화
            
            // 센서 타입 라디오 버튼 활성화 (사용자가 선택 가능)
            // RadioButton_Gap, RadioButton_bline, RadioButton_continues 모두 활성화

            // 인코딩 문제 방지: 한글 텍스트 명시적 설정
            SetKoreanTexts();

            // DPI 스케일링 대응: 폼 크기와 위치 고정
            FixFormSizeAndPosition();

            Init_Control();
            //Set_Control();
            //SetControl_Colume();
            InitEvent();
            InitPrinterComboBox();
            
            // MainForm의 MachineList 이벤트 구독
            if (mainForm != null)
            {
                mainForm.printerList.machineListChanged += OnMachineListChanged;
            }
            
            // 폼 닫힐 때 MainForm 라벨 크기 정보 업데이트
            this.FormClosing += AdjustForm_FormClosing;
        }
                 
        /// <summary>
        /// 한글 텍스트 명시적 설정 (인코딩 문제 방지)
        /// </summary>
        private void SetKoreanTexts()
        {
            try
            {
                // 폼 제목
                this.Text = "고급 설정";

                // 라벨 텍스트 설정 (인코딩 문제 방지)
                if (label1 != null)
                    label1.Text = "프린터 선택";

                System.Diagnostics.Debug.WriteLine("[AdjustForm] 한글 텍스트 설정 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdjustForm] 한글 텍스트 설정 오류: {ex.Message}");
            }
        }

        public void InitEvent() {
            
            // 슬라이더바 값 표시 이벤트
            Slider_Speed.ValueChanged += Slider_Speed_ValueChanged;
            Slider_darkness.ValueChanged += Slider_darkness_ValueChanged;
            
            RadioButton_Gap.CheckedChanged += RadioButton_Gap_CheckedChanged;
            RadioButton_bline.CheckedChanged += RadioButton_bline_CheckedChanged;
            RadioButton_continues.CheckedChanged += RadioButton_continues_CheckedChanged;
            button_GapUp.Click += button_GapUp_Click;
            button_GapDown.Click += button_GapDown_Click;
            button_hOffset_XUp.Click += button_hOffset_XUp_Click;
            button_hOffset_XDown.Click += button_hOffset_XDown_Click;
            button_hOffset_YUp.Click += button_hOffset_YUp_Click;
            button_hOffset_YDown.Click += button_hOffset_YDown_Click;
            // materialComboBox_BarcodeType.SelectedIndexChanged += materialComboBox_BarcodeType_SelectedIndexChanged; // 바코드 타입 선택은 ExcelMultiColumnSelectionForm에서 처리
         }

        private void InitPrinterComboBox()
        {
            // 이벤트 핸들러 연결 (디자이너에서 생성된 컨트롤 사용)
            materialComboBox_PrinterList.SelectedIndexChanged += new System.EventHandler(this.ComboBox_MachineList_SelectedIndexChanged);
            
            // MachineList를 사용하여 프린터 목록 로드
            LoadPrinterListFromMachineList();
             
            // 디버깅을 위한 메시지
            System.Diagnostics.Debug.WriteLine($"프린터 콤보박스 위치: {materialComboBox_PrinterList.Location}, 크기: {materialComboBox_PrinterList.Size}");
            System.Diagnostics.Debug.WriteLine($"폼 크기: {this.Size}");
        }
        
        
                 
        private bool isLoadingPrinterList = false; // 프린터 목록 로딩 중 플래그

        private void LoadPrinterListFromMachineList()
        { 
             try
             {
                 isLoadingPrinterList = true; // 이벤트 핸들러 비활성화                 
                 materialComboBox_PrinterList.Items.Clear();
             
             // MachineList를 사용하여 연결된 USB 프린터만 가져오기
             if (mainForm != null && mainForm.printerList != null)
             {
                 // MachineList의 USB_status에서 연결된 프린터만 필터링
                 var connectedPrinters = new List<string>();
                 
                 if (BarcodePrint.Model.USB_status.Name != null && BarcodePrint.Model.USB_status.Connect != null)
                 {
                     // 두 리스트의 크기가 다를 수 있으므로 더 작은 크기로 반복
                     int minCount = Math.Min(BarcodePrint.Model.USB_status.Name.Count, BarcodePrint.Model.USB_status.Connect.Count);
                     
                     System.Diagnostics.Debug.WriteLine($"USB_status 정보 확인 - 총 프린터 수: {minCount}");
                     
                     for (int i = 0; i < minCount; i++)
                     {
                         string printerName = BarcodePrint.Model.USB_status.Name[i];
                         bool isConnected = BarcodePrint.Model.USB_status.Connect[i];
                         string portInfo = (BarcodePrint.Model.USB_status.Port != null && i < BarcodePrint.Model.USB_status.Port.Count) 
                                           ? BarcodePrint.Model.USB_status.Port[i] : "";
                         
                         System.Diagnostics.Debug.WriteLine($"프린터 {i}: {printerName}, 연결상태: {isConnected}, 포트: {portInfo}");
                         
                         bool isXprinter = !string.IsNullOrEmpty(printerName) && 
                                           (printerName.IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            printerName.IndexOf("XP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            printerName.IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            BarcodePrint.Model.UsbPrinterHelper.supportedPrinterModels.Any(model => printerName.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0));

                         // 엄격한 연결 상태 확인
                         if (isConnected && isXprinter && !string.IsNullOrEmpty(portInfo))
                         {
                             // 빠른 프린터 추가 (Windows 프린터 목록 조회 생략으로 성능 향상)
                             connectedPrinters.Add(printerName);
                             System.Diagnostics.Debug.WriteLine($"✅ 연결된 프린터 추가: {printerName} (포트: {portInfo})");
                             
                             // 선택적으로 Windows 프린터 목록 확인 (백그라운드에서)
                             Task.Run(() =>
                             {
                                 try
                                 {
                                     var installedPrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
                                     bool printerExists = false;
                                     
                                     foreach (string installedPrinter in installedPrinters)
                                     {
                                         if (installedPrinter.Contains(printerName) || printerName.Contains(installedPrinter))
                                         {
                                             printerExists = true;
                                             break;
                                         }
                                     }
                                     
                                     if (!printerExists)
                                     {
                                         System.Diagnostics.Debug.WriteLine($"⚠️ 백그라운드 확인: 설치되지 않은 프린터 감지 - {printerName}");
                                         // 필요시 UI에서 제거할 수 있음
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     System.Diagnostics.Debug.WriteLine($"백그라운드 프린터 존재 확인 중 오류: {ex.Message}");
                                 }
                             });
                         }
                         else
                         {
                             System.Diagnostics.Debug.WriteLine($"❌ 연결되지 않은 프린터 제외: {printerName} (연결상태: {isConnected}, 포트: {portInfo})");
                         }
                     }
                 }

                // 연결된 프린터 목록 추가
                foreach (string printer in connectedPrinters)
                {
                     materialComboBox_PrinterList.Items.Add(printer);
                }

                 // 현재 선택된 프린터 설정 - 연결된 프린터 우선 선택
                 string currentPrinter = PRINT_INFO_CLASS.PrinterInfo.printerName;
                 bool printerSelected = false;
                 
                 // 1순위: 현재 설정된 프린터가 연결된 목록에 있으면 선택
                 if (!string.IsNullOrEmpty(currentPrinter) && materialComboBox_PrinterList.Items.Contains(currentPrinter))
                 {
                     materialComboBox_PrinterList.SelectedItem = currentPrinter;
                     printerSelected = true;
                     System.Diagnostics.Debug.WriteLine($"기존 프린터 유지: {currentPrinter}");
                 }
                 // 2순위: 연결된 프린터가 있으면 첫 번째 프린터 자동 선택
                 else if (materialComboBox_PrinterList.Items.Count > 0)
                 {
                     string autoSelectedPrinter = materialComboBox_PrinterList.Items[0].ToString();
                     materialComboBox_PrinterList.SelectedIndex = 0;
                     printerSelected = true;
                     
                     // 자동 선택된 프린터를 전역 변수에 설정
                     PRINT_INFO_CLASS.PrinterInfo.printerName = autoSelectedPrinter;
                     
                     // 설정에 저장
                     Properties.Settings.Default.SelectedPrinterName = autoSelectedPrinter;
                     Properties.Settings.Default.Save();
                     
                     System.Diagnostics.Debug.WriteLine($"자동 프린터 선택: {autoSelectedPrinter}");
                     
                     // MainForm에 프린터 변경 알림
                     if (mainForm != null)
                     {
                            System.Diagnostics.Debug.WriteLine("MachineList_selectChanged : 3");
                            mainForm.MachineList_selectChanged(autoSelectedPrinter);
                     }
                 }
                 
                 if (!printerSelected)
                 {
                     System.Diagnostics.Debug.WriteLine("선택할 수 있는 연결된 프린터가 없습니다.");
                 }
                 
                 // 프린터가 없을 경우 안내 메시지
                 if (materialComboBox_PrinterList.Items.Count == 0)
                 {
                     materialComboBox_PrinterList.Items.Add("연결된 Xprinter 프린터가 없습니다.");
                     materialComboBox_PrinterList.Enabled = false;
                     System.Diagnostics.Debug.WriteLine("프린터가 없어서 ComboBox 비활성화");
                 }
                 else
                 {
                     materialComboBox_PrinterList.Enabled = true;
                     System.Diagnostics.Debug.WriteLine($"프린터 {materialComboBox_PrinterList.Items.Count}대 감지 - ComboBox 활성화");
                 }
                 
                 // 디버깅을 위한 정보 출력
                 System.Diagnostics.Debug.WriteLine($"연결된 프린터 목록 개수: {materialComboBox_PrinterList.Items.Count}");
                 foreach (var item in materialComboBox_PrinterList.Items)
                 {
                     System.Diagnostics.Debug.WriteLine($"연결된 프린터: {item}");
                 }
             }
             else
             {
                 // MachineList가 없는 경우 기본 USB 프린터 조회 방식 사용
                 var usbPrinterInfoList = BarcodePrint.Model.UsbPrinterHelper.GetConnectedUsbPrinters();
                 var xprinterPrinters = usbPrinterInfoList
                      .Where(p => p.Name.IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  p.Name.IndexOf("XP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  p.Name.IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  BarcodePrint.Model.UsbPrinterHelper.supportedPrinterModels.Any(model => p.Name.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0))
                     .Select(p => p.Name)
                     .ToList();
                     
                 System.Diagnostics.Debug.WriteLine($"UsbPrinterHelper로 조회된 연결된 Xprinter 프린터 수: {xprinterPrinters.Count}");

                // 필터링된 프린터 목록 추가
                foreach (string printer in xprinterPrinters)
                {
                     materialComboBox_PrinterList.Items.Add(printer);
                }

                 // 현재 선택된 프린터 설정 - 연결된 프린터 우선 선택 (UsbPrinterHelper 방식)
                 string currentPrinter = PRINT_INFO_CLASS.PrinterInfo.printerName;
                 bool printerSelected = false;
                 
                 // 1순위: 현재 설정된 프린터가 연결된 목록에 있으면 선택
                 if (!string.IsNullOrEmpty(currentPrinter) && materialComboBox_PrinterList.Items.Contains(currentPrinter))
                 {
                     materialComboBox_PrinterList.SelectedItem = currentPrinter;
                     printerSelected = true;
                     System.Diagnostics.Debug.WriteLine($"기존 프린터 유지 (UsbPrinterHelper): {currentPrinter}");
                 }
                 // 2순위: 연결된 프린터가 있으면 첫 번째 프린터 자동 선택
                 else if (materialComboBox_PrinterList.Items.Count > 0)
                 {
                     string autoSelectedPrinter = materialComboBox_PrinterList.Items[0].ToString();
                     materialComboBox_PrinterList.SelectedIndex = 0;
                     printerSelected = true;
                     
                     // 자동 선택된 프린터를 전역 변수에 설정
                     PRINT_INFO_CLASS.PrinterInfo.printerName = autoSelectedPrinter;
                     
                     // 설정에 저장
                     Properties.Settings.Default.SelectedPrinterName = autoSelectedPrinter;
                     Properties.Settings.Default.Save();
                     
                     System.Diagnostics.Debug.WriteLine($"자동 프린터 선택 (UsbPrinterHelper): {autoSelectedPrinter}");
                     
                     // MainForm에 프린터 변경 알림
                     if (mainForm != null)
                     {
                            System.Diagnostics.Debug.WriteLine("MachineList_selectChanged : 2");
                            mainForm.MachineList_selectChanged();
                     }
                 }
                 
                 if (!printerSelected)
                 {
                     System.Diagnostics.Debug.WriteLine("선택할 수 있는 연결된 프린터가 없습니다 (UsbPrinterHelper).");
                 }
                 
                 // 프린터가 없을 경우 안내 메시지
                 if (materialComboBox_PrinterList.Items.Count == 0)
                 {
                     materialComboBox_PrinterList.Items.Add("연결된 Xprinter 프린터가 없습니다.");
                     materialComboBox_PrinterList.Enabled = false;
                     System.Diagnostics.Debug.WriteLine("UsbPrinterHelper - 프린터가 없어서 ComboBox 비활성화");
                 }
                 else
                 {
                     materialComboBox_PrinterList.Enabled = true;
                     System.Diagnostics.Debug.WriteLine($"UsbPrinterHelper - 프린터 {materialComboBox_PrinterList.Items.Count}대 감지 - ComboBox 활성화");
                 }
             }
            }
            catch (Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"LoadPrinterListFromMachineList 오류: {ex.Message}");
                 
                 // 오류 발생 시에도 프린터가 있으면 ComboBox 활성화
                 if (materialComboBox_PrinterList.Items.Count > 0)
                 {
                     System.Diagnostics.Debug.WriteLine("오류 발생했지만 프린터가 있어서 ComboBox 활성화");
                     materialComboBox_PrinterList.Enabled = true;
                 }
                 else
                 {
                     // 프린터가 없을 때만 비활성화
                     materialComboBox_PrinterList.Items.Clear();
                     materialComboBox_PrinterList.Items.Add("프린터 목록 로딩 중 오류가 발생했습니다.");
                     materialComboBox_PrinterList.Enabled = false;
                 }
             }
             finally
             {
                 isLoadingPrinterList = false; // 이벤트 핸들러 다시 활성화
                 System.Diagnostics.Debug.WriteLine("LoadPrinterListFromMachineList 완료");
             }
         }

                 
        private void ComboBox_MachineList_SelectedIndexChanged(object sender, EventArgs e)
         {
             // 프린터 목록 로딩 중에는 이벤트 처리 건너뛰기 (성능 최적화)
             if (isLoadingPrinterList)
             {
                 System.Diagnostics.Debug.WriteLine("프린터 목록 로딩 중 - 이벤트 처리 건너뛰기");
                 return;
             }

             if (materialComboBox_PrinterList.SelectedItem != null)
             {
                 string selectedPrinter = materialComboBox_PrinterList.SelectedItem.ToString();
                 
                 // 안내 메시지가 선택된 경우 처리하지 않음
                 if (selectedPrinter == "연결된 Xprinter 프린터가 없습니다." || 
                     selectedPrinter == "프린터 목록 로딩 중 오류가 발생했습니다.")
                 {
                     return;
                 }
                 
                 System.Diagnostics.Debug.WriteLine($"프린터 선택 변경: {selectedPrinter}");
                 
                 // 전역 프린터 정보 업데이트
                 PRINT_INFO_CLASS.PrinterInfo.printerName = selectedPrinter;
                 System.Diagnostics.Debug.WriteLine($"전역 프린터 정보 업데이트: {PRINT_INFO_CLASS.PrinterInfo.printerName}");
                 
                 // 설정에 저장
                 Properties.Settings.Default.SelectedPrinterName = selectedPrinter;
                 Properties.Settings.Default.Save();
                 System.Diagnostics.Debug.WriteLine($"프린터 설정 저장 완료: {selectedPrinter}");
                 
                 // 프린터 선택 변경 시 포트 정보 업데이트 및 상태 스레드 재시작 (백그라운드 처리)
                 if (mainForm != null)
                 {
                     System.Diagnostics.Debug.WriteLine($"MainForm.MachineList_selectChanged 백그라운드 호출 시작: {selectedPrinter}");
                     
                     // 백그라운드 스레드에서 프린터 변경 처리
                     Task.Run(() =>
                     {
                         try
                         {
                             System.Diagnostics.Debug.WriteLine($"백그라운드에서 MainForm.MachineList_selectChanged 실행: {selectedPrinter}");
                             System.Diagnostics.Debug.WriteLine("MachineList_selectChanged : 4");
                             mainForm.MachineList_selectChanged(selectedPrinter);
                             System.Diagnostics.Debug.WriteLine($"백그라운드 MainForm.MachineList_selectChanged 완료");
                         }
                         catch (Exception ex)
                         {
                             System.Diagnostics.Debug.WriteLine($"백그라운드 프린터 변경 처리 중 오류: {ex.Message}");
                             
                             // UI 스레드에서 오류 메시지 표시
                             if (this.InvokeRequired)
                             {
                                 this.Invoke(new Action(() =>
                                 {
                                     MessageBox.Show($"프린터 변경 중 오류가 발생했습니다: {ex.Message}", 
                                                     "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                 }));
                             }
                         }
                         finally
                         {
                             // 백그라운드 처리 완료 로그
                             System.Diagnostics.Debug.WriteLine("백그라운드 프린터 변경 처리 완료");
                         }
                     });
                 }
                 else
                 {
                     System.Diagnostics.Debug.WriteLine("⚠️ MainForm이 null입니다!");
                 }
             }
         }

                 
        private void materialComboBox_PrinterList_MouseUp(object sender, MouseEventArgs e)
        {
             // MachineList를 사용하여 프린터 목록 새로고침
             //LoadPrinterListFromMachineList();
        }
        
        /// <summary>
        /// MachineList 변경 이벤트 핸들러 (백그라운드 처리)
        /// </summary>
        private void OnMachineListChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnMachineListChanged 이벤트 발생 - 백그라운드 처리 시작");
            
            // 백그라운드 스레드에서 프린터 목록 로딩 처리
            Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("백그라운드에서 프린터 목록 로딩 시작");
                    
                    // UI 스레드에서 프린터 목록 업데이트
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                LoadPrinterListFromMachineList();
                                System.Diagnostics.Debug.WriteLine("백그라운드 프린터 목록 로딩 완료 (UI 스레드)");
                            }
                            catch (Exception uiEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"UI 스레드에서 프린터 목록 로딩 오류: {uiEx.Message}");
                            }
                        }));
                    }
                    else
                    {
                        LoadPrinterListFromMachineList();
                        System.Diagnostics.Debug.WriteLine("백그라운드 프린터 목록 로딩 완료 (현재 스레드)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"백그라운드 프린터 목록 로딩 중 오류: {ex.Message}");
                    
                    // UI 스레드에서 오류 메시지 표시
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"프린터 목록 업데이트 중 오류가 발생했습니다: {ex.Message}", 
                                            "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                    }
                }
            });
        }
         

        public void Init_Control()
        {
            // 용지 설정
            SetControl_Paper();         
        }


                 private void AdjustForm_FormClosing(object sender, FormClosingEventArgs e)
         {
             PrintSetting();

             // MachineList 이벤트 구독 해제
             if (mainForm != null)
             {
                 mainForm.printerList.machineListChanged -= OnMachineListChanged;
                 
                 // MainForm의 라벨 크기 정보 업데이트
                 mainForm.UpdateLabelSizeInfo();
                 
                 // 미리보기 화면 최신화
                 mainForm.OnPreviewEvent();
                 
                 System.Diagnostics.Debug.WriteLine("AdjustForm 닫힘 - 라벨 크기 정보 업데이트 완료");
             }
             materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
         }


                
        public void SetControl_Paper() {
             Slider_darkness.Value = PRINT_INFO_CLASS.PrinterInfo.nDarkness;
             Slider_Speed.Value = PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed;
             // 슬라이더바 값 표시 업데이트
             if (label_Value_Speed != null)
                 label_Value_Speed.Text = Slider_Speed.Value.ToString();
             if (label_Value_Darkness != null)
                 label_Value_Darkness.Text = Slider_darkness.Value.ToString();
             textBox_Gap.Text = PRINT_INFO_CLASS.PrinterInfo.nGAP.ToString();
             textBox_hOffset_X.Text = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X.ToString();
             textBox_hOffset_Y.Text = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y.ToString();

             if (PRINT_INFO_CLASS.PrinterInfo.bRibbon)
             {
                 Switch_Ribbon.Checked = true;
             }
             else
             {
                 Switch_Ribbon.Checked = false;
             }

             if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
             {
                 RadioButton_bline.Checked = true;
             }
             else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
             {
                 RadioButton_Gap.Checked = true;
             }
             else
             {
                 RadioButton_continues.Checked = true;
             }
         }

       
        

         private void RadioButton_Gap_CheckedChanged(object sender, EventArgs e)
         {
             if (RadioButton_Gap.Checked)
             {
                 PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.GAP;
             }
         }

         private void RadioButton_bline_CheckedChanged(object sender, EventArgs e)
         {
             if (RadioButton_bline.Checked)
             {
                 PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.BLINE;
             }
         }

         private void RadioButton_continues_CheckedChanged(object sender, EventArgs e)
         {
             if (RadioButton_continues.Checked)
             {
                 PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.CONTINEUS;
             }
         }

         private void button_GapUp_Click(object sender, EventArgs e)
         {
             int currentGap = PRINT_INFO_CLASS.PrinterInfo.nGAP;
             int newGap = Math.Min(currentGap + 1, 1000);
             PRINT_INFO_CLASS.PrinterInfo.nGAP = newGap;
             textBox_Gap.Text = newGap.ToString();
         }

         private void button_GapDown_Click(object sender, EventArgs e)
         {
             int currentGap = PRINT_INFO_CLASS.PrinterInfo.nGAP;
             int newGap = Math.Max(currentGap - 1, 1);
             PRINT_INFO_CLASS.PrinterInfo.nGAP = newGap;
             textBox_Gap.Text = newGap.ToString();
         }

         private void button_hOffset_XUp_Click(object sender, EventArgs e)
         {
             int currentOffset = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X;
             int newOffset = Math.Min(currentOffset + 1, 1000);
             PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X = newOffset;
             textBox_hOffset_X.Text = newOffset.ToString();
         }

         private void button_hOffset_XDown_Click(object sender, EventArgs e)
         {
             int currentOffset = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X;
             int newOffset = Math.Max(currentOffset - 1, -1000);
             PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X = newOffset;
             textBox_hOffset_X.Text = newOffset.ToString();
         }

         private void button_hOffset_YUp_Click(object sender, EventArgs e)
         {
             int currentOffset = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y;
             int newOffset = Math.Min(currentOffset + 1, 1000);
             PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y = newOffset;
             textBox_hOffset_Y.Text = newOffset.ToString();
         }

         private void button_hOffset_YDown_Click(object sender, EventArgs e)
         {
             int currentOffset = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y;
             int newOffset = Math.Max(currentOffset - 1, -1000);
             PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y = newOffset;
             textBox_hOffset_Y.Text = newOffset.ToString();
         }

         // 바코드 타입 선택은 ExcelMultiColumnSelectionForm에서 처리하므로 제거됨

         private void new_CalibrationButton(float shadowRate = 1f)
         {
             string ButtonText = "용지센싱";
             System.Drawing.Font TextFont = new System.Drawing.Font("Roboto", 14);
             Color originalColor = Color.DodgerBlue;
             Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);

             using (Graphics g = panel_Calibration.CreateGraphics())
             {
                 g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                 using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                 {
                     int r = 8;
                     int w = panel_Calibration.Width - 1, h = panel_Calibration.Height - 1;
                     path.AddArc(0, 0, r, r, 180, 90);
                     path.AddArc(w - r, 0, r, r, 270, 90);
                     path.AddArc(w - r, h - r, r, r, 0, 90);
                     path.AddArc(0, h - r, r, r, 90, 90);
                     path.CloseFigure();
                     using (SolidBrush brush = new SolidBrush(panelColor))
                     {
                         g.FillPath(brush, path);
                     }
                     using (SolidBrush textBrush = new SolidBrush(Color.White))
                     {
                         SizeF textSize = g.MeasureString(ButtonText, TextFont);
                         PointF textPosition = new PointF(
                             (panel_Calibration.Width - textSize.Width) / 2,
                             (panel_Calibration.Height - textSize.Height) / 2
                         );
                         g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                     }
                 }
             }
         }

         private void new_AdjustSetButton(float shadowRate = 1f)
         {
             string ButtonText = "닫기";
             System.Drawing.Font TextFont = new System.Drawing.Font("Roboto", 13);
             Color originalColor = Color.DodgerBlue;
             Color panelColor = PanelApplyShadowEffect(originalColor, shadowRate);
            try
            {
                using (Graphics g = panel_AdjustSet.CreateGraphics())
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int r = 10;
                        int w = panel_AdjustSet.Width - 1, h = panel_AdjustSet.Height - 1;
                        path.AddArc(0, 0, r, r, 180, 90);
                        path.AddArc(w - r, 0, r, r, 270, 90);
                        path.AddArc(w - r, h - r, r, r, 0, 90);
                        path.AddArc(0, h - r, r, r, 90, 90);
                        path.CloseFigure();
                        using (SolidBrush brush = new SolidBrush(panelColor))
                        {
                            g.FillPath(brush, path);
                        }
                        using (SolidBrush textBrush = new SolidBrush(Color.White))
                        {
                            SizeF textSize = g.MeasureString(ButtonText, TextFont);
                            PointF textPosition = new PointF(
                                (panel_AdjustSet.Width - textSize.Width) / 2,
                                (panel_AdjustSet.Height - textSize.Height) / 2
                            );
                            g.DrawString(ButtonText, TextFont, textBrush, textPosition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //   MessageBox.Show(ex.Message);
            }
                
         }

         private Color PanelApplyShadowEffect(Color originalColor, float shadowRate)
         {
             int r = (int)(originalColor.R * shadowRate);
             int g = (int)(originalColor.G * shadowRate);
             int b = (int)(originalColor.B * shadowRate);
             
             r = Math.Min(255, Math.Max(0, r));
             g = Math.Min(255, Math.Max(0, g));
             b = Math.Min(255, Math.Max(0, b));
             
             return Color.FromArgb(r, g, b);
         }

        private void panel_Calibration_Paint(object sender, PaintEventArgs e)
        {
            new_CalibrationButton();
        }

        private void panel_Calibration_MouseEnter(object sender, EventArgs e)
        {
            new_CalibrationButton(1.1f);
        }

        private void panel_Calibration_MouseLeave(object sender, EventArgs e)
        {
            new_CalibrationButton();
        }

        private void panel_Calibration_MouseUp(object sender, MouseEventArgs e)
        {
            new_CalibrationButton();
        }

        private void panel_Calibration_MouseDown(object sender, MouseEventArgs e)
        {
            new_CalibrationButton(0.8f);
        }

        private void panel_Calibration_Click(object sender, EventArgs e)
        {
            try
            {
                string printerName = PRINT_INFO_CLASS.PrinterInfo.printerName;
                new BarcodePrintManager().SendPrintCommand(printerName, "CALIBRATION");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CALIBRATION 명령어 전송 실패: {ex.Message}", "오류");
            }
        }

        private void panel_AdjustSet_Paint(object sender, PaintEventArgs e)
        {
            new_AdjustSetButton();
        }

        private void panel_AdjustSet_MouseDown(object sender, MouseEventArgs e)
        {
            new_AdjustSetButton(0.8f);
        }

        private void panel_AdjustSet_MouseEnter(object sender, EventArgs e)
        {
            new_AdjustSetButton(1.1f);
        }

        private void panel_AdjustSet_MouseLeave(object sender, EventArgs e)
        {
            new_AdjustSetButton();        
        }

        private void panel_AdjustSet_MouseUp(object sender, MouseEventArgs e)
        {
            new_AdjustSetButton();
        }

        private void panel_AdjustSet_MouseClick(object sender, MouseEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 속도 슬라이더바 값 변경 이벤트
        /// </summary>
        private void Slider_Speed_ValueChanged(object sender, EventArgs e)
        {
            if (label_Value_Speed != null)
            {
                label_Value_Speed.Text = Slider_Speed.Value.ToString();
            }
        }

        /// <summary>
        /// 농도 슬라이더바 값 변경 이벤트
        /// </summary>
        private void Slider_darkness_ValueChanged(object sender, EventArgs e)
        {
            if (label_Value_Darkness != null)
            {
                label_Value_Darkness.Text = Slider_darkness.Value.ToString();
            }
        }
        
        /// <summary>
        /// DPI 스케일링 대응: 폼 크기와 위치를 명시적으로 고정
        /// </summary>
        private void FixFormSizeAndPosition()
        {
            try
            {
                // 폼 크기 명시적 고정
                this.Size = new Size(800, 700);
                this.ClientSize = new Size(800, 700);
                this.MaximumSize = new Size(800, 700);
                this.MinimumSize = new Size(800, 700);
                
                System.Diagnostics.Debug.WriteLine($"[AdjustForm DPI 대응] 폼 크기: {this.Size}, DPI: {this.DeviceDpi}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdjustForm DPI 대응 오류] {ex.Message}");
            }
        }

       
    }
}
