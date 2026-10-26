using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using BarcodePrint.Model;
using Microsoft.Win32;

namespace BarcodePrint.WpfView
{
    public partial class PdfPrintDialog : Window
    {
        private string _pdfFilePath;
        private int _totalPages = 0;
        private int _currentPageIndex = 0;
        private int _rotationAngle = 0; // 0, 90, 180, 270

        private double _paperWidthMm = 100.0;
        private double _paperHeightMm = 100.0;
        private int _copies = 1;

        private readonly Action<string, double, double> _importToCanvasCallback;
        private Bitmap _cachedPageBitmap = null;

        private List<PdfPaperPresetItem> _paperPresets = new List<PdfPaperPresetItem>();
        private bool _isUpdatingRangeText = false;

        public PdfPrintDialog(string pdfFilePath, double defaultWidth = 100.0, double defaultHeight = 100.0, Action<string, double, double> importCallback = null)
        {
            InitializeComponentSafely();

            _pdfFilePath = pdfFilePath;
            _paperWidthMm = defaultWidth > 0 ? defaultWidth : 100.0;
            _paperHeightMm = defaultHeight > 0 ? defaultHeight : 100.0;
            _importToCanvasCallback = importCallback;

            TxtPaperWidth.Text = _paperWidthMm.ToString("0.#");
            TxtPaperHeight.Text = _paperHeightMm.ToString("0.#");

            LoadPaperPresets();
            LoadInstalledPrinters();
            LoadPdfDocument();
            UpdateRangeUiState();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/pdfprintdialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PdfPrintDialog LoadComponent Error: {ex.Message}");
            }
        }

        private void LoadInstalledPrinters()
        {
            try
            {
                CboPrinters.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    CboPrinters.Items.Add(printer);
                }

                // Xprinter 우선 선택
                var settings = new PrinterSettings();
                string defaultPrinter = settings.PrinterName;
                int selectedIdx = 0;

                for (int i = 0; i < CboPrinters.Items.Count; i++)
                {
                    string p = CboPrinters.Items[i].ToString();
                    if (p.IndexOf("XP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        p.IndexOf("4B", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        p.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        p.IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        selectedIdx = i;
                        break;
                    }
                    if (p.Equals(defaultPrinter, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = i;
                    }
                }

                if (CboPrinters.Items.Count > 0)
                {
                    CboPrinters.SelectedIndex = selectedIdx;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadPrinters Error: {ex.Message}");
            }
        }

        private void LoadPdfDocument()
        {
            if (string.IsNullOrEmpty(_pdfFilePath) || !File.Exists(_pdfFilePath))
            {
                MessageBox.Show("지정된 PDF 파일을 찾을 수 없습니다.", "파일 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TxtPdfFileName.Text = $"파일명: {Path.GetFileName(_pdfFilePath)}";

            _totalPages = PdfRenderHelper.GetPdfPageCount(_pdfFilePath);
            if (_totalPages <= 0) _totalPages = 1;

            TxtTotalPagesBadge.Text = $"총 {_totalPages}페이지";
            _currentPageIndex = 0;

            if (TxtStartPage != null) TxtStartPage.Text = "1";
            if (TxtEndPage != null) TxtEndPage.Text = _totalPages.ToString();
            if (TxtCustomRange != null) TxtCustomRange.Text = _totalPages > 1 ? $"1~{_totalPages}" : "1";
            UpdateRangeSummary();

            UpdatePreview();
        }

        private async void UpdatePreview()
        {
            if (_totalPages <= 0) return;

            BtnPrevPage.IsEnabled = _currentPageIndex > 0;
            BtnNextPage.IsEnabled = _currentPageIndex < _totalPages - 1;
            TxtPageIndicator.Text = $"{_currentPageIndex + 1} / {_totalPages} 페이지";

            PanelLoading.Visibility = Visibility.Visible;
            ImgPdfPreview.Opacity = 0.3;

            int targetPage = _currentPageIndex;
            string pdfPath = _pdfFilePath;

            await Task.Run(() =>
            {
                var bmp = PdfRenderHelper.RenderPageToBitmap(pdfPath, targetPage, 203);
                _cachedPageBitmap?.Dispose();
                _cachedPageBitmap = bmp;
            });

            if (_cachedPageBitmap != null)
            {
                Bitmap displayBmp = (Bitmap)_cachedPageBitmap.Clone();
                if (_rotationAngle != 0)
                {
                    switch (_rotationAngle)
                    {
                        case 90: displayBmp.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                        case 180: displayBmp.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                        case 270: displayBmp.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                    }
                }

                ImgPdfPreview.Source = ConvertBitmapToBitmapSource(displayBmp);
                displayBmp.Dispose();
            }

            PanelLoading.Visibility = Visibility.Collapsed;
            ImgPdfPreview.Opacity = 1.0;
            TxtBottomStatus.Text = $"현재 {_currentPageIndex + 1}페이지 미리보기 로드 완료. (인쇄 크기: {_paperWidthMm}x{_paperHeightMm} mm)";
        }

        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #region UI Event Handlers

        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePreview();
            }
        }

        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (_currentPageIndex < _totalPages - 1)
            {
                _currentPageIndex++;
                UpdatePreview();
            }
        }

        private void OnRotateClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                if (int.TryParse(btn.Tag.ToString(), out int angle))
                {
                    _rotationAngle = angle;
                    UpdatePreview();
                }
            }
        }

        private void OnOpenOtherPdfClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "인쇄할 PDF 송장 또는 문서 선택",
                Filter = "PDF 문서 (*.pdf)|*.pdf|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                _pdfFilePath = dlg.FileName;
                LoadPdfDocument();
            }
        }

        private void OnPaperDimensionChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (double.TryParse(TxtPaperWidth?.Text, out double w) && w > 0) _paperWidthMm = w;
            if (double.TryParse(TxtPaperHeight?.Text, out double h) && h > 0) _paperHeightMm = h;
        }

        private void OnDecreaseWidthClick(object sender, RoutedEventArgs e)
        {
            _paperWidthMm = Math.Max(10, _paperWidthMm - 5);
            TxtPaperWidth.Text = _paperWidthMm.ToString("0.#");
        }

        private void OnIncreaseWidthClick(object sender, RoutedEventArgs e)
        {
            _paperWidthMm = Math.Min(300, _paperWidthMm + 5);
            TxtPaperWidth.Text = _paperWidthMm.ToString("0.#");
        }

        private void OnDecreaseHeightClick(object sender, RoutedEventArgs e)
        {
            _paperHeightMm = Math.Max(10, _paperHeightMm - 5);
            TxtPaperHeight.Text = _paperHeightMm.ToString("0.#");
        }

        private void OnIncreaseHeightClick(object sender, RoutedEventArgs e)
        {
            _paperHeightMm = Math.Min(500, _paperHeightMm + 5);
            TxtPaperHeight.Text = _paperHeightMm.ToString("0.#");
        }

        #region Paper Preset Management

        private void LoadPaperPresets()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                _paperPresets = (settings.PdfPaperPresets != null && settings.PdfPaperPresets.Count > 0)
                    ? settings.PdfPaperPresets
                    : AppSettings.GetDefaultPdfPaperPresets();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadPaperPresets Error: {ex.Message}");
                _paperPresets = AppSettings.GetDefaultPdfPaperPresets();
            }

            RenderPresetChips();
        }

        private void SavePaperPresets()
        {
            try
            {
                var settings = new AppSettings();
                settings.Load();
                settings.PdfPaperPresets = _paperPresets;
                settings.Save();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SavePaperPresets Error: {ex.Message}");
            }
        }

        private void RenderPresetChips()
        {
            if (PanelPresetChips == null) return;
            PanelPresetChips.Children.Clear();

            var btnStyle = this.FindResource("MiniStepBtnStyle") as Style;

            foreach (var preset in _paperPresets)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Content = preset.Name,
                    Tag = $"{preset.Width},{preset.Height}",
                    Style = btnStyle,
                    Height = 22,
                    Padding = new Thickness(6, 0, 6, 0),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 4, 4),
                    ToolTip = $"{preset.Name} ({preset.Width} x {preset.Height} mm) - 우클릭으로 수정/삭제"
                };

                btn.Click += OnPresetSizeClick;

                // 우클릭 컨텍스트 메뉴
                var cm = new System.Windows.Controls.ContextMenu();
                var miEdit = new System.Windows.Controls.MenuItem { Header = $"✏️ '{preset.Name}' 수정" };
                miEdit.Click += (s, e) => EditSinglePreset(preset);

                var miDelete = new System.Windows.Controls.MenuItem { Header = $"🗑️ '{preset.Name}' 삭제" };
                miDelete.Click += (s, e) => DeleteSinglePreset(preset);

                cm.Items.Add(miEdit);
                cm.Items.Add(miDelete);
                btn.ContextMenu = cm;

                PanelPresetChips.Children.Add(btn);
            }
        }

        private void OnAddCurrentPresetClick(object sender, RoutedEventArgs e)
        {
            string name = $"{_paperWidthMm:0.#}x{_paperHeightMm:0.#}";
            
            // 중복 확인
            var existing = _paperPresets.FirstOrDefault(p => Math.Abs(p.Width - _paperWidthMm) < 0.1 && Math.Abs(p.Height - _paperHeightMm) < 0.1);
            if (existing != null)
            {
                MessageBox.Show($"이미 '{existing.Name}' ({existing.Width}x{existing.Height}mm) 규격이 등록되어 있습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _paperPresets.Add(new PdfPaperPresetItem(name, _paperWidthMm, _paperHeightMm));
            SavePaperPresets();
            RenderPresetChips();

            TxtBottomStatus.Text = $"자주 쓰는 규격에 '{name}' ({_paperWidthMm}x{_paperHeightMm}mm)이 추가되었습니다.";
        }

        private void OnManagePresetsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new PdfPaperPresetManagerDialog(_paperPresets, _paperWidthMm, _paperHeightMm)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && dlg.ResultPresets != null)
            {
                _paperPresets = dlg.ResultPresets;
                SavePaperPresets();
                RenderPresetChips();
                TxtBottomStatus.Text = "자주 쓰는 용지 규격 목록이 업데이트되었습니다.";
            }
        }

        private void EditSinglePreset(PdfPaperPresetItem item)
        {
            var dlg = new PdfPaperPresetManagerDialog(_paperPresets, item.Width, item.Height)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && dlg.ResultPresets != null)
            {
                _paperPresets = dlg.ResultPresets;
                SavePaperPresets();
                RenderPresetChips();
            }
        }

        private void DeleteSinglePreset(PdfPaperPresetItem item)
        {
            if (_paperPresets.Count <= 1)
            {
                MessageBox.Show("최소 1개 이상의 규격이 유지되어야 합니다.", "삭제 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"'{item.Name}' ({item.Width}x{item.Height}mm) 규격을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _paperPresets.Remove(item);
                SavePaperPresets();
                RenderPresetChips();
            }
        }

        private void OnPresetSizeClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                var parts = btn.Tag.ToString().Split(',');
                if (parts.Length == 2 && double.TryParse(parts[0], out double w) && double.TryParse(parts[1], out double h))
                {
                    _paperWidthMm = w;
                    _paperHeightMm = h;
                    TxtPaperWidth.Text = w.ToString("0.#");
                    TxtPaperHeight.Text = h.ToString("0.#");
                    TxtBottomStatus.Text = $"용지 규격이 {w}x{h} mm로 설정되었습니다.";
                }
            }
        }

        #endregion

        #region Page Range Selection & Input Handlers

        private void OnPrintRangeRadioChanged(object sender, RoutedEventArgs e)
        {
            UpdateRangeUiState();
        }

        private void UpdateRangeUiState()
        {
            if (BorderCustomRange == null) return;
            bool isCustom = RbCustomPages?.IsChecked == true;
            BorderCustomRange.IsEnabled = isCustom;
            BorderCustomRange.Opacity = isCustom ? 1.0 : 0.45;
        }

        private void OnDecreaseStartPageClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtStartPage?.Text, out int cur))
            {
                cur = Math.Max(1, cur - 1);
                TxtStartPage.Text = cur.ToString();
            }
        }

        private void OnIncreaseStartPageClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtStartPage?.Text, out int cur))
            {
                cur = Math.Min(_totalPages > 0 ? _totalPages : 1, cur + 1);
                TxtStartPage.Text = cur.ToString();
            }
        }

        private void OnDecreaseEndPageClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtEndPage?.Text, out int cur))
            {
                cur = Math.Max(1, cur - 1);
                TxtEndPage.Text = cur.ToString();
            }
        }

        private void OnIncreaseEndPageClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtEndPage?.Text, out int cur))
            {
                cur = Math.Min(_totalPages > 0 ? _totalPages : 1, cur + 1);
                TxtEndPage.Text = cur.ToString();
            }
        }

        private void OnStartEndPageChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdatingRangeText) return;

            if (int.TryParse(TxtStartPage?.Text, out int start) && int.TryParse(TxtEndPage?.Text, out int end))
            {
                _isUpdatingRangeText = true;
                try
                {
                    TxtCustomRange.Text = start == end ? $"{start}" : $"{start}~{end}";
                }
                finally
                {
                    _isUpdatingRangeText = false;
                }
            }
            UpdateRangeSummary();
        }

        private void OnCustomRangeTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdatingRangeText) return;

            string text = TxtCustomRange?.Text?.Trim() ?? "";
            string unified = text.Replace('~', '-');
            if (unified.Contains("-"))
            {
                var parts = unified.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int s) && int.TryParse(parts[1].Trim(), out int endVal))
                {
                    _isUpdatingRangeText = true;
                    try
                    {
                        if (TxtStartPage != null) TxtStartPage.Text = s.ToString();
                        if (TxtEndPage != null) TxtEndPage.Text = endVal.ToString();
                    }
                    finally
                    {
                        _isUpdatingRangeText = false;
                    }
                }
            }
            else if (int.TryParse(unified, out int single))
            {
                _isUpdatingRangeText = true;
                try
                {
                    if (TxtStartPage != null) TxtStartPage.Text = single.ToString();
                    if (TxtEndPage != null) TxtEndPage.Text = single.ToString();
                }
                finally
                {
                    _isUpdatingRangeText = false;
                }
            }

            UpdateRangeSummary();
        }

        private void UpdateRangeSummary()
        {
            if (TxtRangeSummary == null) return;

            string rangeText = TxtCustomRange?.Text ?? "";
            var pages = ParsePageRanges(rangeText, _totalPages > 0 ? _totalPages : 9999);
            TxtRangeSummary.Text = $"(총 {pages.Count}페이지)";
        }

        #endregion

        private void OnDecreaseCopiesClick(object sender, RoutedEventArgs e)
        {
            _copies = Math.Max(1, _copies - 1);
            TxtCopies.Text = _copies.ToString();
        }

        private void OnIncreaseCopiesClick(object sender, RoutedEventArgs e)
        {
            _copies = Math.Min(999, _copies + 1);
            TxtCopies.Text = _copies.ToString();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Actions: Print & Canvas Import

        private void OnImportToCanvasClick(object sender, RoutedEventArgs e)
        {
            int targetDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? (int)PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203;
            string tempPng = PdfRenderHelper.SavePageToTempPng(_pdfFilePath, _currentPageIndex, targetDpi);
            if (string.IsNullOrEmpty(tempPng) || !File.Exists(tempPng))
            {
                MessageBox.Show("페이지 이미지를 추출하는 데 실패했습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_importToCanvasCallback != null)
            {
                _importToCanvasCallback.Invoke(tempPng, _paperWidthMm, _paperHeightMm);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show($"페이지 이미지가 저장되었습니다:\n{tempPng}", "가져오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void OnDirectPrintClick(object sender, RoutedEventArgs e)
        {
            string printerName = CboPrinters.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("출력할 프린터를 선택해주세요.", "프린터 선택 필요", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 인쇄 대상 페이지 목록 파싱
            List<int> targetPages = new List<int>();
            if (RbAllPages.IsChecked == true)
            {
                for (int i = 0; i < _totalPages; i++) targetPages.Add(i);
            }
            else if (RbCurrentPage.IsChecked == true)
            {
                targetPages.Add(_currentPageIndex);
            }
            else
            {
                targetPages = ParsePageRanges(TxtCustomRange.Text, _totalPages);
                if (targetPages.Count == 0)
                {
                    MessageBox.Show($"올바른 페이지 번호 또는 구간을 입력해주세요. (예: 1-3, 5 / 1 ~ {_totalPages})", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (targetPages.Count == 0)
            {
                MessageBox.Show("인쇄할 대상 페이지가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TxtBottomStatus.Text = $"총 {targetPages.Count}개 페이지를 '{printerName}' 프린터로 인쇄 중...";
            this.IsEnabled = false;

            try
            {
                int copies = _copies;
                int rotation = _rotationAngle;
                double paperW = _paperWidthMm;
                double paperH = _paperHeightMm;
                string pdfPath = _pdfFilePath;

                await Task.Run(() =>
                {
                    ExecuteDirectPrint(printerName, pdfPath, targetPages, copies, rotation, paperW, paperH);
                });

                TxtBottomStatus.Text = $"인쇄 완료: 총 {targetPages.Count}페이지 x {copies}매 출력이 완료되었습니다.";
                MessageBox.Show($"총 {targetPages.Count}페이지 x {_copies}매 인쇄 작업이 프린터로 전송되었습니다.", "인쇄 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"인쇄 중 오류가 발생했습니다: {ex.Message}", "인쇄 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtBottomStatus.Text = $"인쇄 실패: {ex.Message}";
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        private void ExecuteDirectPrint(string printerName, string pdfPath, List<int> pages, int copies, int rotation, double paperWMm, double paperHMm)
        {
            int targetDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI > 0 ? (int)PRINT_INFO_CLASS.PrinterInfo.printer_DPI : 203;
            for (int copy = 0; copy < copies; copy++)
            {
                foreach (int pageIdx in pages)
                {
                    using (var bmp = PdfRenderHelper.RenderPageToBitmap(pdfPath, pageIdx, targetDpi))
                    {
                        if (bmp == null) continue;

                        if (rotation != 0)
                        {
                            switch (rotation)
                            {
                                case 90: bmp.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                                case 180: bmp.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                                case 270: bmp.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                            }
                        }

                        // Windows PrintDocument를 통한 고품질 래스터 인쇄
                        using (var printDoc = new PrintDocument())
                        {
                            printDoc.PrinterSettings.PrinterName = printerName;
                            printDoc.PrintController = new StandardPrintController(); // 인쇄 진행 팝업 생략

                            // 용지 크기 설정 (1/100 inch 단위)
                            int widthHundredths = (int)Math.Round(paperWMm / 25.4 * 100);
                            int heightHundredths = (int)Math.Round(paperHMm / 25.4 * 100);
                            printDoc.DefaultPageSettings.PaperSize = new PaperSize("CustomLabel", widthHundredths, heightHundredths);
                            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                            printDoc.PrintPage += (s, ev) =>
                            {
                                ev.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                ev.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                ev.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                                // 전체 영역 채움
                                ev.Graphics.DrawImage(bmp, ev.MarginBounds);
                                ev.HasMorePages = false;
                            };

                            printDoc.Print();
                        }
                    }
                }
            }
        }

        private List<int> ParsePageRanges(string text, int maxPage)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(text)) return result.ToList();

            // 물결(~)과 하이픈(-) 모두 구간 구분자로 통일 처리
            var tokens = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                string t = token.Trim().Replace('~', '-');
                if (t.Contains("-"))
                {
                    var range = t.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0].Trim(), out int start) && int.TryParse(range[1].Trim(), out int end))
                    {
                        int min = Math.Min(start, end);
                        int max = Math.Max(start, end);
                        for (int p = min; p <= max; p++)
                        {
                            if (p >= 1 && p <= maxPage) result.Add(p - 1);
                        }
                    }
                }
                else if (int.TryParse(t, out int singlePage))
                {
                    if (singlePage >= 1 && singlePage <= maxPage) result.Add(singlePage - 1);
                }
            }

            var list = result.ToList();
            list.Sort();
            return list;
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _cachedPageBitmap?.Dispose();
            _cachedPageBitmap = null;
        }
    }
}
