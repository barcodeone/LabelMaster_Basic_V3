using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public class PrinterService : IPrinterService
    {
        private static readonly Guid UsbPrintGuid = new Guid("28D78FAD-5A12-11D1-AE5B-0000F803A8C2");

        #region Win32 API Imports for USB Direct Communication
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr h, uint f);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr h, IntPtr e, ref Guid g, uint i, ref SP_DEVICE_INTERFACE_DATA d);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr h, ref SP_DEVICE_INTERFACE_DATA d, IntPtr detail, uint size, ref uint req, ref SP_DEVINFO_DATA devInfo);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr h, ref SP_DEVICE_INTERFACE_DATA d, IntPtr detail, uint size, ref uint req, IntPtr devInfo);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr h);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }
        #endregion

        #region Win32 Spooler API Imports for Status
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_DEFAULTS
        {
            public IntPtr pDatatype;
            public IntPtr pDevMode;
            public uint DesiredAccess;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, ref PRINTER_DEFAULTS pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetPrinter(IntPtr hPrinter, int Level, IntPtr pPrinter, int cbBuf, ref int pcbNeeded);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }
        #endregion

        public List<UsbPrinterInfo> GetVerifiedPrinters(List<string> authenticatedSerials)
        {
            try
            {
                return UsbPrinterHelper.GetConnectedUsbPrinters(null, authenticatedSerials) ?? new List<UsbPrinterInfo>();
            }
            catch (Exception ex)
            {
                PrinterSerialLogger.LogError("프린터 목록 조회 실패", ex);
                return new List<UsbPrinterInfo>();
            }
        }

        public string GetEffectivePrinterPort(UsbPrinterInfo printer)
        {
            if (printer == null) return "USB001";
            return !string.IsNullOrEmpty(printer.PortName) ? printer.PortName : "USB001";
        }

        /// <summary>
        /// New_xPix WindowsPrinterDriverService.GetDriverStatus 와 100% 동일한 검증된 스풀러 상태 감지
        /// </summary>
        public void CheckRealtimePrinterStatus(string printerName, List<UsbPrinterInfo> cachedPrinters, out string badgeText, out string badgeColor)
        {
            if (string.IsNullOrEmpty(printerName))
            {
                badgeText = "🔴 프린터 미선택";
                badgeColor = "#D32F2F";
                return;
            }

            // 1. New_xPix TSPL 실시간 통신 (하드웨어 칩셋 1:1 다이렉트 질의 - 헤드 열림, 용지 없음, 용지 걸림 정밀 감지)
            try
            {
                var info = cachedPrinters?.FirstOrDefault(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
                string portName = GetEffectivePrinterPort(info);
                string connType = info?.Type ?? (portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase) ? "USB" : 
                                                (portName.ToUpper().Contains("COM") || portName.ToUpper().Contains("BTH") ? "Bluetooth" : "LAN"));

                string tsplStatus = QueryTsplLiveStatus(portName, connType);
                if (!string.IsNullOrEmpty(tsplStatus))
                {
                    if (tsplStatus.Contains("정상 준비") || tsplStatus.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                    {
                        badgeText = "🟢 정상 준비 (Ready)";
                        badgeColor = "#22C55E";
                    }
                    else
                    {
                        badgeText = "🔴 " + tsplStatus;
                        badgeColor = "#DC2626";
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TSPL 실시간 상태 조회 예외: {ex.Message}");
            }

            // 2. 폴백: Windows Spooler API 기반 상태 확인
            IntPtr hPrinter = IntPtr.Zero;
            PRINTER_DEFAULTS defaults = new PRINTER_DEFAULTS { DesiredAccess = 0x00000008 /* PRINTER_ACCESS_USE */ };

            try
            {
                if (!OpenPrinter(printerName, out hPrinter, ref defaults))
                {
                    badgeText = "🔴 오프라인 (장치 없음)";
                    badgeColor = "#D32F2F";
                    return;
                }

                int pcbNeeded = 0;
                GetPrinter(hPrinter, 2, IntPtr.Zero, 0, ref pcbNeeded);
                if (pcbNeeded <= 0)
                {
                    badgeText = "🟢 정상 준비 (Ready)";
                    badgeColor = "#22C55E";
                    return;
                }

                IntPtr pPrinter = Marshal.AllocHGlobal(pcbNeeded);
                try
                {
                    if (GetPrinter(hPrinter, 2, pPrinter, pcbNeeded, ref pcbNeeded))
                    {
                        var info = Marshal.PtrToStructure<PRINTER_INFO_2>(pPrinter);
                        uint status = info.Status;
                        bool isWorkOffline = (info.Attributes & 0x00000400) != 0;

                        if (isWorkOffline)
                        {
                            badgeText = "🔴 오프라인 (WorkOffline)";
                            badgeColor = "#D32F2F";
                            return;
                        }

                        if (status == 0)
                        {
                            badgeText = "🟢 정상 준비 (Ready)";
                            badgeColor = "#22C55E";
                            return;
                        }

                        List<string> statusList = new List<string>();
                        if ((status & 0x00000001) != 0) statusList.Add("일시 중지됨");
                        if ((status & 0x00000002) != 0) statusList.Add("오류");
                        if ((status & 0x00000008) != 0) statusList.Add("용지 걸림");
                        if ((status & 0x00000010) != 0) statusList.Add("용지 없음");
                        if ((status & 0x00000080) != 0) statusList.Add("오프라인");
                        if ((status & 0x00000200) != 0) statusList.Add("바쁨 (Busy)");
                        if ((status & 0x00000400) != 0) statusList.Add("인쇄 중");
                        if ((status & 0x00100000) != 0) statusList.Add("사용자 조치 필요");
                        if ((status & 0x00400000) != 0) statusList.Add("커버 열림");

                        if (statusList.Count > 0)
                        {
                            badgeText = ((status & 0x00000010) != 0 || (status & 0x00000080) != 0 ? "🔴 " : "🟡 ") + string.Join(", ", statusList);
                            badgeColor = ((status & 0x00000010) != 0 || (status & 0x00000080) != 0) ? "#EF4444" : "#F59E0B";
                        }
                        else
                        {
                            badgeText = "🟢 정상 준비 (Ready)";
                            badgeColor = "#22C55E";
                        }
                    }
                    else
                    {
                        badgeText = "🟢 정상 준비 (Ready)";
                        badgeColor = "#22C55E";
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pPrinter);
                }
            }
            catch
            {
                badgeText = "🟢 정상 준비 (Ready)";
                badgeColor = "#22C55E";
            }
            finally
            {
                if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter);
            }
        }

        public bool SendLabelPrintBytes(string printerName, byte[] tsplBytes, List<UsbPrinterInfo> cachedPrinters, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                // 1차 시도: Windows Print Spooler RAW 전송
                bool spoolerSuccess = PrinterHelper.SendBytesToPrinter(printerName, tsplBytes);
                if (spoolerSuccess)
                {
                    return true;
                }

                // 2차 시도: TSPL SDK 직렬 통신 폴백
                var info = cachedPrinters?.FirstOrDefault(p => p.Name == printerName);
                string portName = GetEffectivePrinterPort(info);

                IntPtr pHandle = TSPLPrint.InitPrinter(printerName);
                if (pHandle != IntPtr.Zero)
                {
                    try
                    {
                        TSPLPrint.OpenPort(pHandle, portName);
                        int writeRes = TSPLPrint.WriteData(pHandle, tsplBytes, tsplBytes.Length);
                        TSPLPrint.ClosePort(pHandle);

                        if (writeRes > 0)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        TSPLPrint.ReleasePrinter(pHandle);
                    }
                }

                errorMessage = "스풀러 및 TSPL SDK 포트 전송 모두 실패했습니다.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                PrinterSerialLogger.LogError("라벨 바이트 인쇄 오류", ex);
                return false;
            }
        }

        public bool SendTsplRawCommand(string printerName, string command, List<UsbPrinterInfo> cachedPrinters, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                byte[] cmdBytes = System.Text.Encoding.Default.GetBytes(command);
                return SendLabelPrintBytes(printerName, cmdBytes, cachedPrinters, out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// New_xPix TSPL 양방향 통신 기반 실시간 하드웨어 스펙 및 상세 정보 조회 (xPix 기준 100% 동일화)
        /// </summary>
        public Dictionary<string, string> GetPrinterHardwareDetails(string printerName, List<UsbPrinterInfo> cachedPrinters)
        {
            var rawDetails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(printerName))
            {
                var empty = new Dictionary<string, string>();
                empty["상태"] = "선택된 프린터 없음";
                return empty;
            }

            var info = cachedPrinters?.FirstOrDefault(p => p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));
            string portName = GetEffectivePrinterPort(info);
            string registeredSerial = info?.Serial ?? "-";
            string connType = info?.Type ?? (portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase) ? "USB" : 
                                            (portName.ToUpper().Contains("COM") || portName.ToUpper().Contains("BTH") ? "Bluetooth" : "LAN"));

            // 1. New_xPix 방식: USB/LAN/BT Direct 통신으로 TSPL GETSETTING$ 통합 쿼리 전송 및 실시간 응답 수신
            try
            {
                string rawTsplResponse = null;
                if (connType == "LAN")
                {
                    rawTsplResponse = QueryTsplHardwareInfoLan(portName);
                }
                else if (connType == "Bluetooth")
                {
                    rawTsplResponse = QueryTsplHardwareInfoBluetooth(portName);
                }
                else
                {
                    rawTsplResponse = QueryTsplHardwareInfoDirect(portName);
                }

                if (!string.IsNullOrEmpty(rawTsplResponse))
                {
                    ParseTsplResponse(rawTsplResponse, rawDetails);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TSPL Direct 질의 오류: {ex.Message}");
            }

            // 하드웨어 쿼리 후 USB 엔드포인트 안정화 대기
            Thread.Sleep(150);

            // 2. 실시간 상태: New_xPix TSPL 실시간 통신 기반 정밀 하드웨어 상태 분석
            string liveStatus = null;
            try
            {
                liveStatus = QueryTsplLiveStatus(portName, connType);
                if (string.IsNullOrEmpty(liveStatus))
                {
                    Thread.Sleep(300);
                    liveStatus = QueryTsplLiveStatus(portName, connType);
                }
            }
            catch { }

            if (string.IsNullOrEmpty(liveStatus))
            {
                try
                {
                    IntPtr hPrinter = IntPtr.Zero;
                    PRINTER_DEFAULTS defaults = new PRINTER_DEFAULTS { DesiredAccess = 0x00000008 };
                    if (OpenPrinter(printerName, out hPrinter, ref defaults))
                    {
                        try
                        {
                            int pcbNeeded = 0;
                            GetPrinter(hPrinter, 2, IntPtr.Zero, 0, ref pcbNeeded);
                            if (pcbNeeded > 0)
                            {
                                IntPtr pPrinter = Marshal.AllocHGlobal(pcbNeeded);
                                try
                                {
                                    if (GetPrinter(hPrinter, 2, pPrinter, pcbNeeded, ref pcbNeeded))
                                    {
                                        var pInfo = Marshal.PtrToStructure<PRINTER_INFO_2>(pPrinter);
                                        uint status = pInfo.Status;
                                        bool isWorkOffline = (pInfo.Attributes & 0x00000400) != 0;

                                        if (isWorkOffline) liveStatus = "오프라인 (WorkOffline)";
                                        else if (status == 0) liveStatus = "정상 준비 (Ready)";
                                        else
                                        {
                                            List<string> statusList = new List<string>();
                                            if ((status & 0x00000001) != 0) statusList.Add("일시 중지됨");
                                            if ((status & 0x00000002) != 0) statusList.Add("오류");
                                            if ((status & 0x00000008) != 0) statusList.Add("용지 걸림");
                                            if ((status & 0x00000010) != 0) statusList.Add("용지 없음");
                                            if ((status & 0x00000080) != 0) statusList.Add("오프라인");
                                            if ((status & 0x00000200) != 0) statusList.Add("바쁨 (Busy)");
                                            if ((status & 0x00000400) != 0) statusList.Add("인쇄 중");
                                            if ((status & 0x00100000) != 0) statusList.Add("사용자 조치 필요");
                                            if ((status & 0x00400000) != 0) statusList.Add("커버 열림");
                                            liveStatus = statusList.Count > 0 ? string.Join(", ", statusList) : "정상 준비 (Ready)";
                                        }
                                    }
                                }
                                finally
                                {
                                    Marshal.FreeHGlobal(pPrinter);
                                }
                            }
                        }
                        finally
                        {
                            ClosePrinter(hPrinter);
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(liveStatus)) liveStatus = "정상 준비 (Ready)";

            // 3. New_xPix 기준 정렬된 결과 Dictionary 생성
            var details = new Dictionary<string, string>();

            // 해상도
            if (rawDetails.TryGetValue("DPI", out string dpiVal)) details["해상도"] = dpiVal;
            else details["해상도"] = "203";

            // 모델명
            if (rawDetails.TryGetValue("MDL", out string mdlVal)) details["모델명"] = mdlVal;
            else details["모델명"] = printerName;

            // 시리얼
            if (rawDetails.TryGetValue("SER", out string serVal) && !string.IsNullOrWhiteSpace(serVal) && serVal != "-") details["시리얼"] = serVal;
            else details["시리얼"] = registeredSerial;

            // 펌웨어
            if (rawDetails.TryGetValue("VER", out string verVal)) details["펌웨어"] = verVal;
            else details["펌웨어"] = "-";

            // 누적거리
            if (rawDetails.TryGetValue("MIL", out string milVal)) details["누적거리"] = milVal;
            else details["누적거리"] = "-";

            // 센서타입
            if (rawDetails.TryGetValue("SEN_T", out string senVal)) details["센서타입"] = senVal;
            else details["센서타입"] = "GAP";

            // 용지폭
            if (rawDetails.TryGetValue("W", out string wVal)) details["용지폭"] = wVal;
            else details["용지폭"] = "-";

            // 용지길이
            if (rawDetails.TryGetValue("H", out string hVal)) details["용지길이"] = hVal;
            else details["용지길이"] = "-";

            // 간격(Gap)
            if (rawDetails.TryGetValue("GAP", out string gapVal)) details["간격(Gap)"] = gapVal;
            else details["간격(Gap)"] = "-";

            // 표시(Bline)
            if (rawDetails.TryGetValue("BLN", out string blnVal)) details["표시(Bline)"] = blnVal;
            else details["표시(Bline)"] = "-";

            // 리본사용
            if (rawDetails.TryGetValue("RIB", out string ribVal)) details["리본사용"] = ribVal;
            else details["리본사용"] = "OFF";

            // 커터사용
            if (rawDetails.TryGetValue("CUT", out string cutVal)) details["커터사용"] = cutVal;
            else details["커터사용"] = "OFF";

            // 인쇄후작업
            if (rawDetails.TryGetValue("PMODE", out string pmodeVal)) details["인쇄후작업"] = pmodeVal;
            else details["인쇄후작업"] = "TEAR OFF";

            // 인쇄속도
            if (rawDetails.TryGetValue("SPD", out string spdVal)) details["인쇄속도"] = spdVal;
            else details["인쇄속도"] = "4";

            // 인쇄농도
            if (rawDetails.TryGetValue("DEN", out string denVal)) details["인쇄농도"] = denVal;
            else details["인쇄농도"] = "8";

            // 갭 감도(Gap Intensity)
            if (rawDetails.TryGetValue("GAP_INT", out string gapIntVal)) details["갭 감도"] = gapIntVal;

            // 블랙마크 감도(Bline Intensity)
            if (rawDetails.TryGetValue("BLN_INT", out string blnIntVal)) details["블랙마크 감도"] = blnIntVal;

            // 용지 공급 오프셋(Offset)
            if (rawDetails.TryGetValue("OFF", out string offVal)) details["용지 공급 오프셋"] = offVal;

            // 인쇄 기준점(Reference Point)
            if (rawDetails.TryGetValue("REFX", out string refX) && rawDetails.TryGetValue("REFY", out string refY))
                details["인쇄 기준점"] = $"X: {refX}, Y: {refY}";

            // 네트워크(LAN) 및 블루투스 장치 IP / MAC 주소 표시
            if (connType == "LAN")
            {
                var match = Regex.Match(portName, @"(\d{1,3}\.){3}\d{1,3}");
                string ip = match.Success ? match.Value : (!string.IsNullOrEmpty(info?.Address) ? info.Address : portName);
                details["IP 주소"] = ip;
                string mac = UsbPrinterHelper.GetMacAddressFromIp(ip);
                if (mac != "-") details["MAC 주소"] = mac;
                else if (!string.IsNullOrEmpty(registeredSerial) && registeredSerial.Contains(":")) details["MAC 주소"] = registeredSerial;
            }
            else if (connType == "Bluetooth")
            {
                details["COM 포트"] = portName;
                if (!string.IsNullOrEmpty(registeredSerial) && (registeredSerial.Contains(":") || registeredSerial.Contains("-")))
                {
                    details["MAC 주소"] = registeredSerial;
                }
            }

            // 연결 및 실시간 상태
            details["연결 포트"] = portName;
            details["연결 방식"] = connType;
            details["실시간 상태"] = liveStatus;

            return details;
        }

        private static readonly Dictionary<string, string> _portToPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _pathResolveLock = new object();

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        private static extern uint CM_Get_Child(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        private static extern uint CM_Get_Sibling(out uint pdnDevInst, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint CM_Get_Device_ID(uint dnDevInst, StringBuilder Buffer, int BufferLen, uint ulFlags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint devInst;
            public IntPtr Reserved;
        }

        private string ResolveUsbPath(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;
            portName = portName.Trim().ToUpper();

            lock (_pathResolveLock)
            {
                if (_portToPathMap.TryGetValue(portName, out string cachedPath) && !string.IsNullOrEmpty(cachedPath))
                {
                    return cachedPath;
                }
            }

            // 1. 레지스트리에서 포트 이름과 매칭되는 고유 하드웨어 인스턴스 ID 찾기
            string targetInstanceId = FindInstanceIdByPortName(portName);

            Guid classGuid = UsbPrintGuid;
            IntPtr deviceInfoSet = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, 0x12);
            if (deviceInfoSet == (IntPtr)(-1)) return null;

            IntPtr detailDataBuffer = IntPtr.Zero;
            uint deviceIndex = 0;
            try
            {
                SP_DEVICE_INTERFACE_DATA ifaceData = new SP_DEVICE_INTERFACE_DATA();
                ifaceData.cbSize = Marshal.SizeOf(ifaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref classGuid, deviceIndex++, ref ifaceData))
                {
                    uint needed = 0;
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref ifaceData, IntPtr.Zero, 0, ref needed, IntPtr.Zero);
                    if (needed == 0) continue;

                    detailDataBuffer = Marshal.AllocHGlobal((int)needed);
                    Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 8) ? 8 : 6);

                    SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                    devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                    if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref ifaceData, detailDataBuffer, needed, ref needed, ref devInfoData))
                    {
                        IntPtr pPath = new IntPtr(detailDataBuffer.ToInt64() + 4);
                        string path = Marshal.PtrToStringAuto(pPath);
                        if (string.IsNullOrEmpty(path)) continue;

                        string deviceInstanceId = "";
                        StringBuilder sbId = new StringBuilder(260);
                        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref devInfoData, sbId, sbId.Capacity, out _))
                        {
                            deviceInstanceId = sbId.ToString().ToUpper();
                        }

                        // 본체의 ParentIdPrefix 가져오기
                        string devicePrefix = GetDeviceParentPrefix(deviceInstanceId);

                        if (!string.IsNullOrEmpty(targetInstanceId))
                        {
                            string targetUpper = targetInstanceId.ToUpper();
                            bool idMatch = targetUpper.Contains(deviceInstanceId);
                            bool prefixMatch = !string.IsNullOrEmpty(devicePrefix) && targetUpper.Contains(devicePrefix.ToUpper());

                            if (idMatch || prefixMatch)
                            {
                                lock (_pathResolveLock)
                                {
                                    _portToPathMap[portName] = path;
                                }
                                return path;
                            }
                        }

                        // 2순위: 자식 노드 직접 탐색
                        uint childInst;
                        if (CM_Get_Child(out childInst, devInfoData.devInst, 0) == 0)
                        {
                            do
                            {
                                StringBuilder childIdSb = new StringBuilder(260);
                                if (CM_Get_Device_ID(childInst, childIdSb, childIdSb.Capacity, 0) == 0)
                                {
                                    string childId = childIdSb.ToString();
                                    string childPort = GetPortNameFromInstanceId(childId);

                                    if (string.IsNullOrEmpty(childPort))
                                    {
                                        var portInId = Regex.Match(childId, @"(USB\d+)$", RegexOptions.IgnoreCase);
                                        if (portInId.Success) childPort = portInId.Value.ToUpper();
                                    }

                                    if (!string.IsNullOrEmpty(childPort) && childPort.Equals(portName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        lock (_pathResolveLock)
                                        {
                                            _portToPathMap[portName] = path;
                                        }
                                        return path;
                                    }
                                }
                            } while (CM_Get_Sibling(out childInst, childInst, 0) == 0);
                        }

                        // 자식 노드가 없으면 첫 번째 유효 경로 캐시
                        if (!_portToPathMap.ContainsKey(portName))
                        {
                            lock (_pathResolveLock)
                            {
                                _portToPathMap[portName] = path;
                            }
                            return path;
                        }
                    }
                    Marshal.FreeHGlobal(detailDataBuffer);
                    detailDataBuffer = IntPtr.Zero;
                }
            }
            finally
            {
                if (detailDataBuffer != IntPtr.Zero) Marshal.FreeHGlobal(detailDataBuffer);
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            // 폴백: 첫 번째 탐색 경로 사용
            var allPaths = FindUsbPrinterPaths();
            return allPaths.Count > 0 ? allPaths[0] : null;
        }

        private string GetDeviceParentPrefix(string instanceId)
        {
            try
            {
                string[] parts = instanceId.Split('\\');
                if (parts.Length < 3) return null;
                string regPath = $@"SYSTEM\CurrentControlSet\Enum\{parts[0]}\{parts[1]}\{parts[2]}";
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath))
                {
                    return key?.GetValue("ParentIdPrefix") as string;
                }
            }
            catch { return null; }
        }

        private string GetPortNameFromInstanceId(string instanceId)
        {
            try
            {
                string[] parts = instanceId.Split('\\');
                if (parts.Length < 2) return null;
                string path = $@"SYSTEM\CurrentControlSet\Enum\USBPRINT\{parts[0]}\{parts[1]}\Device Parameters";
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                {
                    return key?.GetValue("PortName") as string;
                }
            }
            catch { return null; }
        }

        private string FindInstanceIdByPortName(string portName)
        {
            try
            {
                using (var usbPrintKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBPRINT"))
                {
                    if (usbPrintKey == null) return null;
                    foreach (var hardwareId in usbPrintKey.GetSubKeyNames())
                    {
                        using (var hardwareKey = usbPrintKey.OpenSubKey(hardwareId))
                        {
                            if (hardwareKey == null) continue;
                            foreach (var instanceId in hardwareKey.GetSubKeyNames())
                            {
                                using (var instanceKey = hardwareKey.OpenSubKey(instanceId + @"\Device Parameters"))
                                {
                                    if (instanceKey == null) continue;
                                    var pName = instanceKey.GetValue("PortName") as string;
                                    if (!string.IsNullOrEmpty(pName) && pName.Equals(portName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return $"{hardwareId}\\{instanceId}";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private List<string> FindUsbPrinterPaths()
        {
            var paths = new List<string>();
            Guid classGuid = UsbPrintGuid;
            IntPtr devInfoSet = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, 0x12);
            if (devInfoSet == (IntPtr)(-1)) return paths;

            try
            {
                SP_DEVICE_INTERFACE_DATA ifaceData = new SP_DEVICE_INTERFACE_DATA();
                ifaceData.cbSize = Marshal.SizeOf(ifaceData);
                uint index = 0;

                while (SetupDiEnumDeviceInterfaces(devInfoSet, IntPtr.Zero, ref classGuid, index++, ref ifaceData))
                {
                    uint needed = 0;
                    SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifaceData, IntPtr.Zero, 0, ref needed, IntPtr.Zero);
                    if (needed == 0) continue;

                    IntPtr detailBuffer = Marshal.AllocHGlobal((int)needed);
                    try
                    {
                        Marshal.WriteInt32(detailBuffer, (IntPtr.Size == 8) ? 8 : 6);
                        if (SetupDiGetDeviceInterfaceDetail(devInfoSet, ref ifaceData, detailBuffer, needed, ref needed, IntPtr.Zero))
                        {
                            IntPtr pPath = new IntPtr(detailBuffer.ToInt64() + 4);
                            string path = Marshal.PtrToStringAuto(pPath);
                            if (!string.IsNullOrEmpty(path))
                            {
                                paths.Add(path);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailBuffer);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devInfoSet);
            }

            return paths;
        }

        private string QueryTsplHardwareInfoDirect(string portName)
        {
            string usbPath = ResolveUsbPath(portName);
            if (string.IsNullOrEmpty(usbPath)) return null;

            // New_xPix 정밀 표준 TSPL 쿼리 세트 (센서 감도 및 오프셋/기준점 포함 21개 전체 항목 통합 질의)
            string queryCmd = 
                "OUT \"DPI=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"DPI\");" +
                "\"|MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");" +
                "\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");" +
                "\"|VER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"VERSION\");" +
                "\"|MIL=\";GETSETTING$(\"SYSTEM\",\"RECORD\",\"MILAGE\");" +
                "\"|SEN_T=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"SENSOR TYPE\");" +
                "\"|W=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER WIDTH\");" +
                "\"|H=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER SIZE\");" +
                "\"|GAP=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"GAP SIZE\");" +
                "\"|BLN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"BLINE SIZE\");" +
                "\"|RIB=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"RIBBON\");" +
                "\"|CUT=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"CUTTER\");" +
                "\"|PMODE=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PRINT MODE\");" +
                "\"|SPD=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"SPEED\");" +
                "\"|DEN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"DENSITY\");" +
                "\"|GAP_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"GAP INTENSION\");" +
                "\"|BLN_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"BLINE INTENSION\");" +
                "\"|OFF=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"OFFSET\");" +
                "\"|REFX=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE X\");" +
                "\"|REFY=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE Y\");\"|\"\r\n";

            byte[] cmdBytes = Encoding.ASCII.GetBytes(queryCmd);

            // 1차 시도
            string response = SendAndReceiveUsbDirect(usbPath, cmdBytes);
            if (!string.IsNullOrEmpty(response) && (response.Contains("DPI=") || response.Contains("MDL=") || response.Contains("|")))
            {
                return response;
            }

            // 2차 재시도 (New_xPix와 동일하게 500ms 대기 후 재시도)
            Thread.Sleep(500);
            return SendAndReceiveUsbDirect(usbPath, cmdBytes);
        }

        private string SendAndReceiveUsbDirect(string usbPath, byte[] cmdBytes)
        {
            try
            {
                IntPtr handle = CreateFile(usbPath, 0xC0000000, 0x07, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                if (handle == new IntPtr(-1) || handle == IntPtr.Zero) return null;

                using (var safeHandle = new SafeFileHandle(handle, true))
                using (var fs = new FileStream(safeHandle, FileAccess.ReadWrite, 4096, true))
                {
                    fs.Write(cmdBytes, 0, cmdBytes.Length);
                    fs.Flush();

                    Thread.Sleep(150);

                    List<byte> accumulated = new List<byte>();
                    byte[] buf = new byte[4096];
                    DateTime startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalMilliseconds < 2500)
                    {
                        var readTask = fs.ReadAsync(buf, 0, buf.Length);
                        if (readTask.Wait(300))
                        {
                            int r = readTask.Result;
                            if (r > 0)
                            {
                                byte[] slice = new byte[r];
                                Array.Copy(buf, 0, slice, 0, r);
                                accumulated.AddRange(slice);

                                if (accumulated.Count >= 10)
                                {
                                    byte lastByte = accumulated[accumulated.Count - 1];
                                    if (lastByte == 0x0A || lastByte == (byte)'|' || lastByte == 0x03)
                                    {
                                        break;
                                    }
                                }
                                Thread.Sleep(30);
                            }
                            else
                            {
                                if (accumulated.Count >= 5) break;
                                Thread.Sleep(50);
                            }
                        }
                        else
                        {
                            if (accumulated.Count >= 5) break;
                        }
                    }

                    if (accumulated.Count > 0)
                    {
                        string res = Encoding.ASCII.GetString(accumulated.ToArray());
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendAndReceiveUsbDirect Error: {ex.Message}");
            }
            return null;
        }

        private string QueryTsplHardwareInfoLan(string portName)
        {
            try
            {
                string ip = Regex.Match(portName, @"(\d{1,3}\.){3}\d{1,3}").Value;
                if (string.IsNullOrEmpty(ip)) return null;

                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    tcp.NoDelay = true;
                    var connectTask = tcp.ConnectAsync(ip, 9100);
                    if (Task.WhenAny(connectTask, Task.Delay(1500)).Result != connectTask) return null;

                    using (var stream = tcp.GetStream())
                    {
                        string queryCmd = 
                            "OUT \"DPI=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"DPI\");" +
                            "\"|MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");" +
                            "\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");" +
                            "\"|VER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"VERSION\");" +
                            "\"|MIL=\";GETSETTING$(\"SYSTEM\",\"RECORD\",\"MILAGE\");" +
                            "\"|SEN_T=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"SENSOR TYPE\");" +
                            "\"|W=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER WIDTH\");" +
                            "\"|H=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER SIZE\");" +
                            "\"|GAP=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"GAP SIZE\");" +
                            "\"|BLN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"BLINE SIZE\");" +
                            "\"|RIB=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"RIBBON\");" +
                            "\"|CUT=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"CUTTER\");" +
                            "\"|PMODE=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PRINT MODE\");" +
                            "\"|SPD=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"SPEED\");" +
                            "\"|DEN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"DENSITY\");" +
                            "\"|GAP_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"GAP INTENSION\");" +
                            "\"|BLN_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"BLINE INTENSION\");" +
                            "\"|OFF=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"OFFSET\");" +
                            "\"|REFX=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE X\");" +
                            "\"|REFY=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE Y\");\"|\"\r\n";

                        byte[] cmd = Encoding.ASCII.GetBytes(queryCmd);
                        stream.Write(cmd, 0, cmd.Length);
                        stream.Flush();

                        Thread.Sleep(150);

                        byte[] buffer = new byte[4096];
                        if (stream.DataAvailable || tcp.Client.Poll(2000000, System.Net.Sockets.SelectMode.SelectRead))
                        {
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                                if (response.Contains("DPI=") || response.Contains("MDL=") || response.Contains("|"))
                                {
                                    return response;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string QueryTsplHardwareInfoBluetooth(string portName)
        {
            try
            {
                string cleanPort = Regex.Match(portName, @"COM\d+", RegexOptions.IgnoreCase).Value;
                if (string.IsNullOrEmpty(cleanPort)) return null;

                using (var serial = new System.IO.Ports.SerialPort(cleanPort, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One))
                {
                    serial.ReadTimeout = 2000;
                    serial.WriteTimeout = 1500;
                    serial.Open();

                    string queryCmd = 
                        "OUT \"DPI=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"DPI\");" +
                        "\"|MDL=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"MODEL\");" +
                        "\"|SER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"SERIAL\");" +
                        "\"|VER=\";GETSETTING$(\"SYSTEM\",\"INFORMATION\",\"VERSION\");" +
                        "\"|MIL=\";GETSETTING$(\"SYSTEM\",\"RECORD\",\"MILAGE\");" +
                        "\"|SEN_T=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"SENSOR TYPE\");" +
                        "\"|W=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER WIDTH\");" +
                        "\"|H=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PAPER SIZE\");" +
                        "\"|GAP=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"GAP SIZE\");" +
                        "\"|BLN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"BLINE SIZE\");" +
                        "\"|RIB=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"RIBBON\");" +
                        "\"|CUT=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"CUTTER\");" +
                        "\"|PMODE=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"PRINT MODE\");" +
                        "\"|SPD=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"SPEED\");" +
                        "\"|DEN=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"DENSITY\");" +
                        "\"|GAP_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"GAP INTENSION\");" +
                        "\"|BLN_INT=\";GETSETTING$(\"CONFIG\",\"SENSOR\",\"BLINE INTENSION\");" +
                        "\"|OFF=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"OFFSET\");" +
                        "\"|REFX=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE X\");" +
                        "\"|REFY=\";GETSETTING$(\"CONFIG\",\"TSPL\",\"REFERENCE Y\");\"|\"\r\n";

                    byte[] cmd = Encoding.ASCII.GetBytes(queryCmd);
                    serial.Write(cmd, 0, cmd.Length);

                    Thread.Sleep(200);

                    byte[] buffer = new byte[4096];
                    int bytesRead = serial.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        if (response.Contains("DPI=") || response.Contains("MDL=") || response.Contains("|"))
                        {
                            return response;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private void ParseTsplResponse(string raw, Dictionary<string, string> details)
        {
            if (string.IsNullOrEmpty(raw)) return;

            int dpiIdx = raw.IndexOf("DPI=");
            if (dpiIdx >= 0) raw = raw.Substring(dpiIdx);

            double dpi = 203.0;
            var parts = raw.Split('|');
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part) || !part.Contains('=')) continue;
                var kv = part.Split('=');
                if (kv.Length < 2) continue;

                string k = kv[0].Trim().ToUpper();
                string v = kv[1].Trim().Replace("\"", "");

                try
                {
                    switch (k)
                    {
                        case "DPI":
                            var dpiMatch = Regex.Match(v, @"\d+");
                            if (dpiMatch.Success && double.TryParse(dpiMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedDpi) && parsedDpi > 0)
                            {
                                dpi = parsedDpi;
                                details["DPI"] = $"{parsedDpi}";
                            }
                            else details["DPI"] = v;
                            break;
                        case "MDL":
                            details["MDL"] = v;
                            break;
                        case "SER":
                            details["SER"] = v;
                            break;
                        case "VER":
                            details["VER"] = v;
                            break;
                        case "MIL":
                            var milMatch = Regex.Match(v, @"\d+(\.\d+)?");
                            if (milMatch.Success && double.TryParse(milMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double milDots))
                                details["MIL"] = $"{Math.Round(milDots * 25.4 / dpi / 1000.0, 2)} m";
                            else
                                details["MIL"] = v;
                            break;
                        case "SEN_T":
                            if (v == "0" || v.ToUpper().Contains("GAP")) details["SEN_T"] = "GAP";
                            else if (v == "1" || v.ToUpper().Contains("BLACK") || v.ToUpper().Contains("BLINE")) details["SEN_T"] = "BLACK MARK";
                            else if (v == "2" || v.ToUpper().Contains("CONT")) details["SEN_T"] = "CONTINUOUS";
                            else details["SEN_T"] = v;
                            break;
                        case "W":
                            var wMatch = Regex.Match(v, @"\d+(\.\d+)?");
                            if (wMatch.Success && double.TryParse(wMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double wDots))
                                details["W"] = $"{Math.Round(wDots * 25.4 / dpi, 1)} mm";
                            else
                                details["W"] = v;
                            break;
                        case "H":
                            var hMatch = Regex.Match(v, @"\d+(\.\d+)?");
                            if (hMatch.Success && double.TryParse(hMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double hDots))
                                details["H"] = $"{Math.Round(hDots * 25.4 / dpi, 1)} mm";
                            else
                                details["H"] = v;
                            break;
                        case "GAP":
                            var gMatch = Regex.Match(v, @"\d+(\.\d+)?");
                            if (gMatch.Success && double.TryParse(gMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double gDots))
                                details["GAP"] = $"{Math.Round(gDots * 25.4 / dpi, 1)} mm";
                            else
                                details["GAP"] = v;
                            break;
                        case "BLN":
                            var bMatch = Regex.Match(v, @"\d+(\.\d+)?");
                            if (bMatch.Success && double.TryParse(bMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double blnDots))
                                details["BLN"] = $"{Math.Round(blnDots * 25.4 / dpi, 1)} mm";
                            else
                                details["BLN"] = v;
                            break;
                        case "RIB":
                            details["RIB"] = (v == "1" || v.ToUpper().Contains("ON")) ? "ON" : "OFF";
                            break;
                        case "CUT":
                            details["CUT"] = (v == "1" || v.ToUpper().Contains("ON") || v.ToUpper().Contains("BATCH")) ? "ON" : "OFF";
                            break;
                        case "PMODE":
                            string pMode = v.ToUpper();
                            if (pMode.Contains("PEEL") || pMode == "1") details["PMODE"] = "PEELER";
                            else if (pMode.Contains("CUT") || pMode == "2") details["PMODE"] = "CUTTER";
                            else if (pMode.Contains("REWIND") || pMode == "3") details["PMODE"] = "REWINDER";
                            else details["PMODE"] = "TEAR OFF";
                            break;
                        case "SPD":
                            details["SPD"] = v;
                            break;
                        case "DEN":
                            details["DEN"] = v;
                            break;
                        case "GAP_INT":
                            var giMatch = Regex.Match(v, @"\d+");
                            if (giMatch.Success && int.TryParse(giMatch.Value, out int giVal))
                            {
                                if (giVal > 31)
                                {
                                    string s = giVal.ToString();
                                    if (s.Length > 1 && s[0] == s[1]) giVal = s[0] - '0';
                                    else giVal %= 10;
                                }
                                details["GAP_INT"] = giVal.ToString();
                            }
                            else details["GAP_INT"] = v;
                            break;
                        case "BLN_INT":
                            var biMatch = Regex.Match(v, @"\d+");
                            if (biMatch.Success && int.TryParse(biMatch.Value, out int biVal))
                            {
                                if (biVal > 31)
                                {
                                    string s = biVal.ToString();
                                    if (s.Length > 1 && s[0] == s[1]) biVal = s[0] - '0';
                                    else biVal %= 10;
                                }
                                details["BLN_INT"] = biVal.ToString();
                            }
                            else details["BLN_INT"] = v;
                            break;
                        case "CON_INT":
                            var ciMatch = Regex.Match(v, @"\d+");
                            if (ciMatch.Success && int.TryParse(ciMatch.Value, out int ciVal))
                            {
                                if (ciVal > 31)
                                {
                                    string s = ciVal.ToString();
                                    if (s.Length > 1 && s[0] == s[1]) ciVal = s[0] - '0';
                                    else ciVal %= 10;
                                }
                                details["CON_INT"] = ciVal.ToString();
                            }
                            else details["CON_INT"] = v;
                            break;
                        case "OFF":
                            details["OFF"] = (v.Contains("mm") || v.Contains("dot")) ? v : $"{v} mm";
                            break;
                        case "REFX":
                            details["REFX"] = v;
                            break;
                        case "REFY":
                            details["REFY"] = v;
                            break;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// New_xPix 정밀 진단 알고리즘: TSPL 바이너리 프레임 실시간 질의 및 하드웨어 센서 상태 판별
        /// </summary>
        private string QueryTsplLiveStatus(string portName, string connType)
        {
            if (string.IsNullOrEmpty(portName)) return null;

            try
            {
                if (connType == "LAN")
                {
                    return QueryTsplLiveStatusLan(portName);
                }
                else if (connType == "Bluetooth")
                {
                    return QueryTsplLiveStatusBluetooth(portName);
                }
                else
                {
                    return QueryTsplLiveStatusDirect(portName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QueryTsplLiveStatus 오류: {ex.Message}");
                return null;
            }
        }

        private string QueryTsplLiveStatusDirect(string portName)
        {
            string usbPath = ResolveUsbPath(portName);
            if (string.IsNullOrEmpty(usbPath)) return null;

            byte[] cmd1 = Encoding.ASCII.GetBytes("~!E\r\n\x1B!S");
            byte[] cmd2 = Encoding.ASCII.GetBytes("~!E\r\n\x1B!?");

            // 1차 시도: ESC ! S (8바이트/6바이트 정밀 프레임)
            string response = SendAndReceiveLiveStatusUsb(usbPath, cmd1);
            
            // 2차 시도 (응답 없음 시 300ms 후 ESC ! S 재시도)
            if (string.IsNullOrEmpty(response))
            {
                Thread.Sleep(300);
                response = SendAndReceiveLiveStatusUsb(usbPath, cmd1);
            }

            // 3차 시도 (Fallback: ESC ! ?)
            if (string.IsNullOrEmpty(response))
            {
                Thread.Sleep(200);
                response = SendAndReceiveLiveStatusUsb(usbPath, cmd2);
            }

            if (string.IsNullOrEmpty(response)) return null;

            return ParseTsplLiveStatusResponse(response);
        }

        private string SendAndReceiveLiveStatusUsb(string usbPath, byte[] cmdBytes)
        {
            try
            {
                IntPtr handle = CreateFile(usbPath, 0xC0000000, 0x07, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero);
                if (handle == new IntPtr(-1) || handle == IntPtr.Zero) return null;

                using (var safeHandle = new SafeFileHandle(handle, true))
                using (var fs = new FileStream(safeHandle, FileAccess.ReadWrite, 4096, true))
                {
                    fs.Write(cmdBytes, 0, cmdBytes.Length);
                    fs.Flush();

                    Thread.Sleep(120);

                    List<byte> accumulated = new List<byte>();
                    byte[] buf = new byte[1024];
                    DateTime startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalMilliseconds < 1500)
                    {
                        var readTask = fs.ReadAsync(buf, 0, buf.Length);
                        if (readTask.Wait(250))
                        {
                            int r = readTask.Result;
                            if (r > 0)
                            {
                                byte[] slice = new byte[r];
                                Array.Copy(buf, 0, slice, 0, r);
                                accumulated.AddRange(slice);

                                // 8바이트 표준 프레임(02 ... 03 0D 0A) 또는 6바이트 프레임 검출 시 즉각 종료
                                if (accumulated.Count >= 6)
                                {
                                    for (int i = 0; i <= accumulated.Count - 6; i++)
                                    {
                                        if (accumulated[i] == 0x02 && accumulated[i + 5] == 0x03)
                                        {
                                            char[] chars = new char[accumulated.Count];
                                            for (int c = 0; c < accumulated.Count; c++) chars[c] = (char)accumulated[c];
                                            return new string(chars);
                                        }
                                    }
                                }

                                Thread.Sleep(20);
                            }
                            else
                            {
                                if (accumulated.Count > 0) break;
                                Thread.Sleep(30);
                            }
                        }
                        else
                        {
                            if (accumulated.Count > 0) break;
                        }
                    }

                    if (accumulated.Count > 0)
                    {
                        char[] chars = new char[accumulated.Count];
                        for (int c = 0; c < accumulated.Count; c++) chars[c] = (char)accumulated[c];
                        return new string(chars);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendAndReceiveLiveStatusUsb Error: {ex.Message}");
            }
            return null;
        }

        private string QueryTsplLiveStatusLan(string portName)
        {
            try
            {
                string ip = Regex.Match(portName, @"(\d{1,3}\.){3}\d{1,3}").Value;
                if (string.IsNullOrEmpty(ip)) return null;

                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    tcp.NoDelay = true;
                    var connectTask = tcp.ConnectAsync(ip, 9100);
                    if (Task.WhenAny(connectTask, Task.Delay(1000)).Result != connectTask) return null;

                    using (var stream = tcp.GetStream())
                    {
                        byte[] cmd = Encoding.ASCII.GetBytes("~!E\r\n\x1B!S");
                        stream.Write(cmd, 0, cmd.Length);
                        stream.Flush();

                        Thread.Sleep(80);

                        byte[] buffer = new byte[1024];
                        if (stream.DataAvailable || tcp.Client.Poll(1000000, System.Net.Sockets.SelectMode.SelectRead))
                        {
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                            {
                                char[] chars = new char[bytesRead];
                                for (int i = 0; i < bytesRead; i++) chars[i] = (char)buffer[i];
                                return ParseTsplLiveStatusResponse(new string(chars));
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string QueryTsplLiveStatusBluetooth(string portName)
        {
            try
            {
                string cleanPort = Regex.Match(portName, @"COM\d+", RegexOptions.IgnoreCase).Value;
                if (string.IsNullOrEmpty(cleanPort)) return null;

                using (var serial = new System.IO.Ports.SerialPort(cleanPort, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One))
                {
                    serial.ReadTimeout = 1000;
                    serial.WriteTimeout = 1000;
                    serial.Open();

                    byte[] cmd = Encoding.ASCII.GetBytes("~!E\r\n\x1B!S");
                    serial.Write(cmd, 0, cmd.Length);

                    Thread.Sleep(100);

                    byte[] buffer = new byte[1024];
                    int bytesRead = serial.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        char[] chars = new char[bytesRead];
                        for (int i = 0; i < bytesRead; i++) chars[i] = (char)buffer[i];
                        return ParseTsplLiveStatusResponse(new string(chars));
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// New_xPix 슬라이딩 윈도우 프레임 스캔 알고리즘 기반 상태 파싱
        /// </summary>
        private string ParseTsplLiveStatusResponse(string res)
        {
            if (string.IsNullOrEmpty(res)) return null;

            List<string> issues = new List<string>();
            string cleanFrame = null;

            for (int i = 0; i <= res.Length - 6; i++)
            {
                // 1. 8바이트 포맷 우선 검출 (STX + 4바이트 + ETX + CR + LF)
                if (i <= res.Length - 8 && res[i] == 0x02 && res[i + 5] == 0x03 && res[i + 6] == 0x0D && res[i + 7] == 0x0A)
                {
                    cleanFrame = res.Substring(i, 8);
                    break;
                }
                // 2. 6바이트 포맷 검출 (STX + 4바이트 + ETX)
                else if (res[i] == 0x02 && res[i + 5] == 0x03)
                {
                    cleanFrame = res.Substring(i, 6);
                    break;
                }
            }

            // 1. 프레임이 검출되면, 최우선적으로 신형 바이너리 응답 정밀 분석
            if (cleanFrame != null)
            {
                byte b1 = (byte)cleanFrame[1];
                byte b4 = (byte)cleanFrame[4];

                if (b1 == 0x60) issues.Add("일시 정지 (Pause)");
                if (b4 == 0x41) issues.Add("용지 없음 (Paper Empty)");
                else if (b4 == 0x42) issues.Add("용지 걸림 (Paper Jam)");
                else if (b4 == 0x60) issues.Add("헤드 열림 (Print Head Open)");
            }
            // 2. 쉼표(,)로 구분된 아스키 텍스트 응답 분석
            else if (res.Contains(","))
            {
                var parts = res.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int statusVal))
                {
                    byte status = (byte)statusVal;
                    if (status != 0x00)
                    {
                        if ((status & 0x01) != 0) issues.Add("헤드 열림 (Head Open)");
                        if ((status & 0x02) != 0) issues.Add("용지 걸림 (Paper Jam)");
                        if ((status & 0x04) != 0) issues.Add("용지 없음 (Paper Empty)");
                        if ((status & 0x08) != 0) issues.Add("리본 없음 (No Ribbon)");
                        if ((status & 0x20) != 0) issues.Add("에러 발생 (Error)");
                    }
                }
            }
            // 3. 구형 1바이트 바이너리 비트마스크 분석
            else if (res.Length == 1 && res[0] != 'E')
            {
                byte status = (byte)res[0];
                if (status != 0x00)
                {
                    if ((status & 0x01) != 0) issues.Add("헤드 열림 (Head Open)");
                    if ((status & 0x02) != 0) issues.Add("용지 걸림 (Paper Jam)");
                    if ((status & 0x04) != 0) issues.Add("용지 없음 (Paper Empty)");
                    if ((status & 0x08) != 0) issues.Add("리본 없음 (No Ribbon)");
                    if ((status & 0x20) != 0) issues.Add("에러 발생 (Error)");
                }
            }

            if (issues.Count > 0)
            {
                return string.Join(", ", issues);
            }

            return "정상 준비 (Ready)";
        }
    }
}

