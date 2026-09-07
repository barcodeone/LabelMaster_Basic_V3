using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BarcodePrint.Model
{
    public static class PrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDatatype;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern int StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string szPrinterName, byte[] bytes)
        {
            if (string.IsNullOrEmpty(szPrinterName) || bytes == null || bytes.Length == 0)
            {
                return false;
            }

            IntPtr pBytes = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, pBytes, bytes.Length);

            bool success = false;
            IntPtr hPrinter = IntPtr.Zero;
            var di = new DOCINFOA
            {
                pDocName = "LabelMaster TSPL Job",
                pDatatype = "RAW"
            };

            int dwWritten = 0;

            try
            {
                if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
                {
                    if (StartDocPrinter(hPrinter, 1, di) != 0)
                    {
                        if (StartPagePrinter(hPrinter))
                        {
                            success = WritePrinter(hPrinter, pBytes, bytes.Length, out dwWritten);
                            EndPagePrinter(hPrinter);
                        }
                        EndDocPrinter(hPrinter);
                    }
                    ClosePrinter(hPrinter);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendBytesToPrinter 오류: {ex.Message}");
                success = false;
            }
            finally
            {
                Marshal.FreeCoTaskMem(pBytes);
            }

            return success && (dwWritten > 0);
        }

        public static bool SendStringToPrinter(string szPrinterName, string szString)
        {
            if (string.IsNullOrEmpty(szString)) return false;
            byte[] bytes = Encoding.GetEncoding("ks_c_5601-1987").GetBytes(szString);
            return SendBytesToPrinter(szPrinterName, bytes);
        }
    }
}
