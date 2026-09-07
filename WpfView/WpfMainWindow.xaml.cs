using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BarcodePrint.Model;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    public partial class WpfMainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        private Canvas GlobalCanvas => FindName("GlobalPreviewCanvas") as Canvas;
        private Canvas ExcelCanvas => FindName("ExcelPreviewCanvas") as Canvas;
        private Canvas DragCanvas => FindName("DesignDragCanvas") as Canvas;
        private TextBlock DragInfoBlock => FindName("TxtDragInfo") as TextBlock;
        private TextBlock GlobalDragInfoBlock => FindName("TxtGlobalDragInfo") as TextBlock;

        // 드래그 조작 및 다중 선택 관련 변수들
        private enum TableDragMode
        {
            None,
            MoveItemOrTable,       // 단일/다중 항목 또는 표 전체 위치(TableOffsetX, TableOffsetY) 이동
            ResizeHeaderCol,       // 세로형 표 제목 열 너비 (TableHeaderWidthMm)
            ResizeHeaderHeight,    // 가로형 표 상단 헤더 행 높이 (TableHeaderHeightMm)
            ResizeRowSeparator,    // 특정 행 사이 가로선 (행 높이 조절)
            ResizeRightBorder,     // 표 전체 너비 (TableWidth)
            ResizeBottomBorder,    // 표 전체 높이 (TableHeight)
            ResizeItemWidth        // 자유 배치 개별 항목 너비 (Col1Width ~ Col8Width)
        }

        private TableDragMode _currentDragMode = TableDragMode.None;
        private int _resizingRowIndex = -1;
        private int _resizingColNum = -1;
        private double _startHeaderWidthMm;
        private double _startHeaderHeightMm;
        private double _startTableWidthMm;
        private double _startTableHeightMm;
        private double _startSpecificRowHeightMm;
        private double _startItemWidthMm;

        private bool _isDragging = false;
        private int _selectedItemIndex = -1;
        private readonly HashSet<int> _selectedItemIndices = new HashSet<int>();
        private readonly Dictionary<int, Point> _startOffsets = new Dictionary<int, Point>();
        private Point _dragStartPoint;
        private int _startOffsetX;
        private int _startOffsetY;
        private Canvas _activeDragCanvas = null;

        public WpfMainWindow()
        {
            InitializeComponentSafely();
            Loaded += WpfMainWindow_Loaded;
            Closing += (s, e) => ViewModel?.SaveSettingsSilently();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster_Basic_v2;component/wpfview/wpfmainwindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAML Load Component Exception: {ex.Message}");
            }
        }

        private void WpfMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FocusFirstInputRequested = FocusFirstInput;
                ViewModel.RequestPreviewRedraw = RedrawInteractiveCanvas;
                ViewModel.ScrollToLatestRecordRequested = ScrollToLatestRecord;
                ViewModel.ActiveColumnCountChanged += UpdateDataGridColumnsVisibility;
                ViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName != null && args.PropertyName.StartsWith("Column") && args.PropertyName.EndsWith("Header"))
                    {
                        UpdateDataGridColumnsVisibility(ViewModel.ActiveColumnCount);
                    }
                };
                UpdateDataGridColumnsVisibility(ViewModel.ActiveColumnCount);
            }

            FocusFirstInput();
            RedrawInteractiveCanvas();

            // 메인 윈도우 표시 및 스플래시 창 종료가 완전히 완료된 후 안전하게 하드웨어 인증 수행
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                await System.Threading.Tasks.Task.Delay(300);
                if (ViewModel != null && !string.IsNullOrEmpty(ViewModel.SelectedPrinterName) && ViewModel.AvailablePrinters.Contains(ViewModel.SelectedPrinterName))
                {
                    ViewModel.ExecuteReadPrinterHardwareInfo(showLoadingPopup: false);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]).ToLower();
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    string ext = System.IO.Path.GetExtension(filePath).ToLower();
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        ViewModel?.ExecuteImportFromExcel(filePath);
                        e.Handled = true;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("지원되는 엑셀 파일(*.xlsx, *.xls)을 드래그해 주세요.", "파일 형식 알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ScrollToLatestRecord()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var dg = FindName("MainDataGrid") as DataGrid;
                if (dg != null && dg.Items.Count > 0)
                {
                    dg.ScrollIntoView(dg.Items[dg.Items.Count - 1]);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateDataGridColumnsVisibility(int activeCount)
        {
            void Apply()
            {
                var dg = (FindName("ExcelDataGrid") as DataGrid) ?? (FindName("MainDataGrid") as DataGrid);
                if (dg == null || dg.Columns.Count < 10) return;

                for (int i = 1; i <= 8; i++)
                {
                    int colIdx = i + 1; // [0]=선택, [1]=순번, [2]=Col1, [3]=Col2, ..., [9]=Col8
                    if (colIdx < dg.Columns.Count)
                    {
                        dg.Columns[colIdx].Visibility = (i <= activeCount) ? Visibility.Visible : Visibility.Collapsed;
                        if (ViewModel != null)
                        {
                            string headerText = ViewModel.GetHeaderByIndex(i);
                            dg.Columns[colIdx].Header = !string.IsNullOrWhiteSpace(headerText) ? headerText : $"{i}열";
                        }
                    }
                }
            }

            if (Dispatcher.CheckAccess())
            {
                Apply();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(Apply), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        private void MainDataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var header = FindAncestor<System.Windows.Controls.Primitives.DataGridColumnHeader>(e.OriginalSource as DependencyObject);
            if (header?.Column != null && sender is DataGrid dg)
            {
                int rawIdx = dg.Columns.IndexOf(header.Column);
                int colIndex = rawIdx - 1; // 2열(Col1) -> 1, ..., 9열(Col8) -> 8
                if (colIndex >= 1 && colIndex <= 8)
                {
                    PromptChangeColumnHeader(colIndex);
                    e.Handled = true;
                }
            }
        }

        private void MainDataGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var header = FindAncestor<System.Windows.Controls.Primitives.DataGridColumnHeader>(e.OriginalSource as DependencyObject);
            if (header?.Column != null && sender is DataGrid dg)
            {
                int rawIdx = dg.Columns.IndexOf(header.Column);
                int colIndex = rawIdx - 1; // 2열(Col1) -> 1, ..., 9열(Col8) -> 8
                if (colIndex >= 1 && colIndex <= 8)
                {
                    var cm = new ContextMenu();
                    var miEdit = new MenuItem { Header = "✏️ 컬럼 헤더명 변경..." };
                    miEdit.Click += (s, ev) => PromptChangeColumnHeader(colIndex);

                    var miDel = new MenuItem 
                    { 
                        Header = "🗑️ 이 컬럼 삭제", 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")) 
                    };
                    miDel.Click += (s, ev) => ViewModel?.RemoveColumnAt(colIndex);

                    cm.Items.Add(miEdit);
                    cm.Items.Add(new Separator());
                    cm.Items.Add(miDel);

                    cm.PlacementTarget = header;
                    cm.IsOpen = true;
                    e.Handled = true;
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                if (current is ContextMenu cm) current = cm.PlacementTarget;
                else if (current is Visual) current = VisualTreeHelper.GetParent(current);
                else break;
            }
            return null;
        }

        private void PromptChangeColumnHeader(int colIndex)
        {
            if (ViewModel == null) return;
            string current = ViewModel.GetHeaderByIndex(colIndex);
            var dlg = new HeaderEditDialog(colIndex, current)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                string newHeader = dlg.HeaderResult;
                if (!string.IsNullOrWhiteSpace(newHeader) && newHeader != current)
                {
                    ViewModel.SetColumnHeaderByIndex(colIndex, newHeader);
                    UpdateDataGridColumnsVisibility(ViewModel.ActiveColumnCount);
                }
            }
        }

        public void FocusFirstInput()
        {
            FocusControlByName("TxtCol1");
        }

        private void FocusControlByName(string name)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (FindName(name) is TextBox tb)
                {
                    tb.Focus();
                    tb.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// 동적 활성 컬럼(1~8개) 입력 TextBox 탭/엔터키 네비게이션 & 마지막 컬럼 동작(기록+인쇄 또는 기록만)
        /// </summary>
        private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;

                if (sender is TextBox currentTb && currentTb.Tag != null)
                {
                    if (int.TryParse(currentTb.Tag.ToString(), out int index))
                    {
                        int activeCount = ViewModel != null ? ViewModel.ActiveColumnCount : 5;
                        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                        if (isShift)
                        {
                            // Shift + Tab: 이전 활성 컬럼으로 이동 (순환)
                            int prevIndex = (index > 0) ? index - 1 : activeCount - 1;
                            FocusControlByName($"TxtCol{prevIndex + 1}");
                        }
                        else
                        {
                            // Tab: 다음 활성 컬럼으로 이동 (마지막이면 1번으로 순환)
                            int nextIndex = (index < activeCount - 1) ? index + 1 : 0;
                            FocusControlByName($"TxtCol{nextIndex + 1}");
                        }
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                bool isShiftOrCtrl = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ||
                                     (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                if (isShiftOrCtrl)
                {
                    // Shift + Enter 또는 Ctrl + Enter: 현재 커서 위치에 줄바꿈(개행) 삽입
                    if (sender is TextBox tb)
                    {
                        int caret = tb.CaretIndex;
                        tb.SelectedText = Environment.NewLine;
                        tb.CaretIndex = caret + Environment.NewLine.Length;
                        e.Handled = true;
                        return;
                    }
                }

                e.Handled = true;

                if (sender is TextBox currentTb && currentTb.Tag != null)
                {
                    if (int.TryParse(currentTb.Tag.ToString(), out int index))
                    {
                        int activeCount = ViewModel != null ? ViewModel.ActiveColumnCount : 5;
                        
                        // 현재 컬럼이 마지막 활성 컬럼인 경우
                        if (index >= activeCount - 1)
                        {
                            if (ViewModel != null)
                            {
                                ViewModel.ExecutePrintOnly();
                            }
                        }
                        else
                        {
                            // 다음 활성 컬럼으로 포커스 이동
                            int nextIndex = index + 1;
                            FocusControlByName($"TxtCol{nextIndex + 1}");
                        }
                    }
                }
            }
        }

        #region WYSIWYG Interactive Canvas Designer (우측 전역 미리보기 & 탭2 캔버스 동시 렌더링 & 드래그)

        private const double MM_TO_PX = 96.0 / 25.4; // 표준 96 DPI WPF Device Independent Pixels per mm (3.779527559)

        public void RedrawInteractiveCanvas()
        {
            if (ViewModel == null) return;

            RenderCanvasContent(GlobalCanvas, true);
            RenderCanvasContent(ExcelCanvas, true);
            RenderCanvasContent(DragCanvas, false);
        }

        private void RenderCanvasContent(Canvas targetCanvas, bool isGlobal)
        {
            if (targetCanvas == null || ViewModel == null) return;

            targetCanvas.Children.Clear();

            int wMm = ViewModel.EffectivePaperWidth > 0 ? ViewModel.EffectivePaperWidth : 70;
            int hMm = ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : 28;

            // 캔버스의 1:1 절대 해상도 설정 (1mm = 4.5px)
            targetCanvas.Width = wMm * MM_TO_PX;
            targetCanvas.Height = hMm * MM_TO_PX;

            // 캔버스 전체에 균일한 벡터 스케일 변환(ScaleTransform) 적용 -> 줌에 따른 위치 왜곡 100% 제거
            targetCanvas.LayoutTransform = new ScaleTransform(ViewModel.ZoomRatio, ViewModel.ZoomRatio);

            // 1. 라벨 용지 외곽선 사각형 및 배경
            var borderRect = new Rectangle
            {
                Width = targetCanvas.Width,
                Height = targetCanvas.Height,
                Stroke = Brushes.Gray,
                StrokeThickness = 1.2,
                Fill = Brushes.White
            };
            targetCanvas.Children.Add(borderRect);

            // 1-1. 표 위 제목 (Title) 실시간 렌더링
            double startXMm = (ViewModel.TableOffsetX >= 0 ? ViewModel.TableOffsetX : 2.0);
            double startYMm = (ViewModel.TableOffsetY >= 0 ? ViewModel.TableOffsetY : 2.0);

            if (ViewModel.IsTableTitleEnabled && !string.IsNullOrWhiteSpace(ViewModel.TableTitleText))
            {
                double tableW = (ViewModel.TableWidth > 0 ? ViewModel.TableWidth : (wMm - (startXMm * 2)));
                double titleY = startYMm + ViewModel.TableTitleOffsetY;
                AddWysiwygItem(targetCanvas, 100, ViewModel.TableTitleText, "제목", false, false, false, "NONE", true, ViewModel.TableTitleSize, startXMm + ViewModel.TableTitleOffsetX, titleY, tableW, 0, ViewModel.TableTitleAlign, ViewModel.TableTitleFont, ViewModel.TableTitleBold, ViewModel.TableTitleItalic);

                startYMm += Math.Max(4.0, ViewModel.TableTitleHeight) + ViewModel.TableTitleOffsetY;

                if (ViewModel.IsTableTitleUnderline)
                {
                    double titleLineYPx = startYMm * MM_TO_PX;
                    double lineWPx = tableW * MM_TO_PX;
                    var titleLine = new Line
                    {
                        X1 = startXMm * MM_TO_PX,
                        Y1 = titleLineYPx,
                        X2 = (startXMm * MM_TO_PX) + lineWPx,
                        Y2 = titleLineYPx,
                        Stroke = Brushes.Black,
                        StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                    };
                    targetCanvas.Children.Add(titleLine);

                    startYMm += 1.0;
                }

                startYMm += ViewModel.TableTitleSpacing;
            }

            int activeCount = Math.Max(1, Math.Min(8, ViewModel.ActiveColumnCount));

            // 바코드 컬럼 여부 확인 및 텍스트 컬럼 리스트 추출
            var textColIndices = new List<int>();
            var barcodeColIndices = new List<int>();
            for (int i = 0; i < activeCount; i++)
            {
                int colNum = i + 1;
                if (ViewModel.GetBarcodeByIndex(colNum)) barcodeColIndices.Add(colNum);
                else textColIndices.Add(colNum);
            }

            int textRowCount = Math.Max(1, textColIndices.Count);
            double tableX = startXMm * MM_TO_PX;
            double tableY = startYMm * MM_TO_PX;
            double defaultTableWMm = Math.Max(10, wMm - 4);
            double tableWMm = (ViewModel.TableWidth > 0 ? ViewModel.TableWidth : defaultTableWMm);
            double tableWPx = tableWMm * MM_TO_PX;

            // 표 기준 높이: 상단 제목 및 여백을 고려한 최대 가용 높이(availTableHMm)를 산출하여 인쇄영역을 벗어나지 않도록 보장
            double bottomMarginMm = ViewModel.IsDateTimeEnabled ? 6.0 : 2.0;
            double availTableHMm = Math.Max(6.0, hMm - startYMm - bottomMarginMm);

            double userSpecifiedTotalMm = 0;
            int autoCount = 0;
            for (int r = 0; r < textRowCount; r++)
            {
                int colNum = textColIndices[r];
                int userH = ViewModel.GetRowHeightByIndex(colNum);
                if (userH > 0) userSpecifiedTotalMm += userH;
                else autoCount++;
            }

            double baseTableHMm;
            if (ViewModel.TableHeight > 0)
            {
                baseTableHMm = Math.Min(ViewModel.TableHeight, availTableHMm);
            }
            else if (autoCount == 0 && userSpecifiedTotalMm > 0)
            {
                baseTableHMm = Math.Min(userSpecifiedTotalMm, availTableHMm);
            }
            else
            {
                baseTableHMm = availTableHMm;
            }

            double tableHPx = baseTableHMm * MM_TO_PX;

            // 행별 세로 높이(픽셀) 및 시작 Y좌표 계산 (개별 지정 높이 및 글자 크기 비례 자동 배분 지원)
            double[] rowHeightsPx = new double[textRowCount];
            double[] rowTopsPx = new double[textRowCount];
            double userSpecifiedTotalPx = userSpecifiedTotalMm * MM_TO_PX;

            if (autoCount > 0)
            {
                double remainingPx = Math.Max(autoCount * 4.0 * MM_TO_PX, tableHPx - userSpecifiedTotalPx);
                double totalWeight = 0;
                double[] weights = new double[textRowCount];
                for (int r = 0; r < textRowCount; r++)
                {
                    int colNum = textColIndices[r];
                    if (ViewModel.GetRowHeightByIndex(colNum) <= 0)
                    {
                        int sz = ViewModel.GetSizeByIndex(colNum);
                        weights[r] = Math.Max(6, sz);
                        totalWeight += weights[r];
                    }
                }
                for (int r = 0; r < textRowCount; r++)
                {
                    int colNum = textColIndices[r];
                    if (ViewModel.GetRowHeightByIndex(colNum) <= 0)
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

            // 1-2. 표 (Table / 격자 테두리) 실시간 미리보기 렌더링
            if (ViewModel.IsTableBorderEnabled)
            {
                string[] values = ViewModel.GetEffectivePreviewColumnValues();

                // [가로형 표 모드 (헤더 상단 / 열 단위 배치)]
                if (ViewModel.TableOrientation == 1)
                {
                    int colCount = Math.Max(1, textColIndices.Count);
                    double colWidthPx = tableWPx / colCount;
                    double colWidthMm = tableWMm / colCount;

                    // 헤더 표시 항목이 하나라도 있는지 확인
                    bool anyIncludeHeader = textColIndices.Any(c => ViewModel.GetIncludeHeaderByIndex(c));
                    double headerHMm = anyIncludeHeader ? Math.Max(3.0, Math.Min(ViewModel.TableHeaderHeightMm, baseTableHMm * 0.6)) : 0;
                    double headerHPx = headerHMm * MM_TO_PX;
                    double dataHMm = anyIncludeHeader ? Math.Max(3.0, baseTableHMm - headerHMm) : baseTableHMm;
                    double dataHPx = dataHMm * MM_TO_PX;
                    double totalTableHPx = baseTableHMm * MM_TO_PX;
                    double headerBottomY = tableY + headerHPx;

                    // 1. 외곽 박스
                    if (ViewModel.TableBorderStyle == 0 || ViewModel.TableBorderStyle == 1)
                    {
                        var tableBox = new Rectangle
                        {
                            Width = Math.Max(10, tableWPx),
                            Height = Math.Max(10, totalTableHPx),
                            Stroke = Brushes.Black,
                            StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness),
                            Fill = Brushes.Transparent
                        };
                        Canvas.SetLeft(tableBox, tableX);
                        Canvas.SetTop(tableBox, tableY);
                        targetCanvas.Children.Add(tableBox);
                    }

                    // 2. 가로 구분선 (전체 격자: 헤더 구분선 / 가로선만: 상단, 중간, 하단선)
                    if (ViewModel.TableBorderStyle == 0 && anyIncludeHeader)
                    {
                        var hLine = new Line
                        {
                            X1 = tableX,
                            Y1 = headerBottomY,
                            X2 = tableX + tableWPx,
                            Y2 = headerBottomY,
                            Stroke = Brushes.Black,
                            StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                        };
                        targetCanvas.Children.Add(hLine);
                    }
                    else if (ViewModel.TableBorderStyle == 2)
                    {
                        // 상단선
                        targetCanvas.Children.Add(new Line { X1 = tableX, Y1 = tableY, X2 = tableX + tableWPx, Y2 = tableY, Stroke = Brushes.Black, StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness) });
                        // 헤더 구분선
                        if (anyIncludeHeader)
                        {
                            targetCanvas.Children.Add(new Line { X1 = tableX, Y1 = headerBottomY, X2 = tableX + tableWPx, Y2 = headerBottomY, Stroke = Brushes.Black, StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness) });
                        }
                        // 하단선
                        targetCanvas.Children.Add(new Line { X1 = tableX, Y1 = tableY + totalTableHPx, X2 = tableX + tableWPx, Y2 = tableY + totalTableHPx, Stroke = Brushes.Black, StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness) });
                    }

                    // 3. 세로 구분선 (전체 격자 모드일 때 각 열 사이)
                    if (ViewModel.TableBorderStyle == 0)
                    {
                        for (int c = 1; c < colCount; c++)
                        {
                            double vx = tableX + (c * colWidthPx);
                            var vLine = new Line
                            {
                                X1 = vx,
                                Y1 = tableY,
                                X2 = vx,
                                Y2 = tableY + totalTableHPx,
                                Stroke = Brushes.Black,
                                StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                            };
                            targetCanvas.Children.Add(vLine);
                        }
                    }

                    // 4. 각 열별 텍스트 렌더링
                    for (int c = 0; c < colCount; c++)
                    {
                        int colNum = textColIndices[c];
                        int i = colNum - 1;
                        string val = values[i] ?? "";
                        string header = ViewModel.GetHeaderByIndex(colNum);
                        bool isPrice = ViewModel.GetPriceByIndex(colNum);
                        bool isIncludeHeader = ViewModel.GetIncludeHeaderByIndex(colNum);
                        int size = ViewModel.GetSizeByIndex(colNum);
                        string headerAlign = ViewModel.GetHeaderAlignByIndex(colNum);
                        string contentAlign = ViewModel.GetAlignByIndex(colNum);
                        string fontName = ViewModel.GetFontByIndex(colNum);
                        bool isBold = ViewModel.GetBoldByIndex(colNum);
                        bool isItalic = ViewModel.GetItalicByIndex(colNum);

                        double colLeftPx = tableX + (c * colWidthPx);
                        double colLeftMm = colLeftPx / MM_TO_PX;
                        double tableYMm = tableY / MM_TO_PX;
                        double dataTopMm = (tableY + headerHPx) / MM_TO_PX;

                        if (anyIncludeHeader)
                        {
                            // 상단 헤더 셀
                            if (isIncludeHeader)
                            {
                                AddWysiwygItem(targetCanvas, -colNum, header, header, false, false, false, "NONE", true, size, colLeftMm, tableYMm, colWidthMm, headerHMm, headerAlign, fontName, isBold, isItalic, 1.0);
                            }

                            // 하단 내용 셀
                            AddWysiwygItem(targetCanvas, i, val, header, isPrice, false, false, "NONE", true, size, colLeftMm, dataTopMm, colWidthMm, dataHMm, contentAlign, fontName, isBold, isItalic, 1.0);
                        }
                        else
                        {
                            // 헤더 없는 단일 행 셀
                            AddWysiwygItem(targetCanvas, i, val, header, isPrice, false, false, "NONE", true, size, colLeftMm, tableYMm, colWidthMm, baseTableHMm, contentAlign, fontName, isBold, isItalic, 1.0);
                        }
                    }

                    // 5. 가로형 표 상단 헤더선 드래그 조절 감지선
                    if (anyIncludeHeader)
                    {
                        var hHeaderHitLine = new Line
                        {
                            X1 = tableX,
                            Y1 = headerBottomY,
                            X2 = tableX + tableWPx,
                            Y2 = headerBottomY,
                            Stroke = Brushes.Transparent,
                            StrokeThickness = 8,
                            Tag = "H_HEADER_LINE",
                            Cursor = Cursors.SizeNS,
                            ToolTip = "가로선 드래그: 상단 헤더 높이 조절"
                        };
                        targetCanvas.Children.Add(hHeaderHitLine);
                    }
                }
                else
                {
                    // [기존 세로형 표 모드 (헤더 좌측 / 행 단위 배치)]
                    // 0: 전체 격자, 1: 외곽 박스
                    if (ViewModel.TableBorderStyle == 0 || ViewModel.TableBorderStyle == 1)
                    {
                        var tableBox = new Rectangle
                        {
                            Width = Math.Max(10, tableWPx),
                            Height = Math.Max(10, actualTableHPx),
                            Stroke = Brushes.Black,
                            StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness),
                            Fill = Brushes.Transparent
                        };
                        Canvas.SetLeft(tableBox, tableX);
                        Canvas.SetTop(tableBox, tableY);
                        targetCanvas.Children.Add(tableBox);
                    }

                    // 0: 전체 격자 (내부 가로선)
                    if (ViewModel.TableBorderStyle == 0)
                    {
                        for (int lineIdx = 1; lineIdx < textRowCount; lineIdx++)
                        {
                            double y = rowTopsPx[lineIdx];
                            var hLine = new Line
                            {
                                X1 = tableX,
                                Y1 = y,
                                X2 = tableX + tableWPx,
                                Y2 = y,
                                Stroke = Brushes.Black,
                                StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                            };
                            targetCanvas.Children.Add(hLine);
                        }
                    }
                    // 2: 가로 구분선만 (맨 위, 중간 구분선들, 맨 마지막 항목 아래까지 가로선 표시)
                    else if (ViewModel.TableBorderStyle == 2)
                    {
                        for (int lineIdx = 0; lineIdx <= textRowCount; lineIdx++)
                        {
                            double y = (lineIdx < textRowCount) ? rowTopsPx[lineIdx] : (rowTopsPx[textRowCount - 1] + rowHeightsPx[textRowCount - 1]);
                            var hLine = new Line
                            {
                                X1 = tableX,
                                Y1 = y,
                                X2 = tableX + tableWPx,
                                Y2 = y,
                                Stroke = Brushes.Black,
                                StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                            };
                            targetCanvas.Children.Add(hLine);
                        }
                    }

                    // 3. 텍스트 컬럼들만 순서대로 표 내부 각 행에 렌더링
                    for (int r = 0; r < textColIndices.Count; r++)
                    {
                        int colNum = textColIndices[r];
                        int i = colNum - 1;
                        string val = values[i] ?? "";
                        string header = ViewModel.GetHeaderByIndex(colNum);
                        bool isPrice = ViewModel.GetPriceByIndex(colNum);
                        bool isIncludeHeader = ViewModel.GetIncludeHeaderByIndex(colNum);
                        int size = ViewModel.GetSizeByIndex(colNum);
                        string headerAlign = ViewModel.GetHeaderAlignByIndex(colNum);
                        string contentAlign = ViewModel.GetAlignByIndex(colNum);
                        string fontName = ViewModel.GetFontByIndex(colNum);
                        bool isBold = ViewModel.GetBoldByIndex(colNum);
                        bool isItalic = ViewModel.GetItalicByIndex(colNum);

                        double rowTop = rowTopsPx[r];
                        double currentRowHPx = rowHeightsPx[r];
                        double rowBottom = rowTop + currentRowHPx;
                        double headerLineX = tableX + (ViewModel.TableHeaderWidthMm * MM_TO_PX);

                        double textHeightMm = size * 0.3527;
                        double rowHMm = currentRowHPx / MM_TO_PX;
                        double rowTopMm = rowTop / MM_TO_PX;

                        if (isIncludeHeader)
                        {
                            // 전체 격자(0) 모드일 때만 제목과 내용 사이 세로 구분선 그리기
                            if (ViewModel.IsTableBorderEnabled && ViewModel.TableBorderStyle == 0)
                            {
                                var vLine = new Line
                                {
                                    X1 = headerLineX,
                                    Y1 = rowTop,
                                    X2 = headerLineX,
                                    Y2 = rowBottom,
                                    Stroke = Brushes.Black,
                                    StrokeThickness = Math.Max(1, ViewModel.TableBorderThickness)
                                };
                                targetCanvas.Children.Add(vLine);
                            }

                            // 1. 좌측 헤더 셀 (정확한 너비와 셀 높이, TextAlignment 및 세로 중앙 정렬)
                            double headerWidthMm = ViewModel.TableHeaderWidthMm;
                            AddWysiwygItem(targetCanvas, -colNum, header, header, false, false, false, "NONE", true, size, (tableX / MM_TO_PX), rowTopMm, headerWidthMm, rowHMm, headerAlign, fontName, isBold, isItalic, 1.5);

                            // 2. 우측 내용 셀 (정확한 너비와 셀 높이, TextAlignment 및 세로 중앙 정렬)
                            double contentCellWidthMm = Math.Max(5.0, tableWMm - ViewModel.TableHeaderWidthMm);
                            AddWysiwygItem(targetCanvas, i, val, header, isPrice, false, false, "NONE", true, size, (headerLineX / MM_TO_PX), rowTopMm, contentCellWidthMm, rowHMm, contentAlign, fontName, isBold, isItalic, 1.5);
                        }
                        else
                        {
                            // 텍스트 단독 (제목표시 미체크: 전체 표 너비 기준 정렬 및 1.5mm 내부 여백 적용)
                            AddWysiwygItem(targetCanvas, i, val, header, isPrice, false, false, "NONE", true, size, (tableX / MM_TO_PX), rowTopMm, tableWMm, rowHMm, contentAlign, fontName, isBold, isItalic, 1.5);
                        }
                    }

                    // 3-1. 표 인터랙티브 조절 라인 및 핸들 (세로형 모드 전용 내부 가로선 조절)
                    for (int lineIdx = 1; lineIdx < textRowCount; lineIdx++)
                    {
                        double y = rowTopsPx[lineIdx];
                        var hHitLine = new Line
                        {
                            X1 = tableX,
                            Y1 = y,
                            X2 = tableX + tableWPx,
                            Y2 = y,
                            Stroke = Brushes.Transparent,
                            StrokeThickness = 7,
                            Cursor = Cursors.SizeNS,
                            Tag = $"ROW_LINE_{lineIdx}"
                        };
                        targetCanvas.Children.Add(hHitLine);
                    }
                }

                // (2) 제목 열 너비 세로선 조절 핸들 (세로형 표 모드 && 제목 표시가 켜져 있을 때)
                if (ViewModel.TableOrientation == 0)
                {
                    bool anyIncludeHeader = false;
                    for (int r = 0; r < textColIndices.Count; r++)
                    {
                        if (ViewModel.GetIncludeHeaderByIndex(textColIndices[r])) { anyIncludeHeader = true; break; }
                    }
                    if (anyIncludeHeader)
                    {
                        double headerLineX = tableX + (ViewModel.TableHeaderWidthMm * MM_TO_PX);
                        var vHitLine = new Line
                        {
                            X1 = headerLineX,
                            Y1 = tableY,
                            X2 = headerLineX,
                            Y2 = tableY + actualTableHPx,
                            Stroke = Brushes.Transparent,
                            StrokeThickness = 8,
                            Tag = "V_HEADER_LINE",
                            Cursor = Cursors.SizeWE,
                            ToolTip = "세로선 드래그: 제목 열 너비 조절"
                        };
                        targetCanvas.Children.Add(vHitLine);
                    }
                }

                // (3) 표 오른쪽 외곽선 (표 폭 조절)
                var rightHitLine = new Line
                {
                    X1 = tableX + tableWPx,
                    Y1 = tableY,
                    X2 = tableX + tableWPx,
                    Y2 = tableY + actualTableHPx,
                    Stroke = Brushes.Transparent,
                    StrokeThickness = 8,
                    Tag = "BORDER_RIGHT",
                    Cursor = Cursors.SizeWE,
                    ToolTip = "오른쪽 테두리 드래그: 표 전체 너비 조절"
                };
                targetCanvas.Children.Add(rightHitLine);

                // (4) 표 아래쪽 외곽선 (표 높이 조절)
                var bottomHitLine = new Line
                {
                    X1 = tableX,
                    Y1 = tableY + actualTableHPx,
                    X2 = tableX + tableWPx,
                    Y2 = tableY + actualTableHPx,
                    Stroke = Brushes.Transparent,
                    StrokeThickness = 8,
                    Tag = "BORDER_BOTTOM",
                    Cursor = Cursors.SizeNS,
                    ToolTip = "하단 테두리 드래그: 표 전체 높이 조절"
                };
                targetCanvas.Children.Add(bottomHitLine);

                // 4. 바코드 컬럼들은 표 외부 독립 렌더링
                for (int b = 0; b < barcodeColIndices.Count; b++)
                {
                    int colNum = barcodeColIndices[b];
                    int i = colNum - 1;
                    string val = values[i] ?? "";
                    string header = ViewModel.GetHeaderByIndex(colNum);
                    string bType = ViewModel.GetBarcodeTypeByIndex(colNum);
                    bool showBText = ViewModel.GetBarcodeTextByIndex(colNum);
                    int size = ViewModel.GetSizeByIndex(colNum);
                    int offX = ViewModel.GetOffsetXByIndex(colNum);
                    int offY = ViewModel.GetOffsetYByIndex(colNum);
                    string fontName = ViewModel.GetFontByIndex(colNum);
                    bool isBold = ViewModel.GetBoldByIndex(colNum);
                    bool isItalic = ViewModel.GetItalicByIndex(colNum);
                    AddWysiwygItem(targetCanvas, i, val, header, false, false, true, bType, showBText, size, offX, offY, 0, 0, "LEFT", fontName, isBold, isItalic, 0);
                }
            }
            else
            {
                // 표 비활성화 모드 (자유 레이아웃)
                string[] values = ViewModel.GetEffectivePreviewColumnValues();
                for (int i = 0; i < activeCount; i++)
                {
                    int colNum = i + 1;
                    string val = values[i] ?? "";
                    string header = ViewModel.GetHeaderByIndex(colNum);
                    bool isPrice = ViewModel.GetPriceByIndex(colNum);
                    bool isIncludeHeader = ViewModel.GetIncludeHeaderByIndex(colNum);
                    bool isBarcode = ViewModel.GetBarcodeByIndex(colNum);
                    string bType = ViewModel.GetBarcodeTypeByIndex(colNum);
                    bool showBText = ViewModel.GetBarcodeTextByIndex(colNum);
                    int size = ViewModel.GetSizeByIndex(colNum);
                    int offX = ViewModel.GetOffsetXByIndex(colNum);
                    int offY = ViewModel.GetOffsetYByIndex(colNum);
                    string align = ViewModel.GetAlignByIndex(colNum);
                    string fontName = ViewModel.GetFontByIndex(colNum);
                    bool isBold = ViewModel.GetBoldByIndex(colNum);
                    bool isItalic = ViewModel.GetItalicByIndex(colNum);
                    int userWMm = ViewModel.GetWidthByIndex(colNum);
                    AddWysiwygItem(targetCanvas, i, val, header, isPrice, isIncludeHeader, isBarcode, bType, showBText, size, offX, offY, userWMm, 0, align, fontName, isBold, isItalic, 0);
                }
            }

            // 4. 날짜-시간 항목 렌더링
            if (ViewModel.IsDateTimeEnabled)
            {
                string dtStr = ViewModel.GetFormattedDateTimeString();
                AddWysiwygItem(targetCanvas, 99, dtStr, "날짜시간", false, false, false, "NONE", true, ViewModel.DateTimeSize, ViewModel.DateTimeOffsetX, ViewModel.DateTimeOffsetY, ViewModel.DateTimeWidth, 0, "LEFT", ViewModel.DateTimeFont, ViewModel.DateTimeBold, ViewModel.DateTimeItalic, 0);
            }

            // 5. 순번 (카운터/일련번호) 렌더링
            if (ViewModel.IsSerialEnabled)
            {
                string serialStr = ViewModel.FormattedSerialPreview;
                AddWysiwygItem(targetCanvas, 101, serialStr, "순번", false, false, false, "NONE", true, ViewModel.SerialSize, ViewModel.SerialOffsetX, ViewModel.SerialOffsetY, ViewModel.SerialWidth, 0, ViewModel.SerialAlign, ViewModel.SerialFont, ViewModel.SerialBold, ViewModel.SerialItalic, 0);
            }
        }

        private void AddWysiwygItem(Canvas targetCanvas, int index, string rawText, string headerName, bool isPrice, bool isIncludeHeader, bool isBarcode, string barcodeType, bool showBarcodeText, int size, double xMm, double yMm, double widthMm = 0, double heightMm = 0, string align = "LEFT", string fontName = "맑은 고딕", bool isBold = true, bool isItalic = false, double cellPaddingMm = 0)
        {
            double left = xMm * MM_TO_PX;
            double top = yMm * MM_TO_PX;

            string displayVal = isPrice ? PriceFormatHelper.FormatShortPriceInput(rawText) : rawText;
            if (string.IsNullOrEmpty(displayVal))
            {
                displayVal = (isIncludeHeader && !isBarcode && index >= 0 && index != 99 && index != 101) ? $"{headerName}: [{headerName}]" : $"[{headerName}]";
            }
            else if (isIncludeHeader && !isBarcode && index >= 0 && index != 99 && index != 101)
            {
                displayVal = $"{headerName}: {displayVal}";
            }

            string renderedText = (index == 99) ? $"🕒 {displayVal}" : ((index == 101) ? $"🔢 {displayVal}" : displayVal);
            bool isSelected = _selectedItemIndices.Contains(index) || (_selectedItemIndex == index);

            double exactTextWMm = (ViewModel != null && !isBarcode) 
                ? (ViewModel.CalculateExactTextWidthMm(renderedText, fontName, size, isBold, isItalic) + 3.0) 
                : 20.0;
            double effectiveItemWMm = (widthMm > 0) ? widthMm : exactTextWMm;
            double effectiveItemWPx = effectiveItemWMm * MM_TO_PX;

            var container = new Border
            {
                Background = isSelected ? new SolidColorBrush(Color.FromArgb(60, 37, 99, 235)) : new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
                BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : Brushes.Transparent,
                BorderThickness = new Thickness(isSelected ? 2.0 : 1.0),
                CornerRadius = new CornerRadius(2),
                Padding = cellPaddingMm > 0 ? new Thickness(cellPaddingMm * MM_TO_PX, 0, cellPaddingMm * MM_TO_PX, 0) : new Thickness(0),
                Cursor = (index >= 0 || index == 100 || index == 101 || index <= -1) ? Cursors.SizeAll : Cursors.Arrow,
                Tag = index
            };

            if (isBarcode)
            {
                if (widthMm > 0) container.Width = widthMm * MM_TO_PX;
            }
            else
            {
                // 텍스트 박스의 너비를 계산된 실제 폭(또는 사용자 지정 폭)과 100% 일치시켜 조절 막대와 테두리가 완벽 밀착되도록 함
                container.Width = Math.Max(10.0, effectiveItemWPx);
            }

            if (heightMm > 0)
            {
                container.Height = heightMm * MM_TO_PX;
            }

            if (isBarcode)
            {
                var barcodeVisual = BarcodeVisualHelper.CreateBarcodeVisual(displayVal, barcodeType, size, isSelected, showBarcodeText);
                container.Child = barcodeVisual;
            }
            else
            {
                var textAlignment = TextAlignment.Left;
                if (align == "CENTER") textAlignment = TextAlignment.Center;
                else if (align == "RIGHT") textAlignment = TextAlignment.Right;

                var textBlock = new TextBlock
                {
                    Text = renderedText,
                    FontFamily = new FontFamily(string.IsNullOrWhiteSpace(fontName) ? "맑은 고딕" : fontName),
                    FontSize = size * 1.35,
                    Foreground = (index == 99) ? Brushes.DarkBlue : ((index == 101) ? Brushes.DarkGreen : Brushes.Black),
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal,
                    TextAlignment = textAlignment,
                    TextWrapping = (widthMm > 0) ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };
                container.Child = textBlock;
            }

            Canvas.SetLeft(container, left);
            Canvas.SetTop(container, top);

            targetCanvas.Children.Add(container);

            // 선택된 텍스트 아이템 우측 크기 조절 핸들 추가:
            // 1) 날짜/시간(99), 순번(101): 표 모드 여부와 상관없이 항상 독립 크기 조절 가능
            // 2) 일반 컬럼(0~7): 자유 배치 모드(!IsTableBorderEnabled)일 때 크기 조절 가능
            bool canResizeWidth = isSelected && !isBarcode && ViewModel != null && 
                                  ((index == 99 || index == 101) || (index >= 0 && index < 8 && !ViewModel.IsTableBorderEnabled));

            if (canResizeWidth)
            {
                int colNum = (index == 99 || index == 101) ? index : index + 1;
                string handleToolTip = (index == 99) 
                    ? "드래그: 날짜/시간 박스 너비 조절" 
                    : ((index == 101) ? "드래그: 순번 박스 너비 조절" : $"드래그: {colNum}열 박스 너비(자동 줄바꿈 폭) 조절");
                
                var resizeHandle = new Border
                {
                    Width = 8,
                    Height = 20,
                    Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1.2),
                    CornerRadius = new CornerRadius(3),
                    Cursor = Cursors.SizeWE,
                    Tag = $"RESIZE_ITEM_WIDTH_{colNum}",
                    ToolTip = handleToolTip
                };

                // 박스 우측 테두리에 완벽 밀착
                Canvas.SetLeft(resizeHandle, left + effectiveItemWPx - 4);
                Canvas.SetTop(resizeHandle, top + Math.Max(0, (size * 1.35 * 1.2) / 2 - 10));
                Panel.SetZIndex(resizeHandle, 100);
                targetCanvas.Children.Add(resizeHandle);
            }
        }

        #region Mouse Drag & Wheel Handlers for Both Canvases

        private int GetTextColNumByIndex(int textRowIdx)
        {
            if (ViewModel == null) return 1;
            int activeCount = Math.Max(1, Math.Min(8, ViewModel.ActiveColumnCount));
            var textCols = new List<int>();
            for (int i = 0; i < activeCount; i++)
            {
                int colNum = i + 1;
                if (!ViewModel.GetBarcodeByIndex(colNum)) textCols.Add(colNum);
            }
            if (textRowIdx >= 0 && textRowIdx < textCols.Count) return textCols[textRowIdx];
            return textRowIdx + 1;
        }

        private void UpdateCustomDragInfo(string message)
        {
            if (DragInfoBlock != null) DragInfoBlock.Text = message;
            if (GlobalDragInfoBlock != null) GlobalDragInfoBlock.Text = message;
        }

        private void StartCanvasDrag(Canvas canvas, MouseButtonEventArgs e)
        {
            if (canvas == null || ViewModel == null) return;

            try
            {
                var hit = e.OriginalSource as DependencyObject;
                FrameworkElement hitElement = null;
                while (hit != null && hit != canvas)
                {
                    if (hit is FrameworkElement fe && fe.Tag != null)
                    {
                        hitElement = fe;
                        break;
                    }
                    hit = VisualTreeHelper.GetParent(hit);
                }

                string tagStr = hitElement?.Tag?.ToString() ?? "";

                // 1. 표 전용 라인 및 테두리 조작 인터랙션 검사
                if (tagStr == "V_HEADER_LINE")
                {
                    _currentDragMode = TableDragMode.ResizeHeaderCol;
                    _startHeaderWidthMm = ViewModel.TableHeaderWidthMm;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[제목 열 너비 조절] {ViewModel.TableHeaderWidthMm} mm");
                    e.Handled = true;
                    return;
                }
                else if (tagStr == "H_HEADER_LINE")
                {
                    _currentDragMode = TableDragMode.ResizeHeaderHeight;
                    _startHeaderHeightMm = ViewModel.TableHeaderHeightMm;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[헤더 행 높이 조절] {ViewModel.TableHeaderHeightMm} mm");
                    e.Handled = true;
                    return;
                }
                else if (tagStr.StartsWith("H_LINE_"))
                {
                    _currentDragMode = TableDragMode.ResizeRowSeparator;
                    _resizingRowIndex = int.Parse(tagStr.Substring(7));
                    int targetColNum = GetTextColNumByIndex(_resizingRowIndex);
                    int curH = ViewModel.GetRowHeightByIndex(targetColNum);
                    _startSpecificRowHeightMm = curH > 0 ? curH : 10;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[{targetColNum}열 높이 조절] {_startSpecificRowHeightMm} mm");
                    e.Handled = true;
                    return;
                }
                else if (tagStr == "BORDER_RIGHT")
                {
                    _currentDragMode = TableDragMode.ResizeRightBorder;
                    _startTableWidthMm = ViewModel.TableWidth > 0 ? ViewModel.TableWidth : Math.Max(10, ViewModel.PaperWidth - 4);
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[표 전체 너비 조절] {_startTableWidthMm} mm");
                    e.Handled = true;
                    return;
                }
                else if (tagStr == "BORDER_BOTTOM")
                {
                    _currentDragMode = TableDragMode.ResizeBottomBorder;
                    int startY = ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2;
                    _startTableHeightMm = ViewModel.TableHeight > 0 ? ViewModel.TableHeight : Math.Max(10, (ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : ViewModel.PaperHeight) - startY - 2);
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[표 전체 높이 조절] {_startTableHeightMm} mm");
                    e.Handled = true;
                    return;
                }
                else if (tagStr.StartsWith("RESIZE_ITEM_WIDTH_"))
                {
                    _currentDragMode = TableDragMode.ResizeItemWidth;
                    _resizingColNum = int.Parse(tagStr.Substring(18));
                    int curW = ViewModel.GetWidthByIndex(_resizingColNum);
                    if (curW > 0)
                    {
                        _startItemWidthMm = curW;
                    }
                    else
                    {
                        string displayText = ViewModel.GetPreviewDisplayTextForColumn(_resizingColNum);
                        string fontName = ViewModel.GetFontByIndex(_resizingColNum);
                        int size = ViewModel.GetSizeByIndex(_resizingColNum);
                        bool isBold = ViewModel.GetBoldByIndex(_resizingColNum);
                        bool isItalic = ViewModel.GetItalicByIndex(_resizingColNum);
                        _startItemWidthMm = Math.Max(10, Math.Round(ViewModel.CalculateExactTextWidthMm(displayText, fontName, size, isBold, isItalic)));
                    }
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    canvas.CaptureMouse();
                    string name = (_resizingColNum == 99) ? "날짜/시간" : ((_resizingColNum == 101) ? "순번" : $"{_resizingColNum}열");
                    UpdateCustomDragInfo($"[{name} 박스 너비 조절] {_startItemWidthMm:F0} mm");
                    e.Handled = true;
                    return;
                }

                // 2. 일반 아이템 및 표 내부 셀 (위치 이동)
                bool isMultiKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (hitElement != null && int.TryParse(tagStr, out int rawIndex))
                {
                    _currentDragMode = TableDragMode.MoveItemOrTable;
                    int index = rawIndex;
                    if (index < 0 && index >= -8)
                    {
                        index = Math.Abs(index) - 1; // 헤더 셀(-1~-8) 클릭 시 해당 컬럼 인덱스(0~7)로 매핑
                    }

                    if (isMultiKey)
                    {
                        // Ctrl/Shift 다중 선택 토글
                        if (_selectedItemIndices.Contains(index))
                        {
                            _selectedItemIndices.Remove(index);
                        }
                        else
                        {
                            _selectedItemIndices.Add(index);
                        }
                        _selectedItemIndex = _selectedItemIndices.Count > 0 ? _selectedItemIndices.First() : -999;
                        ViewModel.SelectedItemIndex = _selectedItemIndex;
                    }
                    else
                    {
                        // 일반 클릭: 이미 다중 선택된 그룹 내부를 누른 것이 아니라면 단일 선택으로 교체
                        if (!_selectedItemIndices.Contains(index))
                        {
                            _selectedItemIndices.Clear();
                            _selectedItemIndices.Add(index);
                        }
                        _selectedItemIndex = index;
                        ViewModel.SelectedItemIndex = index;
                    }

                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);

                    // 다중 선택된 모든 항목의 시작 위치(StartOffset) 저장
                    _startOffsets.Clear();
                    foreach (int selIdx in _selectedItemIndices)
                    {
                        bool isSelBarcode = (selIdx >= 0 && selIdx < 8) && ViewModel.GetBarcodeByIndex(selIdx + 1);
                        if (ViewModel.IsTableBorderEnabled && selIdx >= 0 && selIdx < 8 && !isSelBarcode)
                        {
                            _startOffsets[selIdx] = new Point(ViewModel.TableOffsetX > 0 ? ViewModel.TableOffsetX : 2, ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2);
                        }
                        else
                        {
                            GetItemOffset(selIdx, out int sx, out int sy);
                            _startOffsets[selIdx] = new Point(sx, sy);
                        }
                    }

                    bool isBarcodeCol = (index >= 0 && index < 8) && ViewModel.GetBarcodeByIndex(index + 1);
                    if (ViewModel.IsTableBorderEnabled && index >= 0 && index < 8 && !isBarcodeCol)
                    {
                        _startOffsetX = ViewModel.TableOffsetX > 0 ? ViewModel.TableOffsetX : 2;
                        _startOffsetY = ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2;

                        // 위치 이동 시 표의 현재 크기가 유지되도록 확정
                        if (ViewModel.TableWidth <= 0)
                        {
                            ViewModel.TableWidth = Math.Max(10, ViewModel.PaperWidth - 4);
                        }
                    }
                    else
                    {
                        GetItemOffset(index, out _startOffsetX, out _startOffsetY);
                    }

                    canvas.CaptureMouse();
                    RedrawInteractiveCanvas();
                    UpdateDragInfoText();
                    e.Handled = true;
                }
                else
                {
                    // 빈 공간 클릭: 선택 해제
                    if (!isMultiKey)
                    {
                        _selectedItemIndices.Clear();
                        _selectedItemIndex = -999;
                        ViewModel.SelectedItemIndex = -999;
                        _currentDragMode = TableDragMode.None;
                        RedrawInteractiveCanvas();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartCanvasDrag Error: {ex.Message}");
            }
        }

        private void ProcessCanvasDrag(Canvas canvas, MouseEventArgs e)
        {
            if (!_isDragging || ViewModel == null || _activeDragCanvas != canvas) return;

            try
            {
                Point currentPoint = e.GetPosition(canvas);
                double diffXMm = (currentPoint.X - _dragStartPoint.X) / MM_TO_PX;
                double diffYMm = (currentPoint.Y - _dragStartPoint.Y) / MM_TO_PX;

                int effW = ViewModel.EffectivePaperWidth > 0 ? ViewModel.EffectivePaperWidth : ViewModel.PaperWidth;
                int effH = ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : ViewModel.PaperHeight;
                int maxX = Math.Max(10, effW - 2);
                int maxY = Math.Max(10, effH - 2);

                switch (_currentDragMode)
                {
                    case TableDragMode.ResizeItemWidth:
                        {
                            int offX = ViewModel.GetOffsetXByIndex(_resizingColNum);
                            int maxAvailableWMm = Math.Max(5, effW - offX);
                            int newW = (int)Math.Max(5, Math.Min(maxAvailableWMm, Math.Round(_startItemWidthMm + diffXMm)));
                            if (ViewModel.GetWidthByIndex(_resizingColNum) != newW)
                            {
                                ViewModel.SetWidthByIndex(_resizingColNum, newW);
                                RedrawInteractiveCanvas();
                            }
                            string colDesc = (_resizingColNum == 99) ? "날짜/시간" : ((_resizingColNum == 101) ? "순번" : $"{_resizingColNum}열");
                            UpdateCustomDragInfo($"[{colDesc} 박스 너비] {newW} mm (줄바꿈 폭)");
                        }
                        break;
                    case TableDragMode.ResizeHeaderCol:
                        {
                            double curTableWMm = ViewModel.TableWidth > 0 ? ViewModel.TableWidth : Math.Max(10, effW - 4);
                            int newW = (int)Math.Max(3, Math.Min(curTableWMm - 5, Math.Round(_startHeaderWidthMm + diffXMm)));
                            if (ViewModel.TableHeaderWidthMm != newW)
                            {
                                ViewModel.TableHeaderWidthMm = newW;
                                RedrawInteractiveCanvas();
                            }
                            UpdateCustomDragInfo($"[제목 열 너비] {newW} mm");
                        }
                        break;

                    case TableDragMode.ResizeHeaderHeight:
                        {
                            int startY = ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2;
                            double curTableHMm = ViewModel.TableHeight > 0 ? ViewModel.TableHeight : Math.Max(10, effH - startY - 4);
                            int newH = (int)Math.Max(3, Math.Min(curTableHMm - 3, Math.Round(_startHeaderHeightMm + diffYMm)));
                            if (ViewModel.TableHeaderHeightMm != newH)
                            {
                                ViewModel.TableHeaderHeightMm = newH;
                                RedrawInteractiveCanvas();
                            }
                            UpdateCustomDragInfo($"[헤더 행 높이] {newH} mm");
                        }
                        break;

                    case TableDragMode.ResizeRowSeparator:
                        {
                            int targetColNum = GetTextColNumByIndex(_resizingRowIndex);
                            int newH = (int)Math.Max(3, Math.Min(maxY, Math.Round(_startSpecificRowHeightMm + diffYMm)));
                            ViewModel.SetRowHeightByIndex(targetColNum, newH);
                            RedrawInteractiveCanvas();
                            UpdateCustomDragInfo($"[{targetColNum}열 높이] {newH} mm");
                        }
                        break;

                    case TableDragMode.ResizeRightBorder:
                        {
                            int startXMm = ViewModel.TableOffsetX > 0 ? ViewModel.TableOffsetX : 2;
                            int maxW = Math.Max(10, effW - startXMm);
                            int newW = (int)Math.Max(10, Math.Min(maxW, Math.Round(_startTableWidthMm + diffXMm)));
                            if (ViewModel.TableWidth != newW)
                            {
                                ViewModel.TableWidth = newW;
                                RedrawInteractiveCanvas();
                            }
                            UpdateCustomDragInfo($"[표 전체 너비] {newW} mm");
                        }
                        break;

                    case TableDragMode.ResizeBottomBorder:
                        {
                            int startYMm = ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2;
                            int maxH = Math.Max(10, effH - startYMm);
                            int newH = (int)Math.Max(10, Math.Min(maxH, Math.Round(_startTableHeightMm + diffYMm)));
                            if (ViewModel.TableHeight != newH)
                            {
                                ViewModel.TableHeight = newH;
                                RedrawInteractiveCanvas();
                            }
                            UpdateCustomDragInfo($"[표 전체 높이] {newH} mm");
                        }
                        break;

                    case TableDragMode.MoveItemOrTable:
                        {
                            bool isBarcodeCol = (_selectedItemIndex >= 0 && _selectedItemIndex < 8) && ViewModel.GetBarcodeByIndex(_selectedItemIndex + 1);
                            if (ViewModel.IsTableBorderEnabled && _selectedItemIndex >= 0 && _selectedItemIndex < 8 && !isBarcodeCol)
                            {
                                // 표 모드: 표 내부 텍스트 셀 드래그 시 표 전체 위치 이동 (표 폭과 높이는 절대 변하지 않음!)
                                int targetXMm = Math.Max(0, Math.Min(maxX, (int)Math.Round(_startOffsetX + diffXMm)));
                                int targetYMm = Math.Max(0, Math.Min(maxY, (int)Math.Round(_startOffsetY + diffYMm)));
                                ViewModel.TableOffsetX = targetXMm;
                                ViewModel.TableOffsetY = targetYMm;
                                RedrawInteractiveCanvas();
                                UpdateCustomDragInfo($"[표 위치] X: {targetXMm}mm, Y: {targetYMm}mm");
                            }
                            else if (_selectedItemIndex == 100)
                            {
                                // 표 상단 제목 이동 (기존 정렬 유지)
                                int targetXMm = Math.Max(0, Math.Min(maxX, (int)Math.Round(_startOffsetX + diffXMm)));
                                int targetYMm = Math.Max(0, Math.Min(maxY, (int)Math.Round(_startOffsetY + diffYMm)));
                                double startXMm = ViewModel.TableOffsetX > 0 ? ViewModel.TableOffsetX : 2.0;
                                ViewModel.TableTitleOffsetX = (int)Math.Round(targetXMm - startXMm);
                                ViewModel.TableTitleOffsetY = Math.Max(0, targetYMm - (ViewModel.TableOffsetY > 0 ? ViewModel.TableOffsetY : 2));
                                RedrawInteractiveCanvas();
                                UpdateCustomDragInfo($"[표 제목 위치] X: {targetXMm}mm, Y: {targetYMm}mm");
                            }
                            else
                            {
                                // 바코드 또는 자유 배치 모드: 다중 선택된 모든 항목 일괄 동시 이동
                                foreach (var kvp in _startOffsets)
                                {
                                    int idx = kvp.Key;
                                    int targetX = Math.Max(0, Math.Min(maxX, (int)Math.Round(kvp.Value.X + diffXMm)));
                                    int targetY = Math.Max(0, Math.Min(maxY, (int)Math.Round(kvp.Value.Y + diffYMm)));

                                    ViewModel.SetItemOffsetDirect(idx, targetX, targetY, triggerPreview: false);
                                    UpdateCanvasElementPosition(GlobalCanvas, idx, targetX, targetY);
                                    UpdateCanvasElementPosition(ExcelCanvas, idx, targetX, targetY);
                                    UpdateCanvasElementPosition(DragCanvas, idx, targetX, targetY);
                                }
                                UpdateDragInfoText();
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessCanvasDrag Error: {ex.Message}");
            }
        }

        private void UpdateCanvasElementPosition(Canvas canvas, int itemIndex, int xMm, int yMm)
        {
            if (canvas == null) return;
            try
            {
                for (int i = 0; i < canvas.Children.Count; i++)
                {
                    if (canvas.Children[i] is FrameworkElement fe && fe.Tag != null && int.TryParse(fe.Tag.ToString(), out int tagIdx))
                    {
                        if (tagIdx == itemIndex || (tagIdx < 0 && Math.Abs(tagIdx) - 1 == itemIndex))
                        {
                            Canvas.SetLeft(fe, xMm * MM_TO_PX);
                            Canvas.SetTop(fe, yMm * MM_TO_PX);
                        }
                    }
                }
            }
            catch { }
        }

        private void EndCanvasDrag(Canvas canvas)
        {
            if (!_isDragging) return;

            try
            {
                _isDragging = false;
                _currentDragMode = TableDragMode.None;
                _activeDragCanvas = null;
                canvas?.ReleaseMouseCapture();

                if (ViewModel != null)
                {
                    bool isBarcodeCol = (_selectedItemIndex >= 0 && _selectedItemIndex < 8) && ViewModel.GetBarcodeByIndex(_selectedItemIndex + 1);
                    if (ViewModel.IsTableBorderEnabled && _selectedItemIndex >= 0 && _selectedItemIndex < 8 && !isBarcodeCol)
                    {
                        ViewModel.UpdatePreview();
                    }
                    else if (_selectedItemIndex == 100)
                    {
                        ViewModel.UpdatePreview();
                    }
                    else
                    {
                        foreach (int idx in _selectedItemIndices)
                        {
                            GetItemOffset(idx, out int x, out int y);
                            ViewModel.SetItemOffsetDirect(idx, x, y, triggerPreview: false);
                        }
                        ViewModel.UpdatePreview();
                    }
                    RedrawInteractiveCanvas();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EndCanvasDrag Error: {ex.Message}");
            }
        }

        // 1. 우측 전역 미리보기 캔버스 이벤트 (단건 / 엑셀 통합)
        private void OnGlobalCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => StartCanvasDrag(sender as Canvas ?? GlobalCanvas, e);
        private void OnGlobalCanvasMouseMove(object sender, MouseEventArgs e) => ProcessCanvasDrag(sender as Canvas ?? GlobalCanvas, e);
        private void OnGlobalCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndCanvasDrag(sender as Canvas ?? GlobalCanvas);
        private void OnGlobalCanvasMouseWheel(object sender, MouseWheelEventArgs e) => HandleCanvasZoomWheel(e);

        // 2. 탭2 내부 캔버스 이벤트
        private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => StartCanvasDrag(sender as Canvas ?? DragCanvas, e);
        private void OnCanvasMouseMove(object sender, MouseEventArgs e) => ProcessCanvasDrag(sender as Canvas ?? DragCanvas, e);
        private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndCanvasDrag(sender as Canvas ?? DragCanvas);
        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e) => HandleCanvasZoomWheel(e);

        private void HandleCanvasZoomWheel(MouseWheelEventArgs e)
        {
            if (ViewModel != null)
            {
                if (e.Delta > 0)
                {
                    ViewModel.ZoomRatio += 0.1;
                }
                else
                {
                    ViewModel.ZoomRatio -= 0.1;
                }
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
        }

        #endregion

        private void GetItemOffset(int index, out int x, out int y)
        {
            x = 0; y = 0;
            if (ViewModel == null) return;
            switch (index)
            {
                case 0: x = ViewModel.Col1OffsetX; y = ViewModel.Col1OffsetY; break;
                case 1: x = ViewModel.Col2OffsetX; y = ViewModel.Col2OffsetY; break;
                case 2: x = ViewModel.Col3OffsetX; y = ViewModel.Col3OffsetY; break;
                case 3: x = ViewModel.Col4OffsetX; y = ViewModel.Col4OffsetY; break;
                case 4: x = ViewModel.Col5OffsetX; y = ViewModel.Col5OffsetY; break;
                case 5: x = ViewModel.Col6OffsetX; y = ViewModel.Col6OffsetY; break;
                case 6: x = ViewModel.Col7OffsetX; y = ViewModel.Col7OffsetY; break;
                case 7: x = ViewModel.Col8OffsetX; y = ViewModel.Col8OffsetY; break;
                case 99: x = ViewModel.DateTimeOffsetX; y = ViewModel.DateTimeOffsetY; break;
                case 100: x = ViewModel.TableTitleOffsetX; y = ViewModel.TableTitleOffsetY; break;
                case 101: x = ViewModel.SerialOffsetX; y = ViewModel.SerialOffsetY; break;
            }
        }

        private void SetItemOffset(int index, int x, int y)
        {
            if (ViewModel == null) return;
            switch (index)
            {
                case 0: ViewModel.Col1OffsetX = x; ViewModel.Col1OffsetY = y; break;
                case 1: ViewModel.Col2OffsetX = x; ViewModel.Col2OffsetY = y; break;
                case 2: ViewModel.Col3OffsetX = x; ViewModel.Col3OffsetY = y; break;
                case 3: ViewModel.Col4OffsetX = x; ViewModel.Col4OffsetY = y; break;
                case 4: ViewModel.Col5OffsetX = x; ViewModel.Col5OffsetY = y; break;
                case 5: ViewModel.Col6OffsetX = x; ViewModel.Col6OffsetY = y; break;
                case 6: ViewModel.Col7OffsetX = x; ViewModel.Col7OffsetY = y; break;
                case 7: ViewModel.Col8OffsetX = x; ViewModel.Col8OffsetY = y; break;
                case 99: ViewModel.DateTimeOffsetX = x; ViewModel.DateTimeOffsetY = y; break;
                case 100: ViewModel.TableTitleOffsetX = x; ViewModel.TableTitleOffsetY = y; break;
                case 101: ViewModel.SerialOffsetX = x; ViewModel.SerialOffsetY = y; break;
            }
        }

        private void UpdateDragInfoText()
        {
            if (ViewModel == null) return;
            if (_selectedItemIndex >= 0)
            {
                bool isBarcode = (_selectedItemIndex < 8) && ViewModel.GetBarcodeByIndex(_selectedItemIndex + 1);
                string typeStr = isBarcode ? " [바코드]" : "";
                string name = (_selectedItemIndex == 99) ? "날짜-시간" : (_selectedItemIndex == 101 ? "순번" : $"컬럼 {_selectedItemIndex + 1} ({GetColumnHeader(_selectedItemIndex)}){typeStr}");
                GetItemOffset(_selectedItemIndex, out int x, out int y);
                string text = $"선택: [{name}] 위치: X={x}mm, Y={y}mm (드래그로 이동 중)";

                if (DragInfoBlock != null) DragInfoBlock.Text = text;
                if (GlobalDragInfoBlock != null) GlobalDragInfoBlock.Text = text;
            }
        }

        private string GetColumnHeader(int index)
        {
            if (ViewModel == null) return "";
            switch (index)
            {
                case 0: return ViewModel.Column1Header;
                case 1: return ViewModel.Column2Header;
                case 2: return ViewModel.Column3Header;
                case 3: return ViewModel.Column4Header;
                case 4: return ViewModel.Column5Header;
                case 5: return ViewModel.Column6Header;
                case 6: return ViewModel.Column7Header;
                case 7: return ViewModel.Column8Header;
                default: return "";
            }
        }

        #endregion
    }
}
