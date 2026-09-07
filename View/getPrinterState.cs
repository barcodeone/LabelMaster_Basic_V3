using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Windows.Forms;
using BarcodePrint.Controll;
using BarcodePrint.Model;

namespace BarcodePrint.View
{
    internal class getPrinterState {  }
    public partial class MainForm
    {
        private System.Windows.Forms.Timer blinkTimer = new System.Windows.Forms.Timer();

        private void ReceiveEventPrinterStateUpdate(object sender, string state)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ReceiveEventPrinterStateUpdate 호출: {state}");
                
                // 빈 상태나 null 상태는 무시 (초기 상태 유지)
                if (string.IsNullOrWhiteSpace(state))
                {
                    System.Diagnostics.Debug.WriteLine("빈 상태 메시지 무시");
                    return;
                }
                
                if (label_PrinterStatus.InvokeRequired)
                {
                    label_PrinterStatus.Invoke((MethodInvoker)delegate ()
                    {
                        UpdatePrinterStatus(state);
                    });
                }
                else
                {
                    UpdatePrinterStatus(state);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReceiveEventPrinterStateUpdate 오류: {ex.Message}");
            }
        }

        private void UpdatePrinterStatus(string state)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePrinterStatus 호출: '{state}'");
                
                // 상태 텍스트 정리 (줄바꿈 제거)
                string cleanState = state?.Trim().Replace("\r\n", "").Replace("\n", "");
                
                if (string.IsNullOrEmpty(cleanState))
                {
                    cleanState = "연결된 프린터가 없습니다.";
                }
                
                label_PrinterStatus.Text = cleanState;
                System.Diagnostics.Debug.WriteLine($"라벨 텍스트 업데이트: '{cleanState}'");

                if (cleanState.Contains("인쇄 대기중") || cleanState.Contains("인쇄 중"))
                {
                    blinkTimer.Stop();
                    blinkTimer.Tick -= BlinkLabel;
                    label_PrinterStatus.ForeColor = System.Drawing.Color.FromArgb(93, 53, 19);
                    System.Diagnostics.Debug.WriteLine("정상 상태 - 깜빡임 중지");
                }
                else if (cleanState.Contains("프린터를 연결해 주세요") || cleanState.Contains("연결된 프린터가 없습니다"))
                {
                    label_PrinterStatus.ForeColor = System.Drawing.Color.FromArgb(93, 53, 19);
                    if (!blinkTimer.Enabled)
                    {
                        blinkTimer.Interval = 1000;
                        blinkTimer.Tick += BlinkLabel;
                        blinkTimer.Start();
                        System.Diagnostics.Debug.WriteLine("연결 오류 - 깜빡임 시작");
                    }
                }
                else
                {
                    if (!blinkTimer.Enabled)
                    {
                        blinkTimer.Interval = 1000;
                        blinkTimer.Tick += BlinkLabel;
                        blinkTimer.Start();
                        System.Diagnostics.Debug.WriteLine("오류 상태 - 깜빡임 시작");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePrinterStatus 오류: {ex.Message}");
            }
        }
        // 1초마다 라벨 색상 깜빡이기        
        private void BlinkLabel(object sender, EventArgs e)
        {

            if (label_PrinterStatus.ForeColor == Color.FromArgb(132, 109, 90))
            { //
                label_PrinterStatus.ForeColor = Color.FromArgb(93, 53, 19);
            }
            else
            {
                label_PrinterStatus.ForeColor = Color.FromArgb(132, 109, 90);
            }
        }

        /// <summary>
        /// 프린터 선택 변경 시 호출되는 메서드 (AdjustForm에서 호출됨)
        /// </summary>
        /// <param name="printerName">선택된 프린터 컬럼3</param>
        public void MachineList_selectChanged(string printerName = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"MachineList_selectChanged 호출됨 - 프린터: {printerName ?? "자동설정"}");

                // 프린터 정보 초기화
                PRINT_INFO_CLASS.PrinterInfo.printerPortName = "";
                PRINT_INFO_CLASS.PrinterInfo.printerName = "";

                // 프린터 컬럼3이 제공되지 않은 경우 현재 설정된 프린터 사용
                string targetPrinterName = printerName ?? PRINT_INFO_CLASS.PrinterInfo.printerName;

                if (string.IsNullOrEmpty(targetPrinterName))
                {
                    System.Diagnostics.Debug.WriteLine("MachineList_selectChanged - 프린터 컬럼3이 비어있음");
                    return;
                }

                // USB_status에서 해당 프린터 찾기
                for (int i = 0; i < USB_status.Name.Count; i++)
                {
                    if (string.Equals(USB_status.Name[i]?.Trim(), targetPrinterName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // 프린터 정보 업데이트
                        PRINT_INFO_CLASS.PrinterInfo.printerPortName = USB_status.Port[i];
                        PRINT_INFO_CLASS.PrinterInfo.printerName = targetPrinterName;
                        PRINT_INFO_CLASS.PrinterInfo.printer_DPI = USB_status.DPI[i];

                        System.Diagnostics.Debug.WriteLine($"프린터 정보 업데이트: {targetPrinterName} (포트: {USB_status.Port[i]}, DPI: {USB_status.DPI[i]})");

                        // [추가] 실시간으로 수동 연결 변경된 프린터 정보를 통합 설정에 영구 저장
                        if (appSettings != null)
                        {
                            appSettings.LastPrinterName = targetPrinterName;
                            appSettings.LastPrinterPort = USB_status.Port[i];
                            appSettings.Save();
                            System.Diagnostics.Debug.WriteLine($"[설정 저장] 실시간 변경된 프린터 정보 저장: {targetPrinterName} (포트: {USB_status.Port[i]})");
                        }

                        // 기존 프린터 상태 스레드가 있으면 안전하게 종료
                        if (printStatusThread != null && printStatusThread.IsAlive)
                        {
                            System.Diagnostics.Debug.WriteLine("기존 프린터 상태 스레드 종료 중...");
                            
                            int timeout = 5000; // 5초
                            DateTime startTime = DateTime.Now;

                            while (printStatusThread.IsAlive)
                            {
                                try
                                {
                                    printStatusThread.Interrupt();
                                    Thread.Sleep(100);

                                    // 타임아웃 체크
                                    if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                                    {
                                        System.Diagnostics.Debug.WriteLine("프린터 상태 스레드 종료 타임아웃");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"스레드 종료 중 오류: {ex.Message}");
                                    break;
                                }
                            }
                            
                            printStatusThread = null;
                        }

                        // 새로운 프린터 상태 스레드 시작
                        try
                        {
                            printStatusThread = new Thread(previewManager.OnStateCheck)
                            {
                                IsBackground = true,
                                Name = "PrinterStatusThread"
                            };
                            printStatusThread.Start();
                            System.Diagnostics.Debug.WriteLine($"새로운 프린터 상태 스레드 시작: {targetPrinterName}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"새로운 프린터 상태 스레드 시작 오류: {ex.Message}");
                        }

                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"프린터를 찾을 수 없음: {targetPrinterName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MachineList_selectChanged 오류: {ex.Message}");
            }
        }


        public void UpdatePrinterStatusTextOnly(string text, Color? textColor = null)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (label_PrinterStatus.InvokeRequired)
            {
                label_PrinterStatus.Invoke(new Action<string, Color?>(UpdatePrinterStatusTextOnly), text, textColor);
            }
            else
            {
                label_PrinterStatus.Text = text;
                if (textColor.HasValue)
                {
                    label_PrinterStatus.ForeColor = textColor.Value;
                }
                else
                {
                    label_PrinterStatus.ForeColor = Color.FromArgb(93, 53, 19); // 기본 브라운톤 색상
                }
            }
        }

        public void ShowPrinterErrorMessageBox(int errorPage, string reason)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, string>(ShowPrinterErrorMessageBox), errorPage, reason);
            }
            else
            {
                string suffix = string.IsNullOrEmpty(reason) ? "" : $" ({reason})";
                MessageBox.Show(this, 
                    $"오류가 발생하여 인쇄가 중단되었습니다.\n\n▶ 멈춤 지점: {errorPage}번째 페이지{suffix}\n\n프린터의 용지, 리본, 커버 등을 확인 후 조치해 주세요.", 
                    "프린터 오류 감지", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
    }
}
