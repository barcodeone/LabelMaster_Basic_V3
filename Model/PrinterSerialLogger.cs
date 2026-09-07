using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BarcodePrint.Model
{
    public static class PrinterSerialLogger
    {
        private static readonly object lockObject = new object();
        private static string logDirectory = "Info";
        private static string logFileName = "PrinterSerialMatch.log";

        private static string GetLogDirectory()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, logDirectory);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        private static string GetLogFilePath()
        {
            return Path.Combine(GetLogDirectory(), logFileName);
        }

        public static void WriteLog(string message)
        {
            try
            {
                lock (lockObject)
                {
                    string logPath = GetLogFilePath();
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] {message}";

                    using (StreamWriter writer = new StreamWriter(logPath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(logEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 기록 실패: {ex.Message}");
            }
        }

        public static void WriteSeparator()
        {
            WriteLog("========================================");
        }

        public static void LogProgramStart()
        {
            WriteSeparator();
            WriteLog("프로그램 시작 - 프린터 시리얼번호 매칭 시작");
            WriteSeparator();
        }

        public static void LogPrinterListQueryStart(string printerName)
        {
            WriteLog($"프린터 목록 조회 시작 - 대상 프린터: {(string.IsNullOrEmpty(printerName) ? "(없음)" : printerName)}");
        }

        public static void LogPrinterList(List<UsbPrinterInfo> printerList)
        {
            if (printerList == null)
            {
                WriteLog("조회된 프린터 목록: null");
                return;
            }

            WriteLog($"조회된 프린터 개수: {printerList.Count}");

            if (printerList.Count == 0)
            {
                WriteLog("⚠️ 연결된 프린터가 없습니다.");
                return;
            }

            for (int i = 0; i < printerList.Count; i++)
            {
                var printer = printerList[i];
                WriteLog($"  [{i + 1}] 이름: {printer.Name ?? "(없음)"}, 시리얼: {printer.Serial ?? "(없음)"}, 포트: {printer.PortName ?? "(없음)"}, FriendlyName: {printer.FriendlyName ?? "(없음)"}");
            }
        }

        public static void LogSerialValidationStart(string serial, string[] allowedSerials)
        {
            WriteLog($"시리얼번호 검증 시작 - 검증 대상: {serial ?? "(없음)"}");
            if (allowedSerials != null && allowedSerials.Length > 0)
            {
                WriteLog($"허용된 시리얼번호 개수: {allowedSerials.Length}");
                for (int i = 0; i < allowedSerials.Length; i++)
                {
                    WriteLog($"  [{i + 1}] {allowedSerials[i] ?? "(없음)"}");
                }
            }
            else
            {
                WriteLog("허용된 시리얼번호 목록이 비어있습니다.");
            }
        }

        public static void LogSerialValidationResult(string serial, bool isValid)
        {
            if (isValid)
            {
                WriteLog($"✅ 시리얼번호 검증 성공 - {serial}");
            }
            else
            {
                WriteLog($"❌ 시리얼번호 검증 실패 - {serial} (허용된 시리얼번호와 일치하지 않음)");
            }
        }

        public static void LogError(string message, Exception exception = null)
        {
            WriteLog($"❌ 오류: {message}");
            if (exception != null)
            {
                WriteLog($"   예외 타입: {exception.GetType().Name}");
                WriteLog($"   예외 메시지: {exception.Message}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    WriteLog($"   스택 트레이스: {exception.StackTrace}");
                }
            }
        }

        public static void LogProgramEnd()
        {
            WriteSeparator();
            WriteLog("프로그램 종료");
            WriteSeparator();
            WriteLog("");
        }
    }
}




