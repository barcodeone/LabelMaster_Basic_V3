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
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/printersettingswindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAML Load Component Exception: {ex.Message}");
            }
        }

        private void PrinterSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 진입 시 현재 선택된 프린터 동기화 및 하드웨어 스펙 확인
            if (ViewModel != null && !string.IsNullOrEmpty(ViewModel.SelectedPrinterName))
            {
                ViewModel.TargetPrinterName = ViewModel.SelectedPrinterName;

                // 창 진입 시 사용자를 방해하는 강제 연결 팝업을 띄우지 않고, 기존 정보가 있을 때만 백그라운드로 가볍게 갱신
                if (ViewModel.PrinterHardwareDetailsList != null && ViewModel.PrinterHardwareDetailsList.Count > 5)
                {
                    ViewModel.ExecuteReadPrinterHardwareInfo(showLoadingPopup: false);
                }
            }
        }

        private void GoToAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterTabControl != null)
            {
                PrinterTabControl.SelectedIndex = 1;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
