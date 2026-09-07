using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarcodePrint.UserControls;
using BarcodePrint.Model;

namespace BarcodePrint.View
{
    /// <summary>
    /// 위치 조절 폼 - 슬라이더바를 통한 크기 및 위치 조절
    /// </summary>
    public class PositionAdjustmentForm : Form
    {
        private MainForm mainForm;
        private Panel panelTitleBar;
        private Label labelTitle;
        private CustomCloseButton customCloseButton;
        private Panel panelSliders;
        private Panel panelSaveButton;
        
        // 슬라이더바들
        private TouchSlider touchSlider_Size0;
        private TouchSlider touchSlider_Size1;
        private TouchSlider touchSlider_Size2;
        private TouchSlider touchSlider_Size3;
        private TouchSlider touchSlider_OffsetY0;
        private TouchSlider touchSlider_OffsetY1;
        private TouchSlider touchSlider_OffsetY2;
        private TouchSlider touchSlider_OffsetY3;
        private TouchSlider touchSlider_OffsetX3;
        private TouchSlider touchSlider_TitleOffsetX;
        private TouchSlider touchSlider_TitleSize;
        private TouchSlider touchSlider_TitleOffsetY;
        
        // 라벨들
        private Label label_Column0_Size;
        private Label label_Column1_Size;
        private Label label_Column2_Size;
        private Label label_Column3_Size;
        private Label label_Column0_OffsetY;
        private Label label_Column1_OffsetY;
        private Label label_Column2_OffsetY;
        private Label label_Column3_OffsetY;
        private Label label_Column3_OffsetX;
        private Label label_TitleSize;
        private Label label_TitleOffsetY;
        
        // 값 표시 라벨들
        private Label label_Value_TitleSize;
        private Label label_Value_TitleOffsetY;
        private Label label_Value_Size0;
        private Label label_Value_Size1;
        private Label label_Value_Size2;
        private Label label_Value_Size3;
        private Label label_Value_OffsetY0;
        private Label label_Value_OffsetY1;
        private Label label_Value_OffsetY2;
        private Label label_Value_OffsetY3;
        private Label label_Value_OffsetX3;
        private Label label_Value_TitleOffsetX;

        // 타입 및 바코드 설정 컨트롤
        private ComboBox[] combo_Usage = new ComboBox[4];
        private ComboBox[] combo_BarcodeType = new ComboBox[4];
        private CheckBox[] check_ShowText = new CheckBox[4];
        private TouchSlider[] touchSlider_BarcodeHeight = new TouchSlider[4];
        private Label[] label_SizeText = new Label[4];
        private Label[] label_Value_BarcodeHeight = new Label[4];
        
        // 폼 드래그 관련 변수들
        private bool isDragging = false;
        private Point dragStartPoint;
        
        public PositionAdjustmentForm(MainForm mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponent();
            InitializeSliders();
            // 슬라이더 초기화 후 현재 용지 타입에 맞게 Maximum 설정
            UpdateSliderMaximums();
            LoadCurrentValues();
            // DPI 스케일링 대응: 폼 크기와 위치 고정
            FixFormSizeAndPosition();
        }
        
        /// <summary>
        /// DPI 스케일링 대응: 폼 크기와 위치를 명시적으로 고정
        /// </summary>
        private void FixFormSizeAndPosition()
        {
            try
            {
                // 폼 크기 명시적 고정
                this.Size = new Size(400, 600);
                this.ClientSize = new Size(400, 600);
                this.MaximumSize = new Size(400, 600);
                this.MinimumSize = new Size(400, 600);
                
                System.Diagnostics.Debug.WriteLine($"[PositionAdjustmentForm DPI 대응] 폼 크기: {this.Size}, DPI: {this.DeviceDpi}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PositionAdjustmentForm DPI 대응 오류] {ex.Message}");
            }
        }
        
        private void InitializeComponent()
        {
            this.panelTitleBar = new Panel();
            this.labelTitle = new Label();
            this.customCloseButton = new CustomCloseButton();
            this.panelSliders = new Panel();
            
            this.SuspendLayout();
            
            // 
            // panelTitleBar
            // 
            this.panelTitleBar.BackColor = Color.FromArgb(0, 120, 215);
            this.panelTitleBar.Controls.Add(this.labelTitle);
            this.panelTitleBar.Controls.Add(this.customCloseButton);
            this.panelTitleBar.Location = new Point(0, 0);
            this.panelTitleBar.Name = "panelTitleBar";
            this.panelTitleBar.Size = new Size(400, 60);
            this.panelTitleBar.TabIndex = 0;
            this.panelTitleBar.MouseDown += PanelTitleBar_MouseDown;
            this.panelTitleBar.MouseMove += PanelTitleBar_MouseMove;
            this.panelTitleBar.MouseUp += PanelTitleBar_MouseUp;
            
            // 
            // labelTitle
            // 
            this.labelTitle.Font = new Font("맑은 고딕", 26.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(129)));
            this.labelTitle.ForeColor = Color.White;
            this.labelTitle.Location = new Point(15, 10);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new Size(350, 40);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "위치 조절";
            this.labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.labelTitle.Cursor = Cursors.SizeAll; // 드래그 커서 표시
            // labelTitle에도 드래그 이벤트 연결 (타이틀바 드래그 가능하도록)
            this.labelTitle.MouseDown += LabelTitle_MouseDown;
            this.labelTitle.MouseMove += LabelTitle_MouseMove;
            this.labelTitle.MouseUp += LabelTitle_MouseUp;
            
            // 
            // customCloseButton
            // 
            this.customCloseButton.BackColor = Color.Transparent;
            this.customCloseButton.Cursor = Cursors.Hand;
            this.customCloseButton.Location = new Point(365, 4);
            this.customCloseButton.Name = "customCloseButton";
            this.customCloseButton.Size = new Size(34, 32);
            this.customCloseButton.TabIndex = 1;
            this.customCloseButton.TabStop = false;
            this.customCloseButton.Click += CustomCloseButton_Click;
            
            // 
            // panelSliders
            // 
            this.panelSliders.AutoScroll = true;
            this.panelSliders.BorderStyle = BorderStyle.FixedSingle;
            this.panelSliders.Location = new Point(10, 70);
            this.panelSliders.Name = "panelSliders";
            this.panelSliders.Size = new Size(380, 470); // 높이 조정: 570 → 470 (600 - 60 - 45 - 25)
            this.panelSliders.TabIndex = 1;
            
            // 
            // panelSaveButton
            // 
            this.panelSaveButton = new Panel();
            this.panelSaveButton.BackColor = Color.FromArgb(0, 120, 215);
            this.panelSaveButton.Cursor = Cursors.Hand;
            this.panelSaveButton.Location = new Point(10, 550); // 위치 조정: 650 → 550 (60 + 470 + 20)
            this.panelSaveButton.Name = "panelSaveButton";
            this.panelSaveButton.Size = new Size(380, 45);
            this.panelSaveButton.TabIndex = 2;
            this.panelSaveButton.Click += PanelSaveButton_Click;
            this.panelSaveButton.Paint += PanelSaveButton_Paint;
            this.panelSaveButton.MouseDown += PanelSaveButton_MouseDown;
            this.panelSaveButton.MouseUp += PanelSaveButton_MouseUp;
            this.panelSaveButton.MouseEnter += PanelSaveButton_MouseEnter;
            this.panelSaveButton.MouseLeave += PanelSaveButton_MouseLeave;
            
            // 
            // PositionAdjustmentForm
            // 
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(400, 600); // MainForm과 동일한 높이
            this.Controls.Add(this.panelSaveButton);
            this.Controls.Add(this.panelSliders);
            this.Controls.Add(this.panelTitleBar);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximumSize = new Size(400, 600); // MainForm과 동일한 높이
            this.MinimumSize = new Size(400, 600); // MainForm과 동일한 높이
            this.Name = "PositionAdjustmentForm";
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "위치 조절";
            this.TopMost = true; // 항상 최상단에 표시
            
            // 전체 폼에 드래그 이벤트 추가 (드래그 영역 확대)
            this.MouseDown += PositionAdjustmentForm_MouseDown;
            this.MouseMove += PositionAdjustmentForm_MouseMove;
            this.MouseUp += PositionAdjustmentForm_MouseUp;
            
            this.panelTitleBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        
        private void InitializeSliders()
        {
            int yPosition = 15;
            int sliderWidth = 200;
            int sliderHeight = 30;
            int labelWidth = 100;
            int rowHeight = 35; // 같은 컬럼 내 슬라이더 간격 (40 → 35로 더 줄임)
            int groupSpacing = 10; // 컬럼 그룹 사이 간격 (15 → 10으로 줄임)
            int valueLabelWidth = 50; // 값 표시 라벨 너비
            
            // Title 그룹
            Label titleGroupLabel = new Label
            {
                Text = "Title",
                Location = new Point(10, yPosition),
                Size = new Size(labelWidth, 20),
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            panelSliders.Controls.Add(titleGroupLabel);
            yPosition += 25;
            
            // Title 크기
            label_TitleSize = new Label
            {
                Text = "크기",
                Location = new Point(20, yPosition),
                Size = new Size(labelWidth - 10, 25),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
            };
            panelSliders.Controls.Add(label_TitleSize);
            
            // Title 크기 값 표시 (슬라이더 왼쪽)
            label_Value_TitleSize = new Label
            {
                Text = "10",
                Location = new Point(110, yPosition),
                Size = new Size(valueLabelWidth, 30),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelSliders.Controls.Add(label_Value_TitleSize);
            
            touchSlider_TitleSize = new TouchSlider
            {
                Location = new Point(165, yPosition),
                Size = new Size(sliderWidth, sliderHeight),
                Minimum = 8,
                Maximum = 24,
                Value = 10
            };
            touchSlider_TitleSize.ValueChanged += (s, e) => 
            {
                OnSliderValueChanged("TitleSize", touchSlider_TitleSize.Value);
                UpdateValueLabel(label_Value_TitleSize, touchSlider_TitleSize.Value);
            };
            panelSliders.Controls.Add(touchSlider_TitleSize);
            yPosition += rowHeight;
            
            // Title 위치
            label_TitleOffsetY = new Label
            {
                Text = "위치(Y)",
                Location = new Point(20, yPosition),
                Size = new Size(labelWidth - 10, 25),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
            };
            panelSliders.Controls.Add(label_TitleOffsetY);
            
            // Title 위치 값 표시 (슬라이더 왼쪽)
            label_Value_TitleOffsetY = new Label
            {
                Text = "10",
                Location = new Point(110, yPosition),
                Size = new Size(valueLabelWidth, 30),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelSliders.Controls.Add(label_Value_TitleOffsetY);
            
            touchSlider_TitleOffsetY = new TouchSlider
            {
                Location = new Point(165, yPosition),
                Size = new Size(sliderWidth, sliderHeight),
                Minimum = 0,
                Maximum = 145,
                Value = 10
            };
            touchSlider_TitleOffsetY.ValueChanged += (s, e) => 
            {
                OnSliderValueChanged("TitleOffsetY", touchSlider_TitleOffsetY.Value);
                UpdateValueLabel(label_Value_TitleOffsetY, touchSlider_TitleOffsetY.Value);
            };
            panelSliders.Controls.Add(touchSlider_TitleOffsetY);
            yPosition += rowHeight;

            // Title 위치(X)
            Label labelTitleOffsetX = new Label
            {
                Text = "위치(X)",
                Location = new Point(20, yPosition),
                Size = new Size(labelWidth - 10, 25),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
            };
            panelSliders.Controls.Add(labelTitleOffsetX);

            label_Value_TitleOffsetX = new Label
            {
                Text = "0",
                Location = new Point(110, yPosition),
                Size = new Size(valueLabelWidth, 30),
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                TextAlign = ContentAlignment.MiddleRight
            };
            panelSliders.Controls.Add(label_Value_TitleOffsetX);
            
            touchSlider_TitleOffsetX = new TouchSlider
            {
                Location = new Point(165, yPosition),
                Size = new Size(sliderWidth, sliderHeight),
                Minimum = -100,
                Maximum = 100,
                Value = 0
            };
            touchSlider_TitleOffsetX.ValueChanged += (s, e) => 
            {
                OnSliderValueChanged("TitleOffsetX", touchSlider_TitleOffsetX.Value);
                UpdateValueLabel(label_Value_TitleOffsetX, touchSlider_TitleOffsetX.Value);
            };
            panelSliders.Controls.Add(touchSlider_TitleOffsetX);

            yPosition += rowHeight + groupSpacing;
            
            string[] columnNames = { "컬럼1", "컬럼2", "컬럼3", "컬럼4" };
            
            for (int i = 0; i < 4; i++)
            {
                // 컬럼 그룹 헤더
                Label columnGroupLabel = new Label
                {
                    Text = columnNames[i],
                    Location = new Point(10, yPosition),
                    Size = new Size(labelWidth, 20),
                    Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 120, 215)
                };
                panelSliders.Controls.Add(columnGroupLabel);
                yPosition += 25;
                
                // 크기 슬라이더
                Label sizeLabel = new Label
                {
                    Text = "크기",
                    Location = new Point(20, yPosition),
                    Size = new Size(labelWidth - 10, 25),
                    Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                };
                panelSliders.Controls.Add(sizeLabel);
                
                int columnIndex = i;
                
                // 크기 값 표시 라벨 먼저 생성 (슬라이더 왼쪽)
                Label valueLabel_Size = new Label
                {
                    Text = (i == 3 ? 25 : 12).ToString(),
                    Location = new Point(110, yPosition),
                    Size = new Size(valueLabelWidth, 30),
                    Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                    ForeColor = Color.DarkGray,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panelSliders.Controls.Add(valueLabel_Size);
                
                TouchSlider sizeSlider = new TouchSlider
                {
                    Location = new Point(165, yPosition),
                    Size = new Size(sliderWidth, sliderHeight),
                    Minimum = i == 3 ? 15 : 8,
                    Maximum = i == 3 ? 50 : 24,
                    Value = i == 3 ? 25 : 12
                };
                
                switch (i)
                {
                    case 0: touchSlider_Size0 = sizeSlider; label_Column0_Size = sizeLabel; label_Value_Size0 = valueLabel_Size; break;
                    case 1: touchSlider_Size1 = sizeSlider; label_Column1_Size = sizeLabel; label_Value_Size1 = valueLabel_Size; break;
                    case 2: touchSlider_Size2 = sizeSlider; label_Column2_Size = sizeLabel; label_Value_Size2 = valueLabel_Size; break;
                    case 3: touchSlider_Size3 = sizeSlider; label_Column3_Size = sizeLabel; label_Value_Size3 = valueLabel_Size; break;
                }
                
                // 이벤트 핸들러는 라벨 생성 후 연결
                sizeSlider.ValueChanged += (s, e) => 
                {
                    OnSliderValueChanged($"Size{columnIndex}", sizeSlider.Value);
                    UpdateValueLabel(valueLabel_Size, sizeSlider.Value);
                };
                panelSliders.Controls.Add(sizeSlider);
                label_SizeText[i] = sizeLabel;
                
                yPosition += rowHeight;

                // --- [수정] 마지막 컬럼(바코드)에 대해서만 타입 전환 기능 제공 ---
                if (i == 3)
                {
                    Label usageLabel = new Label
                    {
                        Text = "타입",
                        Location = new Point(20, yPosition),
                        Size = new Size(labelWidth - 10, 25),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    panelSliders.Controls.Add(usageLabel);

                    ComboBox cboUsage = new ComboBox
                    {
                        Location = new Point(165, yPosition),
                        Size = new Size(sliderWidth, 28), // 높이 약간 증가
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                        BackColor = Color.White
                    };
                    cboUsage.Items.AddRange(new string[] { "텍스트", "바코드" });
                    cboUsage.SelectedIndexChanged += (s, e) => OnUsageTypeChanged(columnIndex);
                    combo_Usage[i] = cboUsage;
                    panelSliders.Controls.Add(cboUsage);
                    yPosition += rowHeight;

                    // 바코드 타입 선택
                    Label bTypeLabel = new Label
                    {
                        Text = "바코드 종류",
                        Location = new Point(20, yPosition),
                        Size = new Size(labelWidth - 10, 25),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    panelSliders.Controls.Add(bTypeLabel);

                    ComboBox cboBType = new ComboBox
                    {
                        Location = new Point(165, yPosition),
                        Size = new Size(sliderWidth, 25),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    cboBType.Items.AddRange(new string[] { "QR_CODE", "CODE_128", "EAN_13" });
                    combo_BarcodeType[i] = cboBType;
                    panelSliders.Controls.Add(cboBType);
                    yPosition += rowHeight;

                    // 바코드 높이 슬라이더
                    Label bHeightLabel = new Label
                    {
                        Text = "바코드 높이",
                        Location = new Point(20, yPosition),
                        Size = new Size(labelWidth - 10, 25),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    panelSliders.Controls.Add(bHeightLabel);

                    Label valueLabel_BHeight = new Label
                    {
                        Text = "15",
                        Location = new Point(110, yPosition),
                        Size = new Size(valueLabelWidth, 30),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                        ForeColor = Color.DarkGray,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    label_Value_BarcodeHeight[i] = valueLabel_BHeight;
                    panelSliders.Controls.Add(valueLabel_BHeight);

                    TouchSlider bHeightSlider = new TouchSlider
                    {
                        Location = new Point(165, yPosition),
                        Size = new Size(sliderWidth, sliderHeight),
                        Minimum = 5,
                        Maximum = 100,
                        Value = 15
                    };
                    bHeightSlider.ValueChanged += (s, e) => {
                        OnSliderValueChanged($"BarcodeHeight{columnIndex}", bHeightSlider.Value);
                        UpdateValueLabel(valueLabel_BHeight, bHeightSlider.Value);
                    };
                    touchSlider_BarcodeHeight[i] = bHeightSlider;
                    panelSliders.Controls.Add(bHeightSlider);
                    yPosition += rowHeight;

                    // 바코드 값 표시 여부
                    CheckBox chkShowText = new CheckBox
                    {
                        Text = "바코드 값 표시",
                        Location = new Point(165, yPosition),
                        Size = new Size(sliderWidth, 25),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    check_ShowText[i] = chkShowText;
                    panelSliders.Controls.Add(chkShowText);
                    yPosition += rowHeight;
                }
                else
                {
                    // 일반 컬럼은 "텍스트"로 고정
                    combo_Usage[i] = null;
                    combo_BarcodeType[i] = null;
                    check_ShowText[i] = null;
                    touchSlider_BarcodeHeight[i] = null;
                }
                // -----------------------------------------------------------

                // 위치 슬라이더 (Y)
                Label offsetYLabel = new Label
                {
                    Text = "위치(Y)",
                    Location = new Point(20, yPosition),
                    Size = new Size(labelWidth - 10, 25),
                    Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                };
                panelSliders.Controls.Add(offsetYLabel);
                
                // 위치(Y) 값 표시 라벨 먼저 생성 (슬라이더 왼쪽)
                int offsetYValue = i == 0 ? 34 : i == 1 ? 50 : i == 2 ? 69 : 110;
                Label valueLabel_OffsetY = new Label
                {
                    Text = offsetYValue.ToString(),
                    Location = new Point(110, yPosition),
                    Size = new Size(valueLabelWidth, 30),
                    Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                    ForeColor = Color.DarkGray,
                    TextAlign = ContentAlignment.MiddleRight
                };
                panelSliders.Controls.Add(valueLabel_OffsetY);
                
                TouchSlider offsetYSlider = new TouchSlider
                {
                    Location = new Point(165, yPosition),
                    Size = new Size(sliderWidth, sliderHeight),
                    Minimum = 0,
                    Maximum = 145,
                    Value = i == 0 ? 34 : i == 1 ? 50 : i == 2 ? 69 : 110
                };
                
                switch (i)
                {
                    case 0: touchSlider_OffsetY0 = offsetYSlider; label_Column0_OffsetY = offsetYLabel; label_Value_OffsetY0 = valueLabel_OffsetY; break;
                    case 1: touchSlider_OffsetY1 = offsetYSlider; label_Column1_OffsetY = offsetYLabel; label_Value_OffsetY1 = valueLabel_OffsetY; break;
                    case 2: touchSlider_OffsetY2 = offsetYSlider; label_Column2_OffsetY = offsetYLabel; label_Value_OffsetY2 = valueLabel_OffsetY; break;
                    case 3: touchSlider_OffsetY3 = offsetYSlider; label_Column3_OffsetY = offsetYLabel; label_Value_OffsetY3 = valueLabel_OffsetY; break;
                }
                
                // 이벤트 핸들러는 라벨 생성 후 연결
                offsetYSlider.ValueChanged += (s, e) => 
                {
                    OnSliderValueChanged($"OffsetY{columnIndex}", offsetYSlider.Value);
                    UpdateValueLabel(valueLabel_OffsetY, offsetYSlider.Value);
                };
                panelSliders.Controls.Add(offsetYSlider);
                
                yPosition += rowHeight;
                
                // 바코드 X축 슬라이더 (바코드만)
                if (i == 3)
                {
                    label_Column3_OffsetX = new Label
                    {
                        Text = "위치(X)",
                        Location = new Point(20, yPosition),
                        Size = new Size(labelWidth - 10, 25),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular)
                    };
                    panelSliders.Controls.Add(label_Column3_OffsetX);
                    
                    // 위치(X) 값 표시 (슬라이더 왼쪽)
                    label_Value_OffsetX3 = new Label
                    {
                        Text = "0",
                        Location = new Point(110, yPosition),
                        Size = new Size(valueLabelWidth, 30),
                        Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                        ForeColor = Color.DarkGray,
                        TextAlign = ContentAlignment.MiddleRight
                    };
                    panelSliders.Controls.Add(label_Value_OffsetX3);
                    
                    touchSlider_OffsetX3 = new TouchSlider
                    {
                        Location = new Point(165, yPosition),
                        Size = new Size(sliderWidth, sliderHeight),
                        Minimum = -50,
                        Maximum = 50,
                        Value = 0
                    };
                    touchSlider_OffsetX3.ValueChanged += (s, e) => 
                    {
                        OnSliderValueChanged("OffsetX3", touchSlider_OffsetX3.Value);
                        UpdateValueLabel(label_Value_OffsetX3, touchSlider_OffsetX3.Value);
                    };
                    panelSliders.Controls.Add(touchSlider_OffsetX3);
                    
                    yPosition += rowHeight;
                }
                
                // 컬럼 그룹 사이 간격 (마지막 컬럼 제외)
                if (i < 3)
                {
                    yPosition += groupSpacing;
                }
            }
        }
        
        /// <summary>
        /// 현재 용지 타입에 맞게 슬라이더 Maximum 값 업데이트
        /// </summary>
        private void UpdateSliderMaximums()
        {
            try
            {
                // 현재 용지 크기 가져오기
                int paperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
                int paperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                
                // Y축 위치 슬라이더의 Maximum은 용지 높이로 설정
                if (touchSlider_TitleOffsetY != null)
                {
                    int currentValue = touchSlider_TitleOffsetY.Value;
                    touchSlider_TitleOffsetY.Maximum = paperHeight;
                    // 현재 값이 새로운 Maximum보다 크면 조정
                    if (currentValue > paperHeight)
                        touchSlider_TitleOffsetY.Value = paperHeight;
                }
                
                if (touchSlider_OffsetY0 != null)
                {
                    int currentValue = touchSlider_OffsetY0.Value;
                    touchSlider_OffsetY0.Maximum = paperHeight;
                    if (currentValue > paperHeight)
                        touchSlider_OffsetY0.Value = paperHeight;
                }
                
                if (touchSlider_OffsetY1 != null)
                {
                    int currentValue = touchSlider_OffsetY1.Value;
                    touchSlider_OffsetY1.Maximum = paperHeight;
                    if (currentValue > paperHeight)
                        touchSlider_OffsetY1.Value = paperHeight;
                }
                
                if (touchSlider_OffsetY2 != null)
                {
                    int currentValue = touchSlider_OffsetY2.Value;
                    touchSlider_OffsetY2.Maximum = paperHeight;
                    if (currentValue > paperHeight)
                        touchSlider_OffsetY2.Value = paperHeight;
                }
                
                if (touchSlider_OffsetY3 != null)
                {
                    int currentValue = touchSlider_OffsetY3.Value;
                    touchSlider_OffsetY3.Maximum = paperHeight;
                    if (currentValue > paperHeight)
                        touchSlider_OffsetY3.Value = paperHeight;
                }
                
                // X축 위치 슬라이더의 Maximum은 용지 너비의 절반으로 설정 (센터 기준 좌우 이동)
                if (touchSlider_OffsetX3 != null)
                {
                    int maxX = paperWidth / 2;
                    int currentValue = touchSlider_OffsetX3.Value;
                    touchSlider_OffsetX3.Maximum = maxX;
                    touchSlider_OffsetX3.Minimum = -maxX;
                    // 현재 값이 새로운 범위를 벗어나면 조정
                    if (currentValue > maxX)
                        touchSlider_OffsetX3.Value = maxX;
                    else if (currentValue < -maxX)
                        touchSlider_OffsetX3.Value = -maxX;
                }
                
                System.Diagnostics.Debug.WriteLine($"[위치조절 폼] 슬라이더 Maximum 업데이트: 용지 크기 {paperWidth}x{paperHeight}mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PositionAdjustmentForm] 슬라이더 Maximum 업데이트 오류: {ex.Message}");
            }
        }
        
        public void LoadCurrentValues()
        {
            if (mainForm == null) return;
            
            try
            {
                System.Diagnostics.Debug.WriteLine("[PositionAdjustmentForm] LoadCurrentValues 시작");
                
                UpdateSliderMaximums();
                
                var columnSettings = mainForm.GetColumnPositionSettings();
                if (columnSettings != null && columnSettings.Count > 0)
                {
                    for (int i = 0; i < columnSettings.Count; i++)
                    {
                        var s = columnSettings[i];
                        TouchSlider currentSizeSlider = null;
                        TouchSlider currentOffsetYSlider = null;
                        Label currentSizeLabel = null;
                        Label currentOffsetYLabel = null;

                        switch(i) {
                            case 0: currentSizeSlider = touchSlider_Size0; currentOffsetYSlider = touchSlider_OffsetY0; currentSizeLabel = label_Value_Size0; currentOffsetYLabel = label_Value_OffsetY0; break;
                            case 1: currentSizeSlider = touchSlider_Size1; currentOffsetYSlider = touchSlider_OffsetY1; currentSizeLabel = label_Value_Size1; currentOffsetYLabel = label_Value_OffsetY1; break;
                            case 2: currentSizeSlider = touchSlider_Size2; currentOffsetYSlider = touchSlider_OffsetY2; currentSizeLabel = label_Value_Size2; currentOffsetYLabel = label_Value_OffsetY2; break;
                            case 3: currentSizeSlider = touchSlider_Size3; currentOffsetYSlider = touchSlider_OffsetY3; currentSizeLabel = label_Value_Size3; currentOffsetYLabel = label_Value_OffsetY3; break;
                        }

                        if (currentSizeSlider != null)
                        {
                            currentSizeSlider.Value = s.Size;
                            UpdateValueLabel(currentSizeLabel, s.Size);
                        }
                        if (currentOffsetYSlider != null)
                        {
                            currentOffsetYSlider.Value = s.OffsetY;
                            UpdateValueLabel(currentOffsetYLabel, s.OffsetY);
                        }

                        if (i == 3 && touchSlider_OffsetX3 != null)
                        {
                            touchSlider_OffsetX3.Value = s.OffsetX;
                            UpdateValueLabel(label_Value_OffsetX3, s.OffsetX);
                        }

                        // 타입 및 바코드 관련 컨트롤 값 설정
                        if (i < combo_Usage.Length && combo_Usage[i] != null)
                        {
                            combo_Usage[i].SelectedItem = s.UsageType ?? "텍스트";
                            if (combo_BarcodeType[i] != null)
                                combo_BarcodeType[i].SelectedItem = s.BarcodeType ?? "QR_CODE";
                            if (check_ShowText[i] != null)
                                check_ShowText[i].Checked = s.ShowBarcodeText;
                            if (touchSlider_BarcodeHeight[i] != null)
                            {
                                touchSlider_BarcodeHeight[i].Value = s.BarcodeHeight;
                                UpdateValueLabel(label_Value_BarcodeHeight[i], s.BarcodeHeight);
                            }
                            OnUsageTypeChanged(i); // UI 상태 업데이트
                        }
                        else
                        {
                            // 0~2번 컬럼은 텍스트로 고정 (데이터 모델 동기화)
                            if (columnSettings.Count > i) columnSettings[i].UsageType = "텍스트";
                        }
                    }
                }
                
                if (touchSlider_TitleSize != null)
                {
                    int titleSize = mainForm.GetTitleSize();
                    touchSlider_TitleSize.Value = titleSize;
                    UpdateValueLabel(label_Value_TitleSize, titleSize);
                }
                if (touchSlider_TitleOffsetY != null)
                {
                    int titleOffsetY = mainForm.GetTitleOffsetY();
                    touchSlider_TitleOffsetY.Value = titleOffsetY;
                    UpdateValueLabel(label_Value_TitleOffsetY, titleOffsetY);
                }
                
                System.Diagnostics.Debug.WriteLine("[PositionAdjustmentForm] LoadCurrentValues 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PositionAdjustmentForm] 현재 값 로드 오류: {ex.Message}");
            }
        }
        
        private void OnUsageTypeChanged(int idx)
        {
            if (idx < 0 || idx >= 4) return;
            if (combo_Usage[idx] == null) return; // 컨트롤이 없는 경우(0~2번 컬럼) 무시

            bool isBarcode = combo_Usage[idx].SelectedItem != null && combo_Usage[idx].SelectedItem.ToString() == "바코드";

            // 바코드 전용 컨트롤 활성/비활성
            if (combo_BarcodeType[idx] != null) combo_BarcodeType[idx].Enabled = isBarcode;
            if (check_ShowText[idx] != null) check_ShowText[idx].Enabled = isBarcode;
            if (touchSlider_BarcodeHeight[idx] != null) touchSlider_BarcodeHeight[idx].Enabled = isBarcode;

            // 라벨 텍스트 변경: 바코드면 "가로 폭", 텍스트면 "글자 크기"
            if (label_SizeText[idx] != null)
            {
                label_SizeText[idx].Text = isBarcode ? "가로 폭" : "글자 크기";
            }

            // 변경 사항 즉시 반영
            var settings = mainForm.GetColumnPositionSettings();
            if (settings != null && settings.Count > idx)
            {
                settings[idx].UsageType = combo_Usage[idx].SelectedItem.ToString();
                mainForm.SaveColumnPositionSettings();
                mainForm.OnPreviewEvent();
            }
        }

        /// <summary>
        /// 값 표시 라벨 업데이트
        /// </summary>
        private void UpdateValueLabel(Label label, int value)
        {
            if (label != null)
            {
                label.Text = value.ToString();
            }
        }
        
        private void OnSliderValueChanged(string sliderName, int value)
        {
            if (mainForm == null) return;
            
            try
            {
                var columnSettings = mainForm.GetColumnPositionSettings();
                if (columnSettings == null) return;
                
                switch (sliderName)
                {
                    case "TitleSize":
                        mainForm.SetTitleSize(value);
                        break;
                    case "TitleOffsetY":
                        mainForm.SetTitleOffsetY(value);
                        break;
                    case "TitleOffsetX":
                        mainForm.GetAppSettings().TitleOffsetX = value;
                        mainForm.GetAppSettings().Save();
                        mainForm.OnPreviewEvent();
                        break;
                    case "Size0":
                        if (columnSettings.Count > 0)
                        {
                            columnSettings[0].Size = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "Size1":
                        if (columnSettings.Count > 1)
                        {
                            columnSettings[1].Size = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "Size2":
                        if (columnSettings.Count > 2)
                        {
                            columnSettings[2].Size = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "Size3":
                        if (columnSettings.Count > 3)
                        {
                            columnSettings[3].Size = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "OffsetY0":
                        if (columnSettings.Count > 0)
                        {
                            columnSettings[0].OffsetY = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "OffsetY1":
                        if (columnSettings.Count > 1)
                        {
                            columnSettings[1].OffsetY = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "OffsetY2":
                        if (columnSettings.Count > 2)
                        {
                            columnSettings[2].OffsetY = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "OffsetY3":
                        if (columnSettings.Count > 3)
                        {
                            columnSettings[3].OffsetY = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "OffsetX3":
                        if (columnSettings.Count > 3)
                        {
                            columnSettings[3].OffsetX = value;
                            mainForm.SaveColumnPositionSettings();
                            mainForm.OnPreviewEvent();
                        }
                        break;
                    case "BarcodeHeight0":
                        if (columnSettings.Count > 0) columnSettings[0].BarcodeHeight = value;
                        break;
                    case "BarcodeHeight1":
                        if (columnSettings.Count > 1) columnSettings[1].BarcodeHeight = value;
                        break;
                    case "BarcodeHeight2":
                        if (columnSettings.Count > 2) columnSettings[2].BarcodeHeight = value;
                        break;
                    case "BarcodeHeight3":
                        if (columnSettings.Count > 3) columnSettings[3].BarcodeHeight = value;
                        break;
                }
                
                mainForm.SaveColumnPositionSettings();
                mainForm.OnPreviewEvent();
                
                System.Diagnostics.Debug.WriteLine($"[위치 조절] {sliderName} = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PositionAdjustmentForm] 슬라이더 값 변경 오류: {ex.Message}");
            }
        }
        
        private void CustomCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        private void PanelTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                // panelTitleBar의 로컬 좌표를 스크린 좌표로 변환하여 저장
                Point screenPoint = panelTitleBar.PointToScreen(e.Location);
                dragStartPoint = new Point(screenPoint.X - this.Location.X, screenPoint.Y - this.Location.Y);
                this.Cursor = Cursors.SizeAll;
                panelTitleBar.Capture = true; // 마우스 캡처로 이벤트 유지
            }
        }
        
        private void PanelTitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // panelTitleBar의 로컬 좌표를 스크린 좌표로 변환
                Point screenPoint = panelTitleBar.PointToScreen(e.Location);
                
                // 폼의 새 위치 계산
                Point newLocation = new Point(
                    screenPoint.X - dragStartPoint.X,
                    screenPoint.Y - dragStartPoint.Y
                );
                
                this.Location = newLocation;
            }
        }
        
        private void PanelTitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                panelTitleBar.Capture = false; // 마우스 캡처 해제
                this.Cursor = Cursors.Default;
            }
        }
        
        // labelTitle 드래그 이벤트 (타이틀바 드래그 가능하도록)
        private void LabelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                // labelTitle의 로컬 좌표를 스크린 좌표로 변환하여 저장
                Point screenPoint = labelTitle.PointToScreen(e.Location);
                dragStartPoint = new Point(screenPoint.X - this.Location.X, screenPoint.Y - this.Location.Y);
                this.Cursor = Cursors.SizeAll;
                labelTitle.Capture = true; // 마우스 캡처로 이벤트 유지
            }
        }
        
        private void LabelTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // labelTitle의 로컬 좌표를 스크린 좌표로 변환
                Point screenPoint = labelTitle.PointToScreen(e.Location);
                
                // 폼의 새 위치 계산
                Point newLocation = new Point(
                    screenPoint.X - dragStartPoint.X,
                    screenPoint.Y - dragStartPoint.Y
                );
                
                this.Location = newLocation;
            }
        }
        
        private void LabelTitle_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                labelTitle.Capture = false; // 마우스 캡처 해제
                this.Cursor = Cursors.Default;
            }
        }
        
        // 전체 폼 드래그 이벤트 (드래그 영역 확대)
        private void PositionAdjustmentForm_MouseDown(object sender, MouseEventArgs e)
        {
            // 컨트롤(슬라이더, 버튼 등) 위에서는 드래그하지 않음
            Control control = this.GetChildAtPoint(e.Location);
            if (control != null)
            {
                // 슬라이더, 버튼은 드래그하지 않음
                if (control is TouchSlider || control is CustomCloseButton)
                {
                    return;
                }
                
                // Panel인 경우 저장버튼과 타이틀바는 제외 (타이틀바는 자체 이벤트가 처리)
                if (control is Panel)
                {
                    if (control == panelSaveButton || control == panelTitleBar)
                    {
                        return;
                    }
                }
                
                // Label인 경우 저장버튼과 타이틀바의 자식은 제외 (타이틀바 이벤트가 처리)
                if (control is Label && control.Parent != null)
                {
                    if (control.Parent == panelSaveButton || control.Parent == panelTitleBar)
                    {
                        return;
                    }
                }
            }
            
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                // 폼의 클라이언트 좌표를 스크린 좌표로 변환하여 저장
                Point screenPoint = this.PointToScreen(e.Location);
                dragStartPoint = new Point(screenPoint.X - this.Location.X, screenPoint.Y - this.Location.Y);
                this.Cursor = Cursors.SizeAll;
                this.Capture = true; // 마우스 캡처로 이벤트 유지
            }
        }
        
        private void PositionAdjustmentForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // 폼의 클라이언트 좌표를 스크린 좌표로 변환
                Point screenPoint = this.PointToScreen(e.Location);
                
                // 폼의 새 위치 계산
                Point newLocation = new Point(
                    screenPoint.X - dragStartPoint.X,
                    screenPoint.Y - dragStartPoint.Y
                );
                
                this.Location = newLocation;
            }
        }
        
        private void PositionAdjustmentForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                this.Capture = false; // 마우스 캡처 해제
                this.Cursor = Cursors.Default;
            }
        }
        
        private void PanelSaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (mainForm == null) return;
                
                System.Diagnostics.Debug.WriteLine("[위치조절 폼 저장] 저장 시작");
                
                // 현재 모든 슬라이더 값을 columnPositionSettings에 반영
                var columnSettings = mainForm.GetColumnPositionSettings();
                if (columnSettings != null && columnSettings.Count >= 4)
                {
                    // 모든 컬럼의 현재 슬라이더 및 콤보박스 값을 반영
                    for (int i = 0; i < 4; i++)
                    {
                        if (columnSettings.Count > i)
                        {
                            var s = columnSettings[i];
                            
                            // 기본 위치/크기
                            TouchSlider currentSizeSlider = null;
                            TouchSlider currentOffsetYSlider = null;
                            switch(i) {
                                case 0: currentSizeSlider = touchSlider_Size0; currentOffsetYSlider = touchSlider_OffsetY0; break;
                                case 1: currentSizeSlider = touchSlider_Size1; currentOffsetYSlider = touchSlider_OffsetY1; break;
                                case 2: currentSizeSlider = touchSlider_Size2; currentOffsetYSlider = touchSlider_OffsetY2; break;
                                case 3: currentSizeSlider = touchSlider_Size3; currentOffsetYSlider = touchSlider_OffsetY3; break;
                            }

                            if (currentSizeSlider != null) s.Size = currentSizeSlider.Value;
                            if (currentOffsetYSlider != null) s.OffsetY = currentOffsetYSlider.Value;
                            if (i == 3 && touchSlider_OffsetX3 != null) s.OffsetX = touchSlider_OffsetX3.Value;

                            // 타입 및 바코드 관련 필드 저장
                            if (combo_Usage[i] != null) s.UsageType = combo_Usage[i].SelectedItem?.ToString() ?? "텍스트";
                            if (combo_BarcodeType[i] != null) s.BarcodeType = combo_BarcodeType[i].SelectedItem?.ToString() ?? "QR_CODE";
                            if (check_ShowText[i] != null) s.ShowBarcodeText = check_ShowText[i].Checked;
                            if (touchSlider_BarcodeHeight[i] != null) s.BarcodeHeight = touchSlider_BarcodeHeight[i].Value;
                        }
                    }
                }

                // Title 설정 저장
                if (touchSlider_TitleSize != null) mainForm.SetTitleSize(touchSlider_TitleSize.Value);
                if (touchSlider_TitleOffsetY != null) mainForm.SetTitleOffsetY(touchSlider_TitleOffsetY.Value);

                // 컬럼 위치 설정 저장 (용지 타입별 저장 포함)
                mainForm.SaveColumnPositionSettings();
                mainForm.OnPreviewEvent();
                
                System.Windows.Forms.MessageBox.Show(
                    "설정이 저장되었습니다.",
                    "저장 완료",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information
                );
                
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[위치조절 폼 저장 오류] {ex.Message}");
                System.Windows.Forms.MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PanelSaveButton_Paint(object sender, PaintEventArgs e)
        {
            DrawSaveButton(1f);
        }
        
        private void PanelSaveButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DrawSaveButton(0.8f);
            }
        }
        
        private void PanelSaveButton_MouseUp(object sender, MouseEventArgs e)
        {
            DrawSaveButton(1f);
        }
        
        private void PanelSaveButton_MouseEnter(object sender, EventArgs e)
        {
            DrawSaveButton(1.1f);
        }
        
        private void PanelSaveButton_MouseLeave(object sender, EventArgs e)
        {
            DrawSaveButton(1f);
        }
        
        private void DrawSaveButton(float shadowRate = 1f)
        {
            string buttonText = "저장";
            Font textFont = new Font("맑은 고딕", 14F, FontStyle.Bold, GraphicsUnit.Point);
            
            // 음영 효과를 적용한 색상 결정
            Color originalColor = Color.FromArgb(0, 120, 215);
            Color buttonColor = ApplyShadowEffect(originalColor, shadowRate);
            
            using (Graphics g = panelSaveButton.CreateGraphics())
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int r = 8;
                    int w = panelSaveButton.Width - 1;
                    int h = panelSaveButton.Height - 1;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(w - r, 0, r, r, 270, 90);
                    path.AddArc(w - r, h - r, r, r, 0, 90);
                    path.AddArc(0, h - r, r, r, 90, 90);
                    path.CloseFigure();
                    
                    using (SolidBrush brush = new SolidBrush(buttonColor))
                    {
                        g.FillPath(brush, path);
                    }
                    
                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(buttonText, textFont);
                        PointF textPosition = new PointF(
                            (panelSaveButton.Width - textSize.Width) / 2,
                            (panelSaveButton.Height - textSize.Height) / 2
                        );
                        g.DrawString(buttonText, textFont, textBrush, textPosition);
                    }
                }
            }
        }
        
        private Color ApplyShadowEffect(Color originalColor, float shadowRate)
        {
            int r = (int)(originalColor.R * shadowRate);
            int g = (int)(originalColor.G * shadowRate);
            int b = (int)(originalColor.B * shadowRate);
            
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            
            return Color.FromArgb(originalColor.A, r, g, b);
        }
    }
}
