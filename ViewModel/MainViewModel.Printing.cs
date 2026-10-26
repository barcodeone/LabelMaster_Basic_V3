using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public void RefreshPrinters()
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
                    if (filtered.Count == 0)
                    {
                        var freshPrinters = _printerService.GetVerifiedPrinters(_authenticatedSerials);
                        if (freshPrinters != null && freshPrinters.Count > 0)
                        {
                            _cachedPrinters = freshPrinters;
                            filtered = _cachedPrinters.Where(p => p.Type.Equals("USB", StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                    }
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

                    string cleanCurrent = getCleanName(p.Name);
                    if (!AvailablePrinters.Any(existing => getCleanName(existing).Equals(cleanCurrent, StringComparison.OrdinalIgnoreCase)))
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
                try
                {
                    // 캐시된 프린터 정보에서 시리얼 검증
                    var info = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
                    string cachedSerial = info?.Serial;
                    bool isAuthorized = ValidatePrinterAuthorization(cachedSerial);
                    var licenseMode = TrialLicenseManager.EvaluateLicense(isAuthorized, cachedSerial, printerName);

                    _printerService.CheckRealtimePrinterStatus(printerName, _cachedPrinters, out string badge, out string color);

                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        UpdateTrialLicenseState();

                        if (isAuthorized)
                        {
                            IsCurrentPrinterAuthorized = true;
                        }
                        else
                        {
                            // 체험판 모드: 1회 30장 제한으로 인쇄 허용
                            IsCurrentPrinterAuthorized = true;
                        }

                        // 프린터 상태 뱃지에는 항상 실제 프린터 하드웨어 상태(정상 준비, 오프라인, 용지 없음 등)를 표시
                        // (체험판 정보는 좌측 상단 전용 뱃지 1개로 표시)
                        PrinterStatusBadge = badge;
                        PrinterStatusColor = color;

                        // UI 바인딩 강제 갱신
                        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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

        private void DrawTextOnDc(DrawingContext dc, string text, double fontSizeMm, double offsetXMm, double offsetYMm, double dotsPerMm, System.Windows.Media.Brush brush, string fontName = "맑은 고딕", bool isBold = true, bool isItalic = false, double cellWidthMm = 0, string align = "LEFT", double cellHeightMm = 0, double cellPaddingMm = 0, bool isUnderline = false, double fontScaleX = 1.0, double lineSpacing = 1.0, double letterSpacing = 0.0)
        {
            if (string.IsNullOrEmpty(text)) return;

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

            // 장평(가로 비율) 변환 적용
            bool pushedScale = false;
            if (Math.Abs(effScaleX - 1.0) > 0.001)
            {
                dc.PushTransform(new ScaleTransform(effScaleX, 1.0, leftPx, topPx));
                pushedScale = true;
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

            if (cellWidthMm > 0)
            {
                // 셀 또는 박스 너비가 지정된 경우: 내부 패딩(cellPaddingMm)을 정확히 제외한 순수 콘텐츠 너비로 줄바꿈 폭 지정
                double cellWidthPx = (cellWidthMm * dotsPerMm) / effScaleX;
                double padPx = (cellPaddingMm * dotsPerMm) / effScaleX;
                double contentWidthPx = Math.Max(1.0, cellWidthPx - (padPx * 2.0));
                ft.MaxTextWidth = contentWidthPx;

                if (align == "CENTER")
                {
                    leftPx += Math.Max(0, (cellWidthPx - ft.Width) / 2.0);
                }
                else if (align == "RIGHT")
                {
                    leftPx += Math.Max(padPx, cellWidthPx - ft.Width - padPx);
                }
                else
                {
                    leftPx += padPx;
                }
            }

            if (cellHeightMm > 0)
            {
                double cellHeightPx = cellHeightMm * dotsPerMm;
                topPx += Math.Max(0, (cellHeightPx - ft.Height) / 2.0);
            }

            if (Math.Abs(letterSpacing) > 0.001)
            {
                // 자간(em 단위) 반영 렌더링
                string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                double currentY = topPx;
                double lineHeightPx = (effLineSpacing > 0) ? (Math.Max(6, targetPxSize) * effLineSpacing * 1.2) : (targetPxSize * 1.2);
                double extraCharSpacingPx = targetPxSize * letterSpacing;

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
                        if (align == "CENTER")
                        {
                            lineStartXPx = leftPx + Math.Max(0, (cellWPx - totalLineWidthPx) / 2.0);
                        }
                        else if (align == "RIGHT")
                        {
                            lineStartXPx = leftPx + Math.Max(0, cellWPx - totalLineWidthPx - ((cellPaddingMm * dotsPerMm) / effScaleX));
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
                dc.DrawText(ft, new System.Windows.Point(leftPx, topPx));
            }

            if (pushedScale)
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
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, pxW, pxH));

                // 전체 이미지 인쇄 위치 보정 오프셋 (PrinterOffsetX, PrinterOffsetY) 적용
                bool hasOffset = (PrinterOffsetX != 0 || PrinterOffsetY != 0);
                if (hasOffset)
                {
                    dc.PushTransform(new TranslateTransform(PrinterOffsetX * dotsPerMm, PrinterOffsetY * dotsPerMm));
                }

                var colValues = new string[] { col1, col2, col3, col4, col5, col6, col7, col8 };
                int activeCount = Math.Max(1, Math.Min(8, ActiveColumnCount));

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
                    if (GetBarcodeByIndex(colNum)) barcodeColIndices.Add(colNum);
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
                for (int r = 0; r < textRowCount; r++)
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

                if (autoCount > 0)
                {
                    double remainingPx = Math.Max(autoCount * 4.0 * dotsPerMm, tableHPx - userSpecifiedTotalPx);
                    double totalWeight = 0;
                    double[] weights = new double[textRowCount];
                    for (int r = 0; r < textRowCount; r++)
                    {
                        int colNum = textColIndices[r];
                        if (GetRowHeightByIndex(colNum) <= 0)
                        {
                            int sz = GetSizeByIndex(colNum);
                            weights[r] = Math.Max(6, sz);
                            totalWeight += weights[r];
                        }
                    }
                    for (int r = 0; r < textRowCount; r++)
                    {
                        int colNum = textColIndices[r];
                        if (GetRowHeightByIndex(colNum) <= 0)
                        {
                            rowHeightsPx[r] = remainingPx * (weights[r] / totalWeight);
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
                        for (int c = 0; c < colCount; c++)
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
                        BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText, textPos, wMm, hMm);
                    }
                }
                else
                {
                    // 표 비활성화 모드 (자유 레이아웃)
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
                            var elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                            double wMm = (elem != null && elem.Width > 0) ? elem.Width : GetWidthByIndex(colNum);
                            double hMm = (elem != null && elem.Height > 0) ? elem.Height : 0;
                            string textPos = (elem != null && !string.IsNullOrEmpty(elem.BarcodeTextPosition)) ? elem.BarcodeTextPosition : (showBText ? "BELOW" : "NONE");
                            BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText, textPos, wMm, hMm);
                        }
                        else
                        {
                            string displayText = val;
                            if (isPrice && !string.IsNullOrWhiteSpace(val))
                            {
                                displayText = PriceFormatHelper.FormatPrice(val, true);
                            }
                            if (isIncludeHeader && !string.IsNullOrWhiteSpace(header))
                            {
                                displayText = $"{header}: {displayText}";
                            }

                            if (!string.IsNullOrEmpty(displayText))
                            {
                                string align = GetAlignByIndex(colNum);
                                int userWMm = GetWidthByIndex(colNum);
                                DrawTextOnDc(dc, displayText, size, offX, offY, dotsPerMm, System.Windows.Media.Brushes.Black, fontName, isBold, isItalic, userWMm, align, 0, 0);
                            }
                        }
                    }
                }

                if (IsDateTimeEnabled)
                {
                    string currentDtStr = GetFormattedDateTimeString();
                    DrawTextOnDc(dc, currentDtStr, DateTimeSize, DateTimeOffsetX, DateTimeOffsetY, dotsPerMm, System.Windows.Media.Brushes.Black, DateTimeFont, DateTimeBold, DateTimeItalic, DateTimeWidth, "LEFT", 0, 0);
                }

                if (IsSerialEnabled)
                {
                    string serialStr = FormattedSerialPreview;
                    DrawTextOnDc(dc, serialStr, SerialSize, SerialOffsetX, SerialOffsetY, dotsPerMm, System.Windows.Media.Brushes.Black, SerialFont, SerialBold, SerialItalic, SerialWidth, SerialAlign, 0, 0);
                }

                // 자유 배치 커스텀 요소 인쇄 렌더링 (선, 도형, 추가 텍스트/바코드 등)
                RenderCustomElementsOnDc(dc, dotsPerMm);

                if (hasOffset)
                {
                    dc.Pop();
                }
            }

            var rtb = new RenderTargetBitmap(pxW, pxH, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);

            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(ms);
                ms.Position = 0;
                var gdiBitmap = new Bitmap(ms);

                switch (LabelRotationAngle)
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
                return new Bitmap(gdiBitmap);
            }
        }

        private void RenderCustomElementsOnDc(DrawingContext dc, double dotsPerMm)
        {
            if (CurrentTemplate == null || CurrentTemplate.Elements == null) return;

            foreach (var elem in CurrentTemplate.Elements)
            {
                if (elem.IsVariable && elem.InputOrder <= 8 && elem.InputOrder <= ActiveColumnCount) continue;
                if (elem.Id == "TITLE_ELEMENT" || elem.Id == "DATETIME_ELEMENT") continue;

                double leftPx = elem.X * dotsPerMm;
                double topPx = elem.Y * dotsPerMm;
                double widthPx = elem.Width * dotsPerMm;
                double heightPx = elem.Height * dotsPerMm;

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
                        if (!string.IsNullOrWhiteSpace(barcodeContent))
                        {
                            BarcodeVisualHelper.RenderBarcodeOnDc(dc, barcodeContent, elem.BarcodeType, (int)Math.Round(elem.FontSize), (int)Math.Round(elem.X), (int)Math.Round(elem.Y), dotsPerMm, elem.ShowBarcodeText, elem.BarcodeTextPosition, elem.Width, elem.Height);
                        }
                        break;

                    case ElementType.TEXT:
                    case ElementType.DATE_TIME:
                        string textContent = GetElementEffectiveContent(elem);
                        if (!string.IsNullOrWhiteSpace(textContent))
                        {
                            DrawTextOnDc(dc, textContent, elem.FontSize, elem.X, elem.Y, dotsPerMm, System.Windows.Media.Brushes.Black, elem.FontName, elem.IsBold, elem.IsItalic, elem.Width, elem.Alignment, 0, 0, elem.IsUnderline, elem.FontScaleX, elem.LineSpacing, elem.LetterSpacing);
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

            bool pushedRotate = false;
            if (elem.Rotation != 0)
            {
                dc.PushTransform(new RotateTransform(elem.Rotation, x + w / 2.0, y + h / 2.0));
                pushedRotate = true;
            }

            try
            {
                // 3. 헤더 행 음영 (IsFilled 시)
                if (elem.IsFilled && rows > 0)
                {
                    var headerRect = new Rect(x, y, w, Math.Max(1.0, rowY[1] - rowY[0]));
                    dc.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)), null, headerRect);
                }

                // 4. 표 외곽선 및 격자선 그리기
                double strokePx = Math.Max(1.0, elem.LineWidth * (dotsPerMm / (96.0 / 25.4)));
                var gridPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, strokePx);

                // 외곽 테두리
                dc.DrawRectangle(null, gridPen, new Rect(x, y, w, h));

                // 내부 세로 격자선
                for (int c = 1; c < cols; c++)
                {
                    dc.DrawLine(gridPen, new System.Windows.Point(colX[c], y), new System.Windows.Point(colX[c], y + h));
                }

                // 내부 가로 격자선
                for (int r = 1; r < rows; r++)
                {
                    dc.DrawLine(gridPen, new System.Windows.Point(x, rowY[r]), new System.Windows.Point(x + w, rowY[r]));
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
            finally
            {
                if (pushedRotate)
                {
                    dc.Pop();
                }
            }
        }

        private bool SendToTsplPrinter(string col1, string col2, string col3, string col4, string col5, string col6, string col7, string col8, int copies = 1)
        {
            try
            {
                if (AvailablePrinters == null || AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
                {
                    _dialogService.ShowWarning("선택된 프린터가 없습니다. 프린터를 선택해 주세요.", "인쇄 알림");
                    return false;
                }

                int effectiveCopies = Math.Max(1, copies);

                using (var labelBmp = GenerateLabelBitmapForPrint(col1, col2, col3, col4, col5, col6, col7, col8))
                {
                    // 물리 롤 용지 규격 (SIZE 명령어)은 장착된 롤 크기(PaperWidth, PaperHeight) 그대로 유지
                    PRINT_INFO_CLASS.PrinterInfo.Paper_Width = PaperWidth;
                    PRINT_INFO_CLASS.PrinterInfo.Paper_Height = PaperHeight;
                    PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed = PrintSpeed;
                    PRINT_INFO_CLASS.PrinterInfo.nDarkness = PrintDarkness;
                    PRINT_INFO_CLASS.PrinterInfo.nGAP = GapHeight;
                    PRINT_INFO_CLASS.PrinterInfo.bRibbon = IsRibbon;
                    // 소프트웨어 비트맵 레벨에서 이미 PrinterOffsetX, PrinterOffsetY가 완벽하게 시프트되었으므로 하드웨어 오프셋은 0 설정
                    PRINT_INFO_CLASS.PrinterInfo.offset_X = 0;
                    PRINT_INFO_CLASS.PrinterInfo.offset_Y = 0;
                    PRINT_INFO_CLASS.PrinterInfo.bPaperRotated = IsRotated;
                    PRINT_INFO_CLASS.PrinterInfo.PaperType = PaperType == "BLINE" ? PAPER_TYPE.BLINE : (PaperType == "CONTINUOUS" ? PAPER_TYPE.CONTINEUS : PAPER_TYPE.GAP);

                    // 인쇄 후 동작 (PostPrintAction) 및 오프셋(PostPrintOffset) 후처리 명령어 준비
                    string extraCmdsStr = null;
                    if (PostPrintAction != "NONE" || PostPrintOffset != 0)
                    {
                        var extraCmds = new StringBuilder();
                        if (PostPrintOffset != 0)
                        {
                            extraCmds.Append($"OFFSET {PostPrintOffset} mm\r\n");
                        }

                        switch (PostPrintAction)
                        {
                            case "CUTTER":
                                extraCmds.Append("SET PEEL OFF\r\n");
                                extraCmds.Append("SET TEAR OFF\r\n");
                                switch (PostPrintCutTrigger)
                                {
                                    case "EVERY_PAGE":
                                        extraCmds.Append("SET CUTTER 1\r\n");
                                        break;
                                    case "AFTER_COPIES":
                                        extraCmds.Append($"SET CUTTER {effectiveCopies}\r\n");
                                        break;
                                    case "AFTER_JOB":
                                        extraCmds.Append("SET CUTTER BATCH\r\n");
                                        break;
                                    case "CUSTOM_INTERVAL":
                                        extraCmds.Append($"SET CUTTER {Math.Max(1, PostPrintCutInterval)}\r\n");
                                        break;
                                    default:
                                        extraCmds.Append("SET CUTTER 1\r\n");
                                        break;
                                }
                                extraCmds.Append("SET BACK ON\r\n");
                                break;
                            case "TEAR":
                                extraCmds.Append("SET CUTTER OFF\r\n");
                                extraCmds.Append("SET PEEL OFF\r\n");
                                extraCmds.Append("SET TEAR ON\r\n");
                                break;
                            case "PEEL":
                                extraCmds.Append("SET CUTTER OFF\r\n");
                                extraCmds.Append("SET TEAR OFF\r\n");
                                extraCmds.Append("SET PEEL ON\r\n");
                                break;
                            case "NONE":
                                extraCmds.Append("SET CUTTER OFF\r\nSET PEEL OFF\r\nSET TEAR OFF\r\n");
                                break;
                            case "DEFAULT":
                            default:
                                // 프린터 자체 설정 사용 (추가 명령 없음)
                                break;
                        }

                        if (extraCmds.Length > 0)
                        {
                            extraCmdsStr = extraCmds.ToString();
                        }
                    }

                    byte[] tsplCommandBytes = PrintManager.ConvertBitmapToTSPL_(labelBmp, effectiveCopies, extraCmdsStr);

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
            }
            catch (Exception ex)
            {
                PrinterSerialLogger.LogError("SendToTsplPrinter 실패", ex);
                StatusMessage = $"인쇄 생성 실패: {ex.Message}";
                return false;
            }
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

            foreach (var rec in targets)
            {

                bool success = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                rec.Status = success ? "인쇄 완료" : "인쇄 실패";
                if (success)
                {
                    successCount++;
                    totalCopiesPrinted += ExcelPrintCopies;
                }
                else allSuccess = false;
            }

            SaveHistoryRecords();
            StatusMessage = allSuccess
                ? $"구간 인쇄 완료: No.{start} ~ No.{end} (총 {targets.Count}건 전체 성공, 매수: {ExcelPrintCopies})"
                : $"구간 인쇄 완료 (일부 실패): No.{start} ~ No.{end} (성공 {successCount}/{targets.Count})";
        }

        public void ExecutePrintAndRecord()
        {
            if (IsAllInputsEmpty())
            {
                _dialogService.ShowMessage("인쇄할 데이터를 입력해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

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

        public void ExecutePrintOnly()
        {
            string c1 = !string.IsNullOrEmpty(InputCol1) ? InputCol1 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 1)?.Content ?? "");
            string c2 = !string.IsNullOrEmpty(InputCol2) ? InputCol2 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 2)?.Content ?? "");
            string c3 = !string.IsNullOrEmpty(InputCol3) ? InputCol3 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 3)?.Content ?? "");
            string c4 = !string.IsNullOrEmpty(InputCol4) ? InputCol4 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 4)?.Content ?? "");
            string c5 = !string.IsNullOrEmpty(InputCol5) ? InputCol5 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 5)?.Content ?? "");
            string c6 = !string.IsNullOrEmpty(InputCol6) ? InputCol6 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 6)?.Content ?? "");
            string c7 = !string.IsNullOrEmpty(InputCol7) ? InputCol7 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 7)?.Content ?? "");
            string c8 = !string.IsNullOrEmpty(InputCol8) ? InputCol8 : (CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == 8)?.Content ?? "");

            bool hasData = !string.IsNullOrWhiteSpace(c1) || !string.IsNullOrWhiteSpace(c2) || 
                            !string.IsNullOrWhiteSpace(c3) || !string.IsNullOrWhiteSpace(c4) || 
                            !string.IsNullOrWhiteSpace(c5) || !string.IsNullOrWhiteSpace(c6) || 
                            !string.IsNullOrWhiteSpace(c7) || !string.IsNullOrWhiteSpace(c8);

            if (!hasData && (CurrentTemplate == null || CurrentTemplate.Elements == null || CurrentTemplate.Elements.Count == 0))
            {
                _dialogService.ShowMessage("인쇄할 데이터를 입력해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            int copies = Math.Max(1, Math.Min(999, SinglePrintCopies));
            bool printSuccess = SendToTsplPrinter(c1, c2, c3, c4, c5, c6, c7, c8, copies);

            StatusMessage = printSuccess ? $"단건 인쇄 완료: {copies}매" : $"인쇄 실패: {SelectedPrinterName}";
            ClearInputs();
            FocusFirstInputRequested?.Invoke();
        }

        public void ExecuteTestPrint()
        {
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
