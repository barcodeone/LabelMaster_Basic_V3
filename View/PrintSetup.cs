using Argon;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodePrint.Model;

namespace BarcodePrint.View
{
    internal class PrintSetup { }

    public partial class AdjustForm
    {

        // 컨트롤의 값을 클래스에 저장
        private void PrintSetting()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintSetting] 시작 - 컨트롤 값 저장");
                
                // 라벨 사이즈는 이제 LabelLayoutSettingsForm에서 관리하므로 여기서 저장하지 않음
            
            if (textBox_Gap.Text == "")
            {
                PRINT_INFO_CLASS.PrinterInfo.nGAP = 1;
            }
            else {
                PRINT_INFO_CLASS.PrinterInfo.nGAP = Convert.ToInt32(textBox_Gap.Text);
            }

            if (textBox_hOffset_X.Text == "")
            {
                PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X = 0;
            }
            else {
                PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X = Convert.ToInt32(textBox_hOffset_X.Text);
            }

            if (textBox_hOffset_Y.Text == "")
            {
                PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y = 0;
            }
            else {
                PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y = Convert.ToInt32(textBox_hOffset_Y.Text);
            }
            
            PRINT_INFO_CLASS.PrinterInfo.nDarkness = Convert.ToInt32(Slider_darkness.Value);
            PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed = Convert.ToInt32(Slider_Speed.Value);

            PRINT_INFO_CLASS.PrinterInfo.bRibbon = Switch_Ribbon.Checked;

            if (RadioButton_bline.Checked)
            {
                PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.BLINE;
            }
            else if (RadioButton_Gap.Checked)
            {
                PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.GAP;
            }
            else
            {
                PRINT_INFO_CLASS.PrinterInfo.PaperType = PAPER_TYPE.CONTINEUS;
            }

                // Paper Type 설정 저장
                string settingsPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xprinter", "printer_settings");
                PRINT_INFO_CLASS.SaveToFile(settingsPath);
                System.Diagnostics.Debug.WriteLine($"[PrintSetting 저장 완료] {settingsPath}");
                System.Diagnostics.Debug.WriteLine($"  - PaperType: {PRINT_INFO_CLASS.PrinterInfo.PaperType}");
                System.Diagnostics.Debug.WriteLine($"  - Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width} mm");
                System.Diagnostics.Debug.WriteLine($"  - Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height} mm");
                System.Diagnostics.Debug.WriteLine($"  - nDarkness: {PRINT_INFO_CLASS.PrinterInfo.nDarkness}");
                System.Diagnostics.Debug.WriteLine($"  - nPrintSpeed: {PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintSetting 오류] {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
        }
    }
}
