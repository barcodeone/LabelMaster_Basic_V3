using System;
using System.Windows;
using System.Windows.Input;

namespace BarcodePrint.WpfView
{
    public partial class HeaderEditDialog : Window
    {
        public string HeaderResult { get; private set; } = "";

        public HeaderEditDialog(int colIndex, string currentHeader)
        {
            InitializeComponentSafely();
            if (TxtPrompt != null)
            {
                TxtPrompt.Text = $"[{colIndex}열]의 새 컬럼 헤더명을 입력해주세요:";
            }
            if (TxtInput != null)
            {
                TxtInput.Text = currentHeader;
                TxtInput.SelectAll();
                Loaded += (s, e) => TxtInput.Focus();
            }
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/headereditdialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeaderEditDialog Load Exception: {ex.Message}");
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Confirm();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Confirm();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Confirm()
        {
            HeaderResult = TxtInput?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(HeaderResult))
            {
                MessageBox.Show("헤더명을 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
