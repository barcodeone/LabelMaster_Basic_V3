using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    }

    public static class UsbPrinterHelper
    {
        // 지원되는 Xprinter 브랜드 및 모델 키워드 리스트 (일반 사무용 프린터 배제)
        public static readonly List<string> SupportedKeywords = new List<string>
        {
            "XPRINTER", "XP-", "XP", "423B", "T265B", "T4501", "TT428", "TT424", 
            "237B", "375B", "370B", "DT108B", "326B", "427B",
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

        public static List<string> GetAvailableComPorts()
        {
            try
            {
                return SerialPort.GetPortNames().OrderBy(p => p).ToList();
            }
            catch
            {
                return new List<string> { "COM1", "COM2", "COM3", "COM4", "COM5" };
            }
        }

        public static List<UsbPrinterInfo> GetConnectedUsbPrinters(string targetPrinterName = null, IEnumerable<string> targetSerials = null)
        {
            var printers = new List<UsbPrinterInfo>();
            Func<string, string> normalize = (s) => (s ?? "").ToUpperInvariant().Trim().Replace(" ", "");
            var normalizedTargetSerials = targetSerials != null
                ? new HashSet<string>(targetSerials.Select(normalize).Where(s => !string.IsNullOrEmpty(s)))
                : null;

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
                string usbPrintPath = @"SYSTEM\CurrentControlSet\Enum\USBPRINT";
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, PortName, DriverName, WorkOffline FROM Win32_Printer"))
                {
                    foreach (System.Management.ManagementObject obj in searcher.Get())
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

                        if (connType == "USB" && workOffline) continue;

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
                        // 2. TCP/IP (LAN) 연결 시리얼: 절대 9100 포트 소켓을 열지 않고 무출력 ARP MAC 주소만 안전하게 취득
                        else if (connType == "LAN")
                        {
                            var ipMatch = Regex.Match(port, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                            if (ipMatch.Success)
                            {
                                string ip = ipMatch.Value;
                                serial = GetMacAddressFromIp(ip);
                            }
                        }
                        // 3. Bluetooth 연결: 기본 레지스트리/포트 정보 확인
                        else if (connType == "Bluetooth")
                        {
                            var comMatch = Regex.Match(port, @"COM\d+", RegexOptions.IgnoreCase);
                            if (comMatch.Success)
                            {
                                serial = QueryTsplSerialViaCom(comMatch.Value, 9600, 600);
                            }
                        }

                        // 시리얼 번호 미확인 시 기본 레지스트리 검색 폴백
                        if (string.IsNullOrEmpty(serial))
                        {
                            serial = ExtractSerialFromPortOrRegistry(port);
                        }

                        // 인증 시리얼 필터링
                        bool isAllowed = true;
                        if (normalizedTargetSerials != null && normalizedTargetSerials.Count > 0)
                        {
                            string norm = normalize(serial);
                            isAllowed = !string.IsNullOrEmpty(norm) && normalizedTargetSerials.Contains(norm);
                        }

                        if (isAllowed)
                        {
                            if (!printers.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            {
                                printers.Add(new UsbPrinterInfo
                                {
                                    Name = name,
                                    FriendlyName = driver,
                                    PortName = port,
                                    Serial = !string.IsNullOrEmpty(serial) ? serial : "-",
                                    Type = connType,
                                    Address = port
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프린터 탐색 오류: {ex.Message}");
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
        /// 블루투스 가상 COM 포트로 비출력 안전 쿼리(OUT GETSETTING$)를 전송하여 시리얼 번호 획득
        /// </summary>
        public static string QueryTsplSerialViaCom(string comPortName, int baudRate = 9600, int timeoutMs = 1000)
        {
            if (string.IsNullOrEmpty(comPortName)) return null;

            try
            {
                using (var serialPort = new SerialPort(comPortName, baudRate))
                {
                    serialPort.ReadTimeout = timeoutMs;
                    serialPort.WriteTimeout = timeoutMs;
                    serialPort.Open();

                    // 인쇄를 유발하지 않는 TSPL 공식 OUT 질의문 전송
                    serialPort.Write(SafeTsplInfoQuery);
                    System.Threading.Thread.Sleep(200);

                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    if (serialPort.BytesToRead > 0)
                    {
                        bytesRead = serialPort.Read(buffer, 0, Math.Min(buffer.Length, serialPort.BytesToRead));
                    }

                    if (bytesRead > 0)
                    {
                        string raw = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        return ParseSerialFromTsplResponse(raw);
                    }
                }
            }
            catch { }
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

        private static string ExtractSerialFromPortOrRegistry(string port)
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
                        using (var searcher = new ManagementObjectSearcher("SELECT Name, PortName, DriverName FROM Win32_Printer"))
                        {
                            foreach (ManagementObject p in searcher.Get())
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
        /// 시스템에 등록된 블루투스 가상 COM 포트 및 페어링된 블루투스 프린터를 정밀 스캔하여 실제 장치 이름(FriendlyName)을 매핑 후 UsbPrinterInfo 리스트로 반환
        /// </summary>
        public static async Task<List<UsbPrinterInfo>> ScanBluetoothPrintersAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<UsbPrinterInfo>();
                try
                {
                    var portModelMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var portMacMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var bluetoothPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var macToDeviceNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // 1. WMI 및 레지스트리에서 페어링된 모든 블루투스 장치(MAC 주소 ➔ 실제 장치 이름) 마스터 사전 구축
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, Caption, Description, DeviceID, PNPDeviceID FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%BTH%' OR ClassGuid = '{e0cbf06c-cdb3-4647-bb8a-263b43f0f974}'"))
                        {
                            foreach (System.Management.ManagementObject obj in searcher.Get())
                            {
                                string name = obj["Name"]?.ToString() ?? obj["Caption"]?.ToString() ?? "";
                                string pnpId = obj["PNPDeviceID"]?.ToString() ?? "";

                                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pnpId)) continue;

                                // 시스템/스택 기본 장치명 제외
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

                                // 12자리 Hex MAC 주소 추출 (예: DEV_DC0D30D66320, 908C43DFF2C9)
                                var macMatch = Regex.Match(pnpId, @"DEV_([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                if (!macMatch.Success)
                                {
                                    macMatch = Regex.Match(pnpId, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                }

                                if (macMatch.Success)
                                {
                                    string rawMac = macMatch.Groups[1].Value.ToUpper();
                                    if (!rawMac.StartsWith("000000000000"))
                                    {
                                        macToDeviceNameMap[rawMac] = name.Trim();
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // 레지스트리 BTHPORT\Parameters\Devices 에서 추가 블루투스 장치명 수집
                    try
                    {
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

                    // 2. WMI Win32_Printer에서 이미 설치된 블루투스 프린터 매핑
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, PortName, DriverName FROM Win32_Printer"))
                        {
                            foreach (System.Management.ManagementObject obj in searcher.Get())
                            {
                                string name = obj["Name"]?.ToString() ?? "";
                                string port = obj["PortName"]?.ToString() ?? "";
                                string driver = obj["DriverName"]?.ToString() ?? "";

                                var match = Regex.Match(port, @"COM\d+", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    string pKey = match.Value.ToUpper();
                                    string identifiedModel = !string.IsNullOrEmpty(name) ? name : driver;
                                    portModelMap[pKey] = identifiedModel;
                                    bluetoothPorts.Add(pKey);
                                }
                            }
                        }
                    }
                    catch { }

                    // 3. WMI Win32_PnPEntity에서 직렬 포트 및 블루투스 링크 매핑
                    try
                    {
                        using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Name, Caption, DeviceID, PNPDeviceID FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e978-e325-11ce-bfc1-08002be10318}' OR PNPDeviceID LIKE 'BTHENUM%' OR Name LIKE '%(COM%'"))
                        {
                            foreach (System.Management.ManagementObject obj in searcher.Get())
                            {
                                string pnpName = obj["Name"]?.ToString() ?? obj["Caption"]?.ToString() ?? "";
                                string pnpId = obj["PNPDeviceID"]?.ToString() ?? "";

                                var match = Regex.Match(pnpName, @"(COM\d+)", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    string pKey = match.Groups[1].Value.ToUpper();

                                    // 블루투스 포트 여부 확인
                                    bool isBthPort = pnpId.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     pnpName.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0;

                                    if (isBthPort)
                                    {
                                        bluetoothPorts.Add(pKey);

                                        // PNPDeviceID에서 MAC 주소 추출 (예: &DC0D30D66320_C00000000)
                                        var macMatch = Regex.Match(pnpId, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                        if (macMatch.Success)
                                        {
                                            string rawMac = macMatch.Groups[1].Value.ToUpper();
                                            if (!rawMac.StartsWith("000000000000"))
                                            {
                                                portMacMap[pKey] = string.Join("-", Enumerable.Range(0, 6).Select(i => rawMac.Substring(i * 2, 2)));

                                                if (macToDeviceNameMap.TryGetValue(rawMac, out string matchedName))
                                                {
                                                    portModelMap[pKey] = matchedName;
                                                }
                                            }
                                        }

                                        // 아직 이름이 없으면 PnP 이름에서 정제
                                        if (!portModelMap.ContainsKey(pKey))
                                        {
                                            string cleanName = Regex.Replace(pnpName, @"\s*\(COM\d+\)", "").Trim();
                                            if (!cleanName.StartsWith("표준 Bluetooth", StringComparison.OrdinalIgnoreCase) &&
                                                !cleanName.StartsWith("Bluetooth Serial", StringComparison.OrdinalIgnoreCase))
                                            {
                                                portModelMap[pKey] = cleanName;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    // 4. 레지스트리 BTHENUM 키에서 PortName 직접 매핑
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
                                            using (var k2 = k1.OpenSubKey(sub2))
                                            {
                                                if (k2 == null) continue;
                                                string friendly = k2.GetValue("FriendlyName") as string;
                                                string devDesc = k2.GetValue("DeviceDesc") as string;
                                                string targetName = !string.IsNullOrEmpty(friendly) ? friendly : devDesc;

                                                using (var pKey = k2.OpenSubKey("Device Parameters"))
                                                {
                                                    if (pKey != null)
                                                    {
                                                        string port = pKey.GetValue("PortName") as string;
                                                        if (!string.IsNullOrEmpty(port))
                                                        {
                                                            string portClean = port.ToUpper().TrimEnd(':');
                                                            bluetoothPorts.Add(portClean);

                                                            // 서브키 이름에서 MAC 주소 확인
                                                            var macMatch = Regex.Match(sub2, @"([0-9A-Fa-f]{12})", RegexOptions.IgnoreCase);
                                                            if (macMatch.Success)
                                                            {
                                                                string rawMac = macMatch.Groups[1].Value.ToUpper();
                                                                if (!rawMac.StartsWith("000000000000") && macToDeviceNameMap.TryGetValue(rawMac, out string mName))
                                                                {
                                                                    portModelMap[portClean] = mName;
                                                                }
                                                            }

                                                            if (!portModelMap.ContainsKey(portClean) && !string.IsNullOrEmpty(targetName))
                                                            {
                                                                portModelMap[portClean] = targetName;
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

                    // 5. 시스템의 직렬 포트 열거 및 블루투스 장치 목록 생성
                    var allPorts = SerialPort.GetPortNames().OrderBy(p => p).ToList();

                    // 블루투스 포트로 확인된 것들을 우선 추가
                    var targetPorts = allPorts.Where(p => bluetoothPorts.Contains(p.ToUpper())).ToList();
                    
                    // 만약 명시적 블루투스 포트가 없으면 모든 COM 포트 fallback
                    if (targetPorts.Count == 0)
                    {
                        targetPorts = allPorts;
                    }

                    // 정렬: 장치 이름이 명확히 확인된 포트 우선
                    var sortedPorts = targetPorts.OrderByDescending(p => portModelMap.ContainsKey(p.ToUpper()))
                                                 .ThenBy(p => p).ToList();

                    foreach (var port in sortedPorts)
                    {
                        string pUpper = port.ToUpper();
                        string modelName = portModelMap.TryGetValue(pUpper, out string m) ? m : null;
                        string mac = portMacMap.TryGetValue(pUpper, out string mc) ? mc : "-";

                        string displayName;
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            displayName = $"[블루투스] {modelName} ({port})";
                        }
                        else
                        {
                            displayName = $"[블루투스] 블루투스 장치 ({port})";
                        }

                        list.Add(new UsbPrinterInfo
                        {
                            Name = displayName,
                            FriendlyName = modelName ?? "Bluetooth Printer",
                            PortName = port,
                            Serial = mac,
                            Type = "Bluetooth",
                            Address = port
                        });
                    }
                }
                catch { }
                return list;
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
