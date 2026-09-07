using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrint.Model
{
    public class USB_status
    {
        static public List<string> Name;
        static public List<string> Port;
        static public List<bool> Connect;
        static public List<float> DPI;


        public static void USB_init() {
            Name = new List<string>();
            Port = new List<string>();
            Connect = new List<bool>();
            DPI = new List<float>();
        }
        public static void GetPrinterPortInfo()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GetPrinterPortInfo 시작");
                
                // USB_status 초기화 (null 체크)
                if (USB_status.Name == null)
                {
                    USB_status.USB_init();
                }
                
                // 기존 데이터 초기화
                USB_status.Name.Clear();
                USB_status.Port.Clear();
                USB_status.DPI.Clear();
                USB_status.Connect.Clear();
                
                // DLL/WMI 의존성을 배제하고 Registry와 Spooler API를 사용하여 안정적이고 빠르게 프린터 목록을 가져옵니다.
                RunRegistryFallback();

                System.Diagnostics.Debug.WriteLine($"총 {USB_status.Name.Count}개의 프린터 발견");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPrinterPortInfo 오류: {ex.Message}");
            }
        }

        private static void RunRegistryFallback()
        {
            bool wmiSuccess = false;
            try
            {
                System.Diagnostics.Debug.WriteLine("[GetPrinterPortInfo] WMI 기반 프린터 탐색 시작 (실제 연결된 장치 필터)");
                
                using (var searcher = new ManagementObjectSearcher("SELECT Name, PortName, WorkOffline FROM Win32_Printer"))
                using (var results = searcher.Get())
                {
                    wmiSuccess = true; // 예외 없이 조회가 완료되면 성공으로 간주
                    foreach (ManagementObject printer in results)
                    {
                        string printerName = printer["Name"] as string;
                        string portName = printer["PortName"] as string;
                        bool workOffline = false;
                        
                        if (printer["WorkOffline"] != null)
                        {
                            workOffline = (bool)printer["WorkOffline"];
                        }

                        if (string.IsNullOrEmpty(printerName) || string.IsNullOrEmpty(portName))
                            continue;

                        // Xprinter 모델명 검사
                        bool isXprinter = PrinterAuthenticatorWrapper.IsXPrinterModel(printerName) ||
                                          printerName.IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          printerName.IndexOf("XP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          printerName.IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0;

                        // 포트가 USB로 시작하고, 오프라인 상태가 아닌 것만 추가 (실제 연결됨)
                        if (isXprinter && portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase) && !workOffline)
                        {
                            float dpiValue = 203f;
                            if (printerName.IndexOf("XP-423B", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                printerName.IndexOf("XP423B", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                printerName.IndexOf("423B", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                printerName.IndexOf("XP-433B", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                dpiValue = 203f;
                            }

                            USB_status.Name.Add(printerName);
                            USB_status.Port.Add(portName);
                            USB_status.DPI.Add(dpiValue);
                            USB_status.Connect.Add(true);

                            System.Diagnostics.Debug.WriteLine($"[WMI 프린터 추가(Xprinter/USB/온라인)] {printerName} (포트: {portName})");
                        }
                    }
                }
            }
            catch (Exception wmiEx)
            {
                System.Diagnostics.Debug.WriteLine($"WMI 기반 탐색 오류: {wmiEx.Message}");
                wmiSuccess = false;
            }

            // WMI 탐색이 완전히 실패했을 때만 기존의 설치된 모든 장치를 보여주는 폴백 실행
            if (!wmiSuccess)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[GetPrinterPortInfo] WMI 실패. 기존 Registry 기반 폴백 탐색 시작");
                    USB_status.Name.Clear();
                    USB_status.Port.Clear();
                    USB_status.DPI.Clear();
                    USB_status.Connect.Clear();

                    foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                    {
                        bool isXprinter = PrinterAuthenticatorWrapper.IsXPrinterModel(printerName) ||
                                          printerName.IndexOf("Xprinter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          printerName.IndexOf("XP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          printerName.IndexOf("XP ", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (isXprinter)
                        {
                            string portName = GetPrinterPortFromRegistry(printerName);
                            if (!string.IsNullOrEmpty(portName) && portName.StartsWith("USB", StringComparison.OrdinalIgnoreCase))
                            {
                                float dpiValue = 203f;
                                if (printerName.IndexOf("XP-423B", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                    printerName.IndexOf("XP423B", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                    printerName.IndexOf("423B", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    printerName.IndexOf("XP-433B", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    dpiValue = 203f;
                                }

                                USB_status.Name.Add(printerName);
                                USB_status.Port.Add(portName);
                                USB_status.DPI.Add(dpiValue);
                                USB_status.Connect.Add(true);

                                System.Diagnostics.Debug.WriteLine($"[폴백 프린터 추가(Xprinter/USB)] {printerName} (포트: {portName})");
                            }
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Registry 기반 폴백 탐색 오류: {fallbackEx.Message}");
                }
            }
        }

        /// <summary>
        /// 레지스트리에서 프린터명에 해당하는 포트 이름을 가져옵니다.
        /// </summary>
        private static string GetPrinterPortFromRegistry(string printerName)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Print\Printers\{printerName}"))
                {
                    if (key != null)
                    {
                        return key.GetValue("Port") as string;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPrinterPortFromRegistry 오류: {ex.Message}");
            }
            return null;
        }



        public static string getTSPLPrintState(IntPtr hPrinter, int openStatus, out int value)
        {
            value = 0;
            
            // USB_status 초기화 확인
            if (USB_status.Name == null)
            {
                USB_status.USB_init();
            }
            if (openStatus != 0)
            {
                return "프린터를 연결해 주세요";
            }

            int status = 0;
            int ret = TSPLPrint.TSPL_GetPrinterStatus(hPrinter, out status);
            value = status != 0 ? status : ret;

            // 상태값이 0이면 정상 준비 상태
            int checkVal = (status != 0) ? status : ret;
            if (checkVal == 0)
            {
                return "인쇄 대기중";
            }

            var errorList = new List<string>();

            if ((checkVal & 0x01) != 0) errorList.Add("헤드 열림");
            if ((checkVal & 0x02) != 0) errorList.Add("용지 걸림");
            if ((checkVal & 0x04) != 0) errorList.Add("용지 없음");
            if ((checkVal & 0x08) != 0) errorList.Add("리본 없음");
            if ((checkVal & 0x10) != 0) errorList.Add("일시 정지(Pause)");
            if ((checkVal & 0x20) != 0) return "인쇄 중";
            if ((checkVal & 0x40) != 0) errorList.Add("커버 열림");
            if ((checkVal & 0x80) != 0) errorList.Add("프린터 과열/오류");

            if (errorList.Count > 0)
            {
                return string.Join(", ", errorList);
            }

            return "인쇄 대기중";
        }



    }
}
