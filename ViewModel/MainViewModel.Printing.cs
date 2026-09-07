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
            try
            {
                AvailablePrinters.Clear();

                // BT 필터 시 선택된 COM 포트 장치가 목록에 없으면 자동 생성하여 추가
                if (SelectedConnectionType == "BT" || SelectedConnectionType == "Bluetooth")
                {
                    if (!string.IsNullOrEmpty(BluetoothPortName) && !_cachedPrinters.Any(p => p.Type.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase) && p.PortName.Equals(BluetoothPortName, StringComparison.OrdinalIgnoreCase)))
                    {
                        _cachedPrinters.Add(new UsbPrinterInfo
                        {
                            Name = $"[블루투스] {BluetoothPortName}",
                            FriendlyName = "Bluetooth Printer",
                            PortName = BluetoothPortName,
                            Serial = "-",
                            Type = "Bluetooth",
                            Address = BluetoothPortName
                        });
                    }
                }
                // LAN 필터 시 선택된 IP 장치가 목록에 없으면 자동 생성하여 추가
                else if (SelectedConnectionType == "LAN")
                {
                    if (!string.IsNullOrEmpty(TcpPrinterIp) && !_cachedPrinters.Any(p => p.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase) && p.Address.Equals(TcpPrinterIp, StringComparison.OrdinalIgnoreCase)))
                    {
                        _cachedPrinters.Add(new UsbPrinterInfo
                        {
                            Name = $"[네트워크] {TcpPrinterIp}",
                            FriendlyName = "Network Printer",
                            PortName = $"{TcpPrinterIp}:{TcpPrinterPort}",
                            Serial = "-",
                            Type = "LAN",
                            Address = TcpPrinterIp
                        });
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

                foreach (var p in filtered)
                {
                    if (!AvailablePrinters.Contains(p.Name))
                    {
                        AvailablePrinters.Add(p.Name);
                    }
                }

                if (AvailablePrinters.Count > 0)
                {
                    // 이전에 저장된 마지막 프린터 이름 복원 시도
                    string preferredPrinter = _settings?.LastSelectedPrinterName;
                    if (!string.IsNullOrEmpty(preferredPrinter) && AvailablePrinters.Contains(preferredPrinter))
                    {
                        SelectedPrinterName = preferredPrinter;
                    }
                    else if (string.IsNullOrEmpty(SelectedPrinterName) || !AvailablePrinters.Contains(SelectedPrinterName))
                    {
                        SelectedPrinterName = AvailablePrinters[0];
                    }
                }
                else
                {
                    SelectedPrinterName = "";
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
        }

        public void CheckRealtimePrinterStatus()
        {
            if (_isCheckingPrinter) return;
            _isCheckingPrinter = true;

            Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(SelectedPrinterName))
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            IsCurrentPrinterAuthorized = false;
                        }));
                        return;
                    }

                    // 캐시된 프린터 정보에서 시리얼 검증
                    var info = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(SelectedPrinterName, StringComparison.OrdinalIgnoreCase));
                    string cachedSerial = info?.Serial;
                    bool isAuthorized = ValidatePrinterAuthorization(cachedSerial);

                    _printerService.CheckRealtimePrinterStatus(SelectedPrinterName, _cachedPrinters, out string badge, out string color);

                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        if (isAuthorized)
                        {
                            IsCurrentPrinterAuthorized = true;
                            PrinterStatusBadge = badge;
                            PrinterStatusColor = color;
                        }
                        else
                        {
                            IsCurrentPrinterAuthorized = false;

                            if (badge.Contains("헤드 열림") || badge.Contains("용지 없음") || badge.Contains("용지 걸림") || badge.Contains("오류") || badge.Contains("오프라인"))
                            {
                                PrinterStatusBadge = badge;
                                PrinterStatusColor = color;
                            }
                            else
                            {
                                PrinterStatusBadge = "🔴 미등록 프린터 (기기 등록 필요)";
                                PrinterStatusColor = "#DC2626";
                            }
                        }

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

                switch (action)
                {
                    case "CALIBRATE":
                    case "GAPDETECT":
                        if (PaperType == "BLINE")
                        {
                            tsplCmd = "CLS\r\nBLINEDETECT\r\n";
                            desc = "블랙마크 센서 보정(Bline Detect)";
                        }
                        else if (PaperType == "CONTINUOUS")
                        {
                            tsplCmd = "CLS\r\nGAP 0,0\r\n";
                            desc = "연속지 모드 설정";
                        }
                        else
                        {
                            tsplCmd = "CLS\r\nAUTODETECT\r\n";
                            desc = "센서/용지 자동 보정(Auto Detect)";
                        }
                        break;
                    case "AUTODETECT":
                        tsplCmd = "CLS\r\nAUTODETECT\r\n";
                        desc = "센서 자동 감지(Auto Detect)";
                        break;
                    case "HOME":
                        tsplCmd = "CLS\r\nHOME\r\n";
                        desc = "라벨 원점 이동(Home)";
                        break;
                    case "FEED":
                        tsplCmd = "CLS\r\nFORMFEED\r\n";
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

        private void DrawTextOnDc(DrawingContext dc, string text, double fontSizeMm, double offsetXMm, double offsetYMm, double dotsPerMm, System.Windows.Media.Brush brush, string fontName = "맑은 고딕", bool isBold = true, bool isItalic = false, double cellWidthMm = 0, string align = "LEFT", double cellHeightMm = 0, double cellPaddingMm = 0)
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

            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new System.Windows.Media.FontFamily(familyName), style, weight, FontStretches.Normal),
                Math.Max(6, targetPxSize),
                brush,
                1.0);

            if (cellWidthMm > 0)
            {
                // 셀 또는 박스 너비가 지정된 경우: 내부 패딩(cellPaddingMm)을 정확히 제외한 순수 콘텐츠 너비로 줄바꿈 폭 지정
                double cellWidthPx = cellWidthMm * dotsPerMm;
                double padPx = cellPaddingMm * dotsPerMm;
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
            else
            {
                // 너비 미지정(자유 배치 NoWrap): 줄바꿈 없이 한 줄로 렌더링 (미리보기 TextWrapping.NoWrap과 100% 일치)
                int effectiveW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 70;
                if (align == "CENTER")
                {
                    double availableWidthPx = effectiveW * dotsPerMm;
                    leftPx = Math.Max(0, (availableWidthPx - ft.Width) / 2.0);
                }
                else if (align == "RIGHT")
                {
                    double availableWidthPx = effectiveW * dotsPerMm;
                    leftPx = Math.Max(0, availableWidthPx - ft.Width - (2.0 * dotsPerMm));
                }
            }

            if (cellHeightMm > 0)
            {
                double cellHeightPx = cellHeightMm * dotsPerMm;
                topPx += Math.Max(0, (cellHeightPx - ft.Height) / 2.0);
            }

            dc.DrawText(ft, new System.Windows.Point(leftPx, topPx));
        }

        private Bitmap GenerateLabelBitmapForPrint(string col1, string col2, string col3, string col4, string col5, string col6, string col7, string col8)
        {
            double dotsPerMm = 8.0; // 203 DPI (8 dots/mm)
            int effectiveW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 70;
            int effectiveH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 28;
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
                                displayText = PriceFormatHelper.FormatPrice(val);
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
                                    displayText = PriceFormatHelper.FormatPrice(val);
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
                                    displayText = PriceFormatHelper.FormatPrice(val);
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
                        BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText);
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
                            BarcodeVisualHelper.RenderBarcodeOnDc(dc, val, bType, size, offX, offY, dotsPerMm, showBText);
                        }
                        else
                        {
                            string displayText = val;
                            if (isPrice && !string.IsNullOrWhiteSpace(val))
                            {
                                displayText = PriceFormatHelper.FormatPrice(val);
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

        private bool SendToTsplPrinter(string col1, string col2, string col3, string col4, string col5, string col6, string col7, string col8, int copies = 1)
        {
            try
            {
                if (AvailablePrinters == null || AvailablePrinters.Count == 0 || string.IsNullOrEmpty(SelectedPrinterName))
                {
                    _dialogService.ShowWarning("선택된 프린터가 없습니다. 프린터를 선택해 주세요.", "인쇄 알림");
                    return false;
                }

                if (!IsCurrentPrinterAuthorized)
                {
                    _dialogService.ShowWarning("사용 권한이 등록되지 않은 프린터입니다.\n\n프로그램 사용을 위해 기기 등록이 필요합니다. 관리자에게 문의해 주세요.", "기기 등록 필요");
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

            foreach (var rec in targets)
            {
                bool success = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                rec.Status = success ? "인쇄 완료" : "인쇄 실패";
                if (success) successCount++;
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
            if (IsAllInputsEmpty())
            {
                _dialogService.ShowMessage("인쇄할 데이터를 입력해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            int copies = Math.Max(1, Math.Min(999, SinglePrintCopies));
            bool printSuccess = SendToTsplPrinter(InputCol1, InputCol2, InputCol3, InputCol4, InputCol5, InputCol6, InputCol7, InputCol8, copies);

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
            foreach (var rec in Records)
            {
                bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                if (ok)
                {
                    rec.Status = "인쇄 완료";
                    successCount++;
                }
                else
                {
                    rec.Status = "인쇄 실패";
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
            foreach (var rec in unprinted)
            {
                bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                if (ok)
                {
                    rec.Status = "인쇄 완료";
                    successCount++;
                }
                else
                {
                    rec.Status = "인쇄 실패";
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
            foreach (var rec in targets)
            {
                bool ok = SendToTsplPrinter(rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8, ExcelPrintCopies);
                if (ok)
                {
                    rec.Status = "인쇄 완료";
                    successCount++;
                }
                else
                {
                    rec.Status = "인쇄 실패";
                }
            }

            SaveHistoryRecords();
            StatusMessage = $"선택출력 완료: 총 {targets.Count}건 중 {successCount}건 성공 (매수: {ExcelPrintCopies})";
        }
    }
}
