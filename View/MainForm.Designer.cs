using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.View
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
                 private void InitializeComponent()
         {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.label_PrinterStatus = new System.Windows.Forms.Label();
            this.Label_AdjustFormOpen = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label_license = new System.Windows.Forms.Label();
            this.label_licenseInfo = new System.Windows.Forms.Label();
            this.panel_PrintButton = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label_companyLink = new System.Windows.Forms.Label();
            this.panel_OpenAdjustForm = new System.Windows.Forms.Panel();
            this.panel_OpenExcelForm = new System.Windows.Forms.Panel();
            this.panel_Home = new System.Windows.Forms.Panel();
            this.label_Column1 = new System.Windows.Forms.Label();
            this.label_Column3 = new System.Windows.Forms.Label();
            this.label_Column4 = new System.Windows.Forms.Label();
            this.label_Column5 = new System.Windows.Forms.Label();
            this.label_ExcelFileName = new System.Windows.Forms.Label();
            this.label_LabelSize = new System.Windows.Forms.Label();
            this.panel_Separator = new System.Windows.Forms.Panel();
            this.pictureBox_Logo = new System.Windows.Forms.PictureBox();
            this.label_Title = new System.Windows.Forms.Label();
            this.TextBox_InputText4 = new BarcodePrint.UserControls.RoundedTextBox();
            this.numericUpDown_PrintCopies = new BarcodePrint.UserControls.CustomNumericUpDown();
            this.panel_BarcodePreview = new BarcodePrint.UserControls.RoundedPanel();
            this.menuItem_Manual = new BarcodePrint.UserControls.ModeMenuItem();
            this.menuItem_Excel = new BarcodePrint.UserControls.ModeMenuItem();
            this.customCloseButton = new BarcodePrint.UserControls.CustomCloseButton();
            this.touchSwitch_Title = new BarcodePrint.UserControls.TouchSwitch();
            this.textBox_Title = new BarcodePrint.UserControls.RoundedTextBox();
            this.TextBox_InputText = new BarcodePrint.UserControls.RoundedTextBox();
            this.TextBox_InputText3 = new BarcodePrint.UserControls.RoundedTextBox();
            this.TextBox_InputText2 = new BarcodePrint.UserControls.RoundedTextBox();
            this.panel_OpenLayoutForm = new System.Windows.Forms.Panel();
            this.panel_PreviewNav = new System.Windows.Forms.Panel();
            this.btnPrevPage = new System.Windows.Forms.Button();
            this.txtCurrentPage = new System.Windows.Forms.TextBox();
            this.lblSeparator = new System.Windows.Forms.Label();
            this.txtEndPage = new System.Windows.Forms.TextBox();
            this.lblTotalPages = new System.Windows.Forms.Label();
            this.btnNextPage = new System.Windows.Forms.Button();
            this.panel_SaveProject = new System.Windows.Forms.Panel();
            this.panel_OpenProject = new System.Windows.Forms.Panel();
            this.label_SaveProject = new System.Windows.Forms.Label();
            this.label_OpenProject = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Logo)).BeginInit();
            this.panel_PreviewNav.SuspendLayout();
            this.SuspendLayout();
            // 
            // label_PrinterStatus
            // 
            this.label_PrinterStatus.AutoSize = true;
            this.label_PrinterStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_PrinterStatus.Location = new System.Drawing.Point(22, 95);
            this.label_PrinterStatus.Name = "label_PrinterStatus";
            this.label_PrinterStatus.Size = new System.Drawing.Size(118, 24);
            this.label_PrinterStatus.TabIndex = 30;
            this.label_PrinterStatus.Text = "프린터 확인중..";
            // 
            // Label_AdjustFormOpen
            // 
            this.Label_AdjustFormOpen.AutoSize = true;
            this.Label_AdjustFormOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label_AdjustFormOpen.ForeColor = System.Drawing.Color.Black;
            this.Label_AdjustFormOpen.Location = new System.Drawing.Point(602, 6);
            this.Label_AdjustFormOpen.Name = "Label_AdjustFormOpen";
            this.Label_AdjustFormOpen.Size = new System.Drawing.Size(125, 20);
            this.Label_AdjustFormOpen.TabIndex = 1;
            this.Label_AdjustFormOpen.Text = "제작 : 한국Xprinter";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 42F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
            this.label2.Location = new System.Drawing.Point(12, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(641, 65);
            this.label2.TabIndex = 43;
            this.label2.Text = "라벨마스터 for 베이직";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.label2_MouseDown);
            this.label2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.label2_MouseMove);
            this.label2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.label2_MouseUp);
            // 
            // label_license
            // 
            this.label_license.AutoSize = true;
            this.label_license.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label_license.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_license.ForeColor = System.Drawing.Color.Blue;
            this.label_license.Location = new System.Drawing.Point(770, 7);
            this.label_license.Name = "label_license";
            this.label_license.Size = new System.Drawing.Size(86, 18);
            this.label_license.TabIndex = 1;
            this.label_license.Text = "프로그램정보";
            this.label_license.Click += new System.EventHandler(this.label_license_Click);
            this.label_license.MouseEnter += new System.EventHandler(this.label_license_MouseEnter);
            this.label_license.MouseLeave += new System.EventHandler(this.label_license_MouseLeave);
            // 
            // label_licenseInfo
            // 
            this.label_licenseInfo.AutoSize = true;
            this.label_licenseInfo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label_licenseInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_licenseInfo.ForeColor = System.Drawing.Color.Blue;
            this.label_licenseInfo.Location = new System.Drawing.Point(868, 7);
            this.label_licenseInfo.Name = "label_licenseInfo";
            this.label_licenseInfo.Size = new System.Drawing.Size(60, 18);
            this.label_licenseInfo.TabIndex = 2;
            this.label_licenseInfo.Text = "라이센스";
            this.label_licenseInfo.Click += new System.EventHandler(this.label_licenseInfo_Click);
            this.label_licenseInfo.MouseEnter += new System.EventHandler(this.label_licenseInfo_MouseEnter);
            this.label_licenseInfo.MouseLeave += new System.EventHandler(this.label_licenseInfo_MouseLeave);
            // 
            // panel_PrintButton
            // 
            this.panel_PrintButton.Location = new System.Drawing.Point(736, 395);
            this.panel_PrintButton.Name = "panel_PrintButton";
            this.panel_PrintButton.Size = new System.Drawing.Size(165, 137);
            this.panel_PrintButton.TabIndex = 7;
            this.panel_PrintButton.Click += new System.EventHandler(this.panel_PrintButton_Click);
            this.panel_PrintButton.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_PrintButton_Paint);
            this.panel_PrintButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_PrintButton_MouseDown);
            this.panel_PrintButton.MouseEnter += new System.EventHandler(this.panel_PrintButton_MouseEnter);
            this.panel_PrintButton.MouseLeave += new System.EventHandler(this.panel_PrintButton_MouseLeave);
            this.panel_PrintButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_PrintButton_MouseUp);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label_companyLink);
            this.panel1.Controls.Add(this.Label_AdjustFormOpen);
            this.panel1.Controls.Add(this.label_license);
            this.panel1.Controls.Add(this.label_licenseInfo);
            this.panel1.Location = new System.Drawing.Point(0, 570);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(950, 30);
            this.panel1.TabIndex = 49;
            // 
            // label_companyLink
            // 
            this.label_companyLink.AutoSize = true;
            this.label_companyLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label_companyLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_companyLink.ForeColor = System.Drawing.Color.Blue;
            this.label_companyLink.Location = new System.Drawing.Point(18, 7);
            this.label_companyLink.Name = "label_companyLink";
            this.label_companyLink.Size = new System.Drawing.Size(94, 18);
            this.label_companyLink.TabIndex = 0;
            this.label_companyLink.Text = "xprinter.co.kr";
            this.label_companyLink.Click += new System.EventHandler(this.label_companyLink_Click);
            // 
            // panel_OpenAdjustForm
            // 
            this.panel_OpenAdjustForm.Location = new System.Drawing.Point(736, 330);
            this.panel_OpenAdjustForm.Name = "panel_OpenAdjustForm";
            this.panel_OpenAdjustForm.Size = new System.Drawing.Size(165, 48);
            this.panel_OpenAdjustForm.TabIndex = 5;
            this.panel_OpenAdjustForm.Click += new System.EventHandler(this.panel_OpenAdjustForm_Click);
            this.panel_OpenAdjustForm.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_OpenAdjustForm_Paint);
            this.panel_OpenAdjustForm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_OpenAdjustForm_MouseDown);
            this.panel_OpenAdjustForm.MouseEnter += new System.EventHandler(this.panel_OpenAdjustForm_MouseEnter);
            this.panel_OpenAdjustForm.MouseLeave += new System.EventHandler(this.panel_OpenAdjustForm_MouseLeave);
            this.panel_OpenAdjustForm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_OpenAdjustForm_MouseUp);
            // 
            // panel_OpenExcelForm
            // 
            this.panel_OpenExcelForm.Enabled = false;
            this.panel_OpenExcelForm.Location = new System.Drawing.Point(444, 235);
            this.panel_OpenExcelForm.Name = "panel_OpenExcelForm";
            this.panel_OpenExcelForm.Size = new System.Drawing.Size(230, 60);
            this.panel_OpenExcelForm.TabIndex = 9;
            this.panel_OpenExcelForm.Click += new System.EventHandler(this.panel_OpenExcelForm_Click);
            this.panel_OpenExcelForm.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_OpenExcelForm_Paint);
            this.panel_OpenExcelForm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_OpenExcelForm_MouseDown);
            this.panel_OpenExcelForm.MouseEnter += new System.EventHandler(this.panel_OpenExcelForm_MouseEnter);
            this.panel_OpenExcelForm.MouseLeave += new System.EventHandler(this.panel_OpenExcelForm_MouseLeave);
            this.panel_OpenExcelForm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_OpenExcelForm_MouseUp);
            // 
            // panel_Home
            // 
            this.panel_Home.Location = new System.Drawing.Point(736, 200);
            this.panel_Home.Name = "panel_Home";
            this.panel_Home.Size = new System.Drawing.Size(167, 48);
            this.panel_Home.TabIndex = 6;
            this.panel_Home.Click += new System.EventHandler(this.panel_Home_Click);
            this.panel_Home.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_Home_Paint);
            this.panel_Home.DoubleClick += new System.EventHandler(this.panel_Home_DoubleClick);
            this.panel_Home.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_Home_MouseDown);
            this.panel_Home.MouseEnter += new System.EventHandler(this.panel_Home_MouseEnter);
            this.panel_Home.MouseLeave += new System.EventHandler(this.panel_Home_MouseLeave);
            this.panel_Home.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_Home_MouseUp);
            // 
            // label_Column1
            // 
            this.label_Column1.AutoSize = true;
            this.label_Column1.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Column1.Location = new System.Drawing.Point(444, 313);
            this.label_Column1.Name = "label_Column1";
            this.label_Column1.Size = new System.Drawing.Size(48, 20);
            this.label_Column1.TabIndex = 99;
            this.label_Column1.Text = "컬럼2";
            // 
            // label_Column3
            // 
            this.label_Column3.AutoSize = true;
            this.label_Column3.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Column3.Location = new System.Drawing.Point(444, 398);
            this.label_Column3.Name = "label_Column3";
            this.label_Column3.Size = new System.Drawing.Size(48, 20);
            this.label_Column3.TabIndex = 101;
            this.label_Column3.Text = "컬럼3";
            // 
            // label_Column4
            // 
            this.label_Column4.AutoSize = true;
            this.label_Column4.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Column4.Location = new System.Drawing.Point(441, 212);
            this.label_Column4.Name = "label_Column4";
            this.label_Column4.Size = new System.Drawing.Size(48, 20);
            this.label_Column4.TabIndex = 102;
            this.label_Column4.Text = "컬럼1";
            // 
            // label_Column5
            // 
            this.label_Column5.AutoSize = true;
            this.label_Column5.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Column5.Location = new System.Drawing.Point(447, 479);
            this.label_Column5.Name = "label_Column5";
            this.label_Column5.Size = new System.Drawing.Size(48, 20);
            this.label_Column5.TabIndex = 103;
            this.label_Column5.Text = "컬럼4";
            // 
            // label_ExcelFileName
            // 
            this.label_ExcelFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_ExcelFileName.ForeColor = System.Drawing.Color.DarkBlue;
            this.label_ExcelFileName.Location = new System.Drawing.Point(444, 300);
            this.label_ExcelFileName.Name = "label_ExcelFileName";
            this.label_ExcelFileName.Size = new System.Drawing.Size(230, 30);
            this.label_ExcelFileName.TabIndex = 9;
            this.label_ExcelFileName.Visible = false;
            // 
            // label_LabelSize
            // 
            this.label_LabelSize.AutoSize = true;
            this.label_LabelSize.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_LabelSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(118)))), ((int)(((byte)(210)))));
            this.label_LabelSize.Location = new System.Drawing.Point(19, 146);
            this.label_LabelSize.Name = "label_LabelSize";
            this.label_LabelSize.Size = new System.Drawing.Size(164, 22);
            this.label_LabelSize.TabIndex = 103;
            this.label_LabelSize.Text = "라벨 크기: 50x30mm";
            // 
            // panel_Separator
            // 
            this.panel_Separator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(210)))));
            this.panel_Separator.Location = new System.Drawing.Point(399, 164);
            this.panel_Separator.MaximumSize = new System.Drawing.Size(1, 400);
            this.panel_Separator.MinimumSize = new System.Drawing.Size(1, 400);
            this.panel_Separator.Name = "panel_Separator";
            this.panel_Separator.Size = new System.Drawing.Size(1, 400);
            this.panel_Separator.TabIndex = 106;
            // 
            // pictureBox_Logo
            // 
            this.pictureBox_Logo.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox_Logo.Image = global::BarcodePrint.Properties.Resources.LabelMaster_Logo;
            this.pictureBox_Logo.Location = new System.Drawing.Point(659, -46);
            this.pictureBox_Logo.Name = "pictureBox_Logo";
            this.pictureBox_Logo.Size = new System.Drawing.Size(295, 176);
            this.pictureBox_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_Logo.TabIndex = 100;
            this.pictureBox_Logo.TabStop = false;
            // 
            // label_Title
            // 
            this.label_Title.AutoSize = true;
            this.label_Title.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_Title.ForeColor = System.Drawing.Color.Black;
            this.label_Title.Location = new System.Drawing.Point(441, 183);
            this.label_Title.Name = "label_Title";
            this.label_Title.Size = new System.Drawing.Size(69, 20);
            this.label_Title.TabIndex = 16;
            this.label_Title.Text = "공통문구";
            this.label_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TextBox_InputText4
            // 
            this.TextBox_InputText4.AcceptsReturn = false;
            this.TextBox_InputText4.BackColor = System.Drawing.Color.White;
            this.TextBox_InputText4.BorderColor = System.Drawing.Color.LightGray;
            this.TextBox_InputText4.BorderRadius = 8;
            this.TextBox_InputText4.FocusBorderColor = System.Drawing.Color.DodgerBlue;
            this.TextBox_InputText4.Location = new System.Drawing.Point(444, 250);
            this.TextBox_InputText4.Multiline = false;
            this.TextBox_InputText4.Name = "TextBox_InputText4";
            this.TextBox_InputText4.Padding = new System.Windows.Forms.Padding(4);
            this.TextBox_InputText4.SelectionLength = 0;
            this.TextBox_InputText4.SelectionStart = 0;
            this.TextBox_InputText4.Size = new System.Drawing.Size(230, 50);
            this.TextBox_InputText4.TabIndex = 2;
            this.TextBox_InputText4.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.TextBox_InputText4.TextBoxFont = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.TextBox_InputText4.TextBoxForeColor = System.Drawing.Color.Black;
            this.TextBox_InputText4.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText4_KeyDown);
            this.TextBox_InputText4.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText4_KeyUp);
            // 
            // numericUpDown_PrintCopies
            // 
            this.numericUpDown_PrintCopies.BackColor = System.Drawing.Color.Transparent;
            this.numericUpDown_PrintCopies.Increment = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_PrintCopies.LabelText = "인쇄매수";
            this.numericUpDown_PrintCopies.Location = new System.Drawing.Point(708, 90);
            this.numericUpDown_PrintCopies.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.numericUpDown_PrintCopies.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_PrintCopies.Name = "numericUpDown_PrintCopies";
            this.numericUpDown_PrintCopies.Size = new System.Drawing.Size(205, 50);
            this.numericUpDown_PrintCopies.TabIndex = 6;
            this.numericUpDown_PrintCopies.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // panel_BarcodePreview
            // 
            this.panel_BarcodePreview.AutoScroll = true;
            this.panel_BarcodePreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(248)))), ((int)(((byte)(255)))));
            this.panel_BarcodePreview.BorderColor = System.Drawing.Color.DodgerBlue;
            this.panel_BarcodePreview.BorderRadius = 12;
            this.panel_BarcodePreview.BorderWidth = 1;
            this.panel_BarcodePreview.Location = new System.Drawing.Point(20, 176);
            this.panel_BarcodePreview.Name = "panel_BarcodePreview";
            this.panel_BarcodePreview.Size = new System.Drawing.Size(336, 340);
            this.panel_BarcodePreview.TabIndex = 52;
            this.panel_BarcodePreview.TabStop = true;
            // 
            // menuItem_Manual
            // 
            this.menuItem_Manual.BackColor = System.Drawing.Color.Transparent;
            this.menuItem_Manual.Cursor = System.Windows.Forms.Cursors.Hand;
            this.menuItem_Manual.IsSelected = true;
            this.menuItem_Manual.Location = new System.Drawing.Point(442, 109);
            this.menuItem_Manual.MenuIcon = null;
            this.menuItem_Manual.MenuText = "직접 출력";
            this.menuItem_Manual.Name = "menuItem_Manual";
            this.menuItem_Manual.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.menuItem_Manual.Size = new System.Drawing.Size(89, 45);
            this.menuItem_Manual.TabIndex = 0;
            this.menuItem_Manual.MenuClicked += new System.EventHandler(this.menuItem_Manual_Click);
            // 
            // menuItem_Excel
            // 
            this.menuItem_Excel.BackColor = System.Drawing.Color.Transparent;
            this.menuItem_Excel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.menuItem_Excel.IsSelected = false;
            this.menuItem_Excel.Location = new System.Drawing.Point(528, 109);
            this.menuItem_Excel.MenuIcon = null;
            this.menuItem_Excel.MenuText = "엑셀 출력";
            this.menuItem_Excel.Name = "menuItem_Excel";
            this.menuItem_Excel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.menuItem_Excel.Size = new System.Drawing.Size(90, 45);
            this.menuItem_Excel.TabIndex = 1;
            this.menuItem_Excel.MenuClicked += new System.EventHandler(this.menuItem_Excel_Click);
            // 
            // customCloseButton
            // 
            this.customCloseButton.BackColor = System.Drawing.Color.Transparent;
            this.customCloseButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.customCloseButton.Location = new System.Drawing.Point(882, 15);
            this.customCloseButton.Name = "customCloseButton";
            this.customCloseButton.Size = new System.Drawing.Size(41, 42);
            this.customCloseButton.TabIndex = 15;
            this.customCloseButton.TabStop = false;
            this.customCloseButton.Click += new System.EventHandler(this.customCloseButton_Click);
            // 
            // touchSwitch_Title
            // 
            this.touchSwitch_Title.BackColor = System.Drawing.Color.Transparent;
            this.touchSwitch_Title.Checked = false;
            this.touchSwitch_Title.Cursor = System.Windows.Forms.Cursors.Hand;
            this.touchSwitch_Title.Location = new System.Drawing.Point(516, 174);
            this.touchSwitch_Title.Name = "touchSwitch_Title";
            this.touchSwitch_Title.Size = new System.Drawing.Size(66, 40);
            this.touchSwitch_Title.SwitchText = "공통문구";
            this.touchSwitch_Title.TabIndex = 0;
            this.touchSwitch_Title.TabStop = false;
            this.touchSwitch_Title.Text = "공통문구";
            this.touchSwitch_Title.CheckedChanged += new System.EventHandler(this.touchSwitch_Title_CheckedChanged);
            // 
            // textBox_Title
            // 
            this.textBox_Title.AcceptsReturn = false;
            this.textBox_Title.BackColor = System.Drawing.Color.White;
            this.textBox_Title.BorderColor = System.Drawing.Color.LightGray;
            this.textBox_Title.BorderRadius = 8;
            this.textBox_Title.Enabled = false;
            this.textBox_Title.FocusBorderColor = System.Drawing.Color.DodgerBlue;
            this.textBox_Title.Location = new System.Drawing.Point(594, 172);
            this.textBox_Title.Multiline = false;
            this.textBox_Title.Name = "textBox_Title";
            this.textBox_Title.Padding = new System.Windows.Forms.Padding(4);
            this.textBox_Title.SelectionLength = 0;
            this.textBox_Title.SelectionStart = 0;
            this.textBox_Title.Size = new System.Drawing.Size(110, 42);
            this.textBox_Title.TabIndex = 1;
            this.textBox_Title.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.textBox_Title.TextBoxFont = new System.Drawing.Font("맑은 고딕", 11F);
            this.textBox_Title.TextBoxForeColor = System.Drawing.Color.Black;
            this.textBox_Title.TextChanged += new System.EventHandler(this.textBox_Title_TextChanged);
            // 
            // TextBox_InputText
            // 
            this.TextBox_InputText.AcceptsReturn = false;
            this.TextBox_InputText.BackColor = System.Drawing.Color.White;
            this.TextBox_InputText.BorderColor = System.Drawing.Color.LightGray;
            this.TextBox_InputText.BorderRadius = 8;
            this.TextBox_InputText.FocusBorderColor = System.Drawing.Color.DodgerBlue;
            this.TextBox_InputText.Location = new System.Drawing.Point(444, 337);
            this.TextBox_InputText.Multiline = false;
            this.TextBox_InputText.Name = "TextBox_InputText";
            this.TextBox_InputText.Padding = new System.Windows.Forms.Padding(4);
            this.TextBox_InputText.SelectionLength = 0;
            this.TextBox_InputText.SelectionStart = 0;
            this.TextBox_InputText.Size = new System.Drawing.Size(230, 50);
            this.TextBox_InputText.TabIndex = 3;
            this.TextBox_InputText.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.TextBox_InputText.TextBoxFont = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.TextBox_InputText.TextBoxForeColor = System.Drawing.Color.Black;
            this.TextBox_InputText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText_KeyDown);
            this.TextBox_InputText.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText_KeyUp);
            // 
            // TextBox_InputText3
            // 
            this.TextBox_InputText3.AcceptsReturn = false;
            this.TextBox_InputText3.BackColor = System.Drawing.Color.White;
            this.TextBox_InputText3.BorderColor = System.Drawing.Color.LightGray;
            this.TextBox_InputText3.BorderRadius = 8;
            this.TextBox_InputText3.FocusBorderColor = System.Drawing.Color.DodgerBlue;
            this.TextBox_InputText3.Location = new System.Drawing.Point(444, 423);
            this.TextBox_InputText3.Multiline = false;
            this.TextBox_InputText3.Name = "TextBox_InputText3";
            this.TextBox_InputText3.Padding = new System.Windows.Forms.Padding(4);
            this.TextBox_InputText3.SelectionLength = 0;
            this.TextBox_InputText3.SelectionStart = 0;
            this.TextBox_InputText3.Size = new System.Drawing.Size(230, 50);
            this.TextBox_InputText3.TabIndex = 4;
            this.TextBox_InputText3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.TextBox_InputText3.TextBoxFont = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.TextBox_InputText3.TextBoxForeColor = System.Drawing.Color.Black;
            this.TextBox_InputText3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText3_KeyDown);
            this.TextBox_InputText3.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText3_KeyUp);
            // 
            // TextBox_InputText2
            // 
            this.TextBox_InputText2.AcceptsReturn = false;
            this.TextBox_InputText2.BackColor = System.Drawing.Color.White;
            this.TextBox_InputText2.BorderColor = System.Drawing.Color.LightGray;
            this.TextBox_InputText2.BorderRadius = 8;
            this.TextBox_InputText2.FocusBorderColor = System.Drawing.Color.DodgerBlue;
            this.TextBox_InputText2.Location = new System.Drawing.Point(444, 502);
            this.TextBox_InputText2.Multiline = false;
            this.TextBox_InputText2.Name = "TextBox_InputText2";
            this.TextBox_InputText2.Padding = new System.Windows.Forms.Padding(4);
            this.TextBox_InputText2.SelectionLength = 0;
            this.TextBox_InputText2.SelectionStart = 0;
            this.TextBox_InputText2.Size = new System.Drawing.Size(230, 50);
            this.TextBox_InputText2.TabIndex = 5;
            this.TextBox_InputText2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.TextBox_InputText2.TextBoxFont = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.TextBox_InputText2.TextBoxForeColor = System.Drawing.Color.Black;
            this.TextBox_InputText2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText2_KeyDown);
            this.TextBox_InputText2.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_InputText2_KeyUp);
            // 
            // panel_OpenLayoutForm
            // 
            this.panel_OpenLayoutForm.Location = new System.Drawing.Point(736, 265);
            this.panel_OpenLayoutForm.Name = "panel_OpenLayoutForm";
            this.panel_OpenLayoutForm.Size = new System.Drawing.Size(165, 48);
            this.panel_OpenLayoutForm.TabIndex = 8;
            this.panel_OpenLayoutForm.Click += new System.EventHandler(this.panel_OpenLayoutForm_Click);
            this.panel_OpenLayoutForm.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_OpenLayoutForm_Paint);
            this.panel_OpenLayoutForm.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_OpenLayoutForm_MouseDown);
            this.panel_OpenLayoutForm.MouseEnter += new System.EventHandler(this.panel_OpenLayoutForm_MouseEnter);
            this.panel_OpenLayoutForm.MouseLeave += new System.EventHandler(this.panel_OpenLayoutForm_MouseLeave);
            this.panel_OpenLayoutForm.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_OpenLayoutForm_MouseUp);
            // 
            // panel_PreviewNav
            // 
            this.panel_PreviewNav.Controls.Add(this.btnPrevPage);
            this.panel_PreviewNav.Controls.Add(this.txtCurrentPage);
            this.panel_PreviewNav.Controls.Add(this.lblSeparator);
            this.panel_PreviewNav.Controls.Add(this.txtEndPage);
            this.panel_PreviewNav.Controls.Add(this.lblTotalPages);
            this.panel_PreviewNav.Controls.Add(this.btnNextPage);
            this.panel_PreviewNav.Location = new System.Drawing.Point(20, 520);
            this.panel_PreviewNav.Name = "panel_PreviewNav";
            this.panel_PreviewNav.Size = new System.Drawing.Size(336, 40);
            this.panel_PreviewNav.TabIndex = 100;
            // 
            // btnPrevPage
            // 
            this.btnPrevPage.Location = new System.Drawing.Point(15, 5);
            this.btnPrevPage.Name = "btnPrevPage";
            this.btnPrevPage.Size = new System.Drawing.Size(40, 30);
            this.btnPrevPage.TabIndex = 0;
            this.btnPrevPage.Text = "◀";
            this.btnPrevPage.UseVisualStyleBackColor = true;
            this.btnPrevPage.Click += new System.EventHandler(this.btnPrevPage_Click);
            // 
            // txtCurrentPage
            // 
            this.txtCurrentPage.Location = new System.Drawing.Point(60, 8);
            this.txtCurrentPage.Name = "txtCurrentPage";
            this.txtCurrentPage.Size = new System.Drawing.Size(50, 21);
            this.txtCurrentPage.TabIndex = 1;
            this.txtCurrentPage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtCurrentPage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCurrentPage_KeyDown);
            // 
            // lblSeparator
            // 
            this.lblSeparator.AutoSize = true;
            this.lblSeparator.Location = new System.Drawing.Point(115, 12);
            this.lblSeparator.Name = "lblSeparator";
            this.lblSeparator.Size = new System.Drawing.Size(14, 12);
            this.lblSeparator.TabIndex = 4;
            this.lblSeparator.Text = "~";
            this.lblSeparator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtEndPage
            // 
            this.txtEndPage.Location = new System.Drawing.Point(135, 8);
            this.txtEndPage.Name = "txtEndPage";
            this.txtEndPage.Size = new System.Drawing.Size(50, 21);
            this.txtEndPage.TabIndex = 2;
            this.txtEndPage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtEndPage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtEndPage_KeyDown);
            this.txtEndPage.Leave += new System.EventHandler(this.txtEndPage_Leave);
            // 
            // lblTotalPages
            // 
            this.lblTotalPages.AutoSize = true;
            this.lblTotalPages.Location = new System.Drawing.Point(190, 12);
            this.lblTotalPages.Name = "lblTotalPages";
            this.lblTotalPages.Size = new System.Drawing.Size(25, 12);
            this.lblTotalPages.TabIndex = 3;
            this.lblTotalPages.Text = " / 0";
            // 
            // btnNextPage
            // 
            this.btnNextPage.Location = new System.Drawing.Point(235, 5);
            this.btnNextPage.Name = "btnNextPage";
            this.btnNextPage.Size = new System.Drawing.Size(40, 30);
            this.btnNextPage.TabIndex = 5;
            this.btnNextPage.Text = "▶";
            this.btnNextPage.UseVisualStyleBackColor = true;
            this.btnNextPage.Click += new System.EventHandler(this.btnNextPage_Click);
            // 
            // panel_SaveProject
            // 
            this.panel_SaveProject.Location = new System.Drawing.Point(736, 135);
            this.panel_SaveProject.Name = "panel_SaveProject";
            this.panel_SaveProject.Size = new System.Drawing.Size(80, 48);
            this.panel_SaveProject.TabIndex = 110;
            this.panel_SaveProject.Click += new System.EventHandler(this.btn_SaveProject_Click);
            this.panel_SaveProject.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_SaveProject_Paint);
            this.panel_SaveProject.MouseEnter += new System.EventHandler(this.panel_SaveProject_MouseEnter);
            this.panel_SaveProject.MouseLeave += new System.EventHandler(this.panel_SaveProject_MouseLeave);
            // 
            // panel_OpenProject
            // 
            this.panel_OpenProject.Location = new System.Drawing.Point(821, 135);
            this.panel_OpenProject.Name = "panel_OpenProject";
            this.panel_OpenProject.Size = new System.Drawing.Size(80, 48);
            this.panel_OpenProject.TabIndex = 111;
            this.panel_OpenProject.Click += new System.EventHandler(this.btn_OpenProject_Click);
            this.panel_OpenProject.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_OpenProject_Paint);
            this.panel_OpenProject.MouseEnter += new System.EventHandler(this.panel_OpenProject_MouseEnter);
            this.panel_OpenProject.MouseLeave += new System.EventHandler(this.panel_OpenProject_MouseLeave);
            // 
            // label_SaveProject
            // 
            this.label_SaveProject.Location = new System.Drawing.Point(0, 0);
            this.label_SaveProject.Name = "label_SaveProject";
            this.label_SaveProject.Size = new System.Drawing.Size(100, 23);
            this.label_SaveProject.TabIndex = 0;
            // 
            // label_OpenProject
            // 
            this.label_OpenProject.Location = new System.Drawing.Point(0, 0);
            this.label_OpenProject.Name = "label_OpenProject";
            this.label_OpenProject.Size = new System.Drawing.Size(100, 23);
            this.label_OpenProject.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(255)))));
            this.ClientSize = new System.Drawing.Size(950, 600);
            this.Controls.Add(this.panel_PreviewNav);
            this.Controls.Add(this.TextBox_InputText4);
            this.Controls.Add(this.panel_PrintButton);
            this.Controls.Add(this.label_ExcelFileName);
            this.Controls.Add(this.label_Column1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numericUpDown_PrintCopies);
            this.Controls.Add(this.panel_OpenLayoutForm);
            this.Controls.Add(this.panel_OpenAdjustForm);
            this.Controls.Add(this.panel_OpenExcelForm);
            this.Controls.Add(this.panel_Home);
            this.Controls.Add(this.label_PrinterStatus);
            this.Controls.Add(this.panel_BarcodePreview);
            this.Controls.Add(this.label_Column3);
            this.Controls.Add(this.label_Column4);
            this.Controls.Add(this.label_Column5);
            this.Controls.Add(this.label_LabelSize);
            this.Controls.Add(this.panel_Separator);
            this.Controls.Add(this.menuItem_Manual);
            this.Controls.Add(this.menuItem_Excel);
            this.Controls.Add(this.panel_SaveProject);
            this.Controls.Add(this.panel_OpenProject);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.customCloseButton);
            this.Controls.Add(this.pictureBox_Logo);
            this.Controls.Add(this.label_Title);
            this.Controls.Add(this.touchSwitch_Title);
            this.Controls.Add(this.textBox_Title);
            this.Controls.Add(this.TextBox_InputText);
            this.Controls.Add(this.TextBox_InputText3);
            this.Controls.Add(this.TextBox_InputText2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximumSize = new System.Drawing.Size(950, 600);
            this.MinimumSize = new System.Drawing.Size(950, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "라벨마스터forBasic";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseUp);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Logo)).EndInit();
            this.panel_PreviewNav.ResumeLayout(false);
            this.panel_PreviewNav.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private BarcodePrint.UserControls.CustomCloseButton customCloseButton;
        private System.Windows.Forms.PictureBox pictureBox_Logo;
        private System.Windows.Forms.Label label_PrinterStatus;
        private System.Windows.Forms.Label Label_AdjustFormOpen;
        private Label label2;
        private Label label_license;
        private Label label_licenseInfo;
        private Panel panel_PrintButton;
        private Panel panel1;
        private BarcodePrint.UserControls.RoundedPanel panel_BarcodePreview;
        private BarcodePrint.UserControls.CustomNumericUpDown numericUpDown_PrintCopies;
        private Panel panel_OpenAdjustForm;
        private Panel panel_OpenExcelForm;
        private Panel panel_Home;
        private System.Windows.Forms.Label label_ExcelFileName;
        private Label label_Column1;
        private Label label_Column3;
        private Label label_Column4;
        private Label label_Column5;
        private Label label_LabelSize;
        private Label label_companyLink;
        private Panel panel_Separator;
        private BarcodePrint.UserControls.ModeMenuItem menuItem_Manual;
        private BarcodePrint.UserControls.ModeMenuItem menuItem_Excel;
        private System.Windows.Forms.Label label_Title;
        private BarcodePrint.UserControls.TouchSwitch touchSwitch_Title;
        private BarcodePrint.UserControls.RoundedTextBox textBox_Title;
        private BarcodePrint.UserControls.RoundedTextBox TextBox_InputText4;
        private BarcodePrint.UserControls.RoundedTextBox TextBox_InputText;
        private BarcodePrint.UserControls.RoundedTextBox TextBox_InputText3;
        private BarcodePrint.UserControls.RoundedTextBox TextBox_InputText2;
        private Panel panel_OpenLayoutForm;
        private System.Windows.Forms.Panel panel_PreviewNav;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.TextBox txtCurrentPage;
        private System.Windows.Forms.TextBox txtEndPage;
        private System.Windows.Forms.Label lblSeparator;
        private System.Windows.Forms.Label lblTotalPages;
        private BarcodePrint.UserControls.TouchSlider touchSlider_Size0;
        private BarcodePrint.UserControls.TouchSlider touchSlider_Size1;
        private BarcodePrint.UserControls.TouchSlider touchSlider_Size2;
        private BarcodePrint.UserControls.TouchSlider touchSlider_Size3;
        private BarcodePrint.UserControls.TouchSlider touchSlider_OffsetY0;
        private BarcodePrint.UserControls.TouchSlider touchSlider_OffsetY1;
        private BarcodePrint.UserControls.TouchSlider touchSlider_OffsetY2;
        private BarcodePrint.UserControls.TouchSlider touchSlider_OffsetY3;
        private BarcodePrint.UserControls.TouchSlider touchSlider_TitleSize;
        private BarcodePrint.UserControls.TouchSlider touchSlider_TitleOffsetY;
        private BarcodePrint.UserControls.TouchSlider touchSlider_OffsetX3;
        private Panel panel_SaveProject;
        private Panel panel_OpenProject;
        private Label label_SaveProject;
        private Label label_OpenProject;
    }
}
