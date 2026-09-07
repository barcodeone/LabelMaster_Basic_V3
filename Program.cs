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

                WpfMainWindow mainWindow = null;

                Task.Run(() =>
                {
                    try
                    {
                        Thread.Sleep(200);
                        splash.UpdateProgress(35, "USB 프린터 통신 및 드라이버 검색 중...");

                        Thread.Sleep(250);
                        splash.UpdateProgress(60, "메인 작업 공간 및 미리보기 캔버스 로드 중...");

                        // 메인 UI 스레드에서 WpfMainWindow 생성
                        app.Dispatcher.Invoke(() =>
                        {
                            mainWindow = new WpfMainWindow();
                        });

                        Thread.Sleep(200);
                        splash.UpdateProgress(85, "라벨 서식 템플릿 및 사용자 환경설정 복원 중...");

                        Thread.Sleep(200);
                        splash.UpdateProgress(100, "준비 완료! 프로그램을 시작합니다.");
                        Thread.Sleep(250);

                        // 메인 창 표시 및 스플래시 종료
                        app.Dispatcher.Invoke(() =>
                        {
                            if (mainWindow != null)
                            {
                                app.MainWindow = mainWindow;
                                mainWindow.Show();
                            }
                            splash.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Splash Loading Error: {ex.Message}");
                        app.Dispatcher.Invoke(() =>
                        {
                            if (mainWindow != null)
                            {
                                mainWindow.Show();
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
