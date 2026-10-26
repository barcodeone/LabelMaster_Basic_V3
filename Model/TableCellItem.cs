using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BarcodePrint.ViewModel;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 표(Table) 요소의 개별 셀 편집 및 양방향 UI 바인딩용 모델 (개별 엑셀 컬럼 매핑 지원)
    /// </summary>
    public class TableCellItem : INotifyPropertyChanged
    {
        private string _text = "";
        private string _selectedExcelColumn = "0";
        private string _effectivePreviewText = "";
        private readonly Action<int, int, string> _onChanged;
        private readonly Action<int, int, string> _onMappingChanged;
        private readonly Func<ObservableCollection<NameCodePair>> _getExcelOptions;

        public int Row { get; }
        public int Column { get; }
        public string CellLabel => $"[{Row + 1}행 {Column + 1}열]";
        public bool IsHeader => Row == 0;

        public ObservableCollection<NameCodePair> AvailableExcelOptions => _getExcelOptions?.Invoke() ?? new ObservableCollection<NameCodePair>();

        public bool IsExcelLinked => !string.IsNullOrEmpty(SelectedExcelColumn) && SelectedExcelColumn != "0";
        public bool IsExcelNotLinked => !IsExcelLinked;

        public string SelectedExcelColumn
        {
            get => _selectedExcelColumn;
            set
            {
                string norm = (value == "0" || string.IsNullOrWhiteSpace(value)) ? "0" : value;
                if (_selectedExcelColumn != norm)
                {
                    _selectedExcelColumn = norm;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsExcelLinked));
                    OnPropertyChanged(nameof(IsExcelNotLinked));
                    _onMappingChanged?.Invoke(Row, Column, _selectedExcelColumn);
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                    _onChanged?.Invoke(Row, Column, _text);
                }
            }
        }

        public string EffectivePreviewText
        {
            get => _effectivePreviewText;
            set
            {
                if (_effectivePreviewText != value)
                {
                    _effectivePreviewText = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _cellFontSize = 9.0;
        private bool _cellIsBold = false;
        private string _cellAlignment = "CENTER";
        private readonly Action<int, int, double, bool, string> _onStyleChanged;

        public double CellFontSize
        {
            get => _cellFontSize;
            set
            {
                double sz = Math.Max(5.0, Math.Min(40.0, value));
                if (Math.Abs(_cellFontSize - sz) > 0.01)
                {
                    _cellFontSize = sz;
                    OnPropertyChanged();
                    _onStyleChanged?.Invoke(Row, Column, _cellFontSize, _cellIsBold, _cellAlignment);
                }
            }
        }

        public bool CellIsBold
        {
            get => _cellIsBold;
            set
            {
                if (_cellIsBold != value)
                {
                    _cellIsBold = value;
                    OnPropertyChanged();
                    _onStyleChanged?.Invoke(Row, Column, _cellFontSize, _cellIsBold, _cellAlignment);
                }
            }
        }

        public string CellAlignment
        {
            get => _cellAlignment;
            set
            {
                if (_cellAlignment != value)
                {
                    _cellAlignment = value;
                    OnPropertyChanged();
                    _onStyleChanged?.Invoke(Row, Column, _cellFontSize, _cellIsBold, _cellAlignment);
                }
            }
        }

        public void StepFontSize(double delta)
        {
            CellFontSize += delta;
        }

        public void ToggleBold()
        {
            CellIsBold = !CellIsBold;
        }

        public System.Windows.Input.ICommand IncreaseFontSizeCommand { get; }
        public System.Windows.Input.ICommand DecreaseFontSizeCommand { get; }
        public System.Windows.Input.ICommand ToggleBoldCommand { get; }

        public TableCellItem(
            int row, 
            int col, 
            string text, 
            string selectedExcelCol,
            Action<int, int, string> onChanged,
            Action<int, int, string> onMappingChanged = null,
            Func<ObservableCollection<NameCodePair>> getExcelOptions = null,
            string effectivePreview = null,
            double fontSize = 9.0,
            bool isBold = false,
            string alignment = "CENTER",
            Action<int, int, double, bool, string> onStyleChanged = null)
        {
            Row = row;
            Column = col;
            _text = text ?? "";
            _selectedExcelColumn = string.IsNullOrEmpty(selectedExcelCol) ? "0" : selectedExcelCol;
            _onChanged = onChanged;
            _onMappingChanged = onMappingChanged;
            _getExcelOptions = getExcelOptions;
            _effectivePreviewText = effectivePreview ?? _text;
            _cellFontSize = fontSize > 0 ? fontSize : 9.0;
            _cellIsBold = isBold;
            _cellAlignment = alignment ?? "CENTER";
            _onStyleChanged = onStyleChanged;

            IncreaseFontSizeCommand = new RelayCommand(_ => StepFontSize(1.0));
            DecreaseFontSizeCommand = new RelayCommand(_ => StepFontSize(-1.0));
            ToggleBoldCommand = new RelayCommand(_ => ToggleBold());
        }

        public void UpdateStyleSilently(double fontSize, bool isBold, string alignment)
        {
            _cellFontSize = fontSize > 0 ? fontSize : 9.0;
            _cellIsBold = isBold;
            _cellAlignment = alignment ?? "CENTER";
            OnPropertyChanged(nameof(CellFontSize));
            OnPropertyChanged(nameof(CellIsBold));
            OnPropertyChanged(nameof(CellAlignment));
        }

        public void RefreshOptions(string effectivePreview = null)
        {
            if (effectivePreview != null)
            {
                EffectivePreviewText = effectivePreview;
            }
            OnPropertyChanged(nameof(AvailableExcelOptions));
            OnPropertyChanged(nameof(IsExcelLinked));
            OnPropertyChanged(nameof(IsExcelNotLinked));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
