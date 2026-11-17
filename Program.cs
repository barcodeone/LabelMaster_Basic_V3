using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BarcodePrint.WpfView;

namespace BarcodePrint.View
{
    internal static class Program
    {
        // DPI Awareness 설정
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Windows Vista 이상에서 DPI Awareness 활성화
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var app = new System.Windows.Application();
                app.Exit += (s, e) =>
                {
                    BarcodePrint.Model.PrinterOnlineDetector.IsAppExiting = true;
                    BarcodePrint.Model.UsbPrinterHelper.IsAppExiting = true;
                    BarcodePrint.Model.PrinterOnlineDetector.WaitForWmiCompletion(600);
                };
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    BarcodePrint.Model.PrinterOnlineDetector.IsAppExiting = true;
                    BarcodePrint.Model.UsbPrinterHelper.IsAppExiting = true;
                    BarcodePrint.Model.PrinterOnlineDetector.WaitForWmiCompletion(600);
                };
                app.DispatcherUnhandledException += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Dispatcher Unhandled Exception: {e.Exception.Message}");
                    e.Handled = true; // 비정상 종료 방지
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"AppDomain Unhandled Exception: {e.ExceptionObject}");
                };

                // 1. 스플래시 로딩 윈도우 생성 및 표시
                var splash = new SplashScreenWindow();
                splash.Show();
                splash.UpdateProgress(15, "시스템 구성 요소 및 폰트 엔진 초기화 중...");

                StartWindow startWindow = null;

                Task.Run(() =>
                {
                    try
                    {
                        // 백그라운드에서 환경설정 싱글톤 사전 로드 (UI 스레드 디스크 I/O 제로화)
                        _ = BarcodePrint.Model.AppSettings.Default;
                        splash.UpdateProgress(60, "홈 대시보드 및 시스템 컴포넌트 로드 중...");

                        // 메인 UI 스레드에서 StartWindow 생성 및 즉시 표시 (인위적 Thread.Sleep 지연 완전 제거)
                        app.Dispatcher.Invoke(() =>
                        {
                            startWindow = new StartWindow();
                            app.MainWindow = startWindow;
                            startWindow.Show();
                            splash.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Splash Loading Error: {ex.Message}");
                        app.Dispatcher.Invoke(() =>
                        {
                            if (startWindow != null)
                            {
                                startWindow.Show();
                            }
                            splash.Close();
                        });
                    }
                });

                app.Run();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App Launch Error: {ex.Message}");
            }
        }
    }
}
