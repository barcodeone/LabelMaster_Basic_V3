using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace BarcodePrint.WpfView
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponentSafely();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster_Basic_v2;component/wpfview/aboutwindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AboutWindow Load Exception: {ex.Message}");
            }
        }

        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹사이트를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenWebsiteClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://xprinter.co.kr") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹사이트를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://xpix.co.kr/labelmaster/basic/download") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다운로드 링크를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
