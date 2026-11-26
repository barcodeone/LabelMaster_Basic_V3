using System;
using System.Windows;
using System.Windows.Input;

namespace BarcodePrint.WpfView
{
    public partial class TestPrintSizeDialog : Window
    {
        public int PaperWidthMm { get; private set; }
        public int PaperHeightMm { get; private set; }

        public TestPrintSizeDialog(int initialWidthMm, int initialHeightMm)
        {
            InitializeComponentSafely();

            PaperWidthMm = initialWidthMm > 0 ? initialWidthMm : 80;
            PaperHeightMm = initialHeightMm > 0 ? initialHeightMm : 60;

            if (TxtWidth != null) TxtWidth.Text = PaperWidthMm.ToString();
            if (TxtHeight != null) TxtHeight.Text = PaperHeightMm.ToString();

            Loaded += (s, e) =>
            {
                if (TxtWidth != null)
                {
                    TxtWidth.Focus();
                    TxtWidth.SelectAll();
                }
            };
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/testprintsizedialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TestPrintSizeDialog Load Exception: {ex.Message}");
            }
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmAndClose();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ConfirmAndClose();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmAndClose()
        {
            if (!int.TryParse(TxtWidth?.Text?.Trim(), out int w) || w <= 0 || w > 500)
            {
                MessageBox.Show(this, "가로(Width) 크기는 1 ~ 500 mm 사이의 정수로 입력해 주세요.", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtWidth?.Focus();
                TxtWidth?.SelectAll();
                return;
            }

            if (!int.TryParse(TxtHeight?.Text?.Trim(), out int h) || h <= 0 || h > 500)
            {
                MessageBox.Show(this, "세로(Height) 크기는 1 ~ 500 mm 사이의 정수로 입력해 주세요.", "입력 확인", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtHeight?.Focus();
                TxtHeight?.SelectAll();
                return;
            }

            PaperWidthMm = w;
            PaperHeightMm = h;
            DialogResult = true;
            Close();
        }
    }
}
