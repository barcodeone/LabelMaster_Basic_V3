using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace BarcodePrint.Model
{
    public static class UsbPrinterInfoTest
    {
        public static event Action<string> DebugLogEvent;

        private const string XPRINTER_VID = "VID_2D37";

        private static void WriteLog(string message)
        {
            DebugLogEvent?.Invoke($"[UsbPrinterInfoTest] {message}");
            System.Diagnostics.Debug.WriteLine($"[UsbPrinterInfoTest] {message}");
        }

        private static readonly Regex SerialRegex =
            new Regex(@"^USB\\VID_.*\\(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public class PrinterResult
        {
            public string Name { get; set; }
            public string Serial { get; set; }
            public string Port { get; set; }
        }

        public static List<PrinterResult> GetUsbPrinterInfo()
        {
            WriteLog("=== 하드웨어 VID(2D37) 기반 정밀 스캔 시작 ===");
            var results = new List<PrinterResult>();
            try
            {
                var printers = LoadPrinters();

                using (var searcher = new ManagementObjectSearcher("SELECT Name, DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'"))
                {
                    foreach (ManagementObject dev in searcher.Get())
                    {
                        string name = dev["Name"]?.ToString() ?? "";
                        string deviceId = dev["DeviceID"]?.ToString() ?? "";

                        WriteLog($"  [USB 장치 스캔] Name: {name} | InstancePath: {deviceId}");

                        bool isXprinterHardware = deviceId.ToUpper().Contains(XPRINTER_VID);

                        if (isXprinterHardware)
                        {
                            WriteLog($"    -> 하드웨어 후보 발견: {name}");
                            string serial = ExtractSerial(deviceId);

                            if (string.IsNullOrEmpty(serial))
                            {
                                serial = FindSerialInParentChain(deviceId);
                            }

                            if (!string.IsNullOrEmpty(serial))
                            {
                                var matchedDriver = FindMatchedDriver(printers, name);

                                string finalName = matchedDriver != null ? matchedDriver.Name : name;
                                string finalPort = matchedDriver != null ? matchedDriver.PortName : "";

                                if (IsSupportedPrinterModel(finalName))
                                {
                                    if (finalName.ToUpper().Contains("DT108B") && !serial.StartsWith("040"))
                                    {
                                        WriteLog($"    -> [DT108B 검증 실패] 시리얼 '{serial}'");
                                        continue;
                                    }

                                    WriteLog($"    => [발견] Name: {finalName}, Serial: {serial}, Port: {finalPort}");
                                    results.Add(new PrinterResult
                                    {
                                        Name = finalName,
                                        Serial = serial,
                                        Port = finalPort
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"에러 발생: {ex.Message}");
            }
            return results;
        }

        public static string ExtractSerial(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return "";
            var m = SerialRegex.Match(deviceId);
            if (m.Success)
            {
                string rawSerial = m.Groups[1].Value;
                if (rawSerial.Contains("&") || rawSerial.Contains("_")) return "";
                return rawSerial;
            }
            return "";
        }

        public static string FindSerialInParentChain(string deviceId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT Parent FROM Win32_PnPEntity WHERE DeviceID = '{deviceId.Replace("\\", "\\\\")}'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string parentId = obj["Parent"]?.ToString();
                        if (string.IsNullOrEmpty(parentId)) continue;

                        string serial = ExtractSerial(parentId);
                        if (!string.IsNullOrEmpty(serial)) return serial;

                        return FindSerialInParentChain(parentId);
                    }
                }
            }
            catch { }
            return "";
        }

        private static PrinterData FindMatchedDriver(List<PrinterData> printers, string hardwareName)
        {
            string baseName = ExtractBaseName(hardwareName);
            var hit = printers.FirstOrDefault(p => p.Name.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0);
            return hit;
        }

        private static string ExtractBaseName(string name)
        {
            int idx = name.IndexOf('(');
            return (idx >= 0 ? name.Substring(0, idx) : name).Trim();
        }

        private static bool IsSupportedPrinterModel(string printerName)
        {
            if (string.IsNullOrEmpty(printerName)) return true;
            return true;
        }

        public class PrinterData { public string Name { get; set; } public string PortName { get; set; } }

        private static List<PrinterData> LoadPrinters()
        {
            var list = new List<PrinterData>();
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, PortName FROM Win32_Printer"))
                {
                    foreach (ManagementObject p in s.Get())
                    {
                        list.Add(new PrinterData { Name = p["Name"]?.ToString() ?? "", PortName = p["PortName"]?.ToString() ?? "" });
                    }
                }
            }
            catch { }
            return list;
        }
    }
}




