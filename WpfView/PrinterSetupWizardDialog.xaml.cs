using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    /// <summary>
    /// 실제 온라인 상태인 프린터를 검색하고 즉시 연결하는 다이얼로그 (불필요한 마법사 단계 제거)
    /// </summary>
    public partial class PrinterSetupWizardDialog : Window
    {
        private List<OnlinePrinterItem> _onlinePrinters = new List<OnlinePrinterItem>();

        public PrinterSetupWizardDialog()
        {
            InitializeComponent();
            LoadPrinters();
        }

        private void LoadPrinters()
        {
            CboPrinters.Items.Clear();

            var settings = new AppSettings();
            settings.Load();
            string lastSelected = settings.LastSelectedPrinterName ?? "";

            // 1. 실제 온라인 상태인 라벨 프린터만 먼저 조회
            _onlinePrinters = PrinterOnlineDetector.GetOnlinePrinters(labelPrintersOnly: true);

            if (_onlinePrinters.Count > 0)
            {
                TxtPrinterNotice.Text = $"✔ 실제 온라인(연결됨) 상태인 라벨 프린터 {_onlinePrinters.Count}대가 감지되었습니다.";
                TxtPrinterNotice.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                int targetIdx = 0;
                for (int i = 0; i < _onlinePrinters.Count; i++)
                {
                    var item = _onlinePrinters[i];
                    string display = $"{item.Name} ({item.ConnectionType}: {item.PortName})";
                    var cboItem = new ComboBoxItem
                    {
                        Content = display,
                        Tag = item
                    };
                    CboPrinters.Items.Add(cboItem);

                    if (!string.IsNullOrEmpty(lastSelected) && item.Name.Equals(lastSelected, StringComparison.OrdinalIgnoreCase))
                    {
                        targetIdx = i;
                    }
                }

                CboPrinters.SelectedIndex = targetIdx;
            }
            else
            {
                // 라벨 프린터가 온라인이 아닐 경우: 일반 물리 온라인 프린터 확인
                var anyOnline = PrinterOnlineDetector.GetOnlinePrinters(labelPrintersOnly: false);
                if (anyOnline.Count > 0)
                {
                    _onlinePrinters = anyOnline;
                    TxtPrinterNotice.Text = $"ℹ️ 온라인 프린터 {anyOnline.Count}대가 감지되었습니다.";
                    TxtPrinterNotice.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235));

                    for (int i = 0; i < anyOnline.Count; i++)
                    {
                        var item = anyOnline[i];
                        string display = $"{item.Name} ({item.ConnectionType})";
                        var cboItem = new ComboBoxItem { Content = display, Tag = item };
                        CboPrinters.Items.Add(cboItem);
                    }
                    CboPrinters.SelectedIndex = 0;
                }
                else
                {
                    // 완전 오프라인: 시스템에 설치된 프린터 목록 표시
                    TxtPrinterNotice.Text = "⚠️ 온라인 라벨 프린터가 감지되지 않았습니다. 전원 및 케이블 연결을 확인하세요.";
                    TxtPrinterNotice.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));

                    var installed = PrinterSettings.InstalledPrinters;
                    for (int i = 0; i < installed.Count; i++)
                    {
                        string p = installed[i];
                        var item = new OnlinePrinterItem
                        {
                            Name = p,
                            PortName = "-",
                            ConnectionType = "UNKNOWN",
                            IsOnline = false
                        };
                        var cboItem = new ComboBoxItem
                        {
                            Content = $"{p} [오프라인]",
                            Tag = item
                        };
                        CboPrinters.Items.Add(cboItem);
                    }

                    if (CboPrinters.Items.Count > 0)
                    {
                        CboPrinters.SelectedIndex = 0;
                    }
                }
            }
        }

        private OnlinePrinterItem GetSelectedPrinterItem()
        {
            if (CboPrinters.SelectedItem is ComboBoxItem cboItem && cboItem.Tag is OnlinePrinterItem item)
            {
                return item;
            }
            return null;
        }

        public string GetSelectedPrinterName()
        {
            var item = GetSelectedPrinterItem();
            if (item != null) return item.Name;
            if (CboPrinters.SelectedItem is ComboBoxItem ci) return ci.Content?.ToString() ?? "";
            return CboPrinters.SelectedItem?.ToString() ?? "";
        }

        private void OnPrinterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = GetSelectedPrinterItem();
            if (item != null)
            {
                string portDisplay = FormatCompactPort(item.PortName, item.ConnectionType);
                string driverDisplay = !string.IsNullOrEmpty(item.DriverName) ? $" | 드라이버: {item.DriverName}" : "";
                TxtSelectedPrinterPort.Text = $"포트: {portDisplay}{driverDisplay}";
                if (item.IsOnline)
                {
                    TxtSelectedPrinterStatus.Text = "상태: 🟢 정상 온라인 (인쇄 준비 완료)";
                    TxtSelectedPrinterStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    TxtSelectedPrinterStatus.Text = "상태: 🔴 오프라인 (전원 꺼짐 또는 통신 불가)";
                    TxtSelectedPrinterStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            else
            {
                TxtSelectedPrinterPort.Text = "포트: -";
                TxtSelectedPrinterStatus.Text = "상태: 확인 불가";
                TxtSelectedPrinterStatus.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
        }

        private string FormatCompactPort(string portName, string connType)
        {
            if (string.IsNullOrWhiteSpace(portName) || portName == "-")
            {
                return !string.IsNullOrEmpty(connType) && connType != "UNKNOWN" ? $"({connType})" : "-";
            }

            string clean = portName.Trim();

            // WSD 포트 (예: WSD-72b83aa8-db38-4c06-b3a7-4b0207bdab8b) -> 간략화
            if (clean.StartsWith("WSD", StringComparison.OrdinalIgnoreCase))
            {
                return $"WSD ({connType})";
            }

            // TCP/IP IP_ 포트 (예: IP_192.168.0.100) -> 192.168.0.100 (LAN)
            if (clean.StartsWith("IP_", StringComparison.OrdinalIgnoreCase))
            {
                return $"{clean.Substring(3)} ({connType})";
            }

            // 16자 이상 긴 포트명 축약
            if (clean.Length > 16)
            {
                return $"{clean.Substring(0, 13)}... ({connType})";
            }

            return $"{clean} ({connType})";
        }

        private void OnRefreshPrintersClick(object sender, RoutedEventArgs e)
        {
            LoadPrinters();
        }

        private bool SendDirectTsplCommand(string cmd, out string err)
        {
            err = "";
            string printerName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(printerName))
            {
                err = "프린터가 선택되지 않았습니다.";
                return false;
            }

            var item = GetSelectedPrinterItem();
            var cachedPrinters = new List<UsbPrinterInfo>();
            if (item != null)
            {
                cachedPrinters.Add(new UsbPrinterInfo
                {
                    Name = item.Name,
                    FriendlyName = item.Name.Replace("[블루투스]", "").Trim(),
                    PortName = item.PortName,
                    Type = item.ConnectionType,
                    Address = item.PortName
                });
            }

            var svc = new BarcodePrint.Services.PrinterService();
            return svc.SendTsplRawCommand(printerName, cmd, cachedPrinters, out err);
        }

        private void OnQuickTestFeedClick(object sender, RoutedEventArgs e)
        {
            string printerName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("프린터를 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool ok = SendDirectTsplCommand("FORMFEED\r\n", out string err);
                if (ok)
                {
                    MessageBox.Show($"[{printerName}] 프린터로 연결 테스트(라벨 1장 급지) 명령이 성공적으로 전송되었습니다.", "연결테스트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"연결테스트 명령 전송 실패: {err}\n프린터 전원 및 연결 상태를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"연결테스트 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnQuickCalibrateClick(object sender, RoutedEventArgs e)
        {
            string printerName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("프린터를 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 안드로이드 TsplImageBuilder 표준 센서 보정: GAP 2mm 설정 -> CLS -> GAPDETECT 단일 수행 (중복 AUTODETECT 및 HOME 제거)
                string cmd = "GAP 2 mm,0 mm\r\nCLS\r\nGAPDETECT\r\n";
                bool ok = SendDirectTsplCommand(cmd, out string err);
                if (ok)
                {
                    MessageBox.Show($"[{printerName}] 프린터로 용지센싱(자동 측정 및 보정) 명령이 전송되었습니다.\n용지 1~2장을 급지하며 센서 측정을 완료합니다.", "용지센싱 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"용지센싱 명령 전송 실패: {err}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"용지센싱 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnQuickSelfTestClick(object sender, RoutedEventArgs e)
        {
            string printerName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("프린터를 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"[{printerName}] 프린터로 자체 하드웨어 진단(SELFTEST) 라벨을 인쇄하시겠습니까?", "셀프 테스트 인쇄", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            try
            {
                bool ok = SendDirectTsplCommand("SELFTEST\r\n", out string err);
                if (ok)
                {
                    MessageBox.Show("셀프 테스트 인쇄 명령이 전송되었습니다.", "셀프 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"명령 전송 실패: {err}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"셀프 테스트 명령 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnQuickPrinterInfoClick(object sender, RoutedEventArgs e)
        {
            string printerName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("프린터를 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var prevCursor = System.Windows.Input.Mouse.OverrideCursor;
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                var item = GetSelectedPrinterItem();
                var svc = new BarcodePrint.Services.PrinterService();
                // 전체 프린터 목록 전수조사(GetVerifiedPrinters)를 건너뛰고 선택된 포트 정보 직접 주입으로 즉시 조회 (초고속)
                var cachedPrinters = new System.Collections.Generic.List<BarcodePrint.Model.UsbPrinterInfo>();
                if (item != null)
                {
                    cachedPrinters.Add(new BarcodePrint.Model.UsbPrinterInfo
                    {
                        Name = printerName,
                        PortName = item.PortName,
                        Type = item.ConnectionType
                    });
                }
                var details = await System.Threading.Tasks.Task.Run(() => svc.GetPrinterHardwareDetails(printerName, cachedPrinters));

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[🖨️ {printerName} 하드웨어 상세 정보]");
                if (item != null)
                {
                    sb.AppendLine($"• 포트: {item.PortName} ({item.ConnectionType})");
                    sb.AppendLine($"• 드라이버: {item.DriverName}");
                }
                sb.AppendLine();
                foreach (var kv in details)
                {
                    sb.AppendLine($"• {kv.Key} : {kv.Value}");
                }

                System.Windows.Input.Mouse.OverrideCursor = prevCursor;
                MessageBox.Show(sb.ToString(), "프린터 상세 정보 (TSPL)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.Input.Mouse.OverrideCursor = prevCursor;
                MessageBox.Show($"프린터 정보 조회 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = prevCursor;
            }
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            string selected = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(selected))
            {
                MessageBox.Show("연결할 프린터를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = new AppSettings();
            settings.Load();
            settings.LastSelectedPrinterName = selected;
            settings.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
