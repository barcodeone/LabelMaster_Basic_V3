using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BarcodePrint.Model
{
    public class UsbPrinterInfo
    {
        public string Serial { get; set; } = "-";
        public string FriendlyName { get; set; } = "";
        public string Name { get; set; } = "";
        public string PortName { get; set; } = "";
        public string Type { get; set; } = "USB"; // "USB", "LAN", "Bluetooth"
        public string Address { get; set; } = "";
        public bool IsPaired { get; set; } = true;
    }

    public static class UsbPrinterHelper
    {
        public static volatile bool IsAppExiting = false;
        private static object _wmiLock => PrinterOnlineDetector.WmiGlobalLock;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_INFO
        {
            public int dwSize;
            public ulong Address;
            public uint ulClassofDevice;
            [MarshalAs(UnmanagedType.Bool)] public bool fConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fAuthenticated;
            public ushort wYear, wMonth, wDayOfWeek, wDay, wHour, wMinute, wSecond, wMilliseconds;
            public ushort uYear, uMonth, uDayOfWeek, uDay, uHour, uMinute, uSecond, uMilliseconds;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnAuthenticated;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnUnknown;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fIssueInquiry;
            public byte cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [DllImport("BluetoothApis.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS searchParams, ref BLUETOOTH_DEVICE_INFO deviceInfo);

        [DllImport("BluetoothApis.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool BluetoothFindNextDevice(IntPtr hFind, ref BLUETOOTH_DEVICE_INFO deviceInfo);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        public static extern bool BluetoothFindDeviceClose(IntPtr hFind);

        [DllImport("Bthprops.cpl", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint BluetoothAuthenticateDevice(
            IntPtr hwndParent,
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pDevice,
            string pszPasskey,
            ulong ulPasskeyLength
        );

        [DllImport("Bthprops.cpl", SetLastError = true)]
        public static extern uint BluetoothSetServiceState(
            IntPtr hRadio,
            ref BLUETOOTH_DEVICE_INFO pbtdi,
            ref Guid pGuidService,
            uint dwServiceFlags
        );

        // 지원되는 Xprinter 브랜드 및 모델 키워드 리스트 (일반 사무용 프린터 배제)
        public static readonly List<string> SupportedKeywords = new List<string>
        {
            "XPRINTER", "XP-", "XP", "423B", "T265B", "T4501", "T467B", "TT428", "TT424", 
            "237B", "375B", "370B", "DT108B", "326B", "427B", "DT325B", "325B",
            "D4601B", "D4602B", "D4604B", "P423B", "D461B", "D481B", "D480B", "D481", "D480", "D46", "481B",
            "XP420B", "XP433B", "XP423B", "XP481B"
        };

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

        public static bool IsXprinterDevice(string name, string driver)
        {
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(driver)) return false;
            string target = $"{name} {driver}".ToUpperInvariant();
            // 일반 사무용 복합기(HP, Samsung, Canon, Sindoh 등) 배제를 위해 Xprinter 전용 키워드만 엄격 검사
            return SupportedKeywords.Any(k => target.Contains(k));
        }

        /// <summary>
        /// 가상 복사본("1 복사", "Copy 1" 등) 및 통신 접두사를 정규화하여 기본 프린터 모델명 추출
        /// </summary>
        public static string GetBasePrinterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string baseName = name;
            baseName = Regex.Replace(baseName, @"^\[(블루투스|네트워크)\]\s*", "", RegexOptions.IgnoreCase);
            baseName = Regex.Replace(baseName, @"\s*\(\d+\s*복사\)", "", RegexOptions.IgnoreCase);
            baseName = Regex.Replace(baseName, @"\s*\(\s*Copy\s*\d+\)", "", RegexOptions.IgnoreCase);
            baseName = Regex.Replace(baseName, @"\s*\(\s*복사\s*\d+\)", "", RegexOptions.IgnoreCase);
            baseName = Regex.Replace(baseName, @"\s*Copy\s*\d+", "", RegexOptions.IgnoreCase);
            return baseName.Trim();
        }

        /// <summary>
        /// 복사본 표기("1 복사", "Copy 1")가 없는 오리지널 등록 장치명인지 확인
        /// </summary>
        public static bool IsOriginalPrinterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return !Regex.IsMatch(name, @"(\(\d+\s*복사\)|\(\s*Copy\s*\d+\)|\(\s*복사\s*\d+\)|Copy\s*\d+)", RegexOptions.IgnoreCase);
        }

        private static List<string> _cachedComPorts;
        private static DateTime _lastComPortsCheckTime = DateTime.MinValue;
        private static List<UsbPrinterInfo> _cachedUsbPrinters;
        private static DateTime _lastUsbPrintersCheckTime = DateTime.MinValue;
        private static Dictionary<string, string> _cachedBtDeviceMap;
        private static DateTime _lastBtDeviceMapTime = DateTime.MinValue;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _btPortToNameCache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _btNameToPortCache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _netIpToNameCache = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _lastLookupCacheClearTime = DateTime.Now;

        public static void InvalidatePrinterCache()
        {
            _lastComPortsCheckTime = DateTime.MinValue;
            _lastUsbPrintersCheckTime = DateTime.MinValue;
            _lastBtDeviceMapTime = DateTime.MinValue;
            _btPortToNameCache.Clear();
            _btNameToPortCache.Clear();
            _netIpToNameCache.Clear();
            _lastLookupCacheClearTime = DateTime.Now;
        }

        private static void CheckLookupCacheExpiration()
        {
            if ((DateTime.Now - _lastLookupCacheClearTime).TotalSeconds > 15)
            {
                _btPortToNameCache.Clear();
                _btNameToPortCache.Clear();
                _netIpToNameCache.Clear();
                _lastLookupCacheClearTime = DateTime.Now;
            }
        }

        public static List<string> GetAvailableComPorts()
        {
            if (_cachedComPorts != null && (DateTime.Now - _lastComPortsCheckTime).TotalSeconds < 3.0)
            {
                return new List<string>(_cachedComPorts);
            }

            var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var p in SerialPort.GetPortNames())
                {
                    if (!string.IsNullOrWhiteSpace(p)) ports.Add(p.Trim().ToUpper());
                }
            }
            catch { }

            // 현재 설정된 블루투스 포트 포함 (디스크 재로드 없이 메모리 싱글톤 사용)
            try
            {
                var settings = AppSettings.Default;
                if (settings != null && !string.IsNullOrWhiteSpace(settings.BluetoothPortName))
                {
                    ports.Add(settings.BluetoothPortName.Trim().ToUpper());
                }
            }
            catch { }

            if (ports.Count == 0)
            {
                ports.Add("COM3");
            }

            var sorted = ports.OrderBy(p =>
            {
                var match = Regex.Match(p, @"\d+");
                return match.Success ? int.Parse(match.Value) : 999;
            }).ToList();

            _cachedComPorts = sorted;
            _lastComPortsCheckTime = DateTime.Now;
            return new List<string>(sorted);
        }

        public static List<UsbPrinterInfo> GetConnectedUsbPrinters(string targetPrinterName = null, IEnumerable<string> targetSerials = null)
        {
            Func<string, string> normalize = (s) => (s ?? "").ToUpperInvariant().Trim().Replace(" ", "");
            var normalizedTargetSerials = targetSerials != null
                ? new HashSet<string>(targetSerials.Select(normalize).Where(s => !string.IsNullOrEmpty(s)))
                : null;

            bool hasSerialFilter = normalizedTargetSerials != null && normalizedTargetSerials.Count > 0;
            if (!hasSerialFilter && _cachedUsbPrinters != null && (DateTime.Now - _lastUsbPrintersCheckTime).TotalSeconds < 1.5)
            {
                return _cachedUsbPrinters.Select(p => new UsbPrinterInfo
                {
                    Name = p.Name,
                    FriendlyName = p.FriendlyName,
                    PortName = p.PortName,
                    Serial = p.Serial,
                    Type = p.Type,
                    Address = p.Address,
                    IsPaired = p.IsPaired
                }).ToList();
            }

            var printers = new List<UsbPrinterInfo>();
            var rawCandidates = new List<Tuple<UsbPrinterInfo, bool, bool>>();

            // [1단계] USBPRINT 레지스트리를 통한 하드웨어 시리얼 매핑 테이블 구축
            var serialParentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string usbKeyPath = @"SYSTEM\CurrentControlSet\Enum\USB";
                using (var usbKey = Registry.LocalMachine.OpenSubKey(usbKeyPath))
                {
                    if (usbKey != null)
                    {
                        foreach (var vidPid in usbKey.GetSubKeyNames())
                        {
                            using (var devKey = usbKey.OpenSubKey(vidPid))
                            {
                                if (devKey == null) continue;
                                foreach (var instanceKeyName in devKey.GetSubKeyNames())
                                {
                                    using (var instanceKey = devKey.OpenSubKey(instanceKeyName))
                                    {
                                        if (instanceKey == null) continue;
                                        string serial = instanceKeyName;
                                        string parent = instanceKey.GetValue("ParentIdPrefix") as string;
                                        if (!string.IsNullOrEmpty(serial) && !string.IsNullOrEmpty(parent))
                                        {
                                            serialParentMap[parent] = serial;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // [2단계] WMI를 통한 프린터 장치 및 포트 스캔
            try
            {
                if (IsAppExiting) return printers;

                lock (_wmiLock)
                {
                    if (IsAppExiting) return printers;

                    string usbPrintPath = @"SYSTEM\CurrentControlSet\Enum\USBPRINT";
                    using (var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT Name, PortName, DriverName, WorkOffline FROM Win32_Printer"))
                    using (var collection = searcher.Get())
                    {
                        foreach (System.Management.ManagementObject obj in collection)
                        {
                            if (IsAppExiting) break;
                            using (obj)
                            {
                        string name = obj["Name"]?.ToString() ?? "";
                        string port = obj["PortName"]?.ToString() ?? "";
                        string driver = obj["DriverName"]?.ToString() ?? "";
                        bool workOffline = (obj["WorkOffline"]?.ToString() == "True");

                        if (string.IsNullOrEmpty(name)) continue;

                        // 🔍 Xprinter 관련 키워드 엄격 필터링 (일반 사무용 프린터 인쇄 방지)
                        if (!IsXprinterDevice(name, driver)) continue;

                        // 연결 방식 판별
                        string connType = "USB";
                        bool isUsbPort = port.StartsWith("USB", StringComparison.OrdinalIgnoreCase);
                        bool isBtPort = port.ToUpper().Contains("COM") || port.ToUpper().Contains("BTH") || port.ToUpper().Contains("BLUETOOTH");
                        bool isLanPort = !isUsbPort && !isBtPort && 
                                        (Regex.IsMatch(port, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b") || 
                                         port.StartsWith("IP_", StringComparison.OrdinalIgnoreCase) || 
                                         port.StartsWith("WSD", StringComparison.OrdinalIgnoreCase) ||
                                         port.Contains(":9100"));

                        if (isLanPort) connType = "LAN";
                        else if (isBtPort) connType = "Bluetooth";
                        else connType = "USB";

                        // 물리적/통신 연결 상태 사전 판별 (후보 우선순위 선정에 활용)
                        bool isPhysicallyOnline = false;
                        if (connType == "USB")
                        {
                            isPhysicallyOnline = BarcodePrint.Services.PrinterService.IsUsbPortConnected(port, name);
                        }
                        else if (connType == "LAN")
                        {
                            isPhysicallyOnline = PrinterOnlineDetector.IsLanPortReachable(port);
                        }
                        else if (connType == "Bluetooth")
                        {
                            isPhysicallyOnline = GetAvailableComPorts().Contains(port, StringComparer.OrdinalIgnoreCase);
                        }

                        string serial = "";

                        // 1. USB 연결 시리얼 조회
                        if (connType == "USB")
                        {
                            try
                            {
                                using (var usbPrintKey = Registry.LocalMachine.OpenSubKey(usbPrintPath))
                                {
                                    if (usbPrintKey != null)
                                    {
                                        foreach (var printerKeyName in usbPrintKey.GetSubKeyNames())
                                        {
                                            using (var pKey = usbPrintKey.OpenSubKey(printerKeyName))
                                            {
                                                if (pKey == null) continue;
                                                foreach (var instName in pKey.GetSubKeyNames())
                                                {
                                                    if (instName.IndexOf(port, StringComparison.OrdinalIgnoreCase) >= 0)
                                                    {
                                                        int idx = instName.IndexOf("&USB", StringComparison.OrdinalIgnoreCase);
                                                        string parentId = (idx > 0) ? instName.Substring(0, idx) : instName;
                                                        if (serialParentMap.TryGetValue(parentId, out string foundSer))
                                                        {
                                                            serial = foundSer;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(serial)) break;
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        // 2. TCP/IP (LAN) 또는 Bluetooth 연결: 목록 열거 중에는 물리 포트(COM/ARP) 블로킹 호출을 하지 않고 레지스트리에서 즉시 확인
                        else if (connType == "LAN" && hasSerialFilter)
                        {
                            var ipMatch = Regex.Match(port, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                            if (ipMatch.Success)
                            {
                                string ip = ipMatch.Value;
                                serial = GetMacAddressFromIp(ip);
                            }
                        }

                        // 시리얼 번호 미확인 시 기본 레지스트리 검색 폴백
                        if (string.IsNullOrEmpty(serial))
                        {
                            serial = ExtractSerialFromPortOrRegistry(port);
                        }

                        // 인증 시리얼 필터링
                        bool isAllowed = true;
                        if (hasSerialFilter)
                        {
                            string norm = normalize(serial);
                            isAllowed = !string.IsNullOrEmpty(norm) && normalizedTargetSerials.Contains(norm);
                        }

                        if (isAllowed)
                        {
                            rawCandidates.Add(Tuple.Create(new UsbPrinterInfo
                            {
                                Name = name,
                                FriendlyName = driver,
                                PortName = port,
                                Serial = !string.IsNullOrEmpty(serial) ? serial : "-",
                                Type = connType,
                                Address = port
                            }, workOffline, isPhysicallyOnline));
                        }
                    }
                }
            }
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프린터 탐색 오류: {ex.Message}");
            }

            // [3단계] 시스템에 설치된 프린터 목록 수집 (온라인 상태 필터 제거)
            // Windows에 등록된 모든 Xprinter 라벨 프린터를 표시하며, 동일 포트의 중복 복사본만 정규화
            try
            {
                // 기본 모델명, 포트명, 연결 방식을 기준으로 그룹화하여 동일 포트 중복만 정리
                var grouped = rawCandidates.GroupBy(c => new
                {
                    BaseName = GetBasePrinterName(c.Item1.Name),
                    Port = (c.Item1.PortName ?? "").Trim().ToUpperInvariant(),
                    Type = c.Item1.Type
                });

                foreach (var g in grouped)
                {
                    // 동일 포트에 원본과 복사본이 중복된 경우: 원본 장치명 우선, 스풀러 온라인 우선
                    var best = g.OrderByDescending(c => !c.Item2) // !isWorkOffline 우선
                                .ThenByDescending(c => IsOriginalPrinterName(c.Item1.Name))
                                .ThenBy(c => c.Item1.Name)
                                .First();

                    if (!printers.Any(p => p.Name.Equals(best.Item1.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        printers.Add(best.Item1);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프린터 목록 정리 오류: {ex.Message}");
                foreach (var c in rawCandidates)
                {
                    if (!printers.Any(p => p.Name.Equals(c.Item1.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        printers.Add(c.Item1);
                    }
                }
            }

            printers = printers
                .OrderByDescending(p => p.Type == "USB")
                .ThenBy(p => p.Name)
                .ToList();

            if (!hasSerialFilter)
            {
                _cachedUsbPrinters = printers;
                _lastUsbPrintersCheckTime = DateTime.Now;
            }

            return printers;
        }

        // New_xPix 검증 표준 비출력(Silent) 쿼리: 인쇄 엔진을 작동시키지 않고 통신 포트로만 데이터를 회신하는 TSPL 공식 명령
        public const string SafeTsplInfoQuery = "OUT \"MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");\"|\"\r\n";

        /// <summary>
        /// TCP/IP 9100 포트 소켓으로 비출력 안전 쿼리(OUT GETSETTING$)를 전송하여 모델명 및 시리얼 번호 획득 (Socket 직접 제어로 NRE 완벽 방어)
        /// </summary>
        public static string QueryTsplSerialViaTcp(string ipAddress, int port = 9100, int timeoutMs = 1500)
        {
            if (string.IsNullOrEmpty(ipAddress)) return null;

            // IP 주소 정규화 (IP_192.168.0.50 -> 192.168.0.50)
            var match = Regex.Match(ipAddress, @"(\d{1,3}\.){3}\d{1,3}");
            string ip = match.Success ? match.Value : ipAddress.Trim();

            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;
                socket.SendTimeout = timeoutMs;
                socket.ReceiveTimeout = timeoutMs;

                var result = socket.BeginConnect(ip, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(timeoutMs, true);

                if (!success || !socket.Connected)
                {
                    try { socket.Close(); } catch { }
                    return null;
                }

                socket.EndConnect(result);

                // 인쇄를 유발하지 않는 TSPL 공식 OUT 질의문 전송
                byte[] queryCmd = Encoding.ASCII.GetBytes(SafeTsplInfoQuery);
                socket.Send(queryCmd);

                System.Threading.Thread.Sleep(200);

                byte[] buffer = new byte[1024];
                int bytesRead = socket.Receive(buffer);
                if (bytesRead > 0)
                {
                    string raw = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    return ParseSerialFromTsplResponse(raw);
                }
            }
            catch { }
            finally
            {
                if (socket != null)
                {
                    try { if (socket.Connected) socket.Shutdown(SocketShutdown.Both); } catch { }
                    try { socket.Close(); } catch { }
                }
            }
            return null;
        }

        /// <summary>
        /// 블루투스 가상 COM 포트로 비출력 안전 쿼리를 전송하여 연결 점검 및 시리얼/응답 획득
        /// </summary>
        /// <summary>
        /// 블루투스 가상 COM 포트로 비출력 안전 쿼리를 전송하여 연결 점검 및 시리얼/응답 획득
        /// </summary>
        /// <summary>
        /// 블루투스 가상 COM 포트로 비출력 안전 쿼리를 전송하여 연결 점검 및 시리얼/응답 획득
        /// </summary>
        public static string QueryTsplSerialViaCom(string comPortName, int baudRate = 9600, int timeoutMs = 2500)
        {
            if (string.IsNullOrEmpty(comPortName)) return null;

            string cleanPort = Regex.Match(comPortName, @"COM\d+", RegexOptions.IgnoreCase).Value;
            if (string.IsNullOrEmpty(cleanPort)) cleanPort = comPortName.Trim().TrimEnd(':');

            PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] 가상 COM 포트 통신 점검 시작 (입력: '{comPortName}')");

            // 1차 시도 (지정된 baudRate, 기본 9600, 타임아웃 2500ms)
            string res = QueryTsplSerialInternal(cleanPort, baudRate, timeoutMs);
            if (!string.IsNullOrEmpty(res) && res != "-")
            {
                PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] 1차 시도(Baud {baudRate}) 성공: '{res}'");
                return res;
            }

            // 2차 시도 (fallbackBaud: 115200 <-> 9600)
            int fallbackBaud = (baudRate == 9600) ? 115200 : 9600;
            res = QueryTsplSerialInternal(cleanPort, fallbackBaud, 2000);
            if (!string.IsNullOrEmpty(res) && res != "-")
            {
                PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] 2차 시도(Baud {fallbackBaud}) 성공: '{res}'");
                return res;
            }

            // 3차 시도: 포트가 이미 스풀러나 드라이버에 의해 열려있는 경우 Windows Spooler 점검
            try
            {
                if (IsAppExiting) return null;

                lock (_wmiLock)
                {
                    if (IsAppExiting) return null;

                    using (var searcher = new System.Management.ManagementObjectSearcher(
                        $"SELECT Name, PortName, PrinterStatus, WorkOffline FROM Win32_Printer WHERE PortName LIKE '%{cleanPort}%'"))
                    using (var collection = searcher.Get())
                    {
                        foreach (System.Management.ManagementObject obj in collection)
                        {
                            if (IsAppExiting) break;
                            using (obj)
                            {
                                bool offline = (bool)(obj["WorkOffline"] ?? false);
                                if (!offline)
                                {
                                    string pName = obj["Name"]?.ToString() ?? cleanPort;
                                    PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] 스풀러 드라이버 온라인 감지: '{pName}'");
                                    return $"정상 준비 (드라이버 {pName})";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exWmi)
            {
                PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] WMI 스풀러 점검 예외: {exWmi.Message}");
            }

            PrinterSerialLogger.WriteCommLog("COM-TEST", $"[{cleanPort}] 통신 점검 실패 (응답 없음)");
            return null;
        }

        private static string QueryTsplSerialInternal(string cleanPort, int baudRate, int timeoutMs)
        {
            // 1단계: Windows Bluetooth 가상 COM 포트(BthModem)를 위한 NativeSerialStream 우선 시도
            try
            {
                using (var nativeStream = BarcodePrint.Services.NativeSerialStream.Open(cleanPort))
                {
                    PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] NativeSerialStream.Open() 성공!");
                    Thread.Sleep(150);

                    // ~!T
                    string tResp = nativeStream.QueryTsplCommand("~!T\r\n", 1200);
                    if (!string.IsNullOrEmpty(tResp) && !tResp.Contains("!"))
                    {
                        var m = Regex.Match(tResp, @"V\d+(\.\d+)+", RegexOptions.IgnoreCase);
                        return m.Success ? m.Value : tResp;
                    }

                    // TSPL 표준 질의
                    string queryCmd = 
                        "OUT \"MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");" +
                        "\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");" +
                        "\"|VER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"VERSION\");" +
                        "\"|DEN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"DENSITY\");\"|\"\r\n";

                    string resp = nativeStream.QueryTsplCommand(queryCmd, timeoutMs);
                    if (!string.IsNullOrEmpty(resp))
                    {
                        string parsed = ParseSerialFromTsplResponse(resp);
                        if (!string.IsNullOrEmpty(parsed) && parsed != "-") return parsed;
                        var mdlMatch = Regex.Match(resp, @"MDL=([^\|\r\n]+)");
                        if (mdlMatch.Success && !string.IsNullOrEmpty(mdlMatch.Groups[1].Value.Trim()))
                            return mdlMatch.Groups[1].Value.Trim();
                        return "정상 준비 (Ready)";
                    }

                    if (nativeStream.IsOpen)
                    {
                        PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] NativeSerialStream 포트 열림 확인 (준비 완료)");
                        return "정상 준비 (Ready)";
                    }
                }
            }
            catch (Exception exNative)
            {
                PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] NativeSerialStream 시도 예외: {exNative.Message}");
            }

            // 2단계: 표준 .NET SerialPort 시도
            try
            {
                PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] SerialPort.Open() 시도 (Baud: {baudRate})...");
                using (var serialPort = new SerialPort(cleanPort, baudRate, Parity.None, 8, StopBits.One))
                {
                    serialPort.ReadTimeout = 2000;
                    serialPort.WriteTimeout = 1500;
                    serialPort.DtrEnable = true;
                    serialPort.RtsEnable = true;
                    serialPort.Open();

                    PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] SerialPort.Open() 성공! (IsOpen: {serialPort.IsOpen})");

                    // 블루투스 RFCOMM 버퍼 안정화 대기
                    Thread.Sleep(200);

                    // 1) ~!T 및 ~!V 빠른 펌웨어 질의
                    try
                    {
                        serialPort.DiscardInBuffer();
                        serialPort.Write("~!T\r\n");
                        Thread.Sleep(120);
                        if (serialPort.BytesToRead > 0)
                        {
                            byte[] tBuf = new byte[256];
                            int tr = serialPort.Read(tBuf, 0, tBuf.Length);
                            string tStr = Encoding.ASCII.GetString(tBuf, 0, tr).Replace("\r", "").Replace("\n", "").Trim();
                            PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] RX (~!T): '{tStr}'");
                            if (!string.IsNullOrEmpty(tStr) && !tStr.Contains("!")) return tStr;
                        }
                    }
                    catch { }

                    // 2) 표준 TSPL 질의
                    string queryCmd = 
                        "OUT \"MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");" +
                        "\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");" +
                        "\"|VER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"VERSION\");" +
                        "\"|DEN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"DENSITY\");\"|\"\r\n";

                    byte[] cmdBytes = Encoding.ASCII.GetBytes(queryCmd);
                    serialPort.Write(cmdBytes, 0, cmdBytes.Length);
                    PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] TX: {queryCmd.Trim()}");

                    var sw = Stopwatch.StartNew();
                    var acc = new List<byte>();
                    byte[] buffer = new byte[2048];

                    while (sw.ElapsedMilliseconds < timeoutMs)
                    {
                        if (serialPort.BytesToRead > 0)
                        {
                            int bytesRead = serialPort.Read(buffer, 0, Math.Min(buffer.Length, serialPort.BytesToRead));
                            if (bytesRead > 0)
                            {
                                byte[] sl = new byte[bytesRead];
                                Array.Copy(buffer, 0, sl, 0, bytesRead);
                                acc.AddRange(sl);
                                string cur = Encoding.ASCII.GetString(acc.ToArray());

                                string parsed = ParseSerialFromTsplResponse(cur);
                                if (!string.IsNullOrEmpty(parsed) && parsed != "-")
                                {
                                    PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 시리얼 파싱 성공: {parsed}");
                                    return parsed;
                                }

                                if (cur.Contains("MDL=") || cur.Contains("DEN=") || cur.Contains("VER="))
                                {
                                    PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 응답 키워드 수신: '{cur}'");
                                    var mdlMatch = Regex.Match(cur, @"MDL=([^\|\r\n]+)");
                                    if (mdlMatch.Success && !string.IsNullOrEmpty(mdlMatch.Groups[1].Value.Trim()))
                                    {
                                        return mdlMatch.Groups[1].Value.Trim();
                                    }
                                    return "정상 준비 (Ready)";
                                }
                            }
                        }
                        Thread.Sleep(40);
                    }

                    if (acc.Count > 0)
                    {
                        string fullResp = Encoding.ASCII.GetString(acc.ToArray()).Trim();
                        PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 전체 수신 데이터: '{fullResp}'");
                        string parsed = ParseSerialFromTsplResponse(fullResp);
                        if (!string.IsNullOrEmpty(parsed) && parsed != "-") return parsed;
                        if (fullResp.Length > 2) return "정상 준비 (Ready)";
                    }

                    // 3) 보조 쿼리: ~HS
                    serialPort.DiscardInBuffer();
                    serialPort.Write("~HS\r\n");
                    Thread.Sleep(120);
                    if (serialPort.BytesToRead > 0)
                    {
                        PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] ~HS 응답 있음");
                        return "정상 준비 (Ready)";
                    }

                    if (serialPort.IsOpen)
                    {
                        PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 포트 Open 유지됨 (데이터 미수신)");
                        return "정상 준비 (Ready)";
                    }
                }
            }
            catch (UnauthorizedAccessException uex)
            {
                PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 포트 점유 중 (UnauthorizedAccess): {uex.Message}");
                return "정상 준비 (프린터 드라이버 연결 중)";
            }
            catch (Exception ex)
            {
                PrinterSerialLogger.WriteCommLog("COM-INT", $"[{cleanPort}] 시리얼 통신 예외 ({ex.GetType().Name}): {ex.Message}");
            }
            return null;
        }

        public static string ParseSerialFromTsplResponse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "-";

            // 1. "SER=XXXXX|" 파싱 (New_xPix 표준 포맷)
            if (raw.Contains("SER="))
            {
                var match = Regex.Match(raw, @"SER=([^\|\r\n]+)");
                if (match.Success)
                {
                    string sn = match.Groups[1].Value.Trim().Replace("\"", "");
                    if (!string.IsNullOrEmpty(sn) && sn != "-")
                    {
                        return sn;
                    }
                }
            }

            // 2. 쉼표/구분자 파싱 (구형 포맷 호환)
            var parts = raw.Split(new[] { '|', ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                string clean = p.Trim().Replace("\"", "");
                if (clean.StartsWith("SER=")) clean = clean.Substring(4).Trim();
                if (clean.Length >= 6 && clean.Any(char.IsDigit) && !clean.Contains("MDL=") && !clean.Contains("DPI="))
                {
                    return clean;
                }
            }
            return raw.Replace("\"", "").Trim();
        }

        /// <summary>
        /// IP 주소로부터 ARP 테이블을 조회하여 물리적 MAC 주소 획득
        /// </summary>
        public static string GetMacAddressFromIp(string ipAddress)
        {
            try
            {
                var match = Regex.Match(ipAddress, @"(\d{1,3}\.){3}\d{1,3}");
                string cleanIp = match.Success ? match.Value : ipAddress;

                if (System.Net.IPAddress.TryParse(cleanIp, out var ip))
                {
                    byte[] mac = new byte[6];
                    uint len = (uint)mac.Length;
                    int ipInt = BitConverter.ToInt32(ip.GetAddressBytes(), 0);
                    if (SendARP(ipInt, 0, mac, ref len) == 0)
                    {
                        return string.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}",
                            mac[0], mac[1], mac[2], mac[3], mac[4], mac[5]);
                    }
                }
            }
            catch { }
            return "-";
        }

        public static string ExtractSerialFromPortOrRegistry(string port)
        {
            if (string.IsNullOrEmpty(port)) return "-";
            try
            {
                string keyPath = $@"SYSTEM\CurrentControlSet\Control\Print\Printers";
                using (var printersKey = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (printersKey != null)
                    {
                        foreach (var pName in printersKey.GetSubKeyNames())
                        {
                            using (var pKey = printersKey.OpenSubKey(pName))
                            {
                                string pPort = pKey?.GetValue("Port") as string;
                                if (string.Equals(pPort, port, StringComparison.OrdinalIgnoreCase))
                                {
                                    string sn = pKey?.GetValue("SerialNumber") as string;
                                    if (!string.IsNullOrEmpty(sn)) return sn;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return "-";
        }

        public static string FindSerialByPrinterName(string printerName)
        {
            if (string.IsNullOrEmpty(printerName)) return "";
            var printers = GetConnectedUsbPrinters(printerName);
            foreach (var p in printers)
            {
                if (!string.IsNullOrEmpty(p.Name) && p.Name.IndexOf(printerName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return p.Serial;
                }
            }
            return "";
        }

        /// <summary>
        /// SNMP 유니캐스트를 통해 장치의 모델명을 안전하게(인쇄 없이) 조회 (New_xPix 방식)
        /// </summary>
        public static string QuerySnmpModelName(string ip)
        {
            try
            {
                using (var udp = new System.Net.Sockets.UdpClient())
                {
                    udp.Client.ReceiveTimeout = 600;
                    var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), 161);

                    // SNMP GetRequest for sysDescr (1.3.6.1.2.1.1.1.0)
                    byte[] snmpPayload = new byte[] { 
                        0x30, 0x29, 0x02, 0x01, 0x01, 0x04, 0x06, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 
                        0xa0, 0x1c, 0x02, 0x04, 0x72, 0x05, 0x1d, 0x47, 0x02, 0x01, 0x00, 0x02, 0x01, 0x00, 
                        0x30, 0x0e, 0x30, 0x0c, 0x06, 0x08, 0x2b, 0x06, 0x01, 0x02, 0x01, 0x01, 0x01, 0x00, 0x05, 0x00 
                    };

                    udp.Send(snmpPayload, snmpPayload.Length, endpoint);
                    var remoteEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                    byte[] response = udp.Receive(ref remoteEP);

                    var candidates = ExtractPrintableStrings(response);
                    foreach (var c in candidates)
                    {
                        if (c.Equals("public", StringComparison.OrdinalIgnoreCase)) continue;
                        if (c.Contains("XP-") || c.Contains("Xprinter") || c.Contains("D4") || c.Contains("42") || c.Contains("37") || c.Contains("23") || c.Contains("PRINTER"))
                        {
                            var match = Regex.Match(c, @"XP-[A-Z0-9]+", RegexOptions.IgnoreCase);
                            if (match.Success) return match.Value.ToUpper();
                            return c.Trim();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// New_xPix 순수 UDP 브로드캐스트 & SNMP 모델명 해석 방식: 인쇄 포트(9100)를 일체 건드리지 않고 모델명과 IP를 안전하게 감지
        /// </summary>
        public static async Task<List<UsbPrinterInfo>> ScanSubnetForXprinterTcpAsync()
        {
            var found = new List<UsbPrinterInfo>();
            await Task.Run(() =>
            {
                try
                {
                    // 0. 시스템에 이미 설치된 네트워크 프린터 WMI 목록 사전 조회
                    var installedNetPrinters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        if (!IsAppExiting)
                        {
                            lock (_wmiLock)
                            {
                                if (!IsAppExiting)
                                {
                                    using (var searcher = new ManagementObjectSearcher(
                                        "SELECT Name, PortName, DriverName FROM Win32_Printer"))
                                    using (var collection = searcher.Get())
                                    {
                                        foreach (ManagementObject p in collection)
                                        {
                                            if (IsAppExiting) break;
                                            using (p)
                                            {
                                                string pName = p["Name"]?.ToString() ?? "";
                                                string pPort = p["PortName"]?.ToString() ?? "";
                                                string pDriver = p["DriverName"]?.ToString() ?? "";

                                                var match = Regex.Match(pPort, @"(\d{1,3}\.){3}\d{1,3}");
                                                if (match.Success)
                                                {
                                                    string ip = match.Value;
                                                    string model = !string.IsNullOrEmpty(pName) ? pName : pDriver;
                                                    installedNetPrinters[ip] = model;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // 1. New_xPix 검증 방식: 순수 UDP 브로드캐스트 (30303: Xprinter 전용 포트, 161: SNMP)
                    int[] ports = { 30303, 161 };
                    var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                        .Where(i => i.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                                    i.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);

                    foreach (var ni in interfaces)
                    {
                        var props = ni.GetIPProperties();
                        foreach (var addr in props.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;

                            try
                            {
                                var ipBytes = addr.Address.GetAddressBytes();
                                var maskBytes = addr.IPv4Mask != null ? addr.IPv4Mask.GetAddressBytes() : new byte[] { 255, 255, 255, 0 };
                                var bcBytes = new byte[4];
                                for (int i = 0; i < 4; i++) bcBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                                var subnetBroadcast = new System.Net.IPAddress(bcBytes);

                                System.Net.IPAddress[] targets = { subnetBroadcast, System.Net.IPAddress.Broadcast };

                                foreach (var targetIp in targets)
                                {
                                    using (var udp = new System.Net.Sockets.UdpClient())
                                    {
                                        udp.Client.ReceiveTimeout = 600;
                                        udp.EnableBroadcast = true;

                                        foreach (int port in ports)
                                        {
                                            try
                                            {
                                                var endpoint = new System.Net.IPEndPoint(targetIp, port);
                                                byte[] payload = port == 30303 ? new byte[] { 0xff, 0xff, 0x01, 0x01, 0x00, 0x00 } :
                                                                                new byte[] { 0x30, 0x26, 0x02, 0x01, 0x01, 0x04, 0x06, 0x70, 0x75, 0x62, 0x6c, 0x69, 0x63, 0xa0, 0x19, 0x02, 0x04, 0x72, 0x05, 0x1d, 0x47, 0x02, 0x01, 0x00, 0x02, 0x01, 0x00, 0x30, 0x0b, 0x30, 0x09, 0x06, 0x05, 0x2b, 0x06, 0x01, 0x02, 0x01, 0x05, 0x00 };

                                                udp.Send(payload, payload.Length, endpoint);

                                                DateTime start = DateTime.Now;
                                                while ((DateTime.Now - start).TotalMilliseconds < 600)
                                                {
                                                    if (udp.Available > 0)
                                                    {
                                                        var remoteEP = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                                                        byte[] response = udp.Receive(ref remoteEP);
                                                        string resIp = remoteEP.Address.ToString();

                                                        if (!found.Any(f => f.Address == resIp))
                                                        {
                                                            string modelName = null;

                                                            // 1) 이미 설치된 WMI 드라이버 이름 우선 매칭
                                                            if (installedNetPrinters.TryGetValue(resIp, out string instModel) && !string.IsNullOrEmpty(instModel))
                                                            {
                                                                modelName = instModel;
                                                            }

                                                            // 2) SNMP sysDescr 질의로 모델명 조회
                                                            if (string.IsNullOrEmpty(modelName))
                                                            {
                                                                modelName = QuerySnmpModelName(resIp);
                                                            }

                                                            // 3) UDP 패킷 내 문자열 탐색
                                                            if (string.IsNullOrEmpty(modelName))
                                                            {
                                                                var strings = ExtractPrintableStrings(response);
                                                                foreach (var str in strings)
                                                                {
                                                                    if (str.Equals("public", StringComparison.OrdinalIgnoreCase)) continue;
                                                                    if (str.Contains("XP-") || str.Contains("Xprinter") || str.Contains("D48") || str.Contains("42"))
                                                                    {
                                                                        var match = Regex.Match(str, @"XP-[A-Z0-9]+", RegexOptions.IgnoreCase);
                                                                        modelName = match.Success ? match.Value.ToUpper() : str;
                                                                        break;
                                                                    }
                                                                }
                                                            }

                                                            // 4) 기본 이름 폴백
                                                            if (string.IsNullOrEmpty(modelName))
                                                            {
                                                                modelName = "Xprinter Network";
                                                            }

                                                            // MAC 주소 추출
                                                            string mac = GetMacAddressFromIp(resIp);

                                                            found.Add(new UsbPrinterInfo
                                                            {
                                                                Name = $"{modelName} ({resIp})",
                                                                FriendlyName = modelName,
                                                                PortName = $"{resIp}:9100",
                                                                Serial = (mac != "-") ? mac : resIp,
                                                                Type = "LAN",
                                                                Address = resIp
                                                            });
                                                        }
                                                    }
                                                    System.Threading.Thread.Sleep(20);
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ScanSubnetForXprinterTcpAsync 오류: {ex.Message}");
                }
            });

            return found;
        }

        private static List<string> ExtractPrintableStrings(byte[] data)
        {
            var strings = new List<string>();
            var current = new StringBuilder();

            foreach (byte b in data)
            {
                if (b >= 32 && b <= 126) // Printable ASCII
                {
                    current.Append((char)b);
                }
                else
                {
                    if (current.Length >= 3) strings.Add(current.ToString());
                    current.Clear();
                }
            }
            if (current.Length >= 3) strings.Add(current.ToString());
            return strings;
        }

        /// <summary>
        /// 시스템에 페어링된 블루투스 장치 마스터 사전(MAC 주소 ➔ 실제 장치 이름) 구축
        /// </summary>
        public static Dictionary<string, string> GetPairedBluetoothDeviceMap()
        {
            if (_cachedBtDeviceMap != null && (DateTime.Now - _lastBtDeviceMapTime).TotalSeconds < 15.0)
            {
                return new Dictionary<string, string>(_cachedBtDeviceMap, StringComparer.OrdinalIgnoreCase);
            }

            var macToDeviceNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // 1. 레지스트리 BTHPORT\Parameters\Devices 에서 페어링된 블루투스 장치명 수집
                using (var devKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices"))
                {
                    if (devKey != null)
                    {
                        foreach (var subMac in devKey.GetSubKeyNames())
                        {
                            using (var sub = devKey.OpenSubKey(subMac))
                            {
                                if (sub == null) continue;
                                string devName = sub.GetValue("Name") as string;
                                if (string.IsNullOrEmpty(devName))
                                {
                                    var rawBytes = sub.GetValue("Name") as byte[];
                                    if (rawBytes != null && rawBytes.Length > 0)
                                    {
                                        devName = System.Text.Encoding.UTF8.GetString(rawBytes).TrimEnd('\0');
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(devName))
                                {
                                    string cleanMac = subMac.Replace(":", "").Replace("-", "").ToUpper();
                                    if (!macToDeviceNameMap.ContainsKey(cleanMac))
                                    {
                                        macToDeviceNameMap[cleanMac] = devName.Trim();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (!IsAppExiting)
                {
                    lock (_wmiLock)
                    {
                        if (!IsAppExiting)
                        {
                            // 2. WMI Win32_PnPEntity 에서 추가 블루투스 장치명 수집
                            using (var searcher = new System.Management.ManagementObjectSearcher(
                                "SELECT Name, Caption, Description, DeviceID, PNPDeviceID FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%BTH%' OR ClassGuid = '{e0cbf06c-cdb3-4647-bb8a-263b43f0f974}'"))
                            using (var collection = searcher.Get())
                            {
                                foreach (System.Management.ManagementObject obj in collection)
                                {
                                    if (IsAppExiting) break;
                                    using (obj)
                                    {
                                        string name = obj["Name"]?.ToString() ?? obj["Caption"]?.ToString() ?? "";
                                        string pnpId = obj["PNPDeviceID"]?.ToString() ?? "";

                                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pnpId)) continue;

                                        if (name.StartsWith("Generic ", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("Bluetooth LE ", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("Bluetooth Device ", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("표준 Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("Service Discovery", StringComparison.OrdinalIgnoreCase) ||
                                            name.StartsWith("Device Information", StringComparison.OrdinalIgnoreCase) ||
                                            name.Contains("Adapter") || name.Contains("어댑터") || name.Contains("Enumerator") || name.Contains("열거자"))
                                        {
                                            continue;
                                        }

                                        var macMatch = Regex.Match(pnpId, @"DEV_([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                        if (!macMatch.Success)
                                        {
                                            macMatch = Regex.Match(pnpId, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                        }

                                        if (macMatch.Success)
                                        {
                                            string rawMac = macMatch.Groups[1].Value.ToUpper();
                                            if (!rawMac.StartsWith("000000000000") && !macToDeviceNameMap.ContainsKey(rawMac))
                                            {
                                                macToDeviceNameMap[rawMac] = name.Trim();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            _cachedBtDeviceMap = macToDeviceNameMap;
            _lastBtDeviceMapTime = DateTime.Now;
            return new Dictionary<string, string>(macToDeviceNameMap, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 시스템에 페어링된 블루투스 프린터/라벨 장치들의 실제 이름 목록을 우선순위 순으로 반환 (예: "Xprinter 7941", "XP-D465B")
        /// </summary>
        public static List<string> GetPairedBluetoothPrinterNames()
        {
            var results = new List<string>();
            var allDevices = GetPairedBluetoothDeviceMap();

            // 1. 라벨/영수증 프린터 키워드 매칭 우선순위
            string[] printerKeywords = new[] { "Xprinter", "XP-", "Label", "Printer", "POS", "Barcode", "MPT", "BTP", "RP", "TSC", "Bixolon", "Zebra", "Sewoo", "Citizen" };
            string[] nonPrinterKeywords = new[] { 
                "mac", "mini", "phone", "iphone", "galaxy", "headset", "earbuds", "buds", "airpods", 
                "mouse", "keyboard", "audio", "speaker", "client", "service", "profile", "프로필", 
                "프로파일", "iap", "spp", "ble", "avrcp", "a2dp", "handsfree", "dongle", "특성" 
            };

            var printerCandidates = new List<string>();
            var fallbackCandidates = new List<string>();

            foreach (var kvp in allDevices)
            {
                string dev = kvp.Value.Trim();
                if (string.IsNullOrEmpty(dev)) continue;

                bool isNonPrinter = nonPrinterKeywords.Any(k => dev.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                if (isNonPrinter) continue;

                bool isPrinter = printerKeywords.Any(k => dev.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                if (isPrinter)
                {
                    if (!printerCandidates.Contains(dev, StringComparer.OrdinalIgnoreCase))
                        printerCandidates.Add(dev);
                }
                else
                {
                    if (!fallbackCandidates.Contains(dev, StringComparer.OrdinalIgnoreCase))
                        fallbackCandidates.Add(dev);
                }
            }

            // 명확한 프린터 후보가 있으면 비-프린터 프로필/서비스는 완전히 배제
            if (printerCandidates.Count > 0)
            {
                results.AddRange(printerCandidates);
            }
            else
            {
                results.AddRange(fallbackCandidates);
            }
            return results;
        }

        /// <summary>
        /// 지정된 포트(예: "COM3") 또는 시스템 환경에서 가장 적합한 실제 블루투스 장치 이름을 반환 (COM 포트 번호 대신 장치명 표기 보장)
        /// </summary>
        public static string GetBluetoothDeviceNameForPort(string portName)
        {
            CheckLookupCacheExpiration();
            string cleanPort = (portName ?? "").Trim().ToUpper();
            if (!string.IsNullOrEmpty(cleanPort) && _btPortToNameCache.TryGetValue(cleanPort, out string cachedName))
            {
                return cachedName;
            }

            // 1. 레지스트리 BTHENUM에서 포트명에 직접 매핑된 장치명 검색
            try
            {
                string bthPath = @"SYSTEM\CurrentControlSet\Enum\BTHENUM";
                using (var bthKey = Registry.LocalMachine.OpenSubKey(bthPath))
                {
                    if (bthKey != null)
                    {
                        var macMap = GetPairedBluetoothDeviceMap();
                        foreach (var sub1 in bthKey.GetSubKeyNames())
                        {
                            using (var k1 = bthKey.OpenSubKey(sub1))
                            {
                                if (k1 == null) continue;
                                foreach (var sub2 in k1.GetSubKeyNames())
                                {
                                    using (var k2 = k1.OpenSubKey(sub2))
                                    {
                                        if (k2 == null) continue;
                                        using (var pKey = k2.OpenSubKey("Device Parameters"))
                                        {
                                            if (pKey != null)
                                            {
                                                string p = (pKey.GetValue("PortName") as string)?.TrimEnd(':').ToUpper();
                                                if (!string.IsNullOrEmpty(p) && p == cleanPort)
                                                {
                                                    var macMatch = Regex.Match(sub2, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                                    if (macMatch.Success)
                                                    {
                                                        string rawMac = macMatch.Groups[1].Value.ToUpper();
                                                        if (macMap.TryGetValue(rawMac, out string mName) && !string.IsNullOrWhiteSpace(mName))
                                                        {
                                                            _btPortToNameCache[cleanPort] = mName;
                                                            return mName;
                                                        }
                                                    }

                                                    string friendly = k2.GetValue("FriendlyName") as string;
                                                    string devDesc = k2.GetValue("DeviceDesc") as string;
                                                    string targetName = !string.IsNullOrEmpty(friendly) ? friendly : devDesc;
                                                    if (!string.IsNullOrEmpty(targetName) && !targetName.StartsWith("표준", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        _btPortToNameCache[cleanPort] = targetName;
                                                        return targetName;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. 시스템에 페어링된 블루투스 프린터가 있다면 최우선 장치명 반환
            var pairedPrinters = GetPairedBluetoothPrinterNames();
            if (pairedPrinters.Count > 0)
            {
                if (!string.IsNullOrEmpty(cleanPort)) _btPortToNameCache[cleanPort] = pairedPrinters[0];
                return pairedPrinters[0];
            }

            // 3. 대체 명칭 (절대 포트 번호로 표기하지 않음)
            return "블루투스 라벨 프린터";
        }

        /// <summary>
        /// 지정된 블루투스 장치명 또는 모델명(예: "XP-D4604B")에 매핑된 실제 COM 포트명을 레지스트리/WMI에서 역조회
        /// </summary>
        public static string GetBluetoothPortForDeviceName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName)) return "";
            CheckLookupCacheExpiration();
            string cleanTarget = deviceName
                .Replace("[블루투스]", "")
                .Replace("(페어링 필요)", "")
                .Replace("(가상포트 연결 필요)", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToUpperInvariant();

            if (string.IsNullOrEmpty(cleanTarget)) return "";
            if (_btNameToPortCache.TryGetValue(cleanTarget, out string cachedPort))
            {
                return cachedPort;
            }

            // 1. 레지스트리 BTHENUM 키에서 PortName 역조회
            try
            {
                string bthPath = @"SYSTEM\CurrentControlSet\Enum\BTHENUM";
                using (var bthKey = Registry.LocalMachine.OpenSubKey(bthPath))
                {
                    if (bthKey != null)
                    {
                        var macMap = GetPairedBluetoothDeviceMap();
                        foreach (var sub1 in bthKey.GetSubKeyNames())
                        {
                            using (var k1 = bthKey.OpenSubKey(sub1))
                            {
                                if (k1 == null) continue;
                                foreach (var sub2 in k1.GetSubKeyNames())
                                {
                                    using (var k2 = k1.OpenSubKey(sub2))
                                    {
                                        if (k2 == null) continue;
                                        using (var pKey = k2.OpenSubKey("Device Parameters"))
                                        {
                                            if (pKey != null)
                                            {
                                                string port = (pKey.GetValue("PortName") as string)?.TrimEnd(':').ToUpper();
                                                if (!string.IsNullOrEmpty(port))
                                                {
                                                    string friendly = (k2.GetValue("FriendlyName") as string) ?? "";
                                                    string devDesc = (k2.GetValue("DeviceDesc") as string) ?? "";
                                                    string cleanF = friendly.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
                                                    string cleanD = devDesc.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

                                                    if (cleanF.Contains(cleanTarget) || cleanTarget.Contains(cleanF) ||
                                                        cleanD.Contains(cleanTarget) || cleanTarget.Contains(cleanD))
                                                    {
                                                        _btNameToPortCache[cleanTarget] = port;
                                                        return port;
                                                    }

                                                    // MAC 매핑 이름 확인
                                                    var macMatch = Regex.Match(sub2, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                                    if (macMatch.Success)
                                                    {
                                                        string rawMac = macMatch.Groups[1].Value.ToUpper();
                                                        if (macMap.TryGetValue(rawMac, out string mName))
                                                        {
                                                            string cleanM = (mName ?? "").Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
                                                            if (cleanM.Contains(cleanTarget) || cleanTarget.Contains(cleanM))
                                                            {
                                                                _btNameToPortCache[cleanTarget] = port;
                                                                return port;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. WMI Win32_PnPEntity 역조회
            try
            {
                if (!IsAppExiting)
                {
                    lock (_wmiLock)
                    {
                        if (!IsAppExiting)
                        {
                            using (var searcher = new System.Management.ManagementObjectSearcher(
                                "SELECT Name, Caption, PNPDeviceID FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e978-e325-11ce-bfc1-08002be10318}' OR Name LIKE '%(COM%'"))
                            using (var collection = searcher.Get())
                            {
                                foreach (System.Management.ManagementObject obj in collection)
                                {
                                    if (IsAppExiting) break;
                                    using (obj)
                                    {
                                        string pnpName = obj["Name"]?.ToString() ?? obj["Caption"]?.ToString() ?? "";
                                        var match = Regex.Match(pnpName, @"(COM\d+)", RegexOptions.IgnoreCase);
                                        if (match.Success)
                                        {
                                            string port = match.Groups[1].Value.ToUpper();
                                            string cleanPnp = pnpName.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
                                            if (cleanPnp.Contains(cleanTarget) || cleanTarget.Contains(cleanPnp))
                                            {
                                                _btNameToPortCache[cleanTarget] = port;
                                                return port;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            _btNameToPortCache[cleanTarget] = "";
            return "";
        }

        /// <summary>
        /// 지정된 IP 주소에 대응하는 네트워크 프린터 장치명(모델명)을 반환 (블루투스처럼 명확한 장치명 표시 보장)
        /// </summary>
        public static string GetNetworkPrinterDeviceName(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return "네트워크 라벨 프린터";
            CheckLookupCacheExpiration();
            string cleanIp = ip.Trim();
            if (_netIpToNameCache.TryGetValue(cleanIp, out string cachedNetName))
            {
                return cachedNetName;
            }

            // 1. Windows 스풀러 드라이버 목록에서 해당 IP 포트를 사용하는 프린터 검색
            try
            {
                var installed = GetConnectedUsbPrinters();
                if (installed != null)
                {
                    var matched = installed.FirstOrDefault(p => !string.IsNullOrEmpty(p.PortName) && p.PortName.IndexOf(cleanIp, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matched != null && !string.IsNullOrEmpty(matched.Name))
                    {
                        _netIpToNameCache[cleanIp] = matched.Name;
                        return matched.Name;
                    }
                }

                if (!IsAppExiting)
                {
                    lock (_wmiLock)
                    {
                        if (!IsAppExiting)
                        {
                            using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, PortName, DriverName FROM Win32_Printer"))
                            using (var collection = searcher.Get())
                            {
                                foreach (System.Management.ManagementObject obj in collection)
                                {
                                    if (IsAppExiting) break;
                                    using (obj)
                                    {
                                        string pName = obj["Name"]?.ToString() ?? "";
                                        string port = obj["PortName"]?.ToString() ?? "";
                                        string driver = obj["DriverName"]?.ToString() ?? "";

                                        if (!string.IsNullOrEmpty(port) && port.IndexOf(cleanIp, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            string found = !string.IsNullOrEmpty(pName) ? pName : driver;
                                            if (!string.IsNullOrEmpty(found))
                                            {
                                                _netIpToNameCache[cleanIp] = found;
                                                return found;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. 이전에 저장된 네트워크 프린터 모델명 확인
            try
            {
                var settings = AppSettings.Default;
                if (settings != null && !string.IsNullOrWhiteSpace(settings.LastTcpPrinterModel))
                {
                    string saved = settings.LastTcpPrinterModel.Trim();
                    _netIpToNameCache[cleanIp] = saved;
                    return saved;
                }
            }
            catch { }

            _netIpToNameCache[cleanIp] = "Xprinter 라벨 프린터";
            return "Xprinter 라벨 프린터";
        }

        /// <summary>
        /// Windows 시스템에 정식 드라이버로 등록된 블루투스/직렬(COM) 라벨 프린터를 스캔하여 UsbPrinterInfo 리스트 반환
        /// (드라이버가 설치되지 않은 미등록/미페어링 기기는 제외하고 정상 인쇄 가능한 드라이버 프린터만 안전하게 반환)
        /// </summary>
        public static async Task<List<UsbPrinterInfo>> ScanBluetoothPrintersAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<UsbPrinterInfo>();
                try
                {
                    // 1. Windows 스풀러(Win32_Printer)에서 라벨 프린터 목록 취득
                    var allInstalled = GetConnectedUsbPrinters();
                    if (allInstalled != null)
                    {
                        foreach (var p in allInstalled)
                        {
                            if (p.Type.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!list.Any(x => x.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(p);
                                }
                            }
                        }
                    }

                    // 2. 포트명이 COMx이거나 BTH로 시작하는 기타 등록된 Xprinter 드라이버 보강
                    if (!IsAppExiting)
                    {
                        lock (_wmiLock)
                        {
                            if (!IsAppExiting)
                            {
                                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, PortName, DriverName FROM Win32_Printer"))
                                using (var collection = searcher.Get())
                                {
                                    foreach (System.Management.ManagementObject obj in collection)
                                    {
                                        if (IsAppExiting) break;
                                        using (obj)
                                        {
                                            string name = obj["Name"]?.ToString() ?? "";
                                            string port = obj["PortName"]?.ToString() ?? "";
                                            string driver = obj["DriverName"]?.ToString() ?? "";

                                            if (string.IsNullOrEmpty(name)) continue;
                                            if (!IsXprinterDevice(name, driver)) continue;

                                            bool isBtPort = port.ToUpper().Contains("COM") || port.ToUpper().Contains("BTH") || port.ToUpper().Contains("BLUETOOTH");
                                            if (isBtPort)
                                            {
                                                if (!list.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.PortName.Equals(port, StringComparison.OrdinalIgnoreCase)))
                                                {
                                                    list.Add(new UsbPrinterInfo
                                                    {
                                                        Name = name,
                                                        FriendlyName = !string.IsNullOrEmpty(driver) ? driver : name,
                                                        PortName = port,
                                                        Serial = "-",
                                                        Type = "Bluetooth",
                                                        Address = port,
                                                        IsPaired = true
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"블루투스 드라이버 프린터 검색 오류: {ex.Message}");
                }
                return list;
            });
        }

        /// <summary>
        /// 레지스트리 BTHENUM 및 SERIALCOMM 에서 지정된 블루투스 MAC 주소에 매핑된 실제 COM 포트명을 조회
        /// </summary>
        public static string FindComPortForBluetoothMac(string rawMac12)
        {
            if (string.IsNullOrWhiteSpace(rawMac12)) return "";
            string cleanMac = rawMac12.Replace(":", "").Replace("-", "").Trim().ToUpper();

            try
            {
                string bthPath = @"SYSTEM\CurrentControlSet\Enum\BTHENUM";
                using (var bthKey = Registry.LocalMachine.OpenSubKey(bthPath))
                {
                    if (bthKey != null)
                    {
                        foreach (var sub1 in bthKey.GetSubKeyNames())
                        {
                            using (var k1 = bthKey.OpenSubKey(sub1))
                            {
                                if (k1 == null) continue;
                                foreach (var sub2 in k1.GetSubKeyNames())
                                {
                                    if (sub2.IndexOf(cleanMac, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        using (var k2 = k1.OpenSubKey(sub2))
                                        using (var pKey = k2?.OpenSubKey("Device Parameters"))
                                        {
                                            if (pKey != null)
                                            {
                                                string p = (pKey.GetValue("PortName") as string)?.TrimEnd(':').ToUpper();
                                                if (!string.IsNullOrEmpty(p)) return p;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 미페어링된 블루투스 프린터를 윈도우에 자동 페어링하고, 가상 직렬(COM) 포트를 활성화하여 생성된 COM 포트명을 반환
        /// </summary>
        public static async Task<(bool success, string portName, string message)> PairAndSetupBluetoothPrinterAsync(string rawMacOrName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string cleanInput = (rawMacOrName ?? "")
                        .Replace("[블루투스]", "")
                        .Replace("(페어링 필요)", "")
                        .Replace("(가상포트 연결 필요)", "")
                        .Trim();

                    string cleanTargetKey = cleanInput.Replace(":", "").Replace("-", "").Replace(" ", "").Trim().ToUpper();

                    // 1. 라디오 스캔으로 장치 찾기
                    var sp = new BLUETOOTH_DEVICE_SEARCH_PARAMS();
                    sp.dwSize = Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_SEARCH_PARAMS));
                    sp.fReturnAuthenticated = true;
                    sp.fReturnRemembered = true;
                    sp.fReturnUnknown = true;
                    sp.fReturnConnected = true;
                    sp.fIssueInquiry = true;
                    sp.cTimeoutMultiplier = 3;
                    sp.hRadio = IntPtr.Zero;

                    var di = new BLUETOOTH_DEVICE_INFO();
                    di.dwSize = Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO));

                    IntPtr hFind = BluetoothFindFirstDevice(ref sp, ref di);
                    BLUETOOTH_DEVICE_INFO targetDi = new BLUETOOTH_DEVICE_INFO();
                    bool found = false;

                    if (hFind != IntPtr.Zero)
                    {
                        do
                        {
                            string curMac = di.Address.ToString("X12").ToUpper();
                            string curName = (di.szName ?? "").Trim();
                            string cleanCurName = curName.Replace(":", "").Replace("-", "").Replace(" ", "").Trim().ToUpper();

                            bool matchMac = !string.IsNullOrEmpty(curMac) && curMac.Equals(cleanTargetKey, StringComparison.OrdinalIgnoreCase);
                            bool matchName = false;

                            // 12자리 Hex MAC 주소인 경우 오직 MAC 주소 완전 일치로만 매칭 (이름 매칭 방지)
                            bool isTargetMac = cleanTargetKey.Length == 12 && Regex.IsMatch(cleanTargetKey, "^[0-9A-F]{12}$");
                            if (!isTargetMac)
                            {
                                // 이름 기반 매칭인 경우:
                                // 모델 번호(숫자 시퀀스) 불일치 방어 (예: 325 != 7941)
                                var curNums = Regex.Matches(curName, @"\d+").Cast<Match>().Select(m => m.Value).ToList();
                                var targetNums = Regex.Matches(cleanInput, @"\d+").Cast<Match>().Select(m => m.Value).ToList();
                                bool numConflict = curNums.Any() && targetNums.Any() && !curNums.Intersect(targetNums).Any();

                                if (!numConflict && !string.IsNullOrEmpty(cleanCurName) && !string.IsNullOrEmpty(cleanTargetKey))
                                {
                                    matchName = cleanCurName.Contains(cleanTargetKey) || cleanTargetKey.Contains(cleanCurName);
                                }
                            }

                            if (matchMac || matchName)
                            {
                                targetDi = di;
                                found = true;
                                break;
                            }
                            di = new BLUETOOTH_DEVICE_INFO();
                            di.dwSize = Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO));
                        } while (BluetoothFindNextDevice(hFind, ref di));
                        BluetoothFindDeviceClose(hFind);
                    }

                    if (!found)
                    {
                        return (false, "", $"주변에서 해당 블루투스 프린터('{cleanInput}')를 검색하지 못했습니다. 프린터 전원을 확인해주세요.");
                    }

                    // 2. 이미 인증되어 있지 않다면 페어링(Authenticate) 수행
                    if (!targetDi.fAuthenticated)
                    {
                        // 1차 0000 시도
                        uint authRes = BluetoothAuthenticateDevice(IntPtr.Zero, IntPtr.Zero, ref targetDi, "0000", 4);
                        if (authRes != 0)
                        {
                            // 2차 1234 시도
                            authRes = BluetoothAuthenticateDevice(IntPtr.Zero, IntPtr.Zero, ref targetDi, "1234", 4);
                        }

                        if (authRes != 0)
                        {
                            return (false, "", $"블루투스 페어링(인증)에 실패했습니다. (오류코드: {authRes})\nWindows [설정 > Bluetooth]에서 직접 페어링을 진행해주세요.");
                        }
                    }

                    // 3. 직렬 포트(SPP) 서비스 활성화
                    Guid sppGuid = new Guid("00001101-0000-1000-8000-00805F9B34FB");
                    BluetoothSetServiceState(IntPtr.Zero, ref targetDi, ref sppGuid, 1);

                    // 4. Windows PnP 가상 COM 포트 생성 대기 (최대 7초)
                    string matchedPort = "";
                    string devMac12 = targetDi.Address.ToString("X12").ToUpper();

                    var initialPorts = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.OrdinalIgnoreCase);

                    for (int wait = 0; wait < 14; wait++)
                    {
                        Thread.Sleep(500);

                        // 1) SERIALCOMM 또는 BTHENUM 레지스트리에서 해당 MAC 매핑 검색
                        matchedPort = FindComPortForBluetoothMac(devMac12);
                        if (!string.IsNullOrEmpty(matchedPort))
                        {
                            break;
                        }

                        // 2) 새로 추가된 COM 포트가 있는지 확인
                        var currentPorts = SerialPort.GetPortNames();
                        var newlyAdded = currentPorts.FirstOrDefault(p => !initialPorts.Contains(p));
                        if (!string.IsNullOrEmpty(newlyAdded))
                        {
                            matchedPort = newlyAdded.Trim().TrimEnd(':').ToUpper();
                            break;
                        }

                        // 3) 장치명으로 레지스트리/WMI 역조회
                        string nameMatched = GetBluetoothPortForDeviceName(targetDi.szName);
                        if (!string.IsNullOrEmpty(nameMatched))
                        {
                            matchedPort = nameMatched;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(matchedPort))
                    {
                        return (true, matchedPort, $"[{targetDi.szName}] 장치가 성공적으로 페어링되고 {matchedPort} 포트가 생성되었습니다.");
                    }
                    else
                    {
                        // ⚠️ 주의: 절대 기존 다른 장치의 포트(availablePorts.Last)를 반환하지 않음!
                        return (true, "", $"[{targetDi.szName}] 장치가 성공적으로 페어링되었습니다.\n\nWindows에서 가상 직렬(COM) 포트를 생성하는 데 시간이 걸릴 수 있습니다.\n잠시 후 [블루투스 장치 검색]을 눌러 포트를 갱신해주세요.");
                    }
                }
                catch (Exception ex)
                {
                    return (false, "", $"페어링 중 오류 발생: {ex.Message}");
                }
            });
        }
    }

    /*
    public static class FileLogger
    {
        private static readonly string LogDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly object _lock = new object();

        static FileLogger()
        {
            try
            {
                if (!System.IO.Directory.Exists(LogDirectory))
                {
                    System.IO.Directory.CreateDirectory(LogDirectory);
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string fileName = $"Log_{DateTime.Now:yyyyMMdd}.txt";
                    string filePath = System.IO.Path.Combine(LogDirectory, fileName);
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

                    System.IO.File.AppendAllText(filePath, logEntry, System.Text.Encoding.UTF8);
                    System.Diagnostics.Debug.WriteLine(logEntry.Trim());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 기록 실패: {ex.Message}");
            }
        }
    }
    */
}
