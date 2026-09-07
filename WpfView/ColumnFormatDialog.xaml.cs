using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    /// <summary>
    /// 단일 컬럼의 글꼴, 크기, 바코드, 정렬, 서식을 집중 설정하는 모달 다이얼로그
    /// </summary>
    public partial class ColumnFormatDialog : Window
    {
        private readonly MainViewModel _vm;
        private readonly int _colIndex; // 1-based (1 ~ 8)
        private bool _isInitializing = true;

        public ColumnFormatDialog(MainViewModel vm, int colIndex)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _colIndex = Math.Max(1, Math.Min(8, colIndex));

            InitControls();
        }

        private void InitControls()
        {
            _isInitializing = true;

            // 1. 헤더 설정
            TxtColBadge.Text = $"{_colIndex}열";
            string headerName = _vm.GetHeaderByIndex(_colIndex);
            TxtTitle.Text = $"[{_colIndex}열: {headerName}] 서식 및 바코드 설정";
            TxtHeaderName.Text = headerName;

            // 2. 글꼴 콤보박스
            CmbFont.ItemsSource = _vm.AvailableFontFamilies;
            string curFont = _vm.GetFontByIndex(_colIndex);
            if (_vm.AvailableFontFamilies.Contains(curFont))
            {
                CmbFont.SelectedItem = curFont;
            }
            else
            {
                CmbFont.SelectedIndex = 0;
            }

            // 3. 글자 크기, 굵게, 기울임, 정렬
            TxtSize.Text = _vm.GetSizeByIndex(_colIndex).ToString();
            TglBold.IsChecked = _vm.GetBoldByIndex(_colIndex);
            TglItalic.IsChecked = _vm.GetItalicByIndex(_colIndex);

            string align = _vm.GetAlignByIndex(_colIndex)?.ToUpper() ?? "LEFT";
            if (align == "CENTER") CmbAlign.SelectedIndex = 1;
            else if (align == "RIGHT") CmbAlign.SelectedIndex = 2;
            else CmbAlign.SelectedIndex = 0;

            // 4. 특수 서식 & 바코드
            ChkIncludeHeader.IsChecked = _vm.GetIncludeHeaderByIndex(_colIndex);
            ChkPriceFormat.IsChecked = _vm.GetPriceByIndex(_colIndex);
            ChkBarcode.IsChecked = _vm.GetBarcodeByIndex(_colIndex);

            CmbBarcodeType.ItemsSource = _vm.AvailableBarcodeTypes;
            string curBType = _vm.GetBarcodeTypeByIndex(_colIndex) ?? "CODE128";
            if (_vm.AvailableBarcodeTypes.Contains(curBType))
            {
                CmbBarcodeType.SelectedItem = curBType;
            }
            else
            {
                CmbBarcodeType.SelectedIndex = 0;
            }
            ChkShowBarcodeText.IsChecked = _vm.GetBarcodeTextByIndex(_colIndex);

            // 5. 위치 및 크기
            TxtOffsetX.Text = _vm.GetOffsetXByIndex(_colIndex).ToString();
            TxtOffsetY.Text = _vm.GetOffsetYByIndex(_colIndex).ToString();
            TxtWidth.Text = _vm.GetWidthByIndex(_colIndex).ToString();
            TxtRowHeight.Text = _vm.GetRowHeightByIndex(_colIndex).ToString();
            PnlRowHeight.Visibility = _vm.IsTableBorderEnabled ? Visibility.Visible : Visibility.Collapsed;

            // 이벤트 바인딩
            TxtHeaderName.TextChanged += (s, e) => SaveChanges();
            CmbFont.SelectionChanged += (s, e) => SaveChanges();
            TxtSize.TextChanged += (s, e) => SaveChanges();
            TglBold.Checked += (s, e) => SaveChanges();
            TglBold.Unchecked += (s, e) => SaveChanges();
            TglItalic.Checked += (s, e) => SaveChanges();
            TglItalic.Unchecked += (s, e) => SaveChanges();
            CmbAlign.SelectionChanged += (s, e) => SaveChanges();

            ChkIncludeHeader.Checked += (s, e) => SaveChanges();
            ChkIncludeHeader.Unchecked += (s, e) => SaveChanges();
            ChkPriceFormat.Checked += (s, e) => SaveChanges();
            ChkPriceFormat.Unchecked += (s, e) => SaveChanges();
            ChkBarcode.Checked += (s, e) => SaveChanges();
            ChkBarcode.Unchecked += (s, e) => SaveChanges();
            CmbBarcodeType.SelectionChanged += (s, e) => SaveChanges();
            ChkShowBarcodeText.Checked += (s, e) => SaveChanges();
            ChkShowBarcodeText.Unchecked += (s, e) => SaveChanges();

            TxtOffsetX.TextChanged += (s, e) => SaveChanges();
            TxtOffsetY.TextChanged += (s, e) => SaveChanges();
            TxtWidth.TextChanged += (s, e) => SaveChanges();
            TxtRowHeight.TextChanged += (s, e) => SaveChanges();

            _isInitializing = false;
        }

        private void SaveChanges()
        {
            if (_isInitializing || _vm == null) return;

            // 1. 항목명
            string header = TxtHeaderName.Text.Trim();
            if (!string.IsNullOrEmpty(header))
            {
                _vm.SetHeaderByIndex(_colIndex, header);
                TxtTitle.Text = $"[{_colIndex}열: {header}] 서식 및 바코드 설정";
            }

            // 2. 글꼴
            if (CmbFont.SelectedItem is string font)
            {
                _vm.SetFontByIndex(_colIndex, font);
            }

            // 3. 크기
            if (int.TryParse(TxtSize.Text, out int size))
            {
                _vm.SetSizeByIndex(_colIndex, Math.Max(4, Math.Min(72, size)));
            }

            // 4. 스타일
            _vm.SetBoldByIndex(_colIndex, TglBold.IsChecked == true);
            _vm.SetItalicByIndex(_colIndex, TglItalic.IsChecked == true);

            if (CmbAlign.SelectedItem is ComboBoxItem item && item.Tag is string alignTag)
            {
                _vm.SetAlignByIndex(_colIndex, alignTag);
            }

            // 5. 옵션
            _vm.SetIncludeHeaderByIndex(_colIndex, ChkIncludeHeader.IsChecked == true);
            _vm.SetPriceByIndex(_colIndex, ChkPriceFormat.IsChecked == true);
            _vm.SetBarcodeByIndex(_colIndex, ChkBarcode.IsChecked == true);

            if (CmbBarcodeType.SelectedItem is string bType)
            {
                _vm.SetBarcodeTypeByIndex(_colIndex, bType);
            }
            _vm.SetBarcodeTextByIndex(_colIndex, ChkShowBarcodeText.IsChecked == true);

            // 6. 좌표 및 크기
            if (int.TryParse(TxtOffsetX.Text, out int offX)) _vm.SetOffsetXByIndex(_colIndex, offX);
            if (int.TryParse(TxtOffsetY.Text, out int offY)) _vm.SetOffsetYByIndex(_colIndex, offY);
            if (int.TryParse(TxtWidth.Text, out int w)) _vm.SetWidthByIndex(_colIndex, Math.Max(0, w));
            if (int.TryParse(TxtRowHeight.Text, out int rowH)) _vm.SetRowHeightByIndex(_colIndex, Math.Max(0, rowH));

            _vm.UpdatePreview();
        }

        private void OnSizeMinusClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtSize.Text, out int size))
            {
                TxtSize.Text = Math.Max(6, size - 1).ToString();
            }
        }

        private void OnSizePlusClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtSize.Text, out int size))
            {
                TxtSize.Text = Math.Min(72, size + 1).ToString();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
