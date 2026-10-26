using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    public class PresetDisplayModel
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string DisplayDim => $"{Width} x {Height} mm";

        public PresetDisplayModel(PdfPaperPresetItem item)
        {
            Name = item.Name;
            Width = item.Width;
            Height = item.Height;
        }

        public PdfPaperPresetItem ToPresetItem()
        {
            return new PdfPaperPresetItem(Name, Width, Height);
        }
    }

    public partial class PdfPaperPresetManagerDialog : Window
    {
        private ObservableCollection<PresetDisplayModel> _presets = new ObservableCollection<PresetDisplayModel>();
        public List<PdfPaperPresetItem> ResultPresets { get; private set; }

        public PdfPaperPresetManagerDialog(List<PdfPaperPresetItem> currentPresets, double initialWidth = 100, double initialHeight = 100)
        {
            InitializeComponentSafely();

            var list = currentPresets ?? AppSettings.GetDefaultPdfPaperPresets();
            foreach (var item in list)
            {
                _presets.Add(new PresetDisplayModel(item));
            }

            LstPresets.ItemsSource = _presets;

            TxtInputName.Text = $"{initialWidth}x{initialHeight}";
            TxtInputWidth.Text = initialWidth.ToString("0.#");
            TxtInputHeight.Text = initialHeight.ToString("0.#");

            if (_presets.Count > 0)
            {
                LstPresets.SelectedIndex = 0;
            }
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/pdfpaperpresetmanagerdialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PdfPaperPresetManagerDialog InitializeComponent Error: {ex.Message}");
            }
        }

        private void OnPresetSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetDisplayModel item)
            {
                TxtInputName.Text = item.Name;
                TxtInputWidth.Text = item.Width.ToString("0.#");
                TxtInputHeight.Text = item.Height.ToString("0.#");
            }
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out string name, out double w, out double h)) return;

            var newItem = new PresetDisplayModel(new PdfPaperPresetItem(name, w, h));
            _presets.Add(newItem);
            LstPresets.SelectedItem = newItem;
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetDisplayModel item)
            {
                if (!ValidateInputs(out string name, out double w, out double h)) return;

                int idx = _presets.IndexOf(item);
                if (idx >= 0)
                {
                    _presets[idx] = new PresetDisplayModel(new PdfPaperPresetItem(name, w, h));
                    LstPresets.SelectedIndex = idx;
                }
            }
            else
            {
                MessageBox.Show("수정할 규격 항목을 먼저 목록에서 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetDisplayModel item)
            {
                if (_presets.Count <= 1)
                {
                    MessageBox.Show("최소 1개 이상의 용지 규격이 유지되어야 합니다.", "삭제 불가", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"'{item.Name}' 규격을 정말 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    int idx = _presets.IndexOf(item);
                    _presets.Remove(item);
                    if (_presets.Count > 0)
                    {
                        LstPresets.SelectedIndex = Math.Min(idx, _presets.Count - 1);
                    }
                }
            }
            else
            {
                MessageBox.Show("삭제할 규격 항목을 목록에서 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnResetDefaultsClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("자주 쓰는 규격 목록을 시스템 기본값으로 초기화하시겠습니까?", "기본값 복원", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _presets.Clear();
                foreach (var item in AppSettings.GetDefaultPdfPaperPresets())
                {
                    _presets.Add(new PresetDisplayModel(item));
                }
                if (_presets.Count > 0) LstPresets.SelectedIndex = 0;
            }
        }

        private bool ValidateInputs(out string name, out double width, out double height)
        {
            name = TxtInputName.Text.Trim();
            width = 0;
            height = 0;

            if (!double.TryParse(TxtInputWidth.Text.Trim(), out width) || width <= 5 || width > 500)
            {
                MessageBox.Show("너비는 5mm ~ 500mm 사이의 유효한 숫자를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtInputWidth.Focus();
                return false;
            }

            if (!double.TryParse(TxtInputHeight.Text.Trim(), out height) || height <= 5 || height > 500)
            {
                MessageBox.Show("높이는 5mm ~ 500mm 사이의 유효한 숫자를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtInputHeight.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = $"{width}x{height}";
            }

            return true;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            ResultPresets = _presets.Select(p => p.ToPresetItem()).ToList();
            this.DialogResult = true;
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
