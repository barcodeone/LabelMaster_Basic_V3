namespace BarcodePrint.View
{
    partial class ProgramInfoForm
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
            this.label_Info = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label_Info
            // 
            this.label_Info.AutoSize = true;
            this.label_Info.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label_Info.Location = new System.Drawing.Point(20, 20);
            this.label_Info.Name = "label_Info";
            this.label_Info.Size = new System.Drawing.Size(280, 168);
            this.label_Info.TabIndex = 0;
            this.label_Info.Text = "프로그램: 라벨마스터 베이직\r\n버전: 1.1.0\r\n\r\n개발: 한국Xprinter\r\n홈페이지: https://xprinter.co.kr\r\n\r" +
    "\n이 프로그램은 라벨 인쇄를 위한\r\n한국Xprinter 전용 소프트웨어입니다.";
            // 
            // ProgramInfoForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(450, 250);
            this.Controls.Add(this.label_Info);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgramInfoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "프로그램 정보";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Info;
    }
}

