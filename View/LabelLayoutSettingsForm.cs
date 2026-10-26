using System;



using System.Collections.Generic;



using System.Drawing;



using System.Drawing.Drawing2D;



using System.Drawing.Text;



using System.Linq;



using System.Windows.Forms;



using BarcodePrint.Model;



using BarcodePrint.UserControls;







namespace BarcodePrint.View



{



    public partial class LabelLayoutSettingsForm : Form



    {



        // Y축 업/다운 반전 처리를 위한 커스텀 NumericUpDown



        private class InvertedYNumericUpDown : NumericUpDown



        {



            public override void UpButton() { base.DownButton(); }



            public override void DownButton() { base.UpButton(); }



            protected override void OnKeyDown(KeyEventArgs e)



            {



                if (e.KeyCode == Keys.Up) { base.OnKeyDown(new KeyEventArgs(Keys.Down)); e.Handled = true; }



                else if (e.KeyCode == Keys.Down) { base.OnKeyDown(new KeyEventArgs(Keys.Up)); e.Handled = true; }



                else base.OnKeyDown(e);



            }



        }







        private MainForm mainForm;



        private List<ColumnPositionSetting> settings;



        private List<ColumnPositionSetting> originalSettings;



        private List<string> previewTexts;



        private int selectedIndex = 0;



        private float SCALE = 4.0f; // mm to pixel (인쇄와 동일한 mmToPixel 사용)



        private float zoomFactor = 1.0f;



        private bool isDragging = false;



        private bool isResizing = false;

        private bool isInitializing = true;

        private int originalPaperWidth;
        private int originalPaperHeight;
        private bool originalPaperRotate;



        private int resizeStartSize = 0;



        private Point dragStartMousePos;



        private PointF lastMousePos;



        private float labelWidthMm;



        private float labelHeightMm;



        private Panel centeringPanel;







        private enum ResizingHandle { None, TopLeft, TopRight, BottomLeft, BottomRight, Left, Right, Top, Bottom }



        private ResizingHandle activeHandle = ResizingHandle.None;



        private int resizeStartWidth = 0;



        private int resizeStartBarcodeHeight = 0;







        private float dragAccumulatorX, dragAccumulatorY;







        // UI Controls



        private PictureBox pbPreview;



        private Panel previewContainer;



        private ComboBox cbItemSelector;



        private ComboBox cbUsageType;



        private Label lblItemTitle;



        private Label lblSelect;



        private Label lblUsage;



        private Label lblHeight;



        private NumericUpDown numX;



        private NumericUpDown numY;



        private NumericUpDown numSize;



        private NumericUpDown numWidth;



        private NumericUpDown numHeight;



        private ComboBox cbFont;



        private CheckBox chkBold;



        private ComboBox cbRotate;



        private PointF dragStartLocalPos;



        private Panel controlsContainer;



        // 용지크기, 바코드 관련 (AdjustForm에서 이동)



        private NumericUpDown numPaperWidth;



        private NumericUpDown numPaperHeight;



        private CheckBox chkPaperRotate;



        private ComboBox cbBarcodeType;



        private CheckBox chkRemoveBarcodeText;



        private NumericUpDown numBarcodeTextX;



        private NumericUpDown numBarcodeTextY;



        private NumericUpDown numBarcodeTextSize;



        private Panel panelBarcodeOptions;



        private Label lblAlignment;
        private Label lblSize;
        private Label lblWidth;



        private ComboBox cbAlignment;







        public LabelLayoutSettingsForm(MainForm main)



        {



            this.mainForm = main;



            this.settings = mainForm.GetColumnPositionSettings().Select(s => new ColumnPositionSetting



            {



                ColumnIndex = s.ColumnIndex,



                IsEnabled = s.IsEnabled,



                Size = s.Size,



                OffsetX = s.OffsetX,



                OffsetY = s.OffsetY,



                UsageType = s.UsageType,



                BarcodeType = s.BarcodeType,



                ShowBarcodeText = s.ShowBarcodeText,



                BarcodeTextOffsetX = s.BarcodeTextOffsetX,



                BarcodeTextOffsetY = s.BarcodeTextOffsetY,



                BarcodeTextSize = s.BarcodeTextSize,



                FontFamily = s.FontFamily,



                IsBold = s.IsBold,



                CustomWidth = s.CustomWidth,



                BarcodeHeight = s.BarcodeHeight,



                RotateAngle = s.RotateAngle,



                Alignment = s.Alignment



            }).ToList();







            this.originalSettings = mainForm.GetColumnPositionSettings().Select(s => new ColumnPositionSetting



            {



                ColumnIndex = s.ColumnIndex,



                IsEnabled = s.IsEnabled,



                Size = s.Size,



                OffsetX = s.OffsetX,



                OffsetY = s.OffsetY,



                UsageType = s.UsageType,



                BarcodeType = s.BarcodeType,



                ShowBarcodeText = s.ShowBarcodeText,



                BarcodeTextOffsetX = s.BarcodeTextOffsetX,



                BarcodeTextOffsetY = s.BarcodeTextOffsetY,



                BarcodeTextSize = s.BarcodeTextSize,



                FontFamily = s.FontFamily,



                IsBold = s.IsBold,



                CustomWidth = s.CustomWidth,



                BarcodeHeight = s.BarcodeHeight,



                RotateAngle = s.RotateAngle,



                Alignment = s.Alignment



            }).ToList();







            this.previewTexts = mainForm.GetPreviewTexts();

            // 원래 용지 값 백업 (취소 시 롤백용)
            this.originalPaperWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
            this.originalPaperHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
            this.originalPaperRotate = PRINT_INFO_CLASS.PrinterInfo.bPaperRotated;



            // 실제 라벨 크기 가져오기



            if (PRINT_INFO_CLASS.PrinterInfo.Paper_Width > 0)



                this.labelWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;



            else



                this.labelWidthMm = 105f; // 명찰 기본값 (105mm)







            if (PRINT_INFO_CLASS.PrinterInfo.Paper_Height > 0)



                this.labelHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;



            else



                this.labelHeightMm = 145f; // 명찰 기본값 (145mm)







            // 라벨 회전 감안



            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



            {



                float temp = this.labelWidthMm;



                this.labelWidthMm = this.labelHeightMm;



                this.labelHeightMm = temp;



            }



            



            this.FormClosing += LabelLayoutSettingsForm_FormClosing;



            this.Shown += (s, ev) => { previewTexts = mainForm.GetPreviewTexts(); LayoutCenteringPanel(); pbPreview?.Invalidate(); };







            InitializeComponent();
            
            // 런타임 스케일 및 컨트롤 값 재바인딩: 디자인 타임 파서 에러 방지
            float dpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203f;
            SCALE = dpi / 25.4f;
            
            if (numPaperWidth != null)
                numPaperWidth.Value = (decimal)(PRINT_INFO_CLASS.PrinterInfo.Paper_Width > 0 ? PRINT_INFO_CLASS.PrinterInfo.Paper_Width : 80);
            if (numPaperHeight != null)
                numPaperHeight.Value = (decimal)(PRINT_INFO_CLASS.PrinterInfo.Paper_Height > 0 ? PRINT_INFO_CLASS.PrinterInfo.Paper_Height : 60);
            if (chkPaperRotate != null)
                chkPaperRotate.Checked = PRINT_INFO_CLASS.PrinterInfo.bPaperRotated;







            // 폼 크기 고정: 해상도/DPI 변화에 따른 폼 변형 방지



            this.MinimumSize = this.Size;



            this.MaximumSize = this.Size;







            UpdatePropertyControls(0);

            this.isInitializing = false;



        }







        private void InitializeComponent()



        {



            this.SuspendLayout();







            // 폼 기본 속성: DPI 스케일링 방지를 위해 None으로 설정



            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;



            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);



            this.ClientSize = new Size(950, 600);



            this.Text = "비주얼 라벨 레이아웃 에디터";



            this.StartPosition = FormStartPosition.CenterParent;



            this.BackColor = Color.White;



            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;



            this.MaximizeBox = false;







            TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };



            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));



            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));



            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90f));



            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));



            this.Controls.Add(mainLayout);







            // 1. Preview Area



            previewContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(220, 220, 220), Padding = new Padding(10), AutoScroll = true };



            



            Panel paperShadow = new Panel { 



                BackColor = Color.FromArgb(160, 160, 160), 



                Padding = new Padding(0)



            };



            



            pbPreview = new PictureBox { 



                BackColor = Color.White, 



                SizeMode = PictureBoxSizeMode.Normal,



                Cursor = Cursors.Hand,



                Location = new Point(0, 0)



            };



            



            // 디자인 타임 기본 스케일 적용 (실제 스케일은 런타임 생성자에서 덮어씀)
            SCALE = 203f / 25.4f;







            const int MAX_PREVIEW_WIDTH = 550;



            const int MAX_PREVIEW_HEIGHT = 450;







            float labelAspect = labelHeightMm / labelWidthMm;



            int pbW, pbH;







            if (labelAspect < 1) // 가로가 더 긴 경우 (aspectRatio > 1 in MainForm equivalent logic)



            {



                pbW = Math.Min((int)labelWidthMm * 4, MAX_PREVIEW_WIDTH);



                pbH = (int)(pbW * labelAspect);



                if (pbH > MAX_PREVIEW_HEIGHT)



                {



                    pbH = MAX_PREVIEW_HEIGHT;



                    pbW = (int)(pbH / labelAspect);



                }



            }



            else // 세로가 더 긴 경우



            {



                pbH = Math.Min((int)labelHeightMm * 4, MAX_PREVIEW_HEIGHT);



                pbW = (int)(pbH / labelAspect);



                if (pbW > MAX_PREVIEW_WIDTH)



                {



                    pbW = MAX_PREVIEW_WIDTH;



                    pbH = (int)(pbW * labelAspect);



                }



            }







            // 최소 크기 보장



            pbW = Math.Max(pbW, 100);



            pbH = Math.Max(pbH, 80);







            pbPreview.Size = new Size(pbW, pbH);



            paperShadow.Size = new Size(pbW + 2, pbH + 2);



            paperShadow.Controls.Add(pbPreview);







            pbPreview.Paint += PbPreview_Paint;



            pbPreview.MouseDown += PbPreview_MouseDown;



            pbPreview.MouseMove += PbPreview_MouseMove;



            pbPreview.MouseUp += PbPreview_MouseUp;







            centeringPanel = new Panel { Dock = DockStyle.None, BackColor = Color.Transparent };



            centeringPanel.Controls.Add(paperShadow);



            previewContainer.Controls.Add(centeringPanel);



            mainLayout.Controls.Add(previewContainer, 0, 0);







            // 마우스 휠 및 컨테이너 리사이즈 이벤트 바인딩



            pbPreview.MouseWheel += PbPreview_MouseWheel;



            previewContainer.MouseWheel += PbPreview_MouseWheel;



            previewContainer.Resize += (s, e) => LayoutCenteringPanel();







            // 2. Properties Area



            Panel propPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };



            mainLayout.Controls.Add(propPanel, 1, 0);







            TableLayoutPanel rightLayout = new TableLayoutPanel { 



                Dock = DockStyle.Fill, 



                ColumnCount = 1, 



                RowCount = 2,



                BackColor = Color.White



            };



            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));



            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));



            propPanel.Controls.Add(rightLayout);







            Panel headerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 10, 15, 0) };



            rightLayout.Controls.Add(headerPanel, 0, 0);







            Font labelFont = new Font("맑은 고딕", 13, FontStyle.Bold, GraphicsUnit.Pixel);



            Font controlFont = new Font("맑은 고딕", 13, FontStyle.Regular, GraphicsUnit.Pixel);







            lblItemTitle = new Label { 



                Text = "편집 중: 컬럼1", 



                Font = new Font("맑은 고딕", 19, FontStyle.Bold, GraphicsUnit.Pixel), 



                Location = new Point(15, 15),



                Size = new Size(350, 35), 



                TextAlign = ContentAlignment.MiddleLeft,



                ForeColor = Color.FromArgb(63, 81, 181)



            };



            headerPanel.Controls.Add(lblItemTitle);







            lblSelect = new Label {



                Text = "대상 선택:",



                Font = new Font("맑은 고딕", 14, FontStyle.Bold, GraphicsUnit.Pixel),



                Location = new Point(15, 60),



                Size = new Size(80, 30),



                TextAlign = ContentAlignment.MiddleLeft



            };



            headerPanel.Controls.Add(lblSelect);







            cbItemSelector = new ComboBox { 



                DropDownStyle = ComboBoxStyle.DropDownList, 



                Size = new Size(160, 35),



                Location = new Point(100, 58),



                Font = new Font("맑은 고딕", 15, FontStyle.Regular, GraphicsUnit.Pixel) 



            };



            cbItemSelector.Items.AddRange(new string[] { "컬럼1", "컬럼2", "컬럼3", "컬럼4", "공통문구" });



            cbItemSelector.SelectedIndex = 0;



            cbItemSelector.SelectedIndexChanged += (s, e) => UpdatePropertyControls(cbItemSelector.SelectedIndex);



            headerPanel.Controls.Add(cbItemSelector);







            lblUsage = new Label {



                Text = "유형 선택:",



                Font = new Font("맑은 고딕", 14, FontStyle.Bold, GraphicsUnit.Pixel),



                Location = new Point(15, 100),



                Size = new Size(80, 30),



                TextAlign = ContentAlignment.MiddleLeft,



                Visible = false



            };



            headerPanel.Controls.Add(lblUsage);







            cbUsageType = new ComboBox { 



                DropDownStyle = ComboBoxStyle.DropDownList, 



                Size = new Size(160, 35), // 높이 35로 통일



                Location = new Point(100, 98),



                Font = new Font("맑은 고딕", 15, FontStyle.Regular, GraphicsUnit.Pixel), // 폰트 크기 통일



                Visible = false



            };



            cbUsageType.Items.AddRange(new string[] { "텍스트", "바코드" });



            cbUsageType.SelectedIndexChanged += (s, e) => {



                if (cbUsageType.SelectedItem != null) {



                    settings[selectedIndex].UsageType = cbUsageType.SelectedItem.ToString();



                    UpdatePropertyControls(selectedIndex); // UI 갱신 (바코드 옵션 패널 보임/숨김 처리)



                    pbPreview.Invalidate();



                    mainForm.UpdateColumnPositionSettings(settings, false);



                }



            };



            headerPanel.Controls.Add(cbUsageType);







            Panel headerLine = new Panel { 



                BackColor = Color.FromArgb(230, 230, 230), 



                Height = 1, 



                Location = new Point(15, 150), 



                Width = 380 



            };



            headerPanel.Controls.Add(headerLine);







            controlsContainer = new Panel { 



                Dock = DockStyle.Fill, 



                Padding = new Padding(15, 15, 15, 0), 



                AutoScroll = true,



                BackColor = Color.White



            };



            rightLayout.Controls.Add(controlsContainer, 0, 1);







            const int L_LABEL_X = 5;



            const int L_LABEL_W = 80;



            const int L_CTRL_X = 85;



            const int L_CTRL_W = 60;







            const int R_LABEL_X = 150;



            const int R_LABEL_W = 80;



            const int R_CTRL_X = 230;



            const int R_CTRL_W = 60;







            const int ROW_H = 40;   // 줄 높이 축소



            int y = 5;







            // 1. 용지 크기 설정 (표준 컨트롤 사용)



            Label lblPaper = new Label { Text = "용지 크기(mm):", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W + 5, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblPaper);



            



            numPaperWidth = new NumericUpDown { Location = new Point(L_CTRL_X + 5, y + 5), Size = new Size(50, 30), Font = controlFont, Minimum = 10, Maximum = 500, Value = 80 };
            numPaperHeight = new NumericUpDown { Location = new Point(R_LABEL_X + 10, y + 5), Size = new Size(50, 30), Font = controlFont, Minimum = 10, Maximum = 500, Value = 60 };



            numPaperWidth.ValueChanged += (s, e) => OnPaperSizeChanged();



            numPaperHeight.ValueChanged += (s, e) => OnPaperSizeChanged();



            controlsContainer.Controls.Add(numPaperWidth);



            controlsContainer.Controls.Add(numPaperHeight);







            Label lblPaperX = new Label { Text = "x", Font = controlFont, Location = new Point(L_CTRL_X + 55, y + 8), Size = new Size(15, 25), TextAlign = ContentAlignment.MiddleCenter };



            controlsContainer.Controls.Add(lblPaperX);



            y += 40;







            chkPaperRotate = new CheckBox { 



                Text = "라벨 90도 회전", 



                Font = labelFont, 



                Location = new Point(L_CTRL_X + 5, y), 
                Size = new Size(180, 30), 
                Checked = false 



            };



            chkPaperRotate.CheckedChanged += (s, e) => {



                PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = chkPaperRotate.Checked;



                OnPaperSizeChanged(); // 용지 크기 재계산 및 미리보기 갱신 트리거



            };



            controlsContainer.Controls.Add(chkPaperRotate);



            y += 35;







            Panel line1 = new Panel { BackColor = Color.FromArgb(230, 230, 230), Height = 1, Location = new Point(5, y + 5), Width = 290 };



            controlsContainer.Controls.Add(line1);



            y += 15;







            // 2. 폰트 종류 & 글자 굵게 (한 줄 배치)



            Label lblFont = new Label { Text = "폰트 종류:", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblFont);



            cbFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(L_CTRL_X, y + 5), Size = new Size(120, 35), Font = controlFont };



            LoadSystemFonts();



            controlsContainer.Controls.Add(cbFont);



            cbFont.SelectedIndexChanged += (s, e) => { if (cbFont.SelectedItem != null) { settings[selectedIndex].FontFamily = cbFont.SelectedItem.ToString(); pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };







            chkBold = new CheckBox { Text = "굵게", Font = labelFont, Location = new Point(R_LABEL_X + 60, y), Size = new Size(80, 40) };



            controlsContainer.Controls.Add(chkBold);



            chkBold.CheckedChanged += (s, e) => { settings[selectedIndex].IsBold = chkBold.Checked; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); };



            y += ROW_H;







            // 3. X 위치 & Y 위치 (한 줄 배치)



            Label lblX = new Label { Text = "X위치(mm):", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblX);



            numX = new NumericUpDown { Minimum = -200, Maximum = 500, Location = new Point(L_CTRL_X, y + 5), Size = new Size(L_CTRL_W, 30), Font = controlFont };



            controlsContainer.Controls.Add(numX);



            numX.ValueChanged += (s, e) => { if (!isDragging) { settings[selectedIndex].OffsetX = (int)numX.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };







            Label lblY = new Label { Text = "Y위치(mm):", Font = labelFont, Location = new Point(R_LABEL_X, y + 8), Size = new Size(R_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblY);



            numY = new InvertedYNumericUpDown { Minimum = -200, Maximum = 500, Location = new Point(R_CTRL_X, y + 5), Size = new Size(R_CTRL_W, 30), Font = controlFont };



            controlsContainer.Controls.Add(numY);



            numY.ValueChanged += (s, e) => { if (!isDragging) { settings[selectedIndex].OffsetY = (int)numY.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };



            y += ROW_H;







            // 4. 크기 & 회전 각도 (한 줄 배치)



            lblSize = new Label { Text = "크기(pt):", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblSize);



            numSize = new NumericUpDown { Minimum = 1, Maximum = 300, Location = new Point(L_CTRL_X, y + 5), Size = new Size(L_CTRL_W, 30), Font = controlFont };



            controlsContainer.Controls.Add(numSize);



            numSize.ValueChanged += (s, e) => { if (!isResizing) { settings[selectedIndex].Size = (int)numSize.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };







            Label lblRotate = new Label { Text = "회전 각도:", Font = labelFont, Location = new Point(R_LABEL_X, y + 8), Size = new Size(R_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblRotate);



            cbRotate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(R_CTRL_X, y + 5), Size = new Size(R_CTRL_W, 30), Font = controlFont };



            cbRotate.Items.AddRange(new object[] { "0°", "90°", "180°", "270°" });



            cbRotate.SelectedIndex = 0;



            cbRotate.SelectedIndexChanged += (s, e) => {



                if (cbRotate.SelectedIndex >= 0) {



                    settings[selectedIndex].RotateAngle = cbRotate.SelectedIndex * 90;



                    pbPreview.Invalidate();



                    mainForm.UpdateColumnPositionSettings(settings, false);



                }



            };



            controlsContainer.Controls.Add(cbRotate);



            y += ROW_H;







            // 5. 가로폭 & 세로 높이 (한 줄 배치)



            lblWidth = new Label { Text = "폭(0=자동):", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblWidth);



            numWidth = new NumericUpDown { Minimum = 0, Maximum = 1000, Location = new Point(L_CTRL_X, y + 5), Size = new Size(L_CTRL_W, 30), Font = controlFont };



            controlsContainer.Controls.Add(numWidth);



            numWidth.ValueChanged += (s, e) => { if (!isResizing) { settings[selectedIndex].CustomWidth = (int)numWidth.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };







            lblHeight = new Label { Text = "높이(mm):", Font = labelFont, Location = new Point(R_LABEL_X, y + 8), Size = new Size(R_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblHeight);



            numHeight = new NumericUpDown { Minimum = 1, Maximum = 500, Location = new Point(R_CTRL_X, y + 5), Size = new Size(R_CTRL_W, 30), Font = controlFont };



            controlsContainer.Controls.Add(numHeight);



            numHeight.ValueChanged += (s, e) => { if (!isResizing) { settings[selectedIndex].BarcodeHeight = (int)numHeight.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };



            y += ROW_H;







            // 5-2. 정렬 방식



            lblAlignment = new Label { Text = "정렬 방식:", Font = labelFont, Location = new Point(L_LABEL_X, y + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };



            controlsContainer.Controls.Add(lblAlignment);



            cbAlignment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(L_CTRL_X, y + 5), Size = new Size(L_CTRL_W + 40, 30), Font = controlFont };



            cbAlignment.Items.AddRange(new object[] { "Left", "Center", "Right" });



            cbAlignment.SelectedIndex = 0; // 기본 Left



            cbAlignment.SelectedIndexChanged += (s, e) => {



                if (cbAlignment.SelectedItem != null) {



                    settings[selectedIndex].Alignment = cbAlignment.SelectedItem.ToString();



                    pbPreview.Invalidate();



                    mainForm.UpdateColumnPositionSettings(settings, false);



                }



            };



            controlsContainer.Controls.Add(cbAlignment);



            y += ROW_H;







            // 6. 바코드 전용 옵션 패널 (2열 레이아웃으로 변경)



            panelBarcodeOptions = new Panel { Location = new Point(0, y), Size = new Size(300, 160), Visible = false };



            int by = 0;



            



            Label lblBarcodeType = new Label { Text = "바코드 타입:", Font = labelFont, Location = new Point(L_LABEL_X, by + 8), Size = new Size(L_LABEL_W, 25), TextAlign = ContentAlignment.MiddleLeft };
            panelBarcodeOptions.Controls.Add(lblBarcodeType);

            cbBarcodeType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(L_CTRL_X, by + 5), Size = new Size(100, 35), Font = controlFont };
            cbBarcodeType.Items.AddRange(new string[] { "QR_CODE", "CODE_128", "CODE_39", "CODE_93", "EAN_13", "EAN_8", "UPC_A", "UPC_E", "CODABAR", "ITF", "DATA_MATRIX", "PDF_417", "AZTEC" });
            cbBarcodeType.SelectedIndexChanged += (s, e) => {
                if (cbBarcodeType.SelectedItem != null && (settings[selectedIndex].UsageType == "바코드" || selectedIndex == 3))
                {
                    settings[selectedIndex].BarcodeType = cbBarcodeType.SelectedItem.ToString();
                    PRINT_INFO_CLASS.PrinterInfo.BarcodeType = settings[selectedIndex].BarcodeType;
                    UpdateBarcodeControlsState(settings[selectedIndex]);
                    pbPreview.Invalidate();
                    mainForm.UpdateColumnPositionSettings(settings, false);
                    mainForm.OnPreviewEvent();
                }
            };
            panelBarcodeOptions.Controls.Add(cbBarcodeType);

            chkRemoveBarcodeText = new CheckBox { Text = "텍스트 제거", Font = labelFont, Location = new Point(200, by + 5), Size = new Size(95, 30) };
            chkRemoveBarcodeText.CheckedChanged += (s, e) => { if (settings[selectedIndex].UsageType == "바코드" || selectedIndex == 3) { settings[selectedIndex].ShowBarcodeText = !chkRemoveBarcodeText.Checked; ToggleBarcodeTextControls(); pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); mainForm.OnPreviewEvent(); } };
            panelBarcodeOptions.Controls.Add(chkRemoveBarcodeText);

            by += ROW_H;

            Label lblBarcodeTextX = new Label { Text = "텍스트 X:", Font = labelFont, Location = new Point(L_LABEL_X, by + 8), Size = new Size(65, 25), TextAlign = ContentAlignment.MiddleLeft, Tag = "BarcodeText" };
            panelBarcodeOptions.Controls.Add(lblBarcodeTextX);

            numBarcodeTextX = new NumericUpDown { Minimum = -200, Maximum = 200, Location = new Point(75, by + 5), Size = new Size(50, 30), Font = controlFont };
            numBarcodeTextX.ValueChanged += (s, e) => { if (settings[selectedIndex].UsageType == "바코드" || selectedIndex == 3) { settings[selectedIndex].BarcodeTextOffsetX = (int)numBarcodeTextX.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); mainForm.OnPreviewEvent(); } };
            panelBarcodeOptions.Controls.Add(numBarcodeTextX);

            Label lblBarcodeTextY = new Label { Text = "텍스트 Y:", Font = labelFont, Location = new Point(145, by + 8), Size = new Size(65, 25), TextAlign = ContentAlignment.MiddleLeft, Tag = "BarcodeText" };
            panelBarcodeOptions.Controls.Add(lblBarcodeTextY);

            numBarcodeTextY = new InvertedYNumericUpDown { Minimum = -200, Maximum = 200, Location = new Point(215, by + 5), Size = new Size(50, 30), Font = controlFont };
            numBarcodeTextY.ValueChanged += (s, e) => { if (settings[selectedIndex].UsageType == "바코드" || selectedIndex == 3) { settings[selectedIndex].BarcodeTextOffsetY = (int)numBarcodeTextY.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); mainForm.OnPreviewEvent(); } };
            panelBarcodeOptions.Controls.Add(numBarcodeTextY);

            by += ROW_H;

            Label lblBarcodeTextSize = new Label { Text = "텍스트 크기:", Font = labelFont, Location = new Point(L_LABEL_X, by + 8), Size = new Size(80, 25), TextAlign = ContentAlignment.MiddleLeft, Tag = "BarcodeText" };
            panelBarcodeOptions.Controls.Add(lblBarcodeTextSize);

            numBarcodeTextSize = new NumericUpDown { Minimum = 1, Maximum = 200, Location = new Point(90, by + 5), Size = new Size(50, 30), Font = controlFont };
            numBarcodeTextSize.ValueChanged += (s, e) => { if (settings[selectedIndex].UsageType == "바코드" || selectedIndex == 3) { settings[selectedIndex].BarcodeTextSize = (int)numBarcodeTextSize.Value; pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); mainForm.OnPreviewEvent(); } };
            panelBarcodeOptions.Controls.Add(numBarcodeTextSize);

            



            controlsContainer.Controls.Add(panelBarcodeOptions);



            y += by + 10;







            // Bottom Buttons



            Panel bottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 245, 245), Padding = new Padding(10) };



            mainLayout.Controls.Add(bottomPanel, 0, 1);



            mainLayout.SetColumnSpan(bottomPanel, 2);







            RoundedButton btnReset = new RoundedButton { 



                Text = "초기화", Size = new Size(100, 40), 



                Location = new Point(10, 10),



                Style = RoundedButton.ButtonStyle.Secondary,



                BackColor = Color.FromArgb(255, 152, 0),



                ForeColor = Color.White,



                Font = new Font("맑은 고딕", 14, FontStyle.Bold, GraphicsUnit.Pixel) 



            };



            btnReset.Click += (s, e) => {



                if (MessageBox.Show("모든 설정을 기본값으로 초기화하시겠습니까?", "초기화", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)



                {



                    ApplyAutoDefaults();



                    mainForm.UpdateColumnPositionSettings(settings, false);



                    pbPreview.Invalidate();



                }



            };



            bottomPanel.Controls.Add(btnReset);











            RoundedButton btnSave = new RoundedButton { 



                Text = "설정 저장", Size = new Size(120, 40), 



                Location = new Point(700, 10),



                Style = RoundedButton.ButtonStyle.Primary,



                Font = new Font("맑은 고딕", 14, FontStyle.Bold, GraphicsUnit.Pixel) 



            };



            btnSave.Click += (s, e) => { mainForm.UpdateColumnPositionSettings(settings); this.DialogResult = DialogResult.OK; this.Close(); };



            bottomPanel.Controls.Add(btnSave);







            RoundedButton btnCancel = new RoundedButton { 



                Text = "취소", Size = new Size(100, 40), 



                Location = new Point(830, 10), 



                Style = RoundedButton.ButtonStyle.Secondary,



                Font = new Font("맑은 고딕", 14, FontStyle.Regular, GraphicsUnit.Pixel) 



            };



            btnCancel.Click += (s, e) => this.Close();



            bottomPanel.Controls.Add(btnCancel);



            // 템플릿 내보내기 버튼 추가
            RoundedButton btnExportLayout = new RoundedButton { 
                Text = "템플릿 내보내기", Size = new Size(130, 40), 
                Location = new Point(130, 10),
                Style = RoundedButton.ButtonStyle.Secondary,
                Font = new Font("맑은 고딕", 14, FontStyle.Regular, GraphicsUnit.Pixel) 
            };

            btnExportLayout.Click += (s, e) => {
                try
                {
                    // 1. 현재 설정 폼의 실시간 데이터를 직렬화 문자열로 변환
                    var lines = new List<string>();
                    foreach (var col in settings)
                    {
                        lines.Add($"{col.ColumnIndex}|{col.IsEnabled}|{col.Size}|{col.OffsetX}|{col.OffsetY}|{col.UsageType}|{col.BarcodeType}|{col.ShowBarcodeText}|{col.FontFamily}|{col.IsBold}|{col.BarcodeTextOffsetX}|{col.BarcodeTextOffsetY}|{col.BarcodeTextSize}|{col.CustomWidth}|{col.BarcodeHeight}|{col.RotateAngle}|{col.Alignment}");
                    }
                    string currentColumnData = string.Join(";", lines);

                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Label Layout (*.lml)|*.lml|JSON Files (*.json)|*.json";
                        sfd.Title = "레이아웃 설정 내보내기";
                        sfd.FileName = $"Layout_{PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "Custom"}_{DateTime.Now:yyyyMMdd_HHmm}.lml";
                        sfd.InitialDirectory = System.Windows.Forms.Application.StartupPath;

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            BarcodePrint.Model.LayoutData layout = new BarcodePrint.Model.LayoutData
                            {
                                Version = "1.0",
                                SavedDate = DateTime.Now,
                                ColumnPositionData = currentColumnData, // 2. 실시간 조율된 데이터를 직접 삽입
                                PaperTypeColumnPositionData = mainForm.GetAppSettings().PaperTypeColumnPositionData,
                                TitleSize = mainForm.GetAppSettings().TitleSize,
                                TitleOffsetY = mainForm.GetAppSettings().TitleOffsetY,
                                TitleOffsetX = mainForm.GetAppSettings().TitleOffsetX,
                                IsTitleEnabled = mainForm.GetAppSettings().TitleEnabled,
                                PaperTypeTitleData = mainForm.GetAppSettings().PaperTypeTitleData,
                                BarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType,
                                PaperWidth = (int)numPaperWidth.Value,
                                PaperHeight = (int)numPaperHeight.Value,
                                PaperTypeName = PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType ?? "",
                                IsRotated = chkPaperRotate.Checked,
                                GlobalOffsetX = (int)PRINT_INFO_CLASS.PrinterInfo.offset_X,
                                GlobalOffsetY = (int)PRINT_INFO_CLASS.PrinterInfo.offset_Y,
                                PaperGap = PRINT_INFO_CLASS.PrinterInfo.nGAP,
                                PrinterSettings = BarcodePrint.Model.PrinterSettingsSnapshot.FromPaperClass(PRINT_INFO_CLASS.PrinterInfo),
                                MultiColumnSettingsData = mainForm.GetAppSettings().MultiColumnSettingsData
                            };

                            if (BarcodePrint.Model.LayoutData.SaveToFile(layout, sfd.FileName))
                            {
                                MessageBox.Show("레이아웃 설정이 성공적으로 내보내졌습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"내보내기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            bottomPanel.Controls.Add(btnExportLayout);



            // 템플릿 가져오기 버튼 추가
            RoundedButton btnImportLayout = new RoundedButton { 
                Text = "템플릿 가져오기", Size = new Size(130, 40), 
                Location = new Point(270, 10),
                Style = RoundedButton.ButtonStyle.Secondary,
                Font = new Font("맑은 고딕", 14, FontStyle.Regular, GraphicsUnit.Pixel) 
            };

            btnImportLayout.Click += (s, e) => {
                try
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Label Layout (*.lml)|*.lml|JSON Files (*.json)|*.json";
                        ofd.Title = "레이아웃 설정 가져오기";
                        ofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;

                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            BarcodePrint.Model.LayoutData layout = BarcodePrint.Model.LayoutData.LoadFromFile(ofd.FileName);
                            if (layout != null)
                            {
                                mainForm.GetAppSettings().ColumnPositionData = layout.ColumnPositionData;
                                mainForm.GetAppSettings().PaperTypeColumnPositionData = layout.PaperTypeColumnPositionData;
                                mainForm.GetAppSettings().TitleSize = layout.TitleSize;
                                mainForm.GetAppSettings().TitleOffsetY = layout.TitleOffsetY;
                                mainForm.GetAppSettings().TitleOffsetX = layout.TitleOffsetX;
                                mainForm.GetAppSettings().PaperTypeTitleData = layout.PaperTypeTitleData;
                                mainForm.GetAppSettings().MultiColumnSettingsData = layout.MultiColumnSettingsData ?? "";
                                mainForm.GetAppSettings().PaperWidth = layout.PaperWidth;
                                mainForm.GetAppSettings().PaperHeight = layout.PaperHeight;
                                mainForm.GetAppSettings().BarcodeType = layout.BarcodeType ?? "QR_CODE";
                                mainForm.GetAppSettings().IsRotated = layout.IsRotated;

                                PRINT_INFO_CLASS.PrinterInfo.BarcodeType = layout.BarcodeType ?? "QR_CODE";
                                PRINT_INFO_CLASS.PrinterInfo.Paper_Width = layout.PaperWidth;
                                PRINT_INFO_CLASS.PrinterInfo.Paper_Height = layout.PaperHeight;
                                PRINT_INFO_CLASS.PrinterInfo.SelectedPaperType = layout.PaperTypeName;
                                PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = layout.IsRotated;
                                PRINT_INFO_CLASS.PrinterInfo.offset_X = layout.GlobalOffsetX;
                                PRINT_INFO_CLASS.PrinterInfo.offset_Y = layout.GlobalOffsetY;
                                PRINT_INFO_CLASS.PrinterInfo.nGAP = layout.PaperGap;

                                if (layout.PrinterSettings != null)
                                {
                                    layout.PrinterSettings.ApplyTo(PRINT_INFO_CLASS.PrinterInfo);
                                }

                                mainForm.GetAppSettings().Save();

                                mainForm.LoadColumnPositionSettings();
                                this.settings = mainForm.GetColumnPositionSettings().Select(col => new ColumnPositionSetting
                                {
                                    ColumnIndex = col.ColumnIndex,
                                    IsEnabled = col.IsEnabled,
                                    Size = col.Size,
                                    OffsetX = col.OffsetX,
                                    OffsetY = col.OffsetY,
                                    UsageType = col.UsageType,
                                    BarcodeType = col.BarcodeType,
                                    ShowBarcodeText = col.ShowBarcodeText,
                                    BarcodeTextOffsetX = col.BarcodeTextOffsetX,
                                    BarcodeTextOffsetY = col.BarcodeTextOffsetY,
                                    BarcodeTextSize = col.BarcodeTextSize,
                                    FontFamily = col.FontFamily,
                                    IsBold = col.IsBold,
                                    CustomWidth = col.CustomWidth,
                                    BarcodeHeight = col.BarcodeHeight,
                                    RotateAngle = col.RotateAngle,
                                    Alignment = col.Alignment
                                }).ToList();

                                this.isInitializing = true;
                                if (numPaperWidth != null) numPaperWidth.Value = (decimal)layout.PaperWidth;
                                if (numPaperHeight != null) numPaperHeight.Value = (decimal)layout.PaperHeight;
                                if (chkPaperRotate != null) chkPaperRotate.Checked = layout.IsRotated;
                                if (cbBarcodeType != null)
                                {
                                    int bIdx = cbBarcodeType.FindStringExact(layout.BarcodeType ?? "QR_CODE");
                                    if (bIdx >= 0) cbBarcodeType.SelectedIndex = bIdx;
                                }
                                this.isInitializing = false;

                                OnPaperSizeChanged();
                                UpdatePropertyControls(0);
                                pbPreview.Invalidate();

                                MessageBox.Show("레이아웃 설정을 성공적으로 가져왔습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"가져오기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            bottomPanel.Controls.Add(btnImportLayout);



            // 폼 리사이즈 시 버튼 위치 유지 (고정 크기이므로 초기 위치만 잘 잡으면 됨)
            bottomPanel.Resize += (s, e) => {
                btnCancel.Left = bottomPanel.Width - btnCancel.Width - 20;
                btnSave.Left = btnCancel.Left - btnSave.Width - 10;
                btnReset.Left = 20;
                btnExportLayout.Left = btnReset.Right + 15;
                btnImportLayout.Left = btnExportLayout.Right + 10;
            };







            this.ResumeLayout(false);



        }







        /// <summary>



        /// 시스템에 설치된 전체 폰트 목록을 ComboBox에 로드합니다.



        /// </summary>



        private void LoadSystemFonts()



        {



            cbFont.Items.Clear();



            try



            {



                using (var installedFonts = new InstalledFontCollection())



                {



                    var fontNames = installedFonts.Families



                        .Select(f => f.Name)



                        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)



                        .ToList();



                    foreach (var name in fontNames)



                        cbFont.Items.Add(name);



                }



            }



            catch



            {



                // 폰트 로드 실패 시 기본 폰트만 표시



                cbFont.Items.AddRange(new string[] { "맑은 고딕", "굴림", "돋움", "Arial" });



            }



            cbFont.SelectedIndexChanged += (s, e) => { if (cbFont.SelectedItem != null) { settings[selectedIndex].FontFamily = cbFont.SelectedItem.ToString(); pbPreview.Invalidate(); mainForm.UpdateColumnPositionSettings(settings, false); } };



        }







        private string GetItemDisplayText(int index)



        {



            var s = settings[index];



            string text = (index < previewTexts.Count) ? previewTexts[index] : (index == 3 ? "BARCODE" : "TEXT");



            if (string.IsNullOrEmpty(text))



            {



                text = (index == 3 && s.UsageType == "바코드" ? "BARCODE" : cbItemSelector.Items[index].ToString());



            }



            // 공통문구(Index 4): Title 비활성화 시 "표시없음" 대신 "공통문구"로 표시 (에디터 폼용)



            if (index == 4 && text == "표시없음")



                return "공통문구";



            return text;



        }







        private void UpdatePropertyControls(int index)



        {



            selectedIndex = index;



            var s = settings[index];







            string itemName = cbItemSelector.Items[index].ToString();



            lblItemTitle.Text = $"편집 중: {itemName}";







            numX.Value = Math.Max(numX.Minimum, Math.Min(numX.Maximum, (decimal)s.OffsetX));



            numY.Value = Math.Max(numY.Minimum, Math.Min(numY.Maximum, (decimal)s.OffsetY));



            numSize.Value = Math.Max(numSize.Minimum, Math.Min(numSize.Maximum, (decimal)s.Size));



            numWidth.Value = Math.Max(numWidth.Minimum, Math.Min(numWidth.Maximum, (decimal)s.CustomWidth));



            numHeight.Value = Math.Max(numHeight.Minimum, Math.Min(numHeight.Maximum, (decimal)s.BarcodeHeight));







            int fontIdx = !string.IsNullOrEmpty(s.FontFamily) ? cbFont.FindStringExact(s.FontFamily) : -1;



            if (fontIdx >= 0)



                cbFont.SelectedIndex = fontIdx;



            else



                cbFont.SelectedIndex = cbFont.Items.Count > 0 ? 0 : -1;







            chkBold.Checked = s.IsBold;







            if (cbRotate != null)



            {



                int rotateIdx = s.RotateAngle / 90;



                if (rotateIdx >= 0 && rotateIdx < cbRotate.Items.Count)



                    cbRotate.SelectedIndex = rotateIdx;



                else



                    cbRotate.SelectedIndex = 0;



            }







            // 유형 선택 UI (컬럼4 - 인덱스 3일 때만 표시)



            bool canChangeUsage = (index == 3);



            lblUsage.Visible = canChangeUsage;



            cbUsageType.Visible = canChangeUsage;



            if (canChangeUsage)



            {



                int usageIdx = cbUsageType.FindStringExact(s.UsageType ?? "텍스트");



                cbUsageType.SelectedIndex = usageIdx >= 0 ? usageIdx : 0;



            }







            // 바코드 옵션 표시 여부



            bool isBarcode = s.UsageType == "바코드";



            panelBarcodeOptions.Visible = isBarcode;







            if (cbAlignment != null)



            {



                int alignIdx = cbAlignment.FindStringExact(s.Alignment ?? "Left");



                cbAlignment.SelectedIndex = alignIdx >= 0 ? alignIdx : 0;



                cbAlignment.Visible = !isBarcode;



                lblAlignment.Visible = !isBarcode;



            }







            // 높이 조절 값은 바코드로 선택되었을 때만 노출



            if (lblHeight != null) lblHeight.Visible = isBarcode;



            if (numHeight != null) numHeight.Visible = isBarcode;



            if (isBarcode)



            {



                int idx = cbBarcodeType.FindStringExact(s.BarcodeType ?? "QR_CODE");



                if (idx >= 0) cbBarcodeType.SelectedIndex = idx;



                else cbBarcodeType.SelectedIndex = 0;



                



                chkRemoveBarcodeText.Checked = !s.ShowBarcodeText; // 반전된 상태 반영



                numBarcodeTextX.Value = Math.Max(numBarcodeTextX.Minimum, Math.Min(numBarcodeTextX.Maximum, (decimal)s.BarcodeTextOffsetX));



                numBarcodeTextY.Value = Math.Max(numBarcodeTextY.Minimum, Math.Min(numBarcodeTextY.Maximum, (decimal)s.BarcodeTextOffsetY));



                numBarcodeTextSize.Value = s.BarcodeTextSize > 0 ? (decimal)s.BarcodeTextSize : (decimal)Math.Max(8, (int)(s.Size * 0.6f));



                ToggleBarcodeTextControls();



                



                cbFont.Enabled = false;



                chkBold.Enabled = false;



            }



            else



            {



                cbFont.Enabled = true;



                chkBold.Enabled = true;



            }







            UpdateControlsEnabledState(true);

            UpdateBarcodeControlsState(s);

            pbPreview.Invalidate();

        }

        private void UpdateBarcodeControlsState(ColumnPositionSetting s)
        {
            bool isBarcode = s.UsageType == "바코드";
            bool isQr = isBarcode && (s.BarcodeType ?? "QR_CODE") == "QR_CODE";

            if (isBarcode)
            {
                if (isQr)
                {
                    if (lblSize != null) lblSize.Visible = true;
                    if (numSize != null) numSize.Visible = true;
                    if (lblWidth != null) lblWidth.Visible = false;
                    if (numWidth != null) numWidth.Visible = false;
                    if (lblHeight != null) lblHeight.Visible = false;
                    if (numHeight != null) numHeight.Visible = false;
                }
                else
                {
                    if (lblSize != null) lblSize.Visible = false;
                    if (numSize != null) numSize.Visible = false;
                    if (lblWidth != null) lblWidth.Visible = true;
                    if (numWidth != null) numWidth.Visible = true;
                    if (lblHeight != null) lblHeight.Visible = true;
                    if (numHeight != null) numHeight.Visible = true;
                }
            }
            else
            {
                if (lblSize != null) lblSize.Visible = true;
                if (numSize != null) numSize.Visible = true;
                if (lblWidth != null) lblWidth.Visible = true;
                if (numWidth != null) numWidth.Visible = true;
                if (lblHeight != null) lblHeight.Visible = false;
                if (numHeight != null) numHeight.Visible = false;
            }
        }











        private void ToggleBarcodeTextControls()



        {



            bool show = !chkRemoveBarcodeText.Checked; // 반전된 상태 반영



            numBarcodeTextX.Visible = numBarcodeTextY.Visible = numBarcodeTextSize.Visible = show;



            foreach (Control c in panelBarcodeOptions.Controls)



                if (c is Label lbl && lbl.Tag as string == "BarcodeText")



                    c.Visible = show;



        }







        private void OnPaperSizeChanged()



        {

            if (isInitializing) return;



            int w = (int)numPaperWidth.Value;



            int h = (int)numPaperHeight.Value;



            if (w <= 0 || h <= 0) return;



            PRINT_INFO_CLASS.PrinterInfo.Paper_Width = w;



            PRINT_INFO_CLASS.PrinterInfo.Paper_Height = h;



            



            // 회전 여부에 따라 실제 캔버스 크기 결정



            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



            {



                labelWidthMm = h;



                labelHeightMm = w;



            }



            else



            {



                labelWidthMm = w;



                labelHeightMm = h;



            }







            string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");



            PRINT_INFO_CLASS.SaveToFile(settingsPath);



            mainForm?.UpdateLabelSizeInfo();



            mainForm?.OnPreviewEvent();



            RefreshPreviewSize();



        }







        private void RefreshPreviewSize()



        {



            float dpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203f;



            SCALE = dpi / 25.4f;



            float lw = labelWidthMm, lh = labelHeightMm;



            



            const int MAX_PREVIEW_WIDTH = 550;



            const int MAX_PREVIEW_HEIGHT = 450;







            float labelAspect = lh / lw;



            int pbW, pbH;







            if (labelAspect < 1) // 가로가 더 긴 경우



            {



                pbW = Math.Min((int)lw * 4, MAX_PREVIEW_WIDTH);



                pbH = (int)(pbW * labelAspect);



                if (pbH > MAX_PREVIEW_HEIGHT)



                {



                    pbH = MAX_PREVIEW_HEIGHT;



                    pbW = (int)(pbH / labelAspect);



                }



            }



            else // 세로가 더 긴 경우



            {



                pbH = Math.Min((int)lh * 4, MAX_PREVIEW_HEIGHT);



                pbW = (int)(pbH / labelAspect);



                if (pbW > MAX_PREVIEW_WIDTH)



                {



                    pbW = MAX_PREVIEW_WIDTH;



                    pbH = (int)(pbW * labelAspect);



                }



            }







            // 최소 크기 보장



            pbW = Math.Max(pbW, 100);



            pbH = Math.Max(pbH, 80);







            // zoomFactor 적용



            pbW = (int)(pbW * zoomFactor);



            pbH = (int)(pbH * zoomFactor);







            pbPreview.Size = new Size(pbW, pbH);



            if (pbPreview.Parent is Panel paperShadow)



                paperShadow.Size = new Size(pbW + 2, pbH + 2);







            LayoutCenteringPanel();



            pbPreview.Invalidate();



        }







        private void LayoutCenteringPanel()



        {



            if (previewContainer == null || pbPreview == null || centeringPanel == null) return;



            if (!(pbPreview.Parent is Panel paperShadow)) return;







            int shadowW = pbPreview.Width + 2;



            int shadowH = pbPreview.Height + 2;







            int targetW = Math.Max(previewContainer.ClientRectangle.Width, shadowW + 20);



            int targetH = Math.Max(previewContainer.ClientRectangle.Height, shadowH + 20);







            centeringPanel.Size = new Size(targetW, targetH);



            centeringPanel.Location = new Point(0, 0);







            paperShadow.Left = (centeringPanel.Width - shadowW) / 2;



            paperShadow.Top = (centeringPanel.Height - shadowH) / 2;



        }







        private void PbPreview_MouseWheel(object sender, MouseEventArgs e)



        {



            if ((ModifierKeys & Keys.Control) == Keys.Control)



            {



                if (e is HandledMouseEventArgs he)



                {



                    he.Handled = true;



                }







                float oldZoom = zoomFactor;



                if (e.Delta > 0)



                {



                    zoomFactor = Math.Min(zoomFactor + 0.1f, 5.0f);



                }



                else



                {



                    zoomFactor = Math.Max(zoomFactor - 0.1f, 0.2f);



                }







                if (Math.Abs(oldZoom - zoomFactor) > 0.001f)



                {



                    RefreshPreviewSize();



                }



            }



        }







        private void UpdateControlsEnabledState(bool enabled)



        {



            lblSelect.Enabled = enabled;



            cbItemSelector.Enabled = enabled;



            lblItemTitle.Enabled = enabled;



            numX.Enabled = enabled;



            numY.Enabled = enabled;



            numSize.Enabled = enabled;



            numWidth.Enabled = enabled;



            cbFont.Enabled = enabled;



            chkBold.Enabled = enabled;



            chkRemoveBarcodeText.Enabled = enabled;



            pbPreview.Cursor = enabled ? Cursors.Hand : Cursors.Default;



            



            pbPreview.Invalidate(); // 미리보기 선택 효과 제거를 위해 갱신



        }







        private void ApplyAutoDefaults()



        {



            // Calculate dynamic offsets based on actual label height (base 145mm)



            int titleY = (int)(labelHeightMm * 0.17f);



            int jobY = (int)(labelHeightMm * 0.27f);



            int nameY = (int)(labelHeightMm * 0.38f);



            int barcodeY = (int)(labelHeightMm * 0.75f);



            



            // Adjust sizes proportionally



            float scaleRatio = labelHeightMm / 145f;



            int fontSize = Math.Max(6, (int)(12 * scaleRatio));



            int barcodeSize = Math.Max(12, (int)(25 * scaleRatio));



            int barcodeHeight = Math.Max(5, (int)(15 * scaleRatio));



            int commonTextY = (int)(labelHeightMm * 0.05f);







            for (int i = 0; i < settings.Count; i++)



            {



                var s = settings[i];



                s.IsEnabled = true;



                s.OffsetX = 0;



                s.CustomWidth = 0;



                s.RotateAngle = 0;



                s.Alignment = "Left";



                s.FontFamily = "맑은 고딕";



                s.ShowBarcodeText = false;



                s.BarcodeTextOffsetX = 0;



                s.BarcodeTextOffsetY = 0;



                s.BarcodeTextSize = 9;







                if (i == 0) // 품명



                {



                    s.OffsetY = titleY;



                    s.Size = fontSize;



                    s.IsBold = true;



                    s.UsageType = "텍스트";



                }



                else if (i == 1) // 가격



                {



                    s.OffsetY = jobY;



                    s.Size = fontSize;



                    s.IsBold = true;



                    s.UsageType = "텍스트";



                }



                else if (i == 2) // 비고



                {



                    s.OffsetY = nameY;



                    s.Size = fontSize;



                    s.IsBold = true;



                    s.UsageType = "텍스트";



                }



                else if (i == 3) // 바코드



                {



                    s.OffsetY = barcodeY;



                    s.Size = barcodeSize;



                    s.BarcodeHeight = barcodeHeight;



                    s.IsBold = false;



                    s.UsageType = "바코드";



                    s.BarcodeType = "QR_CODE";



                }



                else if (i == 4) // 공통문구



                {



                    s.OffsetY = commonTextY;



                    s.Size = (int)(fontSize * 1.2);



                    s.IsBold = true;



                    s.UsageType = "텍스트";



                }



            }







            UpdatePropertyControls(selectedIndex);



        }







        private Font GetPreviewFont(ColumnPositionSetting s)



        {



            // 입력값(pt 단위)을 렌더링 픽셀로 변환: pt * (dpi / 72)



            float dpi = SCALE * 25.4f;



            float fontSizeInPixels = Math.Max(8, s.Size * dpi / 72f);



            string familyName = s.FontFamily ?? "맑은 고딕";



            FontStyle style = s.IsBold ? FontStyle.Bold : FontStyle.Regular;



            try



            {



                var family = new FontFamily(familyName);



                if (!family.IsStyleAvailable(style))



                {



                    if (family.IsStyleAvailable(FontStyle.Regular)) style = FontStyle.Regular;



                    else if (family.IsStyleAvailable(FontStyle.Bold)) style = FontStyle.Bold;



                    else style = FontStyle.Regular; // Font 생성 시 예외 가능성 있음



                }



                return new Font(family, fontSizeInPixels, style, GraphicsUnit.Pixel);



            }



            catch



            {



                return new Font("맑은 고딕", fontSizeInPixels, FontStyle.Regular, GraphicsUnit.Pixel);



            }



        }







        private void PbPreview_Paint(object sender, PaintEventArgs e)



        {



            int printW = (int)(labelWidthMm * SCALE);



            int printH = (int)(labelHeightMm * SCALE);



            if (printW < 1 || printH < 1) return;







            using (Bitmap offscreen = new Bitmap(printW, printH))



            using (Graphics gOff = Graphics.FromImage(offscreen))



            {



                gOff.Clear(Color.White);



                gOff.SmoothingMode = SmoothingMode.AntiAlias;



                gOff.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;







                DrawPreviewContent(gOff, printW, printH);







                var dest = new Rectangle(0, 0, pbPreview.Width, pbPreview.Height);



                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;



                e.Graphics.DrawImage(offscreen, dest);



            }



        }







        private void DrawPreviewContent(Graphics g, int canvasW, int canvasH)



        {



            // Draw each item (인쇄와 동일한 mmToPixel 스케일 사용)



            for (int i = 0; i < settings.Count; i++)



            {



                var s = settings[i];



                // 바코드(3), 공통문구(4), 현재 선택된 항목은 IsEnabled와 무관하게 에디터에 항상 표시 (레이아웃 조정 용도)



                if (!s.IsEnabled && i != 3 && i != 4 && i != selectedIndex) continue;







                string textValue = GetItemDisplayText(i);







                float pxX = (labelWidthMm / 2f + s.OffsetX) * SCALE;



                float pxY = s.OffsetY * SCALE;







                if (s.UsageType == "바코드")



                {



                    // Draw Barcode Placeholder or Real Barcode



                    float barcodeWidth, barcodeHeight;



                    string bType = s.BarcodeType ?? "QR_CODE";



                    if (bType == "QR_CODE")
                    {
                        barcodeWidth = s.Size * (SCALE * 25.4f) / 72f;
                        barcodeHeight = barcodeWidth; // QR preserves square
                    }



                    else



                    {



                        if (s.CustomWidth > 0) {



                            barcodeWidth = s.CustomWidth * SCALE;



                        }



                        else {



                            barcodeWidth = s.Size * (SCALE * 25.4f) / 72f;



                        }



                        barcodeHeight = s.BarcodeHeight * SCALE;



                    }



                    



                    if (barcodeWidth < 3) barcodeWidth = 3;



                    if (barcodeHeight < 3) barcodeHeight = 3;







                    float centerY = pxY + barcodeHeight / 2f;



                    RectangleF rect = new RectangleF(-barcodeWidth / 2f, -barcodeHeight / 2f, barcodeWidth, barcodeHeight);



                    



                    string barcodeData = textValue;



                    if (string.IsNullOrEmpty(barcodeData) || barcodeData == "BARCODE") barcodeData = "12345678";



                    barcodeData = ProcessBarcodeText(barcodeData, bType);







                    var state = g.Save();



                    g.TranslateTransform(pxX, centerY);



                    g.RotateTransform(s.RotateAngle);







                    try



                    {



                        var barcodeWriter = new ZXing.BarcodeWriter



                        {



                            Format = GetBarcodeFormat(bType),



                            Options = new ZXing.Common.EncodingOptions



                            {



                                Width = (int)barcodeWidth,



                                Height = (int)barcodeHeight,



                                Margin = (bType == "QR_CODE" ? 0 : 5),



                                PureBarcode = true



                            }



                        };



                        



                        try 



                        {



                            using (var bitmap = barcodeWriter.Write(barcodeData))



                            {



                                g.DrawImage(bitmap, rect);



                            }



                        }



                        catch



                        {



                            // If rect is too small to encode the data, try with 0x0 (minimum possible size) and scale it down visually



                            barcodeWriter.Options.Width = 0;



                            barcodeWriter.Options.Height = 0;



                            using (var bitmap = barcodeWriter.Write(barcodeData))



                            {



                                g.DrawImage(bitmap, rect);



                            }



                        }



                    }



                    catch



                    {



                        g.FillRectangle(Brushes.WhiteSmoke, rect);



                        g.DrawRectangle(new Pen(Color.Red, 2), rect.X, rect.Y, rect.Width, rect.Height);



                        g.DrawLine(Pens.Red, rect.X, rect.Y, rect.Right, rect.Bottom);



                        g.DrawLine(Pens.Red, rect.Right, rect.Y, rect.X, rect.Bottom);



                    }



                    



                    if (selectedIndex == i)



                    {



                        g.DrawRectangle(new Pen(Color.Red, 2), rect.X, rect.Y, rect.Width, rect.Height);



                        g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Top - 5, 10, 10);



                        g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Top - 5, 10, 10);



                        g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Bottom - 5, 10, 10);



                        g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Bottom - 5, 10, 10);



                        



                        // 가로 조절 핸들 (텍스트 및 바코드 모두 적용)



                        g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Top + rect.Height/2 - 5, 10, 10);



                        g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Top + rect.Height/2 - 5, 10, 10);



                        



                        // 세로 조절 핸들 (추가)



                        g.FillRectangle(Brushes.Blue, rect.Left + rect.Width/2 - 5, rect.Top - 5, 10, 10);



                        g.FillRectangle(Brushes.Blue, rect.Left + rect.Width/2 - 5, rect.Bottom - 5, 10, 10);



                    }







                    // 바코드 값 텍스트 표시



                    if (s.ShowBarcodeText)
                    {
                        float textSizeMm = s.BarcodeTextSize > 0 ? s.BarcodeTextSize : Math.Max(8, s.Size * 0.6f);
                        float fontSizePx = Math.Max(8, textSizeMm * (SCALE * 25.4f) / 72f);
                        float offsetXPx = s.BarcodeTextOffsetX * SCALE;
                        float offsetYPx = s.BarcodeTextOffsetY * SCALE;
                        float textY = rect.Bottom + 5 + offsetYPx;
                        float textX = offsetXPx;
                        
                        using (Font barcodeTextFont = new Font("Arial", fontSizePx, FontStyle.Bold, GraphicsUnit.Pixel))
                        using (SolidBrush brush = new SolidBrush(Color.Black))
                        using (StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, Trimming = StringTrimming.Word })
                        {
                            System.Drawing.SizeF measuredSize = g.MeasureString(barcodeData, barcodeTextFont);
                            float textWidth = Math.Max(rect.Width, measuredSize.Width + 10);
                            System.Drawing.RectangleF textRect = new System.Drawing.RectangleF(textX - textWidth / 2f, textY, textWidth, fontSizePx * 5);
                            g.DrawString(barcodeData, barcodeTextFont, brush, textRect, sf);
                        }
                    }







                    g.Restore(state);



                }



                else



                {



                    StringAlignment alignVal = StringAlignment.Near;



                    if (s.Alignment == "Center") alignVal = StringAlignment.Center;



                    else if (s.Alignment == "Right") alignVal = StringAlignment.Far;







                    using (Font font = GetPreviewFont(s))



                    using (StringFormat sf = new StringFormat { Alignment = alignVal, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.LineLimit, Trimming = StringTrimming.Word })



                    {



                        SizeF size = g.MeasureString(textValue, font);



                        



                        // 가로 폭 제한 적용



                        float wrapWidth = s.CustomWidth > 0 ? s.CustomWidth * SCALE : size.Width;



                        if (s.CustomWidth > 0) {



                            size = g.MeasureString(textValue, font, (int)wrapWidth, sf);



                        }



                        



                        float textHeight = Math.Max(size.Height, 20);



                        float centerY = pxY + textHeight / 2f;



                        RectangleF rect = new RectangleF(-wrapWidth / 2f, -textHeight / 2f, wrapWidth, textHeight);







                        var state = g.Save();



                        g.TranslateTransform(pxX, centerY);



                        g.RotateTransform(s.RotateAngle);







                        if (selectedIndex == i)



                        {



                            g.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Red)), rect);



                            g.DrawRectangle(Pens.Red, rect.X, rect.Y, rect.Width, rect.Height);



                            



                            // 핸들 그리기



                            g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Top - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Top - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Bottom - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Bottom - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Left - 5, rect.Top + rect.Height/2 - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Right - 5, rect.Top + rect.Height/2 - 5, 10, 10);



                            



                            // 세로 조절 핸들 (추가)



                            g.FillRectangle(Brushes.Blue, rect.Left + rect.Width/2 - 5, rect.Top - 5, 10, 10);



                            g.FillRectangle(Brushes.Blue, rect.Left + rect.Width/2 - 5, rect.Bottom - 5, 10, 10);



                        }



                        g.DrawString(textValue, font, Brushes.Black, rect, sf);







                        g.Restore(state);



                    }



                }



            }



        }







        private void PbPreview_MouseDown(object sender, MouseEventArgs e)



        {



            int printW = (int)(labelWidthMm * SCALE);



            int printH = (int)(labelHeightMm * SCALE);



            float scaleX = printW > 0 ? (float)printW / pbPreview.Width : 1f;



            float scaleY = printH > 0 ? (float)printH / pbPreview.Height : 1f;



            PointF pt = new PointF(e.X * scaleX, e.Y * scaleY);







            // Hit test (인쇄 좌표계로 변환)



            for (int i = settings.Count - 1; i >= 0; i--)



            {



                var s = settings[i];



                if (!s.IsEnabled && i != 3 && i != 4) continue;







                float pxX = (labelWidthMm / 2f + s.OffsetX) * SCALE;



                float pxY = s.OffsetY * SCALE;



                float w, h;







                if (s.UsageType == "바코드") 



                { 



                    string bType = s.BarcodeType ?? "QR_CODE";



                    if (bType == "QR_CODE")



                    {



                        w = s.Size * (SCALE * 25.4f) / 72f;   h = w;



                        if (s.CustomWidth > 0) {



                            w = s.CustomWidth * SCALE;



                            h = w;



                        }



                    }



                    else



                    {



                        w = (s.CustomWidth > 0 ? s.CustomWidth : s.Size) * SCALE;



                        h = s.BarcodeHeight * SCALE;



                    }



                    



                    if (w < 3) w = 3;



                    if (h < 3) h = 3;



                }



                else



                {



                    using (Graphics g = pbPreview.CreateGraphics())



                    {



                        string text = GetItemDisplayText(i);



                        using (Font font = GetPreviewFont(s))



                        {



                            // 가로폭 커스텀 적용하여 높이 측정



                            if (s.CustomWidth > 0) {



                                w = s.CustomWidth * SCALE;



                                SizeF size = g.MeasureString(text, font, (int)w);



                                h = Math.Max(size.Height, 20);



                            } else {



                                SizeF size = g.MeasureString(text, font);



                                w = size.Width; h = size.Height;



                            }



                        }



                    }



                }







                // 로컬 좌표계 변환 및 히트 테스트



                float centerY = pxY + h / 2f;



                PointF localPt = TransformToLocal(pt, pxX, centerY, s.RotateAngle);



                RectangleF localRect = new RectangleF(-w / 2f, -h / 2f, w, h);



                



                // 크기 조절 핸들 판정 (4개 모서리 및 측면)



                if (selectedIndex == i)



                {



                    RectangleF tl = new RectangleF(localRect.Left - 10, localRect.Top - 10, 20, 20);



                    RectangleF tr = new RectangleF(localRect.Right - 10, localRect.Top - 10, 20, 20);



                    RectangleF bl = new RectangleF(localRect.Left - 10, localRect.Bottom - 10, 20, 20);



                    RectangleF br = new RectangleF(localRect.Right - 10, localRect.Bottom - 10, 20, 20);



                    RectangleF l = new RectangleF(localRect.Left - 10, localRect.Top + localRect.Height / 2f - 10, 20, 20);



                    RectangleF r = new RectangleF(localRect.Right - 10, localRect.Top + localRect.Height / 2f - 10, 20, 20);



                    RectangleF t = new RectangleF(localRect.Left + localRect.Width / 2f - 10, localRect.Top - 10, 20, 20);



                    RectangleF b = new RectangleF(localRect.Left + localRect.Width / 2f - 10, localRect.Bottom - 10, 20, 20);



                    



                    if (tl.Contains(localPt)) activeHandle = ResizingHandle.TopLeft;



                    else if (tr.Contains(localPt)) activeHandle = ResizingHandle.TopRight;



                    else if (bl.Contains(localPt)) activeHandle = ResizingHandle.BottomLeft;



                    else if (br.Contains(localPt)) activeHandle = ResizingHandle.BottomRight;



                    else if (l.Contains(localPt)) activeHandle = ResizingHandle.Left;



                    else if (r.Contains(localPt)) activeHandle = ResizingHandle.Right;



                    else if (t.Contains(localPt)) activeHandle = ResizingHandle.Top;



                    else if (b.Contains(localPt)) activeHandle = ResizingHandle.Bottom;



                    else activeHandle = ResizingHandle.None;







                    if (activeHandle != ResizingHandle.None)



                    {



                        isResizing = true;



                        isDragging = false;



                        dragStartMousePos = e.Location;



                        dragStartLocalPos = localPt; // 로컬 드래그 원점 기억



                        resizeStartSize = s.Size;



                        resizeStartWidth = s.CustomWidth > 0 ? s.CustomWidth : (int)(w / SCALE);



                        resizeStartBarcodeHeight = s.BarcodeHeight;



                        return;



                    }



                }







                RectangleF hitRect = localRect;



                hitRect.Inflate(8, 8);



                if (hitRect.Contains(localPt))



                {



                    if (selectedIndex != i)



                    {



                        cbItemSelector.SelectedIndex = i;



                    }



                    isResizing = false;



                    isDragging = true;



                    lastMousePos = pt;



                    dragAccumulatorX = dragAccumulatorY = 0f;



                    break;



                }



            }



        }







        private void PbPreview_MouseMove(object sender, MouseEventArgs e)



        {



            if (isResizing)



            {



                var s = settings[selectedIndex];



                



                // 마우스 현재 위치를 로컬 인쇄 좌표계로 변환



                int printW = (int)(labelWidthMm * SCALE);



                int printH = (int)(labelHeightMm * SCALE);



                float scaleX = printW > 0 ? (float)printW / pbPreview.Width : 1f;



                float scaleY = printH > 0 ? (float)printH / pbPreview.Height : 1f;



                PointF pt = new PointF(e.X * scaleX, e.Y * scaleY);



                



                float pxX = (labelWidthMm / 2f + s.OffsetX) * SCALE;



                float pxY = s.OffsetY * SCALE;



                float h = 0;



                if (s.UsageType == "바코드")



                {



                    string bType = s.BarcodeType ?? "QR_CODE";



                    if (bType == "QR_CODE")



                    {



                        float w_temp = s.Size * (SCALE * 25.4f) / 72f;



                        if (s.CustomWidth > 0) w_temp = s.CustomWidth * SCALE;



                        h = w_temp;



                    }



                    else



                    {



                        h = s.BarcodeHeight * SCALE;



                    }



                    if (h < 3) h = 3;



                }



                else



                {



                    using (Graphics graphics = pbPreview.CreateGraphics())



                    {



                        string text = GetItemDisplayText(selectedIndex);



                        using (Font font = GetPreviewFont(s))



                        {



                            if (s.CustomWidth > 0) {



                                float w_temp = s.CustomWidth * SCALE;



                                SizeF size = graphics.MeasureString(text, font, (int)w_temp);



                                h = Math.Max(size.Height, 20);



                            } else {



                                SizeF size = graphics.MeasureString(text, font);



                                h = size.Height;



                            }



                        }



                    }



                }



                float centerY = pxY + h / 2f;



                PointF localPt = TransformToLocal(pt, pxX, centerY, s.RotateAngle);



                



                // 로컬 좌표계 델타 (mm 단위로 환산)



                float localDx = (localPt.X - dragStartLocalPos.X) / SCALE;



                float localDy = (localPt.Y - dragStartLocalPos.Y) / SCALE;







                if (activeHandle == ResizingHandle.Left || activeHandle == ResizingHandle.Right)
                {
                    // 가로폭 조절 (텍스트/바코드 공통)
                    float dx = localDx;
                    if (activeHandle == ResizingHandle.Left) dx = -dx;

                    int newWidth = (int)(resizeStartWidth + dx * 2); // 센터 정렬이므로 양쪽으로 늘어남

                    if (s.UsageType == "바코드" && (s.BarcodeType ?? "QR_CODE") == "QR_CODE")
                    {
                        // QR코드일 때 가로 조절 시 Size 조절
                        int newSize = (int)(newWidth * 72f / 25.4f);
                        s.Size = Math.Max(5, Math.Min(300, newSize)); // 최저값 5로 적용
                        numSize.Value = (decimal)s.Size;
                        s.CustomWidth = 0;
                    }
                    else
                    {
                        s.CustomWidth = Math.Max(5, Math.Min(500, newWidth)); // 최저값 5로 적용
                        numWidth.Value = (decimal)s.CustomWidth;
                    }
                }
                else if (activeHandle == ResizingHandle.Top || activeHandle == ResizingHandle.Bottom)
                {
                    // 세로폭 조절
                    float dy = localDy;
                    if (activeHandle == ResizingHandle.Top) dy = -dy;

                    if (s.UsageType == "바코드" && (s.BarcodeType ?? "QR_CODE") != "QR_CODE")
                    {
                        // 1D 바코드는 세로만 조절 (최소 높이 1)
                        s.BarcodeHeight = Math.Max(1, Math.Min(200, (int)(resizeStartBarcodeHeight + dy)));
                        numHeight.Value = (decimal)s.BarcodeHeight;
                    }
                    else if (s.UsageType == "바코드" && (s.BarcodeType ?? "QR_CODE") == "QR_CODE")
                    {
                        // QR코드일 때 세로 조절 시 Size 조절
                        int newSize = (int)(resizeStartSize + dy);
                        s.Size = Math.Max(5, Math.Min(300, newSize)); // 최저값 5로 적용
                        numSize.Value = (decimal)s.Size;
                        s.CustomWidth = 0;
                    }
                    else
                    {
                        // 텍스트는 전체 사이즈 조절
                        int newSize = (int)(resizeStartSize + dy);
                        s.Size = Math.Max(3, Math.Min(300, newSize));
                        numSize.Value = (decimal)s.Size;
                    }
                }
                else
                {
                    // 대각선 크기 조절
                    float dx_raw = localPt.X - dragStartLocalPos.X;
                    float dy_raw = localPt.Y - dragStartLocalPos.Y;

                    if (s.UsageType == "바코드")
                    {
                        string bType = s.BarcodeType ?? "QR_CODE";
                        if (bType == "QR_CODE")
                        {
                            // QR 코드는 정비율 유지 (가로 이동량 기준)
                            float delta = (dx_raw / SCALE) * ((activeHandle == ResizingHandle.TopRight || activeHandle == ResizingHandle.BottomRight) ? 1 : -1) * 2;
                            int newWidth = (int)(resizeStartWidth + delta);
                            
                            // QR코드는 Size 조절
                            int newSize = (int)(newWidth * 72f / 25.4f);
                            s.Size = Math.Max(5, Math.Min(300, newSize)); // 최저값 5로 적용
                            numSize.Value = (decimal)s.Size;
                            s.CustomWidth = 0;
                        }
                        else
                        {
                            // 1D 바코드는 가로/세로 독립 조절
                            float deltaW = (dx_raw / SCALE) * ((activeHandle == ResizingHandle.TopRight || activeHandle == ResizingHandle.BottomRight) ? 1 : -1) * 2;
                            float deltaH = (dy_raw / SCALE) * ((activeHandle == ResizingHandle.BottomLeft || activeHandle == ResizingHandle.BottomRight) ? 1 : -1);
                            
                            s.CustomWidth = Math.Max(5, Math.Min(500, (int)(resizeStartWidth + deltaW))); // 최저값 5로 적용
                            s.BarcodeHeight = Math.Max(1, Math.Min(200, (int)(resizeStartBarcodeHeight + deltaH))); // 최저값 1로 적용
                            
                            numWidth.Value = (decimal)s.CustomWidth;
                        }
                    }
                    else
                    {
                        // 텍스트는 폰트 사이즈 조절
                        float distance = 0;
                        switch (activeHandle)
                        {
                            case ResizingHandle.TopLeft: distance = -dx_raw - dy_raw; break;
                            case ResizingHandle.TopRight: distance = dx_raw - dy_raw; break;
                            case ResizingHandle.BottomLeft: distance = -dx_raw + dy_raw; break;
                            case ResizingHandle.BottomRight: distance = dx_raw + dy_raw; break;
                        }

                        int newSize = (int)(resizeStartSize + distance / SCALE);
                        s.Size = Math.Max(3, Math.Min(300, newSize));
                        numSize.Value = (decimal)s.Size;
                    }
                }
 
                pbPreview.Invalidate();



            }



            else if (isDragging)



            {



                pbPreview.Cursor = Cursors.SizeAll;



                int printW = (int)(labelWidthMm * SCALE);



                int printH = (int)(labelHeightMm * SCALE);



                float scaleX = printW > 0 ? (float)printW / pbPreview.Width : 1f;



                float scaleY = printH > 0 ? (float)printH / pbPreview.Height : 1f;



                PointF pt = new PointF(e.X * scaleX, e.Y * scaleY);







                // 화면 픽셀 이동량을 mm로 변환 (소수점 누적으로 반올림 손실 방지)



                float dx = (pt.X - lastMousePos.X) / SCALE;



                float dy = (pt.Y - lastMousePos.Y) / SCALE;







                dragAccumulatorX += dx;



                dragAccumulatorY += dy;







                int addX = (int)Math.Round(dragAccumulatorX);



                int addY = (int)Math.Round(dragAccumulatorY);



                if (addX != 0 || addY != 0)



                {



                    dragAccumulatorX -= addX;



                    dragAccumulatorY -= addY;



                    settings[selectedIndex].OffsetX += addX;



                    settings[selectedIndex].OffsetY += addY;



                }







                numX.Value = Math.Max(numX.Minimum, Math.Min(numX.Maximum, (decimal)settings[selectedIndex].OffsetX));



                numY.Value = Math.Max(numY.Minimum, Math.Min(numY.Maximum, (decimal)settings[selectedIndex].OffsetY));







                lastMousePos = pt;



                pbPreview.Invalidate();



            }



            else



            {



                // Hover 커서 모드 결정 (화면 좌표를 인쇄 좌표로 변환하여 hit test)



                if (settings != null && selectedIndex >= 0 && selectedIndex < settings.Count)



                {



                    int printW = (int)(labelWidthMm * SCALE);



                    int printH = (int)(labelHeightMm * SCALE);



                    float scaleX = printW > 0 ? (float)printW / pbPreview.Width : 1f;



                    float scaleY = printH > 0 ? (float)printH / pbPreview.Height : 1f;



                    PointF pt = new PointF(e.X * scaleX, e.Y * scaleY);







                    var s = settings[selectedIndex];



                    float pxX = (labelWidthMm / 2f + s.OffsetX) * SCALE;



                    float pxY = s.OffsetY * SCALE;



                    float w = 0, h = 0;







                    if (s.UsageType == "바코드") 



                    { 



                        string bType = s.BarcodeType ?? "QR_CODE";



                        if (bType == "QR_CODE")



                        {



                            w = s.Size * (SCALE * 25.4f) / 72f;   h = w;



                            if (s.CustomWidth > 0) {



                                w = s.CustomWidth * SCALE;



                                h = w;



                            }



                        }



                        else



                        {



                            w = (s.CustomWidth > 0 ? s.CustomWidth : s.Size) * SCALE;



                            h = s.BarcodeHeight * SCALE;



                        }



                        



                        if (w < 3) w = 3;



                        if (h < 3) h = 3;



                    }



                    else



                    {



                        using (Graphics g = pbPreview.CreateGraphics())



                        {



                            string textValue = GetItemDisplayText(selectedIndex);



                            using (Font font = GetPreviewFont(s))



                            {



                                if (s.CustomWidth > 0) {



                                    w = s.CustomWidth * SCALE;



                                    SizeF size = g.MeasureString(textValue, font, (int)w);



                                    h = Math.Max(size.Height, 20);



                                } else {



                                    SizeF size = g.MeasureString(textValue, font);



                                    w = size.Width; h = size.Height;



                                }



                            }



                        }



                    }







                    // 로컬 좌표계 변환 및 커서 판정



                    float centerY = pxY + h / 2f;



                    PointF localPt = TransformToLocal(pt, pxX, centerY, s.RotateAngle);



                    RectangleF localRect = new RectangleF(-w / 2f, -h / 2f, w, h);



                    



                    RectangleF tl = new RectangleF(localRect.Left - 10, localRect.Top - 10, 20, 20);



                    RectangleF tr = new RectangleF(localRect.Right - 10, localRect.Top - 10, 20, 20);



                    RectangleF bl = new RectangleF(localRect.Left - 10, localRect.Bottom - 10, 20, 20);



                    RectangleF br = new RectangleF(localRect.Right - 10, localRect.Bottom - 10, 20, 20);



                    RectangleF l = new RectangleF(localRect.Left - 10, localRect.Top + localRect.Height / 2f - 10, 20, 20);



                    RectangleF r = new RectangleF(localRect.Right - 10, localRect.Top + localRect.Height / 2f - 10, 20, 20);



                    RectangleF t = new RectangleF(localRect.Left + localRect.Width / 2f - 10, localRect.Top - 10, 20, 20);



                    RectangleF b = new RectangleF(localRect.Left + localRect.Width / 2f - 10, localRect.Bottom - 10, 20, 20);







                    Cursor cur = Cursors.Default;



                    bool isRotated90or270 = (s.RotateAngle == 90 || s.RotateAngle == 270);



                    



                    if (tl.Contains(localPt) || br.Contains(localPt))



                        cur = isRotated90or270 ? Cursors.SizeNESW : Cursors.SizeNWSE;



                    else if (tr.Contains(localPt) || bl.Contains(localPt))



                        cur = isRotated90or270 ? Cursors.SizeNWSE : Cursors.SizeNESW;



                    else if (l.Contains(localPt) || r.Contains(localPt))



                        cur = isRotated90or270 ? Cursors.SizeNS : Cursors.SizeWE;



                    else if (t.Contains(localPt) || b.Contains(localPt))



                        cur = isRotated90or270 ? Cursors.SizeWE : Cursors.SizeNS;



                    else



                    {



                        RectangleF hitRect = localRect;



                        hitRect.Inflate(8, 8);



                        cur = hitRect.Contains(localPt) ? Cursors.Hand : Cursors.Default;



                    }



                    pbPreview.Cursor = cur;



                }



                else



                {



                    pbPreview.Cursor = Cursors.Default;



                }



            }



        }







        private void PbPreview_MouseUp(object sender, MouseEventArgs e)



        {



            if (isDragging || isResizing)



            {



                mainForm.UpdateColumnPositionSettings(settings, false);



            }



            isDragging = false;



            isResizing = false;



            pbPreview.Cursor = Cursors.Default;



        }







        private void LabelLayoutSettingsForm_FormClosing(object sender, FormClosingEventArgs e)



        {



            if (this.DialogResult != DialogResult.OK)



            {



                mainForm.UpdateColumnPositionSettings(this.originalSettings, false);

                // 용지 설정 원복
                PRINT_INFO_CLASS.PrinterInfo.Paper_Width = this.originalPaperWidth;
                PRINT_INFO_CLASS.PrinterInfo.Paper_Height = this.originalPaperHeight;
                PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = this.originalPaperRotate;

                string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                PRINT_INFO_CLASS.SaveToFile(settingsPath);

                mainForm?.UpdateLabelSizeInfo();
                mainForm?.OnPreviewEvent();



            }



        }







        private string ProcessBarcodeText(string data, string barcodeType)



        {



            try



            {



                if (string.IsNullOrEmpty(data)) return "BARCODE";



                if (barcodeType == "CODE_128")



                {



                    var allowedChars = new List<char>();



                    foreach (char c in data)



                    {



                        if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_')



                            allowedChars.Add(c);



                    }



                    string processedData = new string(allowedChars.ToArray()).Trim();



                    return string.IsNullOrEmpty(processedData) ? "BARCODE" : processedData;



                }



                return data;



            }



            catch { return "BARCODE"; }



        }







        private PointF TransformToLocal(PointF pt, float pxX, float pxY, int angle)



        {



            float dx = pt.X - pxX;



            float dy = pt.Y - pxY;



            switch (angle)



            {



                case 90:



                    return new PointF(dy, -dx);



                case 180:



                    return new PointF(-dx, -dy);



                case 270:



                    return new PointF(-dy, dx);



                default:



                    return new PointF(dx, dy);



            }



        }







        private ZXing.BarcodeFormat GetBarcodeFormat(string barcodeType)



        {



            switch (barcodeType)



            {



                case "QR_CODE": return ZXing.BarcodeFormat.QR_CODE;



                case "CODE_128": return ZXing.BarcodeFormat.CODE_128;



                case "CODE_39": return ZXing.BarcodeFormat.CODE_39;



                case "EAN_13": return ZXing.BarcodeFormat.EAN_13;



                default: return ZXing.BarcodeFormat.QR_CODE;



            }



        }



    }



}



