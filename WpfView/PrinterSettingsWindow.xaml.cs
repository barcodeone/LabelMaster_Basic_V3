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
            if (ViewModel != null)
            {
                // 진입 시 프린터 목록이 비어 있으면 비동기로 즉시 갱신 (창 오픈 지연 방지)
                if (ViewModel.AvailablePrinters.Count == 0)
                {
                    ViewModel.RefreshPrinters(runAsync: true);
                }

                // 진입 시 현재 선택된 프린터 동기화 및 하드웨어 스펙 확인
                if (!string.IsNullOrEmpty(ViewModel.SelectedPrinterName))
                {
                    ViewModel.TargetPrinterName = ViewModel.SelectedPrinterName;

                    // 창 진입 시 사용자를 방해하는 강제 연결 팝업을 띄우지 않고 백그라운드로 가볍게 상태/스펙 갱신
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

        private void SelectActivePrinter_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            string chosen = ViewModel.TargetPrinterName;
            if (string.IsNullOrWhiteSpace(chosen))
            {
                MessageBox.Show(this, "인쇄에 사용할 프린터를 먼저 선택해 주세요.", "프린터 선택", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ViewModel.SelectedPrinterName = chosen;
            BarcodePrint.Model.AppSettings.Default.LastSelectedPrinterName = chosen;
            BarcodePrint.Model.AppSettings.Default.Save();

            MessageBox.Show(
                this,
                $"'{chosen}' 프린터가 인쇄에 사용할 프린터로 선택되었습니다.",
                "프린터 선택 확인",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void TestPrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            int initW = ViewModel.PaperWidth > 0 ? ViewModel.PaperWidth : 80;
            int initH = ViewModel.PaperHeight > 0 ? ViewModel.PaperHeight : 60;

            var dlg = new TestPrintSizeDialog(initW, initH) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                ViewModel.ExecuteTestPrint(dlg.PaperWidthMm, dlg.PaperHeightMm);
            }
        }
    }
}
