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
        public static volatile bool IsAppExiting = false;
        public static readonly object WmiGlobalLock = new object();
        private static object _wmiLock => WmiGlobalLock;

        /// <summary>
        /// 프로그램 종료 시 진행 중인 백그라운드 WMI 쿼리가 안전하게 완료될 때까지 대기하여 RaceOnRCWCleanup MDA 방지
        /// </summary>
        public static void WaitForWmiCompletion(int timeoutMs = 600)
        {
            IsAppExiting = true;
            UsbPrinterHelper.IsAppExiting = true;
            try
            {
                if (System.Threading.Monitor.TryEnter(WmiGlobalLock, Math.Max(100, timeoutMs)))
                {
                    System.Threading.Monitor.Exit(WmiGlobalLock);
                }
            }
            catch { }
        }

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
            if (IsAppExiting) return results;

            try
            {
                string defaultPrinterName = "";
                try
                {
                    var printSettings = new PrinterSettings();
                    defaultPrinterName = printSettings.PrinterName ?? "";
                }
                catch { }

                lock (_wmiLock)
                {
                    if (IsAppExiting) return results;

                    using (var searcher = new ManagementObjectSearcher(
                        "SELECT Name, PortName, DriverName, WorkOffline, PrinterStatus, PrinterState FROM Win32_Printer"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            if (IsAppExiting) break;
                            using (obj)
                            {
                                string name = obj["Name"]?.ToString() ?? "";
                                string port = obj["PortName"]?.ToString() ?? "";
                                string driver = obj["DriverName"]?.ToString() ?? "";
                                bool workOffline = (obj["WorkOffline"]?.ToString() == "True");

                                if (string.IsNullOrWhiteSpace(name)) continue;

                                // 1. 가상 프린터 배제 (OneNote, PDF 등)
                                string upperName = name.ToUpperInvariant();
                                if (VirtualPrinterKeywords.Any(k => upperName.Contains(k))) continue;

                                // 2. 연결 방식 판별
                                string connType = "USB";
                                if (port.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                                {
                                    connType = "USB";
                                }
                                else if (Regex.IsMatch(port, @"(?<!\d)(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?!\d)") ||
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

                                // 2. 연결 방식별 실시간 물리 연결 상태 정밀 검증
                                if (connType == "USB")
                                {
                                    // 시스템에 물리적으로 연결/켜져 있는 USB 프린터 장치가 1대 이상 존재하는지 확인
                                    bool hasAnyUsbDevice = BarcodePrint.Services.PrinterService.HasConnectedUsbPrinters();
                                    if (!hasAnyUsbDevice)
                                    {
                                        continue; // 물리 USB 장치가 전혀 없으면 오프라인
                                    }

                                    // 특정 포트 또는 활성 USB 장치 확인
                                    bool isPortConnected = BarcodePrint.Services.PrinterService.IsUsbPortConnected(port, name);
                                    if (!isPortConnected)
                                    {
                                        continue;
                                    }

                                    // 물리적으로 활성화된 경우 스풀러 지연 WorkOffline 플래그는 무시
                                    if (workOffline && !isPortConnected)
                                    {
                                        continue;
                                    }
                                }
                                else if (connType == "LAN")
                                {
                                    if (workOffline) continue;
                                    if (!IsLanPortReachable(port))
                                    {
                                        continue;
                                    }
                                }
                                else if (connType == "Bluetooth")
                                {
                                    if (workOffline) continue;
                                    if (!UsbPrinterHelper.IsBluetoothDeviceConnected(name, port))
                                    {
                                        continue;
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
                    if (string.IsNullOrEmpty(port) || !availablePorts.Contains(port) || !UsbPrinterHelper.IsBluetoothDeviceConnected(bthName, port))
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

            // Xprinter 라벨 프린터 및 물리적 USB 연결 프린터를 최상단으로 정렬
            return results.OrderByDescending(p => p.IsLabelPrinter)
                          .ThenByDescending(p => p.ConnectionType == "USB")
                          .ThenByDescending(p => p.IsDefault)
                          .ThenBy(p => p.Name)
                          .ToList();
        }

        private static List<OnlinePrinterItem> _cachedAllOnlinePrinters;
        private static DateTime _lastOnlineCheckTime = DateTime.MinValue;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (bool IsReachable, DateTime CheckedAt)> _lanReachabilityCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, (bool, DateTime)>(StringComparer.OrdinalIgnoreCase);

        public static void InvalidateCache()
        {
            _cachedAllOnlinePrinters = null;
            _lastOnlineCheckTime = DateTime.MinValue;
            _lanReachabilityCache.Clear();
        }

        public static bool IsLanPortReachable(string port)
        {
            if (string.IsNullOrWhiteSpace(port)) return false;
            var ipMatch = Regex.Match(port, @"(?<!\d)(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?!\d)");
            if (!ipMatch.Success) return false;

            string ip = ipMatch.Value;
            if (_lanReachabilityCache.TryGetValue(ip, out var cached) && (DateTime.Now - cached.CheckedAt).TotalSeconds < 5.0)
            {
                return cached.IsReachable;
            }

            bool reachable = false;
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var ar = client.BeginConnect(ip, 9100, null, null);
                    if (ar.AsyncWaitHandle.WaitOne(220) && client.Connected)
                    {
                        client.EndConnect(ar);
                        reachable = true;
                    }
                }
            }
            catch
            {
                reachable = false;
            }

            _lanReachabilityCache[ip] = (reachable, DateTime.Now);
            return reachable;
        }

        /// <summary>
        /// 현재 가장 적합한 실제 온라인 프린터 1대를 감지합니다.
        /// </summary>
        public static (bool IsOnline, string PrinterName, string Port, string ConnType) GetPrimaryOnlinePrinter()
        {
            var allOnline = GetOnlinePrinters(false);
            _cachedAllOnlinePrinters = allOnline;
            _lastOnlineCheckTime = DateTime.Now;

            var labelPrinters = allOnline.Where(p => p.IsLabelPrinter).ToList();
            if (labelPrinters.Count > 0)
            {
                var usbLabel = labelPrinters.FirstOrDefault(p => p.ConnectionType == "USB");
                var primary = usbLabel ?? labelPrinters.FirstOrDefault(p => p.IsDefault) ?? labelPrinters.First();
                return (true, primary.Name, primary.PortName, primary.ConnectionType);
            }

            if (allOnline.Count > 0)
            {
                var primary = allOnline.FirstOrDefault(p => p.IsDefault) ?? allOnline.First();
                return (true, primary.Name, primary.PortName, primary.ConnectionType);
            }

            return (false, "연결된 프린터 없음", "", "NONE");
        }

        /// <summary>
        /// 지정된 이름의 프린터가 현재 온라인인지 확인합니다.
        /// </summary>
        public static bool CheckIsPrinterOnline(string printerName)
        {
            if (IsAppExiting || string.IsNullOrWhiteSpace(printerName)) return false;

            // 블루투스 프린터인 경우 실제 블루투스 연결 및 RFCOMM 응답 여부로 확인
            if (printerName.StartsWith("[블루투스]", StringComparison.OrdinalIgnoreCase))
            {
                string port = UsbPrinterHelper.GetBluetoothPortForDeviceName(printerName);
                if (!string.IsNullOrEmpty(port))
                {
                    return UsbPrinterHelper.IsBluetoothDeviceConnected(printerName, port);
                }
                return false;
            }

            if (_cachedAllOnlinePrinters != null && (DateTime.Now - _lastOnlineCheckTime).TotalSeconds < 1.5)
            {
                return _cachedAllOnlinePrinters.Any(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
            }

            try
            {
                var allOnline = GetOnlinePrinters(false);
                _cachedAllOnlinePrinters = allOnline;
                _lastOnlineCheckTime = DateTime.Now;
                return allOnline.Any(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
            }
            catch { }

            return false;
        }
    }
}
