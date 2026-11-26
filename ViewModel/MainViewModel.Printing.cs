using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BarcodePrint.Model;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel
    {
        private List<UsbPrinterInfo> _cachedPrinters = new List<UsbPrinterInfo>();
        private bool _isCheckingPrinter = false;
        private string _selectedConnectionType = "ALL";
        private bool _isCurrentPrinterAuthorized = false;
        private PrintRecordModel _currentPrintingRecord = null;

        /// <summary>
        /// 현재 선택된 프린터가 정품 라이선스에 등록되어 인증되었는지 여부
        /// </summary>
        public bool IsCurrentPrinterAuthorized
        {
            get => _isCurrentPrinterAuthorized;
            set
            {
                if (_isCurrentPrinterAuthorized != value)
                {
                    _isCurrentPrinterAuthorized = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCurrentPrinterNotAuthorized));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsCurrentPrinterNotAuthorized => !IsCurrentPrinterAuthorized;

        public string SelectedConnectionType
        {
            get => _selectedConnectionType;
            set
            {
                if (_selectedConnectionType != value)
                {
                    _selectedConnectionType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFilterAll));
                    OnPropertyChanged(nameof(IsFilterUsb));
                    OnPropertyChanged(nameof(IsFilterLan));
                    OnPropertyChanged(nameof(IsFilterBt));

                    if (_settings != null)
                    {
                        _settings.LastSelectedConnectionType = value;
                        _settings.Save();
                    }

                    ApplyPrinterFilter();
                }
            }
        }

        public bool IsFilterAll => SelectedConnectionType == "ALL";
        public bool IsFilterUsb => SelectedConnectionType == "USB";
        public bool IsFilterLan => SelectedConnectionType == "LAN";
        public bool IsFilterBt => SelectedConnectionType == "BT" || SelectedConnectionType == "Bluetooth";

        /// <summary>
        /// 하드웨어 시리얼 번호가 정품 라이선스에 등록되어 있는지 검증
        /// </summary>
        public bool ValidatePrinterAuthorization(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial) || serial.Trim() == "-")
                return false;

            if (_authenticatedSerials == null || _authenticatedSerials.Count == 0)
                return false;

            string cleanSerial = Regex.Replace(serial, @"[\s\-_\.:]", "").ToUpperInvariant();
            if (string.IsNullOrEmpty(cleanSerial))
                return false;

            foreach (var auth in _authenticatedSerials)
            {
                if (string.IsNullOrWhiteSpace(auth)) continue;
                string cleanAuth = Regex.Replace(auth, @"[\s\-_\.:]", "").ToUpperInvariant();
                if (!string.IsNullOrEmpty(cleanAuth) && (cleanSerial.Contains(cleanAuth) || cleanAuth.Contains(cleanSerial)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool _isScanningPrinters = false;

        public void RefreshPrinters(bool runAsync = true)
        {
            if (runAsync)
            {
                if (_isScanningPrinters) return;
                _isScanningPrinters = true;

                // 1) 초기 진입 시 캐시 컬렉션 준비 (이전 캐시 또는 마지막 선택 프린터를 즉시 선반영해 오프라인/빈 목록 깜빡임 방지)
                if (_cachedPrinters == null)
                {
                    _cachedPrinters = new List<UsbPrinterInfo>();
                }
                if (_cachedPrinters.Count == 0 && _sharedLastPrinters.Count > 0)
                {
                    _cachedPrinters = new List<UsbPrinterInfo>(_sharedLastPrinters);
                    ApplyPrinterFilter();
                }
                else if (AvailablePrinters.Count == 0 && !string.IsNullOrEmpty(_settings?.LastSelectedPrinterName))
                {
                    AvailablePrinters.Add(_settings.LastSelectedPrinterName);
                    _selectedPrinterName = _settings.LastSelectedPrinterName;
                    _targetPrinterName = _settings.LastSelectedPrinterName;
                    OnPropertyChanged(nameof(SelectedPrinterName));
                    OnPropertyChanged(nameof(TargetPrinterName));
                }

                // 2) 백그라운드 스레드에서 WMI/레지스트리 전수조사 및 이름/포트 캐시 워밍업 비동기 실행 (UI 스레드 완전 무차단)
                Task.Run(() =>
                {
                    if (PrinterOnlineDetector.IsAppExiting || UsbPrinterHelper.IsAppExiting) return;
                    try
                    {
                        var printers = _printerService.GetVerifiedPrinters(_authenticatedSerials);
                        if (PrinterOnlineDetector.IsAppExiting || UsbPrinterHelper.IsAppExiting) return;
                        var freshList = printers ?? new List<UsbPrinterInfo>();

                        // 페어링된 블루투스 라벨 프린터 중 가상 COM 포트가 활성화된 장치도 목록에 병합 (대시보드 PrinterOnlineDetector와 1:1 일치)
                        try
                        {
                            var pairedBtNames = UsbPrinterHelper.GetPairedBluetoothPrinterNames();
                            var availComs = new HashSet<string>(UsbPrinterHelper.GetAvailableComPorts(), StringComparer.OrdinalIgnoreCase);
                            foreach (var bthName in pairedBtNames)
                            {
                                string btPort = UsbPrinterHelper.GetBluetoothPortForDeviceName(bthName);
                                if (string.IsNullOrEmpty(btPort) || !availComs.Contains(btPort)) continue;

                                string dispName = $"[블루투스] {bthName}";
                                if (!freshList.Any(x => x.PortName.Equals(btPort, StringComparison.OrdinalIgnoreCase) ||
                                                        x.Name.Equals(dispName, StringComparison.OrdinalIgnoreCase) ||
                                                        x.Name.Equals(bthName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    freshList.Add(new UsbPrinterInfo
                                    {
                                        Name = dispName,
                                        FriendlyName = bthName,
                                        PortName = btPort,
                                        Serial = "-",
                                        Type = "Bluetooth",
                                        Address = btPort,
                                        IsPaired = true
                                    });
                                }
                            }
                        }
                        catch { }

                        // 백그라운드 스레드에서 블루투스/네트워크 이름 및 포트 조회를 미리 수행하여 UI 스레드 블로킹 원천 제거
                        foreach (var p in freshList)
                        {
                            if (p.Type.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase))
                            {
                                UsbPrinterHelper.GetBluetoothDeviceNameForPort(p.PortName);
                                UsbPrinterHelper.GetBluetoothPortForDeviceName(p.FriendlyName ?? p.Name);
                            }
                            else if (p.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase))
                            {
                                UsbPrinterHelper.GetNetworkPrinterDeviceName(p.Address);
                            }
                        }
                        UsbPrinterHelper.GetAvailableComPorts();

                        var dispatcher = System.Windows.Application.Current?.Dispatcher;
                        if (dispatcher != null)
                        {
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    string prevSelected = SelectedPrinterName;
                                    _cachedPrinters = freshList;
                                    _sharedLastPrinters = new List<UsbPrinterInfo>(freshList);
                                    ApplyPrinterFilter();

                                    if (!string.IsNullOrEmpty(prevSelected) && AvailablePrinters.Contains(prevSelected))
                                    {
                                        SelectedPrinterName = prevSelected;
                                    }
                                    else if (!string.IsNullOrEmpty(_settings?.LastSelectedPrinterName) && AvailablePrinters.Contains(_settings.LastSelectedPrinterName))
                                    {
                                        SelectedPrinterName = _settings.LastSelectedPrinterName;
                                    }
                                    else if (AvailablePrinters.Count > 0)
                                    {
                                        SelectedPrinterName = AvailablePrinters[0];
                                    }
                                    else
                                    {
                                        SelectedPrinterName = null;
                                    }
                                }
                                finally
                                {
                                    _isScanningPrinters = false;
                                }
                            }));
                        }
                        else
                        {
                            _isScanningPrinters = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _isScanningPrinters = false;
                        PrinterSerialLogger.LogError("비동기 프린터 목록 갱신 실패", ex);
                    }
                });
            }
            else
            {
                try
                {
                    var printers = _printerService.GetVerifiedPrinters(_authenticatedSerials);
                    _cachedPrinters = printers ?? new List<UsbPrinterInfo>();
                    ApplyPrinterFilter();
                }
                catch (Exception ex)
                {
                    PrinterSerialLogger.LogError("프린터 목록 갱신 실패", ex);
                }
            }
        }

        public void ApplyPrinterFilter()
        {
            _isFilteringPrinters = true;
            try
            {
                AvailablePrinters.Clear();

                // 드라이버가 정식 설치된 프린터만 필터링하여 노출 (가짜 COM3 자동 생성 배제)
                // LAN 필터 시 캐시에 LAN 장치가 전혀 없을 때만 기본 IP를 기반으로 보조 항목 생성
                if (SelectedConnectionType == "LAN")
                {
                    if (!_cachedPrinters.Any(p => p.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!string.IsNullOrEmpty(TcpPrinterIp))
                        {
                            string netDevName = UsbPrinterHelper.GetNetworkPrinterDeviceName(TcpPrinterIp);
                            string displayName = !string.IsNullOrEmpty(netDevName) && !netDevName.Equals("네트워크 라벨 프린터", StringComparison.OrdinalIgnoreCase)
                                ? $"[네트워크] {netDevName}"
                                : $"[네트워크] {TcpPrinterIp}";

                            _cachedPrinters.Add(new UsbPrinterInfo
                            {
                                Name = displayName,
                                FriendlyName = netDevName ?? "Network Printer",
                                PortName = $"{TcpPrinterIp}:{TcpPrinterPort}",
                                Serial = "-",
                                Type = "LAN",
                                Address = TcpPrinterIp
                            });
                        }
                    }
                }

                var filtered = _cachedPrinters;
                if (SelectedConnectionType == "USB")
                {
                    filtered = _cachedPrinters.Where(p => p.Type.Equals("USB", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else if (SelectedConnectionType == "LAN")
                {
                    filtered = _cachedPrinters.Where(p => p.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else if (SelectedConnectionType == "BT" || SelectedConnectionType == "Bluetooth")
                {
                    filtered = _cachedPrinters.Where(p => p.Type.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase)).ToList();
                }

                Func<string, string> getCleanName = s => (s ?? "")
                    .Replace("[블루투스]", "")
                    .Replace("[네트워크]", "")
                    .Replace("(페어링 필요)", "")
                    .Replace("(가상포트 연결 필요)", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .Trim()
                    .ToUpperInvariant();

                foreach (var p in filtered)
                {
                    // 1. 과거 설정이나 포트명으로 저장된 [블루투스] COMx 형태를 실제 장치명으로 자동 보정
                    if (p.Type.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Name.StartsWith("[블루투스] COM", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.Contains("(COM") ||
                            p.Name.Equals("[블루투스] 블루투스 장치", StringComparison.OrdinalIgnoreCase))
                        {
                            string realDev = UsbPrinterHelper.GetBluetoothDeviceNameForPort(p.PortName);
                            p.Name = $"[블루투스] {realDev}";
                            p.FriendlyName = realDev;
                        }
                    }
                    // 2. 네트워크 프린터가 IP 주소로만 표기된 경우 실제 모델명/드라이버 장치명으로 자동 보정 (블루투스와 동일 규격)
                    else if (p.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Name.Equals($"[네트워크] {p.Address}", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.StartsWith("[네트워크] 192.", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.StartsWith("[네트워크] 10.", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.StartsWith("[네트워크] 172.", StringComparison.OrdinalIgnoreCase))
                        {
                            string realNetDev = UsbPrinterHelper.GetNetworkPrinterDeviceName(p.Address);
                            if (!string.IsNullOrEmpty(realNetDev) && !realNetDev.Equals("네트워크 라벨 프린터", StringComparison.OrdinalIgnoreCase))
                            {
                                p.Name = $"[네트워크] {realNetDev}";
                                p.FriendlyName = realNetDev;
                            }
                        }
                    }

                    if (!AvailablePrinters.Any(existing => existing.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        AvailablePrinters.Add(p.Name);
                    }
                }

                if (AvailablePrinters.Count > 0)
                {
                    // 1) 사용자 설정의 마지막 선택 프린터가 현재 필터 목록에 존재하는지 확인
                    string preferredPrinter = _settings?.LastSelectedPrinterName;
                    string target = null;

                    if (!string.IsNullOrEmpty(preferredPrinter) && AvailablePrinters.Contains(preferredPrinter))
                    {
                        target = preferredPrinter;
                    }
                    else if (!string.IsNullOrEmpty(SelectedPrinterName) && AvailablePrinters.Contains(SelectedPrinterName))
                    {
                        target = SelectedPrinterName;
                    }
                    else if (!string.IsNullOrEmpty(TargetPrinterName) && AvailablePrinters.Contains(TargetPrinterName))
                    {
                        target = TargetPrinterName;
                    }
                    else
                    {
                        target = AvailablePrinters[0];
                    }

                    // 콤보박스(TargetPrinterName)와 내부 상태(SelectedPrinterName)를 확실하게 동기화
                    _selectedPrinterName = target;
                    _targetPrinterName = target;
                    OnPropertyChanged(nameof(SelectedPrinterName));
                    OnPropertyChanged(nameof(TargetPrinterName));
                }
                else
                {
                    _selectedPrinterName = "";
                    _targetPrinterName = "";
                    OnPropertyChanged(nameof(SelectedPrinterName));
                    OnPropertyChanged(nameof(TargetPrinterName));

                    string typeDesc = SelectedConnectionType == "ALL" ? "" : $" ({SelectedConnectionType})";
                    PrinterStatusBadge = $"🔴 연결된 프린터 없음{typeDesc}";
                    PrinterStatusColor = "#D32F2F";
                    IsCurrentPrinterAuthorized = false;
                }

                CheckRealtimePrinterStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyPrinterFilter 오류: {ex.Message}");
            }
            finally
            {
                _isFilteringPrinters = false;
            }
        }

        public void CheckRealtimePrinterStatus()
        {
            if (_isCheckingPrinter) return;
            string printerName = SelectedPrinterName;
            if (string.IsNullOrEmpty(printerName))
            {
                IsCurrentPrinterAuthorized = false;
                return;
            }

            _isCheckingPrinter = true;

            Task.Run(() =>
            {
                if (PrinterOnlineDetector.IsAppExiting || UsbPrinterHelper.IsAppExiting) return;
                try
                {
                    // 캐시된 프린터 정보에서 시리얼 검증
                    var info = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
                    string cachedSerial = info?.Serial;
                    bool isAuthorized = ValidatePrinterAuthorization(cachedSerial);
                    var licenseMode = TrialLicenseManager.EvaluateLicense(isAuthorized, cachedSerial, printerName);

                    if (PrinterOnlineDetector.IsAppExiting || UsbPrinterHelper.IsAppExiting) return;
                    _printerService.CheckRealtimePrinterStatus(printerName, _cachedPrinters, out string badge, out string color);
                    if (PrinterOnlineDetector.IsAppExiting || UsbPrinterHelper.IsAppExiting) return;

                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        bool statusChanged = (PrinterStatusBadge != badge) || (PrinterStatusColor != color) || !IsCurrentPrinterAuthorized;

                        if (!IsCurrentPrinterAuthorized)
                        {
                            IsCurrentPrinterAuthorized = true;
                        }

                        // 프린터 상태 뱃지에는 항상 실제 프린터 하드웨어 상태(정상 준비, 오프라인, 용지 없음 등)를 표시
                        // (체험판 정보는 좌측 상단 전용 뱃지 1개로 표시)
                        PrinterStatusBadge = badge;
                        PrinterStatusColor = color;
                        _sharedLastStatusBadge = badge;
                        _sharedLastStatusColor = color;

                        if (statusChanged)
                        {
                            UpdateTrialLicenseState();
                            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                        }
                    }));
                }
                finally
                {
                    _isCheckingPrinter = false;
                }
            });
        }

        public void ExecutePrinterAction(string action)
        {
            try
            {
                if (AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
                {
                    _dialogService.ShowWarning("연결된 프린터가 없습니다.", "알림");
                    return;
                }

                string tsplCmd = "";
                string desc = "";

                // [열전사 리본 연동] IsRibbon 설정에 따라 리본 모터 동기화 및 센서 오작동 방지 명령 주입
                string ribbonCmd = IsRibbon
                    ? "SET RIBBON ON\r\nSET ENCODER OFF\r\nSET RIBBONEND OFF\r\n"
                    : "SET RIBBON OFF\r\n";

                switch (action)
                {
                    case "CALIBRATE":
                    case "GAPDETECT":
                        if (PaperType == "BLINE")
                        {
                            tsplCmd = $"BLINE 2 mm,0 mm\r\nCLS\r\n{ribbonCmd}BLINEDETECT\r\n";
                            desc = "블랙마크 센서 보정(Bline Detect)";
                        }
                        else if (PaperType == "CONTINUOUS")
                        {
                            tsplCmd = $"CLS\r\n{ribbonCmd}GAP 0,0\r\n";
                            desc = "연속지 모드 설정";
                        }
                        else
                        {
                            tsplCmd = $"GAP 2 mm,0 mm\r\nCLS\r\n{ribbonCmd}GAPDETECT\r\n";
                            desc = "갭 센서 자동 보정(Gap Detect)";
                        }
                        break;
                    case "AUTODETECT":
                        tsplCmd = $"CLS\r\n{ribbonCmd}AUTODETECT\r\n";
                        desc = "센서 자동 감지(Auto Detect)";
                        break;
                    case "HOME":
                        string gapCmd = PaperType == "BLINE"
                            ? $"BLINE {GapHeight} mm,{BlineOffset.ToString("0.0", CultureInfo.InvariantCulture)} mm\r\n"
                            : (PaperType == "CONTINUOUS" ? "GAP 0,0\r\n" : $"GAP {GapHeight} mm,0 mm\r\n");
                        tsplCmd = $"SIZE {PaperWidth} mm,{PaperHeight} mm\r\n{gapCmd}{ribbonCmd}HOME\r\nCLS\r\n";
                        desc = "라벨 원점 이동(Home)";
                        break;
                    case "FEED":
                        tsplCmd = $"CLS\r\n{ribbonCmd}FORMFEED\r\n";
                        desc = "용지 1장 배출(Feed)";
                        break;
                    case "SELFTEST":
                        tsplCmd = "CLS\r\nSELFTEST\r\n";
                        desc = "자가 진단(Self Test)";
                        break;
                    case "RESET":
                    case "REBOOT":
                        // [New_xPix 동일] 활성화(~!E) 및 하드웨어 재부팅(\x1B!R)
                        tsplCmd = "~!E\r\n\x1B!R\r\n";
                        desc = "프린터 리셋 (재부팅)";
                        break;
                    case "FACTORY_RESET":
                    case "INITIALPRINTER":
                        if (!_dialogService.ShowQuestion("프린터의 모든 설정을 공장 초기화하시겠습니까?\n\n⚠️ 주의: 초기화 완료 후에는 센서 보정(Calibration) 작업을 다시 수행해야 합니다.", "프린터 공장 초기화 확인"))
                        {
                            return;
                        }
                        // [New_xPix 동일] TSPL 칩셋 및 모든 설정 공장 출하 초기화
                        tsplCmd = "INITIALPRINTER\r\nCLS\r\n";
                        desc = "프린터 공장 초기화";
                        break;
                }

                bool success = _printerService.SendTsplRawCommand(SelectedPrinterName, tsplCmd, _cachedPrinters, out string err);
                if (success)
                {
                    StatusMessage = $"{desc} 명령을 전송했습니다.";
                    _dialogService.ShowMessage($"{desc} 명령이 성공적으로 전송되었습니다.", "프린터 제어", MessageBoxImage.Information);

                    // 센서 보정(Calibration) 완료 후 최신 실측 감도 및 간격 백그라운드 자동 동기화
                    if (action == "CALIBRATE" || action == "GAPDETECT" || action == "AUTODETECT")
                    {
                        Task.Run(async () =>
                        {
                            await Task.Delay(2500);
                            var latest = _printerService.GetPrinterHardwareDetails(SelectedPrinterName, _cachedPrinters);
                            if (latest != null)
                            {
                                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                                {
                                    if (latest.TryGetValue("간격(Gap)", out string gVal))
                                    {
                                        var match = Regex.Match(gVal, @"\d+(\.\d+)?");
                                        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double g))
                                        {
                                            GapHeight = (int)Math.Round(g);
                                        }
                                    }
                                });
                            }
                        });
                    }
                }
                else
                {
                    StatusMessage = $"{desc} 전송 실패: {err}";
                    _dialogService.ShowError($"실행 오류: {err}", "오류");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"{action} 실행 오류: {ex.Message}";
                _dialogService.ShowError($"실행 오류: {ex.Message}", "오류");
            }
        }

        private void DrawTextOnDc(DrawingContext dc, string text, double fontSizeMm, double offsetXMm, double offsetYMm, double dotsPerMm, System.Windows.Media.Brush brush, string fontName = "맑은 고딕", bool isBold = true, bool isItalic = false, double cellWidthMm = 0, string align = "CENTER", double cellHeightMm = 0, double cellPaddingMm = 0, bool isUnderline = false, double fontScaleX = 1.0, double lineSpacing = 1.0, double letterSpacing = 0.0, bool isVertical = false, bool isMirrored = false, bool isInverse = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (isVertical)
            {
                text = ConvertToVerticalText(text);
            }

            // 화면 96 DPI(1mm = 3.779527559px, FontSize = size * 1.35) 대비 인쇄 해상도(1mm = dotsPerMm px) 1:1 완벽 비례 환산
            double scale = dotsPerMm / (96.0 / 25.4);
            double leftPx = offsetXMm * dotsPerMm;
            double topPx = offsetYMm * dotsPerMm;
            double targetPxSize = (fontSizeMm * 1.35) * scale;

            string familyName = string.IsNullOrWhiteSpace(fontName) ? "맑은 고딕" : fontName;
            var style = isItalic ? FontStyles.Italic : FontStyles.Normal;
            var weight = isBold ? FontWeights.Bold : FontWeights.Normal;

            double effScaleX = (fontScaleX > 0) ? fontScaleX : 1.0;
            double effLineSpacing = (lineSpacing > 0) ? lineSpacing : 1.0;

            // 미러(좌우 반전) 변환 적용
            bool pushedMirror = false;
            if (isMirrored)
            {
                double centerX = leftPx + (cellWidthMm > 0 ? (cellWidthMm * dotsPerMm) / 2.0 : 20.0);
                dc.PushTransform(new ScaleTransform(-1.0, 1.0, centerX, topPx));
                pushedMirror = true;
            }

            // 장평(가로 비율) 변환 적용
            bool pushedScale = false;
            if (Math.Abs(effScaleX - 1.0) > 0.001)
            {
                dc.PushTransform(new ScaleTransform(effScaleX, 1.0, leftPx, topPx));
                pushedScale = true;
            }

            if (isInverse)
            {
                brush = System.Windows.Media.Brushes.White;
            }

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new System.Windows.Media.FontFamily(familyName), style, weight, FontStretches.Normal),
                Math.Max(6, targetPxSize),
                brush,
                1.0);

            if (isUnderline)
            {
                ft.SetTextDecorations(TextDecorations.Underline);
            }

            if (effLineSpacing > 0 && Math.Abs(effLineSpacing - 1.0) > 0.001)
            {
                ft.LineHeight = Math.Max(6, targetPxSize) * effLineSpacing;
            }

            TextAlignment targetAlignment = TextAlignment.Left;
            if (string.Equals(align, "CENTER", StringComparison.OrdinalIgnoreCase))
            {
                targetAlignment = TextAlignment.Center;
            }
            else if (string.Equals(align, "RIGHT", StringComparison.OrdinalIgnoreCase))
            {
                targetAlignment = TextAlignment.Right;
            }
            ft.TextAlignment = targetAlignment;

            if (cellHeightMm > 0)
            {
                double cellHeightPx = cellHeightMm * dotsPerMm;
                topPx += Math.Max(0, (cellHeightPx - ft.Height) / 2.0);
            }

            double renderOriginX = leftPx;
            bool pushedAutoFit = false;

            if (cellWidthMm > 0)
            {
                // 셀 또는 박스 너비가 지정된 경우: 내부 패딩을 제외한 콘텐츠 너비로 줄바꿈 폭 지정
                double cellWidthPx = (cellWidthMm * dotsPerMm) / effScaleX;
                double padPx = (cellPaddingMm * dotsPerMm) / effScaleX;
                double contentWidthPx = Math.Max(1.0, cellWidthPx - (padPx * 2.0));

                double naturalWidthPx = ft.Width;
                bool hasExplicitNewline = text.Contains("\n") || text.Contains("\r");

                // [한 줄 인쇄 시 마지막 글자 줄바꿈 원천 차단]
                // 1) 텍스트에 명시적 줄바꿈(\n, \r)이 없는 단일행 텍스트
                // 2) 자연 너비가 콘텐츠 가용 너비 이하이거나,
                //    화면-인쇄 DPI 변환/폰트 힌팅 오차 범위(약 1.5글자 너비 이내) 내에서 약간 초과하는 경우
                double charTolerancePx = Math.Max(targetPxSize * 1.5, 20.0 * scale);
                bool shouldForceSingleLine = !hasExplicitNewline && (naturalWidthPx <= contentWidthPx + charTolerancePx);

                if (shouldForceSingleLine)
                {
                    if (naturalWidthPx > contentWidthPx)
                    {
                        // 콘텐츠 가용 폭보다 약간 큰 경우:
                        // 글자를 다음 줄로 넘기지 않고, 가로 배율(ScaleTransform)을 미세 조정하여
                        // 상자 너비 안에 100% 한 줄로 쏙 들어가도록 맞춤(Fit)
                        double autoFitScaleX = contentWidthPx / naturalWidthPx;
                        if (autoFitScaleX >= 0.75) // 최대 25% 이내의 미세 축소인 경우 1:1 박스 내 수용
                        {
                            double scaleOriginX = leftPx + padPx;
                            if (targetAlignment == TextAlignment.Center)
                            {
                                scaleOriginX = leftPx + (cellWidthPx / 2.0);
                            }
                            else if (targetAlignment == TextAlignment.Right)
                            {
                                scaleOriginX = leftPx + cellWidthPx - padPx;
                            }

                            dc.PushTransform(new ScaleTransform(autoFitScaleX, 1.0, scaleOriginX, topPx));
                            pushedAutoFit = true;
                            ft.MaxTextWidth = naturalWidthPx + 4.0; // 줄바꿈 원천 방지
                        }
                        else
                        {
                            // 축소율이 큰 경우에도 단일행 텍스트는 줄바꿈되지 않도록 여유 폭 부여
                            ft.MaxTextWidth = naturalWidthPx + 4.0;
                        }
                    }
                    else
                    {
                        // 완전히 들어가는 경우:
                        // 부동소수점 오차나 힌팅 오차로 인해 마지막 글자가 줄바꿈되는 것을 방지하기 위해
                        // MaxTextWidth를 여유 있게 부여
                        ft.MaxTextWidth = Math.Max(contentWidthPx, naturalWidthPx + 4.0);
                    }

                    renderOriginX = leftPx + padPx;
                }
                else
                {
                    // 명시적 줄바꿈이 있거나 여러 줄로 표시되어야 하는 긴 텍스트
                    ft.MaxTextWidth = contentWidthPx;
                    renderOriginX = leftPx + padPx;
                }
            }
            else
            {
                // cellWidthMm <= 0인 경우 (자연 크기 모드, MaxTextWidth 미지정)
                // MaxTextWidth가 없을 때 FormattedText는 TextAlignment에 따라
                // Center는 중심축, Right는 우측 끝점을 원점으로 요구함.
                if (targetAlignment == TextAlignment.Center)
                {
                    renderOriginX = leftPx + (ft.Width / 2.0);
                }
                else if (targetAlignment == TextAlignment.Right)
                {
                    renderOriginX = leftPx + ft.Width;
                }
                else
                {
                    renderOriginX = leftPx;
                }
            }

            if (isInverse)
            {
                double bgWPx = cellWidthMm > 0 ? (cellWidthMm * dotsPerMm) : (ft.Width + 10.0);
                double bgHPx = cellHeightMm > 0 ? (cellHeightMm * dotsPerMm) : (ft.Height + 8.0);
                dc.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(leftPx, topPx, bgWPx, bgHPx));
            }

            if (Math.Abs(letterSpacing) > 0.001)
            {
                // 자간(em 단위) 반영 렌더링
                string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                double currentY = topPx;
                double lineHeightPx = (effLineSpacing > 0) ? (Math.Max(6, targetPxSize) * effLineSpacing * 1.2) : (targetPxSize * 1.2);
                double extraCharSpacingPx = targetPxSize * letterSpacing;

                double maxLineWidthPx = 0;
                if (lines.Length > 1 && (targetAlignment == TextAlignment.Center || targetAlignment == TextAlignment.Right))
                {
                    foreach (var l in lines)
                    {
                        if (!string.IsNullOrEmpty(l))
                        {
                            var ftM = new FormattedText(l, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                new Typeface(new System.Windows.Media.FontFamily(familyName), style, weight, FontStretches.Normal),
                                Math.Max(6, targetPxSize), brush, 1.0);
                            double lw = ftM.Width + Math.Max(0, l.Length - 1) * extraCharSpacingPx;
                            if (lw > maxLineWidthPx) maxLineWidthPx = lw;
                        }
                    }
                }

                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        currentY += lineHeightPx;
                        continue;
                    }

                    double totalLineWidthPx = 0;
                    var charFts = new List<FormattedText>();
                    for (int ci = 0; ci < line.Length; ci++)
                    {
                        var ftChar = new FormattedText(
                            line[ci].ToString(),
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(new System.Windows.Media.FontFamily(familyName), style, weight, FontStretches.Normal),
                            Math.Max(6, targetPxSize),
                            brush,
                            1.0);
                        if (isUnderline) ftChar.SetTextDecorations(TextDecorations.Underline);
                        charFts.Add(ftChar);
                        totalLineWidthPx += ftChar.Width;
                        if (ci < line.Length - 1) totalLineWidthPx += extraCharSpacingPx;
                    }

                    double lineStartXPx = leftPx;
                    if (cellWidthMm > 0)
                    {
                        double cellWPx = (cellWidthMm * dotsPerMm) / effScaleX;
                        if (targetAlignment == TextAlignment.Center)
                        {
                            lineStartXPx = leftPx + Math.Max(0, (cellWPx - totalLineWidthPx) / 2.0);
                        }
                        else if (targetAlignment == TextAlignment.Right)
                        {
                            lineStartXPx = leftPx + Math.Max(0, cellWPx - totalLineWidthPx - ((cellPaddingMm * dotsPerMm) / effScaleX));
                        }
                    }
                    else if (maxLineWidthPx > 0)
                    {
                        if (targetAlignment == TextAlignment.Center)
                        {
                            lineStartXPx = leftPx + Math.Max(0, (maxLineWidthPx - totalLineWidthPx) / 2.0);
                        }
                        else if (targetAlignment == TextAlignment.Right)
                        {
                            lineStartXPx = leftPx + Math.Max(0, maxLineWidthPx - totalLineWidthPx);
                        }
                    }

                    double curCharX = lineStartXPx;
                    for (int ci = 0; ci < charFts.Count; ci++)
                    {
                        dc.DrawText(charFts[ci], new System.Windows.Point(curCharX, currentY));
                        curCharX += charFts[ci].Width + extraCharSpacingPx;
                    }

                    currentY += lineHeightPx;
                }
            }
            else
            {
                dc.DrawText(ft, new System.Windows.Point(renderOriginX, topPx));
            }

            if (pushedAutoFit)
            {
                dc.Pop();
            }
            if (pushedScale)
            {
                dc.Pop();
            }
            if (pushedMirror)
            {
                dc.Pop();
            }
        }

        private Bitmap GenerateLabelBitmapForPrint(string col1, string col2, string col3, string col4, string col5, string col6, string col7, string col8)
        {
            // 프린터 실제 해상도(300 DPI vs 203 DPI)에 따른 동적 dotsPerMm 계산 (300 DPI 축소 인쇄 원천 방지)
            double printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? (double)PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203.0;
            double dotsPerMm = printerDpi / 25.4;
            int effectiveW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80;
            int effectiveH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60;
            int pxW = (int)Math.Round(effectiveW * dotsPerMm);
            int pxH = (int)Math.Round(effectiveH * dotsPerMm);

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(-20, -20, pxW + 40, pxH + 40));

                // 전체 이미지 인쇄 위치 보정 오프셋 (PrinterOffsetX, PrinterOffsetY) 적용
                bool hasOffset = (PrinterOffsetX != 0 || PrinterOffsetY != 0);
                if (hasOffset)
                {
                    dc.PushTransform(new TranslateTransform(PrinterOffsetX * dotsPerMm, PrinterOffsetY * dotsPerMm));
                }

                var colValues = new string[] { col1, col2, col3, col4, col5, col6, col7, col8 };
                int activeCount = Math.Max(0, Math.Min(8, ActiveColumnCount));

                double startXMm = TableOffsetX >= 0 ? TableOffsetX : 2.0;
                double startYMm = TableOffsetY >= 0 ? TableOffsetY : 2.0;
                double tableWMm = TableWidth > 0 ? TableWidth : (effectiveW - (startXMm * 2));

                // 1-1. 표 위 제목 (Title) 인쇄 렌더링
                if (IsTableTitleEnabled && !string.IsNullOrWhiteSpace(TableTitleText))
                {
                    double titleY = startYMm + TableTitleOffsetY;
                    DrawTextOnDc(dc, TableTitleText, TableTitleSize, startXMm + TableTitleOffsetX, titleY, dotsPerMm, System.Windows.Media.Brushes.Black, TableTitleFont, TableTitleBold, TableTitleItalic, tableWMm, TableTitleAlign, 0, 0);

                    startYMm += Math.Max(4.0, TableTitleHeight) + TableTitleOffsetY;

                    if (IsTableTitleUnderline)
                    {
                        double titleLineYPx = startYMm * dotsPerMm;
                        var titlePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, Math.Max(1.0, TableBorderThickness));
                        double lineWPx = tableWMm * dotsPerMm;
                        dc.DrawLine(titlePen, new System.Windows.Point(startXMm * dotsPerMm, titleLineYPx), new System.Windows.Point((startXMm * dotsPerMm) + lineWPx, titleLineYPx));

                        startYMm += 1.0;
                    }

                    startYMm += TableTitleSpacing;
                }

                // 바코드 컬럼 여부 확인 및 텍스트 컬럼 리스트 추출
                var textColIndices = new List<int>();
                var barcodeColIndices = new List<int>();
                for (int i = 0; i < activeCount; i++)
                {
                    int colNum = i + 1;
                    TemplateElement elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                    bool isBarcodeCol = (elem != null)
                        ? (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE)
                        : GetBarcodeByIndex(colNum);

                    if (isBarcodeCol) barcodeColIndices.Add(colNum);
                    else textColIndices.Add(colNum);
                }

                int textRowCount = Math.Max(1, textColIndices.Count);
                double tableX = startXMm * dotsPerMm;
                double tableY = startYMm * dotsPerMm;
                tableWMm = TableWidth > 0 ? TableWidth : (PaperWidth - (startXMm * 2));
                double tableWPx = tableWMm * dotsPerMm;

                // 표 기준 높이 계산: TableOffsetY 변경 시 표가 찌그러지지 않고 아래로 이동하도록 처리
                double userSpecifiedTotalMm = 0;
                int autoCount = 0;
                for (int r = 0; r < textColIndices.Count; r++)
                {
                    int colNum = textColIndices[r];
                    int userH = GetRowHeightByIndex(colNum);
                    if (userH > 0) userSpecifiedTotalMm += userH;
                    else autoCount++;
                }

                // 표 기준 높이: 상단 제목 및 여백을 고려한 최대 가용 높이(availTableHMm)를 산출하여 인쇄영역을 벗어나지 않도록 보장
                double bottomMarginMm = IsDateTimeEnabled ? 6.0 : 2.0;
                double availTableHMm = Math.Max(6.0, PaperHeight - startYMm - bottomMarginMm);

                double baseTableHMm;
                if (TableHeight > 0)
                {
                    baseTableHMm = Math.Min(TableHeight, availTableHMm);
                }
                else if (autoCount == 0 && userSpecifiedTotalMm > 0)
                {
                    baseTableHMm = Math.Min(userSpecifiedTotalMm, availTableHMm);
                }
                else
                {
                    baseTableHMm = availTableHMm;
                }

                double tableHPx = baseTableHMm * dotsPerMm;

                // 행별 세로 높이(픽셀) 및 시작 Y좌표 계산 (개별 지정 높이 및 글자 크기 비례 자동 배분 지원)
                double[] rowHeightsPx = new double[textRowCount];
                double[] rowTopsPx = new double[textRowCount];
                double userSpecifiedTotalPx = userSpecifiedTotalMm * dotsPerMm;

                for (int r = 0; r < textColIndices.Count; r++)
                {
                    int colNum = textColIndices[r];
                    int userH = GetRowHeightByIndex(colNum);
                    if (userH > 0)
                    {
                        rowHeightsPx[r] = userH * dotsPerMm;
                    }
                }

                if (textColIndices.Count == 0)
                {
                    rowHeightsPx[0] = tableHPx;
                }
                else if (autoCount > 0)
                {
                    double remainingPx = Math.Max(autoCount * 4.0 * dotsPerMm, tableHPx - userSpecifiedTotalPx);
                    double totalWeight = 0;
                    double[] weights = new double[textRowCount];
                    for (int r = 0; r < textColIndices.Count; r++)
                    {
                        int colNum = textColIndices[r];
                        if (GetRowHeightByIndex(colNum) <= 0)
                        {
                            int sz = GetSizeByIndex(colNum);
                            weights[r] = Math.Max(6, sz);
                            totalWeight += weights[r];
                        }
                    }
                    for (int r = 0; r < textColIndices.Count; r++)
                    {
                        int colNum = textColIndices[r];
                        if (GetRowHeightByIndex(colNum) <= 0)
                        {
                            rowHeightsPx[r] = totalWeight > 0 ? remainingPx * (weights[r] / totalWeight) : (remainingPx / Math.Max(1, autoCount));
                        }
                    }
                }

                // 각 행의 Top 좌표 및 표의 실제 총 높이 계산
                double currentY = tableY;
                for (int r = 0; r < textRowCount; r++)
                {
                    rowTopsPx[r] = currentY;
                    currentY += rowHeightsPx[r];
                }
                double actualTableHPx = currentY - tableY;

                var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, Math.Max(1.0, TableBorderThickness));

                // 표 (Table / 격자 테두리) 인쇄 렌더링
                if (IsTableBorderEnabled)
                {
                    // [가로형 표 모드 (헤더 상단 / 열 단위 배치)]
                    if (TableOrientation == 1)
                    {
                        int colCount = Math.Max(1, textColIndices.Count);
                        double colWidthPx = tableWPx / colCount;
                        double colWidthMm = tableWMm / colCount;

                        bool anyIncludeHeader = textColIndices.Any(c => GetIncludeHeaderByIndex(c));
                        double headerHMm = anyIncludeHeader ? Math.Max(3.0, Math.Min(TableHeaderHeightMm, baseTableHMm * 0.6)) : 0;
                        double headerHPx = headerHMm * dotsPerMm;
                        double dataHMm = anyIncludeHeader ? Math.Max(3.0, baseTableHMm - headerHMm) : baseTableHMm;
                        double dataHPx = dataHMm * dotsPerMm;
                        double totalTableHPx = baseTableHMm * dotsPerMm;
                        double headerBottomY = tableY + headerHPx;

                        // 1. 외곽 박스
                        if (TableBorderStyle == 0 || TableBorderStyle == 1)
                        {
                            dc.DrawRectangle(null, pen, new Rect(tableX, tableY, tableWPx, totalTableHPx));
                        }

                        // 2. 가로 구분선
                        if (TableBorderStyle == 0 && anyIncludeHeader)
                        {
                            dc.DrawLine(pen, new System.Windows.Point(tableX, headerBottomY), new System.Windows.Point(tableX + tableWPx, headerBottomY));
                        }
                        else if (TableBorderStyle == 2)
                        {
                            // 상단선
                            dc.DrawLine(pen, new System.Windows.Point(tableX, tableY), new System.Windows.Point(tableX + tableWPx, tableY));
                            // 헤더 구분선
                            if (anyIncludeHeader)
                            {
                                dc.DrawLine(pen, new System.Windows.Point(tableX, headerBottomY), new System.Windows.Point(tableX + tableWPx, headerBottomY));
                            }
                            // 하단선
                            dc.DrawLine(pen, new System.Windows.Point(tableX, tableY + totalTableHPx), new System.Windows.Point(tableX + tableWPx, tableY + totalTableHPx));
                        }

                        // 3. 세로 구분선 (전체 격자 모드)
                        if (TableBorderStyle == 0)
                        {
                            for (int c = 1; c < colCount; c++)
                            {
                                double vx = tableX + (c * colWidthPx);
                                dc.DrawLine(pen, new System.Windows.Point(vx, tableY), new System.Windows.Point(vx, tableY + totalTableHPx));
                            }
                        }

                        // 4. 각 열별 텍스트 렌더링
                        for (int c = 0; c < textColIndices.Count; c++)
                        {
                            int colNum = textColIndices[c];
                            int i = colNum - 1;
                            string val = colValues[i] ?? "";
                            string header = GetHeaderByIndex(colNum);
                            int size = GetSizeByIndex(colNum);
                            bool isPrice = GetPriceByIndex(colNum);
                            bool isIncludeHeader = GetIncludeHeaderByIndex(colNum);
                            string headerAlign = GetHeaderAlignByIndex(colNum);
                            string contentAlign = GetAlignByIndex(colNum);
                            string fontName = GetFontByIndex(colNum);
                            bool isBold = GetBoldByIndex(colNum);
                            bool isItalic = GetItalicByIndex(colNum);

                            double colLeftPx = tableX + (c * colWidthPx);
                            double colLeftMm = colLeftPx / dotsPerMm;
                            double tableYMm = tableY / dotsPerMm;
                            double dataTopMm = (tableY + headerHPx) / dotsPerMm;

                            string displayText = val;
                            if (isPrice && !string.IsNullOrWhiteSpace(val))
                            {
                                displayText = PriceFormatHelper.FormatPrice(val, true);
                            }

                            if (anyIncludeHeader)
                            {
                                // 상단 헤더 셀
                                if (isIncludeHeader)
                                {
                                    DrawTextOnDc(dc, header, size, colLeftMm, tableYMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, colWidthMm, headerAlign, headerHMm, 1.0);
                                }

                                // 하단 내용 셀
                                if (!string.IsNullOrEmpty(displayText))
                                {
                                    DrawTextOnDc(dc, displayText, size, colLeftMm, dataTopMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, colWidthMm, contentAlign, dataHMm, 1.0);
                                }
                            }
                            else
                            {
                                // 헤더 없는 단일 행 셀
                                if (!string.IsNullOrEmpty(displayText))
                                {
                                    DrawTextOnDc(dc, displayText, size, colLeftMm, tableYMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, colWidthMm, contentAlign, baseTableHMm, 1.0);
                                }
                            }
                        }
                    }
                    else
                    {
                        // [기존 세로형 표 모드 (헤더 좌측 / 행 단위 배치)]
                        // 0: 전체 격자, 1: 외곽 박스
                        if (TableBorderStyle == 0 || TableBorderStyle == 1)
                        {
                            dc.DrawRectangle(null, pen, new Rect(tableX, tableY, tableWPx, actualTableHPx));
                        }

                        // 0: 전체 격자 (내부 가로선)
                        if (TableBorderStyle == 0)
                        {
                            for (int lineIdx = 1; lineIdx < textRowCount; lineIdx++)
                            {
                                double y = rowTopsPx[lineIdx];
                                dc.DrawLine(pen, new System.Windows.Point(tableX, y), new System.Windows.Point(tableX + tableWPx, y));
                            }
                        }
                        // 2: 가로 구분선만 (맨 위, 중간 구분선들, 맨 마지막 항목 아래까지 가로선 표시)
                        else if (TableBorderStyle == 2)
                        {
                            for (int lineIdx = 0; lineIdx <= textRowCount; lineIdx++)
                            {
                                double y = (lineIdx < textRowCount) ? rowTopsPx[lineIdx] : (rowTopsPx[textRowCount - 1] + rowHeightsPx[textRowCount - 1]);
                                dc.DrawLine(pen, new System.Windows.Point(tableX, y), new System.Windows.Point(tableX + tableWPx, y));
                            }
                        }

                        // 1. 텍스트 컬럼들만 순서대로 표 내부 각 행에 렌더링
                        for (int r = 0; r < textColIndices.Count; r++)
                        {
                            int colNum = textColIndices[r];
                            int i = colNum - 1;
                            string val = colValues[i] ?? "";
                            string header = GetHeaderByIndex(colNum);
                            int size = GetSizeByIndex(colNum);
                            bool isPrice = GetPriceByIndex(colNum);
                            bool isIncludeHeader = GetIncludeHeaderByIndex(colNum);
                            string headerAlign = GetHeaderAlignByIndex(colNum);
                            string contentAlign = GetAlignByIndex(colNum);
                            string fontName = GetFontByIndex(colNum);
                            bool isBold = GetBoldByIndex(colNum);
                            bool isItalic = GetItalicByIndex(colNum);

                            double rowTop = rowTopsPx[r];
                            double currentRowHPx = rowHeightsPx[r];
                            double rowBottom = rowTop + currentRowHPx;
                            double headerLineX = tableX + (TableHeaderWidthMm * dotsPerMm);

                            if (isIncludeHeader)
                            {
                                // 전체 격자(0) 스타일일 때만 제목과 내용 사이 세로 구분선 그리기
                                if (IsTableBorderEnabled && TableBorderStyle == 0)
                                {
                                    dc.DrawLine(pen, new System.Windows.Point(headerLineX, rowTop), new System.Windows.Point(headerLineX, rowBottom));
                                }

                                double rowHMm = currentRowHPx / dotsPerMm;
                                double rowTopMm = rowTop / dotsPerMm;

                                // 1. 좌측 헤더 셀
                                double headerWidthMm = TableHeaderWidthMm;
                                DrawTextOnDc(dc, header, size, (tableX / dotsPerMm), rowTopMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, headerWidthMm, headerAlign, rowHMm, 1.5);

                                // 2. 우측 내용 셀
                                string displayText = val;
                                if (isPrice && !string.IsNullOrWhiteSpace(val))
                                {
                                    displayText = PriceFormatHelper.FormatPrice(val, true);
                                }
                                if (!string.IsNullOrEmpty(displayText))
                                {
                                    double contentCellWidthMm = Math.Max(5.0, tableWMm - TableHeaderWidthMm);
                                    DrawTextOnDc(dc, displayText, size, (headerLineX / dotsPerMm), rowTopMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, contentCellWidthMm, contentAlign, rowHMm, 1.5);
                                }
                            }
                            else
                            {
                                // 텍스트 단독 (제목표시 미체크, 행 전체 단일 셀)
                                string displayText = val;
                                if (isPrice && !string.IsNullOrWhiteSpace(val))
                                {
                                    displayText = PriceFormatHelper.FormatPrice(val, true);
                                }

                                if (!string.IsNullOrEmpty(displayText))
                                {
                                    double rowHMm = currentRowHPx / dotsPerMm;
                                    double rowTopMm = rowTop / dotsPerMm;
                                    DrawTextOnDc(dc, displayText, size, (tableX / dotsPerMm), rowTopMm, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, tableWMm, contentAlign, rowHMm, 1.5);
                                }
                            }
                        }
                    }

                    // 2. 바코드 컬럼들은 표 외부 독립 렌더링
                    for (int b = 0; b < barcodeColIndices.Count; b++)
                    {
                        int colNum = barcodeColIndices[b];
                        int i = colNum - 1;
                        string val = colValues[i] ?? "";
                        string bType = GetBarcodeTypeByIndex(colNum);
                        bool showBText = GetBarcodeTextByIndex(colNum);
                        int size = GetSizeByIndex(colNum);
                        int offX = GetOffsetXByIndex(colNum);
                        int offY = GetOffsetYByIndex(colNum);
                        var elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                        double wMm = (elem != null && elem.Width > 0) ? elem.Width : GetWidthByIndex(colNum);
                        double hMm = (elem != null && elem.Height > 0) ? elem.Height : 0;
                        string textPos = (elem != null && !string.IsNullOrEmpty(elem.BarcodeTextPosition)) ? elem.BarcodeTextPosition : (showBText ? "BELOW" : "NONE");

                        bool pushedBarcodeRotate = false;
                        if (elem != null && elem.Rotation != 0)
                        {
                            double bLeftPx = offX * dotsPerMm;
                            double bTopPx = offY * dotsPerMm;
                            double bWidthPx = (wMm > 0 ? wMm : (bType != null && bType.Contains("QR") ? 18.0 : 45.0)) * dotsPerMm;
                            double bHeightPx = (hMm > 0 ? hMm : 15.0) * dotsPerMm;
                            dc.PushTransform(new RotateTransform(elem.Rotation, bLeftPx + (bWidthPx / 2.0), bTopPx + (bHeightPx / 2.0)));
                            pushedBarcodeRotate = true;
                        }

                        try
                        {
                            BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText, textPos, wMm, hMm);
                        }
                        finally
                        {
                            if (pushedBarcodeRotate)
                            {
                                dc.Pop();
                            }
                        }
                    }
                }
                else
                {
                    // 표 비활성화 모드 (자유 레이아웃)
                    if (CurrentTemplate?.Elements != null && CurrentTemplate.Elements.Count > 0)
                    {
                        // 템플릿의 Z-Order(앞/뒤 순서) 리스트 그대로 1:1 완벽 인쇄
                        foreach (var elem in CurrentTemplate.Elements)
                        {
                            if (elem.Id == "TITLE_ELEMENT") continue;
                            if (elem.IsVariable && elem.InputOrder > ActiveColumnCount) continue;

                            RenderSingleElementOnDc(dc, elem, dotsPerMm, colValues);
                        }
                    }
                    else
                    {
                        // 레거시 폴백 렌더링
                        for (int i = 0; i < activeCount; i++)
                        {
                            int colNum = i + 1;
                            string val = colValues[i] ?? "";
                            string header = GetHeaderByIndex(colNum);
                            bool isBarcode = GetBarcodeByIndex(colNum);
                            string bType = GetBarcodeTypeByIndex(colNum);
                            bool showBText = GetBarcodeTextByIndex(colNum);
                            int size = GetSizeByIndex(colNum);
                            int offX = GetOffsetXByIndex(colNum);
                            int offY = GetOffsetYByIndex(colNum);
                            bool isPrice = GetPriceByIndex(colNum);
                            bool isIncludeHeader = GetIncludeHeaderByIndex(colNum);
                            string fontName = GetFontByIndex(colNum);
                            bool isBold = GetBoldByIndex(colNum);
                            bool isItalic = GetItalicByIndex(colNum);

                            if (isBarcode)
                            {
                                double wMm = GetWidthByIndex(colNum);
                                string textPos = showBText ? "BELOW" : "NONE";
                                BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText, textPos, wMm, 0);
                            }
                            else
                            {
                                string displayText = val;
                                if (isPrice && !string.IsNullOrWhiteSpace(val)) displayText = PriceFormatHelper.FormatPrice(val, true);
                                if (isIncludeHeader && !string.IsNullOrWhiteSpace(header)) displayText = $"{header}: {displayText}";
                                if (!string.IsNullOrEmpty(displayText))
                                {
                                    string align = GetAlignByIndex(colNum);
                                    int userWMm = GetWidthByIndex(colNum);
                                    DrawTextOnDc(dc, displayText, size, offX, offY, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, userWMm, align, 0, 0);
                                }
                            }
                        }
                    }
                }

                bool hasDateTimeInTemplate = CurrentTemplate?.Elements?.Any(el => el.Id == "DATETIME_ELEMENT" || el.Type == ElementType.DATE_TIME) ?? false;
                if (IsDateTimeEnabled && !hasDateTimeInTemplate)
                {
                    string currentDtStr = GetFormattedDateTimeString();
                    DrawTextOnDc(dc, currentDtStr, DateTimeSize, DateTimeOffsetX, DateTimeOffsetY, dotsPerMm, System.Windows.Media.Brushes.Black, DateTimeFont, DateTimeBold, DateTimeItalic, DateTimeWidth, "CENTER", 0, 0);
                }

                bool hasSerialInTemplate = CurrentTemplate?.Elements?.Any(el => el.Id == "SERIAL_ELEMENT") ?? false;
                if (IsSerialEnabled && !hasSerialInTemplate)
                {
                    string serialStr = FormattedSerialPreview;
                    DrawTextOnDc(dc, serialStr, SerialSize, SerialOffsetX, SerialOffsetY, dotsPerMm, System.Windows.Media.Brushes.Black, SerialFont, SerialBold, SerialItalic, SerialWidth, SerialAlign, 0, 0);
                }

                if (IsTableBorderEnabled)
                {
                    // 표 활성화 모드에서는 커스텀 요소 후속 렌더링
                    RenderCustomElementsOnDc(dc, dotsPerMm);
                }

                if (hasOffset)
                {
                    dc.Pop();
                }
            }

            var rtb = new RenderTargetBitmap(pxW, pxH, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);

            // [일부 PC 백지 출력 원천 차단]
            // PngBitmapEncoder/WIC 코덱 의존성을 완전히 제거하고, RenderTargetBitmap 메모리 버퍼를 직접 추출
            // 특정 PC(내장 그래픽/RDP/고DPI 등)에서 Alpha=0 투명 픽셀이 검정/흰색으로 꼬이는 현상을 방지하기 위해
            // 모든 픽셀을 순수 불투명 흰색(#FFFFFFFF) 배경과 완벽하게 알파 블렌딩 합성(Flatten)
            int stride = pxW * 4;
            byte[] pixelBytes = new byte[stride * pxH];
            rtb.CopyPixels(pixelBytes, stride, 0);

            for (int i = 0; i < pixelBytes.Length; i += 4)
            {
                byte a = pixelBytes[i + 3];
                if (a == 0)
                {
                    // 완전 투명 영역은 라벨 용지 바탕(순수 흰색)으로 강제 변환
                    pixelBytes[i] = 255;     // B
                    pixelBytes[i + 1] = 255; // G
                    pixelBytes[i + 2] = 255; // R
                    pixelBytes[i + 3] = 255; // A
                }
                else if (a < 255)
                {
                    // 반투명/안티에일리어싱 픽셀을 흰색 배경과 표준 합성
                    int b = pixelBytes[i];
                    int g = pixelBytes[i + 1];
                    int r = pixelBytes[i + 2];
                    pixelBytes[i] = (byte)Math.Min(255, b + (255 - a));
                    pixelBytes[i + 1] = (byte)Math.Min(255, g + (255 - a));
                    pixelBytes[i + 2] = (byte)Math.Min(255, r + (255 - a));
                    pixelBytes[i + 3] = 255;
                }
            }

            var gdiBitmap = new Bitmap(pxW, pxH, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bmpData = gdiBitmap.LockBits(
                new Rectangle(0, 0, pxW, pxH),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb
            );
            try
            {
                Marshal.Copy(pixelBytes, 0, bmpData.Scan0, pixelBytes.Length);
            }
            finally
            {
                gdiBitmap.UnlockBits(bmpData);
            }

            int effectiveRotation = LabelRotationAngle;
            if (CurrentTemplate != null && CurrentTemplate.Rotation != 0)
            {
                effectiveRotation = CurrentTemplate.Rotation;
            }

            switch (effectiveRotation)
            {
                case 90:
                    gdiBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 180:
                    gdiBitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 270:
                    gdiBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
            }
            return gdiBitmap;
        }

        public System.Windows.Media.ImageSource RenderPageThumbnail(List<TemplateElement> pageElements)
        {
            try
            {
                int effectiveW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80;
                int effectiveH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60;

                // 썸네일용 해상도 계산 (최대 140px 기준으로 선명하고 빠르게 렌더링)
                double baseDotsPerMm = 96.0 / 25.4;
                double rawW = effectiveW * baseDotsPerMm;
                double rawH = effectiveH * baseDotsPerMm;
                double scale = Math.Min(1.0, 140.0 / Math.Max(1.0, Math.Max(rawW, rawH)));
                if (Math.Max(rawW, rawH) < 70.0)
                {
                    scale = Math.Min(2.5, 110.0 / Math.Max(1.0, Math.Max(rawW, rawH)));
                }
                double dotsPerMm = baseDotsPerMm * scale;
                int pxW = Math.Max(16, (int)Math.Round(effectiveW * dotsPerMm));
                int pxH = Math.Max(12, (int)Math.Round(effectiveH * dotsPerMm));

                var drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, pxW, pxH));
                    if (pageElements != null)
                    {
                        foreach (var elem in pageElements)
                        {
                            RenderSingleElementOnDc(dc, elem, dotsPerMm, null);
                        }
                    }
                }

                var rtb = new RenderTargetBitmap(pxW, pxH, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(drawingVisual);
                rtb.Freeze();
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        private void RenderSingleElementOnDc(DrawingContext dc, TemplateElement elem, double dotsPerMm, string[] colValues = null)
        {
            if (elem == null || dc == null) return;

            double leftPx = elem.X * dotsPerMm;
            double topPx = elem.Y * dotsPerMm;
            double widthPx = elem.Width * dotsPerMm;
            double heightPx = elem.Height * dotsPerMm;

            bool pushedRotate = false;
            if (elem.Rotation != 0)
            {
                double centerX;
                double centerY;

                if (elem.Type == ElementType.LINE)
                {
                    // LINE은 시작점(0, 0.5) 기준 회전
                    centerX = leftPx;
                    centerY = topPx + (heightPx / 2.0);
                }
                else
                {
                    // 그 외 모든 요소(TEXT, DATE_TIME, BARCODE, QR_CODE, SHAPE, IMAGE, TABLE)는 요소 중앙(0.5, 0.5) 기준 회전
                    double effW = widthPx;
                    double effH = heightPx;

                    if (effW <= 0)
                    {
                        if (elem.Type == ElementType.BARCODE) effW = 45.0 * dotsPerMm;
                        else if (elem.Type == ElementType.QR_CODE) effW = 18.0 * dotsPerMm;
                        else effW = 30.0 * dotsPerMm;
                    }

                    if (effH <= 0)
                    {
                        if (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE) effH = 15.0 * dotsPerMm;
                        else if (elem.Type == ElementType.TEXT || elem.Type == ElementType.DATE_TIME) effH = Math.Max(4.0, (elem.FontSize > 0 ? elem.FontSize : 10.0) * 1.35) * dotsPerMm;
                        else effH = 10.0 * dotsPerMm;
                    }

                    centerX = leftPx + (effW / 2.0);
                    centerY = topPx + (effH / 2.0);
                }

                dc.PushTransform(new RotateTransform(elem.Rotation, centerX, centerY));
                pushedRotate = true;
            }

            try
            {
                switch (elem.Type)
                {
                    case ElementType.LINE:
                        var linePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, Math.Max(1.0, elem.LineWidth));
                        var dashArr = ShapeGeometryHelper.GetStrokeDashArray(elem.LineStyle);
                        if (dashArr != null)
                        {
                            linePen.DashStyle = new DashStyle(dashArr, 0);
                        }
                        var lineGeo = ShapeGeometryHelper.CreateLineGeometry(elem.LineStyle, widthPx, heightPx, elem.LineWidth, leftPx, topPx);
                        dc.DrawGeometry(null, linePen, lineGeo);
                        break;

                    case ElementType.SHAPE:
                        double shapeStroke = Math.Max(1.0, elem.LineWidth);
                        var shapePen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, shapeStroke)
                        {
                            LineJoin = PenLineJoin.Round
                        };
                        var shapeBrush = elem.IsFilled ? System.Windows.Media.Brushes.Black : null;
                        var shapeGeo = ShapeGeometryHelper.CreateShapeGeometry(elem.ShapeType, widthPx, heightPx, shapeStroke, leftPx, topPx);
                        dc.DrawGeometry(shapeBrush, shapePen, shapeGeo);
                        break;

                    case ElementType.TABLE:
                        RenderTableOnDc(dc, elem, dotsPerMm, _currentPrintingRecord);
                        break;

                    case ElementType.BARCODE:
                    case ElementType.QR_CODE:
                        string barcodeContent = GetElementEffectiveContent(elem);
                        if (!IsExcelFileLoaded && elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= 8 && colValues != null && elem.InputOrder - 1 < colValues.Length)
                        {
                            string val = colValues[elem.InputOrder - 1];
                            if (!string.IsNullOrEmpty(val)) barcodeContent = val;
                        }
                        if (!string.IsNullOrWhiteSpace(barcodeContent))
                        {
                            BarcodeVisualHelper.RenderBarcodeOnDc(dc, barcodeContent, elem.BarcodeType, elem.FontSize, elem.X, elem.Y, dotsPerMm, elem.ShowBarcodeText, elem.BarcodeTextPosition, elem.Width, elem.Height);
                        }
                        break;

                    case ElementType.TEXT:
                    case ElementType.DATE_TIME:
                        string textContent = GetElementEffectiveContent(elem);
                        if (!IsExcelFileLoaded && elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= 8 && colValues != null && elem.InputOrder - 1 < colValues.Length)
                        {
                            string val = colValues[elem.InputOrder - 1];
                            if (!string.IsNullOrEmpty(val)) textContent = val;
                        }
                        else if (elem.Type == ElementType.DATE_TIME && IsDateTimeEnabled &&
                                 (!IsExcelFileLoaded || string.IsNullOrEmpty(elem.DataSourceColumn) || elem.DataSourceColumn == "0"))
                        {
                            textContent = GetFormattedDateTimeString();
                        }

                        if (elem.Type == ElementType.TEXT)
                        {
                            if (elem.IsPriceFormat && !string.IsNullOrWhiteSpace(textContent))
                            {
                                textContent = PriceFormatHelper.FormatPrice(textContent, true);
                            }
                            if (elem.IncludeHeaderOnLabel && !string.IsNullOrWhiteSpace(elem.HeaderName))
                            {
                                textContent = $"{elem.HeaderName}: {textContent}";
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(textContent))
                        {
                            double effWidth = elem.Width;
                            if (effWidth <= 0 && (elem.Alignment == "CENTER" || elem.Alignment == "RIGHT"))
                            {
                                effWidth = CalculateExactTextWidthMm(textContent, elem.FontName, (int)Math.Round(elem.FontSize > 0 ? elem.FontSize : 10.0), elem.IsBold, elem.IsItalic);
                            }
                            DrawTextOnDc(dc, textContent, elem.FontSize, elem.X, elem.Y, dotsPerMm, System.Windows.Media.Brushes.Black, elem.FontName, elem.IsBold, elem.IsItalic, effWidth, elem.Alignment, elem.Height, 0, elem.IsUnderline, elem.FontScaleX, elem.LineSpacing, elem.LetterSpacing, elem.IsVertical, elem.IsMirrored, elem.IsInverse);
                        }
                        break;

                    case ElementType.IMAGE:
                        if (!string.IsNullOrWhiteSpace(elem.Content) && System.IO.File.Exists(elem.Content))
                        {
                            try
                            {
                                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(elem.Content);
                                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                dc.DrawImage(bmp, new Rect(leftPx, topPx, widthPx, heightPx));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Image render error: {ex.Message}");
                            }
                        }
                        break;
                }
            }
            finally
            {
                if (pushedRotate)
                {
                    dc.Pop();
                }
            }
        }

        private void RenderCustomElementsOnDc(DrawingContext dc, double dotsPerMm)
        {
            if (CurrentTemplate == null || CurrentTemplate.Elements == null) return;

            foreach (var elem in CurrentTemplate.Elements)
            {
                if (elem.IsVariable && elem.InputOrder <= 8 && elem.InputOrder <= ActiveColumnCount) continue;
                if (elem.Id == "TITLE_ELEMENT" || elem.Id == "DATETIME_ELEMENT") continue;
                RenderSingleElementOnDc(dc, elem, dotsPerMm);
            }
        }

        private void RenderTableOnDc(DrawingContext dc, TemplateElement elem, double dotsPerMm, PrintRecordModel record = null)
        {
            if (elem == null || dc == null) return;
            elem.EnsureTableData();

            int rows = Math.Max(1, elem.Rows);
            int cols = Math.Max(1, elem.Columns);
            double x = elem.X * dotsPerMm;
            double y = elem.Y * dotsPerMm;
            double w = elem.Width * dotsPerMm;
            double h = elem.Height * dotsPerMm;
            if (w <= 0 || h <= 0) return;

            // 1. 각 열 너비 경계 계산 (ColumnWidths 비중 반영)
            double[] colX = new double[cols + 1];
            colX[0] = x;
            double[] colWeights = elem.ColumnWidths;
            double totalColWeight = 0;
            if (colWeights != null && colWeights.Length == cols)
            {
                foreach (var weight in colWeights) totalColWeight += Math.Max(0.1, weight);
            }
            if (totalColWeight <= 0) totalColWeight = cols;

            double currentX = x;
            for (int c = 0; c < cols; c++)
            {
                double weight = (colWeights != null && colWeights.Length == cols) ? Math.Max(0.1, colWeights[c]) : 1.0;
                double cw = w * (weight / totalColWeight);
                currentX += cw;
                colX[c + 1] = (c == cols - 1) ? x + w : currentX;
            }

            // 2. 각 행 높이 경계 계산 (RowHeights 비중 반영)
            double[] rowY = new double[rows + 1];
            rowY[0] = y;
            double[] rowWeights = elem.RowHeights;
            double totalRowWeight = 0;
            if (rowWeights != null && rowWeights.Length == rows)
            {
                foreach (var weight in rowWeights) totalRowWeight += Math.Max(0.1, weight);
            }
            if (totalRowWeight <= 0) totalRowWeight = rows;

            double currentY = y;
            for (int r = 0; r < rows; r++)
            {
                double weight = (rowWeights != null && rowWeights.Length == rows) ? Math.Max(0.1, rowWeights[r]) : 1.0;
                double rh = h * (weight / totalRowWeight);
                currentY += rh;
                rowY[r + 1] = (r == rows - 1) ? y + h : currentY;
            }

            // 3. 헤더 행 음영 (IsFilled 시)
            if (elem.IsFilled && rows > 0)
            {
                var headerRect = new Rect(x, y, w, Math.Max(1.0, rowY[1] - rowY[0]));
                dc.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), null, headerRect);
            }

            // 4. 표 외곽선 및 격자선 그리기
            double pxScale = dotsPerMm / (96.0 / 25.4);
            double outerStrokePx = Math.Max(0.5, elem.LineWidth * pxScale);
            string outerStyle = string.IsNullOrWhiteSpace(elem.LineStyle) ? "SOLID" : elem.LineStyle.ToUpperInvariant();

            double innerStrokePx = Math.Max(0.5, (elem.TableInnerLineWidth ?? elem.LineWidth) * pxScale);
            string innerStyle = string.IsNullOrWhiteSpace(elem.TableInnerLineStyle) ? "SOLID" : elem.TableInnerLineStyle.ToUpperInvariant();

            // 외곽 테두리
            if (outerStyle != "NONE" && outerStrokePx > 0)
            {
                var outerPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, outerStrokePx);
                var outerDash = ShapeGeometryHelper.GetStrokeDashArray(outerStyle);
                if (outerDash != null)
                {
                    outerPen.DashStyle = new DashStyle(outerDash, 0);
                }
                dc.DrawRectangle(null, outerPen, new Rect(x, y, w, h));
            }

            // 내부 세로/가로 격자선
            if (innerStyle != "NONE" && innerStrokePx > 0)
            {
                var innerPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, innerStrokePx);
                var innerDash = ShapeGeometryHelper.GetStrokeDashArray(innerStyle);
                if (innerDash != null)
                {
                    innerPen.DashStyle = new DashStyle(innerDash, 0);
                }

                for (int c = 1; c < cols; c++)
                {
                    dc.DrawLine(innerPen, new System.Windows.Point(colX[c], y), new System.Windows.Point(colX[c], y + h));
                }

                for (int r = 1; r < rows; r++)
                {
                    dc.DrawLine(innerPen, new System.Windows.Point(x, rowY[r]), new System.Windows.Point(x + w, rowY[r]));
                }
            }

            // 5. 셀 텍스트 렌더링
            for (int r = 0; r < rows; r++)
            {
                bool isHeader = (r == 0);
                for (int c = 0; c < cols; c++)
                {
                    string cellText = GetTableCellEffectiveContent(elem, r, c, record);
                    if (string.IsNullOrEmpty(cellText)) continue;

                    double cellLeftMm = (colX[c]) / dotsPerMm;
                    double cellTopMm = (rowY[r]) / dotsPerMm;
                    double cellWidthMm = (colX[c + 1] - colX[c]) / dotsPerMm;
                    double cellHeightMm = (rowY[r + 1] - rowY[r]) / dotsPerMm;

                    var (cFontName, cFontSize, cBold, cAlign) = elem.GetCellResolvedStyle(r, c);
                    string effectiveFontName = string.IsNullOrWhiteSpace(cFontName) ? (string.IsNullOrWhiteSpace(elem.FontName) ? "맑은 고딕" : elem.FontName) : cFontName;

                    DrawTextOnDc(
                        dc,
                        cellText,
                        cFontSize > 0 ? cFontSize : 9.0,
                        cellLeftMm,
                        cellTopMm,
                        dotsPerMm,
                        System.Windows.Media.Brushes.Black,
                        effectiveFontName,
                        cBold,
                        elem.IsItalic,
                        cellWidthMm,
                        cAlign ?? "CENTER",
                        cellHeightMm,
                        Math.Max(0.5, elem.CellPadding),
                        elem.IsUnderline,
                        elem.FontScaleX,
                        elem.LineSpacing,
                        elem.LetterSpacing
                    );
                }
            }
        }

        public bool SendToTsplPrinter(string col1, string col2, string col3, string col4, string col5, string col6, string col7, string col8, int copies = 1)
        {
            try
            {
                if (AvailablePrinters == null || AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
                {
                    _dialogService.ShowWarning("선택된 프린터가 없습니다. 프린터를 선택해 주세요.", "인쇄 알림");
                    return false;
                }

                BarcodePrint.Services.PrinterService.SyncGlobalPrinterDpi(SelectedPrinterName);

                int effectiveCopies = Math.Max(1, copies);
                var serialElements = CurrentTemplate?.Elements?.Where(el => el != null && el.IsSerial).ToList();
                bool hasElementSerial = serialElements != null && serialElements.Count > 0;

                if (hasElementSerial)
                {
                    bool allOk = true;
                    if (IsMultiLabelLayout)
                    {
                        int cols = Math.Max(1, LabelColumns);
                        int rows = Math.Max(1, LabelRows);
                        int slotsPerSheet = cols * rows;
                        double slotW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80.0;
                        double slotH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60.0;
                        double totalW = GetTotalSheetWidthMm();
                        double totalH = GetTotalSheetHeightMm();

                        for (int offset = 0; offset < effectiveCopies; offset += slotsPerSheet)
                        {
                            int countInSheet = Math.Min(slotsPerSheet, effectiveCopies - offset);
                            var slotBmps = new List<Bitmap>(countInSheet);
                            try
                            {
                                for (int s = 0; s < countInSheet; s++)
                                {
                                    slotBmps.Add(GenerateLabelBitmapForPrint(col1, col2, col3, col4, col5, col6, col7, col8));
                                    AdvanceSerialElements(serialElements);
                                }
                                using (var sheetBmp = ComposeMultiLabelSheetBitmap(slotBmps, slotW, slotH, totalW, totalH))
                                {
                                    allOk &= SendSheetBitmapToTsplPrinter(sheetBmp, totalW, totalH, 1);
                                }
                            }
                            finally
                            {
                                foreach (var b in slotBmps) b?.Dispose();
                            }
                        }
                    }
                    else
                    {
                        double wMm = PaperWidth > 0 ? PaperWidth : 80;
                        double hMm = PaperHeight > 0 ? PaperHeight : 60;
                        for (int i = 0; i < effectiveCopies; i++)
                        {
                            using (var labelBmp = GenerateLabelBitmapForPrint(col1, col2, col3, col4, col5, col6, col7, col8))
                            {
                                allOk &= SendSheetBitmapToTsplPrinter(labelBmp, wMm, hMm, 1);
                            }
                            AdvanceSerialElements(serialElements);
                        }
                    }

                    NotifySerialElementsUpdated();
                    return allOk;
                }

                using (var labelBmp = GenerateLabelBitmapForPrint(col1, col2, col3, col4, col5, col6, col7, col8))
                {
                    if (IsMultiLabelLayout)
                    {
                        int cols = Math.Max(1, LabelColumns);
                        int rows = Math.Max(1, LabelRows);
                        int slotsPerSheet = cols * rows;
                        int fullSheets = effectiveCopies / slotsPerSheet;
                        int remainder = effectiveCopies % slotsPerSheet;

                        double slotW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80.0;
                        double slotH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60.0;
                        double totalW = GetTotalSheetWidthMm();
                        double totalH = GetTotalSheetHeightMm();

                        bool allOk = true;
                        if (fullSheets > 0)
                        {
                            var fullSlots = Enumerable.Repeat(labelBmp, slotsPerSheet).ToList();
                            using (var sheetBmp = ComposeMultiLabelSheetBitmap(fullSlots, slotW, slotH, totalW, totalH))
                            {
                                allOk &= SendSheetBitmapToTsplPrinter(sheetBmp, totalW, totalH, fullSheets);
                            }
                        }
                        if (remainder > 0)
                        {
                            var partSlots = Enumerable.Repeat(labelBmp, remainder).ToList();
                            using (var sheetBmp = ComposeMultiLabelSheetBitmap(partSlots, slotW, slotH, totalW, totalH))
                            {
                                allOk &= SendSheetBitmapToTsplPrinter(sheetBmp, totalW, totalH, 1);
                            }
                        }
                        return allOk;
                    }

                    return SendSheetBitmapToTsplPrinter(labelBmp, PaperWidth > 0 ? PaperWidth : 80, PaperHeight > 0 ? PaperHeight : 60, effectiveCopies);
                }
            }
            catch (Exception ex)
            {
                PrinterSerialLogger.LogError("SendToTsplPrinter 실패", ex);
                StatusMessage = $"인쇄 생성 실패: {ex.Message}";
                return false;
            }
        }

        private void AdvanceSerialElements(List<TemplateElement> serialElements)
        {
            if (serialElements == null) return;
            foreach (var el in serialElements)
            {
                int step = el.SerialStep != 0 ? el.SerialStep : 1;
                el.SerialStartNumber = Math.Max(0, el.SerialStartNumber + step);
                el.Content = el.FormatSerialValue();
            }
        }

        private void NotifySerialElementsUpdated()
        {
            OnPropertyChanged(nameof(SelectedElementSerialStartNumber));
            OnPropertyChanged(nameof(SelectedElementSerialPreview));
            OnPropertyChanged(nameof(SelectedElementContent));
            OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
            UpdatePreview();
        }

        private Bitmap ComposeMultiLabelSheetBitmap(List<Bitmap> slotBitmaps, double slotWMm, double slotHMm, double sheetWMm, double sheetHMm)
        {
            double printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? (double)PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203.0;
            double dotsPerMm = printerDpi / 25.4;

            int sheetPxW = Math.Max(16, (int)Math.Round(sheetWMm * dotsPerMm));
            int sheetPxH = Math.Max(16, (int)Math.Round(sheetHMm * dotsPerMm));
            var sheetBmp = new Bitmap(sheetPxW, sheetPxH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(sheetBmp))
            {
                g.Clear(System.Drawing.Color.White);
                int cols = Math.Max(1, LabelColumns);
                int rows = Math.Max(1, LabelRows);
                int slotW = Math.Max(1, (int)Math.Round(slotWMm * dotsPerMm));
                int slotH = Math.Max(1, (int)Math.Round(slotHMm * dotsPerMm));

                for (int i = 0; i < slotBitmaps.Count && i < cols * rows; i++)
                {
                    var bmp = slotBitmaps[i];
                    if (bmp == null) continue;
                    int r = i / cols;
                    int c = i % cols;
                    int x = (int)Math.Round((PageMarginLeftMm + c * (slotWMm + ColumnGapMm)) * dotsPerMm);
                    int y = (int)Math.Round((PageMarginTopMm + r * (slotHMm + RowGapMm)) * dotsPerMm);
                    g.DrawImage(bmp, x, y, slotW, slotH);
                }
            }
            return sheetBmp;
        }

        private bool SendSheetBitmapToTsplPrinter(Bitmap sheetBmp, double sheetWMm, double sheetHMm, int copies)
        {
            int effectiveCopies = Math.Max(1, copies);
            PRINT_INFO_CLASS.PrinterInfo.Paper_Width = (int)Math.Round(sheetWMm);
            PRINT_INFO_CLASS.PrinterInfo.Paper_Height = (int)Math.Round(sheetHMm);
            PRINT_INFO_CLASS.PrinterInfo.Paper_Width_Mm = sheetWMm;
            PRINT_INFO_CLASS.PrinterInfo.Paper_Height_Mm = sheetHMm;
            PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed = PrintSpeed;
            PRINT_INFO_CLASS.PrinterInfo.nDarkness = PrintDarkness;
            PRINT_INFO_CLASS.PrinterInfo.nGAP = GapHeight;
            PRINT_INFO_CLASS.PrinterInfo.bRibbon = IsRibbon;
            PRINT_INFO_CLASS.PrinterInfo.offset_X = 0;
            PRINT_INFO_CLASS.PrinterInfo.offset_Y = 0;
            PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = IsRotated;
            PRINT_INFO_CLASS.PrinterInfo.PaperType = PaperType == "BLINE" ? PAPER_TYPE.BLINE : (PaperType == "CONTINUOUS" ? PAPER_TYPE.CONTINEUS : PAPER_TYPE.GAP);

            string extraCmdsStr = null;
            if (PostPrintAction != "NONE" || PostPrintOffset != 0)
            {
                var extraCmds = new StringBuilder();
                if (PostPrintOffset != 0) extraCmds.Append($"OFFSET {PostPrintOffset} mm\r\n");

                switch (PostPrintAction)
                {
                    case "CUTTER":
                        extraCmds.Append("SET PEEL OFF\r\nSET TEAR OFF\r\n");
                        switch (PostPrintCutTrigger)
                        {
                            case "EVERY_PAGE": extraCmds.Append("SET CUTTER 1\r\n"); break;
                            case "AFTER_COPIES": extraCmds.Append($"SET CUTTER {effectiveCopies}\r\n"); break;
                            case "AFTER_JOB": extraCmds.Append("SET CUTTER BATCH\r\n"); break;
                            case "CUSTOM_INTERVAL": extraCmds.Append($"SET CUTTER {Math.Max(1, PostPrintCutInterval)}\r\n"); break;
                            default: extraCmds.Append("SET CUTTER 1\r\n"); break;
                        }
                        extraCmds.Append("SET BACK ON\r\n");
                        break;
                    case "TEAR": extraCmds.Append("SET CUTTER OFF\r\nSET PEEL OFF\r\nSET TEAR ON\r\n"); break;
                    case "PEEL": extraCmds.Append("SET CUTTER OFF\r\nSET TEAR OFF\r\nSET PEEL ON\r\n"); break;
                    case "NONE": extraCmds.Append("SET CUTTER OFF\r\nSET PEEL OFF\r\nSET TEAR OFF\r\n"); break;
                }
                if (extraCmds.Length > 0) extraCmdsStr = extraCmds.ToString();
            }

            try
            {
                byte[] tsplCommandBytes = PrintManager.ConvertBitmapToTSPL_(sheetBmp, effectiveCopies, extraCmdsStr);
                bool printed = _printerService.SendLabelPrintBytes(SelectedPrinterName, tsplCommandBytes, _cachedPrinters, out string printErr);
                if (printed)
                {
                    if (IsSerialEnabled)
                    {
                        int stepDelta = IsSerialDecrement ? -Math.Abs(SerialStep) : Math.Abs(SerialStep);
                        SerialStartNumber = Math.Max(0, SerialStartNumber + stepDelta);
                    }
                }
                else
                {
                    StatusMessage = $"인쇄 실패: {printErr}";
                    System.Diagnostics.Debug.WriteLine($"SendLabelPrintBytes 실패: {printErr}");
                }
                return printed;
            }
            finally
            {
                PRINT_INFO_CLASS.PrinterInfo.Paper_Width_Mm = 0;
                PRINT_INFO_CLASS.PrinterInfo.Paper_Height_Mm = 0;
            }
        }

        public bool PrintRecordsMultiColumn(List<PrintRecordModel> targetRecords, int copiesPerRecord)
        {
            if (targetRecords == null || targetRecords.Count == 0) return false;
            if (AvailablePrinters == null || AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
            {
                _dialogService.ShowWarning("선택된 프린터가 없습니다. 프린터를 선택해 주세요.", "인쇄 알림");
                return false;
            }

            int cols = Math.Max(1, LabelColumns);
            int rows = Math.Max(1, LabelRows);
            int slotsPerSheet = cols * rows;
            int effCopies = Math.Max(1, copiesPerRecord);
            double slotW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80.0;
            double slotH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60.0;
            double totalW = GetTotalSheetWidthMm();
            double totalH = GetTotalSheetHeightMm();

            var expanded = new List<PrintRecordModel>();
            foreach (var rec in targetRecords)
            {
                for (int c = 0; c < effCopies; c++) expanded.Add(rec);
            }

            bool allOk = true;
            for (int i = 0; i < expanded.Count; i += slotsPerSheet)
            {
                var batch = expanded.Skip(i).Take(slotsPerSheet).ToList();
                var slotBitmaps = new List<Bitmap>();
                try
                {
                    foreach (var rec in batch)
                    {
                        _currentPrintingRecord = rec;
                        slotBitmaps.Add(GenerateLabelBitmapForPrint(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8));
                    }
                    using (var sheetBmp = ComposeMultiLabelSheetBitmap(slotBitmaps, slotW, slotH, totalW, totalH))
                    {
                        bool ok = SendSheetBitmapToTsplPrinter(sheetBmp, totalW, totalH, 1);
                        foreach (var rec in batch) rec.Status = ok ? "인쇄 완료" : "인쇄 실패";
                        if (!ok) allOk = false;
                    }
                }
                finally
                {
                    _currentPrintingRecord = null;
                    foreach (var b in slotBitmaps) b?.Dispose();
                }
            }
            return allOk;
        }

        public void ExecutePrintRange()
        {
            if (Records == null || Records.Count == 0)
            {
                _dialogService.ShowWarning("인쇄할 기록 데이터가 없습니다.", "알림");
                return;
            }

            int start = Math.Max(1, PrintRangeStart);
            int end = Math.Min(Records.Count, PrintRangeEnd);

            if (start > end)
            {
                _dialogService.ShowWarning($"시작 번호({start})가 종료 번호({end})보다 큽니다. 범위를 올바르게 입력해 주세요.", "범위 오류");
                return;
            }

            var targets = Records.Where(r => r.Index >= start && r.Index <= end).ToList();
            if (targets.Count == 0)
            {
                _dialogService.ShowWarning($"지정한 구간 (No.{start} ~ No.{end})에 해당하는 레코드가 없습니다.", "알림");
                return;
            }

            bool allSuccess = true;
            int successCount = 0;
            int totalCopiesPrinted = 0;
            if (IsMultiLabelLayout)
            {
                allSuccess = PrintRecordsMultiColumn(targets, ExcelPrintCopies);
                successCount = targets.Count(r => r.Status == "인쇄 완료");
                totalCopiesPrinted = successCount * ExcelPrintCopies;
            }
            else
            {
                try
                {
                    foreach (var rec in targets)
                    {
                        _currentPrintingRecord = rec;
                        bool success = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                        rec.Status = success ? "인쇄 완료" : "인쇄 실패";
                        if (success)
                        {
                            successCount++;
                            totalCopiesPrinted += ExcelPrintCopies;
                        }
                        else allSuccess = false;
                    }
                }
                finally
                {
                    _currentPrintingRecord = null;
                }
            }

            SaveHistoryRecords();
            StatusMessage = allSuccess
                ? $"구간 인쇄 완료: No.{start} ~ No.{end} (총 {targets.Count}건 전체 성공, 매수: {ExcelPrintCopies})"
                : $"구간 인쇄 완료 (일부 실패): No.{start} ~ No.{end} (성공 {successCount}/{targets.Count})";
        }

        public void ExecutePrintAndRecord()
        {
            int count = Math.Max(1, Math.Min(999, CopyCount));
            bool printSuccess = false;

            for (int i = 0; i < count; i++)
            {
                printSuccess = SendToTsplPrinter(InputCol1, InputCol2, InputCol3, InputCol4, InputCol5, InputCol6, InputCol7, InputCol8, SinglePrintCopies);
            }

            string statusText = printSuccess ? "인쇄 완료" : "인쇄 실패";
            string currentTimestamp = IsDateTimeEnabled ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "";

            for (int i = 0; i < count; i++)
            {
                _recordCounter++;
                var rec = new PrintRecordModel
                {
                    Index = _recordCounter,
                    Timestamp = currentTimestamp,
                    Col1 = InputCol1,
                    Col2 = InputCol2,
                    Col3 = InputCol3,
                    Col4 = InputCol4,
                    Col5 = InputCol5,
                    Col6 = InputCol6,
                    Col7 = InputCol7,
                    Col8 = InputCol8,
                    Status = statusText,
                    IsChecked = false
                };
                Records.Add(rec);
                SelectedRecord = rec;
            }

            SaveHistoryRecords();
            ScrollToLatestRecordRequested?.Invoke();

            StatusMessage = printSuccess 
                ? $"인쇄 및 기록 완료: 총 {count}건 x {SinglePrintCopies}매 (No.{_recordCounter - count + 1}~No.{_recordCounter})" 
                : $"인쇄 실패 (기록은 추가됨): {SelectedPrinterName}";

            ClearInputs();
            FocusFirstInputRequested?.Invoke();
        }

        public void ExecutePrintPages(List<int> pageIndices, int copies)
        {
            if (CurrentTemplate == null || pageIndices == null || pageIndices.Count == 0) return;

            int effCopies = Math.Max(1, Math.Min(999, copies));
            SyncPagesToTemplate();
            var savedElements = CurrentTemplate.Elements;
            int successPages = 0;

            if (IsMultiLabelLayout)
            {
                int cols = Math.Max(1, LabelColumns);
                int rows = Math.Max(1, LabelRows);
                int slotsPerSheet = cols * rows;
                double slotW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80.0;
                double slotH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60.0;
                double totalW = GetTotalSheetWidthMm();
                double totalH = GetTotalSheetHeightMm();

                var expanded = new List<int>();
                foreach (int p in pageIndices)
                {
                    if (p < 0 || p >= TotalPageCount) continue;
                    for (int c = 0; c < effCopies; c++) expanded.Add(p);
                }

                try
                {
                    for (int i = 0; i < expanded.Count; i += slotsPerSheet)
                    {
                        var batch = expanded.Skip(i).Take(slotsPerSheet).ToList();
                        var slotBitmaps = new List<Bitmap>();
                        try
                        {
                            foreach (int p in batch)
                            {
                                var pageElems = GetPageElements(p);
                                CurrentTemplate.Elements = pageElems;
                                string pc1 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 1)?.Content ?? "";
                                string pc2 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 2)?.Content ?? "";
                                string pc3 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 3)?.Content ?? "";
                                string pc4 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 4)?.Content ?? "";
                                string pc5 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 5)?.Content ?? "";
                                string pc6 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 6)?.Content ?? "";
                                string pc7 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 7)?.Content ?? "";
                                string pc8 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 8)?.Content ?? "";
                                slotBitmaps.Add(GenerateLabelBitmapForPrint(pc1, pc2, pc3, pc4, pc5, pc6, pc7, pc8));
                            }

                            using (var sheetBmp = ComposeMultiLabelSheetBitmap(slotBitmaps, slotW, slotH, totalW, totalH))
                            {
                                if (SendSheetBitmapToTsplPrinter(sheetBmp, totalW, totalH, 1))
                                {
                                    successPages += batch.Count;
                                }
                            }
                        }
                        finally
                        {
                            foreach (var b in slotBitmaps) b?.Dispose();
                        }
                    }
                }
                finally
                {
                    CurrentTemplate.Elements = savedElements;
                }

                StatusMessage = $"다중 배열 페이지 인쇄 완료: {pageIndices.Count}페이지 × {effCopies}매";
                return;
            }

            try
            {
                foreach (int p in pageIndices)
                {
                    if (p < 0 || p >= TotalPageCount) continue;
                    var pageElems = GetPageElements(p);
                    CurrentTemplate.Elements = pageElems;

                    string pc1 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 1)?.Content ?? "";
                    string pc2 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 2)?.Content ?? "";
                    string pc3 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 3)?.Content ?? "";
                    string pc4 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 4)?.Content ?? "";
                    string pc5 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 5)?.Content ?? "";
                    string pc6 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 6)?.Content ?? "";
                    string pc7 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 7)?.Content ?? "";
                    string pc8 = pageElems?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 8)?.Content ?? "";

                    if (SendToTsplPrinter(pc1, pc2, pc3, pc4, pc5, pc6, pc7, pc8, effCopies))
                    {
                        successPages++;
                    }
                }
            }
            finally
            {
                CurrentTemplate.Elements = savedElements;
            }

            StatusMessage = successPages == pageIndices.Count
                ? $"다중 페이지 인쇄 완료: {pageIndices.Count}페이지 × {effCopies}매"
                : $"다중 페이지 인쇄: {pageIndices.Count}페이지 중 {successPages}페이지 성공";
        }

        public void ExecutePrintOnly()
        {
            int copies = Math.Max(1, Math.Min(999, SinglePrintCopies));
            int pageCount = TotalPageCount;

            if (pageCount > 1 && CurrentTemplate != null)
            {
                var allPages = Enumerable.Range(0, pageCount).ToList();
                ExecutePrintPages(allPages, copies);
                return;
            }

            string c1 = !string.IsNullOrEmpty(InputCol1) ? InputCol1 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 1)?.Content ?? "");
            string c2 = !string.IsNullOrEmpty(InputCol2) ? InputCol2 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 2)?.Content ?? "");
            string c3 = !string.IsNullOrEmpty(InputCol3) ? InputCol3 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 3)?.Content ?? "");
            string c4 = !string.IsNullOrEmpty(InputCol4) ? InputCol4 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 4)?.Content ?? "");
            string c5 = !string.IsNullOrEmpty(InputCol5) ? InputCol5 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 5)?.Content ?? "");
            string c6 = !string.IsNullOrEmpty(InputCol6) ? InputCol6 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 6)?.Content ?? "");
            string c7 = !string.IsNullOrEmpty(InputCol7) ? InputCol7 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 7)?.Content ?? "");
            string c8 = !string.IsNullOrEmpty(InputCol8) ? InputCol8 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 8)?.Content ?? "");

            bool printSuccess = SendToTsplPrinter(c1, c2, c3, c4, c5, c6, c7, c8, copies);

            StatusMessage = printSuccess ? $"단건 인쇄 완료: {copies}매" : $"인쇄 실패: {SelectedPrinterName}";
            ClearInputs();
            FocusFirstInputRequested?.Invoke();
        }

        public void ExecuteTestPrint(int? overrideWidthMm = null, int? overrideHeightMm = null)
        {
            if (overrideWidthMm.HasValue && overrideHeightMm.HasValue)
            {
                if (AvailablePrinters == null || AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
                {
                    _dialogService.ShowWarning("선택된 프린터가 없습니다. 프린터를 선택해 주세요.", "인쇄 알림");
                    return;
                }

                int wMm = Math.Max(1, overrideWidthMm.Value);
                int hMm = Math.Max(1, overrideHeightMm.Value);
                BarcodePrint.Services.PrinterService.SyncGlobalPrinterDpi(SelectedPrinterName);

                bool hasTemplateContent = CurrentTemplate != null &&
                    ((CurrentTemplate.Elements != null && CurrentTemplate.Elements.Count > 0) || ActiveColumnCount > 0);

                bool printOk;
                if (hasTemplateContent)
                {
                    string tc1 = !string.IsNullOrWhiteSpace(InputCol1) ? InputCol1 : (Column1Header + " (TEST)");
                    string tc2 = !string.IsNullOrWhiteSpace(InputCol2) ? InputCol2 : (Column2Header + " (TEST)");
                    string tc3 = !string.IsNullOrWhiteSpace(InputCol3) ? InputCol3 : (Column3Header + " (TEST)");
                    string tc4 = !string.IsNullOrWhiteSpace(InputCol4) ? InputCol4 : (Column4Header + " (TEST)");
                    string tc5 = !string.IsNullOrWhiteSpace(InputCol5) ? InputCol5 : (Column5Header + " (TEST)");
                    string tc6 = !string.IsNullOrWhiteSpace(InputCol6) ? InputCol6 : (Column6Header + " (TEST)");
                    string tc7 = !string.IsNullOrWhiteSpace(InputCol7) ? InputCol7 : (Column7Header + " (TEST)");
                    string tc8 = !string.IsNullOrWhiteSpace(InputCol8) ? InputCol8 : (Column8Header + " (TEST)");

                    if (IsColumn1Barcode && (string.IsNullOrWhiteSpace(InputCol1) || InputCol1.Contains("TEST"))) tc1 = "1234567890";
                    if (IsColumn2Barcode && (string.IsNullOrWhiteSpace(InputCol2) || InputCol2.Contains("TEST"))) tc2 = "1234567890";
                    if (IsColumn3Barcode && (string.IsNullOrWhiteSpace(InputCol3) || InputCol3.Contains("TEST"))) tc3 = "1234567890";
                    if (IsColumn4Barcode && (string.IsNullOrWhiteSpace(InputCol4) || InputCol4.Contains("TEST"))) tc4 = "1234567890";
                    if (IsColumn5Barcode && (string.IsNullOrWhiteSpace(InputCol5) || InputCol5.Contains("TEST"))) tc5 = "1234567890";
                    if (IsColumn6Barcode && (string.IsNullOrWhiteSpace(InputCol6) || InputCol6.Contains("TEST"))) tc6 = "1234567890";
                    if (IsColumn7Barcode && (string.IsNullOrWhiteSpace(InputCol7) || InputCol7.Contains("TEST"))) tc7 = "1234567890";
                    if (IsColumn8Barcode && (string.IsNullOrWhiteSpace(InputCol8) || InputCol8.Contains("TEST"))) tc8 = "1234567890";

                    using (var labelBmp = GenerateLabelBitmapForPrint(tc1, tc2, tc3, tc4, tc5, tc6, tc7, tc8))
                    {
                        printOk = SendSheetBitmapToTsplPrinter(labelBmp, wMm, hMm, 1);
                    }
                }
                else
                {
                    using (var testBmp = GenerateTestPrintBitmap(wMm, hMm))
                    {
                        printOk = SendSheetBitmapToTsplPrinter(testBmp, wMm, hMm, 1);
                    }
                }

                StatusMessage = printOk ? $"테스트 라벨 1장 인쇄 완료 ({wMm}x{hMm}mm)" : $"테스트 인쇄 실패: {SelectedPrinterName}";
                return;
            }

            string c1 = !string.IsNullOrWhiteSpace(InputCol1) ? InputCol1 : (Column1Header + " (TEST)");
            string c2 = !string.IsNullOrWhiteSpace(InputCol2) ? InputCol2 : (Column2Header + " (TEST)");
            string c3 = !string.IsNullOrWhiteSpace(InputCol3) ? InputCol3 : (Column3Header + " (TEST)");
            string c4 = !string.IsNullOrWhiteSpace(InputCol4) ? InputCol4 : (Column4Header + " (TEST)");
            string c5 = !string.IsNullOrWhiteSpace(InputCol5) ? InputCol5 : (Column5Header + " (TEST)");
            string c6 = !string.IsNullOrWhiteSpace(InputCol6) ? InputCol6 : (Column6Header + " (TEST)");
            string c7 = !string.IsNullOrWhiteSpace(InputCol7) ? InputCol7 : (Column7Header + " (TEST)");
            string c8 = !string.IsNullOrWhiteSpace(InputCol8) ? InputCol8 : (Column8Header + " (TEST)");

            if (IsColumn1Barcode && (string.IsNullOrWhiteSpace(InputCol1) || InputCol1.Contains("TEST"))) c1 = "1234567890";
            if (IsColumn2Barcode && (string.IsNullOrWhiteSpace(InputCol2) || InputCol2.Contains("TEST"))) c2 = "1234567890";
            if (IsColumn3Barcode && (string.IsNullOrWhiteSpace(InputCol3) || InputCol3.Contains("TEST"))) c3 = "1234567890";
            if (IsColumn4Barcode && (string.IsNullOrWhiteSpace(InputCol4) || InputCol4.Contains("TEST"))) c4 = "1234567890";
            if (IsColumn5Barcode && (string.IsNullOrWhiteSpace(InputCol5) || InputCol5.Contains("TEST"))) c5 = "1234567890";
            if (IsColumn6Barcode && (string.IsNullOrWhiteSpace(InputCol6) || InputCol6.Contains("TEST"))) c6 = "1234567890";
            if (IsColumn7Barcode && (string.IsNullOrWhiteSpace(InputCol7) || InputCol7.Contains("TEST"))) c7 = "1234567890";
            if (IsColumn8Barcode && (string.IsNullOrWhiteSpace(InputCol8) || InputCol8.Contains("TEST"))) c8 = "1234567890";

            bool printSuccess = SendToTsplPrinter(c1, c2, c3, c4, c5, c6, c7, c8, 1);
            StatusMessage = printSuccess ? "테스트 라벨 1장 인쇄 완료" : $"테스트 인쇄 실패: {SelectedPrinterName}";
        }

        private Bitmap GenerateTestPrintBitmap(int widthMm, int heightMm)
        {
            double printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? (double)PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203.0;
            double dotsPerMm = printerDpi / 25.4;
            int pxW = Math.Max(32, (int)Math.Round(widthMm * dotsPerMm));
            int pxH = Math.Max(32, (int)Math.Round(heightMm * dotsPerMm));

            var bmp = new Bitmap(pxW, pxH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.White);
                int margin = Math.Max(4, (int)Math.Round(2.0 * dotsPerMm));
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, Math.Max(2f, (float)(0.4 * dotsPerMm))))
                {
                    if (pxW > margin * 2 && pxH > margin * 2)
                    {
                        g.DrawRectangle(pen, margin, margin, pxW - margin * 2, pxH - margin * 2);
                    }
                }

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                float titleFontPx = Math.Max(14f, Math.Min(pxH * 0.16f, pxW * 0.085f));
                float subFontPx = Math.Max(11f, Math.Min(pxH * 0.085f, pxW * 0.042f));
                using (var titleFont = new System.Drawing.Font("Malgun Gothic", titleFontPx, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                using (var subFont = new System.Drawing.Font("Malgun Gothic", subFontPx, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                using (var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center, Trimming = System.Drawing.StringTrimming.EllipsisCharacter })
                {
                    string title = "LabelMaster TEST";
                    string detail = $"{widthMm} x {heightMm} mm | S:{PrintSpeed} D:{PrintDarkness} | {PaperType}";
                    float innerLeft = margin + 4;
                    float innerWidth = Math.Max(16f, pxW - (margin + 4) * 2);
                    g.DrawString(title, titleFont, System.Drawing.Brushes.Black, new RectangleF(innerLeft, pxH * 0.16f, innerWidth, pxH * 0.36f), sf);
                    g.DrawString(detail, subFont, System.Drawing.Brushes.Black, new RectangleF(innerLeft, pxH * 0.52f, innerWidth, pxH * 0.32f), sf);
                }
            }
            return bmp;
        }

        public void ExecuteRecordOnly()
        {
            if (IsAllInputsEmpty())
            {
                _dialogService.ShowMessage("기록할 데이터를 입력해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            int count = Math.Max(1, Math.Min(999, CopyCount));
            string currentTimestamp = IsDateTimeEnabled ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "";

            for (int i = 0; i < count; i++)
            {
                _recordCounter++;
                var rec = new PrintRecordModel
                {
                    Index = _recordCounter,
                    Timestamp = currentTimestamp,
                    Col1 = InputCol1,
                    Col2 = InputCol2,
                    Col3 = InputCol3,
                    Col4 = InputCol4,
                    Col5 = InputCol5,
                    Col6 = InputCol6,
                    Col7 = InputCol7,
                    Col8 = InputCol8,
                    Status = "",
                    IsChecked = false
                };
                Records.Add(rec);
                SelectedRecord = rec;
            }

            SaveHistoryRecords();
            ScrollToLatestRecordRequested?.Invoke();
            StatusMessage = $"기록 저장 완료 (총 {count}건): No.{_recordCounter - count + 1}~No.{_recordCounter}";

            ClearInputs();
            FocusFirstInputRequested?.Invoke();
        }

        public bool PrintSingleExcelRecord(PrintRecordModel rec, int copies)
        {
            if (rec == null) return false;
            try
            {
                _currentPrintingRecord = rec;
                return SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, copies);
            }
            finally
            {
                _currentPrintingRecord = null;
            }
        }

        public void ExecutePrintAllRecords()
        {
            if (Records == null || Records.Count == 0)
            {
                _dialogService.ShowMessage("일괄 인쇄할 기록이 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            if (!_dialogService.ShowQuestion($"목록에 있는 전체 {Records.Count}건의 라벨을 일괄 인쇄하시겠습니까?", "전체 인쇄 확인"))
            {
                return;
            }

            int successCount = 0;
            int totalCopiesPrinted = 0;
            if (IsMultiLabelLayout)
            {
                PrintRecordsMultiColumn(Records.ToList(), ExcelPrintCopies);
                successCount = Records.Count(r => r.Status == "인쇄 완료");
                totalCopiesPrinted = successCount * ExcelPrintCopies;
            }
            else
            {
                try
                {
                    foreach (var rec in Records)
                    {
                        _currentPrintingRecord = rec;
                        bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                        if (ok)
                        {
                            rec.Status = "인쇄 완료";
                            successCount++;
                            totalCopiesPrinted += ExcelPrintCopies;
                        }
                        else
                        {
                            rec.Status = "인쇄 실패";
                        }
                    }
                }
                finally
                {
                    _currentPrintingRecord = null;
                }
            }

            SaveHistoryRecords();
            StatusMessage = $"전체 인쇄 완료: 총 {Records.Count}건 중 {successCount}건 성공 (매수: {ExcelPrintCopies})";
        }

        public void ExecutePrintUnprintedRecords()
        {
            var unprinted = Records.Where(r => r.Status != "인쇄 완료").ToList();
            if (unprinted.Count == 0)
            {
                _dialogService.ShowMessage("미인쇄 상태인 데이터가 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            if (!_dialogService.ShowQuestion($"미인쇄 항목 총 {unprinted.Count}건을 인쇄하시겠습니까?", "미인쇄 출력 확인"))
            {
                return;
            }

            int successCount = 0;
            int totalCopiesPrinted = 0;
            if (IsMultiLabelLayout)
            {
                PrintRecordsMultiColumn(unprinted, ExcelPrintCopies);
                successCount = unprinted.Count(r => r.Status == "인쇄 완료");
                totalCopiesPrinted = successCount * ExcelPrintCopies;
            }
            else
            {
                try
                {
                    foreach (var rec in unprinted)
                    {
                        _currentPrintingRecord = rec;
                        bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                        if (ok)
                        {
                            rec.Status = "인쇄 완료";
                            successCount++;
                            totalCopiesPrinted += ExcelPrintCopies;
                        }
                        else
                        {
                            rec.Status = "인쇄 실패";
                        }
                    }
                }
                finally
                {
                    _currentPrintingRecord = null;
                }
            }

            SaveHistoryRecords();
            StatusMessage = $"미인쇄 출력 완료: 총 {unprinted.Count}건 중 {successCount}건 성공 (매수: {ExcelPrintCopies})";
        }

        public void ExecuteReprintSelectedRecords(object parameter = null)
        {
            var targets = new List<PrintRecordModel>();

            var checkedRecords = Records.Where(r => r.IsChecked).ToList();
            if (checkedRecords.Count > 0)
            {
                targets.AddRange(checkedRecords);
            }
            else if (parameter is System.Collections.IList list && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item is PrintRecordModel rec)
                    {
                        targets.Add(rec);
                    }
                }
            }
            else if (parameter is PrintRecordModel singleTarget)
            {
                targets.Add(singleTarget);
            }
            else if (SelectedRecord != null)
            {
                targets.Add(SelectedRecord);
            }

            if (targets.Count == 0)
            {
                _dialogService.ShowMessage("출력할 데이터를 선택해 주세요. (체크박스 선택 또는 행 클릭)", "알림", MessageBoxImage.Information);
                return;
            }

            int successCount = 0;
            int totalCopiesPrinted = 0;
            try
            {
                foreach (var rec in targets)
                {
                    _currentPrintingRecord = rec;
                    bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                    if (ok)
                    {
                        rec.Status = "인쇄 완료";
                        successCount++;
                        totalCopiesPrinted += ExcelPrintCopies;
                    }
                    else
                    {
                        rec.Status = "인쇄 실패";
                    }
                }
            }
            finally
            {
                _currentPrintingRecord = null;
            }

            SaveHistoryRecords();
            StatusMessage = $"선택출력 완료: 총 {targets.Count}건 중 {successCount}건 성공 (매수: {ExcelPrintCopies})";
        }
    }
}
