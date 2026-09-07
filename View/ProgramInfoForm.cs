using System;
using System.Windows.Forms;

namespace BarcodePrint.View
{
    public partial class ProgramInfoForm : Form
    {
        public ProgramInfoForm()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}

