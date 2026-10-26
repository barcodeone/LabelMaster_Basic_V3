using System;
using System.Globalization;
using System.Windows;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    public partial class PresetEditDialog : Window
    {
        public PresetLabelItem PresetItem { get; private set; }

        public PresetEditDialog(PresetLabelItem item)
        {
            InitializeComponent();

            PresetItem = item?.Clone() ?? new PresetLabelItem();

            TxtTitle.Text = PresetItem.Title;
            TxtWidth.Text = PresetItem.WidthMm.ToString("0.##", CultureInfo.InvariantCulture);
            TxtHeight.Text = PresetItem.HeightMm.ToString("0.##", CultureInfo.InvariantCulture);
            TxtDescription.Text = PresetItem.Description;

            Loaded += (s, e) =>
            {
                TxtTitle.Focus();
                TxtTitle.SelectAll();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = TxtTitle.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("규격 명칭을 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTitle.Focus();
                return;
            }

            if (!double.TryParse(TxtWidth.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double w) &&
                !double.TryParse(TxtWidth.Text.Trim(), out w))
            {
                MessageBox.Show("올바른 가로 너비(mm)를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtWidth.Focus();
                return;
            }

            if (!double.TryParse(TxtHeight.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double h) &&
                !double.TryParse(TxtHeight.Text.Trim(), out h))
            {
                MessageBox.Show("올바른 세로 높이(mm)를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtHeight.Focus();
                return;
            }

            if (w < 10 || w > 300)
            {
                MessageBox.Show("가로 너비는 10mm ~ 300mm 사이로 설정해야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtWidth.Focus();
                return;
            }

            if (h < 10 || h > 300)
            {
                MessageBox.Show("세로 높이는 10mm ~ 300mm 사이로 설정해야 합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtHeight.Focus();
                return;
            }

            PresetItem.Title = title;
            PresetItem.WidthMm = Math.Round(w, 1);
            PresetItem.HeightMm = Math.Round(h, 1);
            PresetItem.Description = TxtDescription.Text?.Trim() ?? "";

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
