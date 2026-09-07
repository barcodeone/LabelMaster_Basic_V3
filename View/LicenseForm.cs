using System;
using System.Windows.Forms;
using System.IO;
using MaterialSkin.Controls;

namespace BarcodePrint.View
{
    public partial class LicenseForm : MaterialForm
    {
        public LicenseForm()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.None;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            LoadLicenseFile();
        }

        private void LoadLicenseFile()
        {
            try
            {
                string licensePath = Path.Combine(Application.StartupPath, "LICENSES.txt");
                if (File.Exists(licensePath))
                {
                    richTextBox_License.Text = File.ReadAllText(licensePath);
                }
                else
                {
                    richTextBox_License.Text = "라이선스 파일을 찾을 수 없습니다.";
                }
            }
            catch (Exception ex)
            {
                richTextBox_License.Text = $"라이선스 파일을 읽는 중 오류가 발생했습니다: {ex.Message}";
            }
        }
    }
}
