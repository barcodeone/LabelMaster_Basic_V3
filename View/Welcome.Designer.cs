namespace BarcodePrint.View
{
    partial class Welcome
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_AccessKey = new System.Windows.Forms.TextBox();
            this.button_login = new System.Windows.Forms.Button();
            this.checkBox_keySave = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("돋움", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(32, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "AccessKey : ";
            // 
            // textBox_AccessKey
            // 
            this.textBox_AccessKey.Font = new System.Drawing.Font("굴림", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.textBox_AccessKey.Location = new System.Drawing.Point(155, 19);
            this.textBox_AccessKey.Name = "textBox_AccessKey";
            this.textBox_AccessKey.Size = new System.Drawing.Size(290, 22);
            this.textBox_AccessKey.TabIndex = 1;
            // 
            // button_login
            // 
            this.button_login.BackColor = System.Drawing.Color.LightGray;
            this.button_login.Font = new System.Drawing.Font("돋움체", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.button_login.Location = new System.Drawing.Point(192, 61);
            this.button_login.Name = "button_login";
            this.button_login.Size = new System.Drawing.Size(148, 28);
            this.button_login.TabIndex = 2;
            this.button_login.Text = "Login";
            this.button_login.UseVisualStyleBackColor = false;
            // 
            // checkBox_keySave
            // 
            this.checkBox_keySave.AutoSize = true;
            this.checkBox_keySave.Font = new System.Drawing.Font("돋움체", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.checkBox_keySave.Location = new System.Drawing.Point(64, 67);
            this.checkBox_keySave.Name = "checkBox_keySave";
            this.checkBox_keySave.Size = new System.Drawing.Size(76, 16);
            this.checkBox_keySave.TabIndex = 3;
            this.checkBox_keySave.Text = "자동저장";
            this.checkBox_keySave.UseVisualStyleBackColor = true;
            // 
            // Welcome
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(517, 107);
            this.Controls.Add(this.checkBox_keySave);
            this.Controls.Add(this.button_login);
            this.Controls.Add(this.textBox_AccessKey);
            this.Controls.Add(this.label1);
            this.Name = "Welcome";
            this.Text = "Welcome";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Welcome_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_AccessKey;
        private System.Windows.Forms.Button button_login;
        private System.Windows.Forms.CheckBox checkBox_keySave;
    }
}