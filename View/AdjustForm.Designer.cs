namespace BarcodePrint.View
{
    partial class AdjustForm
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
            this.materialLabel1 = new System.Windows.Forms.Label();
            this.materialLabel2 = new System.Windows.Forms.Label();
            this.materialLabel4 = new System.Windows.Forms.Label();
            this.Switch_Ribbon = new MaterialSkin.Controls.MaterialSwitch();
            this.panel_AdjustSet = new System.Windows.Forms.Panel();
            this.materialLabel5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel_Calibration = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label_Value_Speed = new System.Windows.Forms.Label();
            this.label_Value_Darkness = new System.Windows.Forms.Label();
            this.button_GapUp = new System.Windows.Forms.Button();
            this.button_GapDown = new System.Windows.Forms.Button();
            this.button_hOffset_XUp = new System.Windows.Forms.Button();
            this.button_hOffset_XDown = new System.Windows.Forms.Button();
            this.button_hOffset_YUp = new System.Windows.Forms.Button();
            this.button_hOffset_YDown = new System.Windows.Forms.Button();
            this.materialLabel_Gap = new System.Windows.Forms.Label();
            this.materialLabel_OffsetX = new System.Windows.Forms.Label();
            this.materialLabel_OffsetY = new System.Windows.Forms.Label();
            this.materialComboBox_PrinterList = new MaterialSkin.Controls.MaterialComboBox();
            this.Slider_darkness = new BarcodePrint.UserControls.TouchSlider();
            this.Slider_Speed = new BarcodePrint.UserControls.TouchSlider();
            this.RadioButton_continues = new BarcodePrint.Controll.CustomRadioButton();
            this.RadioButton_bline = new BarcodePrint.Controll.CustomRadioButton();
            this.RadioButton_Gap = new BarcodePrint.Controll.CustomRadioButton();
            this.textBox_Gap = new BarcodePrint.Controll.CustomTextBox();
            this.textBox_hOffset_X = new BarcodePrint.Controll.CustomTextBox();
            this.textBox_hOffset_Y = new BarcodePrint.Controll.CustomTextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // materialLabel1
            // 
            this.materialLabel1.AutoSize = true;
            this.materialLabel1.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel1.Location = new System.Drawing.Point(18, 87);
            this.materialLabel1.Name = "materialLabel1";
            this.materialLabel1.Size = new System.Drawing.Size(88, 25);
            this.materialLabel1.TabIndex = 7;
            this.materialLabel1.Text = "인쇄속도";
            // 
            // materialLabel2
            // 
            this.materialLabel2.AutoSize = true;
            this.materialLabel2.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel2.Location = new System.Drawing.Point(18, 139);
            this.materialLabel2.Name = "materialLabel2";
            this.materialLabel2.Size = new System.Drawing.Size(88, 25);
            this.materialLabel2.TabIndex = 8;
            this.materialLabel2.Text = "인쇄농도";
            // 
            // materialLabel4
            // 
            this.materialLabel4.AutoSize = true;
            this.materialLabel4.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel4.Location = new System.Drawing.Point(19, 239);
            this.materialLabel4.Name = "materialLabel4";
            this.materialLabel4.Size = new System.Drawing.Size(50, 25);
            this.materialLabel4.TabIndex = 11;
            this.materialLabel4.Text = "리본";
            // 
            // Switch_Ribbon
            // 
            this.Switch_Ribbon.AutoSize = true;
            this.Switch_Ribbon.Depth = 0;
            this.Switch_Ribbon.Location = new System.Drawing.Point(121, 238);
            this.Switch_Ribbon.Margin = new System.Windows.Forms.Padding(0);
            this.Switch_Ribbon.MouseLocation = new System.Drawing.Point(-1, -1);
            this.Switch_Ribbon.MouseState = MaterialSkin.MouseState.HOVER;
            this.Switch_Ribbon.Name = "Switch_Ribbon";
            this.Switch_Ribbon.Ripple = true;
            this.Switch_Ribbon.Size = new System.Drawing.Size(58, 37);
            this.Switch_Ribbon.TabIndex = 8;
            this.Switch_Ribbon.UseVisualStyleBackColor = true;
            // 
            // panel_AdjustSet
            // 
            this.panel_AdjustSet.Location = new System.Drawing.Point(308, 604);
            this.panel_AdjustSet.Name = "panel_AdjustSet";
            this.panel_AdjustSet.Size = new System.Drawing.Size(199, 75);
            this.panel_AdjustSet.TabIndex = 0;
            this.panel_AdjustSet.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_AdjustSet_Paint);
            this.panel_AdjustSet.MouseClick += new System.Windows.Forms.MouseEventHandler(this.panel_AdjustSet_MouseClick);
            this.panel_AdjustSet.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_AdjustSet_MouseDown);
            this.panel_AdjustSet.MouseEnter += new System.EventHandler(this.panel_AdjustSet_MouseEnter);
            this.panel_AdjustSet.MouseLeave += new System.EventHandler(this.panel_AdjustSet_MouseLeave);
            this.panel_AdjustSet.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_AdjustSet_MouseUp);
            // 
            // materialLabel5
            // 
            this.materialLabel5.AutoSize = true;
            this.materialLabel5.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel5.Location = new System.Drawing.Point(18, 189);
            this.materialLabel5.Name = "materialLabel5";
            this.materialLabel5.Size = new System.Drawing.Size(88, 25);
            this.materialLabel5.TabIndex = 14;
            this.materialLabel5.Text = "용지타입";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel_Calibration);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.materialLabel1);
            this.panel1.Controls.Add(this.Slider_darkness);
            this.panel1.Controls.Add(this.materialLabel2);
            this.panel1.Controls.Add(this.Slider_Speed);
            this.panel1.Controls.Add(this.label_Value_Speed);
            this.panel1.Controls.Add(this.label_Value_Darkness);
            this.panel1.Controls.Add(this.materialLabel4);
            this.panel1.Controls.Add(this.RadioButton_continues);
            this.panel1.Controls.Add(this.Switch_Ribbon);
            this.panel1.Controls.Add(this.RadioButton_bline);
            this.panel1.Controls.Add(this.materialLabel5);
            this.panel1.Controls.Add(this.RadioButton_Gap);
            this.panel1.Controls.Add(this.textBox_Gap);
            this.panel1.Controls.Add(this.textBox_hOffset_X);
            this.panel1.Controls.Add(this.textBox_hOffset_Y);
            this.panel1.Controls.Add(this.button_GapUp);
            this.panel1.Controls.Add(this.button_GapDown);
            this.panel1.Controls.Add(this.button_hOffset_XUp);
            this.panel1.Controls.Add(this.button_hOffset_XDown);
            this.panel1.Controls.Add(this.button_hOffset_YUp);
            this.panel1.Controls.Add(this.button_hOffset_YDown);
            this.panel1.Controls.Add(this.materialLabel_Gap);
            this.panel1.Controls.Add(this.materialLabel_OffsetX);
            this.panel1.Controls.Add(this.materialLabel_OffsetY);
            this.panel1.Controls.Add(this.materialComboBox_PrinterList);
            this.panel1.Location = new System.Drawing.Point(5, 43);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(790, 546);
            this.panel1.TabIndex = 26;
            // 
            // panel_Calibration
            // 
            this.panel_Calibration.Location = new System.Drawing.Point(569, 87);
            this.panel_Calibration.Name = "panel_Calibration";
            this.panel_Calibration.Size = new System.Drawing.Size(180, 61);
            this.panel_Calibration.TabIndex = 4;
            this.panel_Calibration.Click += new System.EventHandler(this.panel_Calibration_Click);
            this.panel_Calibration.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_Calibration_Paint);
            this.panel_Calibration.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_Calibration_MouseDown);
            this.panel_Calibration.MouseEnter += new System.EventHandler(this.panel_Calibration_MouseEnter);
            this.panel_Calibration.MouseLeave += new System.EventHandler(this.panel_Calibration_MouseLeave);
            this.panel_Calibration.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_Calibration_MouseUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(11, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 20);
            this.label1.TabIndex = 37;
            this.label1.Text = "프린터 선택";
            // 
            // label_Value_Speed
            // 
            this.label_Value_Speed.AutoSize = true;
            this.label_Value_Speed.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_Value_Speed.Location = new System.Drawing.Point(512, 87);
            this.label_Value_Speed.Name = "label_Value_Speed";
            this.label_Value_Speed.Size = new System.Drawing.Size(23, 25);
            this.label_Value_Speed.TabIndex = 35;
            this.label_Value_Speed.Text = "3";
            // 
            // label_Value_Darkness
            // 
            this.label_Value_Darkness.AutoSize = true;
            this.label_Value_Darkness.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_Value_Darkness.Location = new System.Drawing.Point(512, 139);
            this.label_Value_Darkness.Name = "label_Value_Darkness";
            this.label_Value_Darkness.Size = new System.Drawing.Size(23, 25);
            this.label_Value_Darkness.TabIndex = 36;
            this.label_Value_Darkness.Text = "8";
            // 
            // button_GapUp
            // 
            this.button_GapUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_GapUp.Location = new System.Drawing.Point(267, 298);
            this.button_GapUp.Name = "button_GapUp";
            this.button_GapUp.Size = new System.Drawing.Size(68, 44);
            this.button_GapUp.TabIndex = 13;
            this.button_GapUp.Text = "▲";
            this.button_GapUp.UseVisualStyleBackColor = true;
            // 
            // button_GapDown
            // 
            this.button_GapDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_GapDown.Location = new System.Drawing.Point(346, 298);
            this.button_GapDown.Name = "button_GapDown";
            this.button_GapDown.Size = new System.Drawing.Size(70, 44);
            this.button_GapDown.TabIndex = 14;
            this.button_GapDown.Text = "▼";
            this.button_GapDown.UseVisualStyleBackColor = true;
            // 
            // button_hOffset_XUp
            // 
            this.button_hOffset_XUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_hOffset_XUp.Location = new System.Drawing.Point(267, 357);
            this.button_hOffset_XUp.Name = "button_hOffset_XUp";
            this.button_hOffset_XUp.Size = new System.Drawing.Size(68, 44);
            this.button_hOffset_XUp.TabIndex = 16;
            this.button_hOffset_XUp.Text = "▲";
            this.button_hOffset_XUp.UseVisualStyleBackColor = true;
            // 
            // button_hOffset_XDown
            // 
            this.button_hOffset_XDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_hOffset_XDown.Location = new System.Drawing.Point(346, 357);
            this.button_hOffset_XDown.Name = "button_hOffset_XDown";
            this.button_hOffset_XDown.Size = new System.Drawing.Size(70, 44);
            this.button_hOffset_XDown.TabIndex = 17;
            this.button_hOffset_XDown.Text = "▼";
            this.button_hOffset_XDown.UseVisualStyleBackColor = true;
            // 
            // button_hOffset_YUp
            // 
            this.button_hOffset_YUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_hOffset_YUp.Location = new System.Drawing.Point(267, 411);
            this.button_hOffset_YUp.Name = "button_hOffset_YUp";
            this.button_hOffset_YUp.Size = new System.Drawing.Size(68, 44);
            this.button_hOffset_YUp.TabIndex = 19;
            this.button_hOffset_YUp.Text = "▲";
            this.button_hOffset_YUp.UseVisualStyleBackColor = true;
            // 
            // button_hOffset_YDown
            // 
            this.button_hOffset_YDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_hOffset_YDown.Location = new System.Drawing.Point(346, 411);
            this.button_hOffset_YDown.Name = "button_hOffset_YDown";
            this.button_hOffset_YDown.Size = new System.Drawing.Size(70, 44);
            this.button_hOffset_YDown.TabIndex = 20;
            this.button_hOffset_YDown.Text = "▼";
            this.button_hOffset_YDown.UseVisualStyleBackColor = true;
            // 
            // materialLabel_Gap
            // 
            this.materialLabel_Gap.AutoSize = true;
            this.materialLabel_Gap.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel_Gap.Location = new System.Drawing.Point(19, 312);
            this.materialLabel_Gap.Name = "materialLabel_Gap";
            this.materialLabel_Gap.Size = new System.Drawing.Size(100, 25);
            this.materialLabel_Gap.TabIndex = 30;
            this.materialLabel_Gap.Text = "간격(mm)";
            // 
            // materialLabel_OffsetX
            // 
            this.materialLabel_OffsetX.AutoSize = true;
            this.materialLabel_OffsetX.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel_OffsetX.Location = new System.Drawing.Point(19, 368);
            this.materialLabel_OffsetX.Name = "materialLabel_OffsetX";
            this.materialLabel_OffsetX.Size = new System.Drawing.Size(166, 25);
            this.materialLabel_OffsetX.TabIndex = 31;
            this.materialLabel_OffsetX.Text = "가로 오프셋(ｍｍ)";
            // 
            // materialLabel_OffsetY
            // 
            this.materialLabel_OffsetY.AutoSize = true;
            this.materialLabel_OffsetY.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialLabel_OffsetY.Location = new System.Drawing.Point(19, 425);
            this.materialLabel_OffsetY.Name = "materialLabel_OffsetY";
            this.materialLabel_OffsetY.Size = new System.Drawing.Size(166, 25);
            this.materialLabel_OffsetY.TabIndex = 32;
            this.materialLabel_OffsetY.Text = "세로 오프셋(ｍｍ)";
            // 
            // materialComboBox_PrinterList
            // 
            this.materialComboBox_PrinterList.AutoResize = false;
            this.materialComboBox_PrinterList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.materialComboBox_PrinterList.Depth = 0;
            this.materialComboBox_PrinterList.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.materialComboBox_PrinterList.DropDownHeight = 174;
            this.materialComboBox_PrinterList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.materialComboBox_PrinterList.DropDownWidth = 121;
            this.materialComboBox_PrinterList.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.materialComboBox_PrinterList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialComboBox_PrinterList.FormattingEnabled = true;
            this.materialComboBox_PrinterList.IntegralHeight = false;
            this.materialComboBox_PrinterList.ItemHeight = 43;
            this.materialComboBox_PrinterList.Location = new System.Drawing.Point(115, 14);
            this.materialComboBox_PrinterList.MaxDropDownItems = 4;
            this.materialComboBox_PrinterList.MouseState = MaterialSkin.MouseState.OUT;
            this.materialComboBox_PrinterList.Name = "materialComboBox_PrinterList";
            this.materialComboBox_PrinterList.Size = new System.Drawing.Size(285, 49);
            this.materialComboBox_PrinterList.StartIndex = 0;
            this.materialComboBox_PrinterList.TabIndex = 0;
            // 
            // Slider_darkness
            // 
            this.Slider_darkness.Location = new System.Drawing.Point(115, 128);
            this.Slider_darkness.Maximum = 15;
            this.Slider_darkness.Name = "Slider_darkness";
            this.Slider_darkness.Size = new System.Drawing.Size(387, 40);
            this.Slider_darkness.TabIndex = 3;
            this.Slider_darkness.Value = 8;
            // 
            // Slider_Speed
            // 
            this.Slider_Speed.Location = new System.Drawing.Point(115, 76);
            this.Slider_Speed.Maximum = 8;
            this.Slider_Speed.Name = "Slider_Speed";
            this.Slider_Speed.Size = new System.Drawing.Size(387, 40);
            this.Slider_Speed.TabIndex = 2;
            this.Slider_Speed.Value = 3;
            // 
            // RadioButton_continues
            // 
            this.RadioButton_continues.BackColor = System.Drawing.Color.Transparent;
            this.RadioButton_continues.Checked = false;
            this.RadioButton_continues.Cursor = System.Windows.Forms.Cursors.Hand;
            this.RadioButton_continues.Depth = 0;
            this.RadioButton_continues.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.RadioButton_continues.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.RadioButton_continues.Location = new System.Drawing.Point(473, 186);
            this.RadioButton_continues.Margin = new System.Windows.Forms.Padding(0);
            this.RadioButton_continues.MouseLocation = new System.Drawing.Point(-1, -1);
            this.RadioButton_continues.MouseState = BarcodePrint.Controll.MouseState.HOVER;
            this.RadioButton_continues.Name = "RadioButton_continues";
            this.RadioButton_continues.Ripple = true;
            this.RadioButton_continues.Size = new System.Drawing.Size(182, 37);
            this.RadioButton_continues.TabIndex = 7;
            this.RadioButton_continues.Text = "CONTINUES";
            this.RadioButton_continues.UseVisualStyleBackColor = true;
            // 
            // RadioButton_bline
            // 
            this.RadioButton_bline.BackColor = System.Drawing.Color.Transparent;
            this.RadioButton_bline.Checked = false;
            this.RadioButton_bline.Cursor = System.Windows.Forms.Cursors.Hand;
            this.RadioButton_bline.Depth = 0;
            this.RadioButton_bline.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.RadioButton_bline.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.RadioButton_bline.Location = new System.Drawing.Point(266, 186);
            this.RadioButton_bline.Margin = new System.Windows.Forms.Padding(0);
            this.RadioButton_bline.MouseLocation = new System.Drawing.Point(-1, -1);
            this.RadioButton_bline.MouseState = BarcodePrint.Controll.MouseState.HOVER;
            this.RadioButton_bline.Name = "RadioButton_bline";
            this.RadioButton_bline.Ripple = true;
            this.RadioButton_bline.Size = new System.Drawing.Size(200, 37);
            this.RadioButton_bline.TabIndex = 6;
            this.RadioButton_bline.Text = "BLACK MARK";
            this.RadioButton_bline.UseVisualStyleBackColor = true;
            // 
            // RadioButton_Gap
            // 
            this.RadioButton_Gap.BackColor = System.Drawing.Color.Transparent;
            this.RadioButton_Gap.Checked = false;
            this.RadioButton_Gap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.RadioButton_Gap.Depth = 0;
            this.RadioButton_Gap.Font = new System.Drawing.Font("굴림", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.RadioButton_Gap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.RadioButton_Gap.Location = new System.Drawing.Point(121, 186);
            this.RadioButton_Gap.Margin = new System.Windows.Forms.Padding(0);
            this.RadioButton_Gap.MouseLocation = new System.Drawing.Point(-1, -1);
            this.RadioButton_Gap.MouseState = BarcodePrint.Controll.MouseState.HOVER;
            this.RadioButton_Gap.Name = "RadioButton_Gap";
            this.RadioButton_Gap.Ripple = true;
            this.RadioButton_Gap.Size = new System.Drawing.Size(145, 37);
            this.RadioButton_Gap.TabIndex = 5;
            this.RadioButton_Gap.Text = "GAP";
            this.RadioButton_Gap.UseVisualStyleBackColor = true;
            // 
            // textBox_Gap
            // 
            this.textBox_Gap.BackColor = System.Drawing.Color.White;
            this.textBox_Gap.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_Gap.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Gap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.textBox_Gap.Location = new System.Drawing.Point(191, 303);
            this.textBox_Gap.Name = "textBox_Gap";
            this.textBox_Gap.Size = new System.Drawing.Size(67, 31);
            this.textBox_Gap.TabIndex = 12;
            this.textBox_Gap.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox_hOffset_X
            // 
            this.textBox_hOffset_X.BackColor = System.Drawing.Color.White;
            this.textBox_hOffset_X.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_hOffset_X.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_hOffset_X.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.textBox_hOffset_X.Location = new System.Drawing.Point(191, 362);
            this.textBox_hOffset_X.Name = "textBox_hOffset_X";
            this.textBox_hOffset_X.Size = new System.Drawing.Size(67, 31);
            this.textBox_hOffset_X.TabIndex = 15;
            this.textBox_hOffset_X.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBox_hOffset_Y
            // 
            this.textBox_hOffset_Y.BackColor = System.Drawing.Color.White;
            this.textBox_hOffset_Y.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox_hOffset_Y.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_hOffset_Y.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.textBox_hOffset_Y.Location = new System.Drawing.Point(193, 419);
            this.textBox_hOffset_Y.Name = "textBox_hOffset_Y";
            this.textBox_hOffset_Y.Size = new System.Drawing.Size(65, 31);
            this.textBox_hOffset_Y.TabIndex = 18;
            this.textBox_hOffset_Y.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // AdjustForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(800, 700);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel_AdjustSet);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.FormStyle = MaterialSkin.Controls.MaterialForm.FormStyles.ActionBar_None;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(800, 700);
            this.MinimizeBox = false;
            this.Name = "AdjustForm";
            this.Padding = new System.Windows.Forms.Padding(2, 16, 2, 2);
            this.Text = "AdjustForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AdjustForm_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label materialLabel1;
        private System.Windows.Forms.Label materialLabel2;
        private System.Windows.Forms.Label materialLabel4;
        private MaterialSkin.Controls.MaterialSwitch Switch_Ribbon;
        private System.Windows.Forms.Panel panel_AdjustSet;
        private System.Windows.Forms.Label materialLabel5;
        private BarcodePrint.Controll.CustomRadioButton RadioButton_Gap;
        private BarcodePrint.Controll.CustomRadioButton RadioButton_bline;
        private BarcodePrint.Controll.CustomRadioButton RadioButton_continues;
        private BarcodePrint.UserControls.TouchSlider Slider_Speed;
        private System.Windows.Forms.Panel panel1;
        private BarcodePrint.Controll.CustomTextBox textBox_Gap;
        private BarcodePrint.Controll.CustomTextBox textBox_hOffset_X;
        private BarcodePrint.Controll.CustomTextBox textBox_hOffset_Y;
        private System.Windows.Forms.Button button_GapUp;
        private System.Windows.Forms.Button button_GapDown;
        private System.Windows.Forms.Button button_hOffset_XUp;
        private System.Windows.Forms.Button button_hOffset_XDown;
        private System.Windows.Forms.Button button_hOffset_YUp;
        private System.Windows.Forms.Button button_hOffset_YDown;
        private System.Windows.Forms.Label materialLabel_Gap;
        private System.Windows.Forms.Label materialLabel_OffsetX;
        private System.Windows.Forms.Label materialLabel_OffsetY;
        private MaterialSkin.Controls.MaterialComboBox materialComboBox_PrinterList;
        private BarcodePrint.UserControls.TouchSlider Slider_darkness;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel_Calibration;
        private System.Windows.Forms.Label label_Value_Speed;
        private System.Windows.Forms.Label label_Value_Darkness;
    }
}