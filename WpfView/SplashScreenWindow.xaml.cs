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
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster_Basic_v2;component/wpfview/splashscreenwindow.xaml", UriKind.Relative);
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
