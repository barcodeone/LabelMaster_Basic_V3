using System;
using System.Windows;
using System.Windows.Threading;

namespace BarcodePrint.WpfView
{
    public partial class ConnectionProgressWindow : Window
    {
        private DispatcherTimer _safetyTimer;
        public event Action Cancelled;
        public bool IsCancelled { get; private set; } = false;

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

            // 최대 5초 안전 자동 닫기 타이머 (UI 프리징/갇힘 원천 방지)
            _safetyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _safetyTimer.Tick += (s, e) =>
            {
                _safetyTimer.Stop();
                try { Close(); } catch { }
            };
            _safetyTimer.Start();
        }

        public void SetMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtMessage.Text = message;
            });
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            _safetyTimer?.Stop();
            try
            {
                Cancelled?.Invoke();
            }
            catch { }
            try
            {
                Close();
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            _safetyTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
