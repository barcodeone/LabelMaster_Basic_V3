using System;
using System.Windows;

namespace BarcodePrint.WpfView
{
    public partial class ConnectionProgressWindow : Window
    {
        public ConnectionProgressWindow(string printerName, Window owner = null)
        {
            InitializeComponent();
            TxtPrinterName.Text = string.IsNullOrEmpty(printerName) ? "프린터 연결 중..." : $"[{printerName}] 연결 중";
            
            if (owner != null)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        public void SetMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtMessage.Text = message;
            });
        }
    }
}
