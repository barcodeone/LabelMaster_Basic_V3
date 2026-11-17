using System;
using System.Windows;
using System.Windows.Threading;

namespace BarcodePrint.WpfView
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponentSafely();
            try
            {
                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (ver != null)
                {
                    string verStr = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
                    this.Title = $"라벨마스터 {verStr} 시작 중...";
                    if (TxtSplashVersion != null)
                    {
                        TxtSplashVersion.Text = verStr;
                    }
                }
            }
            catch { }
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/splashscreenwindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SplashScreenWindow Load Exception: {ex.Message}");
            }
        }

        public void UpdateProgress(int percent, string message)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (LoadingProgressBar != null)
                    {
                        LoadingProgressBar.Value = percent;
                    }
                    if (TxtPercent != null)
                    {
                        TxtPercent.Text = $"{percent}%";
                    }
                    if (TxtStatus != null && !string.IsNullOrEmpty(message))
                    {
                        TxtStatus.Text = message;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateProgress Error: {ex.Message}");
                }
            }, DispatcherPriority.Render);
        }
    }
}
