using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Data;
using System.IO;
using BarcodePrint.UserControls;

namespace BarcodePrint.View
{
    /// <summary>
    /// Excel 다중 컬럼 선택 폼 - 각 컬럼별로 바코드 사용 여부와 타입을 선택할 수 있습니다.
    /// </summary>
    public partial class ExcelMultiColumnSelectionForm : MaterialForm
    {
        private IXLWorksheet worksheet;
        private MaterialSkinManager materialSkinManager;
        
        // 컬럼 설정을 저장할 리스트
        private List<ColumnSetting> columnSettings = new List<ColumnSetting>();
        
        // 폼 드래그 관련 변수들
        private bool isDragging = false;
        private Point dragStartPoint;
        
        // 설정 파일 경로
        private string settingsFilePath = Path.Combine(Application.StartupPath, "ColumnSettings.xml");
        
        // UI 컨트롤들
        private Panel panelColumns;
        private Button btnOK;
        private Button btnCancel;
        private Label labelPreview;
        private TextBox textBoxPreview;
        private VScrollBar vScrollBar;
        private Label labelTitle;
        private BarcodePrint.UserControls.CustomCloseButton customCloseButton;
        private Panel panelTitleBar;

        // 컬럼 설정 클래스
        public class ColumnSetting
        {
            public int ColumnIndex { get; set; } // ColumnPositionSetting 매핑용 (0부터 시작)
            public int ExcelColumnNumber { get; set; } // 엑셀 컬럼 번호 (1부터 시작)
            public string ColumnName { get; set; }
            public bool IsEnabled { get; set; } // 컬럼 사용 여부
            public string UsageType { get; set; } = "텍스트"; // 텍스트 고정 (바코드 기능 제거)
            public string BarcodeType { get; set; } = "QR_CODE"; // 기본값 유지 (호환성)
            public int Size { get; set; } = 50; // 크기 (ColumnPositionSetting과 동기화)
            public int OffsetX { get; set; } = 0; // X 오프셋 (ColumnPositionSetting과 동기화)
            public int OffsetY { get; set; } = 0; // Y 오프셋 (ColumnPositionSetting과 동기화)
            public bool ShowBarcodeText { get; set; } = true; // 바코드 값 표시 여부 (호환성)
            public string Prefix { get; set; } = ""; // 접두사 (고정 문구 앞)
            public string Suffix { get; set; } = ""; // 접미사 (고정 문구 뒤)
            public bool UsePrefixNewLine { get; set; } = false; // 머리글 줄바꿈 적용 여부
            public bool UseSuffixNewLine { get; set; } = false; // 꼬리글 줄바꿈 적용 여부
            public object Tag { get; set; } // UI 컨트롤 참조를 위한 Tag 속성 추가
        }
        
        // 엑셀 전체 컬럼 리스트 저장
        private List<string> allExcelColumns = new List<string>();
        
        // 엑셀 시작 컬럼 번호 저장 (A, B, C가 비어있을 수 있음)
        private int firstColumnNumber = 1;
        
        // 선택된 컬럼 설정들을 반환
        public List<ColumnSetting> SelectedColumnSettings => columnSettings;
        
        // ColumnPositionSetting 리스트를 저장할 필드
        private List<BarcodePrint.View.ColumnPositionSetting> columnPositionSettings;
        
        // 이전 설정을 저장할 필드
        private List<ColumnSetting> initialSettings;
        
        public ExcelMultiColumnSelectionForm(IXLWorksheet worksheet, Form parentForm = null, List<BarcodePrint.View.ColumnPositionSetting> columnPositionSettings = null, List<ColumnSetting> initialSettings = null)
        {
            this.worksheet = worksheet;
            this.columnPositionSettings = columnPositionSettings;
            this.initialSettings = initialSettings;
            InitializeComponent();
            
            // 타이틀바 완전 제거를 위한 추가 설정
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = false;
            
            // MaterialSkinManager 초기화
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue500,
                Primary.Blue500,
                Primary.Blue500,
                Accent.Blue400,
                TextShade.WHITE
            );
            
            // 부모 폼이 지정된 경우 자식 폼으로 설정
            if (parentForm != null)
            {
                this.Owner = parentForm;
                this.TopMost = false; // TopMost를 false로 변경
            }
            
            // DPI 스케일링 대응: 폼 크기와 위치 고정
            FixFormSizeAndPosition();
            
            LoadColumns();
        }
        
        /// <summary>
        /// DPI 스케일링 대응: 폼 크기와 위치를 명시적으로 고정
        /// </summary>
        private void FixFormSizeAndPosition()
        {
            try
            {
                // 폼 크기 명시적 고정
                this.Size = new Size(646, 622);
                this.ClientSize = new Size(646, 622);
                this.MaximumSize = new Size(646, 622);
                this.MinimumSize = new Size(646, 622);
                
                System.Diagnostics.Debug.WriteLine($"[ExcelMultiColumnSelectionForm DPI 대응] 폼 크기: {this.Size}, DPI: {this.DeviceDpi}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExcelMultiColumnSelectionForm DPI 대응 오류] {ex.Message}");
            }
        }
        
        private void InitializeComponent()
        {
            this.panelColumns = new System.Windows.Forms.Panel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelPreview = new System.Windows.Forms.Label();
            this.textBoxPreview = new System.Windows.Forms.TextBox();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.labelTitle = new System.Windows.Forms.Label();
            this.customCloseButton = new BarcodePrint.UserControls.CustomCloseButton();
            this.panelTitleBar = new System.Windows.Forms.Panel();
            this.panelTitleBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelColumns
            // 
            this.panelColumns.AutoScroll = true;
            this.panelColumns.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelColumns.Location = new System.Drawing.Point(22, 80);
            this.panelColumns.Name = "panelColumns";
            this.panelColumns.Size = new System.Drawing.Size(600, 346);
            this.panelColumns.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(183, 539);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(140, 70);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "확인";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.Black;
            this.btnCancel.Location = new System.Drawing.Point(329, 539);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(140, 70);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // labelPreview
            // 
            this.labelPreview.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.labelPreview.Location = new System.Drawing.Point(19, 445);
            this.labelPreview.Name = "labelPreview";
            this.labelPreview.Size = new System.Drawing.Size(600, 25);
            this.labelPreview.TabIndex = 3;
            this.labelPreview.Text = "첫 번째 행 미리보기:";
            // 
            // textBoxPreview
            // 
            this.textBoxPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.textBoxPreview.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.textBoxPreview.Location = new System.Drawing.Point(22, 473);
            this.textBoxPreview.Multiline = true;
            this.textBoxPreview.Name = "textBoxPreview";
            this.textBoxPreview.ReadOnly = true;
            this.textBoxPreview.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxPreview.Size = new System.Drawing.Size(600, 60);
            this.textBoxPreview.TabIndex = 4;
            // 
            // vScrollBar
            // 
            this.vScrollBar.Location = new System.Drawing.Point(0, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(17, 80);
            this.vScrollBar.TabIndex = 0;
            // 
            // labelTitle
            // 
            this.labelTitle.Font = new System.Drawing.Font("맑은 고딕", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(15, 10);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(400, 40);
            this.labelTitle.TabIndex = 5;
            this.labelTitle.Text = "컬럼 설정";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // customCloseButton
            // 
            this.customCloseButton.BackColor = System.Drawing.Color.Transparent;
            this.customCloseButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.customCloseButton.Location = new System.Drawing.Point(607, 4);
            this.customCloseButton.Name = "customCloseButton";
            this.customCloseButton.Size = new System.Drawing.Size(34, 32);
            this.customCloseButton.TabIndex = 6;
            this.customCloseButton.TabStop = false;
            this.customCloseButton.Click += new System.EventHandler(this.CustomCloseButton_Click);
            // 
            // panelTitleBar
            // 
            this.panelTitleBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.panelTitleBar.Controls.Add(this.labelTitle);
            this.panelTitleBar.Controls.Add(this.customCloseButton);
            this.panelTitleBar.Location = new System.Drawing.Point(0, 0);
            this.panelTitleBar.Name = "panelTitleBar";
            this.panelTitleBar.Size = new System.Drawing.Size(646, 60);
            this.panelTitleBar.TabIndex = 7;
            this.panelTitleBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PanelTitleBar_MouseDown);
            this.panelTitleBar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PanelTitleBar_MouseMove);
            this.panelTitleBar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PanelTitleBar_MouseUp);
            // 
            // ExcelMultiColumnSelectionForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(646, 622);
            this.Controls.Add(this.panelTitleBar);
            this.Controls.Add(this.textBoxPreview);
            this.Controls.Add(this.labelPreview);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.panelColumns);
            this.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.MaximumSize = new System.Drawing.Size(646, 622);
            this.Name = "ExcelMultiColumnSelectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panelTitleBar.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        
        private void LoadColumns()
        {
            try
            {
                var range = worksheet.RangeUsed();
                if (range != null)
                {
                    // 실제 데이터가 있는 컬럼 범위 가져오기 (A, B, C가 비어있어도 D부터 올바르게 읽음)
                    this.firstColumnNumber = range.FirstCell().Address.ColumnNumber;
                    int lastColumnNumber = range.LastCell().Address.ColumnNumber;
                    int columnCount = lastColumnNumber - firstColumnNumber + 1;
                    
                    columnSettings.Clear();
                    allExcelColumns.Clear();
                    
                    System.Diagnostics.Debug.WriteLine($"[엑셀 범위] FirstColumn={firstColumnNumber} ('{(char)('A' + firstColumnNumber - 1)}'열), LastColumn={lastColumnNumber}, Count={columnCount}");
                    
                    // 엑셀의 모든 컬럼 헤더 정보 수집 (실제 시작 컬럼부터)
                    for (int col = firstColumnNumber; col <= lastColumnNumber; col++)
                    {
                        var headerCell = worksheet.Cell(1, col);
                        string headerText = headerCell.IsEmpty() ? $"컬럼 {col}" : headerCell.GetString();
                        
                        // 드롭다운에 표시할 때는 실제 엑셀 컬럼 번호와 함께 표시
                        char colLetter = (char)('A' + col - 1);
                        allExcelColumns.Add($"{col} ({colLetter}열): {headerText}");
                        System.Diagnostics.Debug.WriteLine($"[엑셀 컬럼 로드] 컬럼 {col} ({colLetter}열): '{headerText}'");
                    }
                    
                    // 기본은 A, B, C, D 순서로 매칭 (1, 2, 3, 4열)
                    int[] autoMapping = new int[4];
                    for (int slotIndex = 0; slotIndex < 4; slotIndex++)
                    {
                        autoMapping[slotIndex] = 1 + slotIndex; // A, B, C, D 순서로 매칭
                    }
                    
                    int yPosition = 15;
                    
                    // 고정 4개 컬럼 슬롯 생성 (엑셀 컬럼을 동적으로 선택 가능) - 명찰용
                    int maxColumns = 4;
                    
                    for (int slotIndex = 0; slotIndex < maxColumns; slotIndex++)
                    {
                        // ColumnPositionSetting에서 기존 설정 가져오기
                        var posSetting = columnPositionSettings?.FirstOrDefault(p => p.ColumnIndex == slotIndex);
                        
                        // initialSettings에서 기존 컬럼 설정(엑셀 매핑 포함) 가져오기
                        var initialSetting = initialSettings?.FirstOrDefault(s => s.ColumnIndex == slotIndex);
                        
                        // 초기 엑셀 컬럼 번호 결정 (기존 설정이 있으면 그것을 우선 사용, 없으면 자동 매핑)
                        int initialExcelCol;
                        if (initialSetting != null && initialSetting.ExcelColumnNumber >= firstColumnNumber && initialSetting.ExcelColumnNumber <= lastColumnNumber)
                        {
                            initialExcelCol = initialSetting.ExcelColumnNumber;
                            System.Diagnostics.Debug.WriteLine($"기존 매핑 유지: 슬롯 {slotIndex} → 엑셀 컬럼 {initialExcelCol}");
                        }
                        else
                        {
                            initialExcelCol = autoMapping[slotIndex];
                            if (initialExcelCol > lastColumnNumber)
                            {
                                initialExcelCol = firstColumnNumber; // 엑셀 컬럼이 부족하면 첫 번째 컬럼 사용
                            }
                            System.Diagnostics.Debug.WriteLine($"자동 매핑 사용: 슬롯 {slotIndex} → 엑셀 컬럼 {initialExcelCol}");
                        }
                        
                        // 초기 컬럼 컬럼3
                        var headerCell = worksheet.Cell(1, initialExcelCol);
                        string headerText = headerCell.IsEmpty() ? $"컬럼 {initialExcelCol}" : headerCell.GetString();
                        
                        // 컬럼 설정 생성 (ColumnPositionSetting이 있으면 그 값 사용)
                        var columnSetting = new ColumnSetting
                        {
                            ColumnIndex = slotIndex, // 슬롯 인덱스 (0부터 시작)
                            ExcelColumnNumber = initialExcelCol, // 초기 엑셀 컬럼 번호 (1부터 시작)
                            ColumnName = headerText,
                            IsEnabled = initialSetting?.IsEnabled ?? posSetting?.IsEnabled ?? true, // 모든 슬롯 기본 활성화
                            UsageType = posSetting?.UsageType ?? initialSetting?.UsageType ?? (slotIndex == 3 ? "바코드" : "텍스트"),
                            BarcodeType = posSetting?.BarcodeType ?? initialSetting?.BarcodeType ?? "QR_CODE", // 기존 저장된 바코드타입 유지
                            Size = posSetting?.Size ?? initialSetting?.Size ?? 50,
                            OffsetX = posSetting?.OffsetX ?? initialSetting?.OffsetX ?? 0,
                            OffsetY = posSetting?.OffsetY ?? initialSetting?.OffsetY ?? 0,
                            ShowBarcodeText = posSetting?.ShowBarcodeText ?? initialSetting?.ShowBarcodeText ?? true, // 기존 저장된 하단텍스트표시여부 유지
                            Prefix = initialSetting?.Prefix ?? "",
                            Suffix = initialSetting?.Suffix ?? "",
                            UsePrefixNewLine = initialSetting?.UsePrefixNewLine ?? false,
                            UseSuffixNewLine = initialSetting?.UseSuffixNewLine ?? false
                        };
                        columnSettings.Add(columnSetting);
                        
                        if (posSetting != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"ExcelMultiColumn 슬롯 {slotIndex} ColumnPositionSetting 적용: IsEnabled={posSetting.IsEnabled}, Type={posSetting.UsageType}, BarcodeType={posSetting.BarcodeType}, Size={posSetting.Size}");
                        }
                        
                        // 컬럼 UI 생성
                        CreateColumnUI(columnSetting, yPosition);
                        yPosition += 100; // GroupBox 높이 + 간격 (접두사/접미사 추가에 따른 간격 확장)
                    }
                    
                    // initialSettings가 전달되지 않았거나 비어있는 경우 파일(ColumnSettings.xml)에서 이전 설정(머리글, 꼬리글 등) 불러오기
                    if (initialSettings == null || initialSettings.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("ExcelMultiColumn: initialSettings 없음 - 파일(ColumnSettings.xml)에서 설정 로드");
                        LoadColumnSettings();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ExcelMultiColumn: initialSettings 설정이 전달됨 - 파일 로드 건너뛰기");
                    }
                    
                    // 모든 컬럼의 초기 UI 상태 업데이트
                    foreach (var setting in columnSettings)
                    {
                        UpdateColumnUI(setting);
                    }
                    
                    // 저장된 설정이 없으면 모든 컬럼을 기본으로 활성화
                    if (columnSettings.Count > 0 && !columnSettings.Any(s => s.IsEnabled))
                    {
                        foreach (var setting in columnSettings)
                        {
                            setting.IsEnabled = true;
                            UpdateColumnUI(setting);
                        }
                        System.Diagnostics.Debug.WriteLine("모든 컬럼 기본 활성화 완료");
                    }
                    
                    UpdatePreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"컬럼 정보를 불러오는 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void CreateColumnUI(ColumnSetting setting, int yPosition)
        {
            // 고정 컬럼명 정의 (가격표 프로그램용)
            // 순서: 컬럼1, 컬럼2, 컬럼3, 컬럼4
            string[] fixedColumnNames = { "컬럼1", "컬럼2", "컬럼3", "컬럼4" };
            string columnLabel = setting.ColumnIndex < fixedColumnNames.Length 
                ? fixedColumnNames[setting.ColumnIndex] 
                : $"컬럼 {setting.ColumnIndex + 1}";
            
            // 각 컬럼을 GroupBox로 그룹화 (높이 60 -> 95로 확장)
            var groupBox = new GroupBox
            {
                Text = $"{setting.ColumnIndex + 1}: {columnLabel}",
                Location = new Point(10, yPosition),
                Size = new Size(580, 95),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.DodgerBlue
            };
            panelColumns.Controls.Add(groupBox);
            
            // 컬럼 사용 여부 체크박스 (터치 친화적 크기) - 가장 왼쪽에 배치
            var checkBoxEnabled = new TouchCheckBox
            {
                CheckBoxText = "사용",
                Location = new Point(5, 15),
                Size = new Size(70, 30),
                Tag = setting
            };
            checkBoxEnabled.CheckedChanged += (s, e) => {
                setting.IsEnabled = checkBoxEnabled.Checked;
                UpdateColumnUI(setting);
                UpdatePreview();
            };
            groupBox.Controls.Add(checkBoxEnabled);
            
            // 엑셀 컬럼 선택 콤보박스 (터치 친화적 크기)
            var comboBoxColumnSelect = new ComboBox
            {
                Location = new Point(85, 18),
                Size = new Size(480, 28), // 가로폭 확대 (바코드 컨트롤 제거로 공간 확보)
                Font = new Font("맑은 고딕", 11F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Tag = setting
            };
            comboBoxColumnSelect.Items.AddRange(allExcelColumns.ToArray());
            
            // 현재 설정된 엑셀 컬럼 선택 (ExcelColumnNumber 기준)
            // ExcelColumnNumber는 실제 엑셀 컬럼 번호 (예: 4, 5, 6)
            // SelectedIndex는 0부터 시작 (예: 0, 1, 2)
            if (setting.ExcelColumnNumber >= firstColumnNumber && setting.ExcelColumnNumber <= firstColumnNumber + allExcelColumns.Count - 1)
            {
                comboBoxColumnSelect.SelectedIndex = setting.ExcelColumnNumber - firstColumnNumber;
            }
            else if (allExcelColumns.Count > 0)
            {
                comboBoxColumnSelect.SelectedIndex = 0; // 기본값: 첫 번째 컬럼
            }
            
            comboBoxColumnSelect.SelectedIndexChanged += (s, e) => {
                if (comboBoxColumnSelect.SelectedIndex >= 0)
                {
                    // SelectedIndex(0, 1, 2...)를 실제 엑셀 컬럼 번호로 변환
                    int selectedExcelCol = firstColumnNumber + comboBoxColumnSelect.SelectedIndex;
                    setting.ExcelColumnNumber = selectedExcelCol;
                    
                    // 선택된 엑셀 컬럼의 헤더 텍스트 가져오기
                    var headerCell = worksheet.Cell(1, selectedExcelCol);
                    setting.ColumnName = headerCell.IsEmpty() ? $"컬럼 {selectedExcelCol}" : headerCell.GetString();
                    
                    System.Diagnostics.Debug.WriteLine($"컬럼 슬롯 {setting.ColumnIndex} → 엑셀 컬럼 {selectedExcelCol} 선택: {setting.ColumnName}");
                    UpdatePreview();
                }
            };
            groupBox.Controls.Add(comboBoxColumnSelect);

            // 접두사 라벨 추가
            var labelPrefix = new Label
            {
                Text = "머리글:",
                Location = new Point(85, 58),
                Size = new Size(50, 20),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupBox.Controls.Add(labelPrefix);

            // 접두사 입력 텍스트박스 추가 (X축 및 너비 조절)
            var textBoxPrefix = new TextBox
            {
                Location = new Point(135, 56),
                Size = new Size(80, 23),
                Font = new Font("맑은 고딕", 9F),
                Tag = setting
            };
            textBoxPrefix.TextChanged += (s, e) => {
                setting.Prefix = textBoxPrefix.Text;
                UpdatePreview();
            };
            groupBox.Controls.Add(textBoxPrefix);

            // 머리글 줄바꿈 체크박스 추가 (너비 95로 확장하여 줄바꿈 텍스트 1줄 출력 보장)
            var checkBoxPrefixNewLine = new TouchCheckBox
            {
                CheckBoxText = "줄바꿈",
                Location = new Point(220, 52),
                Size = new Size(95, 30),
                Tag = setting
            };
            checkBoxPrefixNewLine.CheckedChanged += (s, e) => {
                setting.UsePrefixNewLine = checkBoxPrefixNewLine.Checked;
                UpdatePreview();
            };
            groupBox.Controls.Add(checkBoxPrefixNewLine);

            // 접미사 라벨 추가 (X축 조절)
            var labelSuffix = new Label
            {
                Text = "꼬리글:",
                Location = new Point(325, 58),
                Size = new Size(50, 20),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupBox.Controls.Add(labelSuffix);

            // 접미사 입력 텍스트박스 추가 (X축 및 너비 조절)
            var textBoxSuffix = new TextBox
            {
                Location = new Point(375, 56),
                Size = new Size(80, 23),
                Font = new Font("맑은 고딕", 9F),
                Tag = setting
            };
            textBoxSuffix.TextChanged += (s, e) => {
                setting.Suffix = textBoxSuffix.Text;
                UpdatePreview();
            };
            groupBox.Controls.Add(textBoxSuffix);

            // 꼬리글 줄바꿈 체크박스 추가 (너비 95로 확장하여 줄바꿈 텍스트 1줄 출력 보장)
            var checkBoxSuffixNewLine = new TouchCheckBox
            {
                CheckBoxText = "줄바꿈",
                Location = new Point(460, 52),
                Size = new Size(95, 30),
                Tag = setting
            };
            checkBoxSuffixNewLine.CheckedChanged += (s, e) => {
                setting.UseSuffixNewLine = checkBoxSuffixNewLine.Checked;
                UpdatePreview();
            };
            groupBox.Controls.Add(checkBoxSuffixNewLine);
            
            // 설정 객체에 UI 컨트롤 참조 저장
            setting.Tag = new { checkBoxEnabled, comboBoxColumnSelect, textBoxPrefix, textBoxSuffix, checkBoxPrefixNewLine, checkBoxSuffixNewLine };
            
            // 초기 UI 상태 업데이트
            UpdateColumnUI(setting);
        }
        
        private void UpdateColumnUI(ColumnSetting setting)
        {
            if (setting.Tag != null)
            {
                var ui = setting.Tag as dynamic;
                if (ui != null)
                {
                    // 체크박스 체크 상태 업데이트
                    ui.checkBoxEnabled.Checked = setting.IsEnabled;
                    
                    // 엑셀 컬럼 선택 콤보박스 활성화/비활성화
                    ui.comboBoxColumnSelect.Enabled = setting.IsEnabled;
                    
                    // 접두사/접미사 텍스트박스 활성화/비활성화 및 텍스트 갱신
                    try
                    {
                        ui.textBoxPrefix.Enabled = setting.IsEnabled;
                        ui.textBoxSuffix.Enabled = setting.IsEnabled;
                        ui.checkBoxPrefixNewLine.Enabled = setting.IsEnabled;
                        ui.checkBoxSuffixNewLine.Enabled = setting.IsEnabled;
                        
                        // 이벤트 루프 방지를 위해 값이 다를 때만 업데이트
                        if (ui.textBoxPrefix.Text != (setting.Prefix ?? ""))
                        {
                            ui.textBoxPrefix.Text = setting.Prefix ?? "";
                        }
                        if (ui.textBoxSuffix.Text != (setting.Suffix ?? ""))
                        {
                            ui.textBoxSuffix.Text = setting.Suffix ?? "";
                        }
                        if (ui.checkBoxPrefixNewLine.Checked != setting.UsePrefixNewLine)
                        {
                            ui.checkBoxPrefixNewLine.Checked = setting.UsePrefixNewLine;
                        }
                        if (ui.checkBoxSuffixNewLine.Checked != setting.UseSuffixNewLine)
                        {
                            ui.checkBoxSuffixNewLine.Checked = setting.UseSuffixNewLine;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UpdateColumnUI 컨트롤 바인딩 오류: {ex.Message}");
                    }
                }
            }
        }
        
        private void UpdatePreview()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 미리보기 업데이트 시작 ===");
                
                var selectedSettings = columnSettings.Where(c => c.IsEnabled).ToList();
                System.Diagnostics.Debug.WriteLine($"활성화된 컬럼 수: {selectedSettings.Count}");
                
                if (selectedSettings.Count == 0)
                {
                    textBoxPreview.Text = "사용할 컬럼을 선택해주세요.";
                    return;
                }
                
                string previewText = "[ 첫 번째 행 미리보기 ]\n\n";
                
                var orderedSettings = selectedSettings.OrderBy(s => s.ColumnIndex).ToList();
                
                // 고정 컬럼명 정의 (가격표 프로그램용)
                // 순서: 컬럼1, 컬럼2, 컬럼3, 컬럼4
                string[] fixedColumnNames = { "컬럼1", "컬럼2", "컬럼3", "컬럼4" };
                
                foreach (var setting in orderedSettings)
                {
                    // 고정 컬럼명 사용
                    string columnLabel = setting.ColumnIndex < fixedColumnNames.Length 
                        ? fixedColumnNames[setting.ColumnIndex] 
                        : $"컬럼 {setting.ColumnIndex + 1}";
                    
                    // 첫 번째 데이터 행 (2행)에서 값 가져오기
                    var cell = worksheet.Cell(2, setting.ExcelColumnNumber);
                    string cellValue = cell.IsEmpty() ? "(빈 값)" : cell.GetString();
                    
                    // 접두사/접미사 적용 (각각의 줄바꿈 구분자 반영)
                    string prefixSeparator = setting.UsePrefixNewLine ? "\n" : " ";
                    string suffixSeparator = setting.UseSuffixNewLine ? "\n" : " ";
                    string combinedValue = "";
                    if (!string.IsNullOrWhiteSpace(setting.Prefix))
                    {
                        combinedValue += setting.Prefix.Trim() + prefixSeparator;
                    }
                    combinedValue += cellValue;
                    if (!string.IsNullOrWhiteSpace(setting.Suffix))
                    {
                        combinedValue += suffixSeparator + setting.Suffix.Trim();
                    }
                    
                    // 간결한 형식으로 표시 (텍스트만 사용)
                    previewText += $"• {columnLabel}: {combinedValue}\n\n";
                }
                
                textBoxPreview.Text = previewText.TrimEnd();
            }
            catch (Exception ex)
            {
                textBoxPreview.Text = $"미리보기 업데이트 중 오류: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"미리보기 업데이트 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            var selectedSettings = columnSettings.Where(c => c.IsEnabled).ToList();
            
            if (selectedSettings.Count == 0)
            {
                MessageBox.Show("최소 하나의 컬럼을 선택해주세요.", "알림", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            SaveColumnSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        private void CustomCloseButton_Click(object sender, EventArgs e)
        {
            SaveColumnSettings();
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        // 컬럼 설정 저장
        private void SaveColumnSettings()
        {
            try
            {
                var lines = new List<string>();
                foreach (var setting in columnSettings)
                {
                    // 텍스트만 사용하므로 UsageType, BarcodeType은 고정값으로 저장, 뒤에 Prefix|Suffix|UsePrefixNewLine|UseSuffixNewLine 추가
                    lines.Add($"{setting.ColumnIndex}|{setting.ColumnName}|{setting.IsEnabled}|{setting.UsageType ?? "텍스트"}|{setting.BarcodeType ?? "QR_CODE"}|{setting.Prefix ?? ""}|{setting.Suffix ?? ""}|{setting.UsePrefixNewLine}|{setting.UseSuffixNewLine}");
                }
                System.IO.File.WriteAllLines(settingsFilePath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 저장 오류: {ex.Message}");
            }
        }
        
        // 컬럼 설정 불러오기
        private void LoadColumnSettings()
        {
            try
            {
                if (System.IO.File.Exists(settingsFilePath))
                {
                    var lines = System.IO.File.ReadAllLines(settingsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 3)
                        {
                            int columnIndex = int.Parse(parts[0]);
                            var existingSetting = columnSettings.FirstOrDefault(s => s.ColumnIndex == columnIndex);
                            if (existingSetting != null)
                            {
                                existingSetting.IsEnabled = bool.Parse(parts[2]);
                                existingSetting.UsageType = parts.Length >= 4 ? parts[3] : (existingSetting.ColumnIndex == 3 ? "바코드" : "텍스트");
                                existingSetting.BarcodeType = parts.Length >= 5 ? parts[4] : "QR_CODE";
                                
                                // 하위 호환성 유지: 저장된 데이터에 Prefix, Suffix가 있으면 불러옴
                                if (parts.Length >= 7)
                                {
                                    existingSetting.Prefix = parts[5];
                                    existingSetting.Suffix = parts[6];
                                }
                                else
                                {
                                    existingSetting.Prefix = "";
                                    existingSetting.Suffix = "";
                                }

                                // UsePrefixNewLine / UseSuffixNewLine 불러오기 (9개 이상일 때)
                                if (parts.Length >= 9)
                                {
                                    existingSetting.UsePrefixNewLine = bool.Parse(parts[7]);
                                    existingSetting.UseSuffixNewLine = bool.Parse(parts[8]);
                                }
                                else if (parts.Length >= 8)
                                {
                                    // 기존 8개 형식 하위 호환성 유지
                                    bool oldUseNewLine = bool.Parse(parts[7]);
                                    existingSetting.UsePrefixNewLine = oldUseNewLine;
                                    existingSetting.UseSuffixNewLine = oldUseNewLine;
                                }
                                else
                                {
                                    existingSetting.UsePrefixNewLine = false; // 기본값
                                    existingSetting.UseSuffixNewLine = false; // 기본값
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 불러오기 오류: {ex.Message}");
            }
        }
        
        // 타이틀바 패널 마우스 이벤트 핸들러들
        private void PanelTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = new Point(e.X, e.Y);
                this.Cursor = Cursors.SizeAll;
            }
        }
        
        private void PanelTitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.Location;
                newLocation.X += e.X - dragStartPoint.X;
                newLocation.Y += e.Y - dragStartPoint.Y;
                this.Location = newLocation;
            }
        }
        
        private void PanelTitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                this.Cursor = Cursors.Default;
            }
        }
    }
}
