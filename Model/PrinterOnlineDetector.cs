using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace BarcodePrint.Model
{
    public class OnlinePrinterItem
    {
        public string Name { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = "USB"; // "USB", "LAN", "Bluetooth"
        public bool IsOnline { get; set; } = true;
        public bool IsLabelPrinter { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        public override string ToString() => Name;
    }

    /// <summary>
    /// 실제 PC에 물리적으로 연결되어 있고 켜져 있는(WorkOffline == false) 온라인 프린터만 정밀 감지하는 헬퍼
    /// </summary>
    public static class PrinterOnlineDetector
    {
        private static readonly string[] VirtualPrinterKeywords = new[]
        {
            "ONENOTE", "PDF", "FAX", "XPS", "DOCUMENT", "ALPDF", "ROOT PRINT", "SEND TO"
        };

        /// <summary>
        /// 현재 Windows 시스템에서 실제 연결되어 있고 온라인(Online/Ready) 상태인 프린터 목록만 반환합니다.
        /// </summary>
        /// <param name="labelPrintersOnly">라벨 프린터(Xprinter 등)만 필터링할지 여부</param>
        public static List<OnlinePrinterItem> GetOnlinePrinters(bool labelPrintersOnly = true)
        {
            var results = new List<OnlinePrinterItem>();

            try
            {
                string defaultPrinterName = "";
                try
                {
                    var printSettings = new PrinterSettings();
                    defaultPrinterName = printSettings.PrinterName ?? "";
                }
                catch { }

                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PortName, DriverName, WorkOffline, PrinterStatus, PrinterState FROM Win32_Printer"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        string port = obj["PortName"]?.ToString() ?? "";
                        string driver = obj["DriverName"]?.ToString() ?? "";
                        bool workOffline = (obj["WorkOffline"]?.ToString() == "True");

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        // 1. 오프라인(전원 꺼짐 / 케이블 뽑힘 / 작업 오프라인) 장치는 완벽 배제
                        if (workOffline) continue;

                        // 2. 가상 프린터 배제 (OneNote, PDF 등)
                        string upperName = name.ToUpperInvariant();
                        if (VirtualPrinterKeywords.Any(k => upperName.Contains(k))) continue;

                        // 3. 연결 방식 판별
                        string connType = "USB";
                        if (port.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                        {
                            connType = "USB";
                        }
                        else if (Regex.IsMatch(port, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b") ||
                                 port.StartsWith("IP_", StringComparison.OrdinalIgnoreCase) ||
                                 port.StartsWith("WSD", StringComparison.OrdinalIgnoreCase) ||
                                 port.Contains(":9100"))
                        {
                            connType = "LAN";
                        }
                        else if (port.ToUpper().Contains("COM") || port.ToUpper().Contains("BTH"))
                        {
                            connType = "Bluetooth";
                        }

                        // 🔍 물리적 연결 검증: USB 프린터인데 PC에 실제 연결된 USBPRINT 장치가 전혀 없으면 오프라인
                        if (connType == "USB" && !BarcodePrint.Services.PrinterService.HasConnectedUsbPrinters())
                        {
                            continue;
                        }

                        // 🔍 블루투스 COM 포트 프린터인데 가상 COM 포트가 시스템에 존재하지 않으면 오프라인
                        if (connType == "Bluetooth")
                        {
                            var comMatch = Regex.Match(port, @"COM\d+", RegexOptions.IgnoreCase);
                            if (comMatch.Success)
                            {
                                string portOnly = comMatch.Value.ToUpper();
                                var availablePorts = UsbPrinterHelper.GetAvailableComPorts();
                                if (!availablePorts.Any(p => p.Equals(portOnly, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }
                            }
                        }

                        // 4. 라벨 프린터 여부 판별
                        bool isLabel = UsbPrinterHelper.IsXprinterDevice(name, driver);
                        if (!isLabel)
                        {
                            // TSC, Bixolon, Zebra 등 일반 라벨 프린터 키워드 보조 검사
                            string combined = $"{name} {driver}".ToUpperInvariant();
                            isLabel = combined.Contains("LABEL") || combined.Contains("BARCODE") || combined.Contains("THERMAL") || combined.Contains("XP-");
                        }

                        if (labelPrintersOnly && !isLabel) continue;

                        results.Add(new OnlinePrinterItem
                        {
                            Name = name,
                            PortName = port,
                            DriverName = driver,
                            ConnectionType = connType,
                            IsOnline = true,
                            IsLabelPrinter = isLabel,
                            IsDefault = name.Equals(defaultPrinterName, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrinterOnlineDetector] GetOnlinePrinters Error: {ex.Message}");
            }

            try
            {
                // 💡 페어링되어 있고 실제 유효한 가상 직렬(COM) 포트가 할당된 블루투스 프린터 자동 병합
                var pairedPrinters = UsbPrinterHelper.GetPairedBluetoothPrinterNames();
                var availablePorts = new HashSet<string>(UsbPrinterHelper.GetAvailableComPorts(), StringComparer.OrdinalIgnoreCase);

                foreach (var bthName in pairedPrinters)
                {
                    string port = UsbPrinterHelper.GetBluetoothPortForDeviceName(bthName);
                    if (string.IsNullOrEmpty(port) || !availablePorts.Contains(port))
                    {
                        continue;
                    }

                    string displayName = $"[블루투스] {bthName}";
                    // 이미 Win32_Printer 결과에 같은 이름이나 같은 COM 포트로 등록되어 있다면 중복 방지
                    if (results.Any(r => r.PortName.Equals(port, StringComparison.OrdinalIgnoreCase) ||
                                         r.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase) ||
                                         r.Name.Equals(bthName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    bool isLabel = UsbPrinterHelper.IsXprinterDevice(bthName, "Bluetooth") ||
                                   bthName.ToUpper().Contains("XP-") ||
                                   bthName.ToUpper().Contains("LABEL") ||
                                   bthName.ToUpper().Contains("PRINTER") ||
                                   bthName.ToUpper().Contains("POS") ||
                                   bthName.ToUpper().Contains("BARCODE");

                    if (labelPrintersOnly && !isLabel) continue;

                    results.Add(new OnlinePrinterItem
                    {
                        Name = displayName,
                        PortName = port,
                        DriverName = "Bluetooth Serial Port",
                        ConnectionType = "Bluetooth",
                        IsOnline = true,
                        IsLabelPrinter = isLabel,
                        IsDefault = false
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PrinterOnlineDetector] Bluetooth GetOnlinePrinters Error: {ex.Message}");
            }

            // Xprinter 라벨 프린터를 최상단으로 정렬
            return results.OrderByDescending(p => p.IsLabelPrinter)
                          .ThenByDescending(p => p.IsDefault)
                          .ThenBy(p => p.Name)
                          .ToList();
        }

        /// <summary>
        /// 현재 가장 적합한 실제 온라인 프린터 1대를 감지합니다.
        /// </summary>
        public static (bool IsOnline, string PrinterName, string Port, string ConnType) GetPrimaryOnlinePrinter()
        {
            var onlinePrinters = GetOnlinePrinters(true);

            if (onlinePrinters.Count > 0)
            {
                var primary = onlinePrinters.FirstOrDefault(p => p.IsDefault) ?? onlinePrinters.First();
                return (true, primary.Name, primary.PortName, primary.ConnectionType);
            }

            // 라벨 프린터가 온라인이 아니면, 기타 물리 프린터 중 온라인 장치 확인
            var anyOnline = GetOnlinePrinters(false);
            if (anyOnline.Count > 0)
            {
                var primary = anyOnline.FirstOrDefault(p => p.IsDefault) ?? anyOnline.First();
                return (true, primary.Name, primary.PortName, primary.ConnectionType);
            }

            return (false, "연결된 프린터 없음", "", "NONE");
        }

        /// <summary>
        /// 지정된 이름의 프린터가 현재 온라인인지 확인합니다.
        /// </summary>
        public static bool CheckIsPrinterOnline(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;

            // 블루투스 프린터인 경우 할당된 가상 직렬(COM) 포트의 시스템 존재 여부로 확인
            if (printerName.StartsWith("[블루투스]", StringComparison.OrdinalIgnoreCase))
            {
                string port = UsbPrinterHelper.GetBluetoothPortForDeviceName(printerName);
                if (!string.IsNullOrEmpty(port))
                {
                    return UsbPrinterHelper.GetAvailableComPorts().Contains(port, StringComparer.OrdinalIgnoreCase);
                }
                return false;
            }

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT Name, WorkOffline FROM Win32_Printer WHERE Name = '{printerName.Replace("'", "''")}'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        bool workOffline = (obj["WorkOffline"]?.ToString() == "True");
                        return !workOffline;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
