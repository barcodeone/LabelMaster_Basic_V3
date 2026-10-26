using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BarcodePrint.Model;
using BarcodePrint.Services;

namespace BarcodePrint.WpfView
{
    public class ExcelSourceColumnOption
    {
        public int ExcelColumnIndex { get; set; } // 0: 미사용, 1-based index: 1, 2, 3...
        public string DisplayText { get; set; } = "";
        public string HeaderName { get; set; } = "";

        public override string ToString() => DisplayText;
    }

    public class ExcelColumnMappingItem : INotifyPropertyChanged
    {
        private ExcelSourceColumnOption _selectedOption;

        public int TargetItemNumber { get; set; } // 1 ~ 8
        private string _customLabel;
        public string TargetItemLabel
        {
            get => !string.IsNullOrEmpty(_customLabel) ? _customLabel : $"[항목 {TargetItemNumber}]";
            set => _customLabel = value;
        }

        public ObservableCollection<ExcelSourceColumnOption> AvailableOptions { get; set; }

        public ExcelSourceColumnOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (_selectedOption != value)
                {
                    _selectedOption = value;
                    OnPropertyChanged(nameof(SelectedOption));
                    OnPropertyChanged(nameof(IsMapped));
                }
            }
        }

        public bool IsMapped => SelectedOption != null && SelectedOption.ExcelColumnIndex > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public partial class ExcelImportDialog : Window
    {
        private readonly string _filePath;
        private readonly IExcelService _excelService;
        private readonly BarcodePrint.Model.AppSettings _settings;
        private readonly TemplateElement _targetElement;

        public bool IsSingleElementMode => _targetElement != null;
        public int SelectedSingleColumnIndex { get; private set; } = 0;
        public string SelectedSingleColumnHeader { get; private set; } = "";
        public List<string> AllSheetHeaders { get; private set; } = new List<string>();

        public ObservableCollection<ExcelSourceColumnOption> AvailableOptions { get; } = new ObservableCollection<ExcelSourceColumnOption>();
        public ObservableCollection<ExcelColumnMappingItem> MappingSlots { get; } = new ObservableCollection<ExcelColumnMappingItem>();

        public string SelectedSheetName { get; private set; } = "";
        public List<int> SelectedColumnIndices { get; private set; } = new List<int>();
        public List<string> SelectedCustomHeaders { get; private set; } = new List<string>();

        private ComboBox SheetComboBox => FindName("CboSheets") as ComboBox;
        private ItemsControl SlotsList => FindName("LstMappingSlots") as ItemsControl;
        private TextBlock SelectedCountText => FindName("TxtSelectedCount") as TextBlock;
        private CheckBox RememberMappingCheckBox => FindName("ChkRememberMapping") as CheckBox;

        public ExcelImportDialog(string filePath, IExcelService excelService, BarcodePrint.Model.AppSettings settings = null, TemplateElement targetElement = null)
        {
            InitializeComponentSafely();
            _filePath = filePath;
            _excelService = excelService;
            _settings = settings;
            _targetElement = targetElement;

            if (IsSingleElementMode)
            {
                Title = "📊 엑셀 컬럼 매핑 (선택된 요소)";
                Height = 440;
                Width = 640;
                MinHeight = 400;

                var headerTitle = FindName("TxtHeaderTitle") as TextBlock;
                if (headerTitle != null) headerTitle.Text = "📑 엑셀 데이터 가져오기 & 컬럼 매핑";

                var headerSubtitle = FindName("TxtHeaderSubtitle") as TextBlock;
                if (headerSubtitle != null) headerSubtitle.Text = "선택된 라벨 요소에 연동할 엑셀 컬럼을 콤보박스에서 선택해주세요.";

                var banner = FindName("PnlTargetElementBanner") as Border;
                if (banner != null) banner.Visibility = Visibility.Visible;

                var targetInfo = FindName("TxtTargetElementInfo") as TextBlock;
                if (targetInfo != null)
                {
                    string previewText = string.IsNullOrWhiteSpace(_targetElement.Content) ? "(내용 없음)" : _targetElement.Content.Replace("\r", " ").Replace("\n", " ");
                    targetInfo.Text = $"[{_targetElement.Type}] \"{previewText}\"";
                }

                var slotsTitle = FindName("TxtSlotsTitle") as TextBlock;
                if (slotsTitle != null) slotsTitle.Text = "📋 선택된 요소 컬럼 매핑";

                var batchBtns = FindName("PnlBatchButtons") as StackPanel;
                if (batchBtns != null) batchBtns.Visibility = Visibility.Collapsed;

                var chk = FindName("ChkRememberMapping") as CheckBox;
                if (chk != null) chk.Visibility = Visibility.Collapsed;

                var btnConfirm = FindName("BtnConfirm") as Button;
                if (btnConfirm != null)
                {
                    btnConfirm.Content = "✔️ 선택된 요소에 매핑 적용";
                    btnConfirm.Width = 190;
                }

                // 선택된 요소 전용 1개 슬롯만 추가
                MappingSlots.Add(new ExcelColumnMappingItem
                {
                    TargetItemNumber = 1,
                    TargetItemLabel = "선택 요소",
                    AvailableOptions = this.AvailableOptions
                });
            }
            else
            {
                // 1~8번 고정 슬롯 초기화
                for (int i = 1; i <= 8; i++)
                {
                    MappingSlots.Add(new ExcelColumnMappingItem
                    {
                        TargetItemNumber = i,
                        AvailableOptions = this.AvailableOptions
                    });
                }
            }

            if (SlotsList != null)
            {
                SlotsList.ItemsSource = MappingSlots;
            }

            if (RememberMappingCheckBox != null && _settings != null)
            {
                RememberMappingCheckBox.IsChecked = _settings.RememberLastExcelMapping;
            }

            LoadSheets();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/excelimportdialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExcelImportDialog Load Exception: {ex.Message}");
            }
        }

        private void LoadSheets()
        {
            var sheets = _excelService.GetSheetNames(_filePath);
            if (sheets == null || sheets.Count == 0)
            {
                sheets = new List<string> { "Sheet1" };
            }

            if (SheetComboBox != null)
            {
                SheetComboBox.ItemsSource = sheets;
                if (sheets.Count > 0)
                {
                    SheetComboBox.SelectedIndex = 0;
                }
            }
        }

        private void OnSheetSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetComboBox?.SelectedItem is string sheetName)
            {
                SelectedSheetName = sheetName;
                LoadColumnsForSheet(sheetName);
            }
        }

        private void LoadColumnsForSheet(string sheetName)
        {
            AvailableOptions.Clear();

            // 1. 첫 번째 항목: (미사용)
            AvailableOptions.Add(new ExcelSourceColumnOption
            {
                ExcelColumnIndex = 0,
                DisplayText = "(미사용 / 빈 항목)",
                HeaderName = ""
            });

            // 2. 엑셀 헤더 목록 추가
            var headers = _excelService.GetSheetHeaders(_filePath, sheetName);
            AllSheetHeaders = new List<string>(headers);
            for (int i = 0; i < headers.Count; i++)
            {
                string header = string.IsNullOrWhiteSpace(headers[i]) ? $"열 {i + 1}" : headers[i];
                string colLetter = GetColumnLetter(i + 1);
                AvailableOptions.Add(new ExcelSourceColumnOption
                {
                    ExcelColumnIndex = i + 1,
                    DisplayText = $"[{colLetter}열] {header}",
                    HeaderName = header
                });
            }

            if (IsSingleElementMode)
            {
                // 선택된 요소의 기존 매핑 확인 및 자동 선택
                ExcelSourceColumnOption matchedOpt = null;
                string curCol = _targetElement?.DataSourceColumn;
                if (!string.IsNullOrWhiteSpace(curCol))
                {
                    if (int.TryParse(curCol, out int numCol) && numCol > 0)
                    {
                        matchedOpt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == numCol);
                    }
                    if (matchedOpt == null)
                    {
                        matchedOpt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex > 0 &&
                            string.Equals(o.HeaderName?.Trim(), curCol.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (matchedOpt == null && AvailableOptions.Count > 1)
                {
                    // 기본값: 첫 번째 실제 컬럼 선택
                    matchedOpt = AvailableOptions[1];
                }

                if (MappingSlots.Count > 0)
                {
                    MappingSlots[0].SelectedOption = matchedOpt ?? AvailableOptions[0];
                }
                UpdateMappedCount();
                return;
            }

            // 3. 마지막 매핑 옵션이 활성화되어 있고 저장된 매핑이 있으면 마지막 매핑 적용, 없으면 순차 매핑 적용
            bool hasLastMapping = _settings != null && _settings.RememberLastExcelMapping &&
                ((_settings.LastExcelColumnMappingNames != null && _settings.LastExcelColumnMappingNames.Count > 0) ||
                 (_settings.LastExcelColumnIndices != null && _settings.LastExcelColumnIndices.Count > 0));

            if (hasLastMapping)
            {
                ApplyLastMapping();
            }
            else
            {
                ApplySequentialMapping();
            }
        }

        private string GetColumnLetter(int colIndex)
        {
            string colLetter = "";
            while (colIndex > 0)
            {
                int rem = (colIndex - 1) % 26;
                colLetter = (char)('A' + rem) + colLetter;
                colIndex = (colIndex - 1) / 26;
            }
            return string.IsNullOrEmpty(colLetter) ? "A" : colLetter;
        }

        public void ApplyLastMapping()
        {
            if (_settings == null)
            {
                ApplySequentialMapping();
                return;
            }

            var lastNames = _settings.LastExcelColumnMappingNames;
            var lastIndices = _settings.LastExcelColumnIndices;

            if ((lastNames == null || lastNames.Count == 0) && (lastIndices == null || lastIndices.Count == 0))
            {
                ApplySequentialMapping();
                return;
            }

            bool anyMatched = false;
            for (int i = 0; i < MappingSlots.Count; i++)
            {
                string targetHeader = (lastNames != null && i < lastNames.Count) ? lastNames[i] : null;
                int targetIndex = (lastIndices != null && i < lastIndices.Count) ? lastIndices[i] : 0;

                ExcelSourceColumnOption matchedOpt = null;

                // 1. 헤더명(컬럼명) 기준 우선 매칭 (공백 무시 및 대소문자 무시)
                if (!string.IsNullOrWhiteSpace(targetHeader))
                {
                    matchedOpt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex > 0 &&
                        string.Equals(o.HeaderName?.Trim(), targetHeader.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                // 2. 헤더명으로 못 찾았을 경우 인덱스 기준 매칭 시도
                if (matchedOpt == null && targetIndex > 0)
                {
                    matchedOpt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == targetIndex);
                }

                if (matchedOpt != null)
                {
                    MappingSlots[i].SelectedOption = matchedOpt;
                    anyMatched = true;
                }
                else
                {
                    MappingSlots[i].SelectedOption = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == 0) ?? AvailableOptions[0];
                }
            }

            // 만약 아무것도 매칭되지 않았다면 기본 순차 매핑
            if (!anyMatched)
            {
                ApplySequentialMapping();
            }

            UpdateMappedCount();
        }

        private void ApplySequentialMapping()
        {
            for (int i = 0; i < MappingSlots.Count; i++)
            {
                int targetExcelIndex = i + 1; // 1-based
                var opt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == targetExcelIndex);
                MappingSlots[i].SelectedOption = opt ?? AvailableOptions[0];
            }
            UpdateMappedCount();
        }

        private void OnApplyLastMappingClick(object sender, RoutedEventArgs e)
        {
            ApplyLastMapping();
        }

        private void OnAutoSequentialMapping(object sender, RoutedEventArgs e)
        {
            ApplySequentialMapping();
        }

        private void OnClearAllMappings(object sender, RoutedEventArgs e)
        {
            var noneOpt = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == 0);
            foreach (var slot in MappingSlots)
            {
                slot.SelectedOption = noneOpt;
            }
            UpdateMappedCount();
        }

        private void OnClearSingleSlot(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ExcelColumnMappingItem slot)
            {
                slot.SelectedOption = AvailableOptions.FirstOrDefault(o => o.ExcelColumnIndex == 0);
                UpdateMappedCount();
            }
        }

        private void OnSlotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMappedCount();
        }

        private void UpdateMappedCount()
        {
            if (IsSingleElementMode)
            {
                if (SelectedCountText != null)
                {
                    var opt = MappingSlots.FirstOrDefault()?.SelectedOption;
                    if (opt != null && opt.ExcelColumnIndex > 0)
                    {
                        SelectedCountText.Text = $"연동 컬럼: {opt.DisplayText}";
                        SelectedCountText.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#059669");
                    }
                    else
                    {
                        SelectedCountText.Text = "연동 해제 (직접 입력값 유지)";
                        SelectedCountText.Foreground = System.Windows.Media.Brushes.Gray;
                    }
                }
                return;
            }

            int mappedCount = MappingSlots.Count(s => s.IsMapped);
            if (SelectedCountText != null)
            {
                SelectedCountText.Text = $"매핑된 컬럼: {mappedCount}개 / 최대 8개";
                SelectedCountText.Foreground = mappedCount == 0
                    ? System.Windows.Media.Brushes.OrangeRed
                    : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#059669");
            }
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (IsSingleElementMode)
            {
                var opt = MappingSlots.FirstOrDefault()?.SelectedOption;
                SelectedSingleColumnIndex = opt?.ExcelColumnIndex ?? 0;
                SelectedSingleColumnHeader = opt != null && opt.ExcelColumnIndex > 0 ? opt.HeaderName : "";

                SelectedColumnIndices = new List<int> { SelectedSingleColumnIndex };
                SelectedCustomHeaders = new List<string> { SelectedSingleColumnHeader };

                DialogResult = true;
                Close();
                return;
            }

            int mappedCount = MappingSlots.Count(s => s.IsMapped);
            if (mappedCount == 0)
            {
                MessageBox.Show("최소 1개 이상의 항목에 엑셀 컬럼을 매핑해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1~8번 슬롯의 매핑 정보 구성
            SelectedColumnIndices = MappingSlots.Select(s => s.SelectedOption?.ExcelColumnIndex ?? 0).ToList();
            SelectedCustomHeaders = MappingSlots.Select(s => s.SelectedOption != null && s.SelectedOption.ExcelColumnIndex > 0 ? s.SelectedOption.HeaderName : "").ToList();

            // 마지막 매핑 정보 및 옵션 저장
            if (_settings != null)
            {
                _settings.RememberLastExcelMapping = (RememberMappingCheckBox?.IsChecked == true);
                _settings.LastExcelColumnMappingNames = SelectedCustomHeaders;
                _settings.LastExcelColumnIndices = SelectedColumnIndices;
                _settings.Save();
            }

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
