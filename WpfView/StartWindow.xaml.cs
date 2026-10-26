using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using BarcodePrint.Model;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    public partial class StartWindow : Window
    {
        private string _detectedPrinterName = string.Empty;
        private string _detectedPortName = string.Empty;
        private string _detectedConnType = string.Empty;
        private bool _isPrinterOnline = false;
        private System.Collections.Generic.List<PresetLabelItem> _presets;

        public StartWindow()
        {
            SetBrowserEmulationMode();
            InitializeComponent();
            try
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (ver != null && TxtAppVersion != null)
                {
                    TxtAppVersion.Text = $"라벨마스터 v{ver.Major}.{ver.Minor}.{ver.Build}";
                }
            }
            catch { }

            LoadRecentInfo();
            LoadPresets();
            CheckCurrentPrinterStatus();

            this.Loaded += (s, e) =>
            {
                LoadQuickToolsHtml();
            };
        }

        private void CheckCurrentPrinterStatus()
        {
            _ = CheckCurrentPrinterStatusAsync();
        }

        /// <summary>
        /// 실제 PC에 물리적으로 연결되어 켜져 있는(온라인) 라벨 프린터만 비동기로 감지하여 UI를 업데이트합니다. (UI 블로킹 0ms)
        /// </summary>
        private async System.Threading.Tasks.Task CheckCurrentPrinterStatusAsync()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                string savedPrinter = settings.LastSelectedPrinterName ?? "";

                if (!string.IsNullOrEmpty(savedPrinter))
                {
                    TxtPrinterStatus.Text = $"확인 중: {savedPrinter}";
                    TxtSidePrinterName.Text = savedPrinter;
                    TxtSidePrinterStatusText.Text = "연결 상태 확인 중...";
                }
                else
                {
                    TxtPrinterStatus.Text = "프린터 연결 확인 중...";
                    TxtSidePrinterStatusText.Text = "확인 중...";
                }

                // 1. 실제 온라인 라벨 프린터 목록 검색 (WMI 및 하드웨어 탐색을 백그라운드로 분리)
                var onlinePrinters = await System.Threading.Tasks.Task.Run(() =>
                    PrinterOnlineDetector.GetOnlinePrinters(labelPrintersOnly: true));

                if (onlinePrinters != null && onlinePrinters.Count > 0)
                {
                    // 사용자가 이전에 선택했던 프린터가 온라인 목록에 있으면 우선 선택, 없으면 첫 번째 온라인 라벨 프린터 선택
                    var target = onlinePrinters.FirstOrDefault(p => p.Name.Equals(savedPrinter, StringComparison.OrdinalIgnoreCase))
                                 ?? onlinePrinters.First();

                    string detectedPrinterName = target.Name;
                    string detectedPortName = target.PortName;
                    string detectedConnType = target.ConnectionType;

                    // 블루투스 프린터인 경우 COM 포트 번호 대신 실제 장치 이름 표시 보장
                    if (detectedConnType == "Bluetooth" &&
                        (detectedPrinterName.StartsWith("[블루투스] COM", StringComparison.OrdinalIgnoreCase) ||
                         detectedPrinterName.Contains("(COM") ||
                         detectedPrinterName.Equals("[블루투스] 블루투스 장치", StringComparison.OrdinalIgnoreCase)))
                    {
                        string realDev = await System.Threading.Tasks.Task.Run(() =>
                            UsbPrinterHelper.GetBluetoothDeviceNameForPort(detectedPortName));
                        detectedPrinterName = $"[블루투스] {realDev}";
                    }

                    _detectedPrinterName = detectedPrinterName;
                    _detectedPortName = detectedPortName;
                    _detectedConnType = detectedConnType;
                    _isPrinterOnline = true;

                    // 상단 헤더 뱃지 업데이트
                    DotPrinterStatus.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    if (DotSidebarPrinter != null) DotSidebarPrinter.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    TxtPrinterStatus.Text = $"연결됨: {_detectedPrinterName}";
                    BorderPrinterBadge.ToolTip = $"온라인 프린터: {_detectedPrinterName}\n포트: {_detectedPortName} ({_detectedConnType})";

                    // 사이드바 패널 업데이트
                    TxtSidePrinterName.Text = _detectedPrinterName;
                    TxtSidePrinterPort.Text = $"포트: {_detectedPortName} ({_detectedConnType})";
                    TxtSidePrinterStatusText.Text = "프린터 온라인";
                    TxtSidePrinterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    // 온라인 라벨 프린터 없음: 완전 오프라인으로 명확히 표시
                    _detectedPrinterName = string.Empty;
                    _detectedPortName = "-";
                    _detectedConnType = "-";
                    _isPrinterOnline = false;

                    DotPrinterStatus.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    if (DotSidebarPrinter != null) DotSidebarPrinter.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    TxtPrinterStatus.Text = "오프라인 (프린터 전원 꺼짐 / 미연결)";
                    BorderPrinterBadge.ToolTip = "연결된 라벨 프린터가 없거나 전원이 꺼져 있습니다.\n클릭하여 프린터 설정에서 장치를 확인하거나 연결하세요.";

                    TxtSidePrinterName.Text = "연결된 라벨 프린터 없음";
                    TxtSidePrinterPort.Text = "포트: -";
                    TxtSidePrinterStatusText.Text = "프린터 미연결";
                    TxtSidePrinterStatusText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            catch (Exception ex)
            {
                _isPrinterOnline = false;
                DotPrinterStatus.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
                if (DotSidebarPrinter != null) DotSidebarPrinter.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                TxtPrinterStatus.Text = "프린터 상태 확인 불가";
                BorderPrinterBadge.ToolTip = $"프린터 감지 오류: {ex.Message}";
                Debug.WriteLine($"[StartWindow] CheckCurrentPrinterStatus Error: {ex.Message}");
            }
        }

        private void LoadRecentInfo()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();

                var recentList = settings.RecentWorks ?? new System.Collections.Generic.List<RecentWorkItem>();

                // 기존 설정 파일 호환: 리스트는 비었지만 이전 규격이 있는 경우
                if (recentList.Count == 0 && settings.PaperWidth > 0 && settings.PaperHeight > 0 && settings.HasRecentWork)
                {
                    settings.AddRecentWork("이전 작업 이어서 하기", settings.PaperWidth, settings.PaperHeight);
                    recentList = settings.RecentWorks ?? new System.Collections.Generic.List<RecentWorkItem>();
                }

                if (recentList.Count == 0)
                {
                    if (CardRecentEmpty != null) CardRecentEmpty.Visibility = Visibility.Visible;
                    if (BtnRecent0 != null) BtnRecent0.Visibility = Visibility.Collapsed;
                    if (BtnRecent1 != null) BtnRecent1.Visibility = Visibility.Collapsed;
                    if (BtnRecent2 != null) BtnRecent2.Visibility = Visibility.Collapsed;
                    if (BtnRecent3 != null) BtnRecent3.Visibility = Visibility.Collapsed;
                    return;
                }

                if (CardRecentEmpty != null) CardRecentEmpty.Visibility = Visibility.Collapsed;

                Button[] buttons = new[] { BtnRecent0, BtnRecent1, BtnRecent2, BtnRecent3 };
                TextBlock[] titles = new[] { TxtRecentTitle0, TxtRecentTitle1, TxtRecentTitle2, TxtRecentTitle3 };
                TextBlock[] sizes = new[] { TxtRecentSize0, TxtRecentSize1, TxtRecentSize2, TxtRecentSize3 };
                TextBlock[] subtitles = new[] { TxtRecentSubtitle0, TxtRecentSubtitle1, TxtRecentSubtitle2, TxtRecentSubtitle3 };

                for (int i = 0; i < 4; i++)
                {
                    if (buttons[i] == null) continue;

                    if (i < recentList.Count)
                    {
                        var item = recentList[i];
                        buttons[i].Visibility = Visibility.Visible;
                        if (titles[i] != null) titles[i].Text = !string.IsNullOrEmpty(item.Title) ? item.Title : $"{item.Width}×{item.Height} mm";
                        if (sizes[i] != null) sizes[i].Text = $"{item.Width}×{item.Height} mm";
                        if (subtitles[i] != null)
                        {
                            subtitles[i].Text = !string.IsNullOrEmpty(item.LastModified)
                                ? $"최근 작업: {item.LastModified}"
                                : "편집하던 라벨 작업을 이어서 진행합니다.";
                        }
                    }
                    else
                    {
                        buttons[i].Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadRecentInfo Error: {ex.Message}");
                if (CardRecentEmpty != null) CardRecentEmpty.Visibility = Visibility.Visible;
                if (BtnRecent0 != null) BtnRecent0.Visibility = Visibility.Collapsed;
                if (BtnRecent1 != null) BtnRecent1.Visibility = Visibility.Collapsed;
                if (BtnRecent2 != null) BtnRecent2.Visibility = Visibility.Collapsed;
                if (BtnRecent3 != null) BtnRecent3.Visibility = Visibility.Collapsed;
            }
        }

        #region Event Handlers

        private void OnPrinterBadgeClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenPrinterSetupWizard();
        }

        private void OnStoreLinkClick(object sender, RoutedEventArgs e)
        {
            OpenStoreUrl();
        }

        private void OnStoreLinkClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenStoreUrl();
        }

        private void OpenStoreUrl()
        {
            try
            {
                // 한국Xprinter 공식 스마트스토어 및 정품 라벨몰 링크
                string url = "https://smartstore.naver.com/xprinter/category/61ccd82fe17043f59401bbfb6802fcc9?cp=1";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스토어 링크를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void OnRefreshPrinterClick(object sender, RoutedEventArgs e)
        {
            await CheckCurrentPrinterStatusAsync();
            MessageBox.Show("프린터 연결 상태를 새로고침했습니다.", "프린터 상태", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnQuickFeedClick(object sender, RoutedEventArgs e)
        {
            if (!_isPrinterOnline || string.IsNullOrEmpty(_detectedPrinterName))
            {
                MessageBox.Show("현재 온라인(연결됨) 상태인 라벨 프린터가 없습니다.\n프린터 전원 및 케이블 연결을 확인하세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // TSPL 피드 1장 급지
                bool ok = PrinterHelper.SendStringToPrinter(_detectedPrinterName, "FORMFEED\r\n");
                if (!ok)
                {
                    MessageBox.Show("피드 명령 전송에 실패했습니다. 프린터 오프라인 상태를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"피드 명령 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnQuickHomeClick(object sender, RoutedEventArgs e)
        {
            if (!_isPrinterOnline || string.IsNullOrEmpty(_detectedPrinterName))
            {
                MessageBox.Show("현재 온라인 상태인 라벨 프린터가 없습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // TSPL 라벨 첫 시작점 정렬 (HOME)
                bool ok = PrinterHelper.SendStringToPrinter(_detectedPrinterName, "HOME\r\n");
                if (!ok)
                {
                    MessageBox.Show("홈 정렬 명령 전송에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"홈 정렬 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnQuickCalibrateClick(object sender, RoutedEventArgs e)
        {
            if (!_isPrinterOnline || string.IsNullOrEmpty(_detectedPrinterName))
            {
                MessageBox.Show("현재 온라인 상태인 라벨 프린터가 없습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 안드로이드 TsplImageBuilder 표준 센서 보정: GAP 2mm 설정 -> CLS -> GAPDETECT 단일 수행 (중복 AUTODETECT 및 HOME 제거)
                string cmd = "GAP 2 mm,0 mm\r\nCLS\r\nGAPDETECT\r\n";
                bool ok = PrinterHelper.SendStringToPrinter(_detectedPrinterName, cmd);
                if (ok)
                {
                    MessageBox.Show("센서 자동 측정 및 보정 명령이 전송되었습니다.\n프린터가 용지 1~2장을 급지하며 측정을 완료합니다.", "센서 측정", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("명령 전송에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"센서 보정 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnQuickSelfTestClick(object sender, RoutedEventArgs e)
        {
            if (!_isPrinterOnline || string.IsNullOrEmpty(_detectedPrinterName))
            {
                MessageBox.Show("현재 온라인(연결됨) 상태인 라벨 프린터가 없습니다.\n프린터 전원 및 케이블 연결을 확인하세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"[{_detectedPrinterName}] 프린터로 자체 하드웨어 진단(SELFTEST) 라벨을 인쇄하시겠습니까?\n프린터의 IP, 펌웨어 버전, 센서 전압 등이 인쇄됩니다.", "셀프 테스트 인쇄", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            try
            {
                // 안드로이드 TsplImageBuilder.buildSelfTestCommand() 일치: SELFTEST\r\n
                bool ok = PrinterHelper.SendStringToPrinter(_detectedPrinterName, "SELFTEST\r\n");
                if (ok)
                {
                    MessageBox.Show("셀프 테스트 인쇄 명령이 전송되었습니다.", "셀프 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("셀프 테스트 명령 전송에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"셀프 테스트 명령 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnQuickPrinterInfoClick(object sender, RoutedEventArgs e)
        {
            if (!_isPrinterOnline || string.IsNullOrEmpty(_detectedPrinterName))
            {
                MessageBox.Show("현재 온라인 상태인 라벨 프린터가 없습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var prevCursor = System.Windows.Input.Mouse.OverrideCursor;
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            try
            {
                var svc = new BarcodePrint.Services.PrinterService();
                // 전체 프린터 목록 전수조사(GetVerifiedPrinters)를 건너뛰고 이미 감지된 포트 정보 직접 주입으로 즉시 조회 (초고속)
                var cachedPrinters = new System.Collections.Generic.List<BarcodePrint.Model.UsbPrinterInfo>
                {
                    new BarcodePrint.Model.UsbPrinterInfo
                    {
                        Name = _detectedPrinterName,
                        PortName = _detectedPortName,
                        Type = _detectedConnType
                    }
                };
                var details = await System.Threading.Tasks.Task.Run(() => svc.GetPrinterHardwareDetails(_detectedPrinterName, cachedPrinters));

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[🖨️ {_detectedPrinterName} 하드웨어 상세 정보]");
                sb.AppendLine($"• 연결 포트: {_detectedPortName} ({_detectedConnType})");
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

        private void NavigateToEditor(WpfMainWindow mainWindow)
        {
            mainWindow.DashboardWindow = this;
            WindowLocationHelper.TransitionToWindow(this, mainWindow, hideInsteadOfClose: true);
        }

        public void RefreshDashboard()
        {
            LoadRecentInfo();
            CheckCurrentPrinterStatus();
        }

        private void OnHeroStudioClick(object sender, MouseButtonEventArgs e)
        {
            OnNewTemplateClick(sender, e);
        }

        private void OnNewTemplateClick(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings();
            settings.Load();
            var wizard = new LabelSetupWizardDialog(settings.PaperWidth, settings.PaperHeight);
            wizard.Owner = this;
            if (wizard.ShowDialog() == true)
            {
                int w = (int)wizard.SelectedWidth;
                int h = (int)wizard.SelectedHeight;
                settings.AddRecentWork($"라벨 {w}×{h} mm", w, h, wizard.SelectedPaperType, wizard.SelectedRotation);

                var mainWindow = new WpfMainWindow(wizard.SelectedWidth, wizard.SelectedHeight, wizard.SelectedPaperType, wizard.SelectedRotation);
                NavigateToEditor(mainWindow);
            }
            else
            {
                // 마법사에서 프리셋을 변경한 후 취소했더라도 메인 프리셋 UI 동기화
                LoadPresets();
            }
        }

        private void OnRecentCardClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int index))
            {
                var settings = new AppSettings();
                settings.Load();
                if (settings.RecentWorks != null && index >= 0 && index < settings.RecentWorks.Count)
                {
                    OpenRecentWorkItem(settings.RecentWorks[index]);
                    return;
                }
            }

            // 폴백: 기본 이전 작업 열기
            OnRecentTemplateClick(sender, e);
        }

        private void OpenRecentWorkItem(RecentWorkItem item)
        {
            if (item == null) return;

            var settings = new AppSettings();
            settings.Load();
            settings.AddRecentWork(item.Title, item.Width, item.Height, item.PaperType, item.Rotation, item.TemplateFilePath);

            if (!string.IsNullOrEmpty(item.TemplateFilePath) && System.IO.File.Exists(item.TemplateFilePath))
            {
                var mainWindow = new WpfMainWindow();
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    vm.LoadTemplateFromFile(item.TemplateFilePath);
                }
                NavigateToEditor(mainWindow);
            }
            else
            {
                var mainWindow = new WpfMainWindow(item.Width, item.Height, item.PaperType, item.Rotation);
                NavigateToEditor(mainWindow);
            }
        }

        private void OnRecentTemplateClick(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings();
            settings.Load();
            if (settings.RecentWorks != null && settings.RecentWorks.Count > 0)
            {
                OpenRecentWorkItem(settings.RecentWorks[0]);
                return;
            }
            var mainWindow = new WpfMainWindow();
            NavigateToEditor(mainWindow);
        }

        private void OnLoadTemplateClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "저장된 라벨 서식 템플릿 열기",
                Filter = "LabelMaster 템플릿 (*.lbm)|*.lbm|JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                var mainWindow = new WpfMainWindow();
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    vm.LoadTemplateFromFile(dlg.FileName);
                    var settings = new AppSettings();
                    settings.Load();
                    string templateTitle = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    string paperType = vm.CurrentTemplate?.PaperType ?? "GAP";
                    int rot = vm.CurrentTemplate?.Rotation ?? vm.LabelRotationAngle;
                    settings.AddRecentWork(templateTitle, (int)vm.PaperWidth, (int)vm.PaperHeight, paperType, rot, dlg.FileName);
                }
                NavigateToEditor(mainWindow);
            }
        }

        private void OnPdfPrintClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "인쇄할 PDF 송장 또는 문서 선택",
                Filter = "PDF 문서 (*.pdf)|*.pdf|이미지 파일 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (ext == ".pdf")
                {
                    var pdfDialog = new PdfPrintDialog(dlg.FileName, 100, 100, (tempPng, w, h) =>
                    {
                        var mainWindow = new WpfMainWindow();
                        if (mainWindow.DataContext is MainViewModel vm)
                        {
                            vm.ImportImageFile(tempPng);
                        }
                        NavigateToEditor(mainWindow);
                    });
                    pdfDialog.Owner = this;
                    pdfDialog.ShowDialog();
                }
                else
                {
                    var mainWindow = new WpfMainWindow();
                    if (mainWindow.DataContext is MainViewModel vm)
                    {
                        vm.ImportImageFile(dlg.FileName);
                    }
                    NavigateToEditor(mainWindow);
                }
            }
        }

        private void OnPrinterSetupWizardClick(object sender, RoutedEventArgs e)
        {
            OpenPrinterSetupWizard();
        }

        private void OpenPrinterSetupWizard()
        {
            var wizard = new PrinterSetupWizardDialog();
            wizard.Owner = this;
            wizard.ShowDialog();
            CheckCurrentPrinterStatus();
            LoadRecentInfo();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void OnOpenPrinterSettingsClick(object sender, RoutedEventArgs e)
        {
            OpenPrinterSettings();
        }

        private void OnOpenPrinterSettingsClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenPrinterSettings();
        }

        private void OpenPrinterSettings()
        {
            try
            {
                var vm = new MainViewModel();

                // 1. 대시보드(사이드바)에서 감지된 연결 방식과 설정창 탭 강제 일치
                if (!string.IsNullOrEmpty(_detectedConnType))
                {
                    if (_detectedConnType.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase) || _detectedConnType.Equals("BT", StringComparison.OrdinalIgnoreCase))
                        vm.SelectedConnectionType = "BT";
                    else if (_detectedConnType.Equals("LAN", StringComparison.OrdinalIgnoreCase) || _detectedConnType.Equals("Network", StringComparison.OrdinalIgnoreCase))
                        vm.SelectedConnectionType = "LAN";
                    else if (_detectedConnType.Equals("USB", StringComparison.OrdinalIgnoreCase))
                        vm.SelectedConnectionType = "USB";
                    else
                        vm.SelectedConnectionType = "ALL";
                }

                // 2. 대시보드에서 감지된 프린터 선택 동기화 (접두어 무관 정규화 매칭 및 빈 상자 방지)
                if (!string.IsNullOrEmpty(_detectedPrinterName))
                {
                    string cleanDet = _detectedPrinterName.Replace("[블루투스]", "").Replace("[네트워크]", "").Trim();
                    var matched = vm.AvailablePrinters.FirstOrDefault(p =>
                        p.Replace("[블루투스]", "").Replace("[네트워크]", "").Trim().Equals(cleanDet, StringComparison.OrdinalIgnoreCase));

                    string finalName = matched ?? _detectedPrinterName;
                    if (!vm.AvailablePrinters.Contains(finalName))
                    {
                        vm.AvailablePrinters.Add(finalName);
                    }
                    vm.SelectedPrinterName = finalName;
                    vm.TargetPrinterName = finalName;
                }

                var win = new PrinterSettingsWindow
                {
                    DataContext = vm,
                    Owner = this
                };
                win.ShowDialog();
                CheckCurrentPrinterStatus();
                LoadRecentInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프린터 설정 열기 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Preset Label Handlers (동적 편집 및 기본값 복원 지원)

        private void LoadPresets()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                _presets = settings.FavoritePresets ?? AppSettings.GetDefaultPresets();
                if (_presets.Count < 4)
                {
                    _presets = AppSettings.GetDefaultPresets();
                }
                UpdatePresetCardUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartWindow] LoadPresets Error: {ex.Message}");
                _presets = AppSettings.GetDefaultPresets();
                UpdatePresetCardUI();
            }
        }

        private void UpdatePresetCardUI()
        {
            // 규격 라벨 목록이 다이얼로그(PresetSelectDialog)로 분리되어 메인 화면 직접 바인딩 불필요
        }

        #region Quick Tools HTML WebBrowser Integration

        private void SetBrowserEmulationMode()
        {
            try
            {
                string appName = AppDomain.CurrentDomain.FriendlyName;
                if (!appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    appName += ".exe";
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 11001, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        // 현재 활성화된 배너 URL/경로 (내부 로드 허용 판정용)
        private string _activeBannerBaseUrl = "";

        private void LoadQuickToolsHtml(bool forceReload = false)
        {
            try
            {
                if (QuickToolsWebBrowser == null) return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string bannerUrlTxt = Path.Combine(baseDir, "Info", "banner_url.txt");
                string targetUrl = "";

                // 1. Info/banner_url.txt 파일이 있고 유효한 URL(http:// 또는 https://)이 설정되어 있는지 확인
                if (File.Exists(bannerUrlTxt))
                {
                    try
                    {
                        string line = File.ReadAllText(bannerUrlTxt).Trim();
                        if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            targetUrl = line;
                        }
                    }
                    catch { }
                }

                // 2. AppSettings.Default.QuickToolsBannerUrl 에 설정된 외부 링크 확인
                if (string.IsNullOrWhiteSpace(targetUrl))
                {
                    string cfgUrl = AppSettings.Default.QuickToolsBannerUrl?.Trim();
                    if (!string.IsNullOrEmpty(cfgUrl) &&
                        (cfgUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         cfgUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                    {
                        targetUrl = cfgUrl;
                    }
                }

                // 3. 외부 URL이 설정되어 있는 경우 해당 웹 주소 로드
                if (!string.IsNullOrWhiteSpace(targetUrl))
                {
                    _activeBannerBaseUrl = targetUrl;
                    string fullUrl = targetUrl;
                    if (forceReload)
                    {
                        fullUrl += (fullUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;
                    }
                    QuickToolsWebBrowser.Navigate(new Uri(fullUrl));
                    return;
                }

                // 4. 외부 링크가 없으면 로컬 Info/quick_tools.html 파일 로드 (오프라인 지원)
                string htmlPath = Path.Combine(baseDir, "Info", "quick_tools.html");

                // 없으면 상위 또는 개발 기본 위치 확인
                if (!File.Exists(htmlPath))
                {
                    string rootHtml = Path.Combine(baseDir, "quick_tools.html");
                    if (File.Exists(rootHtml))
                    {
                        htmlPath = rootHtml;
                    }
                    else
                    {
                        // 개발 환경 fallback
                        string devPath = @"E:\4.sourcecode\C#\2.X-quick\0.엑셀인쇄\LabelMaster_Basic_v3\Info\quick_tools.html";
                        if (File.Exists(devPath))
                        {
                            htmlPath = devPath;
                        }
                    }
                }

                // 파일이 아예 없다면 기본 템플릿 파일 생성
                if (!File.Exists(htmlPath))
                {
                    string infoDir = Path.Combine(baseDir, "Info");
                    if (!Directory.Exists(infoDir))
                    {
                        Directory.CreateDirectory(infoDir);
                    }
                    htmlPath = Path.Combine(infoDir, "quick_tools.html");
                    CreateDefaultQuickToolsHtml(htmlPath);
                }

                _activeBannerBaseUrl = htmlPath;

                // WebBrowser 로드 (고정 파일 URL)
                string uriStr = new Uri(htmlPath).AbsoluteUri;
                if (forceReload)
                {
                    uriStr += "?t=" + DateTime.Now.Ticks;
                }
                QuickToolsWebBrowser.Navigate(new Uri(uriStr));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartWindow] LoadQuickToolsHtml Error: {ex.Message}");
            }
        }

        public void OnRefreshQuickToolsHtmlClick(object sender, RoutedEventArgs e)
        {
            if (e != null) e.Handled = true;
            LoadQuickToolsHtml(forceReload: true);
        }

        private void OnQuickToolsWebBrowserNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            try
            {
                if (e.Uri == null) return;

                string uriStr = e.Uri.AbsoluteUri;

                // 1. about:blank 허용
                if (uriStr.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // 2. 로컬 파일인 경우 quick_tools.html 허용
                if (uriStr.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && 
                    uriStr.IndexOf("quick_tools.html", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }

                // 3. 외부 URL인 경우: 현재 배너 타겟 주소와 동일한 루트(초기 로딩 및 리로드)는 허용
                if (!string.IsNullOrWhiteSpace(_activeBannerBaseUrl) &&
                    (_activeBannerBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     _activeBannerBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    string cleanTarget = _activeBannerBaseUrl.Split('?')[0].TrimEnd('/');
                    string cleanUri = uriStr.Split('?')[0].TrimEnd('/');
                    if (string.Equals(cleanTarget, cleanUri, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                // 4. 사용자가 배너 내부의 링크(<a> 태그 등)를 클릭한 경우 시스템 기본 브라우저(Edge, Chrome 등)로 새 창 실행
                e.Cancel = true;
                Process.Start(new ProcessStartInfo(uriStr) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartWindow] QuickTools WebBrowser Link Error: {ex.Message}");
            }
        }

        private void OnQuickToolsWebBrowserLoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                // 스크롤바 비활성화
                dynamic doc = QuickToolsWebBrowser.Document;
                if (doc != null && doc.body != null && doc.body.style != null)
                {
                    doc.body.style.overflow = "hidden";
                    doc.body.scroll = "no";
                }
            }
            catch { }
        }

        private void CreateDefaultQuickToolsHtml(string filePath)
        {
            try
            {
                string html = @"<!DOCTYPE html>
<html lang=""ko"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>라벨마스터 빠른 안내 및 도구</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; user-select: none; }
        body, html { width: 100%; height: 100%; overflow: hidden; font-family: 'Pretendard', 'Malgun Gothic', '맑은 고딕', sans-serif; background: #FFFFFF; }
        .banner-container { width: 100%; height: 138px; display: flex; align-items: center; justify-content: space-between; padding: 0 24px; background: linear-gradient(135deg, #F8FAFC 0%, #EFF6FF 100%); border: 1px solid #E2E8F0; border-radius: 12px; position: relative; overflow: hidden; }
        .left-content { display: flex; align-items: center; z-index: 1; }
        .icon-box { width: 58px; height: 58px; border-radius: 14px; background: #FFFFFF; border: 1.5px solid #BFDBFE; box-shadow: 0 4px 12px rgba(37, 99, 235, 0.08); display: flex; align-items: center; justify-content: center; font-size: 26px; margin-right: 18px; flex-shrink: 0; }
        .text-group { display: flex; flex-direction: column; }
        .title-row { display: flex; align-items: center; margin-bottom: 5px; }
        .title { font-size: 17px; font-weight: 700; color: #0F172A; }
        .badge { background: #DBEAFE; color: #1D4ED8; font-size: 11px; font-weight: 700; padding: 2px 7px; border-radius: 4px; margin-left: 10px; }
        .desc { font-size: 12px; color: #64748B; line-height: 1.4; }
        .right-buttons { display: flex; align-items: center; gap: 10px; z-index: 1; }
        .btn-link { display: inline-flex; align-items: center; justify-content: center; padding: 10px 16px; border-radius: 8px; font-size: 12.5px; font-weight: 600; text-decoration: none; cursor: pointer; white-space: nowrap; }
        .btn-primary { background: #2563EB; color: #FFFFFF !important; border: 1px solid #1D4ED8; box-shadow: 0 2px 6px rgba(37, 99, 235, 0.2); }
        .btn-secondary { background: #FFFFFF; color: #1E293B !important; border: 1px solid #CBD5E1; }
        .btn-icon { margin-right: 6px; font-size: 14px; }
    </style>
</head>
<body>
    <div class=""banner-container"">
        <div class=""left-content"">
            <div class=""icon-box"">🚀</div>
            <div class=""text-group"">
                <div class=""title-row"">
                    <span class=""title"">라벨마스터 온라인 서비스 및 빠른 지원</span>
                    <span class=""badge"">실시간 연동</span>
                </div>
                <div class=""desc"">
                    정품 감열 라벨 주문, 사용 매뉴얼 안내 및 고객 지원 서비스를 이용하세요.<br>
                    본 배너의 내용과 링크는 <strong>Info/quick_tools.html</strong> 파일에서 언제든 자유롭게 수정 가능합니다.
                </div>
            </div>
        </div>
        <div class=""right-buttons"">
            <a href=""https://xpix.co.kr/labelmaster/windows/download"" class=""btn-link btn-secondary"" title=""최신 업데이트 다운로드"">
                <span class=""btn-icon"">🔄</span>최신 버전 확인
            </a>
            <a href=""https://smartstore.naver.com"" class=""btn-link btn-primary"" title=""공식 스토어 바로가기"">
                <span class=""btn-icon"">🛒</span>정품 소모품 몰
            </a>
        </div>
    </div>
</body>
</html>";
                File.WriteAllText(filePath, html, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        #endregion

        public void OnOpenPresetSelectDialogClick(object sender, RoutedEventArgs e)
        {
            if (e != null) e.Handled = true;

            var dlg = new PresetSelectDialog
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && dlg.SelectedPreset != null)
            {
                OpenPresetLabel(dlg.SelectedPreset.WidthMm, dlg.SelectedPreset.HeightMm);
            }
        }

        private void OnPreset1Click(object sender, RoutedEventArgs e)
        {
            OpenPresetByIndex(0);
        }

        private void OnPreset2Click(object sender, RoutedEventArgs e)
        {
            OpenPresetByIndex(1);
        }

        private void OnPreset3Click(object sender, RoutedEventArgs e)
        {
            OpenPresetByIndex(2);
        }

        private void OnPreset4Click(object sender, RoutedEventArgs e)
        {
            OpenPresetByIndex(3);
        }

        private void OpenPresetByIndex(int index)
        {
            if (_presets != null && index >= 0 && index < _presets.Count)
            {
                OpenPresetLabel(_presets[index].WidthMm, _presets[index].HeightMm);
            }
            else
            {
                var defaults = AppSettings.GetDefaultPresets();
                OpenPresetLabel(defaults[index].WidthMm, defaults[index].HeightMm);
            }
        }

        private void OnEditPreset1Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            EditPresetByIndex(0);
        }

        private void OnEditPreset2Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            EditPresetByIndex(1);
        }

        private void OnEditPreset3Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            EditPresetByIndex(2);
        }

        private void OnEditPreset4Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            EditPresetByIndex(3);
        }

        private void OnManagePresetsClick(object sender, RoutedEventArgs e)
        {
            if (e != null) e.Handled = true;

            var dlg = new PresetManagerDialog(_presets)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && dlg.ResultPresets != null)
            {
                _presets = dlg.ResultPresets;
                SaveAndApplyPresets();
            }
        }

        private void MovePreset(int fromIndex, int toIndex)
        {
            if (_presets == null || fromIndex < 0 || fromIndex >= _presets.Count || toIndex < 0 || toIndex >= _presets.Count || fromIndex == toIndex)
                return;

            var item = _presets[fromIndex];
            _presets.RemoveAt(fromIndex);
            _presets.Insert(toIndex, item);

            SaveAndApplyPresets();
        }

        private void OnMovePreset1RightClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(0, 1);
        }

        private void OnMovePreset2LeftClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(1, 0);
        }

        private void OnMovePreset2RightClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(1, 2);
        }

        private void OnMovePreset3LeftClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(2, 1);
        }

        private void OnMovePreset3RightClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(2, 3);
        }

        private void OnMovePreset4LeftClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MovePreset(3, 2);
        }

        private void SaveAndApplyPresets()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                settings.FavoritePresets = _presets;
                settings.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StartWindow] Save Preset Error: {ex.Message}");
            }

            UpdatePresetCardUI();
        }

        private void EditPresetByIndex(int index)
        {
            if (_presets == null || index < 0 || index >= _presets.Count) return;

            var dlg = new PresetEditDialog(_presets[index])
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                _presets[index] = dlg.PresetItem;
                SaveAndApplyPresets();
            }
        }

        private void OnResetPresetsClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var res = MessageBox.Show(
                "자주 사용하는 라벨 규격 프리셋 4개를 초기 기본 설정으로 복원하시겠습니까?",
                "기본 규격 복원",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                _presets = AppSettings.GetDefaultPresets();
                SaveAndApplyPresets();
                MessageBox.Show("기본 라벨 규격으로 복원되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenPresetLabel(double widthMm, double heightMm)
        {
            var mainWindow = new WpfMainWindow(widthMm, heightMm, "GAP", 0);
            NavigateToEditor(mainWindow);
        }

        #endregion

        #endregion
    }
}
