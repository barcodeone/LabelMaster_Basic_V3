using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Documents;
using BarcodePrint.Model;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    public class TableCellEditInfo
    {
        public TemplateElement Element { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }

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
            MoveTemplateElement,   // 자유 배치 템플릿 요소(텍스트, 바코드, QR, 선, 도형, 이미지) 직접 드래그 이동
            ResizeHeaderCol,       // 세로형 표 제목 열 너비 (TableHeaderWidthMm)
            ResizeHeaderHeight,    // 가로형 표 상단 헤더 행 높이 (TableHeaderHeightMm)
            ResizeRowSeparator,    // 특정 행 사이 가로선 (행 높이 조절)
            ResizeRightBorder,     // 표 전체 너비 (TableWidth)
            ResizeBottomBorder,    // 표 전체 높이 (TableHeight)
            ResizeItemWidth,       // 자유 배치 개별 항목 너비 (Col1Width ~ Col8Width)
            RotateElement,         // 텍스트/바코드/도형/선/이미지 마우스 드래그 회전 조절
            ResizeElementNW,       // 요소 좌상단 크기 조절
            ResizeElementN,        // 요소 상단 크기 조절
            ResizeElementNE,       // 요소 우상단 크기 조절
            ResizeElementE,        // 요소 우측 크기 조절 (너비)
            ResizeElementSE,       // 요소 우하단 크기 조절 (너비+높이, 텍스트 사이즈 비례)
            ResizeElementS,        // 요소 하단 크기 조절 (높이)
            ResizeElementSW,       // 요소 좌하단 크기 조절
            ResizeElementW,        // 요소 좌측 크기 조절
            ResizeLineStart,       // 선 시작점 조절
            ResizeLineEnd,         // 선 끝점 조절 (길이 및 회전)
            RubberBandSelect       // 빈 공간 마우스 드래그 영역 다중 선택 (Marquee)
        }

        private TableDragMode _currentDragMode = TableDragMode.None;
        private TemplateElement _selectedTemplateElement = null;
        private readonly Dictionary<TemplateElement, Point> _startTemplateElemOffsets = new Dictionary<TemplateElement, Point>();
        private System.Windows.Shapes.Rectangle _rubberBandRect = null;
        private double _startElemX;
        private double _startElemY;
        private double _startElemW;
        private double _startElemH;
        private double _startFontSize;
        private int _startRotation;

        private int _resizingRowIndex = -1;
        private int _resizingColNum = -1;
        private double _startHeaderWidthMm;
        private double _startHeaderHeightMm;
        private double _startTableWidthMm;
        private double _startTableHeightMm;
        private double _startSpecificRowHeightMm;
        private double _startItemWidthMm;

        private bool _isDragging = false;
        private bool _hasMovedDuringDrag = false;
        private int _selectedItemIndex = -1;
        private readonly HashSet<int> _selectedItemIndices = new HashSet<int>();
        private readonly Dictionary<int, Point> _startOffsets = new Dictionary<int, Point>();
        private Point _dragStartPoint;
        private int _startOffsetX;
        private int _startOffsetY;
        private Canvas _activeDragCanvas = null;

        public StartWindow DashboardWindow { get; set; }

        public WpfMainWindow()
        {
            InitializeComponentSafely();
            Loaded += WpfMainWindow_Loaded;
            Closing += (s, e) => ViewModel?.SaveSettingsSilently();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DashboardWindow != null && !DashboardWindow.IsVisible)
            {
                DashboardWindow.Close();
            }
        }

        public WpfMainWindow(double width, double height, string paperType, int rotation) : this()
        {
            if (ViewModel != null)
            {
                ViewModel.ApplyWizardSetup(width, height, paperType, rotation);
            }
            else
            {
                Loaded += (s, e) => ViewModel?.ApplyWizardSetup(width, height, paperType, rotation);
            }
        }

        private void OnGoHomeClick(object sender, RoutedEventArgs e)
        {
            if (DashboardWindow != null)
            {
                WindowLocationHelper.SyncWindowBounds(this, DashboardWindow);
                DashboardWindow.Show();
                DashboardWindow.Activate();
                DashboardWindow.RefreshDashboard();
                this.Close();
            }
            else
            {
                var start = new StartWindow();
                WindowLocationHelper.TransitionToWindow(this, start);
            }
        }

        private void OnAddShapeClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddShape != null)
            {
                PopupAddShape.IsOpen = !PopupAddShape.IsOpen;
            }
        }

        private void OnCloseShapePopupClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddShape != null)
            {
                PopupAddShape.IsOpen = false;
            }
        }

        private void OnShapePickBtnClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddShape != null)
            {
                PopupAddShape.IsOpen = false;
            }
        }

        private void OnAddLineClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddLine != null)
            {
                PopupAddLine.IsOpen = !PopupAddLine.IsOpen;
            }
        }

        private void OnCloseLinePopupClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddLine != null)
            {
                PopupAddLine.IsOpen = false;
            }
        }

        private void OnLinePickBtnClick(object sender, RoutedEventArgs e)
        {
            if (PopupAddLine != null)
            {
                PopupAddLine.IsOpen = false;
            }
        }

        private void OnPaletteAddTextClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddNewElement(ElementType.TEXT);
                RedrawInteractiveCanvas();
            }
        }

        private void OnPaletteAddBarcodeClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddNewElement(ElementType.BARCODE);
                RedrawInteractiveCanvas();
            }
        }

        private void OnPaletteAddQrClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddNewElement(ElementType.QR_CODE);
                RedrawInteractiveCanvas();
            }
        }

        private void OnPaletteAddTableClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddNewElement(ElementType.TABLE);
                RedrawInteractiveCanvas();
            }
        }

        private void OnPaletteAddImageClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddImageElement();
                RedrawInteractiveCanvas();
            }
        }

        private void OnPaletteAddDateTimeClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AddNewElement(ElementType.DATE_TIME);
                RedrawInteractiveCanvas();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            HandleCanvasKeyShortcuts(e);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleCanvasKeyShortcuts(e);
        }

        private void HandleCanvasKeyShortcuts(KeyEventArgs e)
        {
            if (e.Handled) return;

            // 텍스트 입력 컨트롤에 커서가 있는 경우 처리
            if (Keyboard.FocusedElement is TextBox tb)
            {
                // 인라인 편집 또는 텍스트 입력창에서 Escape 키를 누르면 포커스를 캔버스로 복귀하고 선택 해제
                if (e.Key == Key.Escape)
                {
                    GlobalCanvas?.Focus();
                    if (ViewModel != null)
                    {
                        ViewModel.DeselectElement();
                        RedrawInteractiveCanvas();
                    }
                    e.Handled = true;
                    return;
                }

                // 텍스트박스 내부에서 텍스트 타이핑/선택 중일 때는 텍스트박스 기본 동작 보장
                return;
            }

            if (Keyboard.FocusedElement is RichTextBox || Keyboard.FocusedElement is PasswordBox)
            {
                return;
            }

            if (ViewModel == null) return;

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            // 1. Del / Backspace: 선택된 요소 삭제
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (ViewModel.HasSelectedElement || ViewModel.HasMultipleSelectedElements || _selectedItemIndices.Count > 0 || _selectedItemIndex >= 0)
                {
                    // 레거시 _selectedItemIndices만 있고 ViewModel.SelectedElements가 비어있다면 동기화
                    if (!ViewModel.HasSelectedElement && !ViewModel.HasMultipleSelectedElements && ViewModel.CurrentTemplate?.Elements != null)
                    {
                        var targetsFromIndices = new List<TemplateElement>();
                        foreach (int idx in _selectedItemIndices)
                        {
                            int colNum = idx + 1;
                            var elem = ViewModel.CurrentTemplate.Elements.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                            if (elem != null && !targetsFromIndices.Contains(elem)) targetsFromIndices.Add(elem);
                        }
                        if (_selectedItemIndex >= 0)
                        {
                            int colNum = _selectedItemIndex + 1;
                            var elem = ViewModel.CurrentTemplate.Elements.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                            if (elem != null && !targetsFromIndices.Contains(elem)) targetsFromIndices.Add(elem);
                        }
                        if (targetsFromIndices.Count > 0)
                        {
                            ViewModel.SetSelectedElements(targetsFromIndices);
                        }
                    }

                    ViewModel.DeleteSelectedElement();
                    _selectedItemIndices.Clear();
                    _selectedItemIndex = -1;
                    _selectedTemplateElement = null;
                    RedrawInteractiveCanvas();
                    e.Handled = true;
                }
            }
            // 2. Ctrl + A: 캔버스 모든 요소 전체 선택
            else if (isCtrl && e.Key == Key.A)
            {
                ViewModel.SelectAllElements();
                _selectedItemIndices.Clear();
                int activeCount = Math.Max(1, Math.Min(8, ViewModel.ActiveColumnCount));
                for (int i = 0; i < activeCount; i++) _selectedItemIndices.Add(i);
                if (ViewModel.IsDateTimeEnabled) _selectedItemIndices.Add(99);
                if (ViewModel.IsSerialEnabled) _selectedItemIndices.Add(101);
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
            // 3. Ctrl + C: 요소 복사
            else if (isCtrl && e.Key == Key.C)
            {
                if (ViewModel.HasSelectedElement || ViewModel.HasMultipleSelectedElements)
                {
                    ViewModel.CopySelectedElement();
                    e.Handled = true;
                }
            }
            // 4. Ctrl + V: 요소 붙여넣기
            else if (isCtrl && e.Key == Key.V)
            {
                ViewModel.PasteElement();
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
            // 5. Ctrl + Z: 실행 취소 (Undo)
            else if (isCtrl && !isShift && e.Key == Key.Z)
            {
                ViewModel.Undo();
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
            // 6. Ctrl + Y 또는 Ctrl + Shift + Z: 다시 실행 (Redo)
            else if ((isCtrl && e.Key == Key.Y) || (isCtrl && isShift && e.Key == Key.Z))
            {
                ViewModel.Redo();
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
            // 7. Ctrl + D: 선택된 요소 복제
            else if (isCtrl && e.Key == Key.D)
            {
                if (ViewModel.HasSelectedElement || ViewModel.HasMultipleSelectedElements)
                {
                    ViewModel.DuplicateSelectedElement();
                    RedrawInteractiveCanvas();
                    e.Handled = true;
                }
            }
            // 8. ESC: 선택 해제
            else if (e.Key == Key.Escape)
            {
                ViewModel.DeselectElement();
                RedrawInteractiveCanvas();
                e.Handled = true;
            }
            // 9. 방향키: 미세 위치 이동 (Shift 누르면 0.2mm 정밀 이동, 기본 1.0mm)
            else if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                if (ViewModel.HasSelectedElement || ViewModel.HasMultipleSelectedElements)
                {
                    double step = isShift ? 0.2 : 1.0;
                    double dx = 0, dy = 0;
                    if (e.Key == Key.Left) dx = -step;
                    else if (e.Key == Key.Right) dx = step;
                    else if (e.Key == Key.Up) dy = -step;
                    else if (e.Key == Key.Down) dy = step;

                    ViewModel.MoveSelectedElement(dx, dy);
                    RedrawInteractiveCanvas();
                    e.Handled = true;
                }
            }
        }

        private void OnNewTemplateWizardClick(object sender, RoutedEventArgs e)
        {
            double curW = ViewModel?.EffectivePaperWidth ?? 0;
            double curH = ViewModel?.EffectivePaperHeight ?? 0;
            var wizard = new LabelSetupWizardDialog(curW, curH);
            wizard.Owner = this;
            if (wizard.ShowDialog() == true)
            {
                ViewModel?.ApplyWizardSetup(wizard.SelectedWidth, wizard.SelectedHeight, wizard.SelectedPaperType, wizard.SelectedRotation);
            }
        }

        private void OnSelectedAlignLeftClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.HasSelectedElement)
            {
                ViewModel.SelectedElementAlignment = "LEFT";
                RedrawInteractiveCanvas();
            }
        }

        private void OnSelectedAlignCenterClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.HasSelectedElement)
            {
                ViewModel.SelectedElementAlignment = "CENTER";
                RedrawInteractiveCanvas();
            }
        }

        private void OnSelectedAlignRightClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.HasSelectedElement)
            {
                ViewModel.SelectedElementAlignment = "RIGHT";
                RedrawInteractiveCanvas();
            }
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/wpfmainwindow.xaml", UriKind.Relative);
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
                    else if (args.PropertyName == nameof(ViewModel.ZoomRatio) || 
                             args.PropertyName == nameof(ViewModel.EffectivePaperWidth) || 
                             args.PropertyName == nameof(ViewModel.EffectivePaperHeight) ||
                             args.PropertyName == "PaperWidthStr" ||
                             args.PropertyName == "PaperHeightStr")
                    {
                        Dispatcher.BeginInvoke(new Action(UpdateRulers), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    else if (args.PropertyName == nameof(ViewModel.SelectedElement))
                    {
                        _selectedTemplateElement = ViewModel.SelectedElement;
                    }
                };
                UpdateDataGridColumnsVisibility(ViewModel.ActiveColumnCount);
            }

            if (ViewModel != null && ViewModel.SelectedElement != null)
            {
                _selectedTemplateElement = ViewModel.SelectedElement;
            }

            FocusFirstInput();
            RedrawInteractiveCanvas();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RedrawInteractiveCanvas();
                UpdateRulers();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

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

                // 1. 기본 1~8열 가시성 및 헤더 갱신
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

                // 2. 8개 초과 컬럼 동적 추가 및 동기화 (컬럼 개수 제한 완전 해제)
                for (int c = dg.Columns.Count - 1; c >= 10; c--)
                {
                    if (dg.Columns[c] is DataGridTextColumn dgc && dgc.SortMemberPath != null && dgc.SortMemberPath.StartsWith("COL_"))
                    {
                        if (int.TryParse(dgc.SortMemberPath.Substring(4), out int cIdx) && cIdx > activeCount)
                        {
                            dg.Columns.RemoveAt(c);
                        }
                    }
                }

                for (int i = 9; i <= activeCount; i++)
                {
                    int targetColIndex = i;
                    string colKey = $"COL_{targetColIndex}";
                    var existingCol = dg.Columns.OfType<DataGridTextColumn>().FirstOrDefault(c => c.SortMemberPath == colKey);
                    string headerText = ViewModel != null ? ViewModel.GetHeaderByIndex(targetColIndex) : $"{targetColIndex}열";
                    if (string.IsNullOrWhiteSpace(headerText)) headerText = $"{targetColIndex}열";

                    if (existingCol == null)
                    {
                        var newCol = new DataGridTextColumn
                        {
                            Header = headerText,
                            SortMemberPath = colKey,
                            Width = new DataGridLength(85),
                            Binding = new System.Windows.Data.Binding($"[{targetColIndex}]")
                            {
                                Mode = System.Windows.Data.BindingMode.TwoWay,
                                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
                            }
                        };
                        dg.Columns.Add(newCol);
                    }
                    else
                    {
                        existingCol.Header = headerText;
                        existingCol.Visibility = Visibility.Visible;
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
                if (colIndex >= 1 && (ViewModel == null || colIndex <= ViewModel.ActiveColumnCount))
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
                        int activeCount = Math.Max(1, Math.Min(8, ViewModel != null ? ViewModel.ActiveColumnCount : 1));
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
                e.Handled = true;

                if (sender is TextBox currentTb && currentTb.Tag != null)
                {
                    // 현재 입력값을 즉시 ViewModel에 커밋
                    var binding = currentTb.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();

                    if (int.TryParse(currentTb.Tag.ToString(), out int index))
                    {
                        int activeCount = Math.Max(1, Math.Min(8, ViewModel != null ? ViewModel.ActiveColumnCount : 1));
                        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                        if (isShift)
                        {
                            // Shift + Enter: 이전 활성화된(직접 입력 가능한) 열로 포커스 이동
                            for (int k = index - 1; k >= 0; k--)
                            {
                                if (FindName($"TxtCol{k + 1}") is TextBox prevTb && prevTb.IsEnabled && prevTb.IsVisible)
                                {
                                    FocusControlByName($"TxtCol{k + 1}");
                                    break;
                                }
                            }
                            return;
                        }

                        // 다음 직접 입력 가능한 열 찾기
                        int targetNextIndex = -1;
                        for (int k = index + 1; k < activeCount; k++)
                        {
                            if (FindName($"TxtCol{k + 1}") is TextBox nextTb && nextTb.IsEnabled && nextTb.IsVisible)
                            {
                                targetNextIndex = k;
                                break;
                            }
                        }

                        // 다음 직접 입력 가능한 열이 있으면 포커스 이동, 없으면 즉시 라벨 인쇄 실행
                        if (targetNextIndex != -1)
                        {
                            FocusControlByName($"TxtCol{targetNextIndex + 1}");
                        }
                        else
                        {
                            if (ViewModel != null)
                            {
                                if (ViewModel.SinglePrintCommand != null && ViewModel.SinglePrintCommand.CanExecute(null))
                                {
                                    ViewModel.SinglePrintCommand.Execute(null);
                                }
                                else
                                {
                                    ViewModel.ExecutePrintOnly();
                                }
                            }
                        }
                    }
                }
            }
        }

        #region WYSIWYG Interactive Canvas Designer (우측 전역 미리보기 & 탭2 캔버스 동시 렌더링 & 드래그)

        private const double MM_TO_PX = 96.0 / 25.4; // 표준 96 DPI WPF Device Independent Pixels per mm (3.779527559)

        private Brush _cachedGridBrush = null;
        private Brush CreatePaperGridBrush()
        {
            if (_cachedGridBrush != null) return _cachedGridBrush;

            double unit10mm = 10.0 * MM_TO_PX; // 약 37.795px
            double unit5mm = 5.0 * MM_TO_PX;   // 약 18.898px

            var group = new DrawingGroup();

            // 1. 흰색 기본 바탕
            group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, unit10mm, unit10mm))));

            // 2. 5mm 보조 눈금선 (더 연한 점선 느낌의 연회색)
            var subPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), 0.6);
            subPen.Freeze();
            group.Children.Add(new GeometryDrawing(null, subPen, new LineGeometry(new Point(0, unit5mm), new Point(unit10mm, unit5mm))));
            group.Children.Add(new GeometryDrawing(null, subPen, new LineGeometry(new Point(unit5mm, 0), new Point(unit5mm, unit10mm))));

            // 3. 10mm 주 눈금선 (깔끔한 라인)
            var mainPen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")), 0.8);
            mainPen.Freeze();
            group.Children.Add(new GeometryDrawing(null, mainPen, new LineGeometry(new Point(0, 0), new Point(unit10mm, 0))));
            group.Children.Add(new GeometryDrawing(null, mainPen, new LineGeometry(new Point(0, 0), new Point(0, unit10mm))));

            var brush = new DrawingBrush(group)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, unit10mm, unit10mm),
                ViewportUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            _cachedGridBrush = brush;
            return _cachedGridBrush;
        }

        public void RedrawInteractiveCanvas()
        {
            if (ViewModel == null) return;

            RenderCanvasContent(GlobalCanvas, true);
            RenderCanvasContent(ExcelCanvas, true);
            RenderCanvasContent(DragCanvas, false);
        }

        /// <summary>
        /// 마우스 드래그 중 60~120fps 고속 렌더링: 비활성 캔버스를 건너뛰고 현재 마우스 조작 중인 캔버스 1개만 즉시 갱신
        /// </summary>
        private void RedrawActiveDragCanvas(Canvas canvas)
        {
            if (canvas == null || ViewModel == null) return;
            RenderCanvasContent(canvas, canvas == GlobalCanvas || canvas == ExcelCanvas);
        }

        private void OnCanvasScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateRulers();
        }

        private void OnCanvasScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRulers();
        }

        public void UpdateRulers()
        {
            var topCanvas = FindName("TopRulerCanvas") as Canvas;
            var leftCanvas = FindName("LeftRulerCanvas") as Canvas;
            var globalCanvas = GlobalCanvas;

            if (topCanvas == null || leftCanvas == null || globalCanvas == null || ViewModel == null) return;
            if (!globalCanvas.IsVisible || globalCanvas.ActualWidth <= 0 || globalCanvas.ActualHeight <= 0) return;

            try
            {
                double zoom = ViewModel.ZoomRatio > 0 ? ViewModel.ZoomRatio : 1.0;
                double pxPerMm = MM_TO_PX * zoom;
                if (pxPerMm <= 0) return;

                int wMm = ViewModel.EffectivePaperWidth > 0 ? ViewModel.EffectivePaperWidth : 100;
                int hMm = ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : 100;

                Point ptTop = globalCanvas.TranslatePoint(new Point(0, 0), topCanvas);
                Point ptLeft = globalCanvas.TranslatePoint(new Point(0, 0), leftCanvas);

                double originX = ptTop.X;
                double originY = ptLeft.Y;

                // 1. 상단 가로 눈금자 (Top Ruler) 렌더링
                topCanvas.Children.Clear();
                double rulerW = topCanvas.ActualWidth > 0 ? topCanvas.ActualWidth : 1200;
                double rulerH = topCanvas.ActualHeight > 0 ? topCanvas.ActualHeight : 22;

                // 라벨 용지 너비 영역 하이라이트 배경 (흰색)
                double paperStartPx = originX;
                double paperEndPx = originX + (wMm * pxPerMm);
                if (paperEndPx > 0 && paperStartPx < rulerW)
                {
                    double drawLeft = Math.Max(0, paperStartPx);
                    double drawW = Math.Min(rulerW, paperEndPx) - drawLeft;
                    if (drawW > 0)
                    {
                        var paperBg = new Rectangle
                        {
                            Width = drawW,
                            Height = rulerH,
                            Fill = Brushes.White
                        };
                        Canvas.SetLeft(paperBg, drawLeft);
                        Canvas.SetTop(paperBg, 0);
                        topCanvas.Children.Add(paperBg);
                    }
                }

                int minMm = (int)Math.Floor(-originX / pxPerMm) - 2;
                int maxMm = (int)Math.Ceiling((rulerW - originX) / pxPerMm) + 2;

                int stepMm = 10;
                if (pxPerMm * 10 < 30) stepMm = 20;
                if (pxPerMm * 20 < 30) stepMm = 50;
                if (pxPerMm * 50 < 30) stepMm = 100;

                var textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                var majorTickBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                var minorTickBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                var microTickBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));

                for (int mm = minMm; mm <= maxMm; mm++)
                {
                    double x = originX + (mm * pxPerMm);
                    if (x < -30 || x > rulerW + 30) continue;

                    if (mm % stepMm == 0)
                    {
                        // 주 눈금선 (높이 8px)
                        var line = new Line
                        {
                            X1 = x, Y1 = rulerH - 8,
                            X2 = x, Y2 = rulerH,
                            Stroke = majorTickBrush,
                            StrokeThickness = 1.0
                        };
                        topCanvas.Children.Add(line);

                        // 숫자 라벨
                        var tb = new TextBlock
                        {
                            Text = mm.ToString(),
                            FontSize = 8.5,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = textBrush
                        };
                        Canvas.SetLeft(tb, x + 2);
                        Canvas.SetTop(tb, 1.5);
                        topCanvas.Children.Add(tb);
                    }
                    else if (mm % 5 == 0)
                    {
                        // 5mm 중간 눈금선 (높이 5px)
                        var line = new Line
                        {
                            X1 = x, Y1 = rulerH - 5,
                            X2 = x, Y2 = rulerH,
                            Stroke = minorTickBrush,
                            StrokeThickness = 0.8
                        };
                        topCanvas.Children.Add(line);
                    }
                    else if (pxPerMm >= 3.0)
                    {
                        // 1mm 미세 눈금선 (높이 3px)
                        var line = new Line
                        {
                            X1 = x, Y1 = rulerH - 3,
                            X2 = x, Y2 = rulerH,
                            Stroke = microTickBrush,
                            StrokeThickness = 0.6
                        };
                        topCanvas.Children.Add(line);
                    }
                }

                // 2. 좌측 세로 눈금자 (Left Ruler) 렌더링
                leftCanvas.Children.Clear();
                double leftRulerW = leftCanvas.ActualWidth > 0 ? leftCanvas.ActualWidth : 22;
                double leftRulerH = leftCanvas.ActualHeight > 0 ? leftCanvas.ActualHeight : 800;

                // 라벨 용지 높이 영역 하이라이트 배경 (흰색)
                double paperTopPx = originY;
                double paperBottomPx = originY + (hMm * pxPerMm);
                if (paperBottomPx > 0 && paperTopPx < leftRulerH)
                {
                    double drawTop = Math.Max(0, paperTopPx);
                    double drawH = Math.Min(leftRulerH, paperBottomPx) - drawTop;
                    if (drawH > 0)
                    {
                        var paperBg = new Rectangle
                        {
                            Width = leftRulerW,
                            Height = drawH,
                            Fill = Brushes.White
                        };
                        Canvas.SetLeft(paperBg, 0);
                        Canvas.SetTop(paperBg, drawTop);
                        leftCanvas.Children.Add(paperBg);
                    }
                }

                int minMmY = (int)Math.Floor(-originY / pxPerMm) - 2;
                int maxMmY = (int)Math.Ceiling((leftRulerH - originY) / pxPerMm) + 2;

                for (int mm = minMmY; mm <= maxMmY; mm++)
                {
                    double y = originY + (mm * pxPerMm);
                    if (y < -30 || y > leftRulerH + 30) continue;

                    if (mm % stepMm == 0)
                    {
                        // 주 눈금선 (가로 너비 8px)
                        var line = new Line
                        {
                            X1 = leftRulerW - 8, Y1 = y,
                            X2 = leftRulerW, Y2 = y,
                            Stroke = majorTickBrush,
                            StrokeThickness = 1.0
                        };
                        leftCanvas.Children.Add(line);

                        // 숫자 라벨
                        var tb = new TextBlock
                        {
                            Text = mm.ToString(),
                            FontSize = 8.0,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = textBrush
                        };
                        Canvas.SetLeft(tb, 1.5);
                        Canvas.SetTop(tb, y + 1.5);
                        leftCanvas.Children.Add(tb);
                    }
                    else if (mm % 5 == 0)
                    {
                        // 5mm 중간 눈금선 (가로 너비 5px)
                        var line = new Line
                        {
                            X1 = leftRulerW - 5, Y1 = y,
                            X2 = leftRulerW, Y2 = y,
                            Stroke = minorTickBrush,
                            StrokeThickness = 0.8
                        };
                        leftCanvas.Children.Add(line);
                    }
                    else if (pxPerMm >= 3.0)
                    {
                        // 1mm 미세 눈금선 (가로 너비 3px)
                        var line = new Line
                        {
                            X1 = leftRulerW - 3, Y1 = y,
                            X2 = leftRulerW, Y2 = y,
                            Stroke = microTickBrush,
                            StrokeThickness = 0.6
                        };
                        leftCanvas.Children.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateRulers Exception: {ex.Message}");
            }
        }

        private void RenderCanvasContent(Canvas targetCanvas, bool isGlobal)
        {
            if (targetCanvas == null || ViewModel == null) return;

            targetCanvas.Children.Clear();

            int wMm = ViewModel.EffectivePaperWidth > 0 ? ViewModel.EffectivePaperWidth : 80;
            int hMm = ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : 60;

            // 캔버스의 1:1 절대 해상도 설정 (1mm = 4.5px)
            targetCanvas.Width = wMm * MM_TO_PX;
            targetCanvas.Height = hMm * MM_TO_PX;

            // 캔버스 전체에 균일한 벡터 스케일 변환(ScaleTransform) 적용 -> 줌에 따른 위치 왜곡 100% 제거
            targetCanvas.LayoutTransform = new ScaleTransform(ViewModel.ZoomRatio, ViewModel.ZoomRatio);

            // 1. 라벨 용지 외곽선 사각형 및 깨끗한 순백색 배경 (내부 격자선 제거)
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
                TemplateElement elem = ViewModel.CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                bool isBarcodeCol = (elem != null) 
                    ? (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE) 
                    : ViewModel.GetBarcodeByIndex(colNum);

                if (isBarcodeCol) barcodeColIndices.Add(colNum);
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
                        TemplateElement elem = ViewModel.CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                        if (elem != null && (!string.IsNullOrEmpty(elem.DataSourceColumn) || ViewModel.CurrentPreviewIndex > 0))
                        {
                            val = ViewModel.GetElementEffectiveContent(elem);
                        }
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
                        TemplateElement elem = ViewModel.CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                        if (elem != null && (!string.IsNullOrEmpty(elem.DataSourceColumn) || ViewModel.CurrentPreviewIndex > 0))
                        {
                            val = ViewModel.GetElementEffectiveContent(elem);
                        }
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
                    TemplateElement elem = ViewModel.CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                    if (elem != null)
                    {
                        if (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE)
                        {
                            bType = elem.BarcodeType;
                            showBText = elem.ShowBarcodeText;
                        }
                        string eff = ViewModel.GetElementEffectiveContent(elem);
                        if (!string.IsNullOrEmpty(eff) || string.IsNullOrEmpty(val))
                        {
                            val = eff;
                        }
                    }
                    double finalX = (elem != null) ? elem.X : offX;
                    double finalY = (elem != null) ? elem.Y : offY;
                    double finalW = (elem != null && elem.Width > 0) ? elem.Width : 0;
                    double finalH = (elem != null && elem.Height > 0) ? elem.Height : 0;
                    AddWysiwygItem(targetCanvas, i, val, header, false, false, true, bType, showBText, size, finalX, finalY, finalW, finalH, "LEFT", fontName, isBold, isItalic, 0, elem);
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
                    int offX = ViewModel.GetOffsetXByIndex(colNum);
                    int offY = ViewModel.GetOffsetYByIndex(colNum);
                    string align = ViewModel.GetAlignByIndex(colNum);
                    string fontName = ViewModel.GetFontByIndex(colNum);
                    bool isBold = ViewModel.GetBoldByIndex(colNum);
                    bool isItalic = ViewModel.GetItalicByIndex(colNum);
                    int userWMm = ViewModel.GetWidthByIndex(colNum);

                    // 템플릿 요소 찾기 (크기/위치 및 객체 전달)
                    TemplateElement elem = ViewModel.CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
                    if (elem != null)
                    {
                        isBarcode = (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE);
                        if (isBarcode)
                        {
                            bType = elem.BarcodeType;
                            showBText = elem.ShowBarcodeText;
                        }

                        string eff = ViewModel.GetElementEffectiveContent(elem);
                        if (!string.IsNullOrEmpty(eff) || string.IsNullOrEmpty(val))
                        {
                            val = eff;
                        }
                    }
                    int size = (elem != null && elem.FontSize > 0) ? (int)Math.Round(elem.FontSize) : ViewModel.GetSizeByIndex(colNum);
                    double finalX = (elem != null) ? elem.X : offX;
                    double finalY = (elem != null) ? elem.Y : offY;
                    double finalW = (elem != null && elem.Width > 0) ? elem.Width : userWMm;
                    double finalH = (elem != null && elem.Height > 0) ? elem.Height : 0;

                    AddWysiwygItem(targetCanvas, i, val, header, isPrice, isIncludeHeader, isBarcode, bType, showBText, size, finalX, finalY, finalW, finalH, align, fontName, isBold, isItalic, 0, elem);
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

            // 6. 안드로이드 호환 자유 배치 커스텀 요소 렌더링 (선, 도형, 추가 텍스트/바코드 등)
            RenderCustomTemplateElements(targetCanvas);

            // 7. 선택된 요소 변형 조작 핸들 오버레이 (다중 선택 지원 & 레거시 인덱스 선택 연동)
            if (ViewModel.HasMultipleSelectedElements)
            {
                foreach (var elem in ViewModel.SelectedElements)
                {
                    bool isPrimary = (elem == ViewModel.SelectedElement);
                    DrawSelectionOverlay(targetCanvas, elem, showHandles: isPrimary);
                }
            }
            else if (ViewModel.SelectedElement != null)
            {
                _selectedTemplateElement = ViewModel.SelectedElement;
                DrawSelectionOverlay(targetCanvas, ViewModel.SelectedElement, showHandles: true);
            }
            else if (_selectedItemIndex >= 0 && ViewModel.CurrentTemplate?.Elements != null)
            {
                int targetColNum = (_selectedItemIndex == 99 || _selectedItemIndex == 101) ? _selectedItemIndex : _selectedItemIndex + 1;
                var matchingElem = ViewModel.CurrentTemplate.Elements.FirstOrDefault(el =>
                    (_selectedItemIndex == 99 && el.Id == "DATETIME_ELEMENT") ||
                    (_selectedItemIndex == 101 && el.Id == "SERIAL_ELEMENT") ||
                    (el.IsVariable && el.InputOrder == targetColNum));
                if (matchingElem != null)
                {
                    _selectedTemplateElement = matchingElem;
                    DrawSelectionOverlay(targetCanvas, matchingElem, showHandles: true);
                }
            }
        }

        private void RenderCustomTemplateElements(Canvas targetCanvas)
        {
            if (ViewModel == null || ViewModel.CurrentTemplate == null || ViewModel.CurrentTemplate.Elements == null) return;

            foreach (var elem in ViewModel.CurrentTemplate.Elements)
            {
                // 고정 1~8열 변수로 이미 처리된 요소는 중복 렌더링 방지 (9열 이상은 캔버스에서 직접 렌더링)
                if (elem.IsVariable && elem.InputOrder <= 8 && elem.InputOrder <= ViewModel.ActiveColumnCount)
                {
                    continue;
                }

                if (elem.Id == "TITLE_ELEMENT" || elem.Id == "DATETIME_ELEMENT") continue;

                double leftPx = elem.X * MM_TO_PX;
                double topPx = elem.Y * MM_TO_PX;
                double widthPx = elem.Width * MM_TO_PX;
                double heightPx = elem.Height * MM_TO_PX;
                bool isSelected = (ViewModel != null && (ViewModel.SelectedElements.Contains(elem) || ViewModel.SelectedElement == elem));

                switch (elem.Type)
                {
                    case ElementType.LINE:
                        double lWidth = Math.Max(10, widthPx);
                        double strokeL = Math.Max(1.0, elem.LineWidth);
                        double lHeight = Math.Max(strokeL * 3.0, Math.Max(8, heightPx + 4));
                        var lineBorder = new Border
                        {
                            Width = lWidth,
                            Height = lHeight,
                            ClipToBounds = false,
                            Background = Brushes.Transparent,
                            BorderBrush = isSelected ? Brushes.Blue : Brushes.Transparent,
                            BorderThickness = new Thickness(isSelected ? 1 : 0),
                            Cursor = Cursors.SizeAll,
                            Tag = elem
                        };
                        if (elem.Rotation != 0)
                        {
                            lineBorder.RenderTransformOrigin = new Point(0, 0.5);
                            lineBorder.RenderTransform = new RotateTransform(elem.Rotation);
                        }
                        var linePath = new System.Windows.Shapes.Path
                        {
                            ClipToBounds = false,
                            Data = ShapeGeometryHelper.CreateLineGeometry(elem.LineStyle, lWidth, lHeight, strokeL),
                            Stroke = Brushes.Black,
                            StrokeThickness = strokeL,
                            StrokeDashArray = ShapeGeometryHelper.GetStrokeDashArray(elem.LineStyle)
                        };
                        lineBorder.Child = linePath;
                        Canvas.SetLeft(lineBorder, leftPx);
                        Canvas.SetTop(lineBorder, topPx);
                        targetCanvas.Children.Add(lineBorder);
                        break;

                    case ElementType.SHAPE:
                        double shapeW = Math.Max(5, widthPx);
                        double shapeH = Math.Max(5, heightPx);
                        double strokeS = Math.Max(1.0, elem.LineWidth);
                        var shapePath = new System.Windows.Shapes.Path
                        {
                            Width = shapeW,
                            Height = shapeH,
                            ClipToBounds = false,
                            StrokeLineJoin = PenLineJoin.Round,
                            Data = ShapeGeometryHelper.CreateShapeGeometry(elem.ShapeType, shapeW, shapeH, strokeS),
                            Stroke = isSelected ? Brushes.Blue : Brushes.Black,
                            StrokeThickness = strokeS,
                            Fill = elem.IsFilled ? Brushes.Black : Brushes.Transparent,
                            Cursor = Cursors.SizeAll,
                            Tag = elem
                        };
                        if (elem.Rotation != 0)
                        {
                            shapePath.RenderTransformOrigin = new Point(0.5, 0.5);
                            shapePath.RenderTransform = new RotateTransform(elem.Rotation);
                        }
                        Canvas.SetLeft(shapePath, leftPx);
                        Canvas.SetTop(shapePath, topPx);
                        targetCanvas.Children.Add(shapePath);
                        break;

                    case ElementType.IMAGE:
                        var imgBorder = new Border
                        {
                            Width = Math.Max(15, widthPx),
                            Height = Math.Max(15, heightPx),
                            Background = isSelected ? new SolidColorBrush(Color.FromArgb(40, 37, 99, 235)) : new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)),
                            BorderBrush = isSelected ? Brushes.Blue : Brushes.LightGray,
                            BorderThickness = new Thickness(isSelected ? 2 : 1),
                            Cursor = Cursors.SizeAll,
                            Tag = elem
                        };
                        if (elem.Rotation != 0)
                        {
                            imgBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                            imgBorder.RenderTransform = new RotateTransform(elem.Rotation);
                        }
                        try
                        {
                            if (!string.IsNullOrEmpty(elem.Content) && System.IO.File.Exists(elem.Content))
                            {
                                var bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri(elem.Content));
                                var imgCtrl = new Image { Source = bmp, Stretch = Stretch.Uniform };
                                imgBorder.Child = imgCtrl;
                            }
                            else
                            {
                                var fallbackText = new TextBlock
                                {
                                    Text = "🖼️ 이미지",
                                    Foreground = Brushes.Gray,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    FontSize = 11
                                };
                                imgBorder.Child = fallbackText;
                            }
                        }
                        catch
                        {
                            // fallback
                        }
                        Canvas.SetLeft(imgBorder, leftPx);
                        Canvas.SetTop(imgBorder, topPx);
                        targetCanvas.Children.Add(imgBorder);
                        break;

                    case ElementType.TABLE:
                        AddWysiwygTableItem(targetCanvas, elem);
                        break;

                    case ElementType.TEXT:
                    case ElementType.DATE_TIME:
                    case ElementType.BARCODE:
                    case ElementType.QR_CODE:
                        string effectiveContent = ViewModel != null ? ViewModel.GetElementEffectiveContent(elem) : elem.Content;
                        AddWysiwygItem(targetCanvas, -100 - elem.InputOrder, effectiveContent, elem.HeaderName, elem.IsPriceFormat, elem.IncludeHeaderOnLabel, elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE, elem.BarcodeType, elem.ShowBarcodeText, (int)Math.Round(elem.FontSize), elem.X, elem.Y, elem.Width, elem.Height, elem.Alignment, elem.FontName, elem.IsBold, elem.IsItalic, 0, elem);
                        break;
                }
            }
        }

        private void AddWysiwygTableItem(Canvas targetCanvas, TemplateElement elem)
        {
            if (elem == null || targetCanvas == null) return;
            elem.EnsureTableData();

            int rows = Math.Max(1, elem.Rows);
            int cols = Math.Max(1, elem.Columns);
            double widthPx = Math.Max(20, elem.Width * MM_TO_PX);
            double heightPx = Math.Max(15, elem.Height * MM_TO_PX);
            double leftPx = elem.X * MM_TO_PX;
            double topPx = elem.Y * MM_TO_PX;
            double strokeW = Math.Max(1.0, elem.LineWidth);
            bool isSelected = (ViewModel != null && ViewModel.SelectedElement == elem);

            var outerBorder = new Border
            {
                Width = widthPx,
                Height = heightPx,
                BorderBrush = isSelected ? Brushes.Blue : Brushes.Black,
                BorderThickness = new Thickness(strokeW),
                Background = Brushes.White,
                Cursor = Cursors.SizeAll,
                Tag = elem,
                ClipToBounds = true
            };

            if (elem.Rotation != 0)
            {
                outerBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                outerBorder.RenderTransform = new RotateTransform(elem.Rotation);
            }

            var grid = new Grid();

            double[] colWeights = elem.ColumnWidths;
            for (int c = 0; c < cols; c++)
            {
                double weight = (colWeights != null && colWeights.Length == cols && colWeights[c] > 0) ? colWeights[c] : 1.0;
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(weight, GridUnitType.Star) });
            }

            double[] rowWeights = elem.RowHeights;
            for (int r = 0; r < rows; r++)
            {
                double weight = (rowWeights != null && rowWeights.Length == rows && rowWeights[r] > 0) ? rowWeights[r] : 1.0;
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(weight, GridUnitType.Star) });
            }

            for (int r = 0; r < rows; r++)
            {
                bool isHeader = (r == 0);
                for (int c = 0; c < cols; c++)
                {
                    var cellBorder = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, (c < cols - 1) ? strokeW : 0, (r < rows - 1) ? strokeW : 0),
                        Background = (isHeader && elem.IsFilled) ? new SolidColorBrush(Color.FromRgb(226, 232, 240)) : Brushes.Transparent
                    };
                    Grid.SetRow(cellBorder, r);
                    Grid.SetColumn(cellBorder, c);

                    string cellText = (ViewModel != null)
                        ? ViewModel.GetTableCellEffectiveContent(elem, r, c)
                        : ((elem.TableData != null && r < elem.TableData.Length && c < elem.TableData[r].Length) ? elem.TableData[r][c] : "");

                    var style = elem.GetCellResolvedStyle(r, c);
                    string cellFontName = string.IsNullOrWhiteSpace(style.FontName) ? (string.IsNullOrWhiteSpace(elem.FontName) ? "맑은 고딕" : elem.FontName) : style.FontName;
                    double cellFontSizePx = (style.FontSize > 0 ? style.FontSize : 9.0) * 1.35;

                    var tb = new TextBlock
                    {
                        Text = cellText ?? "",
                        FontFamily = new FontFamily(cellFontName),
                        FontSize = cellFontSizePx,
                        FontWeight = style.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        FontStyle = elem.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(Math.Max(2, elem.CellPadding * MM_TO_PX)),
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None
                    };

                    if (elem.IsUnderline)
                    {
                        tb.TextDecorations = TextDecorations.Underline;
                    }

                    switch ((style.Alignment ?? "CENTER").ToUpperInvariant())
                    {
                        case "LEFT":
                            tb.HorizontalAlignment = HorizontalAlignment.Left;
                            tb.TextAlignment = TextAlignment.Left;
                            break;
                        case "RIGHT":
                            tb.HorizontalAlignment = HorizontalAlignment.Right;
                            tb.TextAlignment = TextAlignment.Right;
                            break;
                        default:
                            tb.HorizontalAlignment = HorizontalAlignment.Center;
                            tb.TextAlignment = TextAlignment.Center;
                            break;
                    }

                    cellBorder.Child = tb;
                    grid.Children.Add(cellBorder);
                }
            }

            outerBorder.Child = grid;
            Canvas.SetLeft(outerBorder, leftPx);
            Canvas.SetTop(outerBorder, topPx);
            targetCanvas.Children.Add(outerBorder);
        }

        private void MeasureTextDimensions(TemplateElement elem, double requestedWidthMm, out double finalWidthPx, out double finalHeightPx, out Thickness padding, out bool isWrapped)
        {
            if (elem == null)
            {
                finalWidthPx = 40.0;
                finalHeightPx = 20.0;
                padding = new Thickness(6, 4, 6, 4);
                isWrapped = false;
                return;
            }

            string textToMeasure = ViewModel != null ? ViewModel.GetElementEffectiveContent(elem) : elem.Content;
            if (string.IsNullOrEmpty(textToMeasure))
            {
                textToMeasure = !string.IsNullOrEmpty(elem.HeaderName) ? $"[{elem.HeaderName}]" : "텍스트";
            }
            else if (elem.IncludeHeaderOnLabel && !string.IsNullOrEmpty(elem.HeaderName))
            {
                textToMeasure = $"{elem.HeaderName}: {textToMeasure}";
            }
            if (elem.IsPriceFormat)
            {
                textToMeasure = PriceFormatHelper.FormatPrice(textToMeasure, true);
            }
            if (elem.Type == ElementType.DATE_TIME)
            {
                textToMeasure = $"🕒 {textToMeasure}";
            }

            string family = string.IsNullOrWhiteSpace(elem.FontName) ? "맑은 고딕" : elem.FontName;
            var style = elem.IsItalic ? FontStyles.Italic : FontStyles.Normal;
            var weight = elem.IsBold ? FontWeights.Bold : FontWeights.Normal;
            double fontSizePx = (elem.FontSize > 0 ? elem.FontSize : 10.0) * 1.35;

            // 폰트 크기에 비례하는 일정한 안전 여백 (글자와 테두리선의 관계가 항상 일정하도록 보장)
            double padX = Math.Max(6.0, fontSizePx * 0.22);
            double padY = Math.Max(4.0, fontSizePx * 0.16);
            padding = new Thickness(padX, padY, padX, padY);

            // 1. 단일행 기준 텍스트 전체 크기 측정
            var ftSingle = new FormattedText(
                textToMeasure,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(family), style, weight, FontStretches.Normal),
                Math.Max(8, fontSizePx),
                Brushes.Black,
                1.0);

            double scaleX = (elem.FontScaleX > 0) ? elem.FontScaleX : 1.0;
            double lineSpacing = (elem.LineSpacing > 0) ? elem.LineSpacing : 1.0;
            double letterSpacing = elem.LetterSpacing;

            string[] lines = textToMeasure.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int maxChars = 0;
            foreach (var line in lines)
            {
                if (line.Length > maxChars) maxChars = line.Length;
            }
            double extraSpacingPx = Math.Max(0, maxChars - 1) * (fontSizePx * letterSpacing) * scaleX;

            // 최소 1글자 너비 보장 (절대 1글자보다 좁아져서 테두리가 글자를 뚫지 못하도록)
            double minCharWPx = ((fontSizePx * 1.1) * scaleX) + (padX * 2);
            double singleLineTotalWPx = (ftSingle.Width * scaleX) + extraSpacingPx + (padX * 2);

            bool hasExplicitNewline = textToMeasure.Contains("\n") || textToMeasure.Contains("\r");

            if (requestedWidthMm > 0)
            {
                double reqWPx = Math.Max(minCharWPx, requestedWidthMm * MM_TO_PX);

                // 사용자가 요청한 폭이 텍스트 단일행 길이보다 좁거나, 텍스트에 명시적 줄바꿈(Enter)이 포함된 경우
                if (reqWPx < singleLineTotalWPx - 2.0 || hasExplicitNewline)
                {
                    isWrapped = true;
                    var ftWrap = new FormattedText(
                        textToMeasure,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily(family), style, weight, FontStretches.Normal),
                        Math.Max(8, fontSizePx),
                        Brushes.Black,
                        1.0);
                    ftWrap.MaxTextWidth = Math.Max(10.0, reqWPx - (padX * 2));
                    if (lineSpacing > 0 && Math.Abs(lineSpacing - 1.0) > 0.001)
                    {
                        ftWrap.LineHeight = Math.Max(8, fontSizePx) * lineSpacing;
                    }

                    finalWidthPx = reqWPx;
                    finalHeightPx = (ftWrap.Height * (lineSpacing > 1.0 ? 1.0 : lineSpacing)) + (padY * 2);
                }
                else
                {
                    isWrapped = false;
                    finalWidthPx = reqWPx;
                    finalHeightPx = (ftSingle.Height * lineSpacing) + (padY * 2);
                }
            }
            else
            {
                isWrapped = hasExplicitNewline;
                finalWidthPx = singleLineTotalWPx;
                finalHeightPx = (ftSingle.Height * lineSpacing) + (padY * 2);
            }
        }

        private void GetElementVisualDimensions(TemplateElement elem, out double widthPx, out double heightPx)
        {
            if (elem == null)
            {
                widthPx = 40.0;
                heightPx = 20.0;
                return;
            }

            if (elem.Type == ElementType.TEXT || elem.Type == ElementType.DATE_TIME)
            {
                MeasureTextDimensions(elem, elem.Width, out widthPx, out heightPx, out _, out _);
                elem.Height = Math.Round(heightPx / MM_TO_PX, 1);
            }
            else
            {
                double defaultW = (elem.Type == ElementType.QR_CODE) ? 18.0 : (elem.Type == ElementType.BARCODE ? 45.0 : 30.0);
                double defaultH = (elem.Type == ElementType.QR_CODE) ? 18.0 : (elem.Type == ElementType.BARCODE ? 15.0 : 15.0);
                double effW = (elem.Width > 0) ? elem.Width : defaultW;
                double effH = (elem.Height > 0) ? elem.Height : defaultH;
                widthPx = Math.Max(14.0, effW * MM_TO_PX);
                heightPx = Math.Max(10.0, effH * MM_TO_PX);
            }
        }

        private void AddWysiwygItem(Canvas targetCanvas, int index, string rawText, string headerName, bool isPrice, bool isIncludeHeader, bool isBarcode, string barcodeType, bool showBarcodeText, int size, double xMm, double yMm, double widthMm = 0, double heightMm = 0, string align = "LEFT", string fontName = "맑은 고딕", bool isBold = true, bool isItalic = false, double cellPaddingMm = 0, TemplateElement elem = null)
        {
            double left = xMm * MM_TO_PX;
            double top = yMm * MM_TO_PX;

            string displayVal = isPrice ? PriceFormatHelper.FormatPrice(rawText, true) : rawText;
            if (string.IsNullOrEmpty(displayVal))
            {
                displayVal = (isIncludeHeader && !isBarcode && index >= 0 && index != 99 && index != 101) ? $"{headerName}: [{headerName}]" : $"[{headerName}]";
            }
            else if (isIncludeHeader && !isBarcode && index >= 0 && index != 99 && index != 101)
            {
                displayVal = $"{headerName}: {displayVal}";
            }

            string renderedText = (index == 99) ? $"🕒 {displayVal}" : ((index == 101) ? $"🔢 {displayVal}" : displayVal);
            bool isSelected = _selectedItemIndices.Contains(index) || (_selectedItemIndex == index) || 
                              (elem != null && ViewModel != null && (ViewModel.SelectedElements.Contains(elem) || ViewModel.SelectedElement == elem));

            // 선택 오버레이(DrawSelectionOverlay)가 렌더링될 대상인지 확인하여 테두리 2중 생성 원천 방지
            bool hasSelectionOverlay = (ViewModel != null && (
                                       (ViewModel.SelectedElement != null && (ViewModel.SelectedElement == elem || (elem != null && ViewModel.SelectedElement.Id == elem.Id))) ||
                                       (elem != null && ViewModel.SelectedElements.Contains(elem))));

            double itemWPx;
            double itemHPx;
            Thickness itemPadding;
            bool isWrapped = false;

            if (elem != null && (elem.Type == ElementType.TEXT || elem.Type == ElementType.DATE_TIME))
            {
                MeasureTextDimensions(elem, elem.Width, out itemWPx, out itemHPx, out itemPadding, out isWrapped);
                elem.Height = Math.Round(itemHPx / MM_TO_PX, 1);
            }
            else if (isBarcode)
            {
                bool isQr = (barcodeType != null && barcodeType.ToUpper().Contains("QR")) || (elem != null && elem.Type == ElementType.QR_CODE);
                double defaultW = isQr ? 18.0 : 45.0;
                double defaultH = isQr ? 18.0 : 15.0;

                double effW = (widthMm > 0) ? widthMm : (elem != null && elem.Width > 0 ? elem.Width : defaultW);
                double effH = (heightMm > 0) ? heightMm : (elem != null && elem.Height > 0 ? elem.Height : defaultH);

                itemWPx = Math.Max(20.0, effW * MM_TO_PX);
                itemHPx = Math.Max(12.0, effH * MM_TO_PX);
                itemPadding = cellPaddingMm > 0 ? new Thickness(cellPaddingMm * MM_TO_PX) : new Thickness(0);
            }
            else
            {
                double effectiveFontSizePt = (elem != null && elem.FontSize > 0) ? elem.FontSize : size;
                double exactTextWMm = (ViewModel != null) 
                    ? (ViewModel.CalculateExactTextWidthMm(renderedText, fontName, (int)Math.Round(effectiveFontSizePt), isBold, isItalic) + 3.0) 
                    : 20.0;
                double effectiveItemWMm = (widthMm > 0) ? widthMm : exactTextWMm;
                itemWPx = Math.Max(12.0, effectiveItemWMm * MM_TO_PX + 10.0);
                itemHPx = (heightMm > 0) ? heightMm * MM_TO_PX : (elem != null && elem.Height > 0 ? elem.Height * MM_TO_PX : 24.0);
                itemPadding = cellPaddingMm > 0 ? new Thickness(cellPaddingMm * MM_TO_PX) : new Thickness(6, 4, 6, 4);
            }

            var container = new Border
            {
                Background = (isSelected && !hasSelectionOverlay) ? new SolidColorBrush(Color.FromArgb(60, 37, 99, 235)) : Brushes.Transparent,
                BorderBrush = (isSelected && !hasSelectionOverlay) ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : Brushes.Transparent,
                BorderThickness = new Thickness((isSelected && !hasSelectionOverlay) ? 2.0 : 0.0),
                CornerRadius = new CornerRadius(2),
                Padding = itemPadding,
                Cursor = (index >= 0 || index == 100 || index == 101 || index <= -1 || elem != null) ? Cursors.SizeAll : Cursors.Arrow,
                Tag = (elem != null) ? (object)elem : index,
                Width = itemWPx,
                Height = itemHPx
            };

            if (isBarcode)
            {
                string textPos = (elem != null && !string.IsNullOrEmpty(elem.BarcodeTextPosition))
                    ? elem.BarcodeTextPosition
                    : (showBarcodeText ? "BELOW" : "NONE");

                var barcodeVisual = BarcodeVisualHelper.CreateBarcodeVisual(
                    displayVal, 
                    barcodeType, 
                    size, 
                    isSelected, 
                    showBarcodeText, 
                    itemWPx, 
                    itemHPx,
                    textPos);
                container.Child = barcodeVisual;
            }
            else
            {
                var textAlignment = TextAlignment.Left;
                if (align == "CENTER") textAlignment = TextAlignment.Center;
                else if (align == "RIGHT") textAlignment = TextAlignment.Right;

                double effectiveFontSizePt = (elem != null && elem.FontSize > 0) ? elem.FontSize : size;
                double scaleX = (elem != null && elem.FontScaleX > 0) ? elem.FontScaleX : 1.0;
                double lineSpacing = (elem != null && elem.LineSpacing > 0) ? elem.LineSpacing : 1.0;
                double letterSpacing = (elem != null) ? elem.LetterSpacing : 0.0;

                var textBlock = new TextBlock
                {
                    FontFamily = new FontFamily(string.IsNullOrWhiteSpace(fontName) ? "맑은 고딕" : fontName),
                    FontSize = effectiveFontSizePt * 1.35,
                    Foreground = (index == 99) ? Brushes.DarkBlue : ((index == 101) ? Brushes.DarkGreen : Brushes.Black),
                    FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal,
                    TextDecorations = (elem != null && elem.IsUnderline) ? TextDecorations.Underline : null,
                    TextAlignment = textAlignment,
                    TextWrapping = isWrapped ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    LineHeight = effectiveFontSizePt * 1.35 * lineSpacing
                };

                if (Math.Abs(scaleX - 1.0) > 0.001)
                {
                    textBlock.LayoutTransform = new ScaleTransform(scaleX, 1.0);
                }

                if (Math.Abs(letterSpacing) > 0.001 && !string.IsNullOrEmpty(renderedText))
                {
                    textBlock.Inlines.Clear();
                    double spacePx = (effectiveFontSizePt * 1.35) * letterSpacing;
                    for (int ci = 0; ci < renderedText.Length; ci++)
                    {
                        char c = renderedText[ci];
                        if (c == '\r') continue;
                        if (c == '\n')
                        {
                            textBlock.Inlines.Add(new LineBreak());
                            continue;
                        }
                        textBlock.Inlines.Add(new Run(c.ToString()));
                        if (ci < renderedText.Length - 1 && renderedText[ci + 1] != '\n' && renderedText[ci + 1] != '\r')
                        {
                            if (spacePx > 0)
                            {
                                double spaceSize = Math.Max(0.5, spacePx * 3.5);
                                textBlock.Inlines.Add(new Run(" ") { FontSize = spaceSize });
                            }
                        }
                    }
                }
                else
                {
                    textBlock.Text = renderedText;
                }

                container.Child = textBlock;
            }

            if (elem != null && elem.Rotation != 0)
            {
                container.RenderTransformOrigin = new Point(0.5, 0.5);
                container.RenderTransform = new RotateTransform(elem.Rotation);
            }

            Canvas.SetLeft(container, left);
            Canvas.SetTop(container, top);

            targetCanvas.Children.Add(container);

            // 선택된 아이템 우측 크기 조절 핸들 추가 (바코드/QR코드 및 텍스트 모두 지원)
            bool canResizeWidth = isSelected && ViewModel != null && 
                                  ((index == 99 || index == 101) || (index >= 0 && index < 8 && (!ViewModel.IsTableBorderEnabled || isBarcode))) &&
                                  !hasSelectionOverlay;

            if (canResizeWidth)
            {
                int colNum = (index == 99 || index == 101) ? index : index + 1;
                string handleToolTip = (index == 99) 
                    ? "드래그: 날짜/시간 박스 너비 조절" 
                    : ((index == 101) ? "드래그: 순번 박스 너비 조절" : (isBarcode ? $"드래그: {colNum}열 바코드/QR 너비 조절" : $"드래그: {colNum}열 박스 너비(자동 줄바꿈 폭) 조절"));
                
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
                Canvas.SetLeft(resizeHandle, left + itemWPx - 4);
                double handleTop = top + Math.Max(0, (itemHPx / 2.0) - 10);
                Canvas.SetTop(resizeHandle, handleTop);
                Panel.SetZIndex(resizeHandle, 100);
                targetCanvas.Children.Add(resizeHandle);
            }
        }

        #region Selection Overlay & Transform Handles (회전 핸들 & 8방향 리사이즈 핸들)

        private void DrawSelectionOverlay(Canvas targetCanvas, TemplateElement elem, bool showHandles = true)
        {
            if (targetCanvas == null || elem == null) return;

            double leftPx = elem.X * MM_TO_PX;
            double topPx = elem.Y * MM_TO_PX;
            double widthPx;
            double heightPx;
            GetElementVisualDimensions(elem, out widthPx, out heightPx);

            // 1. 선(LINE) 요소 전용 조작 오버레이 (시작점 / 끝점 원형 핸들)
            if (elem.Type == ElementType.LINE)
            {
                var lineOverlay = new Canvas
                {
                    Width = widthPx,
                    Height = Math.Max(16.0, elem.LineWidth + 12),
                    IsHitTestVisible = true
                };
                Canvas.SetLeft(lineOverlay, leftPx);
                Canvas.SetTop(lineOverlay, topPx);
                Panel.SetZIndex(lineOverlay, 1000);

                if (elem.Rotation != 0)
                {
                    lineOverlay.RenderTransformOrigin = new Point(0, 0.5); // 시작점 기준 회전
                    lineOverlay.RenderTransform = new RotateTransform(elem.Rotation);
                }

                if (!showHandles)
                {
                    // 다중 선택 시 보조 요소: 테두리 윤곽만 표시
                    var selLine = new Line
                    {
                        X1 = 0,
                        Y1 = lineOverlay.Height / 2.0,
                        X2 = widthPx,
                        Y2 = lineOverlay.Height / 2.0,
                        Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                        StrokeThickness = 2.0,
                        StrokeDashArray = new DoubleCollection { 3, 2 },
                        IsHitTestVisible = false
                    };
                    lineOverlay.Children.Add(selLine);
                    targetCanvas.Children.Add(lineOverlay);
                    return;
                }

                double centerY = lineOverlay.Height / 2.0;

                // 시작점 핸들 (Cross Cursor)
                var hStart = CreateCircleHandle("HANDLE_LINE_START", Cursors.Cross, "선 시작점 드래그: 위치 및 길이 조절");
                Canvas.SetLeft(hStart, -6);
                Canvas.SetTop(hStart, centerY - 6);
                lineOverlay.Children.Add(hStart);

                // 끝점 핸들 (Cross Cursor)
                var hEnd = CreateCircleHandle("HANDLE_LINE_END", Cursors.Cross, "선 끝점 드래그: 길이 및 회전각도 조절");
                Canvas.SetLeft(hEnd, widthPx - 6);
                Canvas.SetTop(hEnd, centerY - 6);
                lineOverlay.Children.Add(hEnd);

                targetCanvas.Children.Add(lineOverlay);
                return;
            }

            // 2. 일반 요소 (텍스트, 바코드, QR코드, 도형, 이미지) 통합 변형 오버레이
            var overlay = new Canvas
            {
                Width = widthPx,
                Height = heightPx,
                IsHitTestVisible = true,
                Tag = elem
            };
            Canvas.SetLeft(overlay, leftPx);
            Canvas.SetTop(overlay, topPx);
            Panel.SetZIndex(overlay, 1000);

            if (elem.Rotation != 0)
            {
                overlay.RenderTransformOrigin = new Point(0.5, 0.5);
                overlay.RenderTransform = new RotateTransform(elem.Rotation);
            }

            // [A] 선택 외곽 테두리 (텍스트를 가리지 않도록 Fill은 투명하게 설정)
            var borderRect = new Rectangle
            {
                Width = widthPx,
                Height = heightPx,
                Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                StrokeThickness = showHandles ? 2.0 : 1.5,
                StrokeDashArray = showHandles ? null : new DoubleCollection { 3, 2 },
                RadiusX = 2.0,
                RadiusY = 2.0,
                Fill = Brushes.Transparent, // 텍스트 가림 방지
                IsHitTestVisible = false
            };
            overlay.Children.Add(borderRect);

            if (!showHandles)
            {
                targetCanvas.Children.Add(overlay);
                return;
            }

            // [B] 상단 회전 핸들 (연결선 + ↻ 원형 버튼)
            double rotDist = 26.0;
            var rotLine = new Line
            {
                X1 = widthPx / 2.0,
                Y1 = 0,
                X2 = widthPx / 2.0,
                Y2 = -rotDist,
                Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                StrokeThickness = 1.8,
                IsHitTestVisible = false
            };
            overlay.Children.Add(rotLine);

            var rotBtn = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2.0),
                Cursor = Cursors.Hand,
                Tag = "HANDLE_ROTATE",
                ToolTip = $"회전 핸들: 마우스 드래그로 회전 방향 조절 (현재: {elem.Rotation}°)"
            };
            var rotIcon = new TextBlock
            {
                Text = "↻",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -1, 0, 0)
            };
            rotBtn.Child = rotIcon;
            Canvas.SetLeft(rotBtn, (widthPx / 2.0) - 12);
            Canvas.SetTop(rotBtn, -rotDist - 12);
            overlay.Children.Add(rotBtn);

            // [C] 4개 테두리선(상, 하, 좌, 우) 직접 드래그 리사이즈 감지 영역 (마우스 조작 편의 극대화)
            double hR = 5.5; // 포인트 반경 (지름 11px)
            double stripThick = 8.0;
            double halfThick = stripThick / 2.0;

            // 상단 테두리선
            AddResizeEdgeStrip(overlay, hR, -halfThick, Math.Max(1.0, widthPx - 2 * hR), stripThick, "HANDLE_RESIZE_N", Cursors.SizeNS, "상단 테두리 드래그: 크기 조절");
            // 하단 테두리선
            AddResizeEdgeStrip(overlay, hR, heightPx - halfThick, Math.Max(1.0, widthPx - 2 * hR), stripThick, "HANDLE_RESIZE_S", Cursors.SizeNS, "하단 테두리 드래그: 크기 조절");
            // 좌측 테두리선
            AddResizeEdgeStrip(overlay, -halfThick, hR, stripThick, Math.Max(1.0, heightPx - 2 * hR), "HANDLE_RESIZE_W", Cursors.SizeWE, "좌측 테두리 드래그: 너비 조절");
            // 우측 테두리선
            AddResizeEdgeStrip(overlay, widthPx - halfThick, hR, stripThick, Math.Max(1.0, heightPx - 2 * hR), "HANDLE_RESIZE_E", Cursors.SizeWE, "우측 테두리 드래그: 너비(폭) 조절");

            // [D] 정확히 8개의 리사이즈 포인트 (NW, N, NE, E, SE, S, SW, W - 테두리선보다 위에 배치되어 정확한 핸들 클릭 보장)
            // 1. 좌상단 (NW)
            AddResizeCircleHandle(overlay, -hR, -hR, "HANDLE_RESIZE_NW", Cursors.SizeNWSE, "좌상단 크기 조절");
            // 2. 상단 중앙 (N)
            AddResizeCircleHandle(overlay, (widthPx / 2.0) - hR, -hR, "HANDLE_RESIZE_N", Cursors.SizeNS, "상단 높이 조절");
            // 3. 우상단 (NE)
            AddResizeCircleHandle(overlay, widthPx - hR, -hR, "HANDLE_RESIZE_NE", Cursors.SizeNESW, "우상단 크기 조절");

            // 4. 우측 중앙 (E)
            AddResizeCircleHandle(overlay, widthPx - hR, (heightPx / 2.0) - hR, "HANDLE_RESIZE_E", Cursors.SizeWE, "우측 너비(폭) 조절");
            // 5. 우하단 (SE)
            AddResizeCircleHandle(overlay, widthPx - hR, heightPx - hR, "HANDLE_RESIZE_SE", Cursors.SizeNWSE, "우하단 크기 조절 (너비+높이/폰트 비례)");

            // 6. 하단 중앙 (S)
            AddResizeCircleHandle(overlay, (widthPx / 2.0) - hR, heightPx - hR, "HANDLE_RESIZE_S", Cursors.SizeNS, "하단 높이 조절");
            // 7. 좌하단 (SW)
            AddResizeCircleHandle(overlay, -hR, heightPx - hR, "HANDLE_RESIZE_SW", Cursors.SizeNESW, "좌하단 크기 조절");
            // 8. 좌측 중앙 (W)
            AddResizeCircleHandle(overlay, -hR, (heightPx / 2.0) - hR, "HANDLE_RESIZE_W", Cursors.SizeWE, "좌측 너비 조절");

            targetCanvas.Children.Add(overlay);
        }

        private Border CreateCircleHandle(string tag, Cursor cursor, string tooltip)
        {
            return new Border
            {
                Width = 13,
                Height = 13,
                CornerRadius = new CornerRadius(6.5),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderThickness = new Thickness(2.2),
                Cursor = cursor,
                Tag = tag,
                ToolTip = tooltip
            };
        }

        private void AddResizeCircleHandle(Canvas container, double x, double y, string tag, Cursor cursor, string tooltip)
        {
            var h = new Border
            {
                Width = 11,
                Height = 11,
                CornerRadius = new CornerRadius(5.5),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderThickness = new Thickness(1.8),
                Cursor = cursor,
                Tag = tag,
                ToolTip = tooltip
            };
            Canvas.SetLeft(h, x);
            Canvas.SetTop(h, y);
            container.Children.Add(h);
        }

        private void AddResizeEdgeStrip(Canvas container, double x, double y, double w, double h, string tag, Cursor cursor, string tooltip)
        {
            if (w <= 0 || h <= 0) return;
            var strip = new Border
            {
                Width = w,
                Height = h,
                Background = Brushes.Transparent,
                Cursor = cursor,
                Tag = tag,
                ToolTip = tooltip
            };
            Canvas.SetLeft(strip, x);
            Canvas.SetTop(strip, y);
            container.Children.Add(strip);
        }

        #endregion

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

        #region Canvas In-Place Inline Text Editor (캔버스 직접 더블클릭 편집)

        private TextBox _canvasInlineTextBox = null;

        private void FocusInspectorContentTextBox()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var tb = FindName("TxtInspectorContent") as TextBox;
                if (tb != null && tb.IsVisible)
                {
                    tb.Focus();
                    tb.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void StartInlineCanvasTextEdit(Canvas canvas, TemplateElement elem)
        {
            if (canvas == null || elem == null) return;

            CommitInlineCanvasTextEdit();

            double fontSize = (elem.FontSize > 0 ? elem.FontSize : 14) * 1.35;
            _canvasInlineTextBox = new TextBox
            {
                Text = elem.Content ?? "",
                FontFamily = new FontFamily(string.IsNullOrWhiteSpace(elem.FontName) ? "맑은 고딕" : elem.FontName),
                FontSize = fontSize,
                FontWeight = elem.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elem.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(4, 2, 4, 2),
                AcceptsReturn = true,
                TextWrapping = elem.Width > 0 ? TextWrapping.Wrap : TextWrapping.NoWrap,
                Tag = elem
            };

            double wPx = elem.Width > 0 ? elem.Width * MM_TO_PX : Math.Max(80, (elem.Content?.Length ?? 5) * (fontSize * 0.9) + 30);
            _canvasInlineTextBox.Width = Math.Max(80, wPx + 10);
            _canvasInlineTextBox.MinHeight = Math.Max(26, elem.Height > 0 ? elem.Height * MM_TO_PX : 26);

            Canvas.SetLeft(_canvasInlineTextBox, elem.X * MM_TO_PX);
            Canvas.SetTop(_canvasInlineTextBox, elem.Y * MM_TO_PX);
            Panel.SetZIndex(_canvasInlineTextBox, 9999);

            _canvasInlineTextBox.KeyDown += (s, ke) =>
            {
                if (ke.Key == Key.Enter)
                {
                    bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    if (isCtrl)
                    {
                        CommitInlineCanvasTextEdit();
                        ke.Handled = true;
                    }
                    // 일반 Enter는 AcceptsReturn=true에 의해 줄바꿈 삽입
                }
                else if (ke.Key == Key.Escape)
                {
                    CancelInlineCanvasTextEdit();
                    ke.Handled = true;
                }
            };

            _canvasInlineTextBox.LostFocus += (s, lfe) =>
            {
                CommitInlineCanvasTextEdit();
            };

            canvas.Children.Add(_canvasInlineTextBox);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _canvasInlineTextBox?.Focus();
                _canvasInlineTextBox?.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void StartInlineCanvasTableCellEdit(Canvas canvas, TemplateElement elem, Point posInsideTable)
        {
            if (canvas == null || elem == null) return;
            elem.EnsureTableData();

            int rows = Math.Max(1, elem.Rows);
            int cols = Math.Max(1, elem.Columns);
            double totalW = Math.Max(20, elem.Width * MM_TO_PX);
            double totalH = Math.Max(15, elem.Height * MM_TO_PX);

            double[] colWeights = elem.ColumnWidths;
            double[] rowWeights = elem.RowHeights;

            double[] colBoundaries = new double[cols + 1];
            colBoundaries[0] = 0;
            double totalColWeight = 0;
            if (colWeights != null && colWeights.Length == cols)
            {
                foreach (var w in colWeights) totalColWeight += Math.Max(0.1, w);
            }
            if (totalColWeight <= 0) totalColWeight = cols;
            double currX = 0;
            for (int c = 0; c < cols; c++)
            {
                double weight = (colWeights != null && colWeights.Length == cols) ? Math.Max(0.1, colWeights[c]) : 1.0;
                currX += totalW * (weight / totalColWeight);
                colBoundaries[c + 1] = (c == cols - 1) ? totalW : currX;
            }

            double[] rowBoundaries = new double[rows + 1];
            rowBoundaries[0] = 0;
            double totalRowWeight = 0;
            if (rowWeights != null && rowWeights.Length == rows)
            {
                foreach (var h in rowWeights) totalRowWeight += Math.Max(0.1, h);
            }
            if (totalRowWeight <= 0) totalRowWeight = rows;
            double currY = 0;
            for (int r = 0; r < rows; r++)
            {
                double weight = (rowWeights != null && rowWeights.Length == rows) ? Math.Max(0.1, rowWeights[r]) : 1.0;
                currY += totalH * (weight / totalRowWeight);
                rowBoundaries[r + 1] = (r == rows - 1) ? totalH : currY;
            }

            int targetC = 0;
            for (int c = 0; c < cols; c++)
            {
                if (posInsideTable.X >= colBoundaries[c] && posInsideTable.X < colBoundaries[c + 1])
                {
                    targetC = c;
                    break;
                }
                if (c == cols - 1 && posInsideTable.X >= colBoundaries[c]) targetC = c;
            }

            int targetR = 0;
            for (int r = 0; r < rows; r++)
            {
                if (posInsideTable.Y >= rowBoundaries[r] && posInsideTable.Y < rowBoundaries[r + 1])
                {
                    targetR = r;
                    break;
                }
                if (r == rows - 1 && posInsideTable.Y >= rowBoundaries[r]) targetR = r;
            }

            CommitInlineCanvasTextEdit();

            double cellLeft = elem.X * MM_TO_PX + colBoundaries[targetC];
            double cellTop = elem.Y * MM_TO_PX + rowBoundaries[targetR];
            double cellWidth = colBoundaries[targetC + 1] - colBoundaries[targetC];
            double cellHeight = rowBoundaries[targetR + 1] - rowBoundaries[targetR];

            string currentCellText = (elem.TableData != null && targetR < elem.TableData.Length && targetC < elem.TableData[targetR].Length)
                ? (elem.TableData[targetR][targetC] ?? "")
                : "";

            var cellStyle = elem.GetCellResolvedStyle(targetR, targetC);
            string activeFontName = string.IsNullOrWhiteSpace(cellStyle.FontName) ? (string.IsNullOrWhiteSpace(elem.FontName) ? "맑은 고딕" : elem.FontName) : cellStyle.FontName;
            double fontSize = (cellStyle.FontSize > 0 ? cellStyle.FontSize : 9.0) * 1.35;
            _canvasInlineTextBox = new TextBox
            {
                Text = currentCellText,
                FontFamily = new FontFamily(activeFontName),
                FontSize = fontSize,
                FontWeight = cellStyle.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = elem.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(4, 2, 4, 2),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Width = Math.Max(40, cellWidth),
                MinHeight = Math.Max(22, cellHeight),
                Tag = new TableCellEditInfo { Element = elem, Row = targetR, Column = targetC }
            };

            Canvas.SetLeft(_canvasInlineTextBox, cellLeft);
            Canvas.SetTop(_canvasInlineTextBox, cellTop);
            Panel.SetZIndex(_canvasInlineTextBox, 9999);

            _canvasInlineTextBox.KeyDown += (s, ke) =>
            {
                if (ke.Key == Key.Enter)
                {
                    bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    if (isCtrl)
                    {
                        CommitInlineCanvasTextEdit();
                        ke.Handled = true;
                    }
                    // Enter 단독 입력 시에는 AcceptsReturn=true에 의해 줄바꿈 삽입되도록 ke.Handled를 설정하지 않음
                }
                else if (ke.Key == Key.Escape)
                {
                    CancelInlineCanvasTextEdit();
                    ke.Handled = true;
                }
            };

            _canvasInlineTextBox.LostFocus += (s, lfe) =>
            {
                CommitInlineCanvasTextEdit();
            };

            canvas.Children.Add(_canvasInlineTextBox);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _canvasInlineTextBox?.Focus();
                _canvasInlineTextBox?.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CommitInlineCanvasTextEdit()
        {
            if (_canvasInlineTextBox != null)
            {
                var box = _canvasInlineTextBox;
                _canvasInlineTextBox = null;

                if (box.Tag is TableCellEditInfo cellInfo && ViewModel != null)
                {
                    if (cellInfo.Element != null && cellInfo.Element.TableData != null &&
                        cellInfo.Row < cellInfo.Element.TableData.Length &&
                        cellInfo.Column < cellInfo.Element.TableData[cellInfo.Row].Length)
                    {
                        cellInfo.Element.TableData[cellInfo.Row][cellInfo.Column] = box.Text;
                        ViewModel.RefreshTableCells();
                        ViewModel.UpdatePreview();
                    }
                }
                else if (box.Tag is TemplateElement elem && ViewModel != null)
                {
                    string newText = box.Text;
                    elem.Content = newText;
                    if (ViewModel.SelectedElement == elem)
                    {
                        ViewModel.SelectedElementContent = newText;
                    }
                    ViewModel.SyncElementToLegacyColumn(elem);
                    ViewModel.UpdatePreview();
                }

                if (box.Parent is Canvas parent)
                {
                    parent.Children.Remove(box);
                }

                RedrawInteractiveCanvas();
            }
        }

        private void CancelInlineCanvasTextEdit()
        {
            if (_canvasInlineTextBox != null)
            {
                var box = _canvasInlineTextBox;
                _canvasInlineTextBox = null;

                if (box.Parent is Canvas parent)
                {
                    parent.Children.Remove(box);
                }

                RedrawInteractiveCanvas();
            }
        }

        #endregion

        private void StartCanvasDrag(Canvas canvas, MouseButtonEventArgs e)
        {
            if (canvas == null || ViewModel == null) return;

            _hasMovedDuringDrag = false;

            // 캔버스 클릭 시 키보드 포커스를 즉시 전환하여 단축키(Del, Ctrl+C/V/Z/A 등)가 즉각 반응하도록 보장
            canvas.Focusable = true;
            canvas.Focus();

            // 기존 인라인 편집 중 다른 곳 클릭 시 자동 커밋
            if (_canvasInlineTextBox != null && !ReferenceEquals(e.OriginalSource, _canvasInlineTextBox))
            {
                CommitInlineCanvasTextEdit();
            }

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

                // 캔버스 인터랙션 시작 시 선택된 템플릿 요소 동기화 보장 (화면 첫 진입 또는 요소 선택 후 null 상태 원천 방지)
                if (_selectedTemplateElement == null && ViewModel.SelectedElement != null)
                {
                    _selectedTemplateElement = ViewModel.SelectedElement;
                }

                // 0. 요소 변형 핸들(회전 / 리사이즈 / 선 제어) 조작 인터랙션 검사
                if (tagStr == "HANDLE_ROTATE" && _selectedTemplateElement != null)
                {
                    _currentDragMode = TableDragMode.RotateElement;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    _startRotation = _selectedTemplateElement.Rotation;
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[{_selectedTemplateElement.HeaderName} 회전 조절] 시작: {_startRotation}°");
                    e.Handled = true;
                    return;
                }
                else if (tagStr == "HANDLE_LINE_START" && _selectedTemplateElement != null)
                {
                    _currentDragMode = TableDragMode.ResizeLineStart;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    _startElemX = _selectedTemplateElement.X;
                    _startElemY = _selectedTemplateElement.Y;
                    _startElemW = _selectedTemplateElement.Width;
                    _startRotation = _selectedTemplateElement.Rotation;
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[선 시작점 이동] 조작 시작");
                    e.Handled = true;
                    return;
                }
                else if (tagStr == "HANDLE_LINE_END" && _selectedTemplateElement != null)
                {
                    _currentDragMode = TableDragMode.ResizeLineEnd;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    _startElemX = _selectedTemplateElement.X;
                    _startElemY = _selectedTemplateElement.Y;
                    _startElemW = _selectedTemplateElement.Width;
                    _startRotation = _selectedTemplateElement.Rotation;
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[선 끝점/길이/회전 조절] 조작 시작");
                    e.Handled = true;
                    return;
                }
                else if (tagStr.StartsWith("HANDLE_RESIZE_") && _selectedTemplateElement != null)
                {
                    string dir = tagStr.Substring(14);
                    switch (dir)
                    {
                        case "NW": _currentDragMode = TableDragMode.ResizeElementNW; break;
                        case "N": _currentDragMode = TableDragMode.ResizeElementN; break;
                        case "NE": _currentDragMode = TableDragMode.ResizeElementNE; break;
                        case "E": _currentDragMode = TableDragMode.ResizeElementE; break;
                        case "SE": _currentDragMode = TableDragMode.ResizeElementSE; break;
                        case "S": _currentDragMode = TableDragMode.ResizeElementS; break;
                        case "SW": _currentDragMode = TableDragMode.ResizeElementSW; break;
                        case "W": _currentDragMode = TableDragMode.ResizeElementW; break;
                    }
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);

                    // 초기 크기가 0.0인 경우(자연 크기 모드) 실제 렌더링된 시각 치수로 시작 기준값 보정
                    GetElementVisualDimensions(_selectedTemplateElement, out double vWPx, out double vHPx);
                    double vWMm = Math.Round(vWPx / MM_TO_PX, 1);
                    double vHMm = Math.Round(vHPx / MM_TO_PX, 1);

                    _startElemX = _selectedTemplateElement.X;
                    _startElemY = _selectedTemplateElement.Y;
                    _startElemW = (_selectedTemplateElement.Width > 0) ? _selectedTemplateElement.Width : vWMm;
                    _startElemH = (_selectedTemplateElement.Height > 0) ? _selectedTemplateElement.Height : vHMm;
                    _startFontSize = (_selectedTemplateElement.FontSize > 0) ? _selectedTemplateElement.FontSize : 10.0;
                    _startRotation = _selectedTemplateElement.Rotation;
                    canvas.CaptureMouse();
                    UpdateCustomDragInfo($"[{_selectedTemplateElement.HeaderName} 크기 조절] {_startElemW:F1} × {_startElemH:F1} mm");
                    e.Handled = true;
                    return;
                }

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

                // 2. 자유 배치 템플릿 요소 (텍스트, 바코드, QR, 선, 도형, 이미지) 직접 선택 및 드래그
                bool isMultiKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (hitElement != null && hitElement.Tag is TemplateElement hitElem)
                {
                    if (isMultiKey)
                    {
                        ViewModel.ToggleElementSelection(hitElem);
                        _selectedTemplateElement = ViewModel.SelectedElement;
                    }
                    else
                    {
                        if (!ViewModel.SelectedElements.Contains(hitElem))
                        {
                            ViewModel.SelectedElement = hitElem;
                        }
                        _selectedTemplateElement = hitElem;
                    }

                    // 더블클릭 감지: 미리보기 영역(캔버스)에서 바로 인라인 편집 & 우측 텍스트 입력창 포커스
                    if (e.ClickCount == 2)
                    {
                        FocusInspectorContentTextBox();
                        if (hitElem.Type == ElementType.TEXT || hitElem.Type == ElementType.DATE_TIME)
                        {
                            StartInlineCanvasTextEdit(canvas, hitElem);
                            e.Handled = true;
                            return;
                        }
                        else if (hitElem.Type == ElementType.TABLE)
                        {
                            Point posInsideTable = e.GetPosition(hitElement);
                            StartInlineCanvasTableCellEdit(canvas, hitElem, posInsideTable);
                            e.Handled = true;
                            return;
                        }
                    }

                    _currentDragMode = TableDragMode.MoveTemplateElement;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);
                    _startElemX = hitElem.X;
                    _startElemY = hitElem.Y;
                    _startElemW = hitElem.Width;
                    _startElemH = hitElem.Height;
                    _startFontSize = hitElem.FontSize;
                    _startRotation = hitElem.Rotation;

                    // 다중 선택 요소들의 시작 위치 저장
                    _startTemplateElemOffsets.Clear();
                    var moveTargets = ViewModel.HasMultipleSelectedElements ? ViewModel.SelectedElements : (ViewModel.SelectedElement != null ? new List<TemplateElement> { ViewModel.SelectedElement } : new List<TemplateElement>());
                    foreach (var target in moveTargets)
                    {
                        _startTemplateElemOffsets[target] = new Point(target.X, target.Y);
                    }

                    canvas.CaptureMouse();
                    RedrawInteractiveCanvas();
                    string selDesc = ViewModel.HasMultipleSelectedElements ? $"{ViewModel.SelectedElementsCount}개 요소 선택" : $"[{hitElem.HeaderName}] 선택 (X={hitElem.X:F1}mm, Y={hitElem.Y:F1}mm)";
                    UpdateCustomDragInfo(selDesc);
                    e.Handled = true;
                    return;
                }

                // 3. 일반 아이템 및 표 내부 셀 (위치 이동)
                if (hitElement != null && int.TryParse(tagStr, out int rawIndex))
                {
                    _currentDragMode = TableDragMode.MoveItemOrTable;
                    int index = rawIndex;
                    if (index < 0 && index >= -8)
                    {
                        index = Math.Abs(index) - 1; // 헤더 셀(-1~-8) 클릭 시 해당 컬럼 인덱스(0~7)로 매핑
                    }

                    // 자유 배치 모드이거나 바코드 컬럼인 경우 대응되는 TemplateElement 찾아 활성화
                    bool isBarcodeColItem = (index >= 0 && index < 8) && ViewModel.GetBarcodeByIndex(index + 1);
                    if (!ViewModel.IsTableBorderEnabled || isBarcodeColItem || index == 99 || index == 101)
                    {
                        int targetColNum = (index == 99 || index == 101) ? index : index + 1;
                        if (ViewModel.CurrentTemplate == null)
                        {
                            ViewModel.InitializeDefaultTemplateFromCurrentSettings();
                        }
                        if (ViewModel.CurrentTemplate != null)
                        {
                            var matchingElem = ViewModel.CurrentTemplate.Elements.FirstOrDefault(el =>
                                (index == 99 && el.Id == "DATETIME_ELEMENT") ||
                                (index == 101 && el.Id == "SERIAL_ELEMENT") ||
                                (el.IsVariable && el.InputOrder == targetColNum));
                            if (matchingElem != null)
                            {
                                if (isMultiKey)
                                {
                                    ViewModel.ToggleElementSelection(matchingElem);
                                    _selectedTemplateElement = ViewModel.SelectedElement;
                                }
                                else
                                {
                                    if (!ViewModel.SelectedElements.Contains(matchingElem))
                                    {
                                        ViewModel.SelectedElement = matchingElem;
                                    }
                                    _selectedTemplateElement = matchingElem;
                                }

                                if (_selectedTemplateElement != null)
                                {
                                    _startElemX = _selectedTemplateElement.X;
                                    _startElemY = _selectedTemplateElement.Y;
                                    _startElemW = _selectedTemplateElement.Width;
                                    _startElemH = _selectedTemplateElement.Height;
                                    _startFontSize = _selectedTemplateElement.FontSize;
                                    _startRotation = _selectedTemplateElement.Rotation;
                                }
                            }
                        }
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
                    // 빈 공간 클릭: 러버밴드(영역 드래그 사각형) 다중 선택 모드 진입
                    if (!isMultiKey)
                    {
                        _selectedItemIndices.Clear();
                        _selectedItemIndex = -999;
                        ViewModel.SelectedItemIndex = -999;
                        _selectedTemplateElement = null;
                        ViewModel.ClearSelectedElements();
                        ViewModel.SelectedElement = null;
                        RedrawInteractiveCanvas();
                    }

                    _currentDragMode = TableDragMode.RubberBandSelect;
                    _isDragging = true;
                    _activeDragCanvas = canvas;
                    _dragStartPoint = e.GetPosition(canvas);

                    if (_rubberBandRect != null)
                    {
                        canvas.Children.Remove(_rubberBandRect);
                        _rubberBandRect = null;
                    }

                    _rubberBandRect = new Rectangle
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                        StrokeThickness = 1.2,
                        StrokeDashArray = new DoubleCollection { 3, 2 },
                        Fill = new SolidColorBrush(Color.FromArgb(35, 59, 130, 246)),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(_rubberBandRect, _dragStartPoint.X);
                    Canvas.SetTop(_rubberBandRect, _dragStartPoint.Y);
                    _rubberBandRect.Width = 0;
                    _rubberBandRect.Height = 0;
                    Panel.SetZIndex(_rubberBandRect, 2000);
                    canvas.Children.Add(_rubberBandRect);

                    canvas.CaptureMouse();
                    e.Handled = true;
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
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > 2 || Math.Abs(currentPoint.Y - _dragStartPoint.Y) > 2)
                {
                    _hasMovedDuringDrag = true;
                }
                double diffXMm = (currentPoint.X - _dragStartPoint.X) / MM_TO_PX;
                double diffYMm = (currentPoint.Y - _dragStartPoint.Y) / MM_TO_PX;

                int effW = ViewModel.EffectivePaperWidth > 0 ? ViewModel.EffectivePaperWidth : ViewModel.PaperWidth;
                int effH = ViewModel.EffectivePaperHeight > 0 ? ViewModel.EffectivePaperHeight : ViewModel.PaperHeight;
                int maxX = Math.Max(10, effW - 2);
                int maxY = Math.Max(10, effH - 2);

                switch (_currentDragMode)
                {
                    case TableDragMode.MoveTemplateElement:
                        if (_startTemplateElemOffsets.Count > 0)
                        {
                            foreach (var kvp in _startTemplateElemOffsets)
                            {
                                var elem = kvp.Key;
                                var startPt = kvp.Value;
                                double newX = Math.Max(0, Math.Min(effW - 2, Math.Round((startPt.X + diffXMm) * 2.0) / 2.0));
                                double newY = Math.Max(0, Math.Min(effH - 2, Math.Round((startPt.Y + diffYMm) * 2.0) / 2.0));
                                elem.X = newX;
                                elem.Y = newY;
                            }
                            RedrawActiveDragCanvas(canvas);
                            if (ViewModel.HasMultipleSelectedElements)
                            {
                                UpdateCustomDragInfo($"{ViewModel.SelectedElementsCount}개 요소 이동 중");
                            }
                            else if (_selectedTemplateElement != null)
                            {
                                UpdateCustomDragInfo($"[{_selectedTemplateElement.HeaderName}] X={_selectedTemplateElement.X:F1}mm, Y={_selectedTemplateElement.Y:F1}mm (드래그 중)");
                            }
                        }
                        else if (_selectedTemplateElement != null)
                        {
                            double newX = Math.Max(0, Math.Min(effW - 2, Math.Round((_startElemX + diffXMm) * 2.0) / 2.0)); // 0.5mm 단위 스냅
                            double newY = Math.Max(0, Math.Min(effH - 2, Math.Round((_startElemY + diffYMm) * 2.0) / 2.0));
                            if (_selectedTemplateElement.X != newX || _selectedTemplateElement.Y != newY)
                            {
                                _selectedTemplateElement.X = newX;
                                _selectedTemplateElement.Y = newY;
                                RedrawActiveDragCanvas(canvas);
                            }
                            UpdateCustomDragInfo($"[{_selectedTemplateElement.HeaderName}] X={newX:F1}mm, Y={newY:F1}mm (드래그 중)");
                        }
                        break;

                    case TableDragMode.RubberBandSelect:
                        if (_rubberBandRect != null)
                        {
                            double rx = Math.Min(_dragStartPoint.X, currentPoint.X);
                            double ry = Math.Min(_dragStartPoint.Y, currentPoint.Y);
                            double rw = Math.Abs(currentPoint.X - _dragStartPoint.X);
                            double rh = Math.Abs(currentPoint.Y - _dragStartPoint.Y);

                            Canvas.SetLeft(_rubberBandRect, rx);
                            Canvas.SetTop(_rubberBandRect, ry);
                            _rubberBandRect.Width = rw;
                            _rubberBandRect.Height = rh;

                            UpdateCustomDragInfo($"[영역 선택] {rw / MM_TO_PX:F1} × {rh / MM_TO_PX:F1} mm");
                        }
                        break;

                    case TableDragMode.RotateElement:
                        if (_selectedTemplateElement != null)
                        {
                            var elem = _selectedTemplateElement;
                            double cxMm = elem.X + (elem.Width / 2.0);
                            double cyMm = elem.Y + (elem.Height / 2.0);
                            if (elem.Type == ElementType.LINE)
                            {
                                cxMm = elem.X;
                                cyMm = elem.Y;
                            }
                            double curXMm = currentPoint.X / MM_TO_PX;
                            double curYMm = currentPoint.Y / MM_TO_PX;
                            double rad = Math.Atan2(curYMm - cyMm, curXMm - cxMm);
                            double deg = rad * (180.0 / Math.PI);
                            double newAngle = deg + 90.0; // 상단 회전 핸들 기준 (0도가 12시 방향)
                            newAngle = (newAngle % 360.0 + 360.0) % 360.0;

                            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                            if (!isShiftDown)
                            {
                                newAngle = Math.Round(newAngle / 15.0) * 15.0; // 15도 스냅
                                newAngle = (newAngle % 360.0 + 360.0) % 360.0;
                            }

                            int finalDeg = (int)Math.Round(newAngle);
                            if (elem.Rotation != finalDeg)
                            {
                                elem.Rotation = finalDeg;
                                ViewModel.SelectedElementRotation = finalDeg;
                                RedrawActiveDragCanvas(canvas);
                            }
                            UpdateCustomDragInfo($"[{elem.HeaderName} 회전] {finalDeg}°" + (isShiftDown ? " (자유 각도)" : " (15° 스냅)"));
                        }
                        break;

                    case TableDragMode.ResizeLineStart:
                        if (_selectedTemplateElement != null)
                        {
                            var elem = _selectedTemplateElement;
                            double rad = _startRotation * Math.PI / 180.0;
                            double endXMm = _startElemX + _startElemW * Math.Cos(rad);
                            double endYMm = _startElemY + _startElemW * Math.Sin(rad);

                            double newStartX = Math.Max(0, Math.Min(effW, currentPoint.X / MM_TO_PX));
                            double newStartY = Math.Max(0, Math.Min(effH, currentPoint.Y / MM_TO_PX));

                            double dx = endXMm - newStartX;
                            double dy = endYMm - newStartY;
                            double newLen = Math.Sqrt(dx * dx + dy * dy);
                            double newAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

                            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                            if (!isShiftDown)
                            {
                                newAngle = Math.Round(newAngle / 15.0) * 15.0;
                            }
                            int finalAngle = (int)Math.Round((newAngle % 360.0 + 360.0) % 360.0);

                            elem.X = Math.Round(newStartX, 1);
                            elem.Y = Math.Round(newStartY, 1);
                            elem.Width = Math.Max(1.0, Math.Round(newLen, 1));
                            elem.Rotation = finalAngle;
                            ViewModel.SelectedElementRotation = finalAngle;
                            RedrawActiveDragCanvas(canvas);
                            UpdateCustomDragInfo($"[선 시작점 이동] 길이: {elem.Width:F1}mm, 각도: {elem.Rotation}°");
                        }
                        break;

                    case TableDragMode.ResizeLineEnd:
                        if (_selectedTemplateElement != null)
                        {
                            var elem = _selectedTemplateElement;
                            double curXMm = Math.Max(0, Math.Min(effW, currentPoint.X / MM_TO_PX));
                            double curYMm = Math.Max(0, Math.Min(effH, currentPoint.Y / MM_TO_PX));

                            double dx = curXMm - _startElemX;
                            double dy = curYMm - _startElemY;
                            double newLen = Math.Sqrt(dx * dx + dy * dy);
                            double newAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;

                            bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                            if (!isShiftDown)
                            {
                                newAngle = Math.Round(newAngle / 15.0) * 15.0;
                            }
                            int finalAngle = (int)Math.Round((newAngle % 360.0 + 360.0) % 360.0);

                            elem.Width = Math.Max(1.0, Math.Round(newLen, 1));
                            elem.Rotation = finalAngle;
                            ViewModel.SelectedElementRotation = finalAngle;
                            RedrawActiveDragCanvas(canvas);
                            UpdateCustomDragInfo($"[선 끝점 조절] 길이: {elem.Width:F1}mm, 각도: {elem.Rotation}°");
                        }
                        break;

                    case TableDragMode.ResizeElementNW:
                    case TableDragMode.ResizeElementN:
                    case TableDragMode.ResizeElementNE:
                    case TableDragMode.ResizeElementE:
                    case TableDragMode.ResizeElementSE:
                    case TableDragMode.ResizeElementS:
                    case TableDragMode.ResizeElementSW:
                    case TableDragMode.ResizeElementW:
                        if (_selectedTemplateElement != null)
                        {
                            var elem = _selectedTemplateElement;
                            double rotRad = _startRotation * Math.PI / 180.0;
                            double cos = Math.Cos(rotRad);
                            double sin = Math.Sin(rotRad);

                            double localDx = diffXMm * cos + diffYMm * sin;
                            double localDy = -diffXMm * sin + diffYMm * cos;

                            double newW = _startElemW;
                            double newH = _startElemH;
                            double localShiftX = 0;
                            double localShiftY = 0;

                            const double MIN_SIZE = 3.0;

                            if (elem.Type == ElementType.TEXT || elem.Type == ElementType.DATE_TIME)
                            {
                                bool isCorner = (_currentDragMode == TableDragMode.ResizeElementSE ||
                                                 _currentDragMode == TableDragMode.ResizeElementSW ||
                                                 _currentDragMode == TableDragMode.ResizeElementNE ||
                                                 _currentDragMode == TableDragMode.ResizeElementNW);
                                bool isVert = (_currentDragMode == TableDragMode.ResizeElementS ||
                                               _currentDragMode == TableDragMode.ResizeElementN);
                                bool isHoriz = (_currentDragMode == TableDragMode.ResizeElementE ||
                                                _currentDragMode == TableDragMode.ResizeElementW);

                                double baseFont = (_startFontSize > 0) ? _startFontSize : 10.0;

                                if (isCorner)
                                {
                                    // 1. 모서리 드래그: 가로/세로 주된 변화량으로 폰트 크기 비례 조절
                                    double delta = (Math.Abs(localDx) > Math.Abs(localDy)) ? localDx : localDy;
                                    if (_currentDragMode == TableDragMode.ResizeElementNW) delta = (Math.Abs(localDx) > Math.Abs(localDy)) ? -localDx : -localDy;
                                    else if (_currentDragMode == TableDragMode.ResizeElementSW) delta = (Math.Abs(localDx) > Math.Abs(localDy)) ? -localDx : localDy;
                                    else if (_currentDragMode == TableDragMode.ResizeElementNE) delta = (Math.Abs(localDx) > Math.Abs(localDy)) ? localDx : -localDy;

                                    double scale = Math.Max(0.2, (_startElemW + delta) / Math.Max(1.0, _startElemW));
                                    int calcFont = (int)Math.Max(5, Math.Min(120, Math.Round(baseFont * scale)));
                                    elem.FontSize = calcFont;
                                    if (ViewModel != null)
                                    {
                                        ViewModel.SyncElementToLegacyColumn(elem);
                                        ViewModel.SelectedElementFontSize = calcFont;
                                    }

                                    double targetW = Math.Max(MIN_SIZE, _startElemW * scale);
                                    // 변경된 폰트 크기에 맞춰 텍스트와 테두리 치수 정밀 동기화
                                    MeasureTextDimensions(elem, targetW, out double wPx, out double hPx, out _, out _);
                                    newW = Math.Round(wPx / MM_TO_PX, 1);
                                    newH = Math.Round(hPx / MM_TO_PX, 1);

                                    double diffW = newW - _startElemW;
                                    double diffH = newH - _startElemH;
                                    if (_currentDragMode == TableDragMode.ResizeElementNW)
                                    {
                                        localShiftX = -diffW;
                                        localShiftY = -diffH;
                                    }
                                    else if (_currentDragMode == TableDragMode.ResizeElementSW)
                                    {
                                        localShiftX = -diffW;
                                    }
                                    else if (_currentDragMode == TableDragMode.ResizeElementNE)
                                    {
                                        localShiftY = -diffH;
                                    }
                                }
                                else if (isVert)
                                {
                                    // 2. 세로 드래그: 세로 높이 변화에 맞춰 폰트 크기 비례 조절
                                    double deltaY = (_currentDragMode == TableDragMode.ResizeElementS) ? localDy : -localDy;
                                    double targetH = Math.Max(MIN_SIZE, _startElemH + deltaY);
                                    double scale = Math.Max(0.2, targetH / Math.Max(1.0, _startElemH));
                                    int calcFont = (int)Math.Max(5, Math.Min(120, Math.Round(baseFont * scale)));
                                    elem.FontSize = calcFont;
                                    if (ViewModel != null)
                                    {
                                        ViewModel.SyncElementToLegacyColumn(elem);
                                        ViewModel.SelectedElementFontSize = calcFont;
                                    }

                                    MeasureTextDimensions(elem, _startElemW, out double wPx, out double hPx, out _, out _);
                                    newW = Math.Round(wPx / MM_TO_PX, 1);
                                    newH = Math.Round(hPx / MM_TO_PX, 1);

                                    if (_currentDragMode == TableDragMode.ResizeElementN)
                                    {
                                        localShiftY = _startElemH - newH;
                                    }
                                }
                                else if (isHoriz)
                                {
                                    // 3. 가로 드래그: 폰트 크기는 그대로 유지하고, 가로폭(Width)을 변경하여 좁아지면 자동 줄바꿈
                                    double deltaX = (_currentDragMode == TableDragMode.ResizeElementE) ? localDx : -localDx;
                                    double targetW = Math.Max(MIN_SIZE, _startElemW + deltaX);

                                    elem.FontSize = (int)baseFont;
                                    if (ViewModel != null)
                                    {
                                        ViewModel.SelectedElementFontSize = (int)baseFont;
                                    }

                                    // targetW를 기준으로 텍스트 자동 줄바꿈 및 전체 높이 재계산
                                    MeasureTextDimensions(elem, targetW, out double wPx, out double hPx, out _, out bool isWrapped);
                                    newW = Math.Round(targetW, 1);
                                    newH = Math.Round(hPx / MM_TO_PX, 1);

                                    if (ViewModel != null)
                                    {
                                        elem.Width = newW;
                                        elem.Height = newH;
                                        ViewModel.SyncElementToLegacyColumn(elem);
                                    }

                                    if (_currentDragMode == TableDragMode.ResizeElementW)
                                    {
                                        localShiftX = _startElemW - newW;
                                    }
                                }
                            }
                            else
                            {
                                // 일반 요소 (바코드, 도형, 이미지 등)
                                switch (_currentDragMode)
                                {
                                    case TableDragMode.ResizeElementE:
                                        newW = Math.Max(MIN_SIZE, _startElemW + localDx);
                                        break;
                                    case TableDragMode.ResizeElementW:
                                        newW = Math.Max(MIN_SIZE, _startElemW - localDx);
                                        localShiftX = _startElemW - newW;
                                        break;
                                    case TableDragMode.ResizeElementS:
                                        newH = Math.Max(MIN_SIZE, _startElemH + localDy);
                                        break;
                                    case TableDragMode.ResizeElementN:
                                        newH = Math.Max(MIN_SIZE, _startElemH - localDy);
                                        localShiftY = _startElemH - newH;
                                        break;
                                    case TableDragMode.ResizeElementSE:
                                        newW = Math.Max(MIN_SIZE, _startElemW + localDx);
                                        newH = Math.Max(MIN_SIZE, _startElemH + localDy);
                                        break;
                                    case TableDragMode.ResizeElementSW:
                                        newW = Math.Max(MIN_SIZE, _startElemW - localDx);
                                        localShiftX = _startElemW - newW;
                                        newH = Math.Max(MIN_SIZE, _startElemH + localDy);
                                        break;
                                    case TableDragMode.ResizeElementNE:
                                        newW = Math.Max(MIN_SIZE, _startElemW + localDx);
                                        newH = Math.Max(MIN_SIZE, _startElemH - localDy);
                                        localShiftY = _startElemH - newH;
                                        break;
                                    case TableDragMode.ResizeElementNW:
                                        newW = Math.Max(MIN_SIZE, _startElemW - localDx);
                                        localShiftX = _startElemW - newW;
                                        newH = Math.Max(MIN_SIZE, _startElemH - localDy);
                                        localShiftY = _startElemH - newH;
                                        break;
                                }

                                if (elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE)
                                {
                                    if (elem.Type == ElementType.QR_CODE)
                                    {
                                        bool isCornerDrag = (_currentDragMode == TableDragMode.ResizeElementSE ||
                                                             _currentDragMode == TableDragMode.ResizeElementSW ||
                                                             _currentDragMode == TableDragMode.ResizeElementNE ||
                                                             _currentDragMode == TableDragMode.ResizeElementNW);
                                        if (isCornerDrag)
                                        {
                                            // QR코드는 모서리 조절 시 완벽한 정사각형(1:1) 비율 유지
                                            double maxDim = Math.Max(newW, newH);
                                            newW = maxDim;
                                            newH = maxDim;
                                            if (_currentDragMode == TableDragMode.ResizeElementNW)
                                            {
                                                localShiftX = _startElemW - newW;
                                                localShiftY = _startElemH - newH;
                                            }
                                            else if (_currentDragMode == TableDragMode.ResizeElementSW)
                                            {
                                                localShiftX = _startElemW - newW;
                                            }
                                            else if (_currentDragMode == TableDragMode.ResizeElementNE)
                                            {
                                                localShiftY = _startElemH - newH;
                                            }
                                        }
                                    }

                                    int calcBarH = (int)Math.Max(10, Math.Min(120, Math.Round(newH * 2.5)));
                                    elem.FontSize = calcBarH;
                                    if (ViewModel != null)
                                    {
                                        ViewModel.SyncElementToLegacyColumn(elem);
                                        ViewModel.SelectedElementFontSize = calcBarH;
                                    }
                                }
                            }

                            // 로컬 Shift(좌상단 이동량)를 글로벌 좌표로 변환
                            double globalShiftX = localShiftX * cos - localShiftY * sin;
                            double globalShiftY = localShiftX * sin + localShiftY * cos;

                            double newX = Math.Max(0, Math.Min(effW - 2, Math.Round((_startElemX + globalShiftX) * 2.0) / 2.0));
                            double newY = Math.Max(0, Math.Min(effH - 2, Math.Round((_startElemY + globalShiftY) * 2.0) / 2.0));

                            newW = Math.Round(newW, 1);
                            newH = Math.Round(newH, 1);

                            elem.X = newX;
                            elem.Y = newY;
                            elem.Width = newW;
                            elem.Height = newH;

                            RedrawActiveDragCanvas(canvas);
                            UpdateCustomDragInfo($"[{elem.HeaderName} 크기 조절] {newW:F1} × {newH:F1} mm" + (elem.Type == ElementType.TEXT ? $", 폰트: {elem.FontSize}pt" : ""));
                        }
                        break;

                    case TableDragMode.ResizeItemWidth:
                        {
                            int offX = ViewModel.GetOffsetXByIndex(_resizingColNum);
                            int maxAvailableWMm = Math.Max(5, effW - offX);
                            int newW = (int)Math.Max(5, Math.Min(maxAvailableWMm, Math.Round(_startItemWidthMm + diffXMm)));
                            if (ViewModel.GetWidthByIndex(_resizingColNum) != newW)
                            {
                                ViewModel.SetWidthByIndex(_resizingColNum, newW);
                                if (ViewModel.CurrentTemplate?.Elements != null)
                                {
                                    var matchingElem = ViewModel.CurrentTemplate.Elements.FirstOrDefault(el => el.IsVariable && el.InputOrder == _resizingColNum);
                                    if (matchingElem != null)
                                    {
                                        matchingElem.Width = newW;
                                    }
                                }
                                RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
                            }
                            UpdateCustomDragInfo($"[헤더 행 높이] {newH} mm");
                        }
                        break;

                    case TableDragMode.ResizeRowSeparator:
                        {
                            int targetColNum = GetTextColNumByIndex(_resizingRowIndex);
                            int newH = (int)Math.Max(3, Math.Min(maxY, Math.Round(_startSpecificRowHeightMm + diffYMm)));
                            ViewModel.SetRowHeightByIndex(targetColNum, newH);
                            RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
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
                                RedrawActiveDragCanvas(canvas);
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
                var prevDragMode = _currentDragMode;
                _isDragging = false;
                _currentDragMode = TableDragMode.None;
                _activeDragCanvas = null;
                canvas?.ReleaseMouseCapture();

                if (prevDragMode == TableDragMode.MoveTemplateElement)
                {
                    bool isMultiKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || 
                                      Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    // 다중 선택 상태에서 마우스 이동 없이(드래그 없이) 단일 클릭한 경우 해당 요소만 단일 선택으로 전환
                    if (!_hasMovedDuringDrag && !isMultiKey && _selectedTemplateElement != null && ViewModel != null && ViewModel.HasMultipleSelectedElements)
                    {
                        ViewModel.SelectedElement = _selectedTemplateElement;
                    }
                }

                if (prevDragMode == TableDragMode.RubberBandSelect)
                {
                    if (_rubberBandRect != null)
                    {
                        double rx = Canvas.GetLeft(_rubberBandRect);
                        double ry = Canvas.GetTop(_rubberBandRect);
                        double rw = _rubberBandRect.Width;
                        double rh = _rubberBandRect.Height;

                        canvas?.Children.Remove(_rubberBandRect);
                        _rubberBandRect = null;

                        bool isMultiKey = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || 
                                          Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                        // 마우스 드래그 박스가 4px 이상일 때 영역 내 요소 검색
                        if (rw > 4 && rh > 4)
                        {
                            var hitSet = new HashSet<TemplateElement>();

                            // [1] 캔버스에 실제로 배치된 UIElement들의 픽셀 Rect 교차 검사 (WYSIWYG 100% 일치)
                            var selRectPx = new Rect(rx, ry, rw, rh);
                            if (canvas != null)
                            {
                                foreach (UIElement child in canvas.Children)
                                {
                                    if (child is FrameworkElement fe && fe.Tag != null)
                                    {
                                        double cLeft = Canvas.GetLeft(fe);
                                        double cTop = Canvas.GetTop(fe);
                                        if (double.IsNaN(cLeft) || double.IsNaN(cTop)) continue;

                                        double cW = fe.Width > 0 ? fe.Width : fe.ActualWidth;
                                        double cH = fe.Height > 0 ? fe.Height : fe.ActualHeight;
                                        if (cW <= 0) cW = 10;
                                        if (cH <= 0) cH = 10;

                                        var childRect = new Rect(cLeft, cTop, cW, cH);
                                        if (selRectPx.IntersectsWith(childRect))
                                        {
                                            if (fe.Tag is TemplateElement te)
                                            {
                                                hitSet.Add(te);
                                            }
                                            else if (int.TryParse(fe.Tag.ToString(), out int rawIdx))
                                            {
                                                int targetColNum = (rawIdx == 99 || rawIdx == 101) ? rawIdx : rawIdx + 1;
                                                var matching = ViewModel?.CurrentTemplate?.Elements?.FirstOrDefault(el =>
                                                    (rawIdx == 99 && el.Id == "DATETIME_ELEMENT") ||
                                                    (rawIdx == 101 && el.Id == "SERIAL_ELEMENT") ||
                                                    (el.IsVariable && el.InputOrder == targetColNum));
                                                if (matching != null)
                                                {
                                                    hitSet.Add(matching);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // [2] 템플릿 모델(TemplateElements) mm 좌표 기반 보조 검사
                            if (ViewModel?.CurrentTemplate?.Elements != null)
                            {
                                double selLeftMm = rx / MM_TO_PX;
                                double selTopMm = ry / MM_TO_PX;
                                var selRectMm = new Rect(selLeftMm, selTopMm, rw / MM_TO_PX, rh / MM_TO_PX);

                                foreach (var elem in ViewModel.CurrentTemplate.Elements)
                                {
                                    if (elem.IsVariable && elem.InputOrder > ViewModel.ActiveColumnCount) continue;

                                    GetElementVisualDimensions(elem, out double vWPx, out double vHPx);
                                    double elemW = Math.Max(4.0, vWPx / MM_TO_PX);
                                    double elemH = Math.Max(4.0, vHPx / MM_TO_PX);
                                    var elemRect = new Rect(elem.X, elem.Y, elemW, elemH);

                                    if (selRectMm.IntersectsWith(elemRect))
                                    {
                                        hitSet.Add(elem);
                                    }
                                }
                            }

                            if (hitSet.Count > 0)
                            {
                                ViewModel?.SetSelectedElements(hitSet, addToExisting: isMultiKey);
                                _selectedItemIndices.Clear();
                                if (ViewModel?.SelectedElements != null)
                                {
                                    foreach (var h in ViewModel.SelectedElements)
                                    {
                                        if (h.IsVariable && h.InputOrder >= 1 && h.InputOrder <= 8)
                                        {
                                            _selectedItemIndices.Add(h.InputOrder - 1);
                                        }
                                    }
                                }
                                _selectedTemplateElement = ViewModel?.SelectedElement;
                                UpdateCustomDragInfo($"{ViewModel?.SelectedElementsCount ?? hitSet.Count}개 요소 선택됨");
                            }
                            else if (!isMultiKey)
                            {
                                ViewModel?.ClearSelectedElements();
                                _selectedTemplateElement = null;
                                _selectedItemIndices.Clear();
                                _selectedItemIndex = -1;
                            }
                        }
                    }
                    RedrawInteractiveCanvas();
                    return;
                }

                if (ViewModel != null)
                {
                    if (ViewModel.HasMultipleSelectedElements)
                    {
                        foreach (var elem in ViewModel.SelectedElements)
                        {
                            ViewModel.SyncElementToLegacyColumn(elem);
                        }
                        ViewModel.UpdatePreview();
                    }
                    else if (_selectedTemplateElement != null)
                    {
                        ViewModel.SyncElementToLegacyColumn(_selectedTemplateElement);
                        ViewModel.UpdatePreview();
                        // 드래그 종료 후에도 선택 상태 및 조절 핸들 유지 (빈 공간 클릭 시 해제)
                    }
                    else
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
