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
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var ver = asm.GetName().Version;
                string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.0.1";
                if (TxtVersionBadge != null)
                {
                    TxtVersionBadge.Text = verStr;
                }

                string buildDate = System.IO.File.Exists(asm.Location)
                    ? System.IO.File.GetLastWriteTime(asm.Location).ToString("yyyy.MM.dd")
                    : DateTime.Now.ToString("yyyy.MM.dd");

                if (TxtBuildVersionInfo != null)
                {
                    TxtBuildVersionInfo.Text = $"{verStr} (Build {buildDate})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadVersionInfo Error: {ex.Message}");
            }
        }

        private void InitializeComponentSafely()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AboutWindow InitializeComponent Error: {ex.Message}");
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
                MessageBox.Show($"링크를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://xpix.co.kr/labelmaster/windows/download") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"다운로드 링크를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenInfoFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string infoDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
                if (!System.IO.Directory.Exists(infoDir))
                {
                    System.IO.Directory.CreateDirectory(infoDir);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{infoDir}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Info 폴더를 여는 중 오류가 발생했습니다: {ex.Message}", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
