using System;
using System.Windows;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    public partial class PrinterSettingsWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public PrinterSettingsWindow()
        {
            InitializeComponentSafely();
            Loaded += PrinterSettingsWindow_Loaded;
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster_Basic_v2;component/wpfview/printersettingswindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAML Load Component Exception: {ex.Message}");
            }
        }

        private void PrinterSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 진입 시 자동으로 최신 하드웨어 스펙 조회 실행
            if (ViewModel != null && !string.IsNullOrEmpty(ViewModel.SelectedPrinterName))
            {
                ViewModel.ReadPrinterHardwareInfoCommand?.Execute(null);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
