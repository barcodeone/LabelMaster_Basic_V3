namespace BarcodePrint.View
{
    partial class LicenseForm
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
            this.richTextBox_License = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBox_License
            // 
            this.richTextBox_License.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_License.Location = new System.Drawing.Point(3, 64);
            this.richTextBox_License.Name = "richTextBox_License";
            this.richTextBox_License.ReadOnly = true;
            this.richTextBox_License.Size = new System.Drawing.Size(594, 383);
            this.richTextBox_License.TabIndex = 0;
            this.richTextBox_License.Text = "";
            // 
            // LicenseForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(600, 450);
            this.Controls.Add(this.richTextBox_License);
            this.Name = "LicenseForm";
            this.Text = "라이선스 정보";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox_License;
    }
}
