using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BarcodePrint.Model;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    /// <summary>
    /// 안드로이드 / iOS / Mac과 완벽히 동일한 구조의 라벨 인쇄 설정 모달 다이얼로그
    /// </summary>
    public partial class PrintSetupDialog : Window
    {
        private readonly MainViewModel _vm;
        private bool _isAdvancedExpanded = false;

        public PrintSetupDialog(MainViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _vm;

            Loaded += OnWindowLoaded;
            PreviewKeyDown += OnWindowPreviewKeyDown;
            MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    DragMove();
                }
            };
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OnStartPrintClick(BtnStartPrint, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                OnCloseClick(null, null);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 1. 프린터 이름 동기화
            TxtTargetPrinterName.Text = !string.IsNullOrWhiteSpace(_vm.SelectedPrinterName)
                ? _vm.SelectedPrinterName
                : "선택된 프린터 없음";

            // 2. 인쇄 매수 초기화
            int defaultCopies = Math.Max(1, _vm.SinglePrintCopies);
            TxtCopies.Text = defaultCopies.ToString();

            // 3. 인쇄 방향 초기화
            int rot = _vm.CurrentTemplate != null ? _vm.CurrentTemplate.Rotation : _vm.LabelRotationAngle;
            switch (rot)
            {
                case 90: CboRotation.SelectedIndex = 1; break;
                case 180: CboRotation.SelectedIndex = 2; break;
                case 270: CboRotation.SelectedIndex = 3; break;
                default: CboRotation.SelectedIndex = 0; break;
            }

            // 4. 엑셀 연동 인쇄 범위 카드 초기화
            if (_vm.IsExcelFileLoaded && _vm.Records != null && _vm.Records.Count > 0)
            {
                BorderExcelPrintRange.Visibility = Visibility.Visible;
                int totalRows = _vm.Records.Count;
                TxtExcelTotalInfo.Text = $"총 {totalRows}건 연동됨";

                RbExcelAll.Content = $"전체 데이터 인쇄 (1 ~ {totalRows}행, 총 {totalRows}건)";
                int curPreviewRow = _vm.CurrentPreviewIndex > 0 ? _vm.CurrentPreviewIndex : 1;
                RbExcelCurrent.Content = $"현재 미리보기 페이지만 인쇄 (제 {curPreviewRow}행)";

                TxtRangeStart.Text = "1";
                TxtRangeEnd.Text = totalRows.ToString();
            }
            else
            {
                BorderExcelPrintRange.Visibility = Visibility.Collapsed;
            }

            // 4-2. 다중 페이지 인쇄 범위 카드 초기화 (엑셀 모드가 아니고 2페이지 이상일 때)
            if (BorderExcelPrintRange.Visibility != Visibility.Visible && _vm.HasMultiplePages)
            {
                BorderTemplatePageRange.Visibility = Visibility.Visible;
                int totalPages = _vm.TotalPageCount;
                TxtTemplatePageTotalInfo.Text = $"총 {totalPages}페이지";
                RbPageAll.Content = $"전체 페이지 인쇄 (1 ~ {totalPages}페이지, 총 {totalPages}장)";
                RbPageCurrent.Content = $"현재 페이지만 인쇄 (제 {_vm.CurrentPageNumber}페이지)";
                TxtPageRangeStart.Text = "1";
                TxtPageRangeEnd.Text = totalPages.ToString();
            }
            else
            {
                BorderTemplatePageRange.Visibility = Visibility.Collapsed;
            }

            // 4-3. 다중 행·열 용지 배열 안내 표시 (다중 배열일 때만)
            if (_vm.IsMultiLabelLayout)
            {
                BorderMultiLayoutInfo.Visibility = Visibility.Visible;
                TxtMultiLayoutSummary.Text = $"{_vm.LabelColumns}열 × {_vm.LabelRows}행 · 전체 {_vm.GetTotalSheetWidthMm():0.#}×{_vm.GetTotalSheetHeightMm():0.#}mm";
            }
            else
            {
                BorderMultiLayoutInfo.Visibility = Visibility.Collapsed;
            }

            // 5. 인쇄 방식 & 센서 유형 초기화
            CboPrintMethod.SelectedIndex = _vm.IsRibbon ? 1 : 0;
            if (_vm.PaperType == "BLINE")
            {
                CboPaperSensor.SelectedIndex = 1;
            }
            else if (_vm.PaperType == "CONTINUOUS")
            {
                CboPaperSensor.SelectedIndex = 2;
            }
            else
            {
                CboPaperSensor.SelectedIndex = 0;
            }

            // 6. 고급 옵션 목록 채우기 (속도 & 농도)
            CboSpeed.Items.Clear();
            CboSpeed.Items.Add("2.0 ips");
            CboSpeed.Items.Add("3.0 ips");
            CboSpeed.Items.Add("4.0 ips");
            CboSpeed.Items.Add("5.0 ips");
            CboSpeed.Items.Add("6.0 ips");
            CboSpeed.Items.Add("7.0 ips");
            CboSpeed.Items.Add("8.0 ips");

            int speed = _vm.PrintSpeed;
            int speedIdx = (speed >= 2 && speed <= 8) ? speed - 2 : 2;
            CboSpeed.SelectedIndex = speedIdx;

            CboDarkness.Items.Clear();
            CboDarkness.Items.Add("1단계 (흐리게)");
            for (int i = 2; i <= 14; i++)
            {
                if (i == 8)
                {
                    CboDarkness.Items.Add("8단계 (표준)");
                }
                else
                {
                    CboDarkness.Items.Add($"{i}단계");
                }
            }
            CboDarkness.Items.Add("15단계 (진하게)");

            int dark = _vm.PrintDarkness;
            int darkIdx = (dark >= 1 && dark <= 15) ? dark - 1 : 7;
            CboDarkness.SelectedIndex = darkIdx;

            // 7. 용지 간격 / 피드 오프셋
            TxtGapHeight.Text = _vm.GapHeight.ToString();
            TxtFeedOffset.Text = "0.0";

            // 8. 팝업 오픈 즉시 엔터 키 인쇄를 위한 기본 포커스 설정
            Dispatcher.BeginInvoke(new Action(() =>
            {
                BtnStartPrint?.Focus();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnCopiesMinusClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtCopies.Text, out int val) && val > 1)
            {
                TxtCopies.Text = (val - 1).ToString();
            }
        }

        private void OnCopiesPlusClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtCopies.Text, out int val) && val < 999)
            {
                TxtCopies.Text = (val + 1).ToString();
            }
            else
            {
                TxtCopies.Text = "1";
            }
        }

        private void OnTemplatePageRangeOptionChanged(object sender, RoutedEventArgs e)
        {
            if (PanelPageRangeInputs == null || PanelPageSpecificInputs == null) return;
            PanelPageRangeInputs.Visibility = (RbPageRange.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
            PanelPageSpecificInputs.Visibility = (RbPageSpecific.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnExcelRangeOptionChanged(object sender, RoutedEventArgs e)
        {
            if (PanelExcelRangeInputs == null || PanelExcelSpecificInputs == null) return;

            PanelExcelRangeInputs.Visibility = (RbExcelRange.IsChecked == true)
                ? Visibility.Visible
                : Visibility.Collapsed;

            PanelExcelSpecificInputs.Visibility = (RbExcelSpecific.IsChecked == true)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnToggleAdvancedClick(object sender, RoutedEventArgs e)
        {
            _isAdvancedExpanded = !_isAdvancedExpanded;
            PanelAdvancedOptions.Visibility = _isAdvancedExpanded ? Visibility.Visible : Visibility.Collapsed;
            TxtToggleAdvancedArrow.Text = _isAdvancedExpanded ? "▼" : "▶";
            TxtToggleAdvancedLabel.Text = _isAdvancedExpanded
                ? "고급 인쇄 옵션 접기"
                : "고급 인쇄 옵션 펼치기 (속도/농도/센서/오프셋)";
        }

        private void OnQuickFeedClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.ExecutePrinterAction("FEED");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"급지 명령 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnStartPrintClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtCopies.Text, out int copies) || copies < 1)
            {
                copies = 1;
            }

            // 1. 인쇄 속도 / 농도 적용
            if (CboSpeed.SelectedIndex >= 0)
            {
                _vm.PrintSpeed = CboSpeed.SelectedIndex + 2;
            }
            if (CboDarkness.SelectedIndex >= 0)
            {
                _vm.PrintDarkness = CboDarkness.SelectedIndex + 1;
            }

            // 2. 인쇄 방향 적용
            if (CboRotation.SelectedIndex >= 0)
            {
                int rot = 0;
                switch (CboRotation.SelectedIndex)
                {
                    case 1: rot = 90; break;
                    case 2: rot = 180; break;
                    case 3: rot = 270; break;
                    default: rot = 0; break;
                }
                if (_vm.CurrentTemplate != null)
                {
                    _vm.CurrentTemplate.Rotation = rot;
                }
                _vm.LabelRotationAngle = rot;
            }

            // 3. 인쇄 방식 및 센서 유형 적용
            if (CboPrintMethod.SelectedIndex >= 0)
            {
                _vm.IsRibbon = (CboPrintMethod.SelectedIndex == 1);
            }
            if (CboPaperSensor.SelectedIndex >= 0)
            {
                switch (CboPaperSensor.SelectedIndex)
                {
                    case 0: _vm.PaperType = "GAP"; break;
                    case 1: _vm.PaperType = "BLINE"; break;
                    case 2: _vm.PaperType = "CONTINUOUS"; break;
                }
            }

            // 4. 용지 갭 적용
            if (int.TryParse(TxtGapHeight.Text, out int gapVal))
            {
                _vm.GapHeight = gapVal;
            }

            // 5. 인쇄 분기 (엑셀 연동 vs 일반 라벨 단건)
            if (_vm.IsExcelFileLoaded && _vm.Records != null && _vm.Records.Count > 0)
            {
                _vm.ExcelPrintCopies = copies;

                if (RbExcelAll.IsChecked == true)
                {
                    // 전체 데이터 인쇄
                    _vm.ExecutePrintAllRecords();
                }
                else if (RbExcelCurrent.IsChecked == true)
                {
                    // 현재 미리보기 페이지만 인쇄
                    int curIdx = _vm.CurrentPreviewIndex - 1;
                    if (curIdx >= 0 && curIdx < _vm.Records.Count)
                    {
                        var rec = _vm.Records[curIdx];
                        _vm.PrintSingleExcelRecord(rec, copies);
                        rec.Status = "인쇄 완료";
                        _vm.SaveHistoryRecords();
                        _vm.StatusMessage = $"현재 행(제 {curIdx + 1}행) 라벨 {copies}매 인쇄 완료";
                    }
                }
                else if (RbExcelRange.IsChecked == true)
                {
                    // 구간 지정 인쇄
                    int.TryParse(TxtRangeStart.Text, out int start);
                    int.TryParse(TxtRangeEnd.Text, out int end);
                    _vm.PrintRangeStart = Math.Max(1, start);
                    _vm.PrintRangeEnd = Math.Min(_vm.Records.Count, Math.Max(start, end));
                    _vm.ExecutePrintRange();
                }
                else if (RbExcelSpecific.IsChecked == true)
                {
                    // 특정 페이지 파싱 및 인쇄
                    string specText = TxtSpecificPages.Text != null ? TxtSpecificPages.Text.Trim() : "";
                    var pageIndices = ParseSpecificPages(specText, _vm.Records.Count);
                    if (pageIndices.Count == 0)
                    {
                        MessageBox.Show("인쇄할 올바른 페이지 번호를 입력해 주세요. (예: 1, 3, 5-8)", "페이지 입력 안내", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    int successCount = 0;
                    var specRecords = pageIndices.Select(idx => _vm.Records[idx]).ToList();
                    if (_vm.IsMultiLabelLayout)
                    {
                        _vm.PrintRecordsMultiColumn(specRecords, copies);
                        successCount = specRecords.Count(r => r.Status == "인쇄 완료");
                    }
                    else
                    {
                        foreach (var rec in specRecords)
                        {
                            bool ok = _vm.PrintSingleExcelRecord(rec, copies);
                            if (ok)
                            {
                                rec.Status = "인쇄 완료";
                                successCount++;
                            }
                        }
                    }
                    _vm.SaveHistoryRecords();
                    _vm.StatusMessage = $"지정 페이지 인쇄 완료: 총 {pageIndices.Count}개 중 {successCount}건 성공 (각 {copies}매)";
                }
            }
            else if (_vm.HasMultiplePages)
            {
                _vm.SinglePrintCopies = copies;
                int totalPages = _vm.TotalPageCount;
                List<int> targetIndices;

                if (RbPageCurrent.IsChecked == true)
                {
                    targetIndices = new List<int> { Math.Max(0, Math.Min(totalPages - 1, _vm.CurrentPageNumber - 1)) };
                }
                else if (RbPageRange.IsChecked == true)
                {
                    int.TryParse(TxtPageRangeStart.Text, out int startP);
                    int.TryParse(TxtPageRangeEnd.Text, out int endP);
                    startP = Math.Max(1, Math.Min(totalPages, startP));
                    endP = Math.Max(startP, Math.Min(totalPages, endP));
                    targetIndices = Enumerable.Range(startP - 1, endP - startP + 1).ToList();
                }
                else if (RbPageSpecific.IsChecked == true)
                {
                    targetIndices = ParseSpecificPages(TxtPageSpecific.Text?.Trim() ?? "", totalPages);
                    if (targetIndices.Count == 0)
                    {
                        MessageBox.Show("인쇄할 올바른 페이지 번호를 입력해 주세요. (예: 1, 3, 5-8)", "페이지 입력 안내", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                else
                {
                    targetIndices = Enumerable.Range(0, totalPages).ToList();
                }

                _vm.ExecutePrintPages(targetIndices, copies);
            }
            else
            {
                // 일반 단건 인쇄
                _vm.SinglePrintCopies = copies;
                _vm.ExecutePrintOnly();
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// "1, 3, 5-8, 10" 형태의 문자열을 0-based 레코드 인덱스 리스트로 파싱
        /// </summary>
        private List<int> ParseSpecificPages(string input, int maxCount)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(input)) return result.ToList();

            var tokens = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                if (trimmed.Contains("-") || trimmed.Contains("~"))
                {
                    var rangeParts = trimmed.Split(new[] { '-', '~' }, StringSplitOptions.RemoveEmptyEntries);
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0], out int rStart) &&
                        int.TryParse(rangeParts[1], out int rEnd))
                    {
                        int min = Math.Min(rStart, rEnd);
                        int max = Math.Max(rStart, rEnd);
                        for (int p = min; p <= max; p++)
                        {
                            if (p >= 1 && p <= maxCount)
                            {
                                result.Add(p - 1);
                            }
                        }
                    }
                }
                else if (int.TryParse(trimmed, out int singlePage))
                {
                    if (singlePage >= 1 && singlePage <= maxCount)
                    {
                        result.Add(singlePage - 1);
                    }
                }
            }

            return result.OrderBy(x => x).ToList();
        }
    }
}
