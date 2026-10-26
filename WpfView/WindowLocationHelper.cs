using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace BarcodePrint.WpfView
{
    /// <summary>
    /// 창 전환 시 화면 위치(DPI 스케일링, 멀티 모니터, 작업표시줄 등)의 오차 없이
    /// 이전 창의 물리 화면 좌표 및 최대화 상태를 1픽셀도 어긋남 없이 새 창으로 동기화하는 Win32 헬퍼
    /// </summary>
    public static class WindowLocationHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMAXIMIZED = 3;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// sourceWindow의 물리 화면 좌표 및 상태(Normal / Maximized)를 targetWindow로 완벽하게 전승하여 창을 전환합니다.
        /// 멀티 모니터(보조 모니터, 고해상도 DPI) 환경에서도 다른 모니터로 튀지 않고 100% 동일한 모니터/위치에 나타납니다.
        /// </summary>
        public static void TransitionToWindow(Window sourceWindow, Window targetWindow, bool hideInsteadOfClose = false)
        {
            if (sourceWindow == null || targetWindow == null) return;

            targetWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            var srcHelper = new WindowInteropHelper(sourceWindow);
            IntPtr srcHwnd = srcHelper.Handle;

            bool isMaximized = (sourceWindow.WindowState == WindowState.Maximized);

            RECT winRect = default;
            bool hasRect = false;
            if (srcHwnd != IntPtr.Zero)
            {
                hasRect = GetWindowRect(srcHwnd, out winRect);
            }

            // 1. 현재 창의 중심점을 기준으로 위치한 실제 모니터(Screen) 판별
            Screen screen = null;
            if (hasRect && winRect.Width > 0 && winRect.Height > 0)
            {
                int centerX = winRect.Left + winRect.Width / 2;
                int centerY = winRect.Top + winRect.Height / 2;
                screen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
            }
            else if (srcHwnd != IntPtr.Zero)
            {
                screen = Screen.FromHandle(srcHwnd);
            }

            if (screen == null)
            {
                screen = Screen.PrimaryScreen;
            }

            var wa = screen.WorkingArea;

            // 2. 해당 모니터의 DPI 스케일 구하기
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var sourcePresentation = PresentationSource.FromVisual(sourceWindow);
            if (sourcePresentation?.CompositionTarget != null)
            {
                dpiScaleX = sourcePresentation.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = sourcePresentation.CompositionTarget.TransformToDevice.M22;
            }
            if (dpiScaleX <= 0) dpiScaleX = 1.0;
            if (dpiScaleY <= 0) dpiScaleY = 1.0;

            // 3. WPF DIP 수준의 초기 좌표 설정 (모니터 WorkingArea 범위 내에 확실히 안착)
            if (isMaximized)
            {
                targetWindow.Left = wa.Left / dpiScaleX;
                targetWindow.Top = wa.Top / dpiScaleY;
                targetWindow.Width = wa.Width / dpiScaleX;
                targetWindow.Height = wa.Height / dpiScaleY;
            }
            else if (hasRect && winRect.Width > 0 && winRect.Height > 0)
            {
                targetWindow.Left = winRect.Left / dpiScaleX;
                targetWindow.Top = winRect.Top / dpiScaleY;
                targetWindow.Width = winRect.Width / dpiScaleX;
                targetWindow.Height = winRect.Height / dpiScaleY;
            }
            else
            {
                double wDip = 1260;
                double hDip = 820;
                double leftPhys = wa.Left + Math.Max(0, (wa.Width - (int)(wDip * dpiScaleX)) / 2);
                double topPhys = wa.Top + Math.Max(0, (wa.Height - (int)(hDip * dpiScaleY)) / 2);
                targetWindow.Left = leftPhys / dpiScaleX;
                targetWindow.Top = topPhys / dpiScaleY;
                targetWindow.Width = wDip;
                targetWindow.Height = hDip;
            }

            // 4. HWND를 미리 생성(EnsureHandle)하여 Show() 이전에 해당 모니터에 물리적 배치
            var tgtHelper = new WindowInteropHelper(targetWindow);
            IntPtr tgtHwnd = tgtHelper.EnsureHandle();

            if (tgtHwnd != IntPtr.Zero)
            {
                if (isMaximized)
                {
                    // 최대화일 경우 해당 모니터의 작업영역 전체로 먼저 물리 좌표 이동
                    SetWindowPos(tgtHwnd, IntPtr.Zero, wa.Left, wa.Top, wa.Width, wa.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
                }
                else if (hasRect && winRect.Width > 0 && winRect.Height > 0)
                {
                    SetWindowPos(tgtHwnd, IntPtr.Zero, winRect.Left, winRect.Top, winRect.Width, winRect.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
                }
                else
                {
                    int wPhys = (int)(1260 * dpiScaleX);
                    int hPhys = (int)(820 * dpiScaleY);
                    int xPhys = wa.Left + Math.Max(0, (wa.Width - wPhys) / 2);
                    int yPhys = wa.Top + Math.Max(0, (wa.Height - hPhys) / 2);
                    SetWindowPos(tgtHwnd, IntPtr.Zero, xPhys, yPhys, wPhys, hPhys, SWP_NOZORDER | SWP_FRAMECHANGED);
                }
            }

            // 5. 창 표시 및 최대화 상태 적용
            targetWindow.Show();

            if (isMaximized)
            {
                targetWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                targetWindow.WindowState = WindowState.Normal;
            }

            targetWindow.Activate();

            if (hideInsteadOfClose)
            {
                sourceWindow.Hide();
            }
            else
            {
                sourceWindow.Close();
            }
        }

        /// <summary>
        /// 이미 생성되어 있는 대상 창의 좌표와 최대화 상태를 현재 창과 100% 동일하게 동기화
        /// </summary>
        public static void SyncWindowBounds(Window sourceWindow, Window targetWindow)
        {
            if (sourceWindow == null || targetWindow == null) return;

            var srcHelper = new WindowInteropHelper(sourceWindow);
            IntPtr srcHwnd = srcHelper.Handle;

            bool isMaximized = (sourceWindow.WindowState == WindowState.Maximized);

            RECT winRect = default;
            bool hasRect = false;
            if (srcHwnd != IntPtr.Zero)
            {
                hasRect = GetWindowRect(srcHwnd, out winRect);
            }

            Screen screen = null;
            if (hasRect && winRect.Width > 0 && winRect.Height > 0)
            {
                int centerX = winRect.Left + winRect.Width / 2;
                int centerY = winRect.Top + winRect.Height / 2;
                screen = Screen.FromPoint(new System.Drawing.Point(centerX, centerY));
            }
            else if (srcHwnd != IntPtr.Zero)
            {
                screen = Screen.FromHandle(srcHwnd);
            }

            if (screen == null)
            {
                screen = Screen.PrimaryScreen;
            }

            var wa = screen.WorkingArea;

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var sourcePresentation = PresentationSource.FromVisual(sourceWindow);
            if (sourcePresentation?.CompositionTarget != null)
            {
                dpiScaleX = sourcePresentation.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = sourcePresentation.CompositionTarget.TransformToDevice.M22;
            }
            if (dpiScaleX <= 0) dpiScaleX = 1.0;
            if (dpiScaleY <= 0) dpiScaleY = 1.0;

            targetWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            if (isMaximized)
            {
                targetWindow.Left = wa.Left / dpiScaleX;
                targetWindow.Top = wa.Top / dpiScaleY;
                targetWindow.Width = wa.Width / dpiScaleX;
                targetWindow.Height = wa.Height / dpiScaleY;
            }
            else if (hasRect && winRect.Width > 0 && winRect.Height > 0)
            {
                targetWindow.Left = winRect.Left / dpiScaleX;
                targetWindow.Top = winRect.Top / dpiScaleY;
                targetWindow.Width = winRect.Width / dpiScaleX;
                targetWindow.Height = winRect.Height / dpiScaleY;
            }

            var tgtHelper = new WindowInteropHelper(targetWindow);
            IntPtr tgtHwnd = tgtHelper.EnsureHandle();

            if (tgtHwnd != IntPtr.Zero)
            {
                if (isMaximized)
                {
                    SetWindowPos(tgtHwnd, IntPtr.Zero, wa.Left, wa.Top, wa.Width, wa.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
                    targetWindow.WindowState = WindowState.Maximized;
                }
                else if (hasRect && winRect.Width > 0 && winRect.Height > 0)
                {
                    SetWindowPos(tgtHwnd, IntPtr.Zero, winRect.Left, winRect.Top, winRect.Width, winRect.Height, SWP_NOZORDER | SWP_FRAMECHANGED);
                    targetWindow.WindowState = WindowState.Normal;
                }
            }
        }
    }
}
